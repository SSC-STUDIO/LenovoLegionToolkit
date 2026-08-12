import { useEffect, useMemo, useState } from 'react'
import { create } from 'zustand'
import { useTranslation } from 'react-i18next'
import { GlobalOutlined } from '@ant-design/icons'
import { changeLanguage, supportedLanguages } from '../../i18n'
import './utils.css'

/**
 * Port of Electron LanguageSelectorWindow: first-launch language gate. Lists the
 * available languages, applies the selection and returns the outcome.
 *
 * The Electron window downloads language packs through LanguagePackManager; the
 * Electron renderer ships its locale files bundled, so every listed language
 * is always "installed" and the download/install progress phase is not needed.
 * The failure actions (Retry / Continue in English / Exit) are still provided
 * through the outcome contract so a host-driven pack install can be added.
 */

export type LanguageGateOutcome = 'Continue' | 'ContinueEnglish' | 'Exit'

export interface LanguageSelectorResult {
  outcome: LanguageGateOutcome
  /** Selected culture (language code); 'en' for ContinueEnglish. */
  culture: string | null
}

export interface LanguageSelectorOptions {
  languages?: { code: string; displayName: string }[]
  defaultLanguage?: string
  allowOfflineEnglish?: boolean
}

interface LanguageSelectorRequest {
  id: number
  options: LanguageSelectorOptions
}

let requestSeq = 0
let pendingResolve: ((result: LanguageSelectorResult) => void) | null = null

interface LanguageSelectorState {
  request: LanguageSelectorRequest | null
  show: (options: LanguageSelectorOptions) => void
  settle: (result: LanguageSelectorResult) => void
}

const useLanguageSelectorStore = create<LanguageSelectorState>((set) => ({
  request: null,
  show: (options) => set({ request: { id: ++requestSeq, options } }),
  settle: (result) => {
    pendingResolve?.(result)
    pendingResolve = null
    set({ request: null })
  }
}))

export function openLanguageSelector(options: LanguageSelectorOptions): Promise<LanguageSelectorResult> {
  return new Promise((resolve) => {
    pendingResolve = resolve
    useLanguageSelectorStore.getState().show(options)
  })
}

const NATIVE_NAMES: Record<string, string> = {
  'zh-CN': '中文（简体）',
  'en-US': 'English'
}

export default function LanguageSelectorModalHost(): React.JSX.Element {
  const { t } = useTranslation()
  const request = useLanguageSelectorStore((s) => s.request)
  const settle = useLanguageSelectorStore((s) => s.settle)
  const [selected, setSelected] = useState<string>('')

  const languages = useMemo(() => {
    if (!request) return []
    const list = request.options.languages ?? supportedLanguages.map((code) => ({
      code,
      displayName: NATIVE_NAMES[code] ?? code
    }))
    return [...list].sort((a, b) => a.displayName.localeCompare(b.displayName, undefined, { sensitivity: 'base' }))
  }, [request])

  useEffect(() => {
    if (!request) return
    const preferred =
      languages.find((lang) => lang.code === request.options.defaultLanguage) ??
      languages.find((lang) => lang.code === 'zh-CN' || lang.code === 'en-US') ??
      languages[0]
    setSelected(preferred?.code ?? '')
  }, [request, languages])

  if (!request) return <></>

  const allowOfflineEnglish = request.options.allowOfflineEnglish === true

  const ok = async (): Promise<void> => {
    const code = selected
    if (!code) return
    const selectedLanguage = languages.find((lang) => lang.code === code)
    if (!selectedLanguage) return
    if (allowOfflineEnglish && !['zh-CN', 'en-US'].includes(code)) {
      settle({ outcome: 'ContinueEnglish', culture: 'en' })
      return
    }
    // Bundled locale: apply immediately (Electron would install the pack first).
    await changeLanguage(code).catch(() => undefined)
    settle({ outcome: 'Continue', culture: code })
  }

  const exit = (): void => {
    settle({ outcome: 'Exit', culture: null })
  }

  return (
    <div className="udt-utils-backdrop">
      <div
        className="udt-utils-modal"
        style={{ width: 460, maxWidth: 520, minHeight: 280 }}
        onClick={(event) => event.stopPropagation()}
      >
        <div className="udt-utils-modal__title">{t('app.name')}</div>
        <div className="udt-utils-modal__body">
          <div style={{ display: 'flex', gap: 16 }}>
            <GlobalOutlined style={{ fontSize: 40, color: 'var(--udt-text-secondary)' }} />
            <div style={{ flex: 1 }}>
              <div style={{ fontWeight: 500, marginBottom: 12 }}>
                {t('wpf.languageSelectorWindowselectLanguage')}
              </div>
              <select
                className="udt-utils-select"
                value={selected}
                onChange={(event) => setSelected(event.target.value)}
              >
                {languages.map((lang) => (
                  <option key={lang.code} value={lang.code}>
                    {lang.displayName}
                  </option>
                ))}
              </select>
              {allowOfflineEnglish && (
                <p className="udt-utils-status">{t('wpf.languageSelectorWindowsafeModeHint')}</p>
              )}
            </div>
          </div>
        </div>
        <div className="udt-utils-modal__actions">
          <button type="button" className="udt-utils-button" onClick={() => void exit()}>
            {t('wpf.exit')}
          </button>
          <button
            type="button"
            className="udt-utils-button udt-utils-button--primary"
            disabled={!selected}
            onClick={() => void ok()}
          >
            {t('wpf.continue')}
          </button>
        </div>
      </div>
    </div>
  )
}
