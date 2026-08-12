/**
 * Mirrors Electron StartupDeviceSetupCoordinator.BuildSelectablePacks: preference
 * ordering for the first-launch device-setup wizard.
 *
 * The setup state file (`%APPDATA%\UniversalDeviceToolkit\device-setup`),
 * the device-support catalog and the on-demand pack download are host-side;
 * this module contains the pure selection logic so the renderer can render
 * the exact same ordering as the Electron wizard.
 */

export interface MachineInformation {
  vendor?: string
  model?: string
}

export interface DevicePack {
  id: string
  displayName: string
  vendor?: string
  vendorAliases: string[]
  modelKeywords: string[]
  enabledFeatures: string[]
}

export interface DeviceSupportCatalog {
  devicePacks: DevicePack[]
}

const HARDWARE_CONTROLS_FEATURE = 'lenovo-hardware-controls'
const HARDWARE_REST_CAP = 12
const BASIC_REST_CAP = 24

/**
 * Prefer packs that match this vendor / model family so the combo is usable,
 * then append remaining catalog packs (hardware first).
 */
export function buildSelectablePacks(
  catalog: DeviceSupportCatalog,
  machineInformation: MachineInformation
): DevicePack[] {
  const all = (catalog.devicePacks ?? [])
    .filter((pack) => pack !== null && pack !== undefined && pack.id.trim().length > 0)
    .filter((pack, index, array) => array.findIndex((other) => other.id.toLowerCase() === pack.id.toLowerCase()) === index)

  if (all.length === 0) return all

  const vendor = machineInformation.vendor ?? ''
  const model = machineInformation.model ?? ''

  const isVendorRelated = (pack: DevicePack): boolean => {
    const packVendor = pack.vendor ?? ''
    if (packVendor.trim().length === 0 || packVendor === '*') return false
    if (packVendor.toLowerCase() === vendor.toLowerCase()) return true
    if (
      packVendor.toLowerCase() === 'lenovo' &&
      vendor.toLowerCase().includes('lenovo')
    ) {
      return true
    }
    return pack.vendorAliases.some((alias) => {
      const a = alias.trim()
      if (a.length === 0) return false
      return (
        vendor.toLowerCase().includes(a.toLowerCase()) ||
        a.toLowerCase().includes(vendor.toLowerCase())
      )
    })
  }

  const isModelRelated = (pack: DevicePack): boolean =>
    pack.modelKeywords.some((keyword) => {
      const k = keyword.trim()
      return k.length > 0 && model.toLowerCase().includes(k.toLowerCase())
    })

  const related = all.filter((pack) => isVendorRelated(pack) || isModelRelated(pack))
  const rest = all.filter((pack) => !related.includes(pack))

  // Cap list size for usability: related first, then top hardware + popular basic.
  const hardwareRest = rest
    .filter((pack) => pack.enabledFeatures.some((feature) => feature.toLowerCase() === HARDWARE_CONTROLS_FEATURE))
    .slice(0, HARDWARE_REST_CAP)
  const basicRest = rest
    .filter((pack) => !pack.enabledFeatures.some((feature) => feature.toLowerCase() === HARDWARE_CONTROLS_FEATURE))
    .sort((a, b) => a.displayName.localeCompare(b.displayName, undefined, { sensitivity: 'base' }))
    .slice(0, BASIC_REST_CAP)

  return [...related, ...hardwareRest, ...basicRest]
    .filter((pack, index, array) => array.findIndex((other) => other.id.toLowerCase() === pack.id.toLowerCase()) === index)
}
