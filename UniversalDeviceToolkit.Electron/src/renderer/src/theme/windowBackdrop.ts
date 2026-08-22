import { settingsApi } from '../api/settings'

export type WindowBackdropStyle = 'Windows' | 'macOS' | 'Off'
type BackgroundMaterial = 'mica' | 'acrylic' | 'none'

const MATERIAL_BY_STYLE: Record<WindowBackdropStyle, BackgroundMaterial> = {
  Windows: 'mica',
  macOS: 'acrylic',
  Off: 'none'
}

/** Bridge exposes the platform (win32 | darwin | linux). */
const CURRENT_PLATFORM: string = window.bridge?.platform ?? 'win32'

export function normalizeWindowBackdropStyle(value: unknown): WindowBackdropStyle {
  return value === 'macOS' || value === 'Off' ? value : 'Windows'
}

export function applyWindowBackdrop(style: WindowBackdropStyle): void {
  const canUseNativeMaterial =
    typeof window.bridge?.setBackgroundMaterial === 'function' &&
    window.bridge.platform !== 'web'

  if (!canUseNativeMaterial) {
    document.documentElement.dataset.backdrop = 'none'
    return
  }

  if (CURRENT_PLATFORM === 'darwin') {
    document.documentElement.dataset.backdrop = MATERIAL_BY_STYLE[style] === 'none' ? 'none' : 'acrylic'
    return
  }

  // Electron backgroundMaterial (mica/acrylic) is a Windows DWM API. On Linux
  // the main window is an opaque #202020 surface (see createWindow); leaving
  // data-backdrop=mica/acrylic punches chrome transparent so light 跟随系统
  // paints white cards on a dark shell — the washed-out mixed theme.
  if (CURRENT_PLATFORM === 'linux') {
    document.documentElement.dataset.backdrop = 'none'
    void window.bridge?.setBackgroundMaterial?.('none')
    return
  }

  const material = MATERIAL_BY_STYLE[style]
  document.documentElement.dataset.backdrop = material
  void window.bridge?.setBackgroundMaterial(material)
}

export async function loadWindowBackdrop(): Promise<void> {
  const result = await settingsApi.get('application')
  const application = result.value as Record<string, unknown> | null
  applyWindowBackdrop(normalizeWindowBackdropStyle(application?.['WindowBackdropStyle']))
}
