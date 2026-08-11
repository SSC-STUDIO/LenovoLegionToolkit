/**
 * Main-process tray strings (mirrors WPF Resource.* used by TrayHelper).
 * Full renderer i18n lives in the renderer; tray only needs these labels.
 */

export interface TrayStrings {
  dashboard: string
  keyboard: string
  automation: string
  macro: string
  windowsOptimization: string
  open: string
  close: string
  unnamed: string
  deactivateGpu: string
}

const ZH_CN: TrayStrings = {
  dashboard: '控制台',
  keyboard: '键盘',
  automation: '自动化',
  macro: '自定义宏',
  windowsOptimization: '系统优化',
  open: '打开',
  close: '关闭',
  unnamed: '未命名',
  deactivateGpu: '停用 GPU'
}

const EN_US: TrayStrings = {
  dashboard: 'Dashboard',
  keyboard: 'Keyboard',
  automation: 'Actions',
  macro: 'Macro',
  windowsOptimization: 'System optimization',
  open: 'Open',
  close: 'Close',
  unnamed: 'Unnamed',
  deactivateGpu: 'Deactivate GPU'
}

const ZH_HANT: TrayStrings = {
  ...ZH_CN,
  dashboard: '控制台',
  macro: '自訂巨集',
  windowsOptimization: '系統最佳化',
  open: '開啟',
  close: '關閉',
  unnamed: '未命名',
  deactivateGpu: '強制休眠獨顯'
}

const BY_LANG: Record<string, TrayStrings> = {
  'zh-CN': ZH_CN,
  'zh-Hans': ZH_CN,
  zh: ZH_CN,
  'zh-Hant': ZH_HANT,
  'zh-TW': ZH_HANT,
  'zh-HK': ZH_HANT,
  'en-US': EN_US,
  en: EN_US
}

const DEACTIVATE_GPU_STABLE = '__udt.quickAction.deactivateGpu'

let currentLang = 'zh-CN'

export function setTrayLanguage(lang: string | null | undefined): void {
  if (!lang || typeof lang !== 'string') return
  currentLang = lang
}

export function getTrayLanguage(): string {
  return currentLang
}

export function trayStrings(): TrayStrings {
  if (BY_LANG[currentLang]) return BY_LANG[currentLang]
  const base = currentLang.split('-')[0] ?? currentLang
  return BY_LANG[base] ?? (base === 'zh' ? ZH_CN : EN_US)
}

/** Port of PipelineNameLocalizer.LocalizeStoredName for the default GPU quick action. */
export function localizePipelineName(storedName: string | null | undefined): string {
  const s = trayStrings()
  if (!storedName || storedName.trim().length === 0) return s.unnamed
  if (storedName === DEACTIVATE_GPU_STABLE) return s.deactivateGpu
  // Legacy baked Chinese/English titles still resolve to the localized title.
  if (
    storedName === '停用 GPU' ||
    storedName === 'Deactivate GPU' ||
    storedName === '強制休眠獨顯' ||
    storedName === '停用GPU'
  ) {
    return s.deactivateGpu
  }
  return storedName
}

/**
 * OSD item labels (mirrors the WPF OsdItem localization: Resource.OsdItem_*).
 * Group headers (FPS/CPU/GPU/RAM/PCH) are fixed English in both clients.
 */
export type OsdItemName =
  | 'Fps'
  | 'LowFps'
  | 'FrameTime'
  | 'CpuFrequency'
  | 'CpuPCoreFrequency'
  | 'CpuECoreFrequency'
  | 'CpuUtilization'
  | 'CpuTemperature'
  | 'CpuPower'
  | 'CpuFan'
  | 'GpuFrequency'
  | 'GpuUtilization'
  | 'GpuTemperature'
  | 'GpuVramUtilization'
  | 'GpuVramTemperature'
  | 'GpuPower'
  | 'GpuFan'
  | 'MemoryUtilization'
  | 'MemoryTemperature'
  | 'Disk1Temperature'
  | 'Disk2Temperature'
  | 'PchTemperature'
  | 'PchFan'

type OsdItemLabels = Record<OsdItemName, string>

const OSD_ITEM_LABELS_EN: OsdItemLabels = {
  Fps: 'FPS',
  LowFps: '1% Low',
  FrameTime: 'Frame Time',
  CpuFrequency: 'Frequency',
  CpuPCoreFrequency: 'P-Core Clock',
  CpuECoreFrequency: 'E-Core Clock',
  CpuUtilization: 'Utilization',
  CpuTemperature: 'Temperature',
  CpuPower: 'Power',
  CpuFan: 'Fan',
  GpuFrequency: 'Frequency',
  GpuUtilization: 'Utilization',
  GpuTemperature: 'Core Temp',
  GpuVramUtilization: 'VRAM Utilization',
  GpuVramTemperature: 'VRAM Temperature',
  GpuPower: 'Power',
  GpuFan: 'Fan',
  MemoryUtilization: 'Utilization',
  MemoryTemperature: 'Memory Temperature',
  Disk1Temperature: 'Disk 1 Temperature',
  Disk2Temperature: 'Disk 2 Temperature',
  PchTemperature: 'Motherboard Temperature',
  PchFan: 'Fan'
}

const OSD_ITEM_LABELS_ZH: OsdItemLabels = {
  Fps: 'FPS',
  LowFps: '1% Low',
  FrameTime: '帧耗时',
  CpuFrequency: '频率',
  CpuPCoreFrequency: 'P 核频率',
  CpuECoreFrequency: 'E 核频率',
  CpuUtilization: '利用率',
  CpuTemperature: '温度',
  CpuPower: '功耗',
  CpuFan: '风扇',
  GpuFrequency: '频率',
  GpuUtilization: '利用率',
  GpuTemperature: '核心温度',
  GpuVramUtilization: '显存占用率',
  GpuVramTemperature: '显存温度',
  GpuPower: '功耗',
  GpuFan: '风扇',
  MemoryUtilization: '利用率',
  MemoryTemperature: '内存温度',
  Disk1Temperature: '磁盘 1 温度',
  Disk2Temperature: '磁盘 2 温度',
  PchTemperature: '主板温度',
  PchFan: '风扇'
}

const OSD_BY_LANG: Record<string, OsdItemLabels> = {
  'zh-CN': OSD_ITEM_LABELS_ZH,
  zh: OSD_ITEM_LABELS_ZH,
  'zh-Hant': OSD_ITEM_LABELS_ZH,
  'en-US': OSD_ITEM_LABELS_EN,
  en: OSD_ITEM_LABELS_EN
}

export function osdItemLabels(): OsdItemLabels {
  const base = currentLang.split('-')[0] ?? currentLang
  return OSD_BY_LANG[base] ?? OSD_BY_LANG[currentLang] ?? OSD_ITEM_LABELS_EN
}
