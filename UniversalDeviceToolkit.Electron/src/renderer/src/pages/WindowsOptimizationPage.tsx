import { useEffect, useMemo, useState } from 'react'
import {
  CheckOutlined,
  InfoCircleOutlined,
  PlayCircleOutlined,
  StarFilled,
  StarOutlined,
  StopOutlined
} from '@ant-design/icons'
import { Tooltip } from 'antd'
import { useTranslation } from 'react-i18next'
import type { TFunction } from 'i18next'
import i18n from '../i18n'
import type {
  NetworkAccelerationConfig,
  NetworkAccelerationMode,
  OptimizationActionDefinition,
  OptimizationCategoryDefinition
} from '../api/optimization'
import { useDriverStore } from '../stores/driverStore'
import { useOptimizationStore } from '../stores/optimizationStore'
import { SkeletonCard, SkeletonList } from '../components/Skeleton'
import CardExpander from '../components/CardExpander'
import CleanupRulesPanel from '../components/optimization/CleanupRulesPanel'
import DriverDownloadPanel from '../components/optimization/DriverDownloadPanel'
import { NetworkPanels } from '../components/optimization/NetworkPanels'
import { presentCategoryActions } from '../utils/optimizationToggle'
import { openActionDetails } from '../components/utils/ActionDetailsModal'
import '../components/optimization/optimization.css'

const NETWORK_RECOMMENDED_GROUP_IDS = new Set(['steam', 'github', 'public-cdn', 'twitch', 'roblox'])

function PlayOutlineIcon(): React.JSX.Element {
  return (
    <svg
      className="udt-opt-chrome__play-icon"
      width="16"
      height="16"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.75"
      strokeLinejoin="round"
      aria-hidden
    >
      <path d="M8 6.5v11l10-5.5-10-5.5z" />
    </svg>
  )
}

function collectRecommendedKeys(
  categories: OptimizationCategoryDefinition[],
  predicate: (categoryKey: string) => boolean
): string[] {
  return categories
    .filter((category) => predicate(category.key))
    .flatMap((category) => category.actions)
    .filter((action) => action.recommended && action.applied !== true)
    .map((action) => action.key)
}

function formatBytes(bytes: number): string {
  if (!Number.isFinite(bytes) || bytes <= 0) return '0 B'
  const gb = bytes / 1024 ** 3
  if (gb >= 1) return `${gb.toFixed(2)} GB`
  const mb = bytes / 1024 ** 2
  if (mb >= 1) return `${mb.toFixed(1)} MB`
  return `${bytes.toFixed(0)} B`
}

/**
 * Electron LocalizationHelper.GetStringOrEnglish: the host sends resource keys as
 * the category/action titles (e.g. "WindowsOptimization_Category_Explorer_Title");
 * translate them when a matching i18n key exists, otherwise show as-is.
 *
 * Locale files store some keys verbatim under `wpf.*` and the rest as the
 * migrated camelCase form (`WindowsOptimization_Category_CleanupCache_Title`
 * → `wpf.windowsOptimizationcategorycleanupCachetitle`).
 */
function wpfResxToI18nKey(resourceKey: string): string {
  return resourceKey
    .split('_')
    .map((part) => (part.length === 0 ? part : part[0].toLowerCase() + part.slice(1)))
    .join('')
}

function localizeText(t: TFunction, text: string): string {
  if (!text) return text
  const candidates = [text, `wpf.${text}`, `wpf.${wpfResxToI18nKey(text)}`]
  for (const key of candidates) {
    if (i18n.exists(key)) return t(key)
  }
  return text
}

type TabKey = 'optimization' | 'cleanup' | 'driverDownload' | 'networkAcceleration'

function ActionRow({
  action,
  selected,
  disabled,
  onToggle
}: {
  action: OptimizationActionDefinition
  selected: boolean
  disabled: boolean
  onToggle: (key: string) => void
}): React.JSX.Element {
  const { t } = useTranslation()
  const showDetails = (): void => {
    void openActionDetails({
      actionKey: action.key,
      title: action.title,
      description: action.description
    })
  }
  return (
    <div className="udt-action-row" onClick={() => !disabled && onToggle(action.key)}>
      <label className="udt-checkbox">
        <input
          type="checkbox"
          checked={action.applied === true || selected}
          disabled={disabled}
          ref={(el) => {
            if (el) el.indeterminate = action.applied === null
          }}
          onChange={() => onToggle(action.key)}
        />
        <span className="udt-checkbox__box">
          <CheckOutlined />
        </span>
      </label>
      <span className={`udt-action-row__title${disabled ? ' udt-action-row__title--muted' : ''}`}>
        {localizeText(t, action.title)}
      </span>
      {action.recommended && (
        <span className="udt-badge">
          <StarFilled /> {t('optimization.recommended')}
        </span>
      )}
      <Tooltip title={t('wpf.actionDetailsWindowtitle')}>
        <button
          type="button"
          style={{
            marginLeft: 8,
            border: 'none',
            background: 'transparent',
            color: 'var(--udt-text-secondary, rgba(255,255,255,0.6))',
            cursor: 'pointer',
            fontSize: 14,
            padding: '4px 6px',
            borderRadius: 6
          }}
          onClick={(event) => {
            event.stopPropagation()
            showDetails()
          }}
        >
          <InfoCircleOutlined />
        </button>
      </Tooltip>
    </div>
  )
}

function CategoryCard({
  category,
  selectedKeys,
  busy,
  onToggle,
  summary
}: {
  category: OptimizationCategoryDefinition
  selectedKeys: string[]
  busy: boolean
  onToggle: (key: string) => void
  summary?: string
}): React.JSX.Element {
  const [expanded, setExpanded] = useState(true)
  const { t } = useTranslation()
  const presentation = useMemo(
    () => presentCategoryActions(category.actions, busy),
    [category.actions, busy]
  )
  return (
    <div className="udt-card udt-category">
      <button type="button" className="udt-category__header" onClick={() => setExpanded(!expanded)}>
        <div className="udt-card__copy">
          <div className="udt-card__title">{localizeText(t, category.title)}</div>
          <div className="udt-card__desc">{localizeText(t, category.description)}</div>
        </div>
        {summary && <span className="udt-category__summary">{summary}</span>}
        <span className={`udt-category__chevron${expanded ? ' udt-category__chevron--open' : ''}`}>
          <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.5">
            <path d="M6 9l6 6 6-6" strokeLinecap="round" strokeLinejoin="round" />
          </svg>
        </span>
      </button>
      {expanded && (
        <div className="udt-category__body">
          {presentation.visible.map(({ action, editable }) => (
            <ActionRow
              key={action.key}
              action={action}
              selected={selectedKeys.includes(action.key)}
              disabled={!editable}
              onToggle={onToggle}
            />
          ))}
        </div>
      )}
    </div>
  )
}

function OptimizationTab({
  selectedKeys,
  busy,
  onToggle
}: {
  selectedKeys: string[]
  busy: boolean
  onToggle: (key: string) => void
}): React.JSX.Element {
  const categories = useOptimizationStore((s) => s.categories)
  const loading = useOptimizationStore((s) => s.loading)

  const optimizationCategories = categories.filter((c) => !c.key.startsWith('cleanup.'))

  return (
    <div className="udt-optimization-layout udt-optimization-layout--solo">
      <div className="udt-optimization-layout__main">
        {loading && <SkeletonList rows={3} />}
        {optimizationCategories.map((category) => {
          const visible = presentCategoryActions(category.actions, busy).visible
          const appliedCount = visible.filter(({ action }) => action.applied === true).length
          return (
            <CategoryCard
              key={category.key}
              category={category}
              selectedKeys={selectedKeys}
              busy={busy}
              onToggle={onToggle}
              summary={`${appliedCount} / ${visible.length}`}
            />
          )
        })}
      </div>
    </div>
  )
}

function CleanupTab({
  selectedKeys,
  onSelectedKeysChange
}: {
  selectedKeys: string[]
  onSelectedKeysChange: (keys: string[]) => void
}): React.JSX.Element {
  const { t } = useTranslation()
  const categories = useOptimizationStore((s) => s.categories)
  const estimate = useOptimizationStore((s) => s.estimate)
  const [estimateBytes, setEstimateBytes] = useState<number | null>(null)
  const [estimating, setEstimating] = useState(false)

  const cleanupCategories = categories.filter(
    (c) => c.key.startsWith('cleanup.') && c.key !== 'cleanup.custom'
  )

  const toggleSelection = (key: string): void => {
    onSelectedKeysChange(
      selectedKeys.includes(key) ? selectedKeys.filter((k) => k !== key) : [...selectedKeys, key]
    )
  }

  const handleEstimate = async (): Promise<void> => {
    if (selectedKeys.length === 0) return
    setEstimating(true)
    const bytes = await estimate(selectedKeys)
    setEstimating(false)
    setEstimateBytes(bytes)
  }

  const cleanupInfoDescription = estimating
    ? t('wpf.windowsOptimizationPageestimatedCleanupSizepending', {
        defaultValue: t('optimization.estimate')
      })
    : estimateBytes !== null
      ? t('wpf.windowsOptimizationPageestimatedCleanupSize', {
          defaultValue: `${t('optimization.estimateResult')}: {0}`
        }).replace('{0}', formatBytes(estimateBytes))
      : t('wpf.windowsOptimizationPagecleanupDescription', {
          defaultValue: t('optimization.cleanupHint')
        })

  return (
    <div className="udt-optimization-layout udt-optimization-layout--cleanup">
      <div className="udt-optimization-layout__main">
        {cleanupCategories.map((category) => {
          const presentation = presentCategoryActions(category.actions, estimating)
          const selectedCount = presentation.visible.filter(
            ({ action }) => selectedKeys.includes(action.key) || action.applied === true
          ).length
          const summary = t('wpf.windowsOptimizationcategoryselectionSummary', {
            defaultValue: t('optimization.selectedActions') + ' {0}/{1}'
          })
            .replace('{0}', String(selectedCount))
            .replace('{1}', String(presentation.visible.length))
          return (
            <CardExpander
              key={category.key}
              header={localizeText(t, category.title)}
              description={localizeText(t, category.description)}
              accessory={<span className="udt-category__summary">{summary}</span>}
              defaultExpanded={false}
            >
              {presentation.visible.map(({ action, editable }) => (
                <ActionRow
                  key={action.key}
                  action={action}
                  selected={selectedKeys.includes(action.key)}
                  disabled={!editable}
                  onToggle={toggleSelection}
                />
              ))}
            </CardExpander>
          )
        })}
      </div>
      <div className="udt-optimization-layout__divider" aria-hidden="true" />
      <div className="udt-optimization-layout__side">
        <div className="udt-card udt-side-card udt-cleanup-info">
          <div className="udt-card__title">
            {t('wpf.windowsOptimizationPagecleanupInfo', { defaultValue: t('optimization.estimate') })}
          </div>
          <div className="udt-card__desc">{cleanupInfoDescription}</div>
          <button
            type="button"
            className="udt-btn udt-btn--primary udt-cleanup-scan"
            disabled={selectedKeys.length === 0 || estimating}
            onClick={() => void handleEstimate()}
          >
            {t('wpf.windowsOptimizationPagescanbutton', { defaultValue: t('optimization.estimate') })}
          </button>
        </div>
        <CleanupRulesPanel />
      </div>
    </div>
  )
}

const NETWORK_MODES: NetworkAccelerationMode[] = ['Off', 'SystemProxy', 'Hosts', 'DiagnosticsOnly']

const NETWORK_MODE_I18N_KEYS: Record<NetworkAccelerationMode, string> = {
  Off: 'optimization.network.modes.off',
  SystemProxy: 'optimization.network.modes.systemProxy',
  Hosts: 'optimization.network.modes.hosts',
  DiagnosticsOnly: 'optimization.network.modes.diagnosticsOnly'
}

function getNetworkSelectedTargetCount(config: NetworkAccelerationConfig | null): number {
  if (!config) return 0
  return config.domainGroups.reduce((sum, group) => {
    if (!group.enabled) return sum
    const direct = (group.domains ?? []).filter((domain) => domain.trim().length > 0).length
    const subItems = (group.subItems ?? []).filter((sub) => sub.enabled).length
    return sum + direct + subItems
  }, 0)
}

function NetworkTab(): React.JSX.Element {
  const { t } = useTranslation()
  const networkStatus = useOptimizationStore((s) => s.networkStatus)
  const saveNetworkConfig = useOptimizationStore((s) => s.saveNetworkConfig)
  const startNetwork = useOptimizationStore((s) => s.startNetwork)
  const stopNetwork = useOptimizationStore((s) => s.stopNetwork)
  const loadTraffic = useOptimizationStore((s) => s.loadTraffic)
  const loadRuntime = useOptimizationStore((s) => s.loadRuntime)
  const [config, setConfig] = useState<NetworkAccelerationConfig | null>(null)
  const [saving, setSaving] = useState(false)
  const [starting, setStarting] = useState(false)
  const [stopping, setStopping] = useState(false)

  const isRunning = networkStatus?.isRunning === true

  useEffect(() => {
    if (!isRunning) return
    const trafficTimer = setInterval(() => void loadTraffic(), 1000)
    const runtimeTimer = setInterval(() => void loadRuntime(), 2000)
    return () => {
      clearInterval(trafficTimer)
      clearInterval(runtimeTimer)
    }
  }, [isRunning, loadTraffic, loadRuntime])

  const editableConfig = config ?? networkStatus?.config ?? null

  const ensureConfig = (): NetworkAccelerationConfig | null => {
    if (config) return config
    if (!networkStatus) return null
    const next: NetworkAccelerationConfig = {
      ...networkStatus.config,
      domainGroups: [...networkStatus.config.domainGroups]
    }
    setConfig(next)
    return next
  }

  const handleSave = async (): Promise<void> => {
    const current = ensureConfig()
    if (!current) return
    setSaving(true)
    await saveNetworkConfig(current)
    setSaving(false)
  }

  const handleStart = async (): Promise<void> => {
    setStarting(true)
    await startNetwork()
    setStarting(false)
  }

  const handleStop = async (): Promise<void> => {
    setStopping(true)
    await stopNetwork()
    setStopping(false)
  }

  if (!networkStatus || !editableConfig) {
    return <SkeletonCard lines={3} withIcon />
  }

  const updateConfig = (patch: Partial<NetworkAccelerationConfig>): void => {
    const current = ensureConfig()
    if (!current) return
    setConfig({ ...current, ...patch })
  }

  const selectedTargets = getNetworkSelectedTargetCount(editableConfig)

  return (
    <div className="udt-network-layout">
      <div className="udt-card udt-card--row">
        <span
          className={`udt-status-dot${networkStatus.isRunning ? ' udt-status-dot--on' : ''}`}
        />
        <div className="udt-card__copy">
          <div className="udt-card__title">
            {networkStatus.isRunning ? t('optimization.network.running') : t('optimization.network.stopped')}
          </div>
          <div className="udt-card__desc">{networkStatus.statusText}</div>
        </div>
        <div className="udt-card__desc">
          {networkStatus.isBackendReady
            ? t('optimization.network.backendReady')
            : t('optimization.network.backendNotReady')}
        </div>
        <div className="udt-card__desc">
          {t('optimization.network.targetsLabel')}: {selectedTargets}
        </div>
        <div className="udt-card__desc">
          {t('optimization.network.portLabel')}: {editableConfig.listenPort}
        </div>
      </div>

      <div className="udt-card">
        <div className="udt-card__title">{t('optimization.network.config')}</div>
        <div className="udt-network-row">
          <span>{t('optimization.network.accelerationEnabled')}</span>
          <label className="udt-switch">
            <input
              type="checkbox"
              checked={editableConfig.accelerationEnabled}
              onChange={(e) => updateConfig({ accelerationEnabled: e.target.checked })}
            />
            <span className="udt-switch__track" />
          </label>
        </div>
        <div className="udt-network-row">
          <span>{t('optimization.network.mode')}</span>
          <select
            className="udt-select"
            value={editableConfig.mode}
            onChange={(e) => updateConfig({ mode: e.target.value as NetworkAccelerationMode })}
          >
            {NETWORK_MODES.map((mode) => (
              <option key={mode} value={mode}>
                {t(NETWORK_MODE_I18N_KEYS[mode])}
              </option>
            ))}
          </select>
        </div>
        <div className="udt-side-card__actions">
          <button type="button" className="udt-btn udt-btn--primary" disabled={saving} onClick={() => void handleSave()}>
            {t('optimization.network.save')}
          </button>
          <button
            type="button"
            className="udt-btn udt-btn--secondary"
            disabled={!networkStatus.isBackendReady || starting}
            onClick={() => void handleStart()}
          >
            <PlayCircleOutlined /> {t('optimization.network.start')}
          </button>
          <button
            type="button"
            className="udt-btn udt-btn--danger"
            disabled={stopping}
            onClick={() => void handleStop()}
          >
            <StopOutlined /> {t('optimization.network.stop')}
          </button>
        </div>
      </div>

      <NetworkPanels />
    </div>
  )
}

const TAB_I18N_KEYS: Record<TabKey, string> = {
  optimization: 'wpf.windowsOptimizationPagetaboptimization',
  cleanup: 'wpf.windowsOptimizationPagetabcleanup',
  driverDownload: 'wpf.windowsOptimizationPagetabdriverDownload',
  networkAcceleration: 'wpf.windowsOptimizationPagetabnetworkAcceleration'
}

const TAB_FALLBACK_KEYS: Record<TabKey, string> = {
  optimization: 'optimization.tabs.optimization',
  cleanup: 'optimization.tabs.cleanup',
  driverDownload: 'optimization.tabs.driverDownload',
  networkAcceleration: 'optimization.tabs.networkAcceleration'
}

const TABS: TabKey[] = ['optimization', 'cleanup', 'driverDownload', 'networkAcceleration']

export default function WindowsOptimizationPage(): React.JSX.Element {
  const { t } = useTranslation()
  const load = useOptimizationStore((s) => s.load)
  const loadNetwork = useOptimizationStore((s) => s.loadNetwork)
  const categories = useOptimizationStore((s) => s.categories)
  const applyRecommended = useOptimizationStore((s) => s.applyRecommended)
  const apply = useOptimizationStore((s) => s.apply)
  const runCleanup = useOptimizationStore((s) => s.runCleanup)
  const startNetwork = useOptimizationStore((s) => s.startNetwork)
  const setNetworkGroupEnabled = useOptimizationStore((s) => s.setNetworkGroupEnabled)
  const networkStatus = useOptimizationStore((s) => s.networkStatus)
  const driverSelectedCount = useDriverStore((s) => s.selectedIds.length)
  const [tab, setTab] = useState<TabKey>('optimization')
  const [optSelectedKeys, setOptSelectedKeys] = useState<string[]>([])
  const [cleanupSelectedKeys, setCleanupSelectedKeys] = useState<string[]>([])
  const [chromeBusy, setChromeBusy] = useState(false)

  useEffect(() => {
    void load()
    void loadNetwork()
  }, [load, loadNetwork])

  const toggleOptSelection = (key: string): void => {
    setOptSelectedKeys((prev) => (prev.includes(key) ? prev.filter((k) => k !== key) : [...prev, key]))
  }

  const starTitle = useMemo(() => {
    if (tab === 'networkAcceleration') {
      return t('wpf.networkAccelerationPageselectionFavoriteTooltip', {
        defaultValue: t('optimization.selectRecommended')
      })
    }
    if (tab === 'driverDownload') {
      return t('optimization.driver.selectRecommended', { defaultValue: t('optimization.selectRecommended') })
    }
    return t('wpf.windowsOptimizationPageselectRecommendedbutton', {
      defaultValue: t('optimization.selectRecommended')
    })
  }, [t, tab])

  const playTitle = useMemo(() => {
    if (tab === 'networkAcceleration') {
      return t('optimization.network.start', { defaultValue: 'Start' })
    }
    if (tab === 'driverDownload') {
      return t('optimization.driver.startAll', { defaultValue: t('optimization.applyRecommended') })
    }
    if (tab === 'cleanup') {
      return t('optimization.runCleanup')
    }
    return t('optimization.applyRecommended')
  }, [t, tab])

  const handleStar = (): void => {
    if (tab === 'driverDownload') {
      useDriverStore.getState().selectRecommended()
      return
    }
    if (tab === 'networkAcceleration') {
      const groups = networkStatus?.config.domainGroups ?? []
      const recommended = groups.filter(
        (group) => group.isFavorite || NETWORK_RECOMMENDED_GROUP_IDS.has(group.id)
      )
      for (const group of recommended) {
        void setNetworkGroupEnabled(group.id, true)
      }
      return
    }
    if (tab === 'cleanup') {
      setCleanupSelectedKeys(collectRecommendedKeys(categories, (key) => key.startsWith('cleanup.')))
      return
    }
    setOptSelectedKeys(collectRecommendedKeys(categories, (key) => !key.startsWith('cleanup.')))
  }

  const handlePlay = async (): Promise<void> => {
    if (chromeBusy) return
    setChromeBusy(true)
    try {
      if (tab === 'networkAcceleration') {
        await startNetwork()
        return
      }
      if (tab === 'driverDownload') {
        await useDriverStore.getState().startSelected()
        return
      }
      if (tab === 'cleanup') {
        if (cleanupSelectedKeys.length === 0) return
        const ok = await runCleanup(cleanupSelectedKeys)
        if (ok) setCleanupSelectedKeys([])
        return
      }
      if (optSelectedKeys.length > 0) {
        const ok = await apply(optSelectedKeys)
        if (ok) setOptSelectedKeys([])
        return
      }
      await applyRecommended()
    } finally {
      setChromeBusy(false)
    }
  }

  const playDisabled =
    chromeBusy ||
    (tab === 'cleanup' && cleanupSelectedKeys.length === 0) ||
    (tab === 'driverDownload' && driverSelectedCount === 0)

  return (
    <div className="udt-page udt-optimization-page">
      <h1 className="udt-page__title">{t('optimization.title')}</h1>
      <p className="udt-page__subtitle">{t('optimization.info')}</p>

      <div className="udt-opt-chrome">
        <div className="udt-segmented-nav" role="tablist" aria-label={t('optimization.title')}>
          {TABS.map((key) => (
            <button
              key={key}
              type="button"
              role="tab"
              aria-selected={tab === key}
              className={`udt-segmented-nav__item${tab === key ? ' udt-segmented-nav__item--active' : ''}`}
              onClick={() => setTab(key)}
            >
              {t(TAB_I18N_KEYS[key], { defaultValue: t(TAB_FALLBACK_KEYS[key]) })}
              <span className="udt-segmented-nav__indicator" />
            </button>
          ))}
        </div>
        <div className="udt-opt-chrome__actions">
          <button
            type="button"
            className="udt-opt-chrome__icon-btn"
            title={starTitle}
            aria-label={starTitle}
            disabled={chromeBusy}
            onClick={handleStar}
          >
            <StarOutlined />
          </button>
          <button
            type="button"
            className="udt-opt-chrome__icon-btn"
            title={playTitle}
            aria-label={playTitle}
            disabled={playDisabled}
            onClick={() => void handlePlay()}
          >
            <PlayOutlineIcon />
          </button>
        </div>
      </div>

      <div className="udt-tab-content" key={tab}>
        {tab === 'optimization' && (
          <OptimizationTab
            selectedKeys={optSelectedKeys}
            busy={chromeBusy}
            onToggle={toggleOptSelection}
          />
        )}
        {tab === 'cleanup' && (
          <CleanupTab
            selectedKeys={cleanupSelectedKeys}
            onSelectedKeysChange={setCleanupSelectedKeys}
          />
        )}
        {tab === 'driverDownload' && <DriverDownloadPanel />}
        {tab === 'networkAcceleration' && <NetworkTab />}
      </div>
    </div>
  )
}
