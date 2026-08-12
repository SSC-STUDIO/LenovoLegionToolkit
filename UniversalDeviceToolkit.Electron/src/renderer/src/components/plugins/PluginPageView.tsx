import { useEffect, useRef, useState } from 'react'
import { ArrowLeftOutlined, FileUnknownOutlined } from '@ant-design/icons'
import { useTranslation } from 'react-i18next'
import { useNavigate, useParams } from 'react-router-dom'
import { usePluginsStore } from '../../stores/pluginsStore'
import './plugins.css'

/**
 * Hosts a plugin's web UI (contributes.webPage) in an embedded <webview>.
 * The guest preload (out/preload/plugin-host.js) injects window.pluginHost,
 * which the plugin page uses to call the host JSON-RPC backend.
 */
export default function PluginPageView(): React.JSX.Element {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const { pluginId = '' } = useParams<{ pluginId: string }>()
  const plugins = usePluginsStore((s) => s.plugins)
  const load = usePluginsStore((s) => s.load)
  const [preloadPath, setPreloadPath] = useState<string | null>(null)
  const [failed, setFailed] = useState(false)
  const webviewRef = useRef<HTMLWebViewElement | null>(null)

  const plugin = plugins.find((p) => p.id === pluginId)
  const src = (() => {
    if (!plugin?.directory || !plugin.webPage) return null
    const base = plugin.directory.replace(/\\/g, '/')
    return `file://${base}/${plugin.webPage.replace(/^\.?\//, '')}`
  })()

  useEffect(() => {
    void load()
  }, [load])

  useEffect(() => {
    void window.bridge?.getPluginPreloadPath?.().then(setPreloadPath).catch(() => undefined)
  }, [])

  useEffect(() => {
    const handleLoadFail = (): void => setFailed(true)
    const webview = webviewRef.current
    webview?.addEventListener('did-fail-load', handleLoadFail)
    return () => {
      webview?.removeEventListener('did-fail-load', handleLoadFail)
    }
  }, [pluginId, src])

  if (plugin == null) {
    return (
      <div className="udt-plugin-page">
        <div className="udt-plugin-page__empty">
          <FileUnknownOutlined className="udt-plugin-page__empty-icon" />
          <div>{t('plugins.notFound', { defaultValue: 'Plugin not found' })}</div>
          <button type="button" className="udt-btn udt-btn--secondary" onClick={() => navigate('/plugins')}>
            <ArrowLeftOutlined /> {t('plugins.back', { defaultValue: 'Back to plugins' })}
          </button>
        </div>
      </div>
    )
  }

  if (src == null) {
    return (
      <div className="udt-plugin-page">
        <div className="udt-plugin-page__empty">
          <FileUnknownOutlined className="udt-plugin-page__empty-icon" />
          <div>{t('plugins.noWebPage', { defaultValue: 'This plugin has no web interface.' })}</div>
          <button type="button" className="udt-btn udt-btn--secondary" onClick={() => navigate('/plugins')}>
            <ArrowLeftOutlined /> {t('plugins.back', { defaultValue: 'Back to plugins' })}
          </button>
        </div>
      </div>
    )
  }

  return (
    <div className="udt-plugin-page">
      <div className="udt-plugin-page__header">
        <button type="button" className="udt-btn udt-btn--secondary udt-btn--sm" onClick={() => navigate('/plugins')}>
          <ArrowLeftOutlined />
        </button>
        <span className="udt-plugin-page__title">{plugin.name}</span>
      </div>
      <div className="udt-plugin-page__host">
        {preloadPath != null && (
          <webview
            ref={(el) => {
              webviewRef.current = el
            }}
            key={`${pluginId}-${src}`}
            src={src}
            preload={`file://${preloadPath.replace(/\\/g, '/')}`}
            className="udt-plugin-page__webview"
            partition={`persist:plugin-${pluginId}`}
          />
        )}
        {failed && (
          <div className="udt-plugin-page__empty">
            <FileUnknownOutlined className="udt-plugin-page__empty-icon" />
            <div>{t('plugins.pageLoadFailed', { defaultValue: 'The plugin page could not be loaded.' })}</div>
          </div>
        )}
      </div>
    </div>
  )
}
