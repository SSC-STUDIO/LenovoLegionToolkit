import { spawnSync } from 'node:child_process'
import { dirname, join } from 'node:path'
import { fileURLToPath } from 'node:url'
import { isDevHostLayoutReady, isRunnableHost, networkProxyPathBesideHost } from './host-sidecar.mjs'

const electronRoot = dirname(dirname(fileURLToPath(import.meta.url)))
const repoRoot = dirname(electronRoot)
const hostProject = join(repoRoot, 'UniversalDeviceToolkit.Host', 'UniversalDeviceToolkit.Host.csproj')
const tfm = 'net10.0-windows10.0.26100.0'
const hostExeName =
  process.platform === 'win32' ? 'UniversalDeviceToolkit.Host.exe' : 'UniversalDeviceToolkit.Host'
const debugHost = join(
  repoRoot,
  'UniversalDeviceToolkit.Host',
  'bin',
  'x64',
  'Debug',
  tfm,
  'win-x64',
  hostExeName
)

if (process.platform !== 'win32') {
  process.exit(0)
}

if (isDevHostLayoutReady(debugHost)) {
  process.exit(0)
}

console.log(
  '[ensure-host] Debug Host layout is incomplete (Host and/or NetworkProxy missing runtimeconfig.json/deps.json). Building UniversalDeviceToolkit.Host...'
)

const result = spawnSync('dotnet', ['build', hostProject, '-c', 'Debug', '--nologo'], {
  stdio: 'inherit',
  cwd: repoRoot
})

if ((result.status ?? 1) !== 0) {
  process.exit(result.status ?? 1)
}

if (!isRunnableHost(debugHost)) {
  console.error(
    `[ensure-host] Host build finished but ${debugHost} is still not a framework-dependent apphost (missing runtimeconfig.json or deps.json).`
  )
  process.exit(1)
}

const debugWorker = networkProxyPathBesideHost(debugHost)
if (!isRunnableHost(debugWorker)) {
  console.error(
    `[ensure-host] Host build finished but NetworkProxy worker is missing beside Host (need ${debugWorker} plus runtimeconfig.json/deps.json).`
  )
  process.exit(1)
}
