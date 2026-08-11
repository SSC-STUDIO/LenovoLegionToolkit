import './custom.css'
import { useTranslation } from 'react-i18next'
import { LoadingOutlined } from '@ant-design/icons'
import { useLoadingStore } from '../stores/loadingStore'

/**
 * Global loading overlay — port of WPF Controls/Loading/LoadingChrome.xaml
 * (ILoadingChromeOwner). Renders when the LoadingStore has an active session:
 * spinner, label/message, optional determinate progress bar and cancel action.
 */

export default function LoadingOverlay(): React.JSX.Element | null {
  const { t } = useTranslation()
  const active = useLoadingStore((s) => s.active)

  if (active == null) return null

  const determinate = active.progress != null

  return (
    <div className="udt-loading-overlay" role="status" aria-live="polite">
      <div className="udt-loading-overlay__card">
        <LoadingOutlined style={{ fontSize: 40, color: 'var(--udt-accent-secondary)' }} />
        <div className="udt-loading-overlay__message">{active.label}</div>
        {active.message != null && active.message !== '' && (
          <div className="udt-loading-overlay__sub">{active.message}</div>
        )}
        {determinate && (
          <div className="udt-loading-overlay__progress">
            <div
              className="udt-loading-overlay__progress-bar"
              style={{ width: `${Math.min(100, Math.max(0, active.progress ?? 0))}%` }}
            />
          </div>
        )}
        {active.canCancel && (
          <button type="button" className="udt-btn udt-btn--secondary" onClick={() => useLoadingStore.getState().cancel(active.id)}>
            {t('common.cancel', { defaultValue: 'Cancel' })}
          </button>
        )}
      </div>
    </div>
  )
}
