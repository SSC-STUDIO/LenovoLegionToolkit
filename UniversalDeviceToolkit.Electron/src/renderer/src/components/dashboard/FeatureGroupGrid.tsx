import { Card, Flex } from 'antd'
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
    <div>
      {groups.map((group, index) => {
        const items = group.items.filter(
          (item) => isFeatureKey(item) && !(infos[item] != null && !infos[item].supported)
        )
        if (items.length === 0) return null
        return (
          <Card
            key={`${group.type}-${index}`}
            title={groupTitle(group, t)}
            style={{ marginBottom: 16 }}
          >
            <Flex wrap gap={16}>
              {items.map((item) => (
                <FeatureCard
                  key={item}
                  feature={item as FeatureKey}
                  title={t(`feature.${item}`, { defaultValue: '' }) || item}
                />
              ))}
            </Flex>
          </Card>
        )
      })}
    </div>
  )
}
