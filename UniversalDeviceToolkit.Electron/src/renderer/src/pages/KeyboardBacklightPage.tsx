import { Card, Typography } from 'antd'
import { useTranslation } from 'react-i18next'

export default function KeyboardBacklightPage(): React.JSX.Element {
  const { t } = useTranslation()
  return (
    <Card>
      <Typography.Title level={3}>{t('nav.keyboardBacklight')}</Typography.Title>
      <Typography.Text type="secondary">{t('pages.placeholder')}</Typography.Text>
    </Card>
  )
}
