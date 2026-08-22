#!/usr/bin/env node
/**
 * Smoke the Linux Host stub over NDJSON JSON-RPC (no Electron).
 */
import { spawn } from 'node:child_process'
import { createInterface } from 'node:readline'
import { dirname, join } from 'node:path'
import { fileURLToPath } from 'node:url'

const root = dirname(fileURLToPath(import.meta.url))
const child = spawn(process.execPath, [join(root, 'host.mjs')], {
  stdio: ['pipe', 'pipe', 'inherit']
})

const pending = new Map()
let nextId = 1
let ready = false

function invoke(method, params = {}) {
  const id = nextId++
  return new Promise((resolve, reject) => {
    const timer = setTimeout(() => {
      pending.delete(id)
      reject(new Error(`timeout: ${method}`))
    }, 4000)
    pending.set(id, {
      resolve: (value) => {
        clearTimeout(timer)
        resolve(value)
      },
      reject: (error) => {
        clearTimeout(timer)
        reject(error)
      }
    })
    child.stdin.write(`${JSON.stringify({ id, method, params })}\n`)
  })
}

const rl = createInterface({ input: child.stdout })
rl.on('line', (line) => {
  let message
  try {
    message = JSON.parse(line)
  } catch {
    return
  }
  if (message.event === 'host.ready') {
    ready = true
    return
  }
  if (typeof message.id === 'number' && pending.has(message.id)) {
    const waiter = pending.get(message.id)
    pending.delete(message.id)
    if (message.error) waiter.reject(new Error(`${message.error.code}: ${message.error.message}`))
    else waiter.resolve(message.result)
  }
})

function assert(condition, message) {
  if (!condition) throw new Error(message)
}

try {
  await new Promise((resolve, reject) => {
    const timer = setTimeout(() => reject(new Error('host.ready timeout')), 4000)
    const check = setInterval(() => {
      if (ready) {
        clearInterval(check)
        clearTimeout(timer)
        resolve()
      }
    }, 20)
  })

  const ping = await invoke('ping')
  assert(ping?.pong === true, 'ping failed')

  const info = await invoke('system.info')
  assert(info?.model === 'Linux Desktop', `unexpected model: ${info?.model}`)
  assert(info?.model !== 'Legion Y9000P IRX9', 'must not fake Legion SKU')

  const status = await invoke('sensors.getStatus')
  assert(status?.initialized === true, 'sensors.getStatus.initialized')

  const snap = await invoke('sensors.getSnapshot')
  assert(snap?.initialized === true, 'snapshot.initialized')
  assert(typeof snap?.cpu?.usage === 'number', 'cpu.usage')
  assert(typeof snap?.cpu?.temperature === 'number', 'cpu.temperature')
  assert(typeof snap?.gpu?.usage === 'number', 'gpu.usage')
  assert(typeof snap?.battery?.chargeLevel === 'number', 'battery.chargeLevel')

  const listed = await invoke('feature.list')
  const features = listed?.features ?? []
  const supported = features.filter((item) => item.supported).map((item) => item.key)
  const unsupported = features.filter((item) => !item.supported).map((item) => item.key)
  assert(supported.includes('microphone'), 'microphone should be supported')
  assert(supported.includes('resolution'), 'resolution should be supported')
  assert(unsupported.includes('powerMode'), 'powerMode stays Legion-unsupported')
  assert(unsupported.includes('hybridMode'), 'hybridMode stays Legion-unsupported')

  const mic = await invoke('feature.getState', { feature: 'microphone' })
  assert(mic?.state === 'On' || mic?.state === 'Off', 'microphone state')

  await invoke('feature.getState', { feature: 'powerMode' }).then(
    () => {
      throw new Error('powerMode getState should fail')
    },
    (error) => {
      assert(String(error).includes('-1001'), `expected -1001, got ${error}`)
    }
  )

  const automation = await invoke('automation.getState')
  assert(automation?.isEnabled === true, 'automation enabled')
  assert(Array.isArray(automation?.pipelines) && automation.pipelines.length >= 2, 'sample pipelines')

  const macros = await invoke('macro.getState')
  assert(macros?.isEnabled === true, 'macros enabled')
  assert(Array.isArray(macros?.slots) && macros.slots.length >= 1, 'sample macro slot')

  const plugins = await invoke('plugins.list')
  assert(Array.isArray(plugins?.plugins) && plugins.plugins.length >= 3, 'plugin catalog')

  const keyboard = await invoke('keyboard.detect')
  assert(keyboard?.mode === 'none', 'keyboard.detect none')

  console.log('linux-host-stub smoke ok')
  console.log(`  supported features: ${supported.join(', ')}`)
  console.log(`  unsupported Legion features: ${unsupported.join(', ')}`)
  console.log(
    `  cpu ${snap.cpu.usage}% ${snap.cpu.temperature}°C  gpu ${snap.gpu.usage}%  battery ${snap.battery.chargeLevel}%`
  )
  process.exitCode = 0
} catch (error) {
  console.error(error instanceof Error ? error.message : error)
  process.exitCode = 1
} finally {
  child.kill('SIGTERM')
}
