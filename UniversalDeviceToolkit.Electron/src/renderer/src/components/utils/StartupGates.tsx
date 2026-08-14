import { useEffect } from 'react'
import { waitForHostReady } from '../../api/bridge'
import { systemApi } from '../../api/system'
import { changeLanguage, LANGUAGES } from '../../i18n'
import { openLanguageSelector } from './LanguageSelectorModal'
import { openDeviceSetup } from './DeviceSetupModal'
import { openUnsupportedDevice } from './UnsupportedDeviceModal'
import type { SystemInfo } from '../../api/system'

/**
 * First-launch / startup gates — shown in the Electron app after install
 * (not in the NSIS file-copy wizard):
 *   1. Language gate (first run only; LanguageSelectorWindow)
 *   2. Device setup wizard (first run only; DeviceSetupWindow)
 *   3. Unsupported device warning (UnsupportedWindow)
 *
 * Completion of the language gate uses a dedicated marker. Writing the current
 * i18n language into `udt.lang` on mount used to skip the picker forever while
 * the UI was still on the English fallback.
 */

export const LANGUAGE_GATE_DONE_KEY = 'udt.language-gate-completed'
export const DEVICE_SETUP_STORAGE_KEY = 'udt.deviceSetup'

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
  useEffect(() => {
    let cancelled = false

    const run = async (): Promise<void> => {
      // 1. Language gate — first launch only, and it does not need the Host.
      if (localStorage.getItem(LANGUAGE_GATE_DONE_KEY) == null) {
        const result = await openLanguageSelector({
          languages: LANGUAGES.map((language) => ({
            code: language.code,
            displayName: language.name
          })),
          defaultLanguage: 'zh-CN'
        })
        if (cancelled) return
        if (result.outcome === 'Exit') {
          window.bridge?.quitApp?.()
          return
        }
        if (result.outcome === 'ContinueEnglish') {
          await changeLanguage('en').catch(() => undefined)
        } else if (result.culture) {
          await changeLanguage(result.culture).catch(() => undefined)
        }
        localStorage.setItem(LANGUAGE_GATE_DONE_KEY, '1')
      }

      if (cancelled) return

      // 2. Device setup wizard — first launch only. Wait for Host when we can,
      // but still show the wizard if the Host is slow or unavailable.
      let hostReady = false
      try {
        await waitForHostReady()
        hostReady = true
      } catch {
        hostReady = false
      }
      if (cancelled) return

      if (localStorage.getItem(DEVICE_SETUP_STORAGE_KEY) == null) {
        const info = await fetchSystemInfo(hostReady ? 6 : 1)
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

      if (cancelled || !hostReady) return

      // 3. Unsupported device warning.
      const info = await fetchSystemInfo()
      if (cancelled || info == null || info.isCompatible !== false) return

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
    }
  }, [])

  return <></>
}
