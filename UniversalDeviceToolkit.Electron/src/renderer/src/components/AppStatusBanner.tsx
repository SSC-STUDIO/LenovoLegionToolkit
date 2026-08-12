import { CloseOutlined, SyncOutlined, WarningFilled } from '@ant-design/icons'
import './AppStatusBanner.css'

/**
 * Persistent corner toast — port of Electron Controls/Shell/AppStatusBanner.xaml(.cs).
 * Hosted in the bottom-right overlay stack (MainWindow._statusNotificationStack),
 * not as a full-width top-of-page bar. Severity: Warning → warning icon;
 * Success → sync icon + clickable action (Electron ArrowSync24 / ActionArea).
 * Closed is raised only from the close button.
 */

export type AppStatusBannerSeverity = 'Warning' | 'Success'

export interface AppStatusBannerProps {
  severity?: AppStatusBannerSeverity
  message: string
  closable?: boolean
  onClosed?: () => void
  onClick?: () => void
}

export default function AppStatusBanner({
  severity = 'Warning',
  message,
  closable = true,
  onClosed,
  onClick
}: AppStatusBannerProps): React.JSX.Element {
  const isSuccess = severity === 'Success'
  return (
    <div
      role="status"
      aria-live="polite"
      className={`udt-status-banner${isSuccess ? ' udt-status-banner--success' : ' udt-status-banner--warning'}${onClick ? ' udt-status-banner--clickable' : ''}`}
      onClick={onClick}
    >
      <span className="udt-status-banner__icon" aria-hidden="true">
        {isSuccess ? <SyncOutlined /> : <WarningFilled />}
      </span>
      <span className="udt-status-banner__message">{message}</span>
      {closable && (
        <button
          type="button"
          className="udt-status-banner__close"
          aria-label="Close"
          onClick={(e) => {
            e.stopPropagation()
            onClosed?.()
          }}
        >
          <CloseOutlined />
        </button>
      )}
    </div>
  )
}
