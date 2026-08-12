import { spawn, ChildProcessWithoutNullStreams } from 'child_process'
import { createInterface } from 'readline'

interface PendingRequest {
  resolve: (value: unknown) => void
  reject: (error: Error) => void
}

interface HostLine {
  event?: string
  data?: unknown
  id?: number
  result?: unknown
  error?: { code: number; message: string }
}

interface ReadyWaiter {
  resolve: () => void
  reject: (error: Error) => void
}

const READY_TIMEOUT_MS = 45_000
const MAX_RESTART_ATTEMPTS = 3
const RESTART_DELAY_MS = 750

/**
 * Manages the UniversalDeviceToolkit.Host child process and speaks the
 * newline-delimited JSON-RPC protocol over its stdio.
 */
export class HostClient {
  private child: ChildProcessWithoutNullStreams | null = null
  private rl: ReturnType<typeof createInterface> | null = null
  private pending = new Map<number, PendingRequest>()
  private listeners = new Map<string, Set<(data: unknown) => void>>()
  private anyListeners = new Set<(event: string, data: unknown) => void>()
  private readyWaiters: ReadyWaiter[] = []
  private nextId = 1
  private ready = false
  private stopping = false
  private starting = false
  private hostPath: string | null = null
  private hostArgs: string[] = []
  private lastError: string | null = null
  private restartAttempts = 0
  private restartTimer: ReturnType<typeof setTimeout> | null = null
  private lastReadyData: unknown = null

  on(event: string, callback: (data: unknown) => void): () => void {
    if (!this.listeners.has(event)) this.listeners.set(event, new Set())
    this.listeners.get(event)!.add(callback)
    return () => this.listeners.get(event)?.delete(callback)
  }

  /** Forward every host event (sensors.updated, settings.changed, etc.). */
  onAny(callback: (event: string, data: unknown) => void): () => void {
    this.anyListeners.add(callback)
    return () => this.anyListeners.delete(callback)
  }

  get isRunning(): boolean {
    return this.child !== null && !this.child.killed
  }

  get isReady(): boolean {
    return this.ready && this.isRunning
  }

  get lastReadyPayload(): unknown {
    return this.lastReadyData
  }

  get lastFailure(): string | null {
    return this.lastError
  }

  /** Record a fatal start failure (missing binary, etc.) before spawn runs. */
  reportFatalError(message: string): void {
    this.lastError = message
    this.ready = false
    this.emitSynthetic('host.error', { message, fatal: true })
    this.rejectReadyWaiters(new Error(message))
  }

  /**
   * Resolves once {@code host.ready} has been observed for the current child.
   * Rejects on timeout, spawn failure, or unexpected exit.
   */
  waitUntilReady(timeoutMs = READY_TIMEOUT_MS): Promise<void> {
    if (this.isReady) return Promise.resolve()
    if (this.lastError && !this.child && !this.starting) {
      return Promise.reject(new Error(this.lastError))
    }

    return new Promise<void>((resolve, reject) => {
      const timer = setTimeout(() => {
        remove()
        reject(new Error(this.lastError ?? 'Host did not become ready in time'))
      }, timeoutMs)

      const waiter: ReadyWaiter = {
        resolve: () => {
          remove()
          resolve()
        },
        reject: (error) => {
          remove()
          reject(error)
        }
      }

      const remove = (): void => {
        clearTimeout(timer)
        const index = this.readyWaiters.indexOf(waiter)
        if (index >= 0) this.readyWaiters.splice(index, 1)
      }

      this.readyWaiters.push(waiter)
    })
  }

  start(hostPath: string, args: string[] = []): void {
    if (this.child) throw new Error('Host already started')

    this.hostPath = hostPath
    this.hostArgs = args
    this.stopping = false
    this.starting = true
    this.ready = false
    this.lastError = null

    if (this.restartTimer) {
      clearTimeout(this.restartTimer)
      this.restartTimer = null
    }

    console.log(`[host] spawning: ${hostPath}${args.length > 0 ? ` ${args.join(' ')}` : ''}`)

    this.child = spawn(hostPath, args, {
      stdio: ['pipe', 'pipe', 'pipe'],
      windowsHide: true
    })

    this.child.stderr.on('data', (d: Buffer) => {
      console.error(`[host] ${d.toString().trim()}`)
    })

    this.child.on('error', (error) => {
      this.starting = false
      this.lastError = `Host spawn failed: ${error.message}`
      console.error(`[host] spawn error: ${error.message}`)
      this.emitSynthetic('host.error', { message: this.lastError })
      this.rejectReadyWaiters(new Error(this.lastError))
    })

    this.rl = createInterface({ input: this.child.stdout })
    this.rl.on('line', (line) => this.handleLine(line))

    this.child.on('exit', (code, signal) => {
      const wasReady = this.ready
      this.starting = false
      this.ready = false
      console.error(`[host] exited code=${code} signal=${signal}`)
      const error = new Error(`Host exited (code=${code ?? 'n/a'} signal=${signal ?? 'n/a'})`)
      this.lastError = error.message
      for (const [, request] of this.pending) request.reject(error)
      this.pending.clear()
      this.rl?.close()
      this.rl = null
      this.child = null
      this.emitSynthetic('host.exited', { code, signal, wasReady })
      this.rejectReadyWaiters(error)

      if (!this.stopping && this.hostPath) {
        this.scheduleRestart()
      }
    })

    // Spawn returned — child handle exists; readiness still waits for host.ready.
    this.starting = false
  }

  async invoke(method: string, params: unknown = {}): Promise<unknown> {
    if (this.stopping) {
      return Promise.reject(new Error('Host is shutting down'))
    }

    if (!this.child || !this.child.stdin.writable) {
      if (this.hostPath && !this.stopping) {
        // Child gone (crash / first call before start finished restarting): ensure a spawn.
        if (!this.child && !this.starting) {
          try {
            this.start(this.hostPath, this.hostArgs)
          } catch (error) {
            const message = error instanceof Error ? error.message : String(error)
            return Promise.reject(new Error(message))
          }
        }
      } else {
        return Promise.reject(new Error(this.lastError ?? 'Host is not running'))
      }
    }

    try {
      await this.waitUntilReady()
    } catch (error) {
      return Promise.reject(error instanceof Error ? error : new Error(String(error)))
    }

    if (!this.child || !this.child.stdin.writable) {
      return Promise.reject(new Error(this.lastError ?? 'Host is not running'))
    }

    const id = this.nextId++
    const promise = new Promise<unknown>((resolve, reject) => {
      this.pending.set(id, { resolve, reject })
    })
    this.child.stdin.write(`${JSON.stringify({ id, method, params })}\n`)
    return promise
  }

  stop(): Promise<void> {
    this.stopping = true
    if (this.restartTimer) {
      clearTimeout(this.restartTimer)
      this.restartTimer = null
    }
    const child = this.child
    if (!child || child.killed) return Promise.resolve()
    return new Promise((resolve) => {
      const timer = setTimeout(() => {
        child.kill()
        resolve()
      }, 5000)
      child.once('exit', () => {
        clearTimeout(timer)
        resolve()
      })
      try {
        if (child.stdin.writable) {
          child.stdin.write(`${JSON.stringify({ id: this.nextId++, method: 'app.quit', params: {} })}\n`)
        } else {
          child.kill()
        }
      } catch {
        child.kill()
      }
    })
  }

  private scheduleRestart(): void {
    if (this.restartAttempts >= MAX_RESTART_ATTEMPTS) {
      console.error(`[host] giving up after ${MAX_RESTART_ATTEMPTS} restart attempts`)
      this.emitSynthetic('host.error', {
        message: this.lastError ?? 'Host failed repeatedly',
        fatal: true
      })
      return
    }
    if (this.restartTimer || !this.hostPath) return

    this.restartAttempts += 1
    console.log(`[host] scheduling restart attempt ${this.restartAttempts}/${MAX_RESTART_ATTEMPTS}`)
    this.restartTimer = setTimeout(() => {
      this.restartTimer = null
      if (this.stopping || this.child || !this.hostPath) return
      try {
        this.start(this.hostPath, this.hostArgs)
      } catch (error) {
        const message = error instanceof Error ? error.message : String(error)
        this.lastError = message
        console.error(`[host] restart failed: ${message}`)
        this.emitSynthetic('host.error', { message, fatal: true })
      }
    }, RESTART_DELAY_MS)
  }

  private setReady(data: unknown): void {
    this.ready = true
    this.lastReadyData = data
    this.lastError = null
    this.restartAttempts = 0
    const waiters = this.readyWaiters.splice(0)
    for (const waiter of waiters) waiter.resolve()
  }

  private rejectReadyWaiters(error: Error): void {
    const waiters = this.readyWaiters.splice(0)
    for (const waiter of waiters) waiter.reject(error)
  }

  private emitSynthetic(event: string, data: unknown): void {
    const set = this.listeners.get(event)
    if (set) {
      for (const callback of set) {
        try {
          callback(data)
        } catch (error) {
          console.error(`[host] event handler failed: ${error}`)
        }
      }
    }
    for (const callback of this.anyListeners) {
      try {
        callback(event, data)
      } catch (error) {
        console.error(`[host] any-event handler failed: ${error}`)
      }
    }
  }

  private handleLine(line: string): void {
    let message: HostLine
    try {
      message = JSON.parse(line) as HostLine
    } catch {
      return
    }
    if (!message || typeof message !== 'object') return

    if (typeof message.event === 'string') {
      if (message.event === 'host.ready') {
        this.setReady(message.data)
      }

      const set = this.listeners.get(message.event)
      if (set) {
        for (const callback of set) {
          try {
            callback(message.data)
          } catch (error) {
            console.error(`[host] event handler failed: ${error}`)
          }
        }
      }
      for (const callback of this.anyListeners) {
        try {
          callback(message.event, message.data)
        } catch (error) {
          console.error(`[host] any-event handler failed: ${error}`)
        }
      }
      return
    }

    if (typeof message.id === 'number') {
      const request = this.pending.get(message.id)
      if (!request) return
      this.pending.delete(message.id)
      if (message.error) {
        request.reject(new Error(message.error.message ?? `Host error ${message.error.code}`))
      } else {
        request.resolve(message.result)
      }
    }
  }
}

export const hostClient = new HostClient()
