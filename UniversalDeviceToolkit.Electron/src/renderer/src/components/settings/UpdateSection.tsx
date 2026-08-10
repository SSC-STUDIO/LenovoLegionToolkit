import { useEffect } from 'react'
import { Button, Form, Select, Switch, message } from 'antd'
import { useTranslation } from 'react-i18next'
import { settingsApi } from '../../api/settings'
import { useSettingsStore } from '../../stores/settingsStore'

const UPDATE_CHECK_FREQUENCIES: Array<{ value: string; i18nKey: string }> = [
  { value: 'PerHour', i18nKey: 'settings.update.frequencies.perHour' },
  { value: 'PerThreeHours', i18nKey: 'settings.update.frequencies.perThreeHours' },
  { value: 'PerTwelveHours', i18nKey: 'settings.update.frequencies.perTwelveHours' },
  { value: 'PerDay', i18nKey: 'settings.update.frequencies.perDay' },
  { value: 'PerWeek', i18nKey: 'settings.update.frequencies.perWeek' },
  { value: 'PerMonth', i18nKey: 'settings.update.frequencies.perMonth' }
]

export function UpdateSection(): React.JSX.Element {
  const { t } = useTranslation()
  const { scopes, load, setScope } = useSettingsStore()

  useEffect(() => {
    void load()
  }, [load])

  const updateCheck = (scopes.updateCheck ?? {}) as Record<string, unknown>
  const updateCheckFrequency = (updateCheck['UpdateCheckFrequency'] as string | undefined) ?? 'PerDay'
  const includePrereleaseUpdates = (updateCheck['IncludePrereleaseUpdates'] as boolean | undefined) ?? false

  const persistUpdateCheck = async (patch: Record<string, unknown>): Promise<void> => {
    const current = (scopes.updateCheck ?? {}) as Record<string, unknown>
    const next = { ...current, ...patch }
    setScope('updateCheck', next)
    try {
      await settingsApi.set('updateCheck', next)
      await settingsApi.save(['updateCheck'])
      message.success(t('settings.saved'))
    } catch {
      message.error(t('settings.saveFailed'))
    }
  }

  const handleCheckForUpdates = (): void => {
    message.info(t('settings.update.comingSoon'))
  }

  return (
    <Form layout="vertical">
      <Form.Item label={t('settings.update.frequency')}>
        <Select
          style={{ maxWidth: 320 }}
          value={updateCheckFrequency}
          onChange={(value: string) => void persistUpdateCheck({ UpdateCheckFrequency: value })}
          options={UPDATE_CHECK_FREQUENCIES.map((option) => ({
            value: option.value,
            label: t(option.i18nKey)
          }))}
        />
      </Form.Item>

      <Form.Item label={t('settings.update.includePrerelease')}>
        <Switch
          checked={includePrereleaseUpdates}
          onChange={(checked: boolean) => void persistUpdateCheck({ IncludePrereleaseUpdates: checked })}
        />
      </Form.Item>

      <Form.Item>
        <Button onClick={handleCheckForUpdates}>{t('settings.update.check')}</Button>
      </Form.Item>
    </Form>
  )
}
