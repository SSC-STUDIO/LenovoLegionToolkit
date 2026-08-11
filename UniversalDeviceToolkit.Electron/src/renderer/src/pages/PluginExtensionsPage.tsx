import { useEffect, useMemo, useState } from 'react'
import {
  Alert,
  Avatar,
  Button,
  Card,
  Collapse,
  Empty,
  Flex,
  Input,
  List,
  Popconfirm,
  Progress,
  Select,
  Space,
  Tag,
  Typography,
  message
} from 'antd'
import { DownloadOutlined, ReloadOutlined, SearchOutlined, UploadOutlined } from '@ant-design/icons'
import { useTranslation } from 'react-i18next'
import type { PluginView } from '../api/plugins'
import { usePluginsStore } from '../stores/pluginsStore'
import './PluginExtensionsPage.css'

type FilterValue = 'all' | 'installed' | 'notInstalled'

function PluginStatusTag({ plugin }: { plugin: PluginView }): React.JSX.Element {
  const { t } = useTranslation()
  if (plugin.installedVersion) {
    return plugin.updateAvailable ? (
      <Tag color="warning">{t('plugins.updateAvailable')}</Tag>
    ) : (
      <Tag color="success">{t('plugins.installed')}</Tag>
    )
  }
  return <Tag color="blue">{t('plugins.online')}</Tag>
}

function PluginCard({ plugin }: { plugin: PluginView }): React.JSX.Element {
  const { t } = useTranslation()
  const install = usePluginsStore((state) => state.install)
  const uninstall = usePluginsStore((state) => state.uninstall)
  const installingIds = usePluginsStore((state) => state.installingIds)

  const installing = plugin.id in installingIds
  const progress = installingIds[plugin.id] ?? 0

  const collapseItems = useMemo(() => {
    const items: { key: string; label: string; children: React.JSX.Element }[] = []
    if (plugin.details) {
      items.push({
        key: 'details',
        label: t('plugins.details'),
        children: <Typography.Paragraph style={{ marginBottom: 0 }}>{plugin.details}</Typography.Paragraph>
      })
    }
    if (plugin.usageGuide) {
      items.push({
        key: 'usageGuide',
        label: t('plugins.usageGuide'),
        children: <Typography.Paragraph style={{ marginBottom: 0 }}>{plugin.usageGuide}</Typography.Paragraph>
      })
    }
    if (plugin.changelog) {
      items.push({
        key: 'changelog',
        label: t('plugins.changelog'),
        children: <Typography.Paragraph style={{ marginBottom: 0 }}>{plugin.changelog}</Typography.Paragraph>
      })
    }
    return items
  }, [plugin, t])

  const handleUninstall = async (): Promise<void> => {
    const result = await uninstall(plugin.id)
    if (result.dependencyBlocked) {
      message.warning(t('plugins.dependenciesBlocked'))
    } else if (!result.ok) {
      message.error(t('plugins.uninstallFailed'))
    }
  }

  const actions = (
    <Space>
      {installing ? (
        <Typography.Text type="secondary">{t('plugins.installing')}</Typography.Text>
      ) : (
        <>
          {plugin.installedVersion ? (
            plugin.updateAvailable && (
              <Button type="primary" size="small" onClick={() => void install(plugin.id)}>
                {t('plugins.update')}
              </Button>
            )
          ) : (
            <Button type="primary" size="small" onClick={() => void install(plugin.id)}>
              {t('plugins.install')}
            </Button>
          )}
          {plugin.installedVersion && (
            <Popconfirm
              title={t('plugins.uninstallConfirm')}
              onConfirm={() => void handleUninstall()}
            >
              <Button size="small" danger>
                {t('plugins.uninstall')}
              </Button>
            </Popconfirm>
          )}
        </>
      )}
    </Space>
  )

  return (
    <Card
      className="udt-plugin-row"
      size="small"
      title={
        <div className="udt-plugin-row__heading">
          <Avatar shape="square" size={66} style={{ background: plugin.iconBackground ?? '#416aa1' }}>
            {plugin.name.split(/\s+/).map((word) => word[0]).join('').slice(0, 2).toUpperCase()}
          </Avatar>
          <div className="udt-plugin-row__heading-copy">
            <Space size={8} wrap>
              <Typography.Text strong>{plugin.name}</Typography.Text>
              <Typography.Text type="secondary">v{plugin.version}</Typography.Text>
              <PluginStatusTag plugin={plugin} />
              {plugin.isSystemPlugin && <Tag color="gold">System</Tag>}
            </Space>
            {(plugin.tags.length > 0 || plugin.dependencies.length > 0) && (
              <Space size={[6, 6]} wrap className="udt-plugin-row__tags">
                {plugin.tags.map((tag) => <Tag key={tag}>{tag}</Tag>)}
                {plugin.dependencies.length > 0 && (
                  <Tag color="geekblue">{t('plugins.dependencies')}: {plugin.dependencies.join(', ')}</Tag>
                )}
              </Space>
            )}
          </div>
        </div>
      }
      extra={actions}
    >
      <Typography.Paragraph className="udt-plugin-row__description" type="secondary">
        {plugin.description}
      </Typography.Paragraph>
      {installing && <Progress percent={progress} size="small" />}
      {collapseItems.length > 0 && <Collapse ghost size="small" items={collapseItems} />}
    </Card>
  )
}

export default function PluginExtensionsPage(): React.JSX.Element {
  const { t } = useTranslation()
  const { plugins, loading, offline, error, load, refresh, install, importFile } = usePluginsStore()
  const [search, setSearch] = useState('')
  const [filter, setFilter] = useState<FilterValue>('all')
  const [bulkAction, setBulkAction] = useState<'import' | 'install' | 'update' | null>(null)

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
        plugin.tags.some((tag) => tag.toLowerCase().includes(query))
      )
    })
  }, [plugins, search, filter])

  const installedCount = plugins.filter((plugin) => plugin.installedVersion).length
  const updateCount = plugins.filter((plugin) => plugin.updateAvailable).length

  const runBulkAction = async (
    action: 'import' | 'install' | 'update',
    pluginIds: string[] = []
  ): Promise<void> => {
    setBulkAction(action)
    try {
      if (action === 'import') {
        const files = await window.bridge?.selectPluginFiles() ?? []
        if (files.length === 0) return
        const results = await Promise.all(files.map((file) => importFile(file)))
        if (results.every(Boolean)) {
          message.success(`Imported ${files.length} plugin package${files.length === 1 ? '' : 's'}`)
        } else {
          message.warning('Some plugin packages could not be imported')
        }
        return
      }

      let succeeded = 0
      for (const pluginId of pluginIds) {
        if (await install(pluginId)) succeeded += 1
      }
      if (succeeded === pluginIds.length) {
        message.success(`${action === 'update' ? 'Updated' : 'Installed'} ${succeeded} plugin${succeeded === 1 ? '' : 's'}`)
      } else {
        message.warning(`${succeeded} of ${pluginIds.length} plugin operations completed`)
      }
    } finally {
      setBulkAction(null)
    }
  }

  const installableIds = plugins.filter((plugin) => !plugin.installedVersion).map((plugin) => plugin.id)
  const updatableIds = plugins.filter((plugin) => plugin.updateAvailable).map((plugin) => plugin.id)

  return (
    <Flex vertical gap={16} className="udt-plugins-page">
      <Flex align="center" justify="space-between" wrap gap={8} className="udt-plugins-page__header">
        <div>
          <Typography.Title level={3} className="udt-plugins-page__title">
          {t('plugins.title')}
          </Typography.Title>
          <Typography.Text className="udt-plugins-page__description">
            Browse, manage, and update installed extensions.
          </Typography.Text>
        </div>
        <Space wrap>
          <Typography.Text type="secondary">
            {t('plugins.total', { count: plugins.length })} ·{' '}
            {t('plugins.summary', { count: installedCount })} ·{' '}
            {t('plugins.updatable', { count: updateCount })}
          </Typography.Text>
          <Button
            icon={<ReloadOutlined />}
            loading={loading}
            onClick={() => void refresh()}
          >
            {t('plugins.refresh')}
          </Button>
        </Space>
      </Flex>

      <Flex gap={8} wrap className="udt-plugins-page__toolbar">
        <Input
          allowClear
          prefix={<SearchOutlined />}
          placeholder={t('plugins.search')}
          value={search}
          onChange={(event) => setSearch(event.target.value)}
          style={{ maxWidth: 320 }}
        />
        <Select<FilterValue>
          value={filter}
          onChange={setFilter}
          style={{ width: 160 }}
          options={[
            { value: 'all', label: t('plugins.filterAll') },
            { value: 'installed', label: t('plugins.filterInstalled') },
            { value: 'notInstalled', label: t('plugins.filterNotInstalled') }
          ]}
        />
        <div className="udt-plugins-page__toolbar-spacer" />
        <Button
          icon={<UploadOutlined />}
          loading={bulkAction === 'import'}
          onClick={() => void runBulkAction('import')}
        >
          Import files
        </Button>
        <Button
          icon={<DownloadOutlined />}
          disabled={installableIds.length === 0 || bulkAction != null}
          loading={bulkAction === 'install'}
          onClick={() => void runBulkAction('install', installableIds)}
        >
          Install all
        </Button>
        <Button
          type="primary"
          icon={<ReloadOutlined />}
          disabled={updatableIds.length === 0 || bulkAction != null}
          loading={bulkAction === 'update'}
          onClick={() => void runBulkAction('update', updatableIds)}
        >
          Update all
        </Button>
      </Flex>

      {offline && <Alert type="warning" showIcon message={t('plugins.offline')} />}
      {error && <Alert type="error" showIcon message={error} />}

      {filtered.length === 0 ? (
        <Empty description={t('plugins.empty')} />
      ) : (
        <List
          className="udt-plugins-page__list"
          loading={loading}
          dataSource={filtered}
          renderItem={(plugin) => <PluginCard key={plugin.id} plugin={plugin} />}
        />
      )}
    </Flex>
  )
}
