import { Select, Switch } from 'antd'
import type { TFunction } from 'i18next'
import {
  BatteryCharge24Regular,
  DesktopPulse24Regular,
  Gauge24Regular,
  Hdr24Regular,
  Keyboard24Regular,
  LeafOne24Regular,
  LightbulbCircle24Regular,
  Mic24Regular,
  PlugDisconnected24Regular,
  Power24Regular,
  ScaleFill24Regular,
  Tablet24Regular,
  TextFontSize24Regular,
  TopSpeed24Regular,
  UsbPlug24Regular,
  UsbStick24Regular,
  WeatherMoon24Regular
} from '@fluentui/react-icons'
import { useTranslation } from 'react-i18next'
import type { FeatureKey } from '../../api/features'
import { useFeature } from '../../hooks/useFeature'

const TOGGLE_FEATURES = new Set<FeatureKey>([
  'batteryNightCharge',
  'flipToStart',
  'fnLock',
  'gSync',
  'hdr',
  'microphone',
  'overDrive',
  'panelLogo',
  'portsBacklight',
  'touchpadLock',
  'winKey',
  'oneLevelWhiteKeyboard'
])

const HIDE_SINGLE_OPTION_FEATURES = new Set<FeatureKey>(['resolution', 'refreshRate', 'dpiScale'])

function FeatureIcon({ feature }: { feature: FeatureKey }): React.JSX.Element {
  const icon = (() => {
    switch (feature) {
      case 'powerMode':
      case 'itsMode':
        return <Gauge24Regular />
      case 'battery':
        return <BatteryCharge24Regular />
      case 'batteryNightCharge':
        return <WeatherMoon24Regular />
      case 'alwaysOnUsb':
        return <UsbStick24Regular />
      case 'instantBoot':
        return <PlugDisconnected24Regular />
      case 'flipToStart':
        return <Power24Regular />
      case 'hybridMode':
      case 'igpuMode':
        return <LeafOne24Regular />
      case 'resolution':
        return <ScaleFill24Regular />
      case 'refreshRate':
        return <DesktopPulse24Regular />
      case 'dpiScale':
        return <TextFontSize24Regular />
      case 'hdr':
        return <Hdr24Regular />
      case 'overDrive':
        return <TopSpeed24Regular />
      case 'panelLogo':
        return <LightbulbCircle24Regular />
      case 'portsBacklight':
        return <UsbPlug24Regular />
      case 'microphone':
        return <Mic24Regular />
      case 'touchpadLock':
        return <Tablet24Regular />
      case 'fnLock':
      case 'whiteKeyboard':
      case 'oneLevelWhiteKeyboard':
      case 'winKey':
        return <Keyboard24Regular />
      default:
        return <Gauge24Regular />
    }
  })()

  return <span className="udt-parity-feature-card__icon" aria-hidden="true">{icon}</span>
}

function stateKey(value: string): string {
  return value.charAt(0).toLowerCase() + value.slice(1)
}

function labelForStringState(feature: FeatureKey, value: string, t: TFunction): string {
  if (feature === 'powerMode') {
    return t(`feature.powerModeOptions.${stateKey(value)}`, { defaultValue: value })
  }
  if (feature === 'battery') {
    return t(`feature.batteryModes.${stateKey(value)}`, { defaultValue: value })
  }
  return t(`dashboardFeatureState.${stateKey(value)}`, { defaultValue: value })
}

function wireKey(value: unknown): string {
  return typeof value === 'string' ? value : JSON.stringify(value)
}

function labelForState(feature: FeatureKey, value: unknown, t: TFunction): string {
  if (typeof value === 'string') return labelForStringState(feature, value, t)
  if (value == null || typeof value !== 'object') return String(value ?? '')

  const record = value as Record<string, unknown>
  if (typeof record.Width === 'number' && typeof record.Height === 'number') {
    return `${record.Width} x ${record.Height}`
  }
  if (typeof record.Frequency === 'number') return `${record.Frequency} Hz`
  if (typeof record.Scale === 'number') return `${record.Scale}%`
  return JSON.stringify(value)
}

function isOnState(value: unknown): boolean {
  return typeof value === 'string' && value.toLowerCase() === 'on'
}

export default function DashboardFeatureCard({ feature }: { feature: FeatureKey }): React.JSX.Element | null {
  const { t } = useTranslation()
  const { supported, state, states, loading, error, setState } = useFeature(feature)

  if (!supported) return null
  if (HIDE_SINGLE_OPTION_FEATURES.has(feature) && states.length < 2) return null

  const title = t(`feature.${feature}`, { defaultValue: feature })
  const description = t(`feature.${feature}.desc`, { defaultValue: '' })
  const isToggle = TOGGLE_FEATURES.has(feature)

  return (
    <article className="udt-parity-feature-card">
      <div className="udt-parity-feature-card__body">
        <FeatureIcon feature={feature} />
        <div className="udt-parity-feature-card__copy">
          <div className="udt-parity-feature-card__title" title={title}>{title}</div>
          {description !== '' && (
            <div className="udt-parity-feature-card__description" title={description}>{description}</div>
          )}
          {error != null && <div className="udt-parity-feature-card__warning" title={error}>{error}</div>}
        </div>
        <div className="udt-parity-feature-card__accessory">
          {isToggle ? (
            <Switch
              aria-label={title}
              checked={isOnState(state)}
              disabled={loading || error != null}
              loading={loading}
              onChange={(checked) => void setState(checked ? 'On' : 'Off')}
            />
          ) : (
            <Select
              aria-label={title}
              className="udt-parity-feature-card__select"
              disabled={loading || error != null || states.length === 0}
              loading={loading}
              value={state == null ? undefined : wireKey(state)}
              options={states.map((value) => ({
                value: wireKey(value),
                label: labelForState(feature, value, t),
                wireValue: value
              }))}
              optionRender={(option) => option.label}
              onChange={(key) => {
                const next = states.find((candidate) => wireKey(candidate) === key)
                if (next !== undefined) void setState(next)
              }}
            />
          )}
        </div>
      </div>
    </article>
  )
}
