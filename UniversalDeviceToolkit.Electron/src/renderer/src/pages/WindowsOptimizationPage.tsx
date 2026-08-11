import { useEffect, useMemo, useState } from 'react'
import {
  CheckOutlined,
  InfoCircleOutlined,
  PlayCircleOutlined,
  SearchOutlined,
  StarFilled,
  StopOutlined,
  ThunderboltOutlined
} from '@ant-design/icons'
import { useTranslation } from 'react-i18next'
import type {
  NetworkAccelerationConfig,
  NetworkAccelerationMode,
  OptimizationActionDefinition,
  OptimizationCategoryDefinition
} from '../api/optimization'
import { useOptimizationStore } from '../stores/optimizationStore'
import CleanupRulesPanel from '../components/optimization/CleanupRulesPanel'
import DriverDownloadPanel from '../components/optimization/DriverDownloadPanel'
import { NetworkPanels } from '../components/optimization/NetworkPanels'
import { presentCategoryActions } from '../utils/optimizationToggle'
import { openActionDetails } from '../components/utils/ActionDetailsModal'
import '../components/optimization/optimization.css'

function formatBytes(bytes: number): string {
  if (!Number.isFinite(bytes) || bytes <= 0) return '0 B'
  const gb = bytes / 1024 ** 3
  if (gb >= 1) return `${gb.toFixed(2)} GB`
  const mb = bytes / 1024 ** 2
  if (mb >= 1) return `${mb.toFixed(1)} MB`
  return `${bytes.toFixed(0)} B`
}

function findAction(
  categories: OptimizationCategoryDefinition[],
  key: string
): OptimizationActionDefinition | null {
  for (const category of categories) {
    const action = category.actions.find((a) => a.key === key)
    if (action) return action
  }
  return null
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
        {action.title}
      </span>
      {action.recommended && (
        <span className="udt-badge">
          <StarFilled /> {t('optimization.recommended')}
        </span>
      )}
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
        title={t('wpf.actionDetailsWindowtitle')}
        onClick={(event) => {
          event.stopPropagation()
          showDetails()
        }}
      >
        <InfoCircleOutlined />
      </button>
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
  const presentation = useMemo(
    () => presentCategoryActions(category.actions, busy),
    [category.actions, busy]
  )
  return (
    <div className="udt-card udt-category">
      <button type="button" className="udt-category__header" onClick={() => setExpanded(!expanded)}>
        <div className="udt-card__copy">
          <div className="udt-card__title">{category.title}</div>
          <div className="udt-card__desc">{category.description}</div>
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

function OptimizationTab(): React.JSX.Element {
  const { t } = useTranslation()
  const categories = useOptimizationStore((s) => s.categories)
  const loading = useOptimizationStore((s) => s.loading)
  const apply = useOptimizationStore((s) => s.apply)
  const revert = useOptimizationStore((s) => s.revert)
  const applyRecommended = useOptimizationStore((s) => s.applyRecommended)
  const [selectedKeys, setSelectedKeys] = useState<string[]>([])
  const [busy, setBusy] = useState(false)

  const optimizationCategories = categories.filter((c) => !c.key.startsWith('cleanup.'))
  const selectedActions = selectedKeys
    .map((key) => findAction(categories, key))
    .filter((action): action is OptimizationActionDefinition => action !== null)

  const toggleSelection = (key: string): void => {
    setSelectedKeys((prev) => (prev.includes(key) ? prev.filter((k) => k !== key) : [...prev, key]))
  }

  const handleSelectRecommended = (): void => {
    const keys = optimizationCategories.flatMap(
      (category) => presentCategoryActions(category.actions).recommendedKeys
    )
    setSelectedKeys(keys)
  }

  const handleApply = async (): Promise<void> => {
    if (selectedKeys.length === 0) return
    setBusy(true)
    const ok = await apply(selectedKeys)
    setBusy(false)
    if (ok) setSelectedKeys([])
  }

  const handleClear = async (): Promise<void> => {
    if (selectedKeys.length === 0) return
    setBusy(true)
    const ok = await revert(selectedKeys)
    setBusy(false)
    if (ok) setSelectedKeys([])
  }

  const handleApplyRecommended = async (): Promise<void> => {
    setBusy(true)
    await applyRecommended()
    setBusy(false)
  }

  return (
    <div className="udt-optimization-layout">
      <div className="udt-optimization-layout__main">
        {loading && <div className="udt-skeleton-list"><div className="udt-skeleton-card" /></div>}
        {optimizationCategories.map((category) => {
          const visible = presentCategoryActions(category.actions).visible
          const count = visible.filter(({ action }) => selectedKeys.includes(action.key)).length
          return (
            <CategoryCard
              key={category.key}
              category={category}
              selectedKeys={selectedKeys}
              busy={busy}
              onToggle={toggleSelection}
              summary={`${count} / ${visible.length}`}
            />
          )
        })}
      </div>
      <div className="udt-optimization-layout__side">
        <div className="udt-card udt-side-card">
          <div className="udt-card__title">{t('optimization.selectedActions')}</div>
          <div className="udt-card__desc">
            {t('optimization.selectedActions')} · {selectedActions.length}
          </div>
          {selectedActions.length === 0 ? (
            <div className="udt-empty">
              <div className="udt-empty__title">{t('optimization.noSelection')}</div>
            </div>
          ) : (
            <div className="udt-side-card__list">
              {selectedActions.map((action) => (
                <div key={action.key} className="udt-side-card__item">
                  <div className="udt-side-card__item-title">{action.title}</div>
                  {action.recommended && <StarFilled className="udt-side-card__star" />}
                </div>
              ))}
            </div>
          )}
          <div className="udt-side-card__actions">
            <button type="button" className="udt-btn udt-btn--secondary" onClick={handleSelectRecommended}>
              {t('optimization.selectRecommended')}
            </button>
            <button
              type="button"
              className="udt-btn udt-btn--primary"
              disabled={selectedActions.length === 0 || busy}
              onClick={() => void handleApply()}
            >
              {t('optimization.apply')}
            </button>
            <button
              type="button"
              className="udt-btn udt-btn--danger"
              disabled={selectedActions.length === 0 || busy}
              onClick={() => void handleClear()}
            >
              {t('optimization.clear')}
            </button>
            <button
              type="button"
              className="udt-btn udt-btn--secondary"
              disabled={busy}
              onClick={() => void handleApplyRecommended()}
            >
              {t('optimization.applyRecommended')}
            </button>
          </div>
        </div>
      </div>
    </div>
  )
}

function CleanupTab(): React.JSX.Element {
  const { t } = useTranslation()
  const categories = useOptimizationStore((s) => s.categories)
  const estimate = useOptimizationStore((s) => s.estimate)
  const runCleanup = useOptimizationStore((s) => s.runCleanup)
  const [selectedKeys, setSelectedKeys] = useState<string[]>([])
  const [estimateBytes, setEstimateBytes] = useState<number | null>(null)
  const [estimating, setEstimating] = useState(false)
  const [cleaning, setCleaning] = useState(false)

  const cleanupCategories = categories.filter((c) => c.key.startsWith('cleanup.'))

  const toggleSelection = (key: string): void => {
    setSelectedKeys((prev) => (prev.includes(key) ? prev.filter((k) => k !== key) : [...prev, key]))
  }

  const handleEstimate = async (): Promise<void> => {
    if (selectedKeys.length === 0) return
    setEstimating(true)
    const bytes = await estimate(selectedKeys)
    setEstimating(false)
    setEstimateBytes(bytes)
  }

  const handleRun = async (): Promise<void> => {
    setCleaning(true)
    const ok = await runCleanup(selectedKeys)
    setCleaning(false)
    if (ok) {
      setSelectedKeys([])
      setEstimateBytes(null)
    }
  }

  return (
    <div className="udt-optimization-layout">
      <div className="udt-optimization-layout__main">
        {cleanupCategories.map((category) => (
          <CategoryCard
            key={category.key}
            category={category}
            selectedKeys={selectedKeys}
            busy={cleaning || estimating}
            onToggle={toggleSelection}
          />
        ))}
      </div>
      <div className="udt-optimization-layout__side">
        <div className="udt-card udt-side-card">
          <div className="udt-card__title">{t('optimization.estimate')}</div>
          <div className="udt-card__desc">
            {estimateBytes !== null
              ? `${t('optimization.estimateResult')}: ${formatBytes(estimateBytes)}`
              : t('optimization.cleanupHint')}
          </div>
          <div className="udt-side-card__actions">
            <button
              type="button"
              className="udt-btn udt-btn--secondary"
              disabled={selectedKeys.length === 0 || estimating}
              onClick={() => void handleEstimate()}
            >
              {t('optimization.estimate')}
            </button>
            <button
              type="button"
              className="udt-btn udt-btn--danger"
              disabled={selectedKeys.length === 0 || cleaning}
              onClick={() => void handleRun()}
            >
              <PlayCircleOutlined /> {t('optimization.runCleanup')}
            </button>
          </div>
        </div>
        <CleanupRulesPanel />
      </div>
    </div>
  )
}

function DriverDownloadTab(): React.JSX.Element {
  return <DriverDownloadPanel />
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
    return <div className="udt-skeleton-card" style={{ minHeight: 120 }} />
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

const TABS: { key: TabKey; icon: React.ReactNode }[] = [
  { key: 'optimization', icon: <ThunderboltOutlined /> },
  { key: 'cleanup', icon: <SearchOutlined /> },
  { key: 'driverDownload', icon: <SearchOutlined /> },
  { key: 'networkAcceleration', icon: <ThunderboltOutlined /> }
]

export default function WindowsOptimizationPage(): React.JSX.Element {
  const { t } = useTranslation()
  const load = useOptimizationStore((s) => s.load)
  const loadNetwork = useOptimizationStore((s) => s.loadNetwork)
  const [tab, setTab] = useState<TabKey>('optimization')

  useEffect(() => {
    void load()
    void loadNetwork()
  }, [load, loadNetwork])

  return (
    <div className="udt-page">
      <h1 className="udt-page__title">{t('optimization.title')}</h1>
      <p className="udt-page__subtitle">{t('optimization.info')}</p>

      <div className="udt-segmented-nav">
        {TABS.map(({ key, icon }) => (
          <button
            key={key}
            type="button"
            className={`udt-segmented-nav__item${tab === key ? ' udt-segmented-nav__item--active' : ''}`}
            onClick={() => setTab(key)}
          >
            {icon}
            {t(`optimization.tabs.${key}`)}
            <span className="udt-segmented-nav__indicator" />
          </button>
        ))}
      </div>

      <div className="udt-tab-content" key={tab}>
        {tab === 'optimization' && <OptimizationTab />}
        {tab === 'cleanup' && <CleanupTab />}
        {tab === 'driverDownload' && <DriverDownloadTab />}
        {tab === 'networkAcceleration' && <NetworkTab />}
      </div>
    </div>
  )
}
