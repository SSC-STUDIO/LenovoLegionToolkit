import { useEffect, useState } from 'react'
import { Modal, Spin, Switch, message } from 'antd'
import { useTranslation } from 'react-i18next'
import { featuresApi } from '../../api/features'
import { keyboardApi } from '../../api/keyboard'
import { settingsApi } from '../../api/settings'
import { useSettingsStore } from '../../stores/settingsStore'

/**
 * Parity modal for WPF Windows/Settings/NavigationItemsSettingsWindow:
 * show/hide the optional sidebar navigation items. Dashboard and Settings
 * are always visible and not listed here. The Keyboard entry is only shown
 * when the keyboard backlight is supported (like the WPF window).
 */

interface NavigationItemsModalProps {
  open: boolean
  onClose: () => void
}

const NAVIGATION_ITEMS: Array<{ key: string; titleKey: string; descKey: string }> = [
  {
    key: 'keyboard',
    titleKey: 'nav.keyboard',
    descKey: 'navigationItemsSettingsWindowshowKeyboardNavigationItemdescription'
  },
  {
    key: 'automation',
    titleKey: 'nav.automation',
    descKey: 'navigationItemsSettingsWindowshowAutomationNavigationItemdescription'
  },
  {
    key: 'macro',
    titleKey: 'nav.macro',
    descKey: 'navigationItemsSettingsWindowshowMacroNavigationItemdescription'
  },
  {
    key: 'windowsOptimization',
    titleKey: 'nav.windowsOptimization',
    descKey: 'navigationItemsSettingsWindowshowWindowsOptimizationNavigationItemdescription'
  },
  {
    key: 'pluginExtensions',
    titleKey: 'nav.pluginExtensions',
    descKey: 'navigationItemsSettingsWindowshowPluginExtensionsNavigationItemdescription'
  },
  {
    key: 'about',
    titleKey: 'nav.about',
    descKey: 'navigationItemsSettingsWindowshowAboutNavigationItemdescription'
  }
]

export default function NavigationItemsModal({
  open,
  onClose
}: NavigationItemsModalProps): React.JSX.Element {
  const { t } = useTranslation()
  const [loading, setLoading] = useState(true)
  const [visibility, setVisibility] = useState<Record<string, boolean>>({})
  const [keyboardSupported, setKeyboardSupported] = useState(false)

  useEffect(() => {
    if (!open) return
    let cancelled = false
    setLoading(true)
    void (async () => {
      try {
        const [settingsResult, keyboardMode, infos] = await Promise.all([
          settingsApi.get('application'),
          keyboardApi.detect().catch(() => null),
          featuresApi.list().catch(() => [])
        ])
        if (cancelled) return

        const store = (settingsResult.value ?? {}) as Record<string, unknown>
        const stored =
          (store.NavigationItemsVisibility as Record<string, boolean> | undefined) ?? {}
        const supported = keyboardMode?.mode != null && keyboardMode.mode !== 'none'
        const whiteKeyboardSupported = infos.some(
          (info) =>
            info.supported &&
            (info.key === 'whiteKeyboard' || info.key === 'oneLevelWhiteKeyboard')
        )
        setKeyboardSupported(supported || whiteKeyboardSupported)
        setVisibility(
          Object.fromEntries(
            NAVIGATION_ITEMS.map((item) => [item.key, stored[item.key] !== false])
          )
        )
      } catch (reason) {
        if (!cancelled) void message.error((reason as Error).message)
      } finally {
        if (!cancelled) setLoading(false)
      }
    })()
    return () => {
      cancelled = true
    }
  }, [open])

  const handleToggle = async (key: string, checked: boolean): Promise<void> => {
    const next = { ...visibility, [key]: checked }
    setVisibility(next)
    try {
      const result = await settingsApi.get('application')
      const current = (result.value ?? {}) as Record<string, unknown>
      const merged = {
        ...current,
        NavigationItemsVisibility: { ...((current.NavigationItemsVisibility as Record<string, boolean> | undefined) ?? {}), ...next }
      }
      useSettingsStore.getState().setScope('application', merged)
      await settingsApi.set('application', merged)
      await settingsApi.save(['application'])
    } catch (reason) {
      void message.error((reason as Error).message)
      setVisibility(visibility)
    }
  }

  return (
    <Modal
      open={open}
      title={t('navigationItemsSettingsWindowtitle')}
      width={500}
      footer={null}
      onCancel={onClose}
    >
      {loading ? (
        <div style={{ textAlign: 'center', padding: 24 }}>
          <Spin />
        </div>
      ) : (
        <div>
          <p style={{ opacity: 0.75 }}>{t('navigationItemsSettingsWindowdescription')}</p>
          {NAVIGATION_ITEMS.map((item) => {
            if (item.key === 'keyboard' && !keyboardSupported) return null
            return (
              <div
                key={item.key}
                style={{
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'space-between',
                  gap: 16,
                  padding: '10px 0',
                  borderBottom: '1px solid rgba(128,128,128,0.15)'
                }}
              >
                <div>
                  <div style={{ fontWeight: 600 }}>{t(item.titleKey)}</div>
                  <div style={{ opacity: 0.65, fontSize: 12 }}>{t(item.descKey)}</div>
                </div>
                <Switch
                  className="udt-settings-switch"
                  checked={visibility[item.key] === true}
                  onChange={(checked) => void handleToggle(item.key, checked)}
                />
              </div>
            )
          })}
        </div>
      )}
    </Modal>
  )
}
