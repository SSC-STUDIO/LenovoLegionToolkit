import {
  AppstoreOutlined,
  ClockCircleOutlined,
  DashboardOutlined,
  DesktopOutlined,
  DisconnectOutlined,
  FundOutlined,
  HighlightOutlined,
  HourglassOutlined,
  LockOutlined,
  PlayCircleOutlined,
  PoweroffOutlined,
  RetweetOutlined,
  SwapOutlined,
  ThunderboltOutlined,
  UnlockOutlined,
  UsbOutlined,
  WifiOutlined
} from '@ant-design/icons'
import type { ReactNode } from 'react'

/**
 * Per-trigger icon mapping — port of WPF
 * Extensions/AutomationPipelineTriggerExtensions.cs (SymbolRegular → antd icon).
 */

const TRIGGER_ICONS: Record<string, ReactNode> = {
  powerState: <ThunderboltOutlined />,
  powerMode: <DashboardOutlined />,
  godModePresetChanged: <DashboardOutlined />,
  game: <PlayCircleOutlined />,
  hdr: <HighlightOutlined />,
  processes: <DesktopOutlined />,
  userInactivity: <ClockCircleOutlined />,
  sessionLock: <LockOutlined />,
  sessionUnlock: <UnlockOutlined />,
  time: <HourglassOutlined />,
  device: <UsbOutlined />,
  nativeWindowsMessage: <AppstoreOutlined />,
  onStartup: <ThunderboltOutlined />,
  onResume: <PoweroffOutlined />,
  wiFiConnected: <WifiOutlined />,
  wiFiDisconnected: <DisconnectOutlined />,
  periodic: <RetweetOutlined />,
  hardwareSensor: <FundOutlined />,
  batteryPercentage: <PoweroffOutlined />,
  composite: <SwapOutlined />
}

/** "WiFiConnectedAutomationPipelineTrigger" → "wiFiConnected"; returns null when unknown. */
export function normalizeTriggerType($type: string): string | null {
  const trimmed = $type
    .replace(/AutomationPipelineTrigger$/i, '')
    .replace(/PipelineTrigger$/i, '')
    .replace(/Trigger$/i, '')
  if (!trimmed) return null
  const first = trimmed.charAt(0).toLowerCase() + trimmed.slice(1)
  return Object.prototype.hasOwnProperty.call(TRIGGER_ICONS, first) ? first : null
}

/** Icon for a serialized trigger $type; null when the type is unknown. */
export function triggerIcon($type: string): ReactNode | null {
  const key = normalizeTriggerType($type)
  return key === null ? null : (TRIGGER_ICONS[key] ?? null)
}
