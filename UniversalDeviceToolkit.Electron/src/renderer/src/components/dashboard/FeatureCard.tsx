import { Alert, Button, Card, Flex, Select, Switch, message, theme } from 'antd'
import { SettingOutlined } from '@ant-design/icons'
import { useTranslation } from 'react-i18next'
import type { FeatureKey } from '../../api/features'
import { useFeature } from '../../hooks/useFeature'

export interface FeatureCardProps {
  feature: FeatureKey
  title: string
}

const CONFIG_KEYS: readonly FeatureKey[] = ['powerMode', 'itsMode']

export default function FeatureCard({
  feature,
  title
}: FeatureCardProps): React.JSX.Element | null {
  const { t } = useTranslation()
  const { token } = theme.useToken()
  const { supported, state, states, loading, error, setState } = useFeature(feature)

  if (!supported) return null

  const desc = t(`feature.${feature}.desc`, { defaultValue: '' })
  const isToggle = states.length > 0 && states.every((value) => typeof value === 'boolean')
  const isStringStates = states.length > 0 && typeof states[0] === 'string'
  const currentValue = typeof state === 'string' ? state : JSON.stringify(state)
  const showsConfig = CONFIG_KEYS.includes(feature)

  const accessory = isToggle ? (
    <Switch
      checked={Boolean(state)}
      disabled={error != null}
      loading={loading}
      onChange={(checked) => {
        void setState(checked)
      }}
    />
  ) : (
    <Flex gap={4} align="center">
      <Select
        size="small"
        style={{ width: 150 }}
        value={currentValue}
        options={isStringStates ? (states as string[]).map((value) => ({ value })) : undefined}
        disabled={!isStringStates || error != null}
        loading={loading}
        onChange={(value) => {
          void setState(value)
        }}
      />
      {showsConfig && (
        <Button
          type="text"
          size="small"
          aria-label={t('dashboard.card.config')}
          icon={<SettingOutlined />}
          onClick={() => {
            message.info(t('dashboard.card.configComingSoon'))
          }}
        />
      )}
    </Flex>
  )

  return (
    <Card
      size="small"
      style={{
        height: '100%',
        borderRadius: 18,
        background: '#303030',
        border: '1px solid rgba(255, 255, 255, 0.12)',
        overflow: 'hidden'
      }}
      styles={{ body: { padding: 12, borderRadius: 18 } }}
    >
      <Flex gap={10} align="center">
        <div
          style={{
            width: 32,
            height: 32,
            borderRadius: 8,
            background: 'rgba(255, 255, 255, 0.08)',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            flexShrink: 0,
            fontSize: 14,
            fontWeight: 600,
            color: token.colorText
          }}
        >
          {title.charAt(0)}
        </div>
        <Flex vertical flex="1" style={{ minWidth: 0 }}>
          <div style={{ fontSize: 15, fontWeight: 600, lineHeight: 1.4, color: token.colorText }}>
            {title}
          </div>
          {desc !== '' && (
            <div
              style={{
                fontSize: 13,
                lineHeight: 1.4,
                marginTop: 2,
                color: token.colorTextSecondary
              }}
            >
              {desc}
            </div>
          )}
        </Flex>
        {accessory}
      </Flex>
      {error != null && (
        <Alert
          type="error"
          showIcon
          message={t('dashboard.card.error')}
          description={error}
          style={{ marginTop: 8, paddingTop: 8, paddingBottom: 8 }}
        />
      )}
    </Card>
  )
}
