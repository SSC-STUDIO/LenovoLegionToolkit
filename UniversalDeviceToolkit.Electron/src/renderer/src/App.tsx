import { useEffect, useState } from 'react'
import { Button, Card, Space, Typography } from 'antd'
import { Route, Routes } from 'react-router-dom'

interface HostReady {
  version?: string
  safeStart?: boolean
  pid?: number
}

function Home(): React.JSX.Element {
  const [result, setResult] = useState<string>('')
  const [hostStatus, setHostStatus] = useState<string>('connecting...')

  useEffect(() => {
    if (!window.bridge) return
    const off = window.bridge.on('host.ready', (data) => {
      const ready = data as HostReady
      setHostStatus(`host ready (pid ${ready.pid}, v${ready.version})`)
    })
    return off
  }, [])

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
        <Typography.Title level={2}>UDT Electron</Typography.Title>
        <Typography.Text type="secondary">{hostStatus}</Typography.Text>
        <Button type="primary" onClick={() => void handlePing()}>
          Ping Host Process
        </Button>
        {result && <Typography.Text code>{result}</Typography.Text>}
      </Space>
    </Card>
  )
}

export default function App(): React.JSX.Element {
  return (
    <Routes>
      <Route path="/" element={<Home />} />
    </Routes>
  )
}
