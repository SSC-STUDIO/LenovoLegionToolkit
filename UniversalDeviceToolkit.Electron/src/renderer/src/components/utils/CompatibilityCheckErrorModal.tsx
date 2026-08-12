import { useMemo } from 'react'
import { create } from 'zustand'
import { useTranslation } from 'react-i18next'
import { FolderOpenOutlined, WarningOutlined } from '@ant-design/icons'
import './utils.css'

/**
 * Port of Electron CompatibilityCheckErrorWindow: startup error display with the
 * exception details, troubleshooting steps and an "open log" action.
 */

export interface CompatibilityErrorInfo {
  /** Exception type name (Error.name when constructed from a JS Error). */
  type: string
  message: string
  innerType?: string
  innerMessage?: string
  stackTrace?: string
}

interface CompatibilityErrorRequest {
  id: number
  info: CompatibilityErrorInfo
}

let requestSeq = 0
let pendingResolve: (() => void) | null = null

interface CompatibilityErrorState {
  request: CompatibilityErrorRequest | null
  show: (info: CompatibilityErrorInfo) => void
  settle: () => void
}

const useCompatibilityErrorStore = create<CompatibilityErrorState>((set) => ({
  request: null,
  show: (info) => set({ request: { id: ++requestSeq, info } }),
  settle: () => {
    pendingResolve?.()
    pendingResolve = null
    set({ request: null })
  }
}))

export function showCompatibilityCheckError(info: CompatibilityErrorInfo): Promise<void> {
  return new Promise((resolve) => {
    pendingResolve = resolve
    useCompatibilityErrorStore.getState().show(info)
  })
}

/** Normalize a caught value into the display model (mirrors Electron exception details). */
export function toCompatibilityErrorInfo(error: unknown): CompatibilityErrorInfo {
  if (error instanceof Error) {
    return {
      type: error.name || 'Exception',
      message: error.message,
      stackTrace: error.stack
    }
  }
  if (typeof error === 'object' && error !== null) {
    const record = error as Record<string, unknown>
    return {
      type: typeof record.type === 'string' ? record.type : 'Exception',
      message: typeof record.message === 'string' ? record.message : String(error),
      stackTrace: typeof record.stackTrace === 'string' ? record.stackTrace : undefined
    }
  }
  return { type: 'Exception', message: String(error) }
}

function formatDetails(info: CompatibilityErrorInfo): string {
  const lines: string[] = []
  lines.push(`Exception Type: ${info.type}`)
  lines.push(`Message: ${info.message}`)
  if (info.innerType || info.innerMessage) {
    lines.push('')
    lines.push(`Inner Exception: ${info.innerType ?? '-'}`)
    lines.push(`Inner Message: ${info.innerMessage ?? '-'}`)
  }
  if (info.stackTrace) {
    lines.push('')
    lines.push('Stack Trace:')
    lines.push(info.stackTrace)
  }
  return lines.join('\n')
}

export default function CompatibilityCheckErrorModalHost(): React.JSX.Element {
  const { t } = useTranslation()
  const request = useCompatibilityErrorStore((s) => s.request)
  const settle = useCompatibilityErrorStore((s) => s.settle)

  const details = useMemo(() => (request ? formatDetails(request.info) : ''), [request])

  const openLog = (): void => {
    void window.bridge?.openLogFolder?.().catch(() => undefined)
  }

  if (!request) return <></>

  return (
    <div className="udt-utils-backdrop" onClick={settle}>
      <div
        className="udt-utils-modal"
        style={{ width: 720, minHeight: 420 }}
        onClick={(event) => event.stopPropagation()}
      >
        <div className="udt-utils-modal__title">{t('wpf.compatibilityCheckErrorWindowtitle')}</div>
        <div className="udt-utils-modal__body">
          <div className="udt-utils-banner">
            <WarningOutlined className="udt-utils-banner__icon" />
            <div className="udt-utils-banner__copy">
              <div className="udt-utils-banner__title">{t('wpf.compatibilityCheckErrormessage')}</div>
              <div className="udt-utils-banner__desc">
                {t('wpf.compatibilityCheckErrorWindowdescription')}
              </div>
            </div>
          </div>
          <div className="udt-utils-details">
            <div className="udt-utils-mono">{details}</div>
          </div>
          <details className="udt-utils-expander" style={{ margin: '14px 0' }}>
            <summary>{t('wpf.compatibilityCheckErrorWindowtroubleshootingTitle')}</summary>
            <p className="udt-utils-tip">• {t('wpf.compatibilityCheckErrorWindowtip1')}</p>
            <p className="udt-utils-tip">• {t('wpf.compatibilityCheckErrorWindowtip2')}</p>
            <p className="udt-utils-tip">• {t('wpf.compatibilityCheckErrorWindowtip3')}</p>
            <p className="udt-utils-tip">• {t('wpf.compatibilityCheckErrorWindowtip4')}</p>
          </details>
        </div>
        <div className="udt-utils-modal__actions udt-utils-modal__actions--space-between">
          <button type="button" className="udt-utils-button" onClick={openLog}>
            <FolderOpenOutlined /> {t('wpf.compatibilityCheckErrorWindowopenLog')}
          </button>
          <button type="button" className="udt-utils-button udt-utils-button--primary" onClick={settle}>
            {t('wpf.close')}
          </button>
        </div>
      </div>
    </div>
  )
}
