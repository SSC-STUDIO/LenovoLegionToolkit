import { useEffect, useMemo, useState } from 'react'
import { create } from 'zustand'
import { useTranslation } from 'react-i18next'
import type { TFunction } from 'i18next'
import { Laptop24Regular } from '../icons/fluent'
import { Select } from 'antd'
import './utils.css'

/**
 * Port of Electron DeviceSetupWindow: first-launch device profile (pack) wizard.
 * Auto-detected packs are pre-selected, the user can override or skip.
 *
 * The option-building logic mirrors DeviceSetupWindow.BuildPackOptions:
 * basic mode is always offered first, hardware packs come before brand basic
 * packs, and the recommended pack is labelled "(recommended)".
 *
 * The device-support catalog download is host-side in the Electron app and is not
 * exposed over the bridge yet, so callers pass `selectablePacks` explicitly;
 * without a catalog the wizard degrades to the basic-mode escape hatch.
 */

export const GENERIC_BASIC_PACK_ID = 'generic-pc-basic'

export interface DeviceSetupMachineInfo {
  vendor?: string | null
  model?: string | null
  machineType?: string | null
}

export interface DevicePackLike {
  id: string
  displayName: string
  enabledFeatures: string[]
}

export interface DeviceSetupOptions {
  machineInformation: DeviceSetupMachineInfo
  recommendedPack?: DevicePackLike | null
  selectablePacks?: DevicePackLike[]
  isBasicMode?: boolean
}

export interface DeviceSetupResult {
  confirmed: boolean
  devicePackId: string | null
  isBasicMode: boolean
}

interface DeviceSetupRequest {
  id: number
  options: DeviceSetupOptions
}

let requestSeq = 0
let pendingResolve: ((result: DeviceSetupResult) => void) | null = null

interface DeviceSetupState {
  request: DeviceSetupRequest | null
  show: (options: DeviceSetupOptions) => void
  settle: (result: DeviceSetupResult) => void
}

const useDeviceSetupStore = create<DeviceSetupState>((set) => ({
  request: null,
  show: (options) => set({ request: { id: ++requestSeq, options } }),
  settle: (result) => {
    pendingResolve?.(result)
    pendingResolve = null
    set({ request: null })
  }
}))

export function openDeviceSetup(options: DeviceSetupOptions): Promise<DeviceSetupResult> {
  return new Promise((resolve) => {
    pendingResolve = resolve
    useDeviceSetupStore.getState().show(options)
  })
}

const HARDWARE_CONTROLS_FEATURE = 'lenovo-hardware-controls'

function isHardwarePack(pack: DevicePackLike): boolean {
  return (pack.enabledFeatures ?? []).some(
    (feature) => feature.toLowerCase() === HARDWARE_CONTROLS_FEATURE
  )
}

interface PackOption {
  id: string
  label: string
  isHardware: boolean
  isRecommended: boolean
}

function buildPackOptions(
  recommendedPack: DevicePackLike | null | undefined,
  selectablePacks: DevicePackLike[] | undefined,
  isBasicMode: boolean,
  t: TFunction
): PackOption[] {
  const options: PackOption[] = []

  // Always offer basic mode first as a safe escape hatch.
  options.push({
    id: GENERIC_BASIC_PACK_ID,
    label: t('wpf.deviceSetupWindowbasicModePackName', 'Basic mode (plugins & optimization only)'),
    isHardware: false,
    isRecommended: recommendedPack == null || isBasicMode
  })

  const packs = (selectablePacks ?? [])
    .filter((pack) => pack != null && pack.id.trim().length > 0)
    .filter((pack, index, array) => array.findIndex((other) => other.id.toLowerCase() === pack.id.toLowerCase()) === index)

  // Hardware packs first, then brand basic packs; alphabetical within groups.
  const ordered = [...packs].sort((a, b) => {
    const aHardware = isHardwarePack(a)
    const bHardware = isHardwarePack(b)
    if (aHardware !== bHardware) return aHardware ? -1 : 1
    return a.displayName.localeCompare(b.displayName, undefined, { sensitivity: 'base' })
  })

  for (const pack of ordered) {
    if (pack.id.toLowerCase() === GENERIC_BASIC_PACK_ID.toLowerCase()) continue

    const isRecommended =
      recommendedPack != null && pack.id.toLowerCase() === recommendedPack.id.toLowerCase()
    const hardware = isHardwarePack(pack)
    let label = isRecommended
      ? t('wpf.deviceSetupWindowrecommendedPackFormat').replace('{0}', pack.displayName)
      : pack.displayName
    label = hardware
      ? t('wpf.deviceSetupWindowhardwarePackFormat').replace('{0}', label)
      : t('wpf.deviceSetupWindowbasicPackFormat').replace('{0}', label)

    options.push({ id: pack.id, label, isHardware: hardware, isRecommended })
  }

  // Ensure the recommended pack is present even if the catalog list was empty/partial.
  if (
    recommendedPack != null &&
    !options.some((option) => option.id.toLowerCase() === recommendedPack.id.toLowerCase())
  ) {
    options.splice(1, 0, {
      id: recommendedPack.id,
      label: t('wpf.deviceSetupWindowrecommendedPackFormat').replace('{0}', recommendedPack.displayName),
      isHardware: isHardwarePack(recommendedPack),
      isRecommended: true
    })
  }

  return options
}

export default function DeviceSetupModalHost(): React.JSX.Element {
  const { t } = useTranslation()
  const request = useDeviceSetupStore((s) => s.request)
  const settle = useDeviceSetupStore((s) => s.settle)

  const [selectedId, setSelectedId] = useState<string | null>(null)
  const [preparing, setPreparing] = useState(false)
  const [statusText, setStatusText] = useState<string | null>(null)

  const options = useMemo(() => {
    if (!request) return []
    const { recommendedPack, selectablePacks, isBasicMode } = request.options
    return buildPackOptions(recommendedPack, selectablePacks, isBasicMode === true, t)
  }, [request, t])

  useEffect(() => {
    if (!request) return
    setPreparing(false)
    setStatusText(null)
    const preferred =
      options.find((option) => option.isRecommended) ?? options[0] ?? null
    setSelectedId(preferred?.id ?? null)
  }, [request, options])

  if (!request) return <></>

  const { machineInformation, recommendedPack, isBasicMode } = request.options
  const selected = options.find((option) => option.id === selectedId) ?? null

  const summary = recommendedPack == null || isBasicMode
    ? t('wpf.deviceSetupWindowbasicModeSummary')
    : t('wpf.deviceSetupWindowmatchingPackSummary')

  const hint = recommendedPack == null || isBasicMode
    ? t('wpf.deviceSetupWindowbasicModeHint')
    : t('wpf.deviceSetupWindowmatchingPackHint')

  const confirm = (): void => {
    if (preparing) return
    setPreparing(true)
    setStatusText(t('wpf.deviceSetupWindowpreparing'))
    window.setTimeout(() => {
      settle({
        confirmed: true,
        devicePackId: selected?.id ?? null,
        isBasicMode: selected == null || !selected.isHardware
      })
    }, 0)
  }

  const skip = (): void => {
    if (preparing) return
    settle({ confirmed: false, devicePackId: null, isBasicMode: true })
  }

  return (
    <div className="udt-utils-backdrop">
      <div
        className="udt-utils-modal"
        style={{ width: 620, maxWidth: 'min(92vw, 620px)', maxHeight: 'min(88vh, 480px)' }}
        onClick={(event) => event.stopPropagation()}
      >
        <div className="udt-utils-modal__title">{t('wpf.deviceSetupWindowtitle')}</div>
        <div className="udt-utils-modal__body">
          <div style={{ display: 'flex', gap: 16 }}>
            <Laptop24Regular style={{ fontSize: 40, color: 'var(--udt-text-secondary)' }} />
            <div style={{ flex: 1 }}>
              <div style={{ fontWeight: 600, marginBottom: 8 }}>{t('wpf.deviceSetupWindowtitle')}</div>
              <p className="udt-utils-text" style={{ marginTop: 0, marginBottom: 14 }}>
                {summary}
              </p>
              <div className="udt-utils-row" style={{ cursor: 'default' }}>
                <span className="udt-utils-row__label">{t('wpf.unsupportedWindowvendor')}</span>
                <span className="udt-utils-row__value">
                  {machineInformation.vendor?.trim() ? machineInformation.vendor : t('wpf.unnamed')}
                </span>
              </div>
              <div className="udt-utils-row" style={{ cursor: 'default' }}>
                <span className="udt-utils-row__label">{t('wpf.unsupportedWindowmodel')}</span>
                <span className="udt-utils-row__value">
                  {machineInformation.model?.trim() ? machineInformation.model : t('wpf.unnamed')}
                </span>
              </div>
              <div className="udt-utils-row" style={{ cursor: 'default', borderBottom: 'none' }}>
                <span className="udt-utils-row__label">{t('wpf.unsupportedWindowmachineType')}</span>
                <span className="udt-utils-row__value">
                  {machineInformation.machineType?.trim() ? machineInformation.machineType : t('wpf.unnamed')}
                </span>
              </div>
              <div style={{ fontWeight: 500, margin: '14px 0 6px' }}>
                {t('wpf.deviceSetupWindowselectPackLabel')}
              </div>
              <Select<string>
                aria-label={t('wpf.deviceSetupWindowselectPackLabel')}
                className="udt-utils-select"
                classNames={{
                  popup: { root: 'udt-device-setup-select-dropdown' }
                }}
                disabled={preparing}
                options={options.map((option) => ({
                  value: option.id,
                  label: option.label
                }))}
                value={selectedId ?? undefined}
                onChange={(value) => setSelectedId(value || null)}
              />
              <p className="udt-utils-text" style={{ margin: '8px 0 0' }}>
                {selected?.isHardware
                  ? t('wpf.deviceSetupWindowhardwarePackDetail')
                  : t('wpf.deviceSetupWindowbasicPackDetail')}
              </p>
              <p className="udt-utils-text" style={{ margin: '10px 0 0' }}>{hint}</p>
              {statusText && <p className="udt-utils-status">{statusText}</p>}
            </div>
          </div>
        </div>
        <div className="udt-utils-modal__actions">
          <button type="button" className="udt-utils-button" disabled={preparing} onClick={skip}>
            {t('wpf.deviceSetupWindowskipButton')}
          </button>
          <button
            type="button"
            className="udt-utils-button udt-utils-button--primary"
            disabled={preparing}
            onClick={confirm}
          >
            {t('wpf.deviceSetupWindowconfirmButton')}
          </button>
        </div>
      </div>
    </div>
  )
}
