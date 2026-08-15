import assert from 'node:assert/strict'
import { readFileSync } from 'node:fs'
import { URL, fileURLToPath } from 'node:url'
import { setImmediate } from 'node:timers'
import test from 'node:test'
import ts from 'typescript'
import vm from 'node:vm'

const gateUrl = new URL('../src/renderer/src/components/utils/UnsupportedDeviceGate.tsx', import.meta.url)
const source = readFileSync(gateUrl, 'utf8')

function compileModule(fileUrl) {
  const fileName = fileURLToPath(fileUrl)
  const result = ts.transpileModule(readFileSync(fileUrl, 'utf8'), {
    fileName,
    reportDiagnostics: true,
    compilerOptions: {
      jsx: ts.JsxEmit.ReactJSX,
      module: ts.ModuleKind.CommonJS,
      target: ts.ScriptTarget.ES2022
    }
  })
  const errors = (result.diagnostics ?? []).filter(
    (diagnostic) => diagnostic.category === ts.DiagnosticCategory.Error
  )
  if (errors.length > 0) {
    throw new Error(errors.map((diagnostic) => ts.flattenDiagnosticMessageText(diagnostic.messageText, '\n')).join('\n'))
  }
  return result.outputText
}

function createHarness(info, continueOnWarning = true) {
  const effects = []
  const opened = []
  let quitCount = 0
  const module = { exports: {} }
  vm.runInNewContext(compileModule(gateUrl), {
    exports: module.exports,
    module,
    require(specifier) {
      if (specifier === 'react') return { useEffect: (effect) => effects.push(effect) }
      if (specifier === 'react/jsx-runtime') {
        return { Fragment: Symbol('Fragment'), jsx: () => ({}), jsxs: () => ({}) }
      }
      if (specifier === '../../api/bridge') return { waitForHostReady: async () => undefined }
      if (specifier === '../../api/system') return { systemApi: { info: async () => info } }
      if (specifier === './UnsupportedDeviceModal') {
        return {
          openUnsupportedDevice: async (options) => {
            opened.push(options)
            return continueOnWarning
          }
        }
      }
      throw new Error(`Unexpected import "${specifier}"`)
    },
    console,
    Promise,
    setTimeout,
    window: { setTimeout, bridge: { quitApp: () => { quitCount += 1 } } }
  }, { filename: fileURLToPath(gateUrl) })

  return {
    opened,
    get quitCount() { return quitCount },
    run: () => {
      module.exports.default()
      return effects[0]()
    }
  }
}

test('the main program no longer contains first-run language or device setup gates', () => {
  assert.doesNotMatch(source, /LanguageSelector|DeviceSetup|localStorage|udt\.lang|udt\.deviceSetup/)
  assert.match(source, /openUnsupportedDevice/)
})

test('unsupported devices are checked after Host readiness and can exit the app', async () => {
  const harness = createHarness({ vendor: 'Acme', model: 'Test', machineType: 'ABC', isCompatible: false }, false)
  const cleanup = harness.run()
  await new Promise((resolve) => setImmediate(resolve))
  assert.equal(harness.opened.length, 1)
  assert.equal(harness.opened[0].machineType, 'ABC')
  assert.equal(harness.quitCount, 1)
  cleanup()
})

test('compatible devices do not open the warning', async () => {
  const harness = createHarness({ vendor: 'Acme', model: 'Test', machineType: 'ABC', isCompatible: true })
  const cleanup = harness.run()
  await new Promise((resolve) => setImmediate(resolve))
  assert.equal(harness.opened.length, 0)
  assert.equal(harness.quitCount, 0)
  cleanup()
})
