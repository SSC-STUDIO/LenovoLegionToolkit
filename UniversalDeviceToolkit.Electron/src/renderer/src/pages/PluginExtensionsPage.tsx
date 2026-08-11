import { useEffect, useMemo, useState } from 'react'
import {
  AppstoreOutlined,
  CheckCircleOutlined,
  CheckOutlined,
  CopyOutlined,
  DeleteOutlined,
  DownOutlined,
  DownloadOutlined,
  ExclamationCircleOutlined,
  FolderOpenOutlined,
  ReloadOutlined,
  SearchOutlined,
  SettingOutlined,
  UpCircleOutlined,
  UpOutlined
} from '@ant-design/icons'
import { Button, Input, Popconfirm, Select, Spin, Tooltip, message } from 'antd'
import { useTranslation } from 'react-i18next'
import type { PluginView } from '../api/plugins'
import { usePluginsStore } from '../stores/pluginsStore'
import PluginSettingsModal from '../components/settings/PluginSettingsModal'
import '../components/pages/pages.css'

type FilterValue = 'all' | 'installed' | 'notInstalled'

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
  const install = usePluginsStore((state) => state.install)
  const uninstall = usePluginsStore((state) => state.uninstall)
  const installingIds = usePluginsStore((state) => state.installingIds)
  const [expanded, setExpanded] = useState(false)
  const [settingsOpen, setSettingsOpen] = useState(false)
  const [contextMenu, setContextMenu] = useState<ContextMenuState | null>(null)

  const installing = plugin.id in installingIds
  const progress = installingIds[plugin.id] ?? 0
  const installed = Boolean(plugin.installedVersion)
  const supportsOpen =
    plugin.capabilities.settingsPage ||
    plugin.capabilities.featurePage ||
    plugin.capabilities.optimizationCategory ||
    plugin.capabilities.executableEntryPoint
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
    const result = await uninstall(plugin.id)
    if (result.dependencyBlocked) {
      message.warning(t('plugins.dependenciesBlocked'))
    } else if (!result.ok) {
      message.error(t('plugins.uninstallFailed'))
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

  const handleDoubleClick = (): void => {
    // Mirrors PluginListBox_MouseDoubleClick → OpenPluginDefaultActionAsync. The
    // plugin pages themselves are hosted by the .NET side (PluginPageWrapper),
    // which the renderer cannot embed; surface the closest equivalent: expand
    // the details so settings/guide content stays reachable.
    setExpanded((value) => !value)
    setContextMenu(null)
  }

  return (
    <div className="udt-plugin-card" onContextMenu={openContextMenu} onDoubleClick={handleDoubleClick}>
      <div className="udt-plugin-card__row">
        <div
          className="udt-plugin-card__icon"
          style={{ background: plugin.iconBackground ?? deterministicIconBackground(plugin.id) }}
        >
          {iconLetterOf(plugin.name)}
          {installed && !installing && (
            <span className="udt-plugin-card__installed-badge">
              <CheckOutlined />
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
              {expanded ? <UpOutlined /> : <DownOutlined />}
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
                    <span>{t('plugins.versionLabel', '版本：')}</span>
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
              {(!installed || plugin.updateAvailable) && (
                <Tooltip title={plugin.updateAvailable ? t('plugins.update') : t('plugins.install')}>
                  <button
                    type="button"
                    className="udt-action-btn udt-action-btn--accent"
                    onClick={() => void install(plugin.id)}
                  >
                    <DownloadOutlined />
                  </button>
                </Tooltip>
              )}
              {installed && plugin.capabilities.settingsPage && (
                <Tooltip title={t('plugins.configure', '配置')}>
                  <button
                    type="button"
                    className="udt-action-btn"
                    onClick={() => setSettingsOpen(true)}
                  >
                    <SettingOutlined />
                  </button>
                </Tooltip>
              )}
              {installed && supportsOpen && (
                <Tooltip title={t('plugins.open', '打开')}>
                  <button
                    type="button"
                    className="udt-action-btn"
                    onClick={() => {
                      setExpanded((value) => !value)
                    }}
                  >
                    <FolderOpenOutlined />
                  </button>
                </Tooltip>
              )}
              {installed && (
                <Popconfirm title={t('plugins.uninstallConfirm')} onConfirm={() => void handleUninstall()}>
                  <button type="button" className="udt-action-btn udt-action-btn--danger">
                    <DeleteOutlined />
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
              <CopyOutlined /> {t('plugins.copyId')}
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
function PluginSkeleton(): React.JSX.Element {
  return (
    <div className="udt-plugins-page__skeleton">
      {SKELETON_ROWS.map((row, index) => (
        <div key={index} className="udt-plugins-page__skeleton-row">
          <div className="udt-skeleton udt-plugins-page__skeleton-icon" />
          <div className="udt-plugins-page__skeleton-copy">
            <div className="udt-skeleton" style={{ width: row.name, height: 14 }} />
            <div className="udt-skeleton" style={{ width: row.sub, height: 10 }} />
            <div
              className="udt-skeleton udt-plugins-page__skeleton-line"
              style={{ height: 10, marginRight: row.right }}
            />
          </div>
        </div>
      ))}
    </div>
  )
}

export default function PluginExtensionsPage(): React.JSX.Element {
  const { t } = useTranslation()
  const { plugins, loading, offline, error, load, refresh, install, importFile } = usePluginsStore()
  const [search, setSearch] = useState('')
  const [filter, setFilter] = useState<FilterValue>('all')
  const [importing, setImporting] = useState(false)
  const [bulkUpdating, setBulkUpdating] = useState(false)

  useEffect(() => {
    void load()
  }, [load])

  const filtered = useMemo(() => {
    const query = search.trim().toLowerCase()
    return plugins.filter((plugin) => {
      if (filter === 'installed' && !plugin.installedVersion) return false
      if (filter === 'notInstalled' && plugin.installedVersion) return false
      if (!query) return true
      return (
        plugin.name.toLowerCase().includes(query) ||
        plugin.description.toLowerCase().includes(query) ||
        plugin.id.toLowerCase().includes(query) ||
        plugin.tags.some((tag) => tag.toLowerCase().includes(query))
      )
    })
  }, [plugins, search, filter])

  const installedCount = plugins.filter((plugin) => plugin.installedVersion).length
  const updateCount = plugins.filter((plugin) => plugin.updateAvailable).length
  const installableIds = useMemo(
    () => plugins.filter((plugin) => !plugin.installedVersion && !plugin.isSystemPlugin).map((plugin) => plugin.id),
    [plugins]
  )
  const updatableIds = useMemo(
    () => plugins.filter((plugin) => plugin.updateAvailable).map((plugin) => plugin.id),
    [plugins]
  )

  const handleImport = async (): Promise<void> => {
    const files = (await window.bridge?.selectPluginFiles()) ?? []
    if (files.length === 0) return
    setImporting(true)
    try {
      message.info(t('plugins.importProgress'))
      const succeeded: string[] = []
      const failed: string[] = []
      for (const file of files) {
        const name = file.split(/[\\/]/).pop() ?? file
        try {
          if (await importFile(file)) succeeded.push(name)
          else failed.push(name)
        } catch {
          failed.push(name)
        }
      }
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
    } finally {
      setImporting(false)
    }
  }

  const handleInstallAll = async (): Promise<void> => {
    setBulkUpdating(true)
    try {
      let succeeded = 0
      for (const pluginId of installableIds) {
        if (await install(pluginId)) succeeded += 1
      }
      if (succeeded === installableIds.length) {
        message.success(t('plugins.installAllComplete', { count: succeeded, defaultValue: 'Installed {{count}} plugin(s)' }))
      } else {
        message.warning(t('plugins.installAllPartial', { count: succeeded, total: installableIds.length, defaultValue: '{{count}} of {{total}} plugin operations completed' }))
      }
    } finally {
      setBulkUpdating(false)
    }
  }

  const handleUpdateAll = async (): Promise<void> => {
    setBulkUpdating(true)
    try {
      let succeeded = 0
      for (const pluginId of updatableIds) {
        if (await install(pluginId)) succeeded += 1
      }
      if (succeeded === updatableIds.length) {
        message.success(`Updated ${succeeded} plugin${succeeded === 1 ? '' : 's'}`)
      } else {
        message.warning(`${succeeded} of ${updatableIds.length} plugin operations completed`)
      }
    } finally {
      setBulkUpdating(false)
    }
  }

  return (
    <div className="udt-plugins-page">
      <header className="udt-plugins-page__header">
        <h1 className="udt-page-title">{t('plugins.title')}</h1>
        <p className="udt-page-description">{t('plugins.description', '安装和管理插件以扩展功能')}</p>
      </header>

      {offline && (
        <div className="udt-plugins-page__offline-banner">
          <ExclamationCircleOutlined className="udt-plugins-page__offline-icon" />
          <div className="udt-plugins-page__offline-copy">
            <div className="udt-plugins-page__offline-title">
              {t('plugins.storeUnavailable', '插件商店不可用')}
            </div>
            <div className="udt-plugins-page__offline-message">{t('plugins.offline')}</div>
          </div>
          <Button
            className="udt-btn-secondary"
            loading={loading}
            onClick={() => void refresh()}
          >
            {t('common.retry')}
          </Button>
        </div>
      )}

      {!offline && error && (
        <div className="udt-plugins-page__offline-banner">
          <ExclamationCircleOutlined className="udt-plugins-page__offline-icon udt-plugins-page__offline-icon--error" />
          <div className="udt-plugins-page__offline-copy">
            <div className="udt-plugins-page__offline-message">{error}</div>
          </div>
          <Button
            className="udt-btn-secondary"
            loading={loading}
            onClick={() => void refresh()}
          >
            {t('common.retry')}
          </Button>
        </div>
      )}

      <div className="udt-plugins-page__summary">
        <div className="udt-plugins-page__metric">
          <AppstoreOutlined />
          <span className="udt-plugins-page__metric-label">{t('plugins.summaryTotal', '插件总数')}</span>
          <span className="udt-plugins-page__metric-value">{plugins.length}</span>
        </div>
        <div className="udt-plugins-page__metric">
          <CheckCircleOutlined />
          <span className="udt-plugins-page__metric-label">{t('plugins.summaryInstalled', '已安装')}</span>
          <span className="udt-plugins-page__metric-value">{installedCount}</span>
        </div>
        <div className="udt-plugins-page__metric">
          <UpCircleOutlined />
          <span className="udt-plugins-page__metric-label">{t('plugins.summaryUpdates', '可更新')}</span>
          <span className="udt-plugins-page__metric-value">{updateCount}</span>
        </div>
      </div>

      <div className="udt-plugins-page__toolbar">
        <Input
          allowClear
          prefix={<SearchOutlined />}
          placeholder={t('plugins.search')}
          value={search}
          onChange={(event) => setSearch(event.target.value)}
        />
        <Select<FilterValue>
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
          className="udt-btn-secondary"
          icon={<FolderOpenOutlined />}
          loading={importing}
          onClick={() => void handleImport()}
        >
          {t('plugins.importFromFiles', '从文件导入')}
        </Button>
        <Tooltip title={t('plugins.refresh')}>
          <Button
            className="udt-btn-secondary udt-btn-icon"
            icon={<ReloadOutlined />}
            loading={loading}
            onClick={() => void refresh()}
          />
        </Tooltip>
        {updateCount > 0 && (
          <Button
            className="udt-btn-primary"
            icon={<UpCircleOutlined />}
            loading={bulkUpdating}
            onClick={() => void handleUpdateAll()}
          >
            {t('plugins.updateAll', '全部更新')}
          </Button>
        )}
        {installableIds.length > 0 && (
          <Button
            className="udt-btn-primary"
            icon={<DownloadOutlined />}
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
            <AppstoreOutlined className="udt-plugins-page__empty-icon" />
            <div className="udt-plugins-page__empty-title">{t('plugins.empty')}</div>
            <div className="udt-plugins-page__empty-description">
              {t('plugins.emptyStore', '插件商城目前为空，敬请期待未来的插件更新。')}
            </div>
          </div>
        ) : (
          <div className="udt-plugins-page__empty">
            <SearchOutlined className="udt-plugins-page__empty-icon" />
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
