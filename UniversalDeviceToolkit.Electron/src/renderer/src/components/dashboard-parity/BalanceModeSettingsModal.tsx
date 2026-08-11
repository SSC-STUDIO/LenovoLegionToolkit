import { useEffect, useState } from 'react'
import { Checkbox, Modal, Spin, message } from 'antd'
import { useTranslation } from 'react-i18next'
import { settingsApi } from '../../api/settings'
import { useFeaturesStore } from '../../stores/featuresStore'

/**
 * Parity modal for WPF Windows/Dashboard/BalanceModeSettingsWindow:
 * a single "Enable AI Engine" toggle persisted to balancemode.json plus
 * switching the power mode to Balance.
 */
interface BalanceModeSettingsModalProps {
  open: boolean
  onClose: () => void
  onSaved?: () => void
}

interface BalanceModeStore {
  aiModeEnabled: boolean
}

function parseStore(value: unknown): BalanceModeStore {
  const record = (value ?? {}) as Record<string, unknown>
  return { aiModeEnabled: record.AIModeEnabled === true || record.aiModeEnabled === true }
}

export default function BalanceModeSettingsModal({
  open,
  onClose,
  onSaved
}: BalanceModeSettingsModalProps): React.JSX.Element {
  const { t } = useTranslation()
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [aiModeEnabled, setAiModeEnabled] = useState(false)

  useEffect(() => {
    if (!open) return
    let cancelled = false
    settingsApi
      .get('balanceMode')
      .then((result) => {
        if (!cancelled) setAiModeEnabled(parseStore(result.value).aiModeEnabled)
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

  const handleSave = async (): Promise<void> => {
    setSaving(true)
    try {
      await settingsApi.set('balanceMode', { AIModeEnabled: aiModeEnabled })
      await settingsApi.save(['balanceMode'])
      await useFeaturesStore.getState().setState('powerMode', 'Balance')
      await useFeaturesStore.getState().refresh('powerMode')
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
      title={t('balanceMode.title')}
      width={400}
      okText={t('common.save')}
      cancelText={t('common.cancel')}
      confirmLoading={saving}
      onOk={() => void handleSave()}
      onCancel={onClose}
    >
      {loading ? (
        <div className="udt-dashboard-edit__loading">
          <Spin size="small" />
        </div>
      ) : (
        <div>
          <Checkbox
            checked={aiModeEnabled}
            onChange={(event) => setAiModeEnabled(event.target.checked)}
          >
            {t('balanceMode.aiEngine')}
          </Checkbox>
          <div className="udt-balance-mode__description">{t('balanceMode.aiEngineDesc')}</div>
        </div>
      )}
    </Modal>
  )
}
