import { useEffect, useState } from 'react'
import { Card, Col, Flex, Row, Typography, message } from 'antd'
import {
  AppstoreOutlined,
  DashboardOutlined,
  HomeOutlined,
  InfoCircleOutlined,
  KeyOutlined,
  MacCommandOutlined,
  RocketOutlined,
  SettingOutlined
} from '@ant-design/icons'
import { useTranslation } from 'react-i18next'
import { useNavigate } from 'react-router-dom'
import { invoke } from '../api/bridge'

interface HostReady {
  version?: string
  safeStart?: boolean
  pid?: number
}

interface HostInitialized {
  success?: boolean
  skippedSteps?: string[]
  error?: string
}

interface SystemInfo {
  vendor?: string
  model?: string
  machineType?: string
  isCompatible?: boolean
}

const NAV_ENTRIES = [
  { path: '/dashboard', icon: <HomeOutlined />, labelKey: 'nav.dashboard' },
  { path: '/keyboard', icon: <KeyOutlined />, labelKey: 'nav.keyboardBacklight' },
  { path: '/automation', icon: <RocketOutlined />, labelKey: 'nav.automation' },
  { path: '/macro', icon: <MacCommandOutlined />, labelKey: 'nav.macro' },
  { path: '/optimization', icon: <DashboardOutlined />, labelKey: 'nav.windowsOptimization' },
  { path: '/plugins', icon: <AppstoreOutlined />, labelKey: 'nav.pluginExtensions' },
  { path: '/settings', icon: <SettingOutlined />, labelKey: 'nav.settings' },
  { path: '/about', icon: <InfoCircleOutlined />, labelKey: 'nav.about' }
]

export default function HomePage(): React.JSX.Element {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const [hostStatus, setHostStatus] = useState<string>(() =>
    window.bridge ? t('common.loading') : t('common.error')
  )
  const [hostReady, setHostReady] = useState<HostReady | null>(null)
  const [hostInitialized, setHostInitialized] = useState<HostInitialized | null>(null)
  const [systemInfo, setSystemInfo] = useState<SystemInfo | null>(null)

  useEffect(() => {
    if (!window.bridge) {
      return
    }
    const offReady = window.bridge.on('host.ready', (data) => {
      const ready = data as HostReady
      setHostReady(ready)
      setHostStatus(t('home.hostReady'))
    })
    const offInitialized = window.bridge.on('host.initialized', (data) => {
      const init = data as HostInitialized
      setHostInitialized(init)
      if (init.success) {
        message.success(t('home.initComplete'))
      }
    })
    void invoke<SystemInfo>('system.info')
      .then(setSystemInfo)
      .catch(() => undefined)
    return () => {
      offReady()
      offInitialized()
    }
  }, [t])

  return (
    <Flex vertical gap={24}>
      <div>
        <Typography.Title level={3} style={{ margin: 0 }}>
          {t('home.title')}
        </Typography.Title>
        <Typography.Text type="secondary">{t('home.subtitle')}</Typography.Text>
      </div>

      <Row gutter={[16, 16]}>
        {NAV_ENTRIES.map((entry) => (
          <Col key={entry.path} xs={24} sm={12} md={12} lg={6}>
            <Card
              hoverable
              onClick={() => navigate(entry.path)}
              styles={{ body: { padding: 16 } }}
            >
              <Flex align="center" gap={12}>
                <span style={{ fontSize: 22, color: 'var(--ant-color-primary)' }}>
                  {entry.icon}
                </span>
                <Typography.Text strong>{t(entry.labelKey)}</Typography.Text>
              </Flex>
            </Card>
          </Col>
        ))}
      </Row>

      <Card title={t('home.status')} size="small">
        <Flex vertical gap={8}>
          <Typography.Text>
            {t('home.hostState')}: {hostStatus}
          </Typography.Text>
          {hostReady && (
            <Typography.Text type="secondary">
              {t('home.hostVersion')}: v{hostReady.version} (PID {hostReady.pid})
            </Typography.Text>
          )}
          {hostInitialized?.skippedSteps && hostInitialized.skippedSteps.length > 0 && (
            <Typography.Text type="warning">
              {t('home.safeStart')}: {hostInitialized.skippedSteps.join(', ')}
            </Typography.Text>
          )}
          {hostInitialized?.error && (
            <Typography.Text type="danger">{hostInitialized.error}</Typography.Text>
          )}
          {systemInfo && (
            <Typography.Text type="secondary">
              {t('home.machine')}: {systemInfo.vendor ?? ''} {systemInfo.model ?? ''} (
              {systemInfo.machineType ?? ''}) · {t('home.compatible')}:{' '}
              {systemInfo.isCompatible ? t('about.yes') : t('about.no')}
            </Typography.Text>
          )}
        </Flex>
      </Card>
    </Flex>
  )
}
