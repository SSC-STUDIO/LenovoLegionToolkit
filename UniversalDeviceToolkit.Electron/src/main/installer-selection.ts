import { existsSync, readFileSync } from 'fs'
import { join } from 'path'
import {
  normalizeInstallerDeviceMode,
  normalizeInstallerLanguage,
  type InstallerSelection
} from '../shared/installer-selection'

export const INSTALLER_SELECTION_FILE_NAME = 'installer-selection.ini'

function installerSelectionPath(): string {
  // The file lives beside the installed resources directory. Keeping it beside
  // the application makes the selection follow a custom install directory and
  // avoids requiring the renderer to read or write a machine-wide settings ACL.
  return join(process.resourcesPath, '..', INSTALLER_SELECTION_FILE_NAME)
}

/** Parse the small INI file written by the per-machine NSIS installer. */
export function parseInstallerSelectionIni(contents: string): InstallerSelection | null {
  let section = ''
  let language: string | undefined
  let deviceMode: string | undefined

  for (const rawLine of contents.split(/\r?\n/)) {
    const line = rawLine.trim()
    if (line.length === 0 || line.startsWith(';') || line.startsWith('#')) continue
    if (line.startsWith('[') && line.endsWith(']')) {
      section = line.slice(1, -1).trim().toLowerCase()
      continue
    }
    if (section !== 'installation') continue
    const separator = line.indexOf('=')
    if (separator < 0) continue
    const key = line.slice(0, separator).trim().toLowerCase()
    const value = line.slice(separator + 1).trim()
    if (key === 'language') language = value
    if (key === 'devicemode') deviceMode = value
  }

  const normalizedLanguage = normalizeInstallerLanguage(language)
  const normalizedDeviceMode = normalizeInstallerDeviceMode(deviceMode)
  if (normalizedLanguage == null || normalizedDeviceMode == null) return null
  return { language: normalizedLanguage, deviceMode: normalizedDeviceMode }
}

export function readInstallerSelection(): InstallerSelection | null {
  const path = installerSelectionPath()
  try {
    if (!existsSync(path)) return null
    return parseInstallerSelectionIni(readFileSync(path, 'utf8'))
  } catch (error) {
    console.warn(`[main] unable to read installer selection from ${path}:`, error)
    return null
  }
}

export function buildInstallerRendererArguments(selection: InstallerSelection): string[] {
  return [
    `--udt-installer-language=${selection.language}`,
    `--udt-installer-device-mode=${selection.deviceMode}`
  ]
}

export function buildInstallerHostArguments(selection: InstallerSelection | null): string[] {
  return selection?.deviceMode === 'basic' ? ['--no-hardware'] : []
}
