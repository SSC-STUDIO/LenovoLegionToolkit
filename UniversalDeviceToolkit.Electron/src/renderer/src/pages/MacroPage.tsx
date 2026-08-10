import { useEffect, useState } from 'react'
import {
  Button,
  Card,
  Flex,
  Select,
  Space,
  Switch,
  Tag,
  Typography,
  message
} from 'antd'
import { useTranslation } from 'react-i18next'
import type { MacroEvent } from '../api/macro'
import { useMacroStore } from '../stores/macroStore'

const NUMPAD_KEYS = [
  { label: '0', code: 0x60 },
  { label: '1', code: 0x61 },
  { label: '2', code: 0x62 },
  { label: '3', code: 0x63 },
  { label: '4', code: 0x64 },
  { label: '5', code: 0x65 },
  { label: '6', code: 0x66 },
  { label: '7', code: 0x67 },
  { label: '8', code: 0x68 },
  { label: '9', code: 0x69 }
]

const REPEAT_OPTIONS = Array.from({ length: 10 }, (_, i) => ({ value: i + 1, label: `${i + 1}` }))

export default function MacroPage(): React.JSX.Element {
  const { t } = useTranslation()
  const { state, load, setEnabled, play, saveSequence, clearSequence } = useMacroStore()
  const [selectedKey, setSelectedKey] = useState<number>(0x61)
  const [repeatCount, setRepeatCount] = useState(1)
  const [events, setEvents] = useState<MacroEvent[]>([])
  const [savedEvents, setSavedEvents] = useState<MacroEvent[]>([])

  useEffect(() => {
    void load()
  }, [load])

  useEffect(() => {
    const slot = state?.slots?.find((s: { key?: number }) => s.key === selectedKey)
    setSavedEvents((slot?.events as MacroEvent[]) ?? [])
  }, [state, selectedKey])

  const handleSave = async (): Promise<void> => {
    try {
      await saveSequence({
        key: selectedKey,
        repeatCount,
        ignoreDelays: false,
        interruptOnOtherKey: false,
        events
      })
      setSavedEvents(events)
      message.success(t('settings.saved'))
    } catch (error) {
      message.error((error as Error).message)
    }
  }

  const handleClear = async (): Promise<void> => {
    try {
      await clearSequence(selectedKey)
      setEvents([])
      setSavedEvents([])
      void load()
    } catch (error) {
      message.error((error as Error).message)
    }
  }

  return (
    <Flex vertical gap={16} style={{ padding: 24 }}>
      <Flex align="center" justify="space-between">
        <Typography.Title level={3} style={{ margin: 0 }}>
          {t('macro.title')}
        </Typography.Title>
        <Space>
          <span>{t('macro.enable')}</span>
          <Switch
            checked={state?.isEnabled ?? false}
            onChange={(checked) => void setEnabled(checked)}
          />
        </Space>
      </Flex>

      <Flex gap={16} wrap>
        <Card title={t('macro.numpad')} style={{ width: 260 }}>
          <Flex gap={8} wrap justify="center">
            {NUMPAD_KEYS.map((key) => (
              <Button
                key={key.code}
                type={selectedKey === key.code ? 'primary' : 'default'}
                style={{ width: 56, height: 56, fontSize: 18 }}
                onClick={() => setSelectedKey(key.code)}
              >
                {key.label}
              </Button>
            ))}
          </Flex>
        </Card>

        <Card
          title={`${t('macro.sequence')} - ${NUMPAD_KEYS.find((k) => k.code === selectedKey)?.label}`}
          style={{ flex: 1, minWidth: 420 }}
        >
          <Space direction="vertical" style={{ width: '100%' }} size="middle">
            <Flex gap={12} align="center">
              <span>{t('macro.repeat')}</span>
              <Select
                style={{ width: 90 }}
                value={repeatCount}
                options={REPEAT_OPTIONS}
                onChange={setRepeatCount}
              />
              <Tag color="blue">{events.length}</Tag>
            </Flex>

            <Flex gap={8}>
              <Button type="primary" onClick={() => void handleSave()}>
                {t('macro.save')}
              </Button>
              <Button danger onClick={() => void handleClear()}>
                {t('macro.clear')}
              </Button>
              <Button onClick={() => void play(selectedKey)}>{t('macro.play')}</Button>
            </Flex>

            <Typography.Text type="secondary">
              {t('macro.events')}: {events.length > 0 ? events.length : savedEvents.length}
            </Typography.Text>
            {(events.length > 0 ? events : savedEvents).length === 0 && (
              <Typography.Text type="secondary">{t('macro.empty')}</Typography.Text>
            )}
          </Space>
        </Card>
      </Flex>
    </Flex>
  )
}
