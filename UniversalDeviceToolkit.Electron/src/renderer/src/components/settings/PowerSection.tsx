import { useEffect } from 'react'
import { Form, Select, Space, Switch, Typography, message } from 'antd'
import { useTranslation } from 'react-i18next'
import { settingsApi } from '../../api/settings'
import { useSettingsStore } from '../../stores/settingsStore'

type PowerModeMappingMode = 'Disabled' | 'WindowsPowerMode' | 'WindowsPowerPlan'

const POWER_MODE_MAPPING_OPTIONS: Array<{ value: PowerModeMappingMode; i18nKey: string }> = [
  { value: 'Disabled', i18nKey: 'settings.power.mappingModes.disabled' },
  { value: 'WindowsPowerMode', i18nKey: 'settings.power.mappingModes.windowsPowerMode' },
  { value: 'WindowsPowerPlan', i18nKey: 'settings.power.mappingModes.windowsPowerPlan' }
]

const POWER_MODE_STATE_KEYS: Record<string, string> = {
  Quiet: 'settings.power.powerModeStates.quiet',
  Balance: 'settings.power.powerModeStates.balance',
  Performance: 'settings.power.powerModeStates.performance',
  Extreme: 'settings.power.powerModeStates.extreme',
  GodMode: 'settings.power.powerModeStates.godMode'
}

const WINDOWS_POWER_MODE_KEYS: Record<string, string> = {
  BestPowerEfficiency: 'settings.power.windowsPowerModes.bestPowerEfficiency',
  Balanced: 'settings.power.windowsPowerModes.balanced',
  BestPerformance: 'settings.power.windowsPowerModes.bestPerformance'
}

const SMART_FN_LOCK_MODIFIERS: Array<{ flag: number; i18nKey: string }> = [
  { flag: 1, i18nKey: 'settings.power.modifierKeys.shift' },
  { flag: 2, i18nKey: 'settings.power.modifierKeys.ctrl' },
  { flag: 4, i18nKey: 'settings.power.modifierKeys.alt' }
]

export function PowerSection(): React.JSX.Element {
  const { t } = useTranslation()
  const { scopes, load, setScope } = useSettingsStore()

  useEffect(() => {
    void load()
  }, [load])

  const app = (scopes.application ?? {}) as Record<string, unknown>
  const powerModeMappingMode =
    (app['PowerModeMappingMode'] as PowerModeMappingMode | undefined) ?? 'WindowsPowerMode'
  const powerModes = (app['PowerModes'] ?? {}) as Record<string, string>
  const synchronizeBrightness = (app['SynchronizeBrightnessToAllPowerPlans'] as boolean | undefined) ?? false
  const smartFnLockFlags = (app['SmartFnLockFlags'] as number | undefined) ?? 0

  const persistApplication = async (patch: Record<string, unknown>): Promise<void> => {
    const current = (scopes.application ?? {}) as Record<string, unknown>
    const next = { ...current, ...patch }
    setScope('application', next)
    try {
      await settingsApi.set('application', next)
      await settingsApi.save(['application'])
      message.success(t('settings.saved'))
    } catch {
      message.error(t('settings.saveFailed'))
    }
  }

  return (
    <Form layout="vertical">
      <Form.Item label={t('settings.power.powerModeMapping')}>
        <Select
          style={{ maxWidth: 320 }}
          value={powerModeMappingMode}
          onChange={(value: string) => void persistApplication({ PowerModeMappingMode: value })}
          options={POWER_MODE_MAPPING_OPTIONS.map((option) => ({
            value: option.value,
            label: t(option.i18nKey)
          }))}
        />
      </Form.Item>

      <Form.Item label={t('settings.power.powerModes')}>
        <Typography.Paragraph type="secondary" style={{ marginBottom: 8 }}>
          {t('settings.power.powerModesHint')}
        </Typography.Paragraph>
        {Object.entries(powerModes).length === 0 ? (
          <Typography.Text type="secondary">{t('settings.power.powerModesEmpty')}</Typography.Text>
        ) : (
          <Space direction="vertical" size={4}>
            {Object.entries(powerModes).map(([state, mode]) => (
              <Typography.Text key={state}>
                {t(POWER_MODE_STATE_KEYS[state] ?? state)} → {t(WINDOWS_POWER_MODE_KEYS[mode] ?? mode)}
              </Typography.Text>
            ))}
          </Space>
        )}
      </Form.Item>

      <Form.Item label={t('settings.power.synchronizeBrightness')}>
        <Switch
          checked={synchronizeBrightness}
          onChange={(checked: boolean) =>
            void persistApplication({ SynchronizeBrightnessToAllPowerPlans: checked })
          }
        />
      </Form.Item>

      <Form.Item label={t('settings.power.smartFnLock')}>
        <Space direction="vertical">
          {SMART_FN_LOCK_MODIFIERS.map((modifier) => (
            <Switch
              key={modifier.flag}
              checked={(smartFnLockFlags & modifier.flag) !== 0}
              onChange={(checked: boolean) => {
                const next = checked ? smartFnLockFlags | modifier.flag : smartFnLockFlags & ~modifier.flag
                void persistApplication({ SmartFnLockFlags: next })
              }}
              checkedChildren={t(modifier.i18nKey)}
              unCheckedChildren={t(modifier.i18nKey)}
            />
          ))}
        </Space>
      </Form.Item>
    </Form>
  )
}
