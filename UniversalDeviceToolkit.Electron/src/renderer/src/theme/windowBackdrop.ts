import { settingsApi } from '../api/settings'

export type WindowBackdropStyle = 'Windows' | 'macOS' | 'Off'
type BackgroundMaterial = 'mica' | 'acrylic' | 'none'

const MATERIAL_BY_STYLE: Record<WindowBackdropStyle, BackgroundMaterial> = {
  Windows: 'mica',
  macOS: 'acrylic',
  Off: 'none'
}

export function normalizeWindowBackdropStyle(value: unknown): WindowBackdropStyle {
  return value === 'macOS' || value === 'Off' ? value : 'Windows'
}

export function applyWindowBackdrop(style: WindowBackdropStyle): void {
  const material = MATERIAL_BY_STYLE[style]
  document.documentElement.dataset.backdrop = material
  void window.bridge?.setBackgroundMaterial(material)
}

export async function loadWindowBackdrop(): Promise<void> {
  const result = await settingsApi.get('application')
  const application = result.value as Record<string, unknown> | null
  applyWindowBackdrop(normalizeWindowBackdropStyle(application?.['WindowBackdropStyle']))
}
