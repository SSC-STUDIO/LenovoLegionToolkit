import { useEffect } from 'react'
import { Form, Select, Space, Switch, Typography, message } from 'antd'
import { useTranslation } from 'react-i18next'
import { settingsApi } from '../../api/settings'
import { useSettingsStore } from '../../stores/settingsStore'
import WindowBackdropSetting from './WindowBackdropSetting'

const NAVIGATION_ITEMS: Array<{ key: string; i18nKey: string }> = [
  { key: 'keyboard', i18nKey: 'settings.display.navigationKeys.keyboard' },
  { key: 'battery', i18nKey: 'settings.display.navigationKeys.battery' },
  { key: 'automation', i18nKey: 'settings.display.navigationKeys.automation' },
  { key: 'macro', i18nKey: 'settings.display.navigationKeys.macro' },
  { key: 'windowsOptimization', i18nKey: 'settings.display.navigationKeys.windowsOptimization' },
  { key: 'pluginExtensions', i18nKey: 'settings.display.navigationKeys.pluginExtensions' },
  { key: 'about', i18nKey: 'settings.display.navigationKeys.about' }
]

const NOTIFICATION_POSITIONS: Array<{ value: string; i18nKey: string }> = [
  { value: 'BottomRight', i18nKey: 'settings.display.notificationPositions.bottomRight' },
  { value: 'BottomCenter', i18nKey: 'settings.display.notificationPositions.bottomCenter' },
  { value: 'BottomLeft', i18nKey: 'settings.display.notificationPositions.bottomLeft' },
  { value: 'CenterLeft', i18nKey: 'settings.display.notificationPositions.centerLeft' },
  { value: 'TopLeft', i18nKey: 'settings.display.notificationPositions.topLeft' },
  { value: 'TopCenter', i18nKey: 'settings.display.notificationPositions.topCenter' },
  { value: 'TopRight', i18nKey: 'settings.display.notificationPositions.topRight' },
  { value: 'CenterRight', i18nKey: 'settings.display.notificationPositions.centerRight' },
  { value: 'Center', i18nKey: 'settings.display.notificationPositions.center' }
]

const NOTIFICATION_DURATIONS: Array<{ value: string; i18nKey: string }> = [
  { value: 'Short', i18nKey: 'settings.display.notificationDurations.short' },
  { value: 'Normal', i18nKey: 'settings.display.notificationDurations.normal' },
  { value: 'Long', i18nKey: 'settings.display.notificationDurations.long' }
]

export function DisplaySection(): React.JSX.Element {
  const { t } = useTranslation()
  const { scopes, load, setScope } = useSettingsStore()

  useEffect(() => {
    void load()
  }, [load])

  const app = (scopes.application ?? {}) as Record<string, unknown>
  const navigationItemsVisibility = (app['NavigationItemsVisibility'] ?? {}) as Record<string, boolean>
  const notificationPosition = (app['NotificationPosition'] as string | undefined) ?? 'BottomRight'
  const notificationDuration = (app['NotificationDuration'] as string | undefined) ?? 'Normal'
  const excludedRefreshRates = (app['ExcludedRefreshRates'] ?? []) as Array<{ Frequency: number }>

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

  const toggleNavigationItem = (key: string, checked: boolean): void => {
    const next = { ...navigationItemsVisibility, [key]: checked }
    void persistApplication({ NavigationItemsVisibility: next })
  }

  return (
    <Form layout="vertical">
      <Form.Item label={t('settings.display.navigationItems')}>
        <Space direction="vertical">
          {NAVIGATION_ITEMS.map((item) => (
            <Switch
              key={item.key}
              checked={navigationItemsVisibility[item.key] ?? true}
              onChange={(checked: boolean) => toggleNavigationItem(item.key, checked)}
              checkedChildren={t(item.i18nKey)}
              unCheckedChildren={t(item.i18nKey)}
            />
          ))}
        </Space>
      </Form.Item>

      <WindowBackdropSetting application={app} persist={(patch) => void persistApplication(patch)} />

      <Form.Item label={t('settings.display.notificationPosition')}>
        <Select
          style={{ maxWidth: 320 }}
          value={notificationPosition}
          onChange={(value: string) => void persistApplication({ NotificationPosition: value })}
          options={NOTIFICATION_POSITIONS.map((option) => ({
            value: option.value,
            label: t(option.i18nKey)
          }))}
        />
      </Form.Item>

      <Form.Item label={t('settings.display.notificationDuration')}>
        <Select
          style={{ maxWidth: 320 }}
          value={notificationDuration}
          onChange={(value: string) => void persistApplication({ NotificationDuration: value })}
          options={NOTIFICATION_DURATIONS.map((option) => ({
            value: option.value,
            label: t(option.i18nKey)
          }))}
        />
      </Form.Item>

      <Form.Item label={t('settings.display.excludedRefreshRates')}>
        {excludedRefreshRates.length === 0 ? (
          <Typography.Text type="secondary">{t('settings.display.excludedRefreshRatesEmpty')}</Typography.Text>
        ) : (
          <Space direction="vertical" size={4}>
            {excludedRefreshRates.map((rate) => (
              <Typography.Text key={rate.Frequency}>{rate.Frequency}Hz</Typography.Text>
            ))}
          </Space>
        )}
        <Typography.Paragraph type="secondary" style={{ marginTop: 8, marginBottom: 0 }}>
          {t('settings.display.excludedRefreshRatesHint')}
        </Typography.Paragraph>
      </Form.Item>
    </Form>
  )
}
