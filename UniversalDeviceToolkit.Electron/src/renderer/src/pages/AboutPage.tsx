import { useEffect, useState } from 'react'
import { Card, Descriptions, Flex, Typography } from 'antd'
import { useTranslation } from 'react-i18next'
import { invoke } from '../api/bridge'
import { settingsApi } from '../api/settings'

interface SystemInfo {
  vendor?: string
  model?: string
  machineType?: string
  biosVersion?: string | null
  isCompatible?: boolean
}

interface AppStatus {
  pid?: number
  version?: string
  logPath?: string
}

const THIRD_PARTY_LIBS = [
  'Autofac',
  'Serilog',
  'LibreHardwareMonitorLib',
  'ManagedNativeWifi',
  'NAudio.Wasapi',
  'WindowsDisplayAPI',
  'System.Management',
  'Octokit',
  'Markdig',
  'Humanizer'
]

export default function AboutPage(): React.JSX.Element {
  const { t } = useTranslation()
  const [appStatus, setAppStatus] = useState<AppStatus | null>(null)
  const [systemInfo, setSystemInfo] = useState<SystemInfo | null>(null)
  const [dataFolder, setDataFolder] = useState<string>('')

  useEffect(() => {
    void invoke<AppStatus>('app.getStatus').then(setAppStatus).catch(() => undefined)
    void invoke<SystemInfo>('system.info').then(setSystemInfo).catch(() => undefined)
    void settingsApi.get('application').catch(() => undefined)
  }, [])

  useEffect(() => {
    const maybeFolder = (appStatus as { logPath?: string } | null)?.logPath
    if (maybeFolder) {
      setDataFolder(maybeFolder.replace(/[\\/][^\\/]*$/, ''))
    }
  }, [appStatus])

  return (
    <Flex vertical gap={16} style={{ maxWidth: 720 }}>
      <Typography.Title level={3}>{t('about.title')}</Typography.Title>

      <Card>
        <Descriptions column={1} size="small">
          <Descriptions.Item label={t('about.appName')}>
            Universal Device Toolkit
          </Descriptions.Item>
          <Descriptions.Item label={t('about.version')}>
            {appStatus?.version ?? '...'}
          </Descriptions.Item>
          <Descriptions.Item label={t('about.pid')}>{appStatus?.pid ?? '...'}</Descriptions.Item>
          <Descriptions.Item label={t('about.machine')}>
            {systemInfo
              ? `${systemInfo.vendor ?? ''} ${systemInfo.model ?? ''} (${systemInfo.machineType ?? ''})`
              : '...'}
          </Descriptions.Item>
          <Descriptions.Item label={t('about.bios')}>
            {systemInfo?.biosVersion ?? '...'}
          </Descriptions.Item>
          <Descriptions.Item label={t('about.compatible')}>
            {systemInfo ? (systemInfo.isCompatible ? t('about.yes') : t('about.no')) : '...'}
          </Descriptions.Item>
          <Descriptions.Item label={t('about.dataFolder')}>
            {dataFolder || '...'}
          </Descriptions.Item>
        </Descriptions>
      </Card>

      <Card title={t('about.thirdParty')}>
        <Flex gap={8} wrap>
          {THIRD_PARTY_LIBS.map((lib) => (
            <Typography.Text key={lib} code>
              {lib}
            </Typography.Text>
          ))}
        </Flex>
      </Card>

      <Typography.Text type="secondary">
        {t('about.copyright')} © SSC-STUDIO
      </Typography.Text>
    </Flex>
  )
}
