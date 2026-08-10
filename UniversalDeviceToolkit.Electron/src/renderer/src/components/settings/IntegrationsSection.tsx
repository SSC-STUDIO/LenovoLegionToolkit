import { useEffect } from 'react'
import { Form, Switch, message } from 'antd'
import { useTranslation } from 'react-i18next'
import { settingsApi } from '../../api/settings'
import { useSettingsStore } from '../../stores/settingsStore'

export function IntegrationsSection(): React.JSX.Element {
  const { t } = useTranslation()
  const { scopes, load, setScope } = useSettingsStore()

  useEffect(() => {
    void load()
  }, [load])

  const integrations = (scopes.integrations ?? {}) as Record<string, unknown>
  const hwinfoEnabled = (integrations['HWiNFO'] as boolean | undefined) ?? false
  const cliEnabled = (integrations['CLI'] as boolean | undefined) ?? false

  const persistIntegrations = async (patch: Record<string, unknown>): Promise<void> => {
    const current = (scopes.integrations ?? {}) as Record<string, unknown>
    const next = { ...current, ...patch }
    setScope('integrations', next)
    try {
      await settingsApi.set('integrations', next)
      await settingsApi.save(['integrations'])
      message.success(t('settings.saved'))
    } catch {
      message.error(t('settings.saveFailed'))
    }
  }

  return (
    <Form layout="vertical">
      <Form.Item label={t('settings.integrations.hwinfo')}>
        <Switch
          checked={hwinfoEnabled}
          onChange={(checked: boolean) => void persistIntegrations({ HWiNFO: checked })}
        />
      </Form.Item>

      <Form.Item label={t('settings.integrations.cli')}>
        <Switch
          checked={cliEnabled}
          onChange={(checked: boolean) => void persistIntegrations({ CLI: checked })}
        />
      </Form.Item>
    </Form>
  )
}
