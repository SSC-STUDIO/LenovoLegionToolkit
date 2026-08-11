import { useEffect } from 'react'
import { useTranslation } from 'react-i18next'
import { on } from '../../api/bridge'
import { settingsApi } from '../../api/settings'
import { systemApi } from '../../api/system'
import { changeLanguage } from '../../i18n'
import { openLanguageSelector } from './LanguageSelectorModal'
import { openDeviceSetup } from './DeviceSetupModal'
import { openUnsupportedDevice } from './UnsupportedDeviceModal'
import type { SystemInfo } from '../../api/system'

/**
 * First-launch / startup gates — port of the WPF startup sequence:
 *   1. Language gate (first run only; LanguageSelectorWindow)
 *   2. Device setup wizard (first run only; DeviceSetupWindow)
 *   3. Unsupported device warning (unless disabled in settings; UnsupportedWindow)
 *
 * The state markers are kept in localStorage because the renderer owns the
 * language choice; the WPF host-side `device-setup` state file has no bridge
 * equivalent yet. Every gate resolves to "proceed" unless the user explicitly
 * exits the application (app:quit).
 */

const LANGUAGE_STORAGE_KEY = 'udt.lang'
const DEVICE_SETUP_STORAGE_KEY = 'udt.deviceSetup'

async function waitForHost(timeoutMs = 15000): Promise<void> {
  const hostReady = new Promise<void>((resolve) => {
    const unsubscribe = on('host.ready', () => {
      unsubscribe()
      resolve()
    })
  })
  await Promise.race([
    hostReady,
    new Promise<void>((resolve) => window.setTimeout(resolve, timeoutMs))
  ])
}

async function fetchSystemInfo(retries = 6): Promise<SystemInfo | null> {
  for (let attempt = 0; attempt < retries; attempt++) {
    try {
      return await systemApi.info()
    } catch {
      await new Promise((resolve) => window.setTimeout(resolve, 1000))
    }
  }
  return null
}

export default function StartupGates(): React.JSX.Element {
  const { i18n } = useTranslation()

  useEffect(() => {
    let cancelled = false

    // Persist the chosen language so the first-launch gate does not repeat
    // (the settings page also changes the language through i18n directly).
    const markLanguage = (lng: string): void => {
      if (lng) localStorage.setItem(LANGUAGE_STORAGE_KEY, lng)
    }
    i18n.on('languageChanged', markLanguage)
    if (i18n.language) markLanguage(i18n.language)

    const run = async (): Promise<void> => {
      await waitForHost()

      if (cancelled) return

      // 1. Language gate — only on the very first launch.
      if (localStorage.getItem(LANGUAGE_STORAGE_KEY) == null) {
        const result = await openLanguageSelector({
          defaultLanguage: i18n.language.startsWith('zh') ? 'zh-CN' : 'en-US'
        })
        if (cancelled) return
        if (result.outcome === 'Exit') {
          window.bridge?.quitApp?.()
          return
        }
        if (result.outcome === 'ContinueEnglish') {
          await changeLanguage('en-US').catch(() => undefined)
        } else if (result.culture) {
          await changeLanguage(result.culture).catch(() => undefined)
        }
        // Explicit marker: changeLanguage() with the current language is a
        // no-op and would not fire languageChanged.
        localStorage.setItem(LANGUAGE_STORAGE_KEY, result.culture ?? 'en-US')
      }

      if (cancelled) return

      // 2. Device setup wizard — first launch only.
      if (localStorage.getItem(DEVICE_SETUP_STORAGE_KEY) == null) {
        const info = await fetchSystemInfo()
        if (cancelled) return
        const result = await openDeviceSetup({
          machineInformation: {
            vendor: info?.vendor ?? null,
            model: info?.model ?? null,
            machineType: info?.machineType ?? null
          },
          isBasicMode: true
        })
        if (cancelled) return
        localStorage.setItem(
          DEVICE_SETUP_STORAGE_KEY,
          JSON.stringify({ packId: result.devicePackId, isBasicMode: result.isBasicMode })
        )
      }

      if (cancelled) return

      // 3. Unsupported device warning.
      const info = await fetchSystemInfo()
      if (cancelled || info == null || info.isCompatible !== false) return

      let warningDisabled = false
      try {
        const result = await settingsApi.get('application')
        const app = (result.value ?? {}) as Record<string, unknown>
        warningDisabled = app['DisableUnsupportedHardwareWarning'] === true
      } catch {
        // Treat as enabled when the settings cannot be read.
      }
      if (cancelled || warningDisabled) return

      const shouldContinue = await openUnsupportedDevice({
        vendor: info.vendor ?? null,
        model: info.model ?? null,
        machineType: info.machineType ?? null
      })
      if (cancelled) return
      if (!shouldContinue) {
        window.bridge?.quitApp?.()
      }
    }

    void run()

    return () => {
      cancelled = true
      i18n.off('languageChanged', markLanguage)
    }
    // Run the sequence once per app session; the gates are idempotent.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  return <></>
}

/** Testing helper: reset the first-launch markers. */
export function resetStartupGates(): void {
  localStorage.removeItem(LANGUAGE_STORAGE_KEY)
  localStorage.removeItem(DEVICE_SETUP_STORAGE_KEY)
}
