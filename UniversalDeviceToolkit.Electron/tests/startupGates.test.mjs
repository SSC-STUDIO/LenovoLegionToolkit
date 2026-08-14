import assert from 'node:assert/strict'
import { readFileSync } from 'node:fs'
import { URL, fileURLToPath } from 'node:url'
import { setImmediate } from 'node:timers'
import test from 'node:test'
import ts from 'typescript'
import vm from 'node:vm'

const startupGatesUrl = new URL(
  '../src/renderer/src/components/utils/StartupGates.tsx',
  import.meta.url
)
const sourcePath = fileURLToPath(startupGatesUrl)
const source = readFileSync(startupGatesUrl, 'utf8')

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

function loadStartupGates(mocks, globals) {
  const module = { exports: {} }
  const context = {
    ...globals,
    exports: module.exports,
    module,
    require(specifier) {
      if (Object.prototype.hasOwnProperty.call(mocks, specifier)) {
        return mocks[specifier]
      }
      throw new Error(`Unexpected import "${specifier}" from ${sourcePath}`)
    },
    console,
    setTimeout,
    Promise,
    JSON
  }
  vm.runInNewContext(compileModule(startupGatesUrl), context, { filename: sourcePath })
  return module.exports
}

function createHarness() {
  const storage = new Map()
  const languageSelectorCalls = []
  const deviceSetupCalls = []
  let hostReadyImpl = async () => {
    throw new Error('Host did not become ready in time')
  }
  const effects = []

  const react = {
    useEffect(effect) {
      effects.push(effect)
    }
  }

  const loaded = loadStartupGates(
    {
      react,
      'react/jsx-runtime': {
        Fragment: Symbol('Fragment'),
        jsx: (type, props) => ({ type, props }),
        jsxs: (type, props) => ({ type, props })
      },
      '../../api/bridge': {
        waitForHostReady: () => hostReadyImpl()
      },
      '../../api/system': {
        systemApi: {
          info: async () => ({
            vendor: 'Lenovo',
            model: 'Y9000P',
            machineType: 'IRX9',
            isCompatible: true
          })
        }
      },
      '../../i18n': {
        LANGUAGES: [
          { code: 'en', name: 'English' },
          { code: 'zh-CN', name: '简体中文' }
        ],
        changeLanguage: async () => undefined
      },
      './LanguageSelectorModal': {
        openLanguageSelector: async (options) => {
          languageSelectorCalls.push(options)
          return { outcome: 'Continue', culture: 'zh-CN' }
        }
      },
      './DeviceSetupModal': {
        openDeviceSetup: async (options) => {
          deviceSetupCalls.push(options)
          return { confirmed: true, devicePackId: 'generic-pc-basic', isBasicMode: true }
        }
      },
      './UnsupportedDeviceModal': {
        openUnsupportedDevice: async () => true
      }
    },
    {
      localStorage: {
        getItem(key) {
          return storage.get(key) ?? null
        },
        setItem(key, value) {
          storage.set(key, String(value))
        }
      },
      window: {
        setTimeout,
        bridge: { quitApp() {} }
      }
    }
  )

  return {
    storage,
    languageSelectorCalls,
    deviceSetupCalls,
    effects,
    setHostReady(impl) {
      hostReadyImpl = impl
    },
    render() {
      loaded.default()
    }
  }
}

test('startup gates do not treat i18n.language as first-run completion', () => {
  assert.match(source, /LANGUAGE_GATE_DONE_KEY = 'udt\.language-gate-completed'/)
  assert.doesNotMatch(source, /i18n\.on\('languageChanged'/)
  assert.doesNotMatch(source, /if \(i18n\.language\) markLanguage/)
})

test('language selector runs even when Host is down and udt.lang is already set', async () => {
  const harness = createHarness()
  harness.storage.set('udt.lang', 'zh-CN')
  harness.render()
  assert.equal(harness.effects.length, 1)
  const cleanup = harness.effects[0]()
  await new Promise((resolve) => setImmediate(resolve))
  await new Promise((resolve) => setImmediate(resolve))
  await new Promise((resolve) => setImmediate(resolve))
  assert.equal(harness.languageSelectorCalls.length, 1)
  assert.equal(harness.languageSelectorCalls[0].defaultLanguage, 'zh-CN')
  assert.equal(harness.storage.get('udt.language-gate-completed'), '1')
  assert.equal(harness.deviceSetupCalls.length, 1)
  cleanup()
})

test('StrictMode cleanup does not permanently skip the language gate', async () => {
  const harness = createHarness()
  harness.render()
  const cleanupFirst = harness.effects[0]()
  cleanupFirst()
  const cleanupSecond = harness.effects[0]()
  await new Promise((resolve) => setImmediate(resolve))
  await new Promise((resolve) => setImmediate(resolve))
  await new Promise((resolve) => setImmediate(resolve))
  assert.ok(harness.languageSelectorCalls.length >= 1)
  cleanupSecond()
})
