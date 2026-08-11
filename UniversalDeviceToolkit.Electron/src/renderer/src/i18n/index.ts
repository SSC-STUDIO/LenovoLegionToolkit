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

export const supportedLanguages = [
  'zh-CN',
  'en-US',
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
export type SupportedLanguage = (typeof supportedLanguages)[number]

const DEFAULT_LANGUAGE: SupportedLanguage = 'zh-CN'
const LANGUAGE_STORAGE_KEY = 'udt.lang'

function resolveInitialLanguage(): SupportedLanguage {
  const stored = localStorage.getItem(LANGUAGE_STORAGE_KEY)
  if (supportedLanguages.includes(stored as SupportedLanguage)) return stored as SupportedLanguage
  return DEFAULT_LANGUAGE
}

void i18n.use(initReactI18next).init({
  resources: {
    'zh-CN': {
      translation: {
        ...zhCN.translation,
        ...dashboardParityZhCN
      }
    },
    'en-US': {
      translation: {
        ...enUS.translation,
        ...dashboardParityEnUS
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
  await i18n.changeLanguage(lng)
}

export default i18n
