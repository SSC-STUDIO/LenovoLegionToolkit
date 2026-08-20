import assert from 'node:assert/strict'
import { readFileSync } from 'node:fs'
import { createRequire } from 'node:module'
import test from 'node:test'
import { fileURLToPath } from 'node:url'
import vm from 'node:vm'
import ts from 'typescript'

const nodeRequire = createRequire(import.meta.url)

function compileModule(fileUrl) {
  const fileName = fileURLToPath(fileUrl)
  const result = ts.transpileModule(readFileSync(fileUrl, 'utf8'), {
    fileName,
    reportDiagnostics: true,
    compilerOptions: {
      module: ts.ModuleKind.CommonJS,
      target: ts.ScriptTarget.ES2022
    }
  })
  const errors = (result.diagnostics ?? []).filter(
    (diagnostic) => diagnostic.category === ts.DiagnosticCategory.Error
  )
  if (errors.length > 0) {
    throw new Error(
      errors.map((diagnostic) => ts.flattenDiagnosticMessageText(diagnostic.messageText, '\n')).join('\n')
    )
  }
  return result.outputText
}

function loadModule(fileUrl, mocks = {}) {
  const module = { exports: {} }
  const sourcePath = fileURLToPath(fileUrl)
  const requireFn = (specifier) => {
    if (Object.prototype.hasOwnProperty.call(mocks, specifier)) return mocks[specifier]
    if (specifier.startsWith('.')) {
      const targetUrl = new URL(specifier.endsWith('.ts') ? specifier : `${specifier}.ts`, fileUrl)
      return loadModule(targetUrl, mocks)
    }
    return nodeRequire(specifier)
  }
  const wrapped = `(function (exports, module, require) {\n${compileModule(fileUrl)}\n})`
  const run = vm.runInThisContext(wrapped, { filename: sourcePath })
  run(module.exports, module, requireFn)
  return module.exports
}

const pluginWebviewUrl = new URL('../src/main/plugin-webview.ts', import.meta.url)
const pluginHostUrl = new URL('../src/preload/plugin-host.ts', import.meta.url)
const pluginPageViewSource = readFileSync(
  new URL('../src/renderer/src/components/plugins/PluginPageView.tsx', import.meta.url),
  'utf8'
)

const {
  PLUGIN_HOST_INVOKE_CHANNEL,
  PLUGIN_HOST_RESPONSE_CHANNEL,
  PLUGIN_HOST_EVENT_CHANNEL,
  PLUGIN_WEBVIEW_PREFERENCES,
  isAllowedPluginBridgeMethod,
  bindPluginBridgeParams,
  isAllowedPluginNavigationUrl,
  isPluginWebviewPartition,
  parsePluginHostInvokeArgs,
  dispatchPluginHostInvoke,
  bindPluginWebviewEmbedder,
  lockPluginWebviewPreferences,
  attachPluginWebviewContents,
  installPluginWebviewGuards
} = loadModule(pluginWebviewUrl)

function createWebview(initialUrl = '') {
  const listeners = new Map()
  const sent = []
  const webview = {
    src: initialUrl,
    stopped: 0,
    loaded: [],
    send(...args) {
      sent.push(args)
    },
    stop() {
      webview.stopped += 1
    },
    getURL() {
      return webview.src
    },
    loadURL(url) {
      webview.src = url
      webview.loaded.push(url)
    },
    setAttribute(name, value) {
      if (name === 'src') webview.src = value
    },
    addEventListener(type, listener) {
      listeners.set(type, listener)
    },
    removeEventListener(type, listener) {
      if (listeners.get(type) === listener) listeners.delete(type)
    },
    emit(type, event) {
      const listener = listeners.get(type)
      if (listener != null) listener(event)
    }
  }
  return { webview, sent, listeners }
}

function loadPluginHost() {
  const ipcListeners = new Map()
  const sendToHostCalls = []
  let exposed = null
  loadModule(pluginHostUrl, {
    electron: {
      contextBridge: {
        exposeInMainWorld(name, api) {
          exposed = { name, api }
        }
      },
      ipcRenderer: {
        on(channel, listener) {
          ipcListeners.set(channel, listener)
        },
        sendToHost(...args) {
          sendToHostCalls.push(args)
        }
      }
    }
  })
  return { exposed, ipcListeners, sendToHostCalls }
}

test('PluginPageView binds embedder ipc-message and does not allow popups', () => {
  assert.match(pluginPageViewSource, /bindPluginWebviewEmbedder/)
  assert.match(pluginPageViewSource, /PLUGIN_WEBVIEW_PREFERENCES/)
  assert.match(pluginPageViewSource, /webpreferences/)
  assert.doesNotMatch(pluginPageViewSource, /allowpopups/)
  assert.match(pluginPageViewSource, /ipc-message/)
})

test('official plugin methods are allowed and privileged RPC is denied', () => {
  assert.equal(isAllowedPluginBridgeMethod('custom-mouse', 'plugin.customMouse.getState'), true)
  assert.equal(isAllowedPluginBridgeMethod('shell-integration', 'plugin.shell.getStatus'), true)
  assert.equal(isAllowedPluginBridgeMethod('vive-tool', 'plugin.vive.download'), true)
  assert.equal(isAllowedPluginBridgeMethod('custom-mouse', 'dialog:open-file'), true)
  assert.equal(isAllowedPluginBridgeMethod('custom-mouse', 'plugins.getConfig'), true)
  assert.equal(isAllowedPluginBridgeMethod('custom-mouse', 'plugin.vive.download'), false)
  assert.equal(isAllowedPluginBridgeMethod('custom-mouse', 'power.shutdown'), false)
  assert.equal(isAllowedPluginBridgeMethod('custom-mouse', 'plugins.install'), false)
  assert.equal(isAllowedPluginBridgeMethod('custom-mouse', 'dialog:open-url'), false)
  assert.equal(isAllowedPluginBridgeMethod('custom-mouse', 'bridge:invoke'), false)
  assert.equal(isAllowedPluginBridgeMethod('', 'plugin.customMouse.getState'), false)
})

test('third-party plugin ids bind to a camelCase plugin.* prefix', () => {
  assert.equal(isAllowedPluginBridgeMethod('color-picker', 'plugin.colorPicker.getState'), true)
  assert.equal(isAllowedPluginBridgeMethod('color-picker', 'plugin.customMouse.getState'), false)
})

function sameJson(actual, expected) {
  assert.equal(JSON.stringify(actual), JSON.stringify(expected))
}

test('config writes always bind the hosting plugin id', () => {
  sameJson(
    bindPluginBridgeParams('custom-mouse', 'plugins.getConfig', { pluginId: 'other', key: 'theme' }),
    { pluginId: 'custom-mouse', key: 'theme' }
  )
  sameJson(
    bindPluginBridgeParams('custom-mouse', 'plugins.setConfig', { pluginId: 'other', key: 'theme', value: 1 }),
    { pluginId: 'custom-mouse', key: 'theme', value: 1 }
  )
  sameJson(
    bindPluginBridgeParams('custom-mouse', 'plugin.customMouse.getState', { pluginId: 'other' }),
    { pluginId: 'other' }
  )
})

test('navigation stays on the local plugin file entry', () => {
  const entry = 'file:///C:/plugins/custom-mouse/web/index.html'
  const directory = 'file:///C:/plugins/custom-mouse/'
  assert.equal(isAllowedPluginNavigationUrl(entry, entry, directory), true)
  assert.equal(
    isAllowedPluginNavigationUrl(`${entry}#section`, entry, directory),
    true
  )
  assert.equal(
    isAllowedPluginNavigationUrl('file:///C:/plugins/custom-mouse/web/help.html', entry, directory),
    true
  )
  assert.equal(
    isAllowedPluginNavigationUrl('file:///C:/plugins/custom-mouse-evil/web/index.html', entry, directory),
    false
  )
  assert.equal(isAllowedPluginNavigationUrl('https://evil.example/', entry, directory), false)
  assert.equal(isAllowedPluginNavigationUrl('file:///C:/Windows/system.ini', entry, directory), false)
  assert.equal(isPluginWebviewPartition('persist:plugin-custom-mouse'), true)
  assert.equal(isPluginWebviewPartition('persist:app'), false)
})

test('embedder ipc-message completes guest invoke and rejects privileged methods', async () => {
  const entry = 'file:///C:/plugins/custom-mouse/web/index.html'
  const { webview, sent, listeners } = createWebview(entry)
  const invoked = []
  const session = {
    pluginId: 'custom-mouse',
    entryUrl: entry,
    directoryUrl: 'file:///C:/plugins/custom-mouse/',
    async invoke(method, params) {
      invoked.push({ method, params })
      return { ok: true, method }
    }
  }
  const release = bindPluginWebviewEmbedder(webview, session)
  assert.equal(listeners.has('ipc-message'), true)

  webview.emit('ipc-message', {
    channel: PLUGIN_HOST_INVOKE_CHANNEL,
    args: [1, 'plugin.customMouse.getState', { x: 1 }]
  })
  await Promise.resolve()
  await Promise.resolve()
  sameJson(invoked, [{ method: 'plugin.customMouse.getState', params: { x: 1 } }])
  sameJson(sent, [[PLUGIN_HOST_RESPONSE_CHANNEL, 1, { ok: true, method: 'plugin.customMouse.getState' }, null]])

  webview.emit('ipc-message', {
    channel: PLUGIN_HOST_INVOKE_CHANNEL,
    args: [2, 'power.shutdown', {}]
  })
  await Promise.resolve()
  await Promise.resolve()
  assert.equal(invoked.length, 1)
  assert.equal(sent[1][0], PLUGIN_HOST_RESPONSE_CHANNEL)
  assert.equal(sent[1][1], 2)
  assert.equal(sent[1][2], null)
  assert.match(sent[1][3], /not available/)

  webview.emit('ipc-message', {
    channel: PLUGIN_HOST_INVOKE_CHANNEL,
    args: [3, 'plugins.setConfig', { pluginId: 'other', key: 'theme', value: 'dark' }]
  })
  await Promise.resolve()
  await Promise.resolve()
  sameJson(invoked[1], {
    method: 'plugins.setConfig',
    params: { pluginId: 'custom-mouse', key: 'theme', value: 'dark' }
  })

  release()
  assert.equal(listeners.size, 0)
})

test('embedder blocks remote navigation and popups then restores the local entry', () => {
  const entry = 'file:///C:/plugins/custom-mouse/web/index.html'
  const { webview } = createWebview(entry)
  bindPluginWebviewEmbedder(webview, {
    pluginId: 'custom-mouse',
    entryUrl: entry,
    directoryUrl: 'file:///C:/plugins/custom-mouse/',
    invoke: async () => ({})
  })

  const prevented = []
  webview.src = 'https://evil.example/'
  webview.emit('will-navigate', {
    url: 'https://evil.example/',
    preventDefault() {
      prevented.push('navigate')
    }
  })
  assert.deepEqual(prevented, ['navigate'])
  assert.equal(webview.stopped, 1)
  assert.equal(webview.src, entry)

  webview.emit('new-window', {
    url: 'https://popup.example/',
    preventDefault() {
      prevented.push('popup')
    }
  })
  assert.deepEqual(prevented, ['navigate', 'popup'])
})

test('parsePluginHostInvokeArgs rejects malformed guest payloads', () => {
  assert.equal(parsePluginHostInvokeArgs([]), null)
  assert.equal(parsePluginHostInvokeArgs(['1', 'plugin.customMouse.getState']), null)
  assert.equal(parsePluginHostInvokeArgs([1, '']), null)
  sameJson(parsePluginHostInvokeArgs([7, 'plugin.customMouse.getState', { a: 1 }]), {
    id: 7,
    method: 'plugin.customMouse.getState',
    params: { a: 1 }
  })
})

test('dispatchPluginHostInvoke does not forward denied methods to the privileged bridge', async () => {
  const sent = []
  const invoked = []
  await dispatchPluginHostInvoke(
    'custom-mouse',
    9,
    'plugins.install',
    { pluginId: 'custom-mouse' },
    async (method, params) => {
      invoked.push({ method, params })
      return { ok: true }
    },
    (channel, ...args) => {
      sent.push([channel, ...args])
    }
  )
  assert.deepEqual(invoked, [])
  assert.equal(sent[0][0], PLUGIN_HOST_RESPONSE_CHANNEL)
  assert.match(sent[0][3], /not available/)
})

test('main will-attach-webview locks preload and denies non-local guests', () => {
  const listeners = new Map()
  let windowOpenHandler = null
  const contents = {
    kind: 'window',
    getType() {
      return contents.kind
    },
    getURL() {
      return ''
    },
    on(event, listener) {
      listeners.set(event, listener)
    },
    setWindowOpenHandler(handler) {
      windowOpenHandler = handler
    }
  }

  attachPluginWebviewContents(contents, 'C:\\UDT\\out\\preload\\plugin-host.js')
  assert.equal(windowOpenHandler, null)

  let prevented = false
  const prefs = { nodeIntegration: true, preloadURL: 'file:///evil.js' }
  listeners.get('will-attach-webview')(
    {
      preventDefault() {
        prevented = true
      }
    },
    prefs,
    { src: 'https://evil.example/', partition: 'persist:plugin-x' }
  )
  assert.equal(prevented, true)
  assert.equal(prefs.nodeIntegration, true)

  prevented = false
  listeners.get('will-attach-webview')(
    {
      preventDefault() {
        prevented = true
      }
    },
    prefs,
    {
      src: 'file:///C:/plugins/custom-mouse/web/index.html',
      partition: 'persist:plugin-custom-mouse'
    }
  )
  assert.equal(prevented, false)
  assert.equal(prefs.nodeIntegration, false)
  assert.equal(prefs.contextIsolation, true)
  assert.equal(prefs.sandbox, true)
  assert.equal(prefs.webSecurity, true)
  assert.equal(prefs.preload, 'C:\\UDT\\out\\preload\\plugin-host.js')
  assert.equal(prefs.preloadURL, undefined)

  contents.kind = 'webview'
  attachPluginWebviewContents(contents, 'C:\\UDT\\out\\preload\\plugin-host.js')
  sameJson(windowOpenHandler(), { action: 'deny' })

  prevented = false
  listeners.get('will-navigate')(
    {
      preventDefault() {
        prevented = true
      }
    },
    'https://evil.example/'
  )
  assert.equal(prevented, true)
})

test('installPluginWebviewGuards subscribes to web-contents-created', () => {
  const subscribed = []
  installPluginWebviewGuards('C:\\preload\\plugin-host.js', {
    on(event, listener) {
      subscribed.push({ event, listener })
    }
  })
  assert.equal(subscribed.length, 1)
  assert.equal(subscribed[0].event, 'web-contents-created')
  assert.equal(typeof subscribed[0].listener, 'function')
})

test('guest preload sendToHost round-trips through embedder ipc-message', async () => {
  const host = loadPluginHost()
  assert.equal(host.exposed.name, 'pluginHost')

  const pending = host.exposed.api.invoke('plugin.customMouse.getState', { n: 4 })
  sameJson(host.sendToHostCalls, [
    [PLUGIN_HOST_INVOKE_CHANNEL, 1, 'plugin.customMouse.getState', { n: 4 }]
  ])

  const { webview, sent } = createWebview('file:///C:/plugins/custom-mouse/web/index.html')
  bindPluginWebviewEmbedder(webview, {
    pluginId: 'custom-mouse',
    entryUrl: 'file:///C:/plugins/custom-mouse/web/index.html',
    directoryUrl: 'file:///C:/plugins/custom-mouse/',
    async invoke(method, params) {
      return { method, params }
    }
  })
  webview.emit('ipc-message', {
    channel: PLUGIN_HOST_INVOKE_CHANNEL,
    args: [host.sendToHostCalls[0][1], host.sendToHostCalls[0][2], host.sendToHostCalls[0][3]]
  })
  await Promise.resolve()
  await Promise.resolve()
  sameJson(sent[0], [
    PLUGIN_HOST_RESPONSE_CHANNEL,
    1,
    { method: 'plugin.customMouse.getState', params: { n: 4 } },
    null
  ])

  host.ipcListeners.get(PLUGIN_HOST_RESPONSE_CHANNEL)(null, sent[0][1], sent[0][2], sent[0][3])
  sameJson(await pending, { method: 'plugin.customMouse.getState', params: { n: 4 } })
})

test('guest preload ignores non-plugin events and empty methods', async () => {
  const host = loadPluginHost()
  const received = []
  const unsubscribe = host.exposed.api.on('sensors.updated', (data) => {
    received.push(data)
  })
  const pluginReceived = []
  host.exposed.api.on('plugin.vive.downloadProgress', (data) => {
    pluginReceived.push(data)
  })

  host.ipcListeners.get(PLUGIN_HOST_EVENT_CHANNEL)(null, 'sensors.updated', { cpu: 90 })
  host.ipcListeners.get(PLUGIN_HOST_EVENT_CHANNEL)(null, 'plugin.vive.downloadProgress', { percent: 40 })
  sameJson(received, [])
  sameJson(pluginReceived, [{ percent: 40 }])
  unsubscribe()

  await assert.rejects(host.exposed.api.invoke(''), /method name is required/)
})

test('locked webview preferences keep node integration off', () => {
  const prefs = {
    nodeIntegration: true,
    nodeIntegrationInSubFrames: true,
    contextIsolation: false,
    sandbox: false,
    webSecurity: false,
    allowRunningInsecureContent: true,
    nativeWindowOpen: true,
    preloadURL: 'file:///tmp/evil.js'
  }
  lockPluginWebviewPreferences(prefs, 'C:\\preload\\plugin-host.js')
  assert.equal(prefs.nodeIntegration, false)
  assert.equal(prefs.nodeIntegrationInSubFrames, false)
  assert.equal(prefs.contextIsolation, true)
  assert.equal(prefs.sandbox, true)
  assert.equal(prefs.webSecurity, true)
  assert.equal(prefs.allowRunningInsecureContent, false)
  assert.equal(prefs.nativeWindowOpen, false)
  assert.equal(prefs.preload, 'C:\\preload\\plugin-host.js')
  assert.equal('preloadURL' in prefs, false)
  assert.match(PLUGIN_WEBVIEW_PREFERENCES, /nodeIntegration=no/)
  assert.match(PLUGIN_WEBVIEW_PREFERENCES, /contextIsolation=yes/)
})
