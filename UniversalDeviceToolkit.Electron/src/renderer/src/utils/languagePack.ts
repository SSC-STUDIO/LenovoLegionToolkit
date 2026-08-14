/**
 * Language pack models & catalog helpers — port of Electron
 * Utils/LanguagePackModels.cs (models) + Utils/LanguagePackManager.cs
 * (installed-pack enumeration, culture metadata).
 */

export type LanguagePackFailureKind =
  | 'Cancelled'
  | 'CatalogUnavailable'
  | 'CultureNotInCatalog'
  | 'AppVersionTooOld'
  | 'DownloadFailed'
  | 'HashMismatch'
  | 'CorruptPackage'
  | 'ValidationFailed'
  | 'ApplyFailed'
  | 'Unknown'

export class LanguagePackError extends Error {
  readonly kind: LanguagePackFailureKind
  readonly culture: string | null

  constructor(kind: LanguagePackFailureKind, message: string, culture: string | null = null) {
    super(message)
    this.name = 'LanguagePackError'
    this.kind = kind
    this.culture = culture
  }
}

export type LanguageGateOutcome = 'Continue' | 'ContinueEnglish' | 'Exit'

export interface LanguagePackCatalogEntry {
  culture: string
  parent: string | null
  size: number
  sha256: string
  resourceVersion: string | null
  minAppVersion: string | null
  url: string
  displayName: string
}

export interface InstalledLanguagePack {
  culture: string
  displayName: string
  /** locale files embedded in this app are always available. */
  builtIn: boolean
}

/** Native display name for a culture (for example zh-Hans -> Chinese Simplified). */
export function cultureDisplayName(culture: string): string {
  try {
    return new Intl.DisplayNames(['en'], { type: 'language' }).of(culture) ?? culture
  } catch {
    return culture
  }
}

/** BCP-47 normalization used when matching catalog entries. */
export function normalizeCulture(culture: string): string {
  return culture.replace(/_/g, '-').toLowerCase()
}

/** Maps a failure kind to a user-facing i18n key (wpf namespace or fallback). */
export function languagePackFailureKey(kind: LanguagePackFailureKind): string {
  switch (kind) {
    case 'Cancelled':
      return 'languagePackCancelled'
    case 'CatalogUnavailable':
      return 'languagePackCatalogUnavailable'
    case 'CultureNotInCatalog':
      return 'languagePackCultureNotInCatalog'
    case 'AppVersionTooOld':
      return 'languagePackAppVersionTooOld'
    case 'DownloadFailed':
      return 'languagePackDownloadFailed'
    case 'HashMismatch':
      return 'languagePackHashMismatch'
    case 'CorruptPackage':
      return 'languagePackCorruptPackage'
    case 'ValidationFailed':
      return 'languagePackValidationFailed'
    case 'ApplyFailed':
      return 'languagePackApplyFailed'
    default:
      return 'languagePackUnknown'
  }
}
