import { useEffect, useState } from 'react'
import { Button, List, Modal, Spin, Switch, message } from 'antd'
import { useTranslation } from 'react-i18next'
import { settingsApi } from '../../api/settings'
import { useSettingsStore } from '../../stores/settingsStore'

/**
 * Parity modal for Electron Windows/Settings/HardwareSensorSectionsWindow:
 * choose which sensor sections (CPU / Battery / GPU) are visible on the
 * dashboard and in which order. Persisted to the hardwareSensors scope
 * (VisibleSections / SectionOrder).
 */

interface HardwareSensorSectionsModalProps {
  open: boolean
  onClose: () => void
  onSaved?: () => void
}

const ALL_SECTIONS = ['CPU', 'Battery', 'GPU'] as const

type SensorSection = (typeof ALL_SECTIONS)[number]

function toTitleCase(value: string): string {
  return value.charAt(0).toUpperCase() + value.slice(1)
}

export default function HardwareSensorSectionsModal({
  open,
  onClose,
  onSaved
}: HardwareSensorSectionsModalProps): React.JSX.Element {
  const { t } = useTranslation()
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [visibility, setVisibility] = useState<Record<string, boolean>>({})
  const [order, setOrder] = useState<SensorSection[]>([])
  const [selected, setSelected] = useState<number | null>(null)

  useEffect(() => {
    if (!open) return
    let cancelled = false
    setLoading(true)
    settingsApi
      .get('hardwareSensors')
      .then((result) => {
        if (cancelled) return
        const store = (result.value ?? {}) as Record<string, unknown>
        const sectionOrder = Array.isArray(store.SectionOrder)
          ? (store.SectionOrder as unknown[]).filter((section): section is string => typeof section === 'string')
          : []
        const visibleSections = Array.isArray(store.VisibleSections)
          ? (store.VisibleSections as unknown[]).filter((section): section is string => typeof section === 'string')
          : ALL_SECTIONS

        const normalizedOrder = [
          ...sectionOrder.filter((section) =>
            (ALL_SECTIONS as readonly string[]).includes(section)
          ),
          ...ALL_SECTIONS.filter(
            (section) => !sectionOrder.some((entry) => entry === section)
          )
        ]

        const visible = new Set(visibleSections.map((section) => section.toUpperCase()))
        setVisibility(
          Object.fromEntries(ALL_SECTIONS.map((section) => [section, visible.has(section)]))
        )
        setOrder(normalizedOrder as SensorSection[])
        setSelected(normalizedOrder.length > 0 ? 0 : null)
      })
      .catch((reason: unknown) => {
        if (!cancelled) void message.error((reason as Error).message)
      })
      .finally(() => {
        if (!cancelled) setLoading(false)
      })
    return () => {
      cancelled = true
    }
  }, [open])

  const move = (delta: number): void => {
    if (selected == null) return
    const target = selected + delta
    if (target < 0 || target >= order.length) return
    setOrder((current) => {
      const next = [...current]
      const item = next[selected]
      if (item == null) return current
      next.splice(selected, 1)
      next.splice(target, 0, item)
      return next
    })
    setSelected(target)
  }

  const handleSave = async (): Promise<void> => {
    setSaving(true)
    try {
      const result = await settingsApi.get('hardwareSensors')
      const current = (result.value ?? {}) as Record<string, unknown>
      const visibleSections = ALL_SECTIONS.filter((section) => visibility[section] === true)
      const next = {
        ...current,
        VisibleSections: visibleSections.length > 0 ? visibleSections : [...ALL_SECTIONS],
        SectionOrder: order
      }
      useSettingsStore.getState().setScope('hardwareSensors', next)
      await settingsApi.set('hardwareSensors', next)
      await settingsApi.save(['hardwareSensors'])
      onSaved?.()
      onClose()
    } catch (reason) {
      void message.error((reason as Error).message)
    } finally {
      setSaving(false)
    }
  }

  return (
    <Modal
      open={open}
      title={t('wpf.hardwareSensorSectionsWindowtitle')}
      width={420}
      okText={t('saveButton')}
      cancelText={t('common.cancel')}
      confirmLoading={saving}
      onOk={() => void handleSave()}
      onCancel={onClose}
    >
      {loading ? (
        <div style={{ textAlign: 'center', padding: 24 }}>
          <Spin />
        </div>
      ) : (
        <div>
          <div style={{ fontWeight: 600, marginBottom: 8 }}>{t('sensorSectionsvisibletitle')}</div>
          {ALL_SECTIONS.map((section) => (
            <div
              key={section}
              style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: 8 }}
            >
              <span>{t(`sensorSection${toTitleCase(section)}`)}</span>
              <Switch
                className="udt-settings-switch"
                checked={visibility[section] === true}
                onChange={(checked) =>
                  setVisibility((current) => ({ ...current, [section]: checked }))
                }
              />
            </div>
          ))}
          <div style={{ fontWeight: 600, margin: '16px 0 8px' }}>{t('sensorSectionsordertitle')}</div>
          <List
            size="small"
            bordered
            style={{ maxHeight: 140, overflowY: 'auto' }}
            dataSource={order}
            renderItem={(section, index) => (
              <List.Item
                onClick={() => setSelected(index)}
                style={{
                  cursor: 'pointer',
                  background: selected === index ? 'rgba(22,119,255,0.12)' : undefined,
                  paddingLeft: 12
                }}
              >
                {t(`sensorSection${toTitleCase(section)}`)}
              </List.Item>
            )}
          />
          <div style={{ marginTop: 8 }}>
            <Button size="small" disabled={selected == null || selected <= 0} onClick={() => move(-1)}>
              {t('sensorSectionsmoveUp')}
            </Button>
            <Button
              size="small"
              style={{ marginLeft: 8 }}
              disabled={selected == null || selected >= order.length - 1}
              onClick={() => move(1)}
            >
              {t('sensorSectionsmoveDown')}
            </Button>
          </div>
        </div>
      )}
    </Modal>
  )
}
