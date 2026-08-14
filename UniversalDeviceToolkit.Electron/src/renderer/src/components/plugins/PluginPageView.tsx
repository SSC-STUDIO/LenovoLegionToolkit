import { useCallback, useEffect, useRef, useState } from 'react'
import {
  ArrowLeft24Regular,
  DocumentQuestionMark24Regular,
  FluentLoadingIcon
} from '../icons/fluent'
import { useTranslation } from 'react-i18next'
import { useNavigate, useParams } from 'react-router-dom'
import { usePluginsStore } from '../../stores/pluginsStore'
import {
  bindPluginWebviewListeners,
  buildPluginPageSource,
  buildPluginPartition,
  buildPluginPreloadUrl
} from './pluginPageViewModel'
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
  const loading = usePluginsStore((s) => s.loading)
  const load = usePluginsStore((s) => s.load)
  const [preloadUrl, setPreloadUrl] = useState<string | null>(null)
  const [preloadFailed, setPreloadFailed] = useState(
    () => window.bridge?.getPluginPreloadPath == null
  )
  const releaseWebviewListeners = useRef<(() => void) | null>(null)

  const plugin = plugins.find((p) => p.id === pluginId)
  const src = buildPluginPageSource(plugin?.directory, plugin?.webPage)
  const webviewKey = `${pluginId}-${src ?? ''}`
  const [webviewState, setWebviewState] = useState<{
    key: string
    status: 'loading' | 'ready' | 'failed'
  }>({ key: webviewKey, status: 'loading' })
  const currentWebviewStatus =
    webviewState.key === webviewKey ? webviewState.status : 'loading'
  const ready = currentWebviewStatus === 'ready'
  const failed = currentWebviewStatus === 'failed'

  useEffect(() => {
    void load()
  }, [load])

  useEffect(() => {
    let active = true
    const preloadRequest = window.bridge?.getPluginPreloadPath?.()
    if (preloadRequest == null) {
      return () => {
        active = false
      }
    }

    void preloadRequest
      .then((path) => {
        if (!active) return
        const nextPreloadUrl = buildPluginPreloadUrl(path)
        if (nextPreloadUrl == null) {
          setPreloadFailed(true)
          return
        }
        setPreloadUrl(nextPreloadUrl)
      })
      .catch(() => {
        if (active) setPreloadFailed(true)
      })

    return () => {
      active = false
    }
  }, [])

  const handleWebviewRef = useCallback((webview: HTMLWebViewElement | null): void => {
    releaseWebviewListeners.current?.()
    releaseWebviewListeners.current = null
    if (webview == null) return

    releaseWebviewListeners.current = bindPluginWebviewListeners(
      webview,
      () => {
        setWebviewState({ key: webviewKey, status: 'ready' })
      },
      () => {
        setWebviewState({ key: webviewKey, status: 'failed' })
      }
    )
  }, [webviewKey])

  useEffect(() => {
    return () => {
      releaseWebviewListeners.current?.()
      releaseWebviewListeners.current = null
    }
  }, [])

  if (plugin == null) {
    if (loading) {
      return (
        <div className="udt-plugin-page udt-content-wide udt-content-fill">
          <div className="udt-plugin-page__empty">
            <FluentLoadingIcon className="udt-plugin-page__loading-icon" />
            <div>{t('plugins.pageLoading', { defaultValue: 'Loading plugin page...' })}</div>
          </div>
        </div>
      )
    }
    return (
      <div className="udt-plugin-page udt-content-wide udt-content-fill">
        <div className="udt-plugin-page__empty">
          <DocumentQuestionMark24Regular className="udt-plugin-page__empty-icon" />
          <div>{t('plugins.notFound', { defaultValue: 'Plugin not found' })}</div>
          <button type="button" className="udt-btn udt-btn--secondary" onClick={() => navigate('/plugins')}>
            <ArrowLeft24Regular /> {t('plugins.back', { defaultValue: 'Back to plugins' })}
          </button>
        </div>
      </div>
    )
  }

  if (src == null) {
    return (
      <div className="udt-plugin-page udt-content-wide udt-content-fill">
        <div className="udt-plugin-page__empty">
          <DocumentQuestionMark24Regular className="udt-plugin-page__empty-icon" />
          <div>{t('plugins.noWebPage', { defaultValue: 'This plugin has no web interface.' })}</div>
          <button type="button" className="udt-btn udt-btn--secondary" onClick={() => navigate('/plugins')}>
            <ArrowLeft24Regular /> {t('plugins.back', { defaultValue: 'Back to plugins' })}
          </button>
        </div>
      </div>
    )
  }

  const pageFailed = failed || preloadFailed

  return (
    <div className="udt-plugin-page udt-content-wide udt-content-fill">
      <div className="udt-plugin-page__header">
        <button
          type="button"
          className="udt-plugin-page__back"
          aria-label={t('plugins.back', { defaultValue: 'Back to plugins' })}
          onClick={() => navigate('/plugins')}
        >
          <ArrowLeft24Regular />
        </button>
        {plugin.icon != null && plugin.icon !== '' && (
          <span
            className="udt-plugin-page__icon"
            style={plugin.iconBackground ? { background: plugin.iconBackground } : undefined}
          >
            {plugin.icon}
          </span>
        )}
        <div className="udt-plugin-page__copy">
          <div className="udt-plugin-page__title">{plugin.name}</div>
          {plugin.installedVersion != null && (
            <div className="udt-plugin-page__version">v{plugin.installedVersion}</div>
          )}
        </div>
      </div>
      <div className="udt-plugin-page__host">
        {!ready && !pageFailed && (
          <div className="udt-plugin-page__loading">
            <FluentLoadingIcon className="udt-plugin-page__loading-icon" />
            <div>{t('plugins.pageLoading', { defaultValue: 'Loading plugin page…' })}</div>
          </div>
        )}
        {preloadUrl != null && (
          <webview
            ref={handleWebviewRef}
            key={`${pluginId}-${src}`}
            src={src}
            preload={preloadUrl}
            className={`udt-plugin-page__webview${ready ? ' udt-plugin-page__webview--ready' : ''}`}
            partition={buildPluginPartition(pluginId)}
          />
        )}
        {pageFailed && (
          <div className="udt-plugin-page__empty">
            <DocumentQuestionMark24Regular className="udt-plugin-page__empty-icon" />
            <div>{t('plugins.pageLoadFailed', { defaultValue: 'The plugin page could not be loaded.' })}</div>
            <button type="button" className="udt-btn udt-btn--secondary" onClick={() => navigate('/plugins')}>
              <ArrowLeft24Regular /> {t('plugins.back', { defaultValue: 'Back to plugins' })}
            </button>
          </div>
        )}
      </div>
    </div>
  )
}
