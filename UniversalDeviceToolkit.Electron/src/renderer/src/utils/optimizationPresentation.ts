import type {
  NetworkAccelerationConfig,
  NetworkAccelerationMode,
  NetworkAccelerationStatus,
  OptimizationActionDefinition,
  OptimizationCategoryDefinition
} from '../api/optimization'

export type OptimizationTabKey =
  | 'optimization'
  | 'cleanup'
  | 'driverDownload'
  | 'networkAcceleration'

export const NETWORK_ACCELERATION_MODES = [
  'Off',
  'SystemProxy',
  'Hosts',
  'DiagnosticsOnly'
] as const satisfies readonly NetworkAccelerationMode[]

export interface ActionSelectionPresentation {
  checked: boolean
  indeterminate: boolean
}

export function getActionSelectionPresentation(
  action: OptimizationActionDefinition,
  selected: boolean
): ActionSelectionPresentation {
  return {
    checked: action.applied === true || selected,
    indeterminate: action.applied === null
  }
}

export function collectRecommendedActionKeys(
  categories: readonly OptimizationCategoryDefinition[],
  predicate: (categoryKey: string) => boolean
): string[] {
  return categories
    .filter((category) => predicate(category.key))
    .flatMap((category) => category.actions)
    .filter((action) => action.recommended && action.applied !== true)
    .map((action) => action.key)
}

export function getNetworkSelectedTargetCount(
  config: NetworkAccelerationConfig | null
): number {
  if (!config) return 0
  return config.domainGroups.reduce((sum, group) => {
    if (!group.enabled) return sum
    const direct = (group.domains ?? []).filter((domain) => domain.trim().length > 0).length
    const subItems = (group.subItems ?? []).filter((subItem) => subItem.enabled).length
    return sum + direct + subItems
  }, 0)
}

export interface OptimizationPlayState {
  tab: OptimizationTabKey
  busy: boolean
  cleanupSelectedCount: number
  driverSelectedCount: number
  networkStatus: NetworkAccelerationStatus | null
}

export function isOptimizationPlayDisabled({
  tab,
  busy,
  cleanupSelectedCount,
  driverSelectedCount,
  networkStatus
}: OptimizationPlayState): boolean {
  if (busy) return true
  if (tab === 'cleanup') return cleanupSelectedCount === 0
  if (tab === 'driverDownload') return driverSelectedCount === 0
  if (tab !== 'networkAcceleration') return false

  return (
    networkStatus == null ||
    !networkStatus.isBackendReady ||
    networkStatus.config.mode === 'Hosts'
  )
}
