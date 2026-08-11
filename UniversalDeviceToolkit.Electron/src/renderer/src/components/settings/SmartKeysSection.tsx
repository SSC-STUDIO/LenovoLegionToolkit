import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useSettingsStore } from '../../stores/settingsStore'
import { SettingsCard } from './SettingsCard'
import SmartKeyPipelinesModal from './SmartKeyPipelinesModal'

const SMART_FN_LOCK_MODIFIERS: Array<{ flag: number; i18nKey: string }> = [
  { flag: 1, i18nKey: 'settings.power.modifierKeys.shift' },
  { flag: 2, i18nKey: 'settings.power.modifierKeys.ctrl' },
  { flag: 4, i18nKey: 'settings.power.modifierKeys.alt' }
]

export function SmartKeysSection(): React.JSX.Element {
  const { t } = useTranslation()
  const { scopes, load } = useSettingsStore()
  const [singlePressOpen, setSinglePressOpen] = useState(false)
  const [doublePressOpen, setDoublePressOpen] = useState(false)

  useEffect(() => {
    void load()
  }, [load])

  const app = (scopes.application ?? {}) as Record<string, unknown>
  const smartFnLockFlags = (app['SmartFnLockFlags'] as number | undefined) ?? 0
  const enabledModifiers = SMART_FN_LOCK_MODIFIERS.filter(
    (modifier) => (smartFnLockFlags & modifier.flag) !== 0
  )

  return (
    <div className="udt-settings-section udt-settings-section--smart-keys">
      <SettingsCard
        title={t('settings.smartKeys.smartFnLock')}
        description={t('settings.smartKeys.smartFnLockDesc')}
      >
        <div className="udt-settings-smart-fn">
          <span className="udt-settings-smart-fn__value">
            {enabledModifiers.length > 0
              ? enabledModifiers.map((modifier) => t(modifier.i18nKey)).join(' + ')
              : t('settings.smartKeys.off')}
          </span>
          <span className="udt-settings-smart-fn__hint">{t('settings.smartKeys.hint')}</span>
        </div>
      </SettingsCard>
      <SettingsCard
        title={t('settingsPagesmartKeySinglePressActiontitle')}
        description={t('settings.smartKeys.singlePressActionDesc')}
        onClick={() => setSinglePressOpen(true)}
      />
      <SettingsCard
        title={t('settingsPagesmartKeyDoublePressActiontitle')}
        description={t('settings.smartKeys.doublePressActionDesc')}
        onClick={() => setDoublePressOpen(true)}
      />
      <SmartKeyPipelinesModal
        open={singlePressOpen}
        onClose={() => setSinglePressOpen(false)}
        onSaved={() => void load()}
      />
      <SmartKeyPipelinesModal
        open={doublePressOpen}
        isDoublePress
        onClose={() => setDoublePressOpen(false)}
        onSaved={() => void load()}
      />
    </div>
  )
}
