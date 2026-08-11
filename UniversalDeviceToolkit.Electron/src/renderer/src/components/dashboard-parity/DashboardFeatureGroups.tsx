import type { TFunction } from 'i18next'
import { useTranslation } from 'react-i18next'
import type { DashboardGroup, DashboardItem } from '../../api/dashboard'
import { useFeaturesStore } from '../../stores/featuresStore'
import DashboardFeatureCard from './DashboardFeatureCard'
import HybridModeCard from './HybridModeCard'
import { resolveDashboardFeature } from './dashboardItems'

function groupTitle(group: DashboardGroup, t: TFunction): string {
  if (group.type === 'Custom' && group.customName) return group.customName
  return t(`dashboard.group.${group.type.toLowerCase()}`, { defaultValue: group.type })
}

function RegularDashboardItem({ item }: { item: DashboardItem }): React.JSX.Element | null {
  const infos = useFeaturesStore((state) => state.infos)
  const feature = resolveDashboardFeature(item, infos)
  if (feature == null) return null
  if (feature === 'hybridMode') return <HybridModeCard />
  return <DashboardFeatureCard feature={feature} />
}

export default function DashboardFeatureGroups({ groups }: { groups: DashboardGroup[] }): React.JSX.Element {
  const { t } = useTranslation()

  return (
    <div className="udt-parity-feature-groups">
      {groups.map((group, index) => {
        const regularItems = group.items.filter((item) => resolveDashboardFeature(
          item,
          useFeaturesStore.getState().infos
        ) != null)
        if (regularItems.length === 0) return null

        return (
          <section key={`${group.type}-${index}`} className="udt-parity-feature-group">
            <h2>{groupTitle(group, t)}</h2>
            <div className="udt-parity-feature-group__items">
              {regularItems.map((item) => <RegularDashboardItem key={item} item={item} />)}
            </div>
          </section>
        )
      })}
    </div>
  )
}
