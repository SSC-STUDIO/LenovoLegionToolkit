import type { FeatureInfo, FeatureKey } from '../../api/features'
import type { DashboardGroup, DashboardItem } from '../../api/dashboard'

export const DEFAULT_DASHBOARD_GROUPS: DashboardGroup[] = [
  {
    type: 'Power',
    items: [
      'PowerMode',
      'ItsMode',
      'BatteryMode',
      'BatteryNightChargeMode',
      'AlwaysOnUsb',
      'InstantBoot',
      'FlipToStart'
    ]
  },
  {
    type: 'Graphics',
    items: ['HybridMode', 'DiscreteGpu', 'OverclockDiscreteGpu']
  },
  {
    type: 'Display',
    items: ['Resolution', 'RefreshRate', 'DpiScale', 'Hdr', 'OverDrive', 'TurnOffMonitors']
  },
  {
    type: 'Other',
    items: [
      'Microphone',
      'WhiteKeyboardBacklight',
      'PanelLogoBacklight',
      'PortsBacklight',
      'TouchpadLock',
      'FnLock',
      'WinKeyLock'
    ]
  }
]

const FEATURE_CANDIDATES: Partial<Record<DashboardItem, readonly FeatureKey[]>> = {
  PowerMode: ['powerMode'],
  ItsMode: ['itsMode'],
  BatteryMode: ['battery'],
  BatteryNightChargeMode: ['batteryNightCharge'],
  AlwaysOnUsb: ['alwaysOnUsb'],
  InstantBoot: ['instantBoot'],
  FlipToStart: ['flipToStart'],
  HybridMode: ['hybridMode', 'igpuMode'],
  Resolution: ['resolution'],
  RefreshRate: ['refreshRate'],
  DpiScale: ['dpiScale'],
  Hdr: ['hdr'],
  OverDrive: ['overDrive'],
  Microphone: ['microphone'],
  WhiteKeyboardBacklight: ['whiteKeyboard', 'oneLevelWhiteKeyboard'],
  PanelLogoBacklight: ['panelLogo'],
  PortsBacklight: ['portsBacklight'],
  TouchpadLock: ['touchpadLock'],
  FnLock: ['fnLock'],
  WinKeyLock: ['winKey']
}

export type FeatureInfoMap = Partial<Record<FeatureKey, FeatureInfo>>

export function resolveDashboardFeature(
  item: DashboardItem,
  infos: FeatureInfoMap
): FeatureKey | null {
  const candidates = FEATURE_CANDIDATES[item]
  if (candidates == null) return null

  return candidates.find((candidate) => infos[candidate]?.supported === true) ?? null
}

export function isSpecialDashboardItem(item: DashboardItem): boolean {
  return item === 'DiscreteGpu' || item === 'OverclockDiscreteGpu' || item === 'TurnOffMonitors'
}
