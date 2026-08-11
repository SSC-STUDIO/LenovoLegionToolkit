import { useEffect, useState } from 'react'
import { Alert, Button, Select, Switch, message } from 'antd'
import { useTranslation } from 'react-i18next'
import { updateApi } from '../../api/update'
import { settingsApi } from '../../api/settings'
import { useSettingsStore } from '../../stores/settingsStore'
import { openUpdateModal } from '../utils/UpdateModal'
import { SettingsCard } from './SettingsCard'

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
  const [checking, setChecking] = useState(false)
  const [checkResult, setCheckResult] = useState<{
    available: boolean
    version?: string | null
    error?: string | null
  } | null>(null)

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

  const handleCheckForUpdates = async (): Promise<void> => {
    setChecking(true)
    setCheckResult(null)
    try {
      const result = await updateApi.check(true)
      setCheckResult(result)
      if (result.error) {
        message.error(result.error)
      } else if (!result.available) {
        message.info(t('update.checkResult.latest'))
      }
    } catch (error) {
      message.error((error as Error).message)
    } finally {
      setChecking(false)
    }
  }

  return (
    <div className="udt-settings-section udt-settings-section--update">
      <SettingsCard
        title={t('settings.update.frequency')}
        action={
          <Select<string>
            className="udt-settings-select"
            value={updateCheckFrequency}
            onChange={(value) => void persistUpdateCheck({ UpdateCheckFrequency: value })}
            options={UPDATE_CHECK_FREQUENCIES.map((option) => ({
              value: option.value,
              label: t(option.i18nKey)
            }))}
          />
        }
      />
      <SettingsCard
        title={t('settings.update.includePrerelease')}
        description={t('settings.update.includePrereleaseDesc')}
        action={
          <Switch
            className="udt-settings-switch"
            checked={includePrereleaseUpdates}
            onChange={(checked) => void persistUpdateCheck({ IncludePrereleaseUpdates: checked })}
          />
        }
      />
      <SettingsCard title={t('settings.update.check')}>
        <Button
          type="primary"
          className="udt-settings-check-button"
          onClick={() => void handleCheckForUpdates()}
          loading={checking}
        >
          {t('settings.update.check')}
        </Button>
        {checkResult?.available && (
          <Alert
            className="udt-settings-card__alert"
            type="success"
            showIcon
            message={t('update.checkResult.available', { version: checkResult.version ?? '' })}
            action={
              <Button size="small" type="primary" onClick={() => void openUpdateModal({ version: checkResult.version ?? null })}>
                {t('wpf.update')}
              </Button>
            }
          />
        )}
      </SettingsCard>
    </div>
  )
}
