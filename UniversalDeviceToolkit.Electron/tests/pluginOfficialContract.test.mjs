import assert from 'node:assert/strict'
import { readFileSync } from 'node:fs'
import test from 'node:test'

const contract = JSON.parse(
  readFileSync(
    new URL('../../Plugins/Official/plugin-rpc-contract.json', import.meta.url),
    'utf8'
  )
)
const handler = readFileSync(
  new URL('../../UniversalDeviceToolkit.Host/Rpc/Handlers/PluginOfficialHandlers.cs', import.meta.url),
  'utf8'
)
const listHandler = readFileSync(
  new URL('../../UniversalDeviceToolkit.Host/Rpc/Handlers/PluginHandlers.cs', import.meta.url),
  'utf8'
)
const main = readFileSync(new URL('../src/main/index.ts', import.meta.url), 'utf8')
const dialogs = readFileSync(new URL('../src/main/dialogs.ts', import.meta.url), 'utf8')
const customMouseWeb = readFileSync(
  new URL('../../Plugins/Official/CustomMouse/web/index.html', import.meta.url),
  'utf8'
)
const shellWeb = readFileSync(
  new URL('../../Plugins/Official/ShellIntegration/web/index.html', import.meta.url),
  'utf8'
)
const viveWeb = readFileSync(
  new URL('../../Plugins/Official/ViveTool/web/index.html', import.meta.url),
  'utf8'
)

const customMouseMethods = contract.methods.customMouse
const shellMethods = contract.methods.shell
const viveMethods = contract.methods.vive
const events = new Set(contract.events)

function quotedPluginMethods(html, prefix) {
  const names = new Set()
  const pattern = new RegExp(`['"](${prefix.replaceAll('.', '\\.')}\\.[A-Za-z0-9]+)['"]`, 'g')
  for (const match of html.matchAll(pattern)) {
    names.add(match[1])
  }
  return [...names]
}

test('official plugin RPC names stay registered on the Host', () => {
  for (const method of customMouseMethods) {
    assert.match(handler, new RegExp(method.replaceAll('.', '\\.')))
  }
  for (const method of shellMethods) {
    assert.match(handler, new RegExp(method.replaceAll('.', '\\.')))
  }
  for (const method of viveMethods) {
    assert.match(handler, new RegExp(method.replaceAll('.', '\\.')))
  }
})

test('plugin web pages only invoke registered plugin.* methods', () => {
  const registered = new Set([...customMouseMethods, ...shellMethods, ...viveMethods])

  for (const name of quotedPluginMethods(customMouseWeb, 'plugin.customMouse')) {
    assert.ok(registered.has(name), `unregistered CustomMouse invoke: ${name}`)
  }
  for (const name of quotedPluginMethods(shellWeb, 'plugin.shell')) {
    assert.ok(registered.has(name), `unregistered Shell invoke: ${name}`)
  }
  for (const name of quotedPluginMethods(viveWeb, 'plugin.vive')) {
    if (events.has(name)) continue
    assert.ok(registered.has(name), `unregistered Vive invoke: ${name}`)
  }
  assert.match(viveWeb, /plugin\.vive\.downloadProgress/)
  assert.match(handler, /plugin\.vive\.downloadProgress/)
})

test('plugins.list projections include directory and webPage', () => {
  assert.match(listHandler, /directory = ResolvePluginDirectory\(metadata\)/)
  assert.match(listHandler, /webPage = webPage is \{ Entry\.Length: > 0 \}/)
  assert.match(listHandler, /private static object ProjectInstalledOnlyView/)
})

test('plugin webviews can open and save files through Electron dialogs', () => {
  assert.match(dialogs, /method === 'dialog:open-file'/)
  assert.match(dialogs, /method === 'dialog:save-file'/)
  assert.match(main, /isDialogBridgeMethod\(method\)/)
  assert.match(main, /invokeDialogBridgeMethod\(method, params,/)
  assert.match(main, /invokeBridgeMethod/)
  assert.match(shellWeb, /dialog:save-file/)
  assert.match(shellWeb, /dialog:open-file/)
  assert.match(viveWeb, /dialog:open-file/)
  assert.match(viveWeb, /dialog:save-file/)
})
