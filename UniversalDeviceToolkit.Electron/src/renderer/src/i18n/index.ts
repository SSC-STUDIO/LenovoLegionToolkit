import i18n from 'i18next'
import { initReactI18next } from 'react-i18next'
import enUS from './locales/en-US'
import zhCN from './locales/zh-CN'
import zhHant from './locales/zh-Hant'
import ja from './locales/ja'
import de from './locales/de'
import fr from './locales/fr'
import es from './locales/es'
import it from './locales/it'
import ptBR from './locales/pt-BR'
import pt from './locales/pt'
import ru from './locales/ru'
import uk from './locales/uk'
import pl from './locales/pl'
import cs from './locales/cs'
import sk from './locales/sk'
import hu from './locales/hu'
import ro from './locales/ro'
import bg from './locales/bg'
import tr from './locales/tr'
import el from './locales/el'
import ar from './locales/ar'
import lv from './locales/lv'
import nlNL from './locales/nl-NL'
import vi from './locales/vi'
import uzLatnUZ from './locales/uz-Latn-UZ'
import { dashboardParityEnUS, dashboardParityZhCN } from './locales/dashboard-parity'

export interface LanguageOption {
  code: string
  /** Native name shown in the language picker. */
  name: string
}

/**
 * Selectable languages (25) — mirrors the WPF resx set. Every code here ships
 * with a full locale file; unknown legacy codes fall back to English.
 */
export const LANGUAGES: LanguageOption[] = [
  { code: 'en', name: 'English' },
  { code: 'zh-CN', name: '简体中文' },
  { code: 'zh-Hant', name: '繁體中文' },
  { code: 'ja', name: '日本語' },
  { code: 'de', name: 'Deutsch' },
  { code: 'fr', name: 'Français' },
  { code: 'es', name: 'Español' },
  { code: 'it', name: 'Italiano' },
  { code: 'pt-BR', name: 'Português (Brasil)' },
  { code: 'pt', name: 'Português' },
  { code: 'ru', name: 'Русский' },
  { code: 'uk', name: 'Українська' },
  { code: 'pl', name: 'Polski' },
  { code: 'cs', name: 'Čeština' },
  { code: 'sk', name: 'Slovenčina' },
  { code: 'hu', name: 'Magyar' },
  { code: 'ro', name: 'Română' },
  { code: 'bg', name: 'Български' },
  { code: 'tr', name: 'Türkçe' },
  { code: 'el', name: 'Ελληνικά' },
  { code: 'ar', name: 'العربية' },
  { code: 'lv', name: 'Latviešu' },
  { code: 'nl-NL', name: 'Nederlands' },
  { code: 'vi', name: 'Tiếng Việt' },
  { code: 'uz-Latn-UZ', name: "O'zbek" }
]

export const supportedLanguages: string[] = LANGUAGES.map((language) => language.code)
export type SupportedLanguage = (typeof supportedLanguages)[number]

const DEFAULT_LANGUAGE: SupportedLanguage = 'zh-CN'
const LANGUAGE_STORAGE_KEY = 'udt-language'
const LEGACY_LANGUAGE_STORAGE_KEY = 'udt.lang'

/**
 * Legacy language codes (persisted by older builds / the WPF app) mapped onto
 * the current registry.
 */
const LEGACY_ALIASES: Record<string, string> = {
  'en-US': 'en',
  'nl-NL': 'nl-NL',
  pt: 'pt',
  ko: 'en',
  'ko-KR': 'en'
}

function normalizeLanguage(lng: string): string {
  const normalized = LEGACY_ALIASES[lng] ?? lng
  return supportedLanguages.includes(normalized as SupportedLanguage) ? normalized : 'en'
}

function resolveInitialLanguage(): string {
  try {
    const stored =
      localStorage.getItem(LANGUAGE_STORAGE_KEY) ?? localStorage.getItem(LEGACY_LANGUAGE_STORAGE_KEY)
    if (stored != null) return normalizeLanguage(stored)
  } catch {
    /* ignore quota / private mode */
  }
  return DEFAULT_LANGUAGE
}

void i18n.use(initReactI18next).init({
  resources: {
    // 'en-US' stays registered so the legacy code path and fallbackLng keep
    // working; 'en' is the canonical code used by the UI.
    'en-US': {
      translation: {
        ...enUS.translation,
        ...dashboardParityEnUS
      }
    },
    en: {
      translation: {
        ...enUS.translation,
        ...dashboardParityEnUS
      }
    },
    'zh-CN': {
      translation: {
        ...zhCN.translation,
        ...dashboardParityZhCN
      }
    },
    'zh-Hant': { translation: zhHant.translation },
    ja: { translation: ja.translation },
    de: { translation: de.translation },
    fr: { translation: fr.translation },
    es: { translation: es.translation },
    it: { translation: it.translation },
    'pt-BR': { translation: ptBR.translation },
    pt: { translation: pt.translation },
    ru: { translation: ru.translation },
    uk: { translation: uk.translation },
    pl: { translation: pl.translation },
    cs: { translation: cs.translation },
    sk: { translation: sk.translation },
    hu: { translation: hu.translation },
    ro: { translation: ro.translation },
    bg: { translation: bg.translation },
    tr: { translation: tr.translation },
    el: { translation: el.translation },
    ar: { translation: ar.translation },
    lv: { translation: lv.translation },
    'nl-NL': { translation: nlNL.translation },
    vi: { translation: vi.translation },
    'uz-Latn-UZ': { translation: uzLatnUZ.translation }
  },
  lng: resolveInitialLanguage(),
  fallbackLng: 'en-US',
  interpolation: {
    escapeValue: false
  }
})

export async function changeLanguage(lng: string): Promise<void> {
  const normalized = normalizeLanguage(lng)
  await i18n.changeLanguage(normalized)
  try {
    localStorage.setItem(LANGUAGE_STORAGE_KEY, normalized)
  } catch {
    /* ignore quota / private mode */
  }
  window.bridge?.setTrayLanguage?.(normalized)
}

// Keep the main-process tray menu in sync with the active UI language.
try {
  window.bridge?.setTrayLanguage?.(resolveInitialLanguage())
} catch {
  /* bridge may be unavailable during unit tests */
}

export default i18n
