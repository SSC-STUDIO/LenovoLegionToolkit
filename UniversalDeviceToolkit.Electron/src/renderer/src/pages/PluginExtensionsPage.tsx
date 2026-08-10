import { Card, Typography } from 'antd'
import { useTranslation } from 'react-i18next'

export default function PluginExtensionsPage(): React.JSX.Element {
  const { t } = useTranslation()
  return (
    <Card>
      <Typography.Title level={3}>{t('nav.pluginExtensions')}</Typography.Title>
      <Typography.Text type="secondary">{t('pages.placeholder')}</Typography.Text>
    </Card>
  )
}
