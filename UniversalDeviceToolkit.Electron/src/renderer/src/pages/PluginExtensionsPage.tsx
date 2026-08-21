import { useEffect, useMemo, useState } from 'react'
import {
  Apps24Regular,
  CheckmarkCircle24Regular,
  Checkmark24Regular,
  Copy24Regular,
  Delete24Regular,
  ChevronDown24Regular,
  ArrowDownload24Regular,
  ErrorCircle24Regular,
  FolderOpen24Regular,
  ArrowClockwise24Regular,
  Search24Regular,
  Settings24Regular,
  ArrowCircleUp24Regular,
  ChevronUp24Regular
} from '../components/icons/fluent'
import { Button, Input, Popconfirm, Select, Spin, Tooltip, message } from 'antd'
import { useTranslation } from 'react-i18next'
import { useNavigate } from 'react-router-dom'
import type { PluginView } from '../api/plugins'
import { resolvePluginWebPageEntry } from '../components/plugins/pluginPageViewModel'
import { SkeletonBone } from '../components/Skeleton'
import { usePluginsStore } from '../stores/pluginsStore'
import PluginSettingsModal from '../components/settings/PluginSettingsModal'
import {
  filterPlugins,
  pluginCardActions,
  pluginFileName,
  runPluginOperations,
  summarizePlugins,
  uninstallFeedback
} from './pluginExtensionsModel'
import type { PluginFilterValue } from './pluginExtensionsModel'
import './pages.css'

function pluginWebPageNavigable(plugin: PluginView): boolean {
  if (resolvePluginWebPageEntry(plugin.webPage) == null) return false
  return (
    Boolean(plugin.directory) ||
    Boolean(plugin.installedVersion) ||
    plugin.state === 'Installed'
  )
}

function operationErrorText(fallback: string): string {
  const detail = usePluginsStore.getState().error
  return detail != null && detail.length > 0 ? detail : fallback
}

interface ContextMenuState {
  id: string
  x: number
  y: number
}

function clampMenu(x: number, y: number, width = 180, height = 90): { x: number; y: number } {
  const margin = 8
  return {
    x: Math.max(margin, Math.min(x, window.innerWidth - width - margin)),
    y: Math.max(margin, Math.min(y, window.innerHeight - height - margin))
  }
}

const SKELETON_ROWS = [
  { name: 220, sub: 128, right: 52 },
  { name: 194, sub: 116, right: 68 },
  { name: 172, sub: 98, right: 84 }
]

function deterministicIconBackground(seed: string): string {
  let hash = 0
  for (let i = 0; i < seed.length; i++) {
    hash = (hash * 31 + seed.charCodeAt(i)) | 0
  }
  return `hsl(${Math.abs(hash % 360)} 70% 52%)`
}

function iconLetterOf(name: string): string {
  const words = name.split(/[\s\-_]+/).filter(Boolean)
  const letters: string[] = []
  for (const word of words) {
    const first = word[0]
    if (!first) continue
    if (/[a-zA-Z]/.test(first)) letters.push(first.toUpperCase())
    else if (/[0-9]/.test(first)) letters.push(first)
    if (letters.length >= 2) break
  }
  if (letters.length === 0) {
    const first = name[0]
    if (first) letters.push(/[a-zA-Z]/.test(first) ? first.toUpperCase() : first)
  }
  return letters.join('').slice(0, 2)
}

function PluginCard({ plugin }: { plugin: PluginView }): React.JSX.Element {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const install = usePluginsStore((state) => state.install)
  const uninstall = usePluginsStore((state) => state.uninstall)
  const installingIds = usePluginsStore((state) => state.installingIds)
  const [expanded, setExpanded] = useState(false)
  const [settingsOpen, setSettingsOpen] = useState(false)
  const [contextMenu, setContextMenu] = useState<ContextMenuState | null>(null)

  const installing = plugin.id in installingIds
  const progress = installingIds[plugin.id] ?? 0
  const actions = pluginCardActions(plugin)
  const installed = actions.installed
  const canOpenWebPage = actions.canOpenWebPage || pluginWebPageNavigable(plugin)
  const hasUpdateInfo =
    plugin.updateAvailable && Boolean(plugin.availableVersion || plugin.releaseDate || plugin.changelog)
  const hasExpandableContent = Boolean(plugin.details || plugin.usageGuide || hasUpdateInfo)

  const badgeText = installing
    ? null
    : plugin.updateAvailable
      ? t('plugins.updateAvailable')
      : installed
        ? t('plugins.installed')
        : plugin.isSystemPlugin
          ? t('plugins.local', '本地')
          : null

  const secondaryLine = installing
    ? `${t('plugins.installing')}${progress > 0 ? ` · ${Math.round(progress)}%` : ''}`
    : null

  const handleUninstall = async (): Promise<void> => {
    try {
      const result = await uninstall(plugin.id)
      const feedback = uninstallFeedback(result)
      if (feedback === 'dependencyBlocked') {
        message.warning(t('plugins.dependenciesBlocked'))
        return
      }
      if (feedback === 'failed' || result.ok !== true) {
        message.error(
          operationErrorText(
            t('plugins.uninstallFailed', { defaultValue: 'Failed to uninstall' })
          )
        )
      }
    } catch (error) {
      message.error(
        error instanceof Error
          ? error.message
          : t('plugins.uninstallFailed', { defaultValue: 'Failed to uninstall' })
      )
    }
  }

  const handleInstall = async (): Promise<void> => {
    const failedText = t(
      plugin.updateAvailable
        ? 'pluginExtensionsPageupdateFailed'
        : 'pluginExtensionsPageinstallFailed',
      { defaultValue: plugin.updateAvailable ? 'Update failed' : 'Installation failed' }
    )
    try {
      if ((await install(plugin.id)) === true) return
      message.error(operationErrorText(failedText))
    } catch (error) {
      message.error(error instanceof Error ? error.message : failedText)
    }
  }

  const handleCopyId = async (): Promise<void> => {
    try {
      await navigator.clipboard.writeText(plugin.id)
      message.success(t('plugins.copied'))
    } catch {
      message.error(t('plugins.copyFailed'))
    }
    setContextMenu(null)
  }

  const openContextMenu = (event: React.MouseEvent): void => {
    event.preventDefault()
    const position = clampMenu(event.clientX, event.clientY)
    setContextMenu({ id: plugin.id, x: position.x, y: position.y })
  }

  return (
    <div className="udt-plugin-card" onContextMenu={openContextMenu}>
      <div className="udt-plugin-card__row">
        <div
          className="udt-plugin-card__icon"
          style={{ background: plugin.iconBackground ?? deterministicIconBackground(plugin.id) }}
        >
          {iconLetterOf(plugin.name)}
          {installed && !installing && (
            <span className="udt-plugin-card__installed-badge">
              <Checkmark24Regular />
            </span>
          )}
        </div>

        <div className="udt-plugin-card__main">
          <div className="udt-plugin-card__title-row">
            <span className="udt-plugin-card__name">{plugin.name}</span>
            <span className="udt-plugin-card__version">v{plugin.version}</span>
            {badgeText && <span className="udt-badge">{badgeText}</span>}
            {plugin.isSystemPlugin && <span className="udt-badge udt-badge--plain">{t('plugins.local', '本地')}</span>}
          </div>

          {secondaryLine && <div className="udt-plugin-card__secondary">{secondaryLine}</div>}

          {!installing && plugin.description && (
            <div className="udt-plugin-card__description">{plugin.description}</div>
          )}

          {plugin.tags.length > 0 && (
            <div className="udt-plugin-card__tags">
              {plugin.tags.map((tag) => (
                <span key={tag} className="udt-badge udt-badge--plain">
                  {tag}
                </span>
              ))}
            </div>
          )}

          {hasExpandableContent && (
            <button
              type="button"
              className="udt-plugin-card__toggle"
              onClick={() => setExpanded((value) => !value)}
            >
              {expanded ? <ChevronUp24Regular /> : <ChevronDown24Regular />}
              {expanded
                ? t('plugins.collapseDetails', '隐藏详细资料')
                : t('plugins.showDetails', '显示详细资料')}
            </button>
          )}

          {expanded && hasExpandableContent && (
            <div className="udt-plugin-card__details">
              {plugin.details && (
                <div className="udt-plugin-card__details-section">
                  <div className="udt-plugin-card__details-label">{t('plugins.details')}</div>
                  <div className="udt-plugin-card__details-text">{plugin.details}</div>
                </div>
              )}
              {plugin.usageGuide && (
                <div className="udt-plugin-card__details-section">
                  <div className="udt-plugin-card__details-label">{t('plugins.usageGuide')}</div>
                  <div className="udt-plugin-card__details-text">{plugin.usageGuide}</div>
                </div>
              )}
              {hasUpdateInfo && (
                <div className="udt-plugin-card__details-section">
                  <div className="udt-plugin-card__details-label">
                    {t('plugins.updateInfo', '更新信息')}
                  </div>
                  <div className="udt-plugin-card__details-version-row">
                    <span>{t('plugins.versionLabel', 'Version:')}</span>
                    <strong>{plugin.availableVersion}</strong>
                    {plugin.releaseDate && (
                      <span className="udt-plugin-card__details-date">{plugin.releaseDate}</span>
                    )}
                  </div>
                  {plugin.changelog && (
                    <div className="udt-plugin-card__details-changelog">{plugin.changelog}</div>
                  )}
                </div>
              )}
            </div>
          )}
        </div>

        <div className="udt-plugin-card__actions">
          {installing ? (
            <div className="udt-plugin-card__progress-ring">
              <Spin size="small" />
              {progress > 0 && (
                <span className="udt-plugin-card__progress-text">{Math.round(progress)}%</span>
              )}
            </div>
          ) : (
            <>
              {actions.canInstallOrUpdate && (
                <Tooltip title={plugin.updateAvailable ? t('plugins.update') : t('plugins.install')}>
                  <button
                    type="button"
                    className="udt-action-btn udt-action-btn--accent"
                    aria-label={`${plugin.updateAvailable ? t('plugins.update') : t('plugins.install')} ${plugin.name}`}
                    onClick={() => void handleInstall()}
                  >
                    <ArrowDownload24Regular />
                  </button>
                </Tooltip>
              )}
              {actions.canConfigure && (
                <Tooltip title={t('plugins.configure', '配置')}>
                  <button
                    type="button"
                    className="udt-action-btn"
                    aria-label={`${t('plugins.configure', 'Configure')} ${plugin.name}`}
                    onClick={() => setSettingsOpen(true)}
                  >
                    <Settings24Regular />
                  </button>
                </Tooltip>
              )}
              {canOpenWebPage && (
                <Tooltip title={t('plugins.openPage', '打开插件页面')}>
                  <button
                    type="button"
                    className="udt-action-btn"
                    aria-label={`${t('plugins.openPage', 'Open plugin page')} ${plugin.name}`}
                    onClick={() => navigate(`/plugins/${encodeURIComponent(plugin.id)}`)}
                  >
                    <FolderOpen24Regular />
                  </button>
                </Tooltip>
              )}
              {actions.canOpenCapability && (
                <Tooltip title={t('plugins.open', '打开')}>
                  <button
                    type="button"
                    className="udt-action-btn"
                    aria-label={`${t('plugins.open', 'Open')} ${plugin.name}`}
                    onClick={() => {
                      setExpanded((value) => !value)
                    }}
                  >
                    <FolderOpen24Regular />
                  </button>
                </Tooltip>
              )}
              {actions.canUninstall && (
                <Popconfirm title={t('plugins.uninstallConfirm')} onConfirm={() => void handleUninstall()}>
                  <button
                    type="button"
                    className="udt-action-btn udt-action-btn--danger"
                    aria-label={`${t('plugins.uninstall')} ${plugin.name}`}
                  >
                    <Delete24Regular />
                  </button>
                </Popconfirm>
              )}
            </>
          )}
        </div>
      </div>

      {installing && progress > 0 && (
        <div
          className="udt-plugin-card__progress-fill"
          style={{ transform: `scaleX(${progress / 100})` }}
        />
      )}
      <PluginSettingsModal
        open={settingsOpen}
        pluginId={plugin.id}
        onClose={() => setSettingsOpen(false)}
      />
      {contextMenu?.id === plugin.id && (
        <>
          <div
            className="udt-plugin-card__context-menu"
            style={{ left: contextMenu.x, top: contextMenu.y }}
            role="menu"
          >
            <button type="button" role="menuitem" onClick={() => void handleCopyId()}>
              <Copy24Regular /> {t('plugins.copyId')}
            </button>
          </div>
          <div
            className="udt-context-menu-backdrop"
            onClick={() => setContextMenu(null)}
            onContextMenu={(event) => {
              event.preventDefault()
              setContextMenu(null)
            }}
          />
        </>
      )}
    </div>
  )
}
/**
 * Mirrors the live plugin card: bordered card shell, 44px icon, name row with
 * version badge, secondary/description lines and a trailing action button.
 */
function PluginSkeleton(): React.JSX.Element {
  return (
    <div className="udt-plugins-page__skeleton">
      {SKELETON_ROWS.map((row, index) => (
        <div key={index} className="udt-plugin-card">
          <div className="udt-plugin-card__row">
            <SkeletonBone delay={index * 4} className="udt-plugins-page__skeleton-icon" />
            <div className="udt-plugins-page__skeleton-copy">
              <div className="udt-plugins-page__skeleton-title-row">
                <SkeletonBone delay={index * 4 + 1} width={row.name} height={15} radius="small" />
                <SkeletonBone delay={index * 4 + 1} variant="muted" width={36} height={12} radius="small" />
              </div>
              <SkeletonBone
                delay={index * 4 + 2}
                width={row.sub}
                height={12}
                radius="small"
                style={{ marginTop: 8 }}
              />
              <SkeletonBone
                delay={index * 4 + 3}
                className="udt-plugins-page__skeleton-line"
                height={12}
                radius="small"
                style={{ marginRight: row.right }}
              />
            </div>
            <div className="udt-plugin-card__actions">
              <SkeletonBone delay={index * 4 + 3} width={84} height={32} radius="control" />
            </div>
          </div>
        </div>
      ))}
    </div>
  )
}

export default function PluginExtensionsPage(): React.JSX.Element {
  const { t } = useTranslation()
  // Field-level selectors: only the consumed slices re-render this page
  // (install progress mutates other store fields at a high rate).
  const plugins = usePluginsStore((s) => s.plugins)
  const loading = usePluginsStore((s) => s.loading)
  const offline = usePluginsStore((s) => s.offline)
  const error = usePluginsStore((s) => s.error)
  const load = usePluginsStore((s) => s.load)
  const refresh = usePluginsStore((s) => s.refresh)
  const install = usePluginsStore((s) => s.install)
  const importFile = usePluginsStore((s) => s.importFile)
  const [search, setSearch] = useState('')
  const [filter, setFilter] = useState<PluginFilterValue>('all')
  const [importing, setImporting] = useState(false)
  const [bulkUpdating, setBulkUpdating] = useState(false)

  useEffect(() => {
    void load()
  }, [load])

  const filtered = useMemo(() => {
    return filterPlugins(plugins, filter, search)
  }, [plugins, search, filter])

  const {
    totalCount,
    installedCount,
    updateCount,
    installableIds,
    updatableIds
  } = useMemo(() => summarizePlugins(plugins), [plugins])

  const handleImport = async (): Promise<void> => {
    let files: string[]
    try {
      files = (await window.bridge?.selectPluginFiles()) ?? []
    } catch (error) {
      message.error(
        error instanceof Error
          ? error.message
          : t('plugins.importFailed', { count: 1, defaultValue: 'Failed to import {{count}} plugin package(s)' })
      )
      return
    }
    if (files.length === 0) {
      if (window.bridge?.platform === 'web') {
        message.warning(
          t('plugins.importDesktopOnly', {
            defaultValue: 'Importing plugin files requires the desktop app.'
          })
        )
      }
      return
    }
    setImporting(true)
    try {
      message.info(t('plugins.importProgress'))
      const result = await runPluginOperations(files, importFile)
      const succeeded = result.succeeded.map(pluginFileName)
      const failed = result.failed.map(pluginFileName)
      if (succeeded.length > 0) {
        message.success(
          t('plugins.importSuccess', { count: succeeded.length, defaultValue: 'Imported {{count}} plugin package(s)' })
        )
      }
      if (failed.length > 0) {
        message.error(
          t('plugins.importFailed', { count: failed.length, defaultValue: 'Failed to import {{count}} plugin package(s)' })
        )
      }
      if (succeeded.length === 0 && failed.length === 0) {
        message.error(
          t('plugins.importFailed', { count: files.length, defaultValue: 'Failed to import {{count}} plugin package(s)' })
        )
      }
    } catch (error) {
      message.error(
        error instanceof Error
          ? error.message
          : t('plugins.importFailed', { count: files.length, defaultValue: 'Failed to import {{count}} plugin package(s)' })
      )
    } finally {
      setImporting(false)
    }
  }

  const handleInstallAll = async (): Promise<void> => {
    setBulkUpdating(true)
    try {
      const result = await runPluginOperations(installableIds, install)
      if (result.succeeded.length > 0 && result.failed.length === 0) {
        message.success(t('plugins.installAllComplete', { count: result.succeeded.length, defaultValue: 'Installed {{count}} plugin(s)' }))
      } else if (result.succeeded.length > 0) {
        message.warning(t('plugins.installAllPartial', { count: result.succeeded.length, total: installableIds.length, defaultValue: '{{count}} of {{total}} plugin operations completed' }))
      } else {
        message.error(
          operationErrorText(
            t('plugins.installAllPartial', {
              count: 0,
              total: installableIds.length,
              defaultValue: '{{count}} of {{total}} plugin operations completed'
            })
          )
        )
      }
    } catch (error) {
      message.error(
        error instanceof Error
          ? error.message
          : t('pluginExtensionsPageinstallFailed', { defaultValue: 'Installation failed' })
      )
    } finally {
      setBulkUpdating(false)
    }
  }

  const handleUpdateAll = async (): Promise<void> => {
    setBulkUpdating(true)
    try {
      const result = await runPluginOperations(updatableIds, install)
      if (result.succeeded.length > 0 && result.failed.length === 0) {
        message.success(`Updated ${result.succeeded.length} plugin${result.succeeded.length === 1 ? '' : 's'}`)
      } else if (result.succeeded.length > 0) {
        message.warning(`${result.succeeded.length} of ${updatableIds.length} plugin operations completed`)
      } else {
        message.error(
          operationErrorText(
            `${result.succeeded.length} of ${updatableIds.length} plugin operations completed`
          )
        )
      }
    } catch (error) {
      message.error(
        error instanceof Error
          ? error.message
          : t('pluginExtensionsPageupdateFailed', { defaultValue: 'Update failed' })
      )
    } finally {
      setBulkUpdating(false)
    }
  }

  return (
    <div className="udt-plugins-page udt-content-column udt-content-fill">
      <header className="udt-plugins-page__header">
        <h1 className="udt-page-title">{t('plugins.title')}</h1>
        <p className="udt-page-description">{t('plugins.description', '安装和管理插件以扩展功能')}</p>
      </header>

      {offline && (
        <div className="udt-plugins-page__offline-banner">
          <ErrorCircle24Regular className="udt-plugins-page__offline-icon" />
          <div className="udt-plugins-page__offline-copy">
            <div className="udt-plugins-page__offline-title">
              {t('plugins.storeUnavailable', '插件商店不可用')}
            </div>
            <div className="udt-plugins-page__offline-message">{t('plugins.offline')}</div>
          </div>
          <Button
            className="udt-btn udt-btn--secondary"
            loading={loading}
            onClick={() => void refresh()}
          >
            {t('common.retry')}
          </Button>
        </div>
      )}

      {!offline && error && (
        <div className="udt-plugins-page__offline-banner">
          <ErrorCircle24Regular className="udt-plugins-page__offline-icon udt-plugins-page__offline-icon--error" />
          <div className="udt-plugins-page__offline-copy">
            <div className="udt-plugins-page__offline-message">{error}</div>
          </div>
          <Button
            className="udt-btn udt-btn--secondary"
            loading={loading}
            onClick={() => void refresh()}
          >
            {t('common.retry')}
          </Button>
        </div>
      )}

      <div className="udt-plugins-page__summary">
        <div className="udt-plugins-page__metric">
          <Apps24Regular />
          <span className="udt-plugins-page__metric-label">{t('plugins.summaryTotal', '插件总数')}</span>
          <span className="udt-plugins-page__metric-value">{totalCount}</span>
        </div>
        <div className="udt-plugins-page__metric">
          <CheckmarkCircle24Regular />
          <span className="udt-plugins-page__metric-label">{t('plugins.summaryInstalled', '已安装')}</span>
          <span className="udt-plugins-page__metric-value">{installedCount}</span>
        </div>
        <div className="udt-plugins-page__metric">
          <ArrowCircleUp24Regular />
          <span className="udt-plugins-page__metric-label">{t('plugins.summaryUpdates', '可更新')}</span>
          <span className="udt-plugins-page__metric-value">{updateCount}</span>
        </div>
      </div>

      <div className="udt-plugins-page__toolbar">
        <Input
          allowClear
          prefix={<Search24Regular />}
          placeholder={t('plugins.search')}
          value={search}
          onChange={(event) => setSearch(event.target.value)}
        />
        <Select<PluginFilterValue>
          className="udt-plugins-page__filter"
          value={filter}
          onChange={setFilter}
          options={[
            { value: 'all', label: t('plugins.filterAll') },
            { value: 'installed', label: t('plugins.filterInstalled') },
            { value: 'notInstalled', label: t('plugins.filterNotInstalled') }
          ]}
        />
        <Button
          className="udt-btn udt-btn--secondary"
          icon={<FolderOpen24Regular />}
          loading={importing}
          onClick={() => void handleImport()}
        >
          {t('plugins.importFromFiles', '从文件导入')}
        </Button>
        <Tooltip title={t('plugins.refresh')}>
          <Button
            className="udt-btn udt-btn--secondary udt-btn-icon"
            icon={<ArrowClockwise24Regular />}
            loading={loading}
            onClick={() => void refresh()}
          />
        </Tooltip>
        {updateCount > 0 && (
          <Button
            className="udt-btn udt-btn--primary"
            icon={<ArrowCircleUp24Regular />}
            loading={bulkUpdating}
            onClick={() => void handleUpdateAll()}
          >
            {t('plugins.updateAll', '全部更新')}
          </Button>
        )}
        {installableIds.length > 0 && (
          <Button
            className="udt-btn udt-btn--primary"
            icon={<ArrowDownload24Regular />}
            loading={bulkUpdating}
            onClick={() => void handleInstallAll()}
          >
            {t('plugins.installAll', '全部安装')}
          </Button>
        )}
      </div>

      {loading && plugins.length === 0 ? (
        <PluginSkeleton />
      ) : filtered.length === 0 ? (
        plugins.length === 0 ? (
          <div className="udt-plugins-page__empty">
            <Apps24Regular className="udt-plugins-page__empty-icon" />
            <div className="udt-plugins-page__empty-title">{t('plugins.empty')}</div>
            <div className="udt-plugins-page__empty-description">
              {t(
                'plugins.emptyStore',
                'The plugin store is currently empty. Stay tuned for future plugin updates.'
              )}
            </div>
          </div>
        ) : (
          <div className="udt-plugins-page__empty">
            <Search24Regular className="udt-plugins-page__empty-icon" />
            <div className="udt-plugins-page__empty-title">
              {t('plugins.noResults', '未找到符合搜索条件的插件')}
            </div>
          </div>
        )
      ) : (
        <div className="udt-plugins-page__list">
          {filtered.map((plugin) => (
            <PluginCard key={plugin.id} plugin={plugin} />
          ))}
        </div>
      )}
    </div>
  )
}
