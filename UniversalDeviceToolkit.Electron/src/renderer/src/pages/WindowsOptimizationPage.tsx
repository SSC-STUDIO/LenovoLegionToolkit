import { useEffect, useState } from 'react'
import {
  Button,
  Card,
  Checkbox,
  Col,
  Divider,
  Empty,
  Flex,
  Popconfirm,
  Row,
  Select,
  Space,
  Spin,
  Switch,
  Tabs,
  Tag,
  Typography,
  message
} from 'antd'
import { useTranslation } from 'react-i18next'
import type {
  NetworkAccelerationConfig,
  NetworkAccelerationMode,
  OptimizationActionDefinition,
  OptimizationCategoryDefinition
} from '../api/optimization'
import { useOptimizationStore } from '../stores/optimizationStore'

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
    const keys = optimizationCategories.flatMap((category) =>
      category.actions.filter((action) => action.recommended).map((action) => action.key)
    )
    setSelectedKeys(keys)
  }

  const handleApply = async (): Promise<void> => {
    if (selectedKeys.length === 0) return
    setBusy(true)
    const ok = await apply(selectedKeys)
    setBusy(false)
    if (ok) {
      setSelectedKeys([])
      message.success(t('optimization.applied'))
    } else {
      message.error(t('optimization.applyFailed'))
    }
  }

  const handleClear = async (): Promise<void> => {
    if (selectedKeys.length === 0) return
    setBusy(true)
    const ok = await revert(selectedKeys)
    setBusy(false)
    if (ok) {
      setSelectedKeys([])
      message.success(t('optimization.reverted'))
    } else {
      message.error(t('optimization.revertFailed'))
    }
  }

  const handleApplyRecommended = async (): Promise<void> => {
    setBusy(true)
    const ok = await applyRecommended()
    setBusy(false)
    if (ok) message.success(t('optimization.applied'))
    else message.error(t('optimization.applyFailed'))
  }

  return (
    <Row gutter={16}>
      <Col xs={24} lg={12}>
        <Flex vertical gap={12}>
          {loading && <Spin />}
          {optimizationCategories.map((category) => (
            <Card key={category.key} size="small" title={category.title}>
              <Typography.Paragraph type="secondary" style={{ marginBottom: 8 }}>
                {category.description}
              </Typography.Paragraph>
              <Flex vertical gap={4}>
                {category.actions.map((action) => {
                  const selected = selectedKeys.includes(action.key)
                  return (
                    <Flex key={action.key} align="center" justify="space-between">
                      <Checkbox
                        checked={action.applied === true}
                        indeterminate={action.applied === null}
                        onChange={() => toggleSelection(action.key)}
                      >
                        {action.title}
                      </Checkbox>
                      <Space size={4}>
                        {action.recommended && <Tag color="gold">★ {t('optimization.recommended')}</Tag>}
                        {selected && <Tag color="blue">{t('optimization.selected')}</Tag>}
                      </Space>
                    </Flex>
                  )
                })}
              </Flex>
            </Card>
          ))}
        </Flex>
      </Col>
      <Col xs={24} lg={12}>
        <Card
          size="small"
          title={t('optimization.selectedActions')}
          extra={<Typography.Text type="secondary">{selectedActions.length}</Typography.Text>}
        >
          {selectedActions.length === 0 ? (
            <Empty description={t('optimization.noSelection')} />
          ) : (
            <Flex vertical gap={8}>
              {selectedActions.map((action) => (
                <Flex key={action.key} align="center" justify="space-between">
                  <Typography.Text>{action.title}</Typography.Text>
                  {action.recommended && <Tag color="gold">★</Tag>}
                </Flex>
              ))}
              <Divider style={{ margin: '8px 0' }} />
              <Flex gap={8} wrap>
                <Button onClick={handleSelectRecommended}>{t('optimization.selectRecommended')}</Button>
                <Button
                  type="primary"
                  loading={busy}
                  disabled={selectedActions.length === 0}
                  onClick={() => void handleApply()}
                >
                  {t('optimization.apply')}
                </Button>
                <Button
                  danger
                  loading={busy}
                  disabled={selectedActions.length === 0}
                  onClick={() => void handleClear()}
                >
                  {t('optimization.clear')}
                </Button>
                <Button onClick={() => void handleApplyRecommended()}>
                  {t('optimization.applyRecommended')}
                </Button>
              </Flex>
            </Flex>
          )}
        </Card>
      </Col>
    </Row>
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
      message.success(t('optimization.cleanupDone'))
      setSelectedKeys([])
      setEstimateBytes(null)
    } else {
      message.error(t('optimization.cleanupFailed'))
    }
  }

  return (
    <Flex vertical gap={16}>
      <Typography.Paragraph type="secondary">{t('optimization.cleanupHint')}</Typography.Paragraph>
      {cleanupCategories.map((category) => (
        <Card key={category.key} size="small" title={category.title}>
          <Typography.Paragraph type="secondary" style={{ marginBottom: 8 }}>
            {category.description}
          </Typography.Paragraph>
          <Flex vertical gap={4}>
            {category.actions.map((action) => (
              <Checkbox
                key={action.key}
                checked={selectedKeys.includes(action.key)}
                onChange={() => toggleSelection(action.key)}
              >
                {action.title}
              </Checkbox>
            ))}
          </Flex>
        </Card>
      ))}
      <Flex align="center" gap={12} wrap>
        <Button loading={estimating} disabled={selectedKeys.length === 0} onClick={() => void handleEstimate()}>
          {t('optimization.estimate')}
        </Button>
        {estimateBytes !== null && (
          <Typography.Text strong>
            {t('optimization.estimateResult')}: {formatBytes(estimateBytes)}
          </Typography.Text>
        )}
        <Popconfirm title={t('optimization.cleanupConfirm')} onConfirm={() => void handleRun()}>
          <Button type="primary" danger loading={cleaning} disabled={selectedKeys.length === 0}>
            {t('optimization.runCleanup')}
          </Button>
        </Popconfirm>
      </Flex>
    </Flex>
  )
}

function DriverDownloadTab(): React.JSX.Element {
  const { t } = useTranslation()
  return (
    <Card>
      <Empty description={t('optimization.driverDownload.comingSoon')} />
    </Card>
  )
}

const NETWORK_MODES: NetworkAccelerationMode[] = ['Off', 'SystemProxy', 'Hosts', 'DiagnosticsOnly']

const NETWORK_MODE_I18N_KEYS: Record<NetworkAccelerationMode, string> = {
  Off: 'optimization.network.modes.off',
  SystemProxy: 'optimization.network.modes.systemProxy',
  Hosts: 'optimization.network.modes.hosts',
  DiagnosticsOnly: 'optimization.network.modes.diagnosticsOnly'
}

function NetworkTab(): React.JSX.Element {
  const { t } = useTranslation()
  const networkStatus = useOptimizationStore((s) => s.networkStatus)
  const saveNetworkConfig = useOptimizationStore((s) => s.saveNetworkConfig)
  const startNetwork = useOptimizationStore((s) => s.startNetwork)
  const stopNetwork = useOptimizationStore((s) => s.stopNetwork)
  const [config, setConfig] = useState<NetworkAccelerationConfig | null>(null)
  const [saving, setSaving] = useState(false)
  const [starting, setStarting] = useState(false)
  const [stopping, setStopping] = useState(false)

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
    const ok = await saveNetworkConfig(current)
    setSaving(false)
    if (ok) message.success(t('optimization.network.saved'))
    else message.error(t('optimization.network.saveFailed'))
  }

  const handleStart = async (): Promise<void> => {
    setStarting(true)
    const ok = await startNetwork()
    setStarting(false)
    if (!ok) message.error(t('optimization.network.startFailed'))
  }

  const handleStop = async (): Promise<void> => {
    setStopping(true)
    const ok = await stopNetwork()
    setStopping(false)
    if (!ok) message.error(t('optimization.network.stopFailed'))
  }

  if (!networkStatus || !editableConfig) return <Spin />

  const updateConfig = (patch: Partial<NetworkAccelerationConfig>): void => {
    const current = ensureConfig()
    if (!current) return
    setConfig({ ...current, ...patch })
  }

  return (
    <Flex vertical gap={16}>
      <Card size="small" title={t('optimization.network.status')}>
        <Flex gap={16} align="center" wrap>
          <Tag color={networkStatus.isRunning ? 'green' : 'default'}>
            {networkStatus.isRunning ? t('optimization.network.running') : t('optimization.network.stopped')}
          </Tag>
          <Typography.Text type="secondary">{networkStatus.statusText}</Typography.Text>
          <Tag color={networkStatus.isBackendReady ? 'blue' : 'red'}>
            {networkStatus.isBackendReady
              ? t('optimization.network.backendReady')
              : t('optimization.network.backendNotReady')}
          </Tag>
        </Flex>
      </Card>

      <Card size="small" title={t('optimization.network.config')}>
        <Flex vertical gap={12}>
          <Flex align="center" gap={8}>
            <Typography.Text>{t('optimization.network.accelerationEnabled')}</Typography.Text>
            <Switch
              checked={editableConfig.accelerationEnabled}
              onChange={(checked) => updateConfig({ accelerationEnabled: checked })}
            />
          </Flex>
          <Flex align="center" gap={8}>
            <Typography.Text>{t('optimization.network.mode')}</Typography.Text>
            <Select<NetworkAccelerationMode>
              style={{ width: 220 }}
              value={editableConfig.mode}
              options={NETWORK_MODES.map((mode) => ({
                value: mode,
                label: t(NETWORK_MODE_I18N_KEYS[mode])
              }))}
              onChange={(mode) => updateConfig({ mode })}
            />
          </Flex>
          <Flex gap={8} wrap>
            <Button type="primary" loading={saving} onClick={() => void handleSave()}>
              {t('optimization.network.save')}
            </Button>
            <Button
              loading={starting}
              disabled={!networkStatus.isBackendReady}
              onClick={() => void handleStart()}
            >
              {t('optimization.network.start')}
            </Button>
            <Button danger loading={stopping} onClick={() => void handleStop()}>
              {t('optimization.network.stop')}
            </Button>
          </Flex>
        </Flex>
      </Card>
    </Flex>
  )
}

export default function WindowsOptimizationPage(): React.JSX.Element {
  const { t } = useTranslation()
  const load = useOptimizationStore((s) => s.load)
  const loadNetwork = useOptimizationStore((s) => s.loadNetwork)

  useEffect(() => {
    void load()
    void loadNetwork()
  }, [load, loadNetwork])

  return (
    <Flex vertical gap={16}>
      <Typography.Title level={3} style={{ margin: 0 }}>
        {t('optimization.title')}
      </Typography.Title>
      <Tabs
        items={[
          {
            key: 'optimization',
            label: t('optimization.tabs.optimization'),
            children: <OptimizationTab />
          },
          {
            key: 'cleanup',
            label: t('optimization.tabs.cleanup'),
            children: <CleanupTab />
          },
          {
            key: 'driverDownload',
            label: t('optimization.tabs.driverDownload'),
            children: <DriverDownloadTab />
          },
          {
            key: 'networkAcceleration',
            label: t('optimization.tabs.networkAcceleration'),
            children: <NetworkTab />
          }
        ]}
      />
    </Flex>
  )
}
