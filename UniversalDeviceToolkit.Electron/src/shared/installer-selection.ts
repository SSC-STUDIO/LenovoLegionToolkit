/**
 * Values exchanged between the NSIS setup pages and the Electron processes.
 * Keep this module platform-neutral so both the main and preload bundles can
 * validate installer input without sharing filesystem or Electron code.
 */
export const INSTALLER_LANGUAGES = [
  'en',
  'zh-CN',
  'zh-Hant',
  'ja',
  'de',
  'fr',
  'es',
  'it',
  'pt-BR',
  'pt',
  'ru',
  'uk',
  'pl',
  'cs',
  'sk',
  'hu',
  'ro',
  'bg',
  'tr',
  'el',
  'ar',
  'lv',
  'nl-NL',
  'vi',
  'uz-Latn-UZ'
] as const

export type InstallerLanguage = (typeof INSTALLER_LANGUAGES)[number]
export type InstallerDeviceMode = 'auto' | 'basic'

export interface InstallerSelection {
  language: InstallerLanguage
  deviceMode: InstallerDeviceMode
}

export function normalizeInstallerLanguage(value: string | undefined): InstallerLanguage | null {
  if (value == null) return null
  return (INSTALLER_LANGUAGES as readonly string[]).includes(value)
    ? (value as InstallerLanguage)
    : null
}

export function normalizeInstallerDeviceMode(value: string | undefined): InstallerDeviceMode | null {
  if (value === 'auto' || value === 'basic') return value
  return null
}

/** Parse only the two explicit, validated switches passed by Electron main. */
export function parseInstallerSelectionArguments(args: readonly string[]): InstallerSelection | null {
  let language: InstallerLanguage | null = null
  let deviceMode: InstallerDeviceMode | null = null

  for (const argument of args) {
    const languagePrefix = '--udt-installer-language='
    const deviceModePrefix = '--udt-installer-device-mode='
    if (argument.startsWith(languagePrefix)) {
      language = normalizeInstallerLanguage(argument.slice(languagePrefix.length))
    } else if (argument.startsWith(deviceModePrefix)) {
      deviceMode = normalizeInstallerDeviceMode(argument.slice(deviceModePrefix.length))
    }
  }

  if (language == null || deviceMode == null) return null
  return { language, deviceMode }
}
