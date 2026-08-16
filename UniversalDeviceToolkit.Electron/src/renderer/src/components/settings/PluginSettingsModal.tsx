import { useEffect, useState } from 'react'
import { Alert, Modal, Spin } from 'antd'
import { CheckmarkCircle24Filled, ArrowDownload24Regular, ArrowCircleUp24Regular } from '../icons/fluent'
import { useTranslation } from 'react-i18next'
import { useNavigate } from 'react-router-dom'
import type { PluginView } from '../../api/plugins'
import { usePluginsStore } from '../../stores/pluginsStore'

/**
 * Plugin metadata modal. When the plugin ships contributes.webPage, Settings
 * navigates to the embedded web page instead of showing a native-page warning.
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
  const navigate = useNavigate()
  const plugins = usePluginsStore((state) => state.plugins)
  const [plugin, setPlugin] = useState<PluginView | null>(null)
  const [notFound, setNotFound] = useState(false)

  useEffect(() => {
    if (!open) return
    const found = plugins.find((entry) => entry.id === pluginId) ?? null
    setPlugin(found)
    setNotFound(found == null)
  }, [open, pluginId, plugins])

  useEffect(() => {
    if (!open || plugin == null) return
    if (plugin.webPage && plugin.directory) {
      onClose()
      navigate(`/plugins/${encodeURIComponent(plugin.id)}`)
    }
  }, [open, plugin, navigate, onClose])

  return (
    <Modal
      centered
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
                fontSize: 14,
                flexShrink: 0
              }}
            >
              {iconLetterOf(plugin.name)}
            </div>
            <div style={{ minWidth: 0 }}>
              <div style={{ fontWeight: 600, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
                {plugin.name}
              </div>
              <div style={{ display: 'flex', gap: 6, marginTop: 4, flexWrap: 'wrap' }}>
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
                {plugin.installedVersion && plugin.installedVersion !== plugin.version && (
                  <span
                    style={{
                      fontSize: 12,
                      padding: '1px 6px',
                      borderRadius: 6,
                      background: 'rgba(128,128,128,0.2)'
                    }}
                  >
                    {t('plugins.installedVersion', '已安装')} v{plugin.installedVersion}
                  </span>
                )}
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
                {plugin.isSystemPlugin && (
                  <span
                    style={{
                      fontSize: 12,
                      padding: '1px 6px',
                      borderRadius: 6,
                      background: 'rgba(128,128,128,0.2)'
                    }}
                  >
                    {t('plugins.local', '本地')}
                  </span>
                )}
              </div>
            </div>
          </div>

          {plugin.installedVersion ? (
            <div style={{ display: 'flex', gap: 6, alignItems: 'center', marginBottom: 12 }}>
              <CheckmarkCircle24Filled style={{ color: '#6fbf73' }} />
              <span style={{ fontSize: 13 }}>
                {t('plugins.settings.installedState', '已安装（v{{version}}）').replace(
                  '{{version}}',
                  plugin.installedVersion
                )}
              </span>
              {plugin.updateAvailable && (
                <span style={{ display: 'inline-flex', gap: 4, alignItems: 'center', fontSize: 12, color: '#e0a92e' }}>
                  <ArrowCircleUp24Regular /> {t('plugins.updateAvailable')} v{plugin.availableVersion}
                </span>
              )}
            </div>
          ) : (
            <div style={{ display: 'flex', gap: 6, alignItems: 'center', marginBottom: 12 }}>
              <ArrowDownload24Regular style={{ color: 'var(--udt-text-secondary, rgba(255,255,255,0.6))' }} />
              <span style={{ fontSize: 13 }}>{t('plugins.settings.notInstalledState', '未安装')}</span>
            </div>
          )}

          <div style={{ opacity: 0.75, fontSize: 13 }}>
            {t('wpf.pluginSettingsWindownoConfigMessage')}
          </div>

          {plugin.webPage ? (
            <Alert
              type="info"
              showIcon
              style={{ marginTop: 12 }}
              message={t('plugins.settings.openWebPage', 'Opening plugin page…')}
            />
          ) : plugin.capabilities.settingsPage || plugin.capabilities.featurePage ? (
            <Alert
              type="warning"
              showIcon
              style={{ marginTop: 12 }}
              message={t('plugins.settings.nativePageUnavailable')}
            />
          ) : null}

          <div style={{ display: 'flex', gap: 6, marginTop: 12, flexWrap: 'wrap' }}>
            {[
              { on: plugin.capabilities.settingsPage, label: t('plugins.settings.capability.settingsPage') },
              { on: plugin.capabilities.featurePage, label: t('plugins.settings.capability.featurePage') },
              {
                on: plugin.capabilities.optimizationCategory,
                label: t('plugins.settings.capability.optimizationCategory')
              },
              {
                on: plugin.capabilities.webPage === true || Boolean(plugin.webPage),
                label: t('plugins.settings.capability.webPage', 'Web page')
              },
              {
                on: plugin.capabilities.executableEntryPoint,
                label: t('plugins.settings.capability.executableEntryPoint')
              }
            ].map((chip) => (
              <span
                key={chip.label}
                style={{
                  fontSize: 12,
                  padding: '1px 6px',
                  borderRadius: 6,
                  background: chip.on ? 'rgba(111, 191, 115, 0.2)' : 'rgba(128,128,128,0.15)',
                  color: chip.on ? '#6fbf73' : 'var(--udt-text-tertiary, rgba(255,255,255,0.45))'
                }}
              >
                {chip.label}
              </span>
            ))}
          </div>

          {plugin.description.length > 0 && (
            <div style={{ opacity: 0.55, fontSize: 12, marginTop: 8 }}>{plugin.description}</div>
          )}

          {plugin.details && (
            <div style={{ marginTop: 12 }}>
              <div style={{ fontSize: 12, fontWeight: 600, opacity: 0.7 }}>{t('plugins.details')}</div>
              <div style={{ fontSize: 13, marginTop: 4, whiteSpace: 'pre-line' }}>{plugin.details}</div>
            </div>
          )}

          {plugin.usageGuide && (
            <div style={{ marginTop: 12 }}>
              <div style={{ fontSize: 12, fontWeight: 600, opacity: 0.7 }}>{t('plugins.usageGuide')}</div>
              <div style={{ fontSize: 13, marginTop: 4, whiteSpace: 'pre-line' }}>{plugin.usageGuide}</div>
            </div>
          )}

          {plugin.dependencies.length > 0 && (
            <div style={{ marginTop: 12 }}>
              <div style={{ fontSize: 12, fontWeight: 600, opacity: 0.7 }}>
                {t('plugins.dependencies')}
              </div>
              <div style={{ display: 'flex', gap: 6, marginTop: 4, flexWrap: 'wrap' }}>
                {plugin.dependencies.map((dependency) => (
                  <span
                    key={dependency}
                    style={{
                      fontSize: 12,
                      padding: '1px 6px',
                      borderRadius: 6,
                      background: 'rgba(128,128,128,0.2)'
                    }}
                  >
                    {dependency}
                  </span>
                ))}
              </div>
            </div>
          )}

          {plugin.updateAvailable && plugin.changelog && (
            <div style={{ marginTop: 12 }}>
              <div style={{ fontSize: 12, fontWeight: 600, opacity: 0.7 }}>
                {t('plugins.updateInfo', '更新信息')}
              </div>
              <div style={{ fontSize: 12, marginTop: 4, opacity: 0.8, whiteSpace: 'pre-line' }}>
                {plugin.changelog}
              </div>
            </div>
          )}
        </div>
      )}
    </Modal>
  )
}
