import assert from 'node:assert/strict'
import { readFileSync } from 'node:fs'
import { fileURLToPath, URL } from 'node:url'
import test from 'node:test'

const installerSource = readFileSync(
  fileURLToPath(new URL('../buildResources/installer.nsh', import.meta.url)),
  'utf8'
)
const mainSource = readFileSync(
  fileURLToPath(new URL('../installer/main.mjs', import.meta.url)),
  'utf8'
)
const rendererSource = readFileSync(
  fileURLToPath(new URL('../installer/renderer.mjs', import.meta.url)),
  'utf8'
)
const styleSource = readFileSync(
  fileURLToPath(new URL('../installer/styles.css', import.meta.url)),
  'utf8'
)

test('installer uses a real Windows four-pane icon for the platform badge', () => {
  assert.match(rendererSource, /class="platform-icon"/)
  assert.match(rendererSource, /viewBox="0 0 24 24"/)
  assert.doesNotMatch(rendererSource, /▦/)
  assert.match(styleSource, /\.platform-icon\s*\{[^}]*width:\s*17px/)
  assert.match(rendererSource, /brand-logo-frame/)
  assert.match(rendererSource, /logoData/)
})

test('installer keeps the reference visual language and setup choices', () => {
  assert.match(rendererSource, /准备安装/)
  assert.match(rendererSource, /语言选择/)
  assert.match(rendererSource, /设备选择/)
  assert.match(rendererSource, /需要管理员权限/)
  assert.doesNotMatch(rendererSource, /不会修改设备设置，安装后由你控制/)
  assert.doesNotMatch(installerSource, /不会修改设备设置，安装后由你控制/)
  assert.match(rendererSource, /onThemeChanged/)
  assert.match(rendererSource, /dataset\.theme/)
  assert.match(mainSource, /systemPreferences\.getAccentColor/)
  assert.match(mainSource, /nativeTheme\.themeSource = 'system'/)
  assert.match(mainSource, /systemPreferences\.on\('color-changed'/)
  assert.match(styleSource, /--accent:\s*#ff2a38/)
  assert.match(styleSource, /data-theme="dark"/)
  assert.match(styleSource, /\.brand-panel\s*\{/)
})
