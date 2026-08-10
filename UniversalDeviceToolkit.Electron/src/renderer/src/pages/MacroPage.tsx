import { Card, Typography } from 'antd'
import { useTranslation } from 'react-i18next'

export default function MacroPage(): React.JSX.Element {
  const { t } = useTranslation()
  return (
    <Card>
      <Typography.Title level={3}>{t('nav.macro')}</Typography.Title>
      <Typography.Text type="secondary">{t('pages.placeholder')}</Typography.Text>
    </Card>
  )
}
