import { useEffect } from 'react'
import { Form, Typography } from 'antd'
import { useTranslation } from 'react-i18next'
import { useSettingsStore } from '../../stores/settingsStore'

const SMART_FN_LOCK_MODIFIERS: Array<{ flag: number; i18nKey: string }> = [
  { flag: 1, i18nKey: 'settings.power.modifierKeys.shift' },
  { flag: 2, i18nKey: 'settings.power.modifierKeys.ctrl' },
  { flag: 4, i18nKey: 'settings.power.modifierKeys.alt' }
]

export function SmartKeysSection(): React.JSX.Element {
  const { t } = useTranslation()
  const { scopes, load } = useSettingsStore()

  useEffect(() => {
    void load()
  }, [load])

  const app = (scopes.application ?? {}) as Record<string, unknown>
  const smartFnLockFlags = (app['SmartFnLockFlags'] as number | undefined) ?? 0
  const enabledModifiers = SMART_FN_LOCK_MODIFIERS.filter((modifier) => (smartFnLockFlags & modifier.flag) !== 0)

  return (
    <div>
      <Typography.Title level={4}>{t('settings.smartKeys.title')}</Typography.Title>
      <Typography.Paragraph type="secondary">{t('settings.smartKeys.description')}</Typography.Paragraph>
      <Form layout="vertical">
        <Form.Item label={t('settings.smartKeys.smartFnLock')}>
          <Typography.Text>
            {enabledModifiers.length > 0
              ? enabledModifiers.map((modifier) => t(modifier.i18nKey)).join(' + ')
              : t('settings.smartKeys.off')}
          </Typography.Text>
        </Form.Item>
        <Typography.Text type="secondary">{t('settings.smartKeys.hint')}</Typography.Text>
      </Form>
    </div>
  )
}
