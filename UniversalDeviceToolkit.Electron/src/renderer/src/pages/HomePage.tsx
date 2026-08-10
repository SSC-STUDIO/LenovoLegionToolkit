import { useEffect, useState } from 'react'
import { Button, Card, Space, Typography } from 'antd'
import { useTranslation } from 'react-i18next'

interface HostReady {
  version?: string
  safeStart?: boolean
  pid?: number
}

export default function HomePage(): React.JSX.Element {
  const { t } = useTranslation()
  const [result, setResult] = useState('')
  const [hostStatus, setHostStatus] = useState(t('common.loading'))

  useEffect(() => {
    if (!window.bridge) {
      setHostStatus(t('common.error'))
      return
    }
    const off = window.bridge.on('host.ready', (data) => {
      const ready = data as HostReady
      setHostStatus(`host ready (pid ${ready.pid}, v${ready.version})`)
    })
    return off
  }, [t])

  const handlePing = async (): Promise<void> => {
    if (!window.bridge) {
      setResult('bridge not available')
      return
    }
    try {
      const res = (await window.bridge.invoke('ping', {})) as { pong?: boolean; version?: string }
      setResult(`pong=${res.pong} host=${res.version}`)
    } catch (error) {
      setResult(`error: ${(error as Error).message}`)
    }
  }

  return (
    <Card style={{ maxWidth: 560, margin: '64px auto' }}>
      <Space direction="vertical" size="middle">
        <Typography.Title level={2}>{t('home.title')}</Typography.Title>
        <Typography.Text type="secondary">{t('home.subtitle')}</Typography.Text>
        <Typography.Text type="secondary">{hostStatus}</Typography.Text>
        <Button type="primary" onClick={() => void handlePing()}>
          Ping Host Process
        </Button>
        {result && <Typography.Text code>{result}</Typography.Text>}
      </Space>
    </Card>
  )
}
