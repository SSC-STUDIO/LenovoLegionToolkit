import { Alert, Button, Card, Flex, Select, Switch, message } from 'antd'
import {
  AppstoreOutlined,
  BulbOutlined,
  DesktopOutlined,
  PoweroffOutlined,
  SettingOutlined,
  SoundOutlined,
  ThunderboltOutlined,
  UsbOutlined
} from '@ant-design/icons'
import { useTranslation } from 'react-i18next'
import type { FeatureKey } from '../../api/features'
import { useFeature } from '../../hooks/useFeature'

export interface FeatureCardProps {
  feature: FeatureKey
  title: string
}

const CONFIG_KEYS: readonly FeatureKey[] = ['powerMode', 'itsMode']

function FeatureIcon({ feature }: { feature: FeatureKey }): React.JSX.Element {
  const icon = (() => {
    switch (feature) {
      case 'powerMode':
      case 'itsMode':
        return <ThunderboltOutlined />
      case 'battery':
      case 'batteryNightCharge':
        return <ThunderboltOutlined />
      case 'alwaysOnUsb':
      case 'portsBacklight':
        return <UsbOutlined />
      case 'speaker':
      case 'microphone':
        return <SoundOutlined />
      case 'panelLogo':
      case 'whiteKeyboard':
      case 'oneLevelWhiteKeyboard':
        return <BulbOutlined />
      case 'refreshRate':
      case 'resolution':
      case 'dpiScale':
      case 'gSync':
      case 'hdr':
        return <DesktopOutlined />
      case 'instantBoot':
      case 'flipToStart':
        return <PoweroffOutlined />
      default:
        return <AppstoreOutlined />
    }
  })()

  return <span className="udt-feature-card__icon" aria-hidden="true">{icon}</span>
}

export default function FeatureCard({
  feature,
  title
}: FeatureCardProps): React.JSX.Element | null {
  const { t } = useTranslation()
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
        className="udt-feature-card__select"
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
      className="udt-feature-card"
      styles={{ body: { padding: 0 } }}
    >
      <div className="udt-feature-card__content">
        <FeatureIcon feature={feature} />
        <Flex vertical flex="1" className="udt-feature-card__copy">
          <div className="udt-feature-card__title" title={title}>{title}</div>
          {desc !== '' && <div className="udt-feature-card__description" title={desc}>{desc}</div>}
        </Flex>
        <div className="udt-feature-card__accessory">{accessory}</div>
      </div>
      {error != null && (
        <Alert
          type="error"
          showIcon
          message={t('dashboard.card.error')}
          description={error}
          className="udt-feature-card__error"
        />
      )}
    </Card>
  )
}
