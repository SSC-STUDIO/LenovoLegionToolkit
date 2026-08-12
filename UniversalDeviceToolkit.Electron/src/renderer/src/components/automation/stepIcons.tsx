import {
  AppstoreOutlined,
  AudioOutlined,
  BellOutlined,
  BulbOutlined,
  ClockCircleOutlined,
  CodeOutlined,
  ConsoleSqlOutlined,
  DashboardOutlined,
  DesktopOutlined,
  EyeInvisibleOutlined,
  FontSizeOutlined,
  HighlightOutlined,
  ImportOutlined,
  KeyOutlined,
  MonitorOutlined,
  MoonOutlined,
  PoweroffOutlined,
  RocketOutlined,
  SlidersOutlined,
  SoundOutlined,
  SunOutlined,
  SwapOutlined,
  TabletOutlined,
  ThunderboltOutlined,
  UsbOutlined,
  WifiOutlined
} from '@ant-design/icons'
import type { ReactNode } from 'react'

/**
 * Per-step icon mapping — mirrors the Electron step card controls' SymbolRegular
 * icons (RGBKeyboardBacklightAutomationStepControl → Keyboard24, Run → WindowConsole20, …).
 */
const STEP_ICONS: Record<string, ReactNode> = {
  alwaysOnUsb: <UsbOutlined />,
  battery: <ThunderboltOutlined />,
  batteryNightCharge: <MoonOutlined />,
  deactivateGPU: <DesktopOutlined />,
  delay: <ClockCircleOutlined />,
  displayBrightness: <HighlightOutlined />,
  dpiScale: <FontSizeOutlined />,
  flipToStart: <PoweroffOutlined />,
  fnLock: <KeyOutlined />,
  godModePreset: <SlidersOutlined />,
  hdr: <SunOutlined />,
  hideMainWindow: <EyeInvisibleOutlined />,
  hybridMode: <SwapOutlined />,
  instantBoot: <PoweroffOutlined />,
  macro: <CodeOutlined />,
  microphone: <AudioOutlined />,
  notification: <BellOutlined />,
  oneLevelWhiteKeyboardBacklight: <BulbOutlined />,
  osd: <AppstoreOutlined />,
  overclockDiscreteGPU: <ThunderboltOutlined />,
  overDrive: <RocketOutlined />,
  panelLogoBacklight: <BulbOutlined />,
  playSound: <SoundOutlined />,
  portsBacklight: <UsbOutlined />,
  powerMode: <DashboardOutlined />,
  quickAction: <RocketOutlined />,
  refreshRate: <ClockCircleOutlined />,
  resolution: <MonitorOutlined />,
  rgbKeyboardBacklight: <BulbOutlined />,
  run: <ConsoleSqlOutlined />,
  showMainWindow: <MonitorOutlined />,
  speaker: <SoundOutlined />,
  spectrumKeyboardBacklightBrightness: <BulbOutlined />,
  spectrumKeyboardBacklightImportProfile: <ImportOutlined />,
  spectrumKeyboardBacklightProfile: <BulbOutlined />,
  touchpadLock: <TabletOutlined />,
  turnOffMonitors: <MonitorOutlined />,
  turnOffWiFi: <WifiOutlined />,
  turnOnWiFi: <WifiOutlined />,
  whiteKeyboardBacklight: <BulbOutlined />,
  winKey: <KeyOutlined />
}

export function stepIcon($type: string): ReactNode | null {
  return STEP_ICONS[$type] ?? null
}
