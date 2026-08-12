import { screen } from 'electron'
import { isRemoteSession } from './remote-session'

/**
 * Mirrors Electron RenderingCompatibilityHelper: decides whether the client must
 * fall back to software rendering (remote sessions, forced flag, missing or
 * invalid displays) and maps the backdrop style onto the Electron
 * background-material surface.
 *
 * Call after `app.whenReady()`; the screen queries throw before that and are
 * treated as conservative "force compatibility" like the Electron catch-all.
 */

export interface BackdropSurfaceOpacities {
  shell: number
  content: number
  card: number
}

export type BackdropStyle = 'Windows' | 'macOS' | 'Off'

export function shouldForceSoftwareRendering(forceSoftwareRendering?: boolean): boolean {
  try {
    if (forceSoftwareRendering === true) return true

    if (isRemoteSession()) return true

    const displays = screen.getAllDisplays()
    if (displays.length === 0) return true

    const primary = screen.getPrimaryDisplay()
    if (!(primary.size.width > 0 && primary.size.height > 0)) return true
  } catch {
    return true
  }

  return false
}

/** Mirrors RenderingCompatibilityHelper.GetPreferredBackgroundType. */
export function getPreferredBackgroundType(
  style: BackdropStyle | null | undefined,
  forceSoftwareRendering?: boolean
): 'none' | 'mica' | 'acrylic' {
  if (shouldDisableBackdrop(style, forceSoftwareRendering)) return 'none'
  return style === 'macOS' ? 'acrylic' : 'mica'
}

export function shouldDisableBackdrop(
  style: BackdropStyle | null | undefined,
  forceSoftwareRendering?: boolean
): boolean {
  return shouldForceSoftwareRendering(forceSoftwareRendering) || style === 'Off'
}

/** Mirrors RenderingCompatibilityHelper.GetBackdropSurfaceOpacities. */
export function getBackdropSurfaceOpacities(style: BackdropStyle | null | undefined, forceSoftwareRendering?: boolean): BackdropSurfaceOpacities {
  const active = !shouldDisableBackdrop(style, forceSoftwareRendering)
  if (!active) return { shell: 1.0, content: 1.0, card: 1.0 }

  // The native material belongs to shell chrome only. The page surface stays
  // opaque so content remains stable while switching applications.
  return style === 'macOS'
    ? { shell: 0.08, content: 1.0, card: 1.0 }
    : { shell: 0.18, content: 1.0, card: 1.0 }
}
