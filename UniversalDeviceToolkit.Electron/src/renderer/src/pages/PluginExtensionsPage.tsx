import { useEffect, useMemo, useState } from 'react'
import {
  Alert,
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
import { ReloadOutlined, SearchOutlined } from '@ant-design/icons'
import { useTranslation } from 'react-i18next'
import type { PluginView } from '../api/plugins'
import { usePluginsStore } from '../stores/pluginsStore'

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
      size="small"
      title={
        <Space size={8} wrap>
          <Typography.Text strong>{plugin.name}</Typography.Text>
          <Typography.Text type="secondary">v{plugin.version}</Typography.Text>
          <PluginStatusTag plugin={plugin} />
          {plugin.isSystemPlugin && <Tag color="gold">System</Tag>}
        </Space>
      }
      extra={actions}
    >
      <Typography.Paragraph type="secondary" style={{ marginBottom: 8 }}>
        {plugin.description}
      </Typography.Paragraph>
      {(plugin.tags.length > 0 || plugin.dependencies.length > 0) && (
        <Space size={[4, 4]} wrap style={{ marginBottom: 8 }}>
          {plugin.tags.map((tag) => (
            <Tag key={tag}>{tag}</Tag>
          ))}
          {plugin.dependencies.length > 0 && (
            <Tag color="geekblue">
              {t('plugins.dependencies')}: {plugin.dependencies.join(', ')}
            </Tag>
          )}
        </Space>
      )}
      {installing && <Progress percent={progress} size="small" />}
      {collapseItems.length > 0 && <Collapse ghost size="small" items={collapseItems} />}
    </Card>
  )
}

export default function PluginExtensionsPage(): React.JSX.Element {
  const { t } = useTranslation()
  const { plugins, loading, offline, error, load, refresh } = usePluginsStore()
  const [search, setSearch] = useState('')
  const [filter, setFilter] = useState<FilterValue>('all')

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

  return (
    <Flex vertical gap={16}>
      <Flex align="center" justify="space-between" wrap gap={8}>
        <Typography.Title level={3} style={{ margin: 0 }}>
          {t('plugins.title')}
        </Typography.Title>
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

      <Flex gap={8} wrap>
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
      </Flex>

      {offline && <Alert type="warning" showIcon message={t('plugins.offline')} />}
      {error && <Alert type="error" showIcon message={error} />}

      {filtered.length === 0 ? (
        <Empty description={t('plugins.empty')} />
      ) : (
        <List
          loading={loading}
          dataSource={filtered}
          renderItem={(plugin) => <PluginCard key={plugin.id} plugin={plugin} />}
        />
      )}
    </Flex>
  )
}
