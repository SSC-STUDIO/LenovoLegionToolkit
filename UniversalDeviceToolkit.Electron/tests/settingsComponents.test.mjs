import assert from 'node:assert/strict'
import { readFileSync } from 'node:fs'
import { setImmediate } from 'node:timers'
import { URL, fileURLToPath } from 'node:url'
import vm from 'node:vm'
import test from 'node:test'
import ts from 'typescript'

const appearanceSectionUrl = new URL(
  '../src/renderer/src/components/settings/AppearanceSection.tsx',
  import.meta.url
)
const settingsCardUrl = new URL(
  '../src/renderer/src/components/settings/SettingsCard.tsx',
  import.meta.url
)
const settingsStoreUrl = new URL(
  '../src/renderer/src/stores/settingsStore.ts',
  import.meta.url
)
const themeStoreUrl = new URL(
  '../src/renderer/src/stores/themeStore.ts',
  import.meta.url
)
const uiScaleUrl = new URL('../src/renderer/src/theme/uiScale.ts', import.meta.url)

const Fragment = Symbol('Fragment')

function createElement(type, props, key) {
  return {
    type,
    key: key ?? null,
    props: props ?? {}
  }
}

const jsxRuntime = {
  Fragment,
  jsx: createElement,
  jsxs: createElement
}

function compileModule(fileUrl) {
  const fileName = fileURLToPath(fileUrl)
  const result = ts.transpileModule(readFileSync(fileUrl, 'utf8'), {
    fileName,
    reportDiagnostics: true,
    compilerOptions: {
      esModuleInterop: true,
      jsx: ts.JsxEmit.ReactJSX,
      module: ts.ModuleKind.CommonJS,
      target: ts.ScriptTarget.ES2022
    }
  })
  const errors = (result.diagnostics ?? []).filter(
    (diagnostic) => diagnostic.category === ts.DiagnosticCategory.Error
  )
  if (errors.length > 0) {
    throw new Error(
      errors
        .map((diagnostic) => ts.flattenDiagnosticMessageText(diagnostic.messageText, '\n'))
        .join('\n')
    )
  }
  return result.outputText
}

function loadModule(fileUrl, mocks, globals = {}) {
  const fileName = fileURLToPath(fileUrl)
  const module = { exports: {} }
  const context = {
    ...globals,
    exports: module.exports,
    module,
    require(specifier) {
      if (Object.prototype.hasOwnProperty.call(mocks, specifier)) {
        return mocks[specifier]
      }
      throw new Error(`Unexpected import "${specifier}" from ${fileName}`)
    }
  }

  new vm.Script(compileModule(fileUrl), { filename: fileName }).runInNewContext(context)
  return module.exports
}

function createZustandMock() {
  function buildStore(initializer) {
    let state
    const listeners = new Set()
    const getState = () => state
    const setState = (update, replace = false) => {
      const next = typeof update === 'function' ? update(state) : update
      const previous = state
      state = replace ? next : { ...state, ...next }
      for (const listener of listeners) {
        listener(state, previous)
      }
    }
    const subscribe = (listener) => {
      listeners.add(listener)
      return () => listeners.delete(listener)
    }
    const api = { getState, setState, subscribe }
    state = initializer(setState, getState, api)

    const useStore = (selector = (current) => current) => selector(state)
    useStore.getState = getState
    useStore.setState = setState
    useStore.subscribe = subscribe
    return useStore
  }

  return {
    create: (initializer) => (initializer == null ? buildStore : buildStore(initializer))
  }
}

function createMemoryStorage(initial = {}) {
  const values = new Map(Object.entries(initial).map(([key, value]) => [key, String(value)]))
  return {
    clear: () => values.clear(),
    getItem: (key) => values.get(key) ?? null,
    key: (index) => [...values.keys()][index] ?? null,
    get length() {
      return values.size
    },
    removeItem: (key) => values.delete(key),
    setItem: (key, value) => values.set(key, String(value))
  }
}

function cloneJson(value) {
  return JSON.parse(JSON.stringify(value))
}

function collectElements(node, result = [], seen = new Set()) {
  if (node == null || typeof node !== 'object' || seen.has(node)) {
    return result
  }
  seen.add(node)
  if (Array.isArray(node)) {
    for (const child of node) {
      collectElements(child, result, seen)
    }
    return result
  }
  if ('type' in node && 'props' in node) {
    result.push(node)
    for (const value of Object.values(node.props)) {
      collectElements(value, result, seen)
    }
  }
  return result
}

function findSingleElement(root, predicate, description) {
  const matches = collectElements(root).filter(predicate)
  assert.equal(matches.length, 1, `Expected one ${description}, found ${matches.length}`)
  return matches[0]
}

async function settleAsyncWork() {
  await Promise.resolve()
  await new Promise((resolve) => setImmediate(resolve))
}

function createAppearanceFixture({
  application = { UnrelatedSetting: 'preserved' },
  storage = {},
  setError,
  saveError
} = {}) {
  const calls = {
    errors: [],
    languageChanges: [],
    loads: [],
    saves: [],
    sets: []
  }
  const localStorage = createMemoryStorage(storage)
  const style = {
    zoom: '',
    removeProperty(property) {
      if (property !== 'zoom') return ''
      const previous = this.zoom
      this.zoom = ''
      return previous
    }
  }
  const globals = {
    document: { documentElement: { style } },
    localStorage,
    window: { bridge: { platform: 'web' } }
  }
  const initialApplication = cloneJson(application)
  const settingsApi = {
    getAll: async (scopes) => {
      calls.loads.push(scopes == null ? undefined : Array.from(scopes))
      return { scopes: { application: cloneJson(initialApplication) } }
    },
    onChanged: () => () => undefined,
    save: async (scopes) => {
      const savedScopes = scopes == null ? undefined : Array.from(scopes)
      calls.saves.push(savedScopes)
      if (saveError != null) throw saveError
      return { saved: savedScopes ?? [] }
    },
    set: async (scope, value) => {
      calls.sets.push({ scope, value: cloneJson(value) })
      if (setError != null) throw setError
      return undefined
    }
  }
  const zustand = createZustandMock()
  const uiScaleModule = loadModule(uiScaleUrl, {}, globals)
  const settingsStoreModule = loadModule(
    settingsStoreUrl,
    {
      '../api/settings': { settingsApi },
      zustand
    },
    globals
  )
  settingsStoreModule.useSettingsStore.setState({
    loading: false,
    scopes: { application: cloneJson(initialApplication) }
  })
  const themeStoreModule = loadModule(
    themeStoreUrl,
    { zustand, '../theme/uiScale': uiScaleModule },
    globals
  )

  const effectCleanups = []
  const Select = function Select() {}
  const Checkbox = function Checkbox() {}
  const ColorPicker = function ColorPicker() {}
  const SettingsCard = function SettingsCard() {}
  const react = {
    useEffect(effect) {
      const cleanup = effect()
      if (typeof cleanup === 'function') effectCleanups.push(cleanup)
    },
    useState(initial) {
      return [typeof initial === 'function' ? initial() : initial, () => undefined]
    }
  }
  const systemApi = {
    getAccentColor: async () => ({ r: 0, g: 120, b: 212 }),
    setAccentColor: async () => undefined
  }
  const appearanceModule = loadModule(
    appearanceSectionUrl,
    {
      '../../api/settings': { settingsApi },
      '../../api/system': { systemApi },
      '../../i18n': {
        LANGUAGES: [
          { code: 'en', name: 'English' },
          { code: 'zh-Hans', name: 'Chinese' }
        ],
        changeLanguage: async (language) => {
          calls.languageChanges.push(language)
        }
      },
      '../../stores/settingsStore': settingsStoreModule,
      '../../stores/themeStore': themeStoreModule,
      '../../theme/useTheme': { storeAccentPreference: () => undefined },
      '../ColorPicker': { __esModule: true, default: ColorPicker },
      './SettingsCard': { SettingsCard },
      '@fluentui/react-icons': {},
      antd: {
        Checkbox,
        Select,
        message: {
          error: (message) => calls.errors.push(message)
        }
      },
      react,
      'react-i18next': {
        useTranslation: () => ({
          i18n: { language: 'en' },
          t: (key) => key
        })
      },
      'react/jsx-runtime': jsxRuntime
    },
    globals
  )
  const root = appearanceModule.default()

  return {
    calls,
    cleanup() {
      for (const cleanup of effectCleanups.reverse()) cleanup()
    },
    localStorage,
    root,
    settingsStore: settingsStoreModule.useSettingsStore,
    style,
    themeStore: themeStoreModule.useThemeStore,
    types: { Checkbox, Select, SettingsCard }
  }
}

test('language selection invokes the language change path', async (t) => {
  const fixture = createAppearanceFixture()
  t.after(fixture.cleanup)
  await settleAsyncWork()

  const languageSelect = findSingleElement(
    fixture.root,
    (element) =>
      element.type === fixture.types.Select &&
      element.props.className.includes('--language'),
    'language select'
  )
  languageSelect.props.onChange('zh-Hans')

  assert.deepEqual(fixture.calls.languageChanges, ['zh-Hans'])
})

test('temperature selections persist locally and to application settings', async (t) => {
  for (const unit of ['C', 'F']) {
    await t.test(unit, async (t) => {
      const fixture = createAppearanceFixture({
        storage: { 'udt-temperature-unit': unit === 'C' ? 'F' : 'C' }
      })
      t.after(fixture.cleanup)
      await settleAsyncWork()

      const temperatureSelect = findSingleElement(
        fixture.root,
        (element) =>
          element.type === fixture.types.Select &&
          element.props.className === 'udt-settings-select',
        'temperature select'
      )
      temperatureSelect.props.onChange(unit)
      await settleAsyncWork()

      assert.equal(fixture.localStorage.getItem('udt-temperature-unit'), unit)
      assert.equal(
        fixture.settingsStore.getState().scopes.application.TemperatureUnit,
        unit
      )
      assert.deepEqual(fixture.calls.sets, [
        {
          scope: 'application',
          value: { UnrelatedSetting: 'preserved', TemperatureUnit: unit }
        }
      ])
      assert.deepEqual(fixture.calls.saves, [['application']])
    })
  }
})

test('UI scale selection updates and persists the theme store', async (t) => {
  const fixture = createAppearanceFixture()
  t.after(fixture.cleanup)
  await settleAsyncWork()

  const scaleSelect = findSingleElement(
    fixture.root,
    (element) =>
      element.type === fixture.types.Select &&
      element.props.className.includes('--scale'),
    'UI scale select'
  )
  scaleSelect.props.onChange(1.25)

  assert.equal(fixture.themeStore.getState().uiScale, 1.25)
  assert.equal(fixture.themeStore.getState().uiScalePreference, 1.25)
  assert.equal(fixture.localStorage.getItem('udt-ui-scale'), '1.25')
  assert.equal(fixture.style.zoom, '1.25')
})

test('UI scale Auto preference persists and leaves the applied scale unlocked', async (t) => {
  const fixture = createAppearanceFixture({ storage: { 'udt-ui-scale': '1.25' } })
  t.after(fixture.cleanup)
  await settleAsyncWork()

  const scaleSelect = findSingleElement(
    fixture.root,
    (element) =>
      element.type === fixture.types.Select &&
      element.props.className.includes('--scale'),
    'UI scale select'
  )
  assert.equal(scaleSelect.props.value, 1.25)

  scaleSelect.props.onChange('auto')

  assert.equal(fixture.themeStore.getState().uiScalePreference, 'auto')
  assert.equal(fixture.localStorage.getItem('udt-ui-scale'), 'auto')
  assert.equal(scaleSelect.props.options[0].value, 'auto')
  assert.equal(scaleSelect.props.options.at(-1).value, 1.5)
})

test('UI scale manual selection reaches 150 percent', async (t) => {
  const fixture = createAppearanceFixture()
  t.after(fixture.cleanup)
  await settleAsyncWork()

  const scaleSelect = findSingleElement(
    fixture.root,
    (element) =>
      element.type === fixture.types.Select &&
      element.props.className.includes('--scale'),
    'UI scale select'
  )
  scaleSelect.props.onChange(1.5)

  assert.equal(fixture.themeStore.getState().uiScale, 1.5)
  assert.equal(fixture.themeStore.getState().uiScalePreference, 1.5)
  assert.equal(fixture.localStorage.getItem('udt-ui-scale'), '1.5')
  assert.equal(fixture.style.zoom, '1.5')
})

test('theme selections persist System, Light, and Dark representations', async (t) => {
  for (const [applicationTheme, storeTheme] of [
    ['System', 'system'],
    ['Light', 'light'],
    ['Dark', 'dark']
  ]) {
    await t.test(applicationTheme, async (t) => {
      const fixture = createAppearanceFixture()
      t.after(fixture.cleanup)
      await settleAsyncWork()

      const themeOption = findSingleElement(
        fixture.root,
        (element) => element.props.option?.value === applicationTheme,
        `${applicationTheme} theme option`
      )
      themeOption.props.onClick()
      await settleAsyncWork()

      assert.equal(fixture.themeStore.getState().themePreference, storeTheme)
      assert.equal(fixture.localStorage.getItem('udt.theme'), storeTheme)
      assert.equal(
        fixture.settingsStore.getState().scopes.application.Theme,
        applicationTheme
      )
      assert.deepEqual(fixture.calls.sets, [
        {
          scope: 'application',
          value: { UnrelatedSetting: 'preserved', Theme: applicationTheme }
        }
      ])
      assert.deepEqual(fixture.calls.saves, [['application']])
    })
  }
})

test('failed application persistence surfaces the existing save error path', async (t) => {
  const fixture = createAppearanceFixture({
    saveError: new Error('save failed')
  })
  t.after(fixture.cleanup)
  await settleAsyncWork()

  const temperatureSelect = findSingleElement(
    fixture.root,
    (element) =>
      element.type === fixture.types.Select &&
      element.props.className === 'udt-settings-select',
    'temperature select'
  )
  temperatureSelect.props.onChange('F')
  await settleAsyncWork()

  assert.equal(fixture.calls.sets.length, 1)
  assert.deepEqual(fixture.calls.saves, [['application']])
  assert.deepEqual(fixture.calls.errors, ['settings.saveFailed'])
})

function loadSettingsCard() {
  return loadModule(settingsCardUrl, {
    '@fluentui/react-icons': {
      ChevronRight16Regular: function ChevronRight16Regular() {}
    },
    'react/jsx-runtime': jsxRuntime
  }).SettingsCard
}

test('clickable SettingsCard activates with Enter and Space', () => {
  const SettingsCard = loadSettingsCard()
  let activations = 0
  let prevented = 0
  const card = SettingsCard({
    onClick: () => {
      activations += 1
    },
    title: 'Clickable setting'
  })
  const keyDown = (key) =>
    card.props.onKeyDown({
      key,
      preventDefault: () => {
        prevented += 1
      }
    })

  assert.equal(card.props.role, 'button')
  assert.equal(card.props.tabIndex, 0)

  keyDown('ArrowRight')
  assert.equal(activations, 0)
  assert.equal(prevented, 0)

  keyDown('Enter')
  keyDown(' ')
  assert.equal(activations, 2)
  assert.equal(prevented, 2)
})

test('non-clickable SettingsCard rows have no interactive role', () => {
  const SettingsCard = loadSettingsCard()
  let prevented = false
  const card = SettingsCard({
    action: jsxRuntime.jsx('span', { children: 'Current value' }),
    title: 'Read-only setting'
  })

  assert.equal(card.props.role, undefined)
  assert.equal(card.props.tabIndex, undefined)
  assert.equal(card.props.onClick, undefined)

  card.props.onKeyDown({
    key: 'Enter',
    preventDefault: () => {
      prevented = true
    }
  })
  assert.equal(prevented, false)
})
