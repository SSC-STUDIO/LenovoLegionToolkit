import { useEffect, useMemo, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { message } from 'antd'
import { invoke } from '../api/bridge'
import { systemApi, type SystemInfo } from '../api/system'
import { sensorsApi } from '../api/sensors'
import './titlebar.css'

/**
 * Device info modal — port of the WPF DeviceInformationWindow, opened from the
 * TitleBar device button.
 *
 * Primary data source is the `device.info` bridge method (implemented host
 * side; contract below mirrors WPF MachineInformation + HardwareInventory +
 * warranty). When `device.info` is unavailable it falls back to the existing
 * `system.info` + `sensors.getStatus` APIs so identity rows and CPU/GPU names
 * still render; the warranty section stays hidden without data.
 */

export interface DeviceInfoProcessor {
  name?: string | null
  numberOfCores?: number | null
  numberOfLogicalProcessors?: number | null
  maxClockSpeedMHz?: number | null
}

export interface DeviceInfoVideoController {
  name?: string | null
  adapterCompatibility?: string | null
  adapterRamBytes?: number | null
}

export interface DeviceInfoMemory {
  totalCapacityBytes?: number | null
  moduleCount?: number | null
  configuredClockSpeedMHz?: number | null
  speedMHz?: number | null
}

export interface DeviceInfoWarranty {
  startDate?: string | null
  endDate?: string | null
  link?: string | null
}

/** Contract for the `device.info` bridge method. */
export interface DeviceInfo {
  vendor?: string | null
  model?: string | null
  machineType?: string | null
  serialNumber?: string | null
  biosVersion?: string | null
  processor?: DeviceInfoProcessor | null
  videoController?: DeviceInfoVideoController | null
  memory?: DeviceInfoMemory | null
  warranty?: DeviceInfoWarranty | null
}

interface DeviceInfoModalProps {
  open: boolean
  onClose: () => void
}

const DASH = '-'

function formatCapacity(bytes?: number | null): string | null {
  if (typeof bytes !== 'number' || !Number.isFinite(bytes) || bytes <= 0) return null
  return `${(bytes / 1024 ** 3).toFixed(1).replace(/\.0$/, '')} GiB`
}

function formatProcessor(processor: DeviceInfoProcessor | null | undefined): string {
  if (!processor?.name) return DASH
  const details: string[] = []
  if (
    typeof processor.numberOfCores === 'number' &&
    typeof processor.numberOfLogicalProcessors === 'number'
  ) {
    details.push(`${processor.numberOfCores}C/${processor.numberOfLogicalProcessors}T`)
  }
  if (typeof processor.maxClockSpeedMHz === 'number') {
    details.push(`${processor.maxClockSpeedMHz} MHz`)
  }
  return details.length === 0 ? processor.name : `${processor.name} (${details.join(', ')})`
}

function formatVideoController(
  videoController: DeviceInfoVideoController | null | undefined
): string {
  if (!videoController?.name) return DASH
  const details: string[] = []
  if (videoController.adapterCompatibility) details.push(videoController.adapterCompatibility)
  const ram = formatCapacity(videoController.adapterRamBytes)
  if (ram) details.push(ram)
  return details.length === 0 ? videoController.name : `${videoController.name} (${details.join(', ')})`
}

function formatMemory(memory: DeviceInfoMemory | null | undefined): string {
  if (!memory) return DASH
  const details: string[] = []
  const total = formatCapacity(memory.totalCapacityBytes)
  if (total) details.push(total)
  if (typeof memory.moduleCount === 'number' && memory.moduleCount > 0) {
    details.push(`${memory.moduleCount} module${memory.moduleCount === 1 ? '' : 's'}`)
  }
  const speed = memory.configuredClockSpeedMHz ?? memory.speedMHz
  if (typeof speed === 'number') details.push(`${speed} MHz`)
  return details.length === 0 ? DASH : details.join(', ')
}

/** Formats ISO yyyy-MM-dd (or a leading ISO date) as a locale short date. */
function formatDate(value: string | null | undefined): string {
  if (!value) return DASH
  const match = /^(\d{4})-(\d{2})-(\d{2})/.exec(value)
  if (!match) return value
  const date = new Date(Number(match[1]), Number(match[2]) - 1, Number(match[3]))
  return date.toLocaleDateString()
}

/** Mirrors the WPF warranty fallback URL builder. */
function buildLenovoSupportUri(serialNumber: string | null, machineType: string | null): string {
  const serial = serialNumber && serialNumber !== DASH ? serialNumber.trim() : null
  const mtm = machineType && machineType !== DASH ? machineType.trim() : null
  if (serial && mtm) {
    return `https://pcsupport.lenovo.com/warrantylookup?serialNumber=${encodeURIComponent(serial)}&machineType=${encodeURIComponent(mtm)}`
  }
  if (serial) {
    return `https://pcsupport.lenovo.com/warrantylookup?serialNumber=${encodeURIComponent(serial)}`
  }
  return 'https://pcsupport.lenovo.com/'
}

interface FallbackInfo {
  identity: SystemInfo | null
  cpuName: string | null
  gpuName: string | null
}

export default function DeviceInfoModal({ open, onClose }: DeviceInfoModalProps): React.JSX.Element | null {
  const { t } = useTranslation()
  const [device, setDevice] = useState<DeviceInfo | null>(null)
  const [fallback, setFallback] = useState<FallbackInfo | null>(null)

  useEffect(() => {
    if (!open) return
    let cancelled = false
    const load = async (): Promise<void> => {
      try {
        const result = await invoke<DeviceInfo>('device.info')
        if (!cancelled) setDevice(result)
      } catch (error) {
        console.warn('[device-info] device.info failed, falling back to system.info:', error)
        try {
          const [identity, sensors] = await Promise.all([
            systemApi.info().catch(() => null),
            sensorsApi.getStatus().catch(() => null)
          ])
          if (!cancelled) {
            setFallback({ identity, cpuName: sensors?.cpuName ?? null, gpuName: sensors?.gpuName ?? null })
          }
        } catch (fallbackError) {
          console.warn('[device-info] fallback info failed:', fallbackError)
          if (!cancelled) setFallback({ identity: null, cpuName: null, gpuName: null })
        }
      }
    }
    void load()
    return () => {
      cancelled = true
    }
  }, [open])

  useEffect(() => {
    if (!open) return
    const onKeyDown = (event: KeyboardEvent): void => {
      if (event.key === 'Escape') onClose()
    }
    window.addEventListener('keydown', onKeyDown)
    return () => window.removeEventListener('keydown', onKeyDown)
  }, [open, onClose])

  const identityRows = useMemo(() => {
    const identity = fallback?.identity
    return [
      {
        key: 'manufacturer',
        label: t('wpf.deviceInformationWindowmanufacturertitle'),
        value: device?.vendor ?? identity?.vendor ?? DASH
      },
      {
        key: 'model',
        label: t('wpf.deviceInformationWindowmodeltitle'),
        value: device?.model ?? identity?.model ?? DASH
      },
      {
        key: 'machineType',
        label: t('wpf.deviceInformationWindowmachineTypetitle'),
        value: device?.machineType ?? identity?.machineType ?? DASH
      },
      {
        key: 'serialNumber',
        label: t('wpf.deviceInformationWindowserialNumbertitle'),
        value: device?.serialNumber ?? DASH
      },
      {
        key: 'biosVersion',
        label: t('wpf.deviceInformationWindowbiosVersiontitle'),
        value: device?.biosVersion ?? identity?.biosVersion ?? DASH
      }
    ]
  }, [device, fallback, t])

  const hardwareRows = useMemo(() => {
    const cpuName = device?.processor?.name || fallback?.cpuName
    const gpuName = device?.videoController?.name || fallback?.gpuName
    return [
      {
        key: 'cpu',
        label: t('wpf.sensorsControlcputitle'),
        value: cpuName ? (device?.processor ? formatProcessor(device.processor) : cpuName) : DASH
      },
      {
        key: 'gpu',
        label: t('wpf.sensorsControlgputitle'),
        value: gpuName
          ? device?.videoController
            ? formatVideoController(device.videoController)
            : gpuName
          : DASH
      },
      {
        key: 'memory',
        label: t('wpf.deviceInformationWindowmemorytitle'),
        value: formatMemory(device?.memory)
      }
    ]
  }, [device, fallback, t])

  const warranty = device?.warranty
  const hasWarranty = Boolean(warranty && (warranty.startDate || warranty.endDate || warranty.link))
  const warrantyLink =
    warranty?.link ??
    buildLenovoSupportUri(device?.serialNumber ?? null, device?.machineType ?? null)

  const copyRow = async (value: string): Promise<void> => {
    if (!value || value === DASH) return
    try {
      await navigator.clipboard.writeText(value)
      void message.success({
        content: t('wpf.copiedToClipboardmessagewithParam').replace('{0}', value),
        duration: 1.5
      })
    } catch {
      // Clipboard unavailable; ignore like the WPF catch block.
    }
  }

  if (!open) return null

  return (
    <div className="udt-device-info-backdrop" onClick={onClose}>
      <div
        className="udt-device-info-modal"
        role="dialog"
        aria-modal="true"
        aria-label={t('wpf.deviceInformationWindowtitle')}
        onClick={(event) => event.stopPropagation()}
      >
        <div className="udt-device-info-modal__title">{t('wpf.deviceInformationWindowtitle')}</div>
        <div className="udt-device-info-modal__body">
          <div className="udt-device-info-section">
            <div className="udt-device-info-card">
              {identityRows.map((row) => (
                <div key={row.key} className="udt-device-info-row" onClick={() => void copyRow(row.value)}>
                  <span className="udt-device-info-row__label">{row.label}</span>
                  <span className="udt-device-info-row__value">{row.value}</span>
                </div>
              ))}
            </div>
          </div>

          <div className="udt-device-info-section">
            <div className="udt-device-info-section__title">
              {t('wpf.deviceInformationWindowhardwaretitle')}
            </div>
            <div className="udt-device-info-card">
              {hardwareRows.map((row) => (
                <div key={row.key} className="udt-device-info-row" onClick={() => void copyRow(row.value)}>
                  <span className="udt-device-info-row__label">{row.label}</span>
                  <span className="udt-device-info-row__value">{row.value}</span>
                </div>
              ))}
            </div>
          </div>

          {hasWarranty && (
            <div className="udt-device-info-section">
              <div className="udt-device-info-section__title">
                {t('wpf.deviceInformationWindowwarrantytitle')}
              </div>
              <div className="udt-device-info-card">
                <div className="udt-device-info-row" onClick={() => void copyRow(formatDate(warranty?.startDate))}>
                  <span className="udt-device-info-row__label">
                    {t('wpf.deviceInformationWindowwarrantyStartDatetitle')}
                  </span>
                  <span className="udt-device-info-row__value">{formatDate(warranty?.startDate)}</span>
                </div>
                <div className="udt-device-info-row" onClick={() => void copyRow(formatDate(warranty?.endDate))}>
                  <span className="udt-device-info-row__label">
                    {t('wpf.deviceInformationWindowwarrantyEndDatetitle')}
                  </span>
                  <span className="udt-device-info-row__value">{formatDate(warranty?.endDate)}</span>
                </div>
              </div>
              <button
                type="button"
                className="udt-device-info-link"
                onClick={() => void window.bridge?.openExternal?.(warrantyLink)?.catch(() => undefined)}
              >
                <span>{t('wpf.deviceInformationWindowlenovoSupport')}</span>
              </button>
            </div>
          )}
        </div>
        <div className="udt-device-info-modal__actions">
          <button type="button" className="udt-device-info-close" onClick={onClose}>
            {t('wpf.close')}
          </button>
        </div>
      </div>
    </div>
  )
}
