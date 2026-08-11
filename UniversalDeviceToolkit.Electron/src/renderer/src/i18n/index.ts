import i18n from 'i18next'
import { initReactI18next } from 'react-i18next'
import enUS from './locales/en-US'
import zhCN from './locales/zh-CN'
import { dashboardParityEnUS, dashboardParityZhCN } from './locales/dashboard-parity'

export const supportedLanguages = ['zh-CN', 'en-US'] as const
export type SupportedLanguage = (typeof supportedLanguages)[number]

const DEFAULT_LANGUAGE: SupportedLanguage = 'zh-CN'
const LANGUAGE_STORAGE_KEY = 'udt.lang'

function resolveInitialLanguage(): SupportedLanguage {
  const stored = localStorage.getItem(LANGUAGE_STORAGE_KEY)
  if (stored === 'zh-CN' || stored === 'en-US') return stored
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
    }
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
