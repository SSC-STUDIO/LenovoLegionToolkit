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
  private nextId = 1

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

  start(hostPath: string, args: string[] = []): void {
    if (this.child) throw new Error('Host already started')

    this.child = spawn(hostPath, args, {
      stdio: ['pipe', 'pipe', 'pipe'],
      windowsHide: true
    })

    this.child.stderr.on('data', (d: Buffer) => {
      console.error(`[host] ${d.toString().trim()}`)
    })

    this.child.on('error', (error) => {
      console.error(`[host] spawn error: ${error.message}`)
    })

    this.rl = createInterface({ input: this.child.stdout })
    this.rl.on('line', (line) => this.handleLine(line))

    this.child.on('exit', (code, signal) => {
      console.error(`[host] exited code=${code} signal=${signal}`)
      const error = new Error(`Host exited (code=${code ?? 'n/a'} signal=${signal ?? 'n/a'})`)
      for (const [, request] of this.pending) request.reject(error)
      this.pending.clear()
      this.rl?.close()
      this.rl = null
      this.child = null
    })
  }

  get isRunning(): boolean {
    return this.child !== null && !this.child.killed
  }

  invoke(method: string, params: unknown = {}): Promise<unknown> {
    if (!this.child || !this.child.stdin.writable) {
      return Promise.reject(new Error('Host is not running'))
    }
    const id = this.nextId++
    const promise = new Promise<unknown>((resolve, reject) => {
      this.pending.set(id, { resolve, reject })
    })
    this.child.stdin.write(`${JSON.stringify({ id, method, params })}\n`)
    return promise
  }

  stop(): Promise<void> {
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
      child.stdin.write(`${JSON.stringify({ id: this.nextId++, method: 'app.quit', params: {} })}\n`)
    })
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
