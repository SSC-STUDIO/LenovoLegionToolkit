import assert from 'node:assert/strict'
import { readFileSync } from 'node:fs'
import test from 'node:test'

const api = readFileSync(new URL('../src/renderer/src/api/dashboardHardware.ts', import.meta.url), 'utf8')
const handler = readFileSync(new URL('../../UniversalDeviceToolkit.Host/Rpc/Handlers/DashboardHardwareHandlers.cs', import.meta.url), 'utf8')

test('dashboard hardware RPC operation names stay aligned', () => {
  for (const operation of [
    'getState',
    'killGpuProcesses',
    'restartGpu',
    'setOverclockEnabled',
    'setOverclock',
    'turnOffMonitors'
  ]) {
    assert.match(api, new RegExp(`dashboardHardware\\.${operation}`))
    assert.match(handler, new RegExp(`dashboardHardware\\.${operation}`))
  }
})

test('special dashboard items remain represented by dedicated cards', () => {
  const items = readFileSync(new URL('../src/renderer/src/components/dashboard-parity/dashboardItems.ts', import.meta.url), 'utf8')
  const cards = readFileSync(new URL('../src/renderer/src/components/dashboard-parity/DashboardSpecialCard.tsx', import.meta.url), 'utf8')

  for (const item of ['DiscreteGpu', 'OverclockDiscreteGpu', 'TurnOffMonitors']) {
    assert.match(items, new RegExp(item))
    assert.match(cards, new RegExp(item))
  }
})
