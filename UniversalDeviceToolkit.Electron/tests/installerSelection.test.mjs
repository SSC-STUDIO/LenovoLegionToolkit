import assert from 'node:assert/strict'
import { readFileSync } from 'node:fs'
import { URL, fileURLToPath } from 'node:url'
import { createRequire } from 'node:module'
import test from 'node:test'
import ts from 'typescript'
import vm from 'node:vm'

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
    throw new Error(errors.map((diagnostic) => ts.flattenDiagnosticMessageText(diagnostic.messageText, '\n')).join('\n'))
  }
  return result.outputText
}

function loadModule(fileUrl, mocks = {}) {
  const module = { exports: {} }
  const sourcePath = fileURLToPath(fileUrl)
  vm.runInNewContext(compileModule(fileUrl), {
    exports: module.exports,
    module,
    require(specifier) {
      if (Object.prototype.hasOwnProperty.call(mocks, specifier)) return mocks[specifier]
      return nodeRequire(specifier)
    },
    console
  }, { filename: sourcePath })
  return module.exports
}

const sharedUrl = new URL('../src/shared/installer-selection.ts', import.meta.url)
const mainUrl = new URL('../src/main/installer-selection.ts', import.meta.url)
const shared = loadModule(sharedUrl)
const main = loadModule(mainUrl, { '../shared/installer-selection': shared })

test('installer selection arguments accept only supported language and device values', () => {
  assert.equal(JSON.stringify(
    shared.parseInstallerSelectionArguments([
      '--udt-installer-language=ja',
      '--udt-installer-device-mode=basic'
    ])),
    JSON.stringify({ language: 'ja', deviceMode: 'basic' })
  )
  assert.equal(
    shared.parseInstallerSelectionArguments([
      '--udt-installer-language=../../evil',
      '--udt-installer-device-mode=basic'
    ]),
    null
  )
})

test('installer INI parser ignores unrelated sections and rejects incomplete input', () => {
  assert.equal(JSON.stringify(
    main.parseInstallerSelectionIni([
      '[other]',
      'language=ru',
      '[installation]',
      'language=ru',
      'deviceMode=auto'
    ].join('\n'))),
    JSON.stringify({ language: 'ru', deviceMode: 'auto' })
  )
  assert.equal(main.parseInstallerSelectionIni('[installation]\nlanguage=en'), null)
})
