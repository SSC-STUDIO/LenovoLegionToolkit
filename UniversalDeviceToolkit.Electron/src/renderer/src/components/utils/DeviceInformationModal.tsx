import { create } from 'zustand'
import type { JSX } from 'react'
import DeviceInfoModal from '../DeviceInfoModal'

/**
 * Utils-host entry for device information. The TitleBar button uses
 * DeviceInfoModal directly; this store lets other flows open the same UI.
 */

interface DeviceInfoRequest {
  id: number
}

let requestSeq = 0
let pendingResolve: (() => void) | null = null

interface DeviceInfoState {
  request: DeviceInfoRequest | null
  show: () => void
  settle: () => void
}

const useDeviceInfoStore = create<DeviceInfoState>((set) => ({
  request: null,
  show: () => set({ request: { id: ++requestSeq } }),
  settle: () => {
    pendingResolve?.()
    pendingResolve = null
    set({ request: null })
  }
}))

export function openDeviceInformation(): Promise<void> {
  return new Promise((resolve) => {
    pendingResolve = resolve
    useDeviceInfoStore.getState().show()
  })
}

export default function DeviceInformationModalHost(): JSX.Element {
  const request = useDeviceInfoStore((s) => s.request)
  const settle = useDeviceInfoStore((s) => s.settle)
  return <DeviceInfoModal open={request != null} onClose={settle} />
}
