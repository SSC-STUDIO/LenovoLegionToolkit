import { useEffect, useMemo, useState } from 'react'
import { create } from 'zustand'
import { useTranslation } from 'react-i18next'
import { DownloadOutlined, UpCircleFilled } from '@ant-design/icons'
import { updateApi } from '../../api/update'
import './utils.css'

/**
 * Port of WPF UpdateWindow: shows the newest version and its release notes
 * and offers to download/install it.
 *
 * The WPF window downloads the installer through UpdateChecker and launches it
 * with `/SILENT /RESTARTAPPLICATIONS`. The host does not expose a download
 * bridge yet, so the primary action opens the GitHub "latest release" page —
 * the same fallback the WPF window uses when the downloaded path is not an
 * allowed installer.
 */

export interface UpdateModalOptions {
  version?: string | null
  releaseNotes?: string | null
  releaseDate?: string | null
}

interface UpdateRequest {
  id: number
  options: UpdateModalOptions
}

let requestSeq = 0
let pendingResolve: ((downloaded: boolean) => void) | null = null

interface UpdateState {
  request: UpdateRequest | null
  show: (options: UpdateModalOptions) => void
  settle: (downloaded: boolean) => void
}

const useUpdateStore = create<UpdateState>((set) => ({
  request: null,
  show: (options) => set({ request: { id: ++requestSeq, options } }),
  settle: (downloaded) => {
    pendingResolve?.(downloaded)
    pendingResolve = null
    set({ request: null })
  }
}))

export function openUpdateModal(options: UpdateModalOptions): Promise<boolean> {
  return new Promise((resolve) => {
    pendingResolve = resolve
    useUpdateStore.getState().show(options)
  })
}

const LATEST_RELEASE_URL = 'https://github.com/SSC-STUDIO/UniversalDeviceToolkit/releases/latest'

export default function UpdateModalHost(): React.JSX.Element {
  const { t } = useTranslation()
  const request = useUpdateStore((s) => s.request)
  const settle = useUpdateStore((s) => s.settle)
  const [checking, setChecking] = useState(false)

  useEffect(() => {
    if (!request) return
    if (request.options.version) return
    // No version was provided by the caller: resolve it from the host check.
    setChecking(true)
    void updateApi
      .check(true)
      .then((result) => {
        if (result.available) {
          useUpdateStore.setState({
            request: { ...request, options: { ...request.options, version: result.version ?? null } }
          })
        } else {
          settle(false)
        }
      })
      .catch(() => settle(false))
      .finally(() => setChecking(false))
  }, [request, settle])

  const notes = useMemo(() => request?.options.releaseNotes ?? null, [request])

  if (!request) return <></>

  const { version, releaseDate } = request.options

  const openReleases = (): void => {
    void window.bridge?.openExternal?.(LATEST_RELEASE_URL).catch(() => undefined)
    settle(false)
  }

  return (
    <div className="udt-utils-backdrop" onClick={() => settle(false)}>
      <div
        className="udt-utils-modal"
        style={{ width: 720, maxWidth: 720, maxHeight: 560 }}
        onClick={(event) => event.stopPropagation()}
      >
        <div className="udt-utils-modal__title">{t('wpf.updateWindowtitle')}</div>
        <div className="udt-utils-modal__body">
          <div style={{ display: 'flex', gap: 16, marginBottom: 16 }}>
            <span
              style={{
                width: 48,
                height: 48,
                flexShrink: 0,
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
                background: 'var(--udt-surface-chart)',
                borderRadius: 'var(--udt-radius-control)',
                fontSize: 24,
                color: '#4f9df7'
              }}
            >
              <UpCircleFilled />
            </span>
            <div style={{ flex: 1 }}>
              <div style={{ fontWeight: 600 }}>{t('wpf.updateWindowwhatsNew')}</div>
              <div className="udt-utils-text" style={{ fontSize: 12, margin: '2px 0 8px' }}>
                {t('wpf.updateWindowtitle')}
              </div>
              <div style={{ display: 'flex', gap: 8, flexWrap: 'wrap' }}>
                {version && (
                  <span className="udt-utils-chip" style={{ background: 'rgba(79, 157, 247, 0.18)', color: '#7db4f5' }}>
                    {version}
                  </span>
                )}
                {releaseDate && <span className="udt-utils-chip">{releaseDate}</span>}
              </div>
            </div>
          </div>

          <div className="udt-utils-details" style={{ maxHeight: 300, marginTop: 0 }}>
            {checking ? (
              <div className="udt-utils-text">{t('common.loading')}</div>
            ) : notes && notes.trim().length > 0 ? (
              <div className="udt-utils-mono" style={{ whiteSpace: 'pre-wrap' }}>
                {notes}
              </div>
            ) : (
              <div className="udt-utils-text">
                {t('wpf.updateWindowreleaseNotesUnavailable')}
              </div>
            )}
          </div>

          <div className="udt-utils-progress-track" style={{ visibility: 'hidden', height: 4 }}>
            <div className="udt-utils-progress-fill" style={{ width: '100%' }} />
          </div>
        </div>
        <div className="udt-utils-modal__actions">
          <button type="button" className="udt-utils-button" onClick={() => settle(false)}>
            {t('wpf.cancel')}
          </button>
          <button type="button" className="udt-utils-button udt-utils-button--primary" onClick={openReleases}>
            <DownloadOutlined /> {t('wpf.update')}
          </button>
        </div>
      </div>
    </div>
  )
}
