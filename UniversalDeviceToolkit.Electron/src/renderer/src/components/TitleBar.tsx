import { useEffect, useState } from 'react'
import {
  BorderOutlined,
  CloseOutlined,
  MinusOutlined,
  SwitcherOutlined,
  ToolOutlined
} from '@ant-design/icons'
import { theme } from 'antd'
import { useTranslation } from 'react-i18next'
import { useLocation } from 'react-router-dom'

const TITLEBAR_HEIGHT = 38

const DRAG_STYLE = { WebkitAppRegion: 'drag' } as React.CSSProperties
const NO_DRAG_STYLE = { WebkitAppRegion: 'no-drag' } as React.CSSProperties

const PAGE_LABELS: Record<string, string> = {
  '/dashboard': 'nav.dashboard',
  '/keyboard': 'nav.keyboardBacklight',
  '/automation': 'nav.automation',
  '/macro': 'nav.macro',
  '/optimization': 'nav.windowsOptimization',
  '/plugins': 'nav.pluginExtensions',
  '/settings': 'nav.settings',
  '/about': 'nav.about'
}

type WindowButtonKind = 'minimize' | 'maximize' | 'close'

export default function TitleBar(): React.JSX.Element {
  const { t } = useTranslation()
  const { token } = theme.useToken()
  const location = useLocation()
  const [isMaximized, setIsMaximized] = useState(false)
  const [hover, setHover] = useState<WindowButtonKind | null>(null)

  useEffect(() => {
    let disposed = false
    void window.bridge?.isMaximized().then((value) => {
      if (!disposed) setIsMaximized(value)
    })
    const offChanged = window.bridge?.onMaximizedChanged((value) => {
      if (!disposed) setIsMaximized(value)
    })
    return () => {
      disposed = true
      offChanged?.()
    }
  }, [])

  const toggleMaximize = (): void => {
    window.bridge?.maximizeToggle()
  }

  const buttonStyle = (kind: WindowButtonKind): React.CSSProperties => {
    const isHover = hover === kind
    const isClose = kind === 'close'
    return {
      width: 46,
      height: '100%',
      border: 'none',
      padding: 0,
      display: 'inline-flex',
      alignItems: 'center',
      justifyContent: 'center',
      cursor: 'default',
      fontSize: 13,
      color: isClose && isHover ? '#fff' : token.colorText,
      background: isHover ? (isClose ? '#E81123' : token.colorFillTertiary) : 'transparent',
      ...NO_DRAG_STYLE
    }
  }

  const pageKey = PAGE_LABELS[location.pathname] ?? 'app.name'
  const windowTitle = `${t('app.name')} - ${t(pageKey)}`

  return (
    <div className="udt-titlebar" style={{ height: TITLEBAR_HEIGHT }}>
      <div
        className="udt-titlebar__drag-region"
        style={DRAG_STYLE}
        onDoubleClick={toggleMaximize}
      >
        <ToolOutlined className="udt-titlebar__app-icon" />
        <span className="udt-titlebar__title" title={windowTitle}>
          {windowTitle}
        </span>
      </div>
      <div className="udt-titlebar__window-controls" style={NO_DRAG_STYLE}>
        <button
          type="button"
          aria-label="minimize"
          style={buttonStyle('minimize')}
          onMouseEnter={() => setHover('minimize')}
          onMouseLeave={() => setHover(null)}
          onClick={() => window.bridge?.minimize()}
        >
          <MinusOutlined />
        </button>
        <button
          type="button"
          aria-label={isMaximized ? 'restore' : 'maximize'}
          style={buttonStyle('maximize')}
          onMouseEnter={() => setHover('maximize')}
          onMouseLeave={() => setHover(null)}
          onClick={toggleMaximize}
        >
          {isMaximized ? <SwitcherOutlined /> : <BorderOutlined />}
        </button>
        <button
          type="button"
          aria-label="close"
          style={buttonStyle('close')}
          onMouseEnter={() => setHover('close')}
          onMouseLeave={() => setHover(null)}
          onClick={() => window.bridge?.closeWindow()}
        >
          <CloseOutlined />
        </button>
      </div>
    </div>
  )
}
