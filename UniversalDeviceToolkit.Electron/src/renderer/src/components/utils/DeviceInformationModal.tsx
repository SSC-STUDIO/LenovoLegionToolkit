import { useCallback, useEffect, useMemo, useState } from 'react'
import { create } from 'zustand'
import { message } from 'antd'
import { useTranslation } from 'react-i18next'
import { ArrowUpOutlined, ExportOutlined, ReloadOutlined } from '@ant-design/icons'
import { systemApi, type SystemInfo } from '../../api/system'
import { sensorsApi } from '../../api/sensors'
import './utils.css'

/**
 * Port of Electron DeviceInformationWindow: displays machine identity rows
 * (manufacturer, model, machine type, serial number, BIOS), a hardware section
 * (CPU/GPU names) and a warranty section with a Lenovo support link.
 *
 * The host exposes vendor/model/machineType/biosVersion through `system.info`
 * and CPU/GPU names through the sensors API. Serial number, detailed hardware
 * inventory (memory/baseboard/chassis) and the warranty lookup are not exposed
 * by the host yet — those rows render as "-" or stay hidden. The Lenovo
 * support card mirrors the Electron fallback URL builder.
 */

interface DeviceInfoRequest {
  id: number
}

let requestSeq = 0
let pendingResolve: (() => void) | null = null

interface DeviceInfoCache {
  info: SystemInfo
  cpuName: string | null
  gpuName: string | null
}

/**
 * Module-level cache: the modal renders the cached data instantly on repeat
 * opens and only hits the host again when the user presses refresh.
 */
let deviceInfoCache: DeviceInfoCache | null = null

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

interface HardwareRow {
  labelKey: string
  value: string
  visible: boolean
}

function buildLenovoSupportUri(serialNumber: string | null, machineType: string | null): string {
  const serial = serialNumber && serialNumber !== '-' ? serialNumber.trim() : null
  const mtm = machineType && machineType !== '-' ? machineType.trim() : null
  if (serial && mtm) {
    return `https://pcsupport.lenovo.com/warrantylookup?serialNumber=${encodeURIComponent(serial)}&machineType=${encodeURIComponent(mtm)}`
  }
  if (serial) {
    return `https://pcsupport.lenovo.com/warrantylookup?serialNumber=${encodeURIComponent(serial)}`
  }
  return 'https://pcsupport.lenovo.com/'
}

export default function DeviceInformationModalHost(): React.JSX.Element {
  const { t } = useTranslation()
  const request = useDeviceInfoStore((s) => s.request)
  const settle = useDeviceInfoStore((s) => s.settle)

  const [info, setInfo] = useState<SystemInfo | null>(null)
  const [cpuName, setCpuName] = useState<string | null>(null)
  const [gpuName, setGpuName] = useState<string | null>(null)
  const [ready, setReady] = useState(false)
  const [refreshing, setRefreshing] = useState(false)
  const [failed, setFailed] = useState(false)

  const load = useCallback(async (force = false): Promise<void> => {
    if (!force && deviceInfoCache) {
      setInfo(deviceInfoCache.info)
      setCpuName(deviceInfoCache.cpuName)
      setGpuName(deviceInfoCache.gpuName)
      setReady(true)
      return
    }
    setRefreshing(true)
    try {
      const [systemInfo, sensorsStatus] = await Promise.all([
        systemApi.info(),
        sensorsApi.getStatus().catch(() => null)
      ])
      const status = sensorsStatus ?? null
      deviceInfoCache = {
        info: systemInfo,
        cpuName: status?.cpuName ?? null,
        gpuName: status?.gpuName ?? null
      }
      setInfo(systemInfo)
      setCpuName(status?.cpuName ?? null)
      setGpuName(status?.gpuName ?? null)
      setFailed(false)
      setReady(true)
    } catch {
      setFailed(true)
      setReady(true)
    } finally {
      setRefreshing(false)
    }
  }, [])

  useEffect(() => {
    if (!request) return
    void load()
  }, [request, load])

  const identityRows = useMemo(() => {
    const dash = '-'
    const fallback = failed ? t('wpf.compatibilityCheckErrormessage') : dash
    return [
      { labelKey: 'wpf.deviceInformationWindowmanufacturertitle', value: info?.vendor || fallback },
      { labelKey: 'wpf.deviceInformationWindowmodeltitle', value: info?.model || fallback },
      { labelKey: 'wpf.deviceInformationWindowmachineTypetitle', value: info?.machineType || fallback },
      { labelKey: 'wpf.deviceInformationWindowserialNumbertitle', value: dash },
      { labelKey: 'wpf.deviceInformationWindowbiosVersiontitle', value: info?.biosVersion || fallback }
    ]
  }, [info, failed, t])

  const hardwareRows: HardwareRow[] = useMemo(
    () => [
      {
        labelKey: 'wpf.sensorsControlcputitle',
        value: cpuName ?? '-',
        visible: Boolean(cpuName)
      },
      {
        labelKey: 'wpf.sensorsControlgputitle',
        value: gpuName ?? '-',
        visible: Boolean(gpuName)
      }
    ],
    [cpuName, gpuName]
  )

  const hasHardwareInfo = hardwareRows.some((row) => row.visible)

  const copyRow = async (value: string): Promise<void> => {
    if (!value || value === '-') return
    try {
      await navigator.clipboard.writeText(value)
      void message.success({
        content: t('wpf.copiedToClipboardmessagewithParam').replace('{0}', value),
        duration: 1.5
      })
    } catch {
      // Clipboard unavailable; ignore like the Electron catch block.
    }
  }

  if (!request) return <></>

  const warrantyLink = buildLenovoSupportUri(info?.serialNumber ?? null, info?.machineType ?? null)

  return (
    <div className="udt-utils-backdrop" onClick={settle}>
      <div
        className="udt-utils-modal"
        style={{ width: 600, maxHeight: '86vh' }}
        onClick={(event) => event.stopPropagation()}
      >
        <div className="udt-utils-modal__title">{t('wpf.deviceInformationWindowtitle')}</div>
        <div className="udt-utils-modal__body">
          {!ready ? (
            /* Show a skeleton until the data is ready instead of blank "-" rows. */
            <div className="udt-utils-loading-card" aria-busy="true">
              <div className="udt-utils-loading-row">
                <span className="udt-skeleton" style={{ width: 90, height: 12 }} />
                <span className="udt-skeleton" style={{ width: 180, height: 12 }} />
              </div>
              <div className="udt-utils-loading-row">
                <span className="udt-skeleton" style={{ width: 70, height: 12 }} />
                <span className="udt-skeleton" style={{ width: 220, height: 12 }} />
              </div>
              <div className="udt-utils-loading-row">
                <span className="udt-skeleton" style={{ width: 100, height: 12 }} />
                <span className="udt-skeleton" style={{ width: 160, height: 12 }} />
              </div>
            </div>
          ) : (
            <>
          <div className="udt-utils-card" style={{ padding: 0 }}>
            {identityRows.map((row) => (
              <div key={row.labelKey} className="udt-utils-row" onClick={() => void copyRow(row.value)}>
                <span className="udt-utils-row__label">{t(row.labelKey)}</span>
                <span className="udt-utils-row__value">{row.value}</span>
              </div>
            ))}
          </div>

          {hasHardwareInfo && (
            <>
              <div className="udt-utils-section-title">{t('wpf.deviceInformationWindowhardwaretitle')}</div>
              <div className="udt-utils-card" style={{ padding: 0 }}>
                {hardwareRows.map(
                  (row) =>
                    row.visible && (
                      <div key={row.labelKey} className="udt-utils-row" onClick={() => void copyRow(row.value)}>
                        <span className="udt-utils-row__label">{t(row.labelKey)}</span>
                        <span className="udt-utils-row__value">{row.value}</span>
                      </div>
                    )
                )}
              </div>
            </>
          )}

          <div
            style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', margin: '10px 0' }}
          >
            <div className="udt-utils-section-title" style={{ margin: 0 }}>
              {t('wpf.deviceInformationWindowwarrantytitle')}
            </div>
            <button
              type="button"
              className="udt-utils-button"
              style={{ minWidth: 0, padding: '4px 10px' }}
              disabled={refreshing}
              onClick={() => void load(true)}
              title={t('wpf.deviceInformationWindowrefresh')}
            >
              <ReloadOutlined />
            </button>
          </div>
          <div className="udt-utils-card" style={{ padding: 0 }}>
            <div className="udt-utils-row" style={{ cursor: 'default' }}>
              <span className="udt-utils-row__label">{t('wpf.deviceInformationWindowwarrantyStartDatetitle')}</span>
              <span className="udt-utils-row__value">-</span>
            </div>
            <div className="udt-utils-row" style={{ cursor: 'default' }}>
              <span className="udt-utils-row__label">{t('wpf.deviceInformationWindowwarrantyEndDatetitle')}</span>
              <span className="udt-utils-row__value">-</span>
            </div>
          </div>
          <button
            type="button"
            className="udt-utils-link"
            style={{ width: '100%', justifyContent: 'flex-start', marginBottom: 14 }}
            onClick={() => void window.bridge?.openExternal?.(warrantyLink).catch(() => undefined)}
          >
            <ExportOutlined />
            <span>
              <ArrowUpOutlined style={{ transform: 'rotate(45deg)', fontSize: 12 }} /> {t('wpf.deviceInformationWindowlenovoSupport')}
            </span>
          </button>
            </>
          )}
        </div>
        <div className="udt-utils-modal__actions">
          <button type="button" className="udt-utils-button udt-utils-button--primary" onClick={settle}>
            {t('wpf.close')}
          </button>
        </div>
      </div>
    </div>
  )
}
