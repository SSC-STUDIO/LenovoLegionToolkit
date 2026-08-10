import { Alert, Card, Select, Typography } from 'antd'
import { useTranslation } from 'react-i18next'
import type { ReactNode } from 'react'
import type { FeatureKey } from '../../api/features'
import { useFeature } from '../../hooks/useFeature'

export interface FeatureCardProps {
  feature: FeatureKey
  title: string
  description?: string
  icon?: ReactNode
}

export default function FeatureCard({
  feature,
  title,
  description,
  icon
}: FeatureCardProps): React.JSX.Element | null {
  const { t } = useTranslation()
  const { supported, state, states, loading, error, setState } = useFeature(feature)

  if (!supported) return null

  const isStringStates = states.length > 0 && typeof states[0] === 'string'
  const currentValue = typeof state === 'string' ? state : JSON.stringify(state)

  const select = (
    <Select
      size="small"
      style={{ width: '100%' }}
      value={currentValue}
      options={isStringStates ? (states as string[]).map((value) => ({ value })) : undefined}
      disabled={!isStringStates || error != null}
      loading={loading}
      onChange={(value) => {
        void setState(value)
      }}
    />
  )

  return (
    <Card size="small" title={title} extra={icon} style={{ width: 240 }}>
      {description != null && (
        <Typography.Text
          type="secondary"
          style={{ display: 'block', fontSize: 12, marginBottom: 8 }}
        >
          {description}
        </Typography.Text>
      )}
      {select}
      {error != null && (
        <Alert
          type="error"
          showIcon
          message={t('dashboard.card.error')}
          description={error}
          style={{ marginTop: 8 }}
        />
      )}
    </Card>
  )
}
