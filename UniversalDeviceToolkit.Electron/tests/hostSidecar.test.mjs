import assert from 'node:assert/strict'
import test from 'node:test'
import {
  hostSidecarPath,
  isDevHostLayoutReady,
  isRunnableHost,
  networkProxyPathBesideHost
} from '../scripts/host-sidecar.mjs'

test('hostSidecarPath strips .exe on Windows', () => {
  const exe = 'C:\\out\\UniversalDeviceToolkit.Host.exe'
  assert.equal(hostSidecarPath(exe, 'runtimeconfig.json', 'win32'), 'C:\\out\\UniversalDeviceToolkit.Host.runtimeconfig.json')
  assert.equal(hostSidecarPath(exe, 'deps.json', 'win32'), 'C:\\out\\UniversalDeviceToolkit.Host.deps.json')
})

test('hostSidecarPath appends extension on Unix', () => {
  const exe = '/opt/udt/UniversalDeviceToolkit.Host'
  assert.equal(
    hostSidecarPath(exe, 'runtimeconfig.json', 'linux'),
    '/opt/udt/UniversalDeviceToolkit.Host.runtimeconfig.json'
  )
})

test('isRunnableHost requires exe plus runtimeconfig and deps', () => {
  const exe = 'C:\\out\\UniversalDeviceToolkit.Host.exe'
  const present = new Set([exe])
  const exists = (path) => present.has(path)

  assert.equal(isRunnableHost(exe, exists, 'win32'), false)

  present.add('C:\\out\\UniversalDeviceToolkit.Host.runtimeconfig.json')
  assert.equal(isRunnableHost(exe, exists, 'win32'), false)

  present.add('C:\\out\\UniversalDeviceToolkit.Host.deps.json')
  assert.equal(isRunnableHost(exe, exists, 'win32'), true)
})

test('networkProxyPathBesideHost keeps the Host directory', () => {
  assert.equal(
    networkProxyPathBesideHost('C:\\out\\UniversalDeviceToolkit.Host.exe', 'win32'),
    'C:\\out\\UniversalDeviceToolkit.NetworkProxy.exe'
  )
  assert.equal(
    networkProxyPathBesideHost('/opt/udt/UniversalDeviceToolkit.Host', 'linux'),
    '/opt/udt/UniversalDeviceToolkit.NetworkProxy'
  )
})

test('isDevHostLayoutReady requires Host and NetworkProxy sidecars', () => {
  const host = 'C:\\out\\UniversalDeviceToolkit.Host.exe'
  const worker = 'C:\\out\\UniversalDeviceToolkit.NetworkProxy.exe'
  const present = new Set([
    host,
    'C:\\out\\UniversalDeviceToolkit.Host.runtimeconfig.json',
    'C:\\out\\UniversalDeviceToolkit.Host.deps.json'
  ])
  const exists = (path) => present.has(path)

  assert.equal(isDevHostLayoutReady(host, exists, 'win32'), false)

  present.add(worker)
  present.add('C:\\out\\UniversalDeviceToolkit.NetworkProxy.runtimeconfig.json')
  present.add('C:\\out\\UniversalDeviceToolkit.NetworkProxy.deps.json')
  assert.equal(isDevHostLayoutReady(host, exists, 'win32'), true)
})
