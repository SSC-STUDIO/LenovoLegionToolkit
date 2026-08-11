import { useEffect, useRef, useState } from 'react'
import { Alert, Button, Modal, Spin, Typography } from 'antd'
import { useTranslation } from 'react-i18next'
import { bootLogoApi, type BootLogoStatus } from '../../api/bootLogo'

/**
 * Parity modal for WPF Windows/Settings/BootLogoWindow: shows the current
 * boot-logo status (default vs custom), lets the user pick an image to
 * install as the custom boot logo and revert to the default one.
 */

interface BootLogoModalProps {
  open: boolean
  onClose: () => void
}

interface BootLogoResult {
  kind: 'success' | 'error'
  text: string
}

const BOOT_LOGO_ERROR_MARKERS: Array<{ marker: string; i18nKey: string }> = [
  { marker: 'CantSetUEFIPrivilegeException', i18nKey: 'bootLogoWindowsetErrorcannotsetuefiprivilege' },
  { marker: 'CantMountUEFIPartitionException', i18nKey: 'bootLogoWindowsetErrorcannotmountefipartition' },
  { marker: 'NotEnoughSpaceOnUEFIPartitionException', i18nKey: 'bootLogoWindowsetErrornotenoughfreespaceonefipartition' },
  { marker: 'InvalidBootLogoImageSizeException', i18nKey: 'bootLogoWindowsetErrorinvalidimagesize' },
  { marker: 'InvalidBootLogoImageFormatException', i18nKey: 'bootLogoWindowsetErrorinvalidimageformat' }
]

export default function BootLogoModal({ open, onClose }: BootLogoModalProps): React.JSX.Element {
  const { t } = useTranslation()
  const [loading, setLoading] = useState(true)
  const [loadError, setLoadError] = useState<string | null>(null)
  const [status, setStatus] = useState<BootLogoStatus | null>(null)
  const [busy, setBusy] = useState(false)
  const [result, setResult] = useState<BootLogoResult | null>(null)
  const fileInputRef = useRef<HTMLInputElement | null>(null)

  useEffect(() => {
    if (!open) return
    let cancelled = false

    const refresh = async (): Promise<void> => {
      setLoading(true)
      setLoadError(null)
      setResult(null)
      try {
        const next = await bootLogoApi.getStatus()
        if (!cancelled) setStatus(next)
      } catch (reason) {
        if (!cancelled) setLoadError((reason as Error).message)
      } finally {
        if (!cancelled) setLoading(false)
      }
    }

    void refresh()
    return () => {
      cancelled = true
    }
  }, [open])

  const errorText = (reason: unknown): string => {
    const message = (reason as Error).message ?? ''
    const known = BOOT_LOGO_ERROR_MARKERS.find((entry) => message.includes(entry.marker))
    if (known != null) return t(known.i18nKey)
    return message
  }

  const retry = async (): Promise<void> => {
    setLoading(true)
    setLoadError(null)
    try {
      setStatus(await bootLogoApi.getStatus())
    } catch (reason) {
      setLoadError((reason as Error).message)
    } finally {
      setLoading(false)
    }
  }

  const handleRevertToDefault = async (): Promise<void> => {
    setBusy(true)
    setResult(null)
    try {
      await bootLogoApi.disable()
      setResult({ kind: 'success', text: t('bootLogoWindowsetDefaultSuccess') })
      setStatus(await bootLogoApi.getStatus())
    } catch (reason) {
      setResult({
        kind: 'error',
        text: t('bootLogoWindowsetDefaultFailed').replace('{0}', errorText(reason))
      })
    } finally {
      setBusy(false)
    }
  }

  const pickImage = (): void => {
    fileInputRef.current?.click()
  }

  const handleFileChosen = async (file: File | undefined): Promise<void> => {
    if (file == null) return
    setBusy(true)
    setResult(null)
    try {
      const filePath = (file as File & { path?: string }).path
      if (typeof filePath !== 'string' || filePath.length === 0) {
        throw new Error('File path is not available')
      }
      await bootLogoApi.enable(filePath)
      setResult({ kind: 'success', text: t('bootLogoWindowsetCustomSuccess') })
      setStatus(await bootLogoApi.getStatus())
    } catch (reason) {
      setResult({
        kind: 'error',
        text: t('bootLogoWindowsetCustomFailed').replace('{0}', errorText(reason))
      })
    } finally {
      setBusy(false)
    }
  }

  const accept = (): string => {
    const filters = status?.filters ?? []
    if (filters.length === 0) return 'image/*'
    return filters
      .flatMap((filter) => filter.split(';'))
      .map((entry) => entry.trim().replace(/^\*\.?/, '.'))
      .filter((entry) => entry.startsWith('.'))
      .join(',')
  }

  const description = (): string => {
    const resolution = status?.resolution?.DisplayName ?? ''
    const formats = (status?.formats ?? []).map((format) => format.toUpperCase()).join(', ')
    return t('bootLogoWindowdescription').replace('{0}', resolution).replace('{1}', formats)
  }

  const showRevert = status?.enabled === true
  const showCustomize = status?.enabled === false

  return (
    <Modal
      open={open}
      title={t('bootLogoWindowtitle')}
      width={400}
      footer={[
        <Button key="close" onClick={onClose}>
          {t('common.close')}
        </Button>
      ]}
      onCancel={onClose}
    >
      {loading ? (
        <div style={{ textAlign: 'center', padding: 24 }}>
          <Spin />
        </div>
      ) : loadError != null || status == null ? (
        <div>
          <Alert
            type="error"
            showIcon
            message={loadError ?? t('common.error')}
            action={
              <Button size="small" onClick={() => void retry()}>
                {t('common.retry')}
              </Button>
            }
          />
        </div>
      ) : (
        <div className="udt-settings-modal">
          <input
            ref={fileInputRef}
            type="file"
            accept={accept()}
            style={{ display: 'none' }}
            onChange={(event) => {
              void handleFileChosen(event.target.files?.[0])
              event.target.value = ''
            }}
          />
          <div className="udt-settings-modal__row">
            <span>{t('bootLogoWindowstatus')}</span>
            <strong>
              {showRevert ? t('bootLogoWindowcustomLogoSet') : t('bootLogoWindowdefaultLogoSet')}
            </strong>
          </div>
          <div className="udt-settings-modal__description">{description()}</div>
          <div className="udt-settings-modal__actions">
            {showCustomize && (
              <Button type="primary" loading={busy} onClick={pickImage}>
                {t('bootLogoWindowcustomize')}
              </Button>
            )}
            {showRevert && (
              <Button type="primary" loading={busy} onClick={() => void handleRevertToDefault()}>
                {t('revertToDefault')}
              </Button>
            )}
          </div>
          {result != null && (
            <Typography.Text type={result.kind === 'success' ? 'success' : 'danger'}>
              {result.text}
            </Typography.Text>
          )}
        </div>
      )}
    </Modal>
  )
}
