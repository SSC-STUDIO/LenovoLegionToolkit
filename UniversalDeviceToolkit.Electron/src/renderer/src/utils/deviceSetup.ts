/**
 * Pure first-run device-pack selection. Host persistence and download stay
 * outside this module; the renderer only ranks catalog entries.
 */

export const GENERIC_BASIC_PACK_ID = 'generic-pc-basic'
export const HARDWARE_CONTROLS_FEATURE_ID = 'lenovo-hardware-controls'

export type InstallerDeviceMode = 'auto' | 'basic'

export interface DevicePackCatalogEntry {
  id: string
  displayName: string
  vendor: string
  enabledFeatures?: readonly string[]
  hiddenFeatures?: readonly string[]
}

export interface FirstRunPackOption {
  id: string
  displayName: string
  vendor: string
  isHardware: boolean
  isRecommended: boolean
  isBasic: boolean
}

export interface FirstRunPackSelection {
  selectedPackId: string
  recommendedPackId: string
  options: FirstRunPackOption[]
  isBasicMode: boolean
}

function featureListContains(features: readonly string[] | undefined, featureId: string): boolean {
  return (features ?? []).some((feature) => feature.toLowerCase() === featureId.toLowerCase())
}

export function isHardwareDevicePack(
  pack: Pick<DevicePackCatalogEntry, 'enabledFeatures' | 'hiddenFeatures'> | null | undefined
): boolean {
  if (pack == null) return false
  return (
    featureListContains(pack.enabledFeatures, HARDWARE_CONTROLS_FEATURE_ID) &&
    !featureListContains(pack.hiddenFeatures, HARDWARE_CONTROLS_FEATURE_ID)
  )
}

export function normalizeDevicePackId(packId: string | null | undefined): string {
  const trimmed = packId?.trim() ?? ''
  return trimmed.length > 0 ? trimmed : GENERIC_BASIC_PACK_ID
}

export function findDevicePack(
  packs: readonly DevicePackCatalogEntry[],
  packId: string | null | undefined
): DevicePackCatalogEntry | null {
  const normalized = packId?.trim() ?? ''
  if (normalized.length === 0) return null
  return packs.find((pack) => pack.id.toLowerCase() === normalized.toLowerCase()) ?? null
}

export function resolveConfirmedPackId(
  selectedPackId: string | null | undefined,
  packs: readonly DevicePackCatalogEntry[]
): string {
  const normalized = normalizeDevicePackId(selectedPackId)
  if (normalized.toLowerCase() === GENERIC_BASIC_PACK_ID) return GENERIC_BASIC_PACK_ID
  return findDevicePack(packs, normalized)?.id ?? GENERIC_BASIC_PACK_ID
}

export function buildFirstRunPackOptions(
  packs: readonly DevicePackCatalogEntry[],
  recommendedPackId: string
): FirstRunPackOption[] {
  const recommended = recommendedPackId.toLowerCase()
  const seen = new Set<string>()
  const options: FirstRunPackOption[] = []

  const push = (pack: DevicePackCatalogEntry): void => {
    const key = pack.id.toLowerCase()
    if (seen.has(key) || pack.id.trim().length === 0) return
    seen.add(key)
    const isHardware = isHardwareDevicePack(pack)
    options.push({
      id: pack.id,
      displayName: pack.displayName.trim().length > 0 ? pack.displayName : pack.id,
      vendor: pack.vendor,
      isHardware,
      isRecommended: key === recommended,
      isBasic: !isHardware || key === GENERIC_BASIC_PACK_ID
    })
  }

  if (!seen.has(GENERIC_BASIC_PACK_ID)) {
    const generic = findDevicePack(packs, GENERIC_BASIC_PACK_ID) ?? {
      id: GENERIC_BASIC_PACK_ID,
      displayName: 'Generic PC Basic',
      vendor: '*',
      enabledFeatures: ['system-optimization', 'language', 'theme', 'updates', 'logs'],
      hiddenFeatures: [HARDWARE_CONTROLS_FEATURE_ID, 'power-modes', 'keyboard-backlight', 'god-mode', 'gpu-overclock', 'fan-curve']
    }
    push(generic)
  }

  for (const pack of packs) push(pack)

  options.sort((left, right) => {
    if (left.isRecommended !== right.isRecommended) return left.isRecommended ? -1 : 1
    if (left.isHardware !== right.isHardware) return left.isHardware ? -1 : 1
    if (left.id.toLowerCase() === GENERIC_BASIC_PACK_ID) return 1
    if (right.id.toLowerCase() === GENERIC_BASIC_PACK_ID) return -1
    return left.displayName.localeCompare(right.displayName)
  })

  return options
}

export function selectFirstRunPack(
  packs: readonly DevicePackCatalogEntry[],
  detectedPackId: string | null | undefined,
  installerDeviceMode: InstallerDeviceMode | null | undefined = 'auto'
): FirstRunPackSelection {
  const recommendedPackId =
    installerDeviceMode === 'basic'
      ? GENERIC_BASIC_PACK_ID
      : resolveConfirmedPackId(detectedPackId, packs)
  const selected = findDevicePack(packs, recommendedPackId)
  const options = buildFirstRunPackOptions(packs, recommendedPackId)

  return {
    selectedPackId: recommendedPackId,
    recommendedPackId,
    options,
    isBasicMode: recommendedPackId.toLowerCase() === GENERIC_BASIC_PACK_ID || !isHardwareDevicePack(selected)
  }
}
