import assert from 'node:assert/strict'
import { after, before, describe, test } from 'node:test'
import { mkdtemp, rm } from 'node:fs/promises'
import { tmpdir } from 'node:os'
import { dirname, resolve } from 'node:path'
import { fileURLToPath } from 'node:url'
import React from 'react'
import { renderToStaticMarkup } from 'react-dom/server'
import { createInstance } from 'i18next'
import { I18nextProvider, initReactI18next } from 'react-i18next'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { createServer } from 'vite'
import { createPluginsApi } from '../src/renderer/src/api/pluginsCore.ts'
import {
  filterPlugins,
  pluginCardActions,
  pluginFileName,
  runPluginOperations,
  summarizePlugins,
  uninstallFeedback
} from '../src/renderer/src/pages/pluginExtensionsModel.ts'
import {
  bindPluginWebviewListeners,
  buildPluginPageSource,
  buildPluginPartition,
  buildPluginPreloadUrl,
  fileUrlFromAbsolutePath
} from '../src/renderer/src/components/plugins/pluginPageViewModel.ts'
import {
  createPluginsStoreState,
  reduceInstallProgress
} from '../src/renderer/src/stores/pluginsStoreCore.ts'

const electronRoot = resolve(dirname(fileURLToPath(import.meta.url)), '..')

function plugin(overrides = {}) {
  const base = {
    id: 'sample.plugin',
    name: 'Sample Plugin',
    description: 'A renderer test plugin',
    author: 'UDT',
    version: '1.0.0',
    icon: 'SP',
    tags: ['utility'],
    isSystemPlugin: false,
    dependencies: [],
    releaseDate: '2026-08-01',
    fileSize: 1024,
    updateAvailable: false,
    state: 'NotInstalled',
    directory: null,
    webPage: null,
    capabilities: {
      settingsPage: false,
      featurePage: false,
      optimizationCategory: false,
      webPage: false,
      executableEntryPoint: false
    }
  }

  return {
    ...base,
    ...overrides,
    capabilities: {
      ...base.capabilities,
      ...overrides.capabilities
    }
  }
}

function mockPluginsApi(overrides = {}) {
  return {
    list: async () => ({ plugins: [], online: true }),
    checkUpdates: async () => ({ updates: [] }),
    install: async () => ({ ok: true, degraded: false, unloadPending: false }),
    uninstall: async () => ({ ok: true, degraded: false, unloadPending: false }),
    importFile: async () => ({ ok: true, degraded: false, unloadPending: false }),
    refresh: async () => ({
      ok: true,
      registeredCount: 0,
      degraded: false,
      unloadPending: false,
      failures: []
    }),
    onInstallProgress: () => () => undefined,
    onInstalled: () => () => undefined,
    onUninstalled: () => () => undefined,
    ...overrides
  }
}

function createStoreHarness(api) {
  let state
  const set = (update) => {
    const patch = typeof update === 'function' ? update(state) : update
    state = { ...state, ...patch }
  }
  const get = () => state
  state = createPluginsStoreState(api, set, get)
  return { get, set }
}

function rendererTestMocks() {
  const iconsId = '\0plugin-renderer-icons'
  const antdId = '\0plugin-renderer-antd'
  const storeId = '\0plugin-renderer-store'
  return {
    name: 'plugin-renderer-test-mocks',
    enforce: 'pre',
    resolveId(source) {
      if (source.endsWith('icons/fluent')) return iconsId
      if (source.endsWith('stores/pluginsStore')) return storeId
      if (source === 'antd') return antdId
      return null
    },
    load(id) {
      if (id === iconsId) {
        return `
          import React from 'react'
          const Icon = (props) => React.createElement('svg', props)
          export {
            Icon as Apps24Regular,
            Icon as ArrowCircleUp24Regular,
            Icon as ArrowClockwise24Regular,
            Icon as ArrowDownload24Regular,
            Icon as ArrowLeft24Regular,
            Icon as Checkmark24Regular,
            Icon as CheckmarkCircle24Filled,
            Icon as CheckmarkCircle24Regular,
            Icon as ChevronDown24Regular,
            Icon as ChevronUp24Regular,
            Icon as Copy24Regular,
            Icon as Delete24Regular,
            Icon as DocumentQuestionMark24Regular,
            Icon as ErrorCircle24Regular,
            Icon as FluentLoadingIcon,
            Icon as FolderOpen24Regular,
            Icon as Search24Regular,
            Icon as Settings24Regular
          }
        `
      }
      if (id === antdId) {
        return `
          import React from 'react'
          export const Button = ({ children }) => React.createElement('button', null, children)
          export const Input = ({ placeholder }) => React.createElement('input', { placeholder })
          export const Select = () => React.createElement('select')
          export const Spin = () => React.createElement('span')
          export const Tooltip = ({ children }) => React.createElement(React.Fragment, null, children)
          export const Popconfirm = ({ children }) => React.createElement(React.Fragment, null, children)
          export const Modal = ({ open, children }) => open ? React.createElement('div', null, children) : null
          export const Alert = ({ message }) => React.createElement('div', null, message)
          export const message = {
            error() {},
            info() {},
            success() {},
            warning() {}
          }
        `
      }
      if (id === storeId) {
        return `
          const state = globalThis.__pluginRendererTestStore ??= {
            plugins: [],
            updates: {},
            installingIds: {},
            loading: false,
            offline: false,
            error: null,
            load: async () => undefined,
            install: async () => true,
            uninstall: async () => ({ ok: true, dependencyBlocked: false }),
            refresh: async () => undefined,
            importFile: async () => true
          }
          export const usePluginsStore = (selector) => selector(state)
          usePluginsStore.getState = () => state
          usePluginsStore.setState = (update) => {
            Object.assign(state, typeof update === 'function' ? update(state) : update)
          }
        `
      }
      return null
    }
  }
}

test('plugin API maps renderer calls and event subscriptions to bridge contracts', async () => {
  const calls = []
  const subscriptions = []
  const responses = {
    'plugins.list': { plugins: [], online: true },
    'plugins.checkUpdates': { updates: [] },
    'plugins.install': { ok: true },
    'plugins.uninstall': { ok: true, dependencyBlocked: false },
    'plugins.import': { ok: true },
    'plugins.refresh': { registeredCount: 3 }
  }
  const invoke = async (method, params) => {
    calls.push({ method, params })
    return responses[method]
  }
  const on = (event, callback) => {
    const subscription = { event, callback, removed: false }
    subscriptions.push(subscription)
    return () => {
      subscription.removed = true
    }
  }
  const api = createPluginsApi(invoke, on)

  await api.list()
  await api.list(true)
  await api.checkUpdates()
  await api.install('alpha')
  await api.uninstall('beta')
  await api.importFile('C:\\packages\\gamma.zip')
  await api.refresh()

  assert.deepEqual(calls, [
    { method: 'plugins.list', params: {} },
    { method: 'plugins.list', params: { forceRefresh: true } },
    { method: 'plugins.checkUpdates', params: {} },
    { method: 'plugins.install', params: { pluginId: 'alpha' } },
    { method: 'plugins.uninstall', params: { pluginId: 'beta' } },
    { method: 'plugins.import', params: { filePath: 'C:\\packages\\gamma.zip' } },
    { method: 'plugins.refresh', params: {} }
  ])

  let receivedProgress = null
  const offProgress = api.onInstallProgress((progress) => {
    receivedProgress = progress
  })
  const offInstalled = api.onInstalled(() => undefined)
  const offUninstalled = api.onUninstalled(() => undefined)
  assert.deepEqual(subscriptions.map(({ event }) => event), [
    'plugins.installProgress',
    'plugins.installed',
    'plugins.uninstalled'
  ])

  subscriptions[0].callback({
    pluginId: 'alpha',
    progressPercentage: 40,
    statusText: 'Downloading',
    phase: 'downloading'
  })
  assert.equal(receivedProgress.progressPercentage, 40)
  offProgress()
  offInstalled()
  offUninstalled()
  assert.ok(subscriptions.every(({ removed }) => removed))
})

test('plugin filters cover all states and searchable fields', () => {
  const plugins = [
    plugin({
      id: 'alpha.device',
      name: 'Alpha Device',
      description: 'Controls keyboards',
      tags: ['input'],
      installedVersion: '1.0.0',
      state: 'Installed'
    }),
    plugin({
      id: 'beta.network',
      name: 'Beta Network',
      description: 'Accelerates downloads',
      tags: ['network', 'speed']
    }),
    plugin({
      id: 'gamma.display',
      name: 'Gamma Display',
      description: 'Color profiles',
      tags: ['visual'],
      installedVersion: '2.0.0',
      state: 'Installed'
    })
  ]

  assert.deepEqual(filterPlugins(plugins, 'all', '').map(({ id }) => id), [
    'alpha.device',
    'beta.network',
    'gamma.display'
  ])
  assert.deepEqual(filterPlugins(plugins, 'installed', '').map(({ id }) => id), [
    'alpha.device',
    'gamma.display'
  ])
  assert.deepEqual(filterPlugins(plugins, 'notInstalled', '').map(({ id }) => id), [
    'beta.network'
  ])
  assert.deepEqual(filterPlugins(plugins, 'all', '  KEYBOARD  ').map(({ id }) => id), [
    'alpha.device'
  ])
  assert.deepEqual(filterPlugins(plugins, 'all', 'beta.network').map(({ id }) => id), [
    'beta.network'
  ])
  assert.deepEqual(filterPlugins(plugins, 'all', 'SPEED').map(({ id }) => id), [
    'beta.network'
  ])
  assert.deepEqual(filterPlugins(plugins, 'installed', 'visual').map(({ id }) => id), [
    'gamma.display'
  ])
})

test('plugin summaries produce installed, update, and installable identities', () => {
  const plugins = [
    plugin({ id: 'installed', installedVersion: '1.0.0', state: 'Installed' }),
    plugin({
      id: 'update',
      installedVersion: '1.0.0',
      state: 'Installed',
      updateAvailable: true,
      availableVersion: '2.0.0'
    }),
    plugin({ id: 'remote' }),
    plugin({ id: 'system', isSystemPlugin: true })
  ]

  assert.deepEqual(summarizePlugins(plugins), {
    totalCount: 4,
    installedCount: 2,
    updateCount: 1,
    installableIds: ['remote'],
    updatableIds: ['update']
  })
})

test('plugin card actions stay gated by installation state and capabilities', () => {
  const installed = plugin({
    installedVersion: '1.0.0',
    state: 'Installed',
    webPage: 'web/index.html',
    capabilities: {
      settingsPage: true,
      featurePage: true
    }
  })
  assert.deepEqual(pluginCardActions(installed), {
    installed: true,
    canInstallOrUpdate: false,
    canConfigure: true,
    canOpenWebPage: true,
    canOpenCapability: true,
    canUninstall: true
  })

  const notInstalled = plugin({
    webPage: 'web/index.html',
    capabilities: {
      settingsPage: true,
      executableEntryPoint: true
    }
  })
  assert.deepEqual(pluginCardActions(notInstalled), {
    installed: false,
    canInstallOrUpdate: true,
    canConfigure: false,
    canOpenWebPage: false,
    canOpenCapability: false,
    canUninstall: false
  })

  assert.equal(uninstallFeedback({ ok: false, dependencyBlocked: true }), 'dependencyBlocked')
  assert.equal(uninstallFeedback({ ok: false, dependencyBlocked: false }), 'failed')
  assert.equal(uninstallFeedback({ ok: true, dependencyBlocked: false }), null)
})

test('batch operations retain successful and failed items without aborting', async () => {
  const attempted = []
  const result = await runPluginOperations(
    ['alpha', 'beta', 'gamma'],
    async (pluginId) => {
      attempted.push(pluginId)
      if (pluginId === 'beta') throw new Error('network unavailable')
      return pluginId === 'alpha'
    }
  )

  assert.deepEqual(attempted, ['alpha', 'beta', 'gamma'])
  assert.deepEqual(result, {
    succeeded: ['alpha'],
    failed: ['beta', 'gamma']
  })
  assert.equal(pluginFileName('C:\\packages\\alpha.zip'), 'alpha.zip')
  assert.equal(pluginFileName('/packages/beta.zip'), 'beta.zip')
})

test('store load derives update state and offline status', async () => {
  const listed = [plugin({ id: 'alpha', installedVersion: '1.0.0', state: 'Installed' })]
  const forceValues = []
  const store = createStoreHarness(mockPluginsApi({
    list: async (force) => {
      forceValues.push(force)
      return { plugins: listed, online: false }
    },
    checkUpdates: async () => ({
      updates: [{ id: 'alpha', availableVersion: '2.0.0' }]
    })
  }))

  await store.get().load(true)

  assert.deepEqual(forceValues, [true])
  assert.equal(store.get().loading, false)
  assert.equal(store.get().offline, true)
  assert.equal(store.get().error, null)
  assert.deepEqual(store.get().plugins, listed)
  assert.deepEqual(store.get().updates, { alpha: '2.0.0' })
})

test('store install, update, uninstall, and import reload the projected state', async () => {
  let catalog = [
    plugin({ id: 'remote', version: '1.0.0' }),
    plugin({
      id: 'updatable',
      version: '2.0.0',
      installedVersion: '1.0.0',
      availableVersion: '2.0.0',
      updateAvailable: true,
      state: 'Installed'
    })
  ]
  const operations = []
  const api = mockPluginsApi({
    list: async () => ({ plugins: catalog.map((entry) => ({ ...entry })), online: true }),
    checkUpdates: async () => ({
      updates: catalog
        .filter(({ updateAvailable }) => updateAvailable)
        .map(({ id, availableVersion }) => ({ id, availableVersion }))
    }),
    install: async (pluginId) => {
      operations.push(`install:${pluginId}`)
      catalog = catalog.map((entry) => {
        if (entry.id !== pluginId) return entry
        return {
          ...entry,
          installedVersion: entry.availableVersion ?? entry.version,
          updateAvailable: false,
          state: 'Installed'
        }
      })
      return { ok: true }
    },
    uninstall: async (pluginId) => {
      operations.push(`uninstall:${pluginId}`)
      catalog = catalog.map((entry) => entry.id === pluginId
        ? {
            ...entry,
            installedVersion: undefined,
            state: 'NotInstalled'
          }
        : entry)
      return { ok: true }
    },
    importFile: async (path) => {
      operations.push(`import:${path}`)
      catalog = [...catalog, plugin({
        id: 'imported',
        installedVersion: '1.0.0',
        state: 'Installed'
      })]
      return { ok: true }
    }
  })
  const store = createStoreHarness(api)

  await store.get().load()
  assert.equal(await store.get().install('remote'), true)
  assert.equal(store.get().plugins.find(({ id }) => id === 'remote').installedVersion, '1.0.0')
  assert.equal(await store.get().install('updatable'), true)
  assert.equal(store.get().plugins.find(({ id }) => id === 'updatable').installedVersion, '2.0.0')
  assert.deepEqual(store.get().updates, {})

  assert.deepEqual(await store.get().uninstall('remote'), {
    ok: true,
    dependencyBlocked: false
  })
  assert.equal(store.get().plugins.find(({ id }) => id === 'remote').installedVersion, undefined)

  assert.equal(await store.get().importFile('C:\\packages\\imported.zip'), true)
  assert.ok(store.get().plugins.some(({ id }) => id === 'imported'))
  assert.deepEqual(operations, [
    'install:remote',
    'install:updatable',
    'uninstall:remote',
    'import:C:\\packages\\imported.zip'
  ])
})

test('store operation failures clear progress and expose useful errors', async () => {
  const api = mockPluginsApi({
    install: async () => ({ ok: false }),
    uninstall: async () => ({ ok: false }),
    importFile: async () => ({ ok: false }),
    refresh: async () => {
      throw new Error('refresh unavailable')
    }
  })
  const store = createStoreHarness(api)

  assert.equal(await store.get().install('broken'), false)
  assert.equal(store.get().error, 'Failed to install plugin: broken')
  assert.deepEqual(store.get().installingIds, {})

  api.install = async () => {
    throw new Error('download failed')
  }
  assert.equal(await store.get().install('broken-update'), false)
  assert.equal(store.get().error, 'download failed')
  assert.deepEqual(store.get().installingIds, {})

  assert.deepEqual(await store.get().uninstall('broken'), {
    ok: false,
    dependencyBlocked: false
  })
  assert.equal(store.get().error, 'Failed to uninstall plugin: broken')

  api.uninstall = async () => ({ ok: false, dependencyBlocked: true })
  assert.deepEqual(await store.get().uninstall('required'), {
    ok: false,
    dependencyBlocked: true
  })
  assert.equal(store.get().error, null)

  api.uninstall = async () => {
    throw new Error('uninstall unavailable')
  }
  assert.deepEqual(await store.get().uninstall('broken'), {
    ok: false,
    dependencyBlocked: false
  })
  assert.equal(store.get().error, 'uninstall unavailable')

  assert.equal(await store.get().importFile('broken.zip'), false)
  assert.equal(store.get().error, 'Failed to import plugin package: broken.zip')

  api.importFile = async () => {
    throw new Error('invalid package')
  }
  assert.equal(await store.get().importFile('invalid.zip'), false)
  assert.equal(store.get().error, 'invalid package')

  await store.get().refresh()
  assert.equal(store.get().error, 'refresh unavailable')

  api.list = async () => {
    throw new Error('store offline')
  }
  await store.get().load()
  assert.equal(store.get().loading, false)
  assert.equal(store.get().error, 'store offline')
})

test('store retains and surfaces structured degraded operation outcomes', async () => {
  const installOutcome = {
    ok: false,
    degraded: true,
    unloadPending: true,
    recoveryId: 'held-plugin',
    recoveryPath: 'C:\\recovery\\held-plugin',
    error: 'Runtime unload is pending.'
  }
  const importOutcome = {
    ok: false,
    degraded: true,
    unloadPending: false,
    recoveryId: 'imported-plugin',
    recoveryPath: 'C:\\recovery\\imported-plugin',
    error: 'Rollback material was retained.'
  }
  const refreshOutcome = {
    ok: false,
    registeredCount: 2,
    degraded: true,
    unloadPending: true,
    failures: [installOutcome]
  }
  const store = createStoreHarness(mockPluginsApi({
    install: async () => installOutcome,
    importFile: async () => importOutcome,
    refresh: async () => refreshOutcome
  }))

  assert.equal(await store.get().install('held-plugin'), false)
  assert.deepEqual(store.get().lastOperationOutcome, installOutcome)
  assert.match(store.get().error, /Runtime unload is pending/)
  assert.match(store.get().error, /C:\\recovery\\held-plugin/)

  assert.equal(await store.get().importFile('held.zip'), false)
  assert.deepEqual(store.get().lastOperationOutcome, importOutcome)
  assert.match(store.get().error, /Rollback material was retained/)

  await store.get().refresh()
  assert.deepEqual(store.get().lastScanOutcome, refreshOutcome)
  assert.match(store.get().error, /Runtime unload is pending/)
})

test('install progress only updates active operations and removes terminal entries', () => {
  const store = createStoreHarness(mockPluginsApi())
  store.set({ installingIds: { alpha: 0 } })

  const ignored = reduceInstallProgress(store.get(), {
    pluginId: 'other',
    progressPercentage: 50,
    statusText: 'Downloading',
    phase: 'downloading'
  })
  assert.equal(ignored, null)

  store.set(reduceInstallProgress(store.get(), {
    pluginId: 'alpha',
    progressPercentage: 42.5,
    statusText: 'Downloading',
    phase: 'downloading'
  }))
  assert.deepEqual(store.get().installingIds, { alpha: 42.5 })

  store.set(reduceInstallProgress(store.get(), {
    pluginId: 'alpha',
    progressPercentage: 100,
    statusText: 'Complete',
    phase: 'completed'
  }))
  assert.deepEqual(store.get().installingIds, {})
})

test('webview URLs and partitions are deterministic and path-safe', () => {
  assert.equal(
    fileUrlFromAbsolutePath('C:\\Program Files\\UDT Plugins\\plugin-host.js'),
    'file:///C:/Program%20Files/UDT%20Plugins/plugin-host.js'
  )
  assert.equal(
    buildPluginPageSource(
      'C:\\Program Files\\UDT Plugins\\alpha #1',
      '.\\web\\index #1.html'
    ),
    'file:///C:/Program%20Files/UDT%20Plugins/alpha%20%231/web/index%20%231.html'
  )
  assert.equal(
    buildPluginPageSource('/opt/udt plugins/alpha', 'web/index.html'),
    'file:///opt/udt%20plugins/alpha/web/index.html'
  )
  assert.equal(
    buildPluginPreloadUrl('C:\\UDT\\out\\preload\\plugin-host.js'),
    'file:///C:/UDT/out/preload/plugin-host.js'
  )

  for (const unsafePage of [
    '../outside.html',
    'web/../../outside.html',
    '/absolute.html',
    'C:\\outside.html',
    'https://example.invalid/plugin.html'
  ]) {
    assert.equal(buildPluginPageSource('C:\\UDT\\plugins\\alpha', unsafePage), null)
  }
  assert.equal(fileUrlFromAbsolutePath('relative/plugin-host.js'), null)
  assert.equal(fileUrlFromAbsolutePath('C:\\UDT\\..\\outside.js'), null)

  const partition = buildPluginPartition('vendor/plugin:alpha?#')
  assert.equal(partition, 'persist:plugin-vendor%2Fplugin%3Aalpha%3F%23')
  assert.equal(buildPluginPartition('vendor/plugin:alpha?#'), partition)
  assert.equal(/[\\/:?#]/.test(partition.slice('persist:'.length)), false)
})

test('webview lifecycle listeners handle ready/failure and clean up once', () => {
  const listeners = new Map()
  const removed = []
  const webview = {
    addEventListener(type, listener) {
      listeners.set(type, listener)
    },
    removeEventListener(type, listener) {
      if (listeners.get(type) === listener) listeners.delete(type)
      removed.push(type)
    }
  }
  const states = []
  const cleanup = bindPluginWebviewListeners(
    webview,
    () => states.push('ready'),
    () => states.push('failed')
  )

  listeners.get('dom-ready')()
  listeners.get('did-fail-load')()
  assert.deepEqual(states, ['ready', 'failed'])

  cleanup()
  cleanup()
  assert.deepEqual([...listeners.keys()], [])
  assert.deepEqual(removed, ['did-fail-load', 'dom-ready'])
})

describe('plugin renderer components', () => {
  let viteServer
  let i18n
  let PluginExtensionsPage
  let PluginPageView
  let usePluginsStore
  let previousWindow
  let viteCacheDir

  before(async () => {
    previousWindow = globalThis.window
    globalThis.window = globalThis
    globalThis.window.innerWidth = 1280
    globalThis.window.innerHeight = 720
    globalThis.window.bridge = {
      platform: 'win32',
      invoke: async (method) => {
        if (method === 'plugins.list') return { plugins: [], online: true }
        if (method === 'plugins.checkUpdates') return { updates: [] }
        return {}
      },
      on: () => () => undefined,
      getPluginPreloadPath: async () => 'C:\\UDT\\out\\preload\\plugin-host.js',
      selectPluginFiles: async () => []
    }

    i18n = createInstance()
    await i18n.use(initReactI18next).init({
      lng: 'en',
      resources: {
        en: {
          translation: {
            common: {
              retry: 'Retry'
            },
            plugins: {
              title: 'Plugins and Extensions',
              description: 'Manage plugins',
              notFound: 'Plugin not found',
              noWebPage: 'This plugin has no web interface.',
              pageLoading: 'Loading plugin page...',
              pageLoadFailed: 'The plugin page could not be loaded.',
              back: 'Back to plugins',
              summaryTotal: 'Total plugins',
              summaryInstalled: 'Installed',
              summaryUpdates: 'Updates available',
              search: 'Search plugins',
              filterAll: 'All',
              filterInstalled: 'Installed',
              filterNotInstalled: 'Not installed',
              importFromFiles: 'Import from files',
              refresh: 'Refresh',
              updateAll: 'Update all',
              installAll: 'Install all',
              update: 'Update',
              install: 'Install',
              uninstall: 'Uninstall',
              uninstallConfirm: 'Uninstall?',
              configure: 'Configure',
              openPage: 'Open plugin page',
              open: 'Open',
              installed: 'Installed',
              updateAvailable: 'Update available',
              local: 'Local',
              empty: 'No plugins',
              emptyStore: 'No plugins available'
            }
          }
        }
      }
    })

    viteCacheDir = await mkdtemp(resolve(tmpdir(), 'udt-plugin-renderer-tests-'))
    viteServer = await createServer({
      root: electronRoot,
      cacheDir: viteCacheDir,
      configFile: false,
      logLevel: 'silent',
      plugins: [rendererTestMocks()],
      server: {
        middlewareMode: true
      },
      appType: 'custom',
      optimizeDeps: {
        noDiscovery: true,
        include: []
      }
    })

    const pageModule = await viteServer.ssrLoadModule(
      '/src/renderer/src/components/plugins/PluginPageView.tsx'
    )
    const extensionsModule = await viteServer.ssrLoadModule(
      '/src/renderer/src/pages/PluginExtensionsPage.tsx'
    )
    PluginPageView = pageModule.default
    PluginExtensionsPage = extensionsModule.default
    usePluginsStore = {
      setState(update) {
        const state = globalThis.__pluginRendererTestStore
        Object.assign(state, typeof update === 'function' ? update(state) : update)
      }
    }
  })

  after(async () => {
    await viteServer?.close()
    if (viteCacheDir != null) {
      await rm(viteCacheDir, { recursive: true, force: true })
    }
    if (previousWindow === undefined) {
      delete globalThis.window
    } else {
      globalThis.window = previousWindow
    }
  })

  function translated(element) {
    return React.createElement(I18nextProvider, { i18n }, element)
  }

  function renderPluginPage(pluginId, plugins, loading = false) {
    usePluginsStore.setState({
      plugins,
      loading,
      offline: false,
      error: null,
      installingIds: {},
      updates: {}
    })
    const routedPage = React.createElement(
      MemoryRouter,
      { initialEntries: [`/plugins/${pluginId}`] },
      React.createElement(
        Routes,
        null,
        React.createElement(Route, {
          path: '/plugins/:pluginId',
          element: React.createElement(PluginPageView)
        })
      )
    )
    return renderToStaticMarkup(translated(routedPage))
  }

  function renderExtensions(plugins, error = null) {
    usePluginsStore.setState({
      plugins,
      loading: false,
      offline: false,
      error,
      installingIds: {},
      updates: {}
    })
    const page = React.createElement(
      MemoryRouter,
      { initialEntries: ['/plugins'] },
      React.createElement(PluginExtensionsPage)
    )
    return renderToStaticMarkup(translated(page))
  }

  test('PluginPageView renders unknown, loading, and missing-page states', () => {
    const unknown = renderPluginPage('missing', [], false)
    assert.match(unknown, /Plugin not found/)

    const loading = renderPluginPage('missing', [], true)
    assert.match(loading, /Loading plugin page\.\.\./)
    assert.doesNotMatch(loading, /Plugin not found/)

    const withoutWebPage = renderPluginPage('alpha', [
      plugin({
        id: 'alpha',
        name: 'Alpha',
        installedVersion: '1.0.0',
        state: 'Installed',
        directory: 'C:\\UDT\\plugins\\alpha'
      })
    ])
    assert.match(withoutWebPage, /This plugin has no web interface\./)

    const embeddedLoading = renderPluginPage('alpha', [
      plugin({
        id: 'alpha',
        name: 'Alpha',
        installedVersion: '1.0.0',
        state: 'Installed',
        directory: 'C:\\UDT\\plugins\\alpha',
        webPage: 'web/index.html'
      })
    ])
    assert.match(embeddedLoading, /Loading plugin page\.\.\./)
    assert.match(embeddedLoading, /Alpha/)
  })

  test('PluginExtensionsPage renders counts, failures, and gated actions', () => {
    const plugins = [
      plugin({
        id: 'alpha',
        name: 'Alpha Settings',
        installedVersion: '1.0.0',
        state: 'Installed',
        capabilities: { settingsPage: true }
      }),
      plugin({
        id: 'beta',
        name: 'Beta Web',
        installedVersion: '1.0.0',
        state: 'Installed',
        updateAvailable: true,
        availableVersion: '2.0.0',
        directory: 'C:\\UDT\\plugins\\beta',
        webPage: 'web/index.html'
      }),
      plugin({
        id: 'gamma',
        name: 'Gamma Remote',
        capabilities: { settingsPage: true }
      }),
      plugin({
        id: 'system',
        name: 'System Local',
        isSystemPlugin: true
      })
    ]
    const markup = renderExtensions(plugins, 'Plugin operation failed')
    const metricValues = [...markup.matchAll(
      /udt-plugins-page__metric-value">(\d+)</g
    )].map((match) => Number(match[1]))

    assert.deepEqual(metricValues, [4, 2, 1])
    assert.match(markup, /Plugin operation failed/)
    assert.match(markup, /aria-label="Configure Alpha Settings"/)
    assert.match(markup, /aria-label="Open Alpha Settings"/)
    assert.match(markup, /aria-label="Open plugin page Beta Web"/)
    assert.match(markup, /aria-label="Update Beta Web"/)
    assert.match(markup, /aria-label="Install Gamma Remote"/)
    assert.doesNotMatch(markup, /aria-label="Configure Gamma Remote"/)
    assert.doesNotMatch(markup, /aria-label="Open Gamma Remote"/)
  })
})
