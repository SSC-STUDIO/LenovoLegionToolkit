import { useEffect, useState } from 'react'
import { Alert, Modal, Spin } from 'antd'
import { useTranslation } from 'react-i18next'
import type { PluginView } from '../../api/plugins'
import { usePluginsStore } from '../../stores/pluginsStore'

/**
 * Parity modal for WPF Windows/Settings/PluginSettingsWindow: shows the
 * plugin identity (icon, name, version, author) and either the plugin's
 * custom settings page or a "no configuration" empty state.
 *
 * Note: the WPF window hosts the plugin's WPF settings page via reflection;
 * the Electron renderer has no equivalent hosting mechanism, so the modal
 * always shows the empty-state message with the plugin description.
 */

interface PluginSettingsModalProps {
  open: boolean
  pluginId: string
  onClose: () => void
}

function iconLetterOf(name: string): string {
  const words = name.split(/[\s\-_]+/).filter(Boolean)
  const letters: string[] = []
  for (const word of words) {
    const first = word[0]
    if (!first) continue
    if (/[a-zA-Z]/.test(first)) letters.push(first.toUpperCase())
    else if (/[0-9]/.test(first)) letters.push(first)
    if (letters.length >= 2) break
  }
  if (letters.length === 0) {
    const first = name[0]
    if (first) letters.push(/[a-zA-Z]/.test(first) ? first.toUpperCase() : first)
  }
  return letters.join('').slice(0, 2)
}

function deterministicIconBackground(seed: string): string {
  let hash = 0
  for (let i = 0; i < seed.length; i++) {
    hash = (hash * 31 + seed.charCodeAt(i)) | 0
  }
  return `hsl(${Math.abs(hash % 360)} 70% 52%)`
}

export default function PluginSettingsModal({
  open,
  pluginId,
  onClose
}: PluginSettingsModalProps): React.JSX.Element {
  const { t } = useTranslation()
  const plugins = usePluginsStore((state) => state.plugins)
  const [plugin, setPlugin] = useState<PluginView | null>(null)
  const [notFound, setNotFound] = useState(false)

  useEffect(() => {
    if (!open) return
    const found = plugins.find((entry) => entry.id === pluginId) ?? null
    setPlugin(found)
    setNotFound(found == null)
  }, [open, pluginId, plugins])

  return (
    <Modal
      open={open}
      title={plugin != null ? `${plugin.name} — ${t('wpf.pluginSettingsWindowsettings')}` : t('wpf.pluginSettingsWindowtitle')}
      width={560}
      footer={null}
      onCancel={onClose}
    >
      {notFound ? (
        <Alert
          type="error"
          showIcon
          message={t('wpf.pluginSettingsWindowpluginNotFound').replace('{0}', pluginId)}
        />
      ) : plugin == null ? (
        <div style={{ textAlign: 'center', padding: 24 }}>
          <Spin />
        </div>
      ) : (
        <div>
          <div style={{ display: 'flex', alignItems: 'center', gap: 12, marginBottom: 16 }}>
            <div
              style={{
                width: 36,
                height: 36,
                borderRadius: 6,
                background: plugin.iconBackground ?? deterministicIconBackground(plugin.id),
                color: '#fff',
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
                fontWeight: 600,
                fontSize: 14
              }}
            >
              {iconLetterOf(plugin.name)}
            </div>
            <div style={{ minWidth: 0 }}>
              <div style={{ fontWeight: 600, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
                {plugin.name}
              </div>
              <div style={{ display: 'flex', gap: 6, marginTop: 4 }}>
                <span
                  style={{
                    fontSize: 12,
                    padding: '1px 6px',
                    borderRadius: 6,
                    background: 'rgba(128,128,128,0.2)'
                  }}
                >
                  v{plugin.version}
                </span>
                {plugin.author.length > 0 && (
                  <span
                    style={{
                      fontSize: 12,
                      padding: '1px 6px',
                      borderRadius: 6,
                      background: 'rgba(128,128,128,0.2)'
                    }}
                  >
                    {t('wpf.pluginSettingsWindowauthor').replace('{0}', plugin.author)}
                  </span>
                )}
              </div>
            </div>
          </div>
          <div style={{ opacity: 0.75, fontSize: 13 }}>
            {t('wpf.pluginSettingsWindownoConfigMessage')}
          </div>
          {plugin.description.length > 0 && (
            <div style={{ opacity: 0.55, fontSize: 12, marginTop: 8 }}>{plugin.description}</div>
          )}
        </div>
      )}
    </Modal>
  )
}
