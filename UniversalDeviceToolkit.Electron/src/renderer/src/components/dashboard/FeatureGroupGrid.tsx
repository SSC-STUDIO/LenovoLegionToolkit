import { useTranslation } from 'react-i18next'
import type { TFunction } from 'i18next'
import type { FeatureKey } from '../../api/features'
import { useFeaturesStore } from '../../stores/featuresStore'
import FeatureCard from './FeatureCard'

export interface DashboardGroupConfig {
  type: string
  customName?: string
  items: string[]
}

const FEATURE_KEYS: readonly FeatureKey[] = [
  'alwaysOnUsb',
  'battery',
  'batteryNightCharge',
  'flipToStart',
  'fnLock',
  'gSync',
  'hdr',
  'hybridMode',
  'igpuMode',
  'itsMode',
  'instantBoot',
  'microphone',
  'overDrive',
  'panelLogo',
  'portsBacklight',
  'powerMode',
  'refreshRate',
  'resolution',
  'dpiScale',
  'speaker',
  'touchpadLock',
  'whiteKeyboard',
  'winKey',
  'oneLevelWhiteKeyboard'
]

function isFeatureKey(value: string): value is FeatureKey {
  return (FEATURE_KEYS as readonly string[]).includes(value)
}

function groupTitle(group: DashboardGroupConfig, t: TFunction): string {
  if (group.type === 'Custom' && group.customName) return group.customName
  const translated = t(`dashboard.group.${group.type.toLowerCase()}`, { defaultValue: '' })
  return translated !== '' ? translated : group.type
}

export default function FeatureGroupGrid({
  groups
}: {
  groups: DashboardGroupConfig[]
}): React.JSX.Element {
  const { t } = useTranslation()
  const infos = useFeaturesStore((s) => s.infos)

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
      {groups.map((group, index) => {
        const items = group.items.filter(
          (item) => isFeatureKey(item) && !(infos[item] != null && !infos[item].supported)
        )
        if (items.length === 0) return null
        return (
          <section key={`${group.type}-${index}`}>
            <h3 style={{ fontSize: 19, fontWeight: 500, margin: '0 0 12px' }}>
              {groupTitle(group, t)}
            </h3>
            <div
              style={{
                display: 'grid',
                gridTemplateColumns: 'repeat(auto-fill, minmax(240px, 1fr))',
                gap: 12
              }}
            >
              {items.map((item) => (
                <FeatureCard
                  key={item}
                  feature={item as FeatureKey}
                  title={t(`feature.${item}`, { defaultValue: '' }) || item}
                />
              ))}
            </div>
          </section>
        )
      })}
    </div>
  )
}
