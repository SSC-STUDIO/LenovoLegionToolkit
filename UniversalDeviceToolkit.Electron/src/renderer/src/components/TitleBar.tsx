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

const TITLEBAR_HEIGHT = 38

const DRAG_STYLE = { WebkitAppRegion: 'drag' } as React.CSSProperties
const NO_DRAG_STYLE = { WebkitAppRegion: 'no-drag' } as React.CSSProperties

type WindowButtonKind = 'minimize' | 'maximize' | 'close'

export default function TitleBar(): React.JSX.Element {
  const { t } = useTranslation()
  const { token } = theme.useToken()
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

  return (
    <div
      style={{
        height: TITLEBAR_HEIGHT,
        flexShrink: 0,
        display: 'flex',
        alignItems: 'stretch',
        background: token.colorBgLayout,
        color: token.colorText,
        userSelect: 'none'
      }}
    >
      <div
        style={{
          flex: 1,
          display: 'flex',
          alignItems: 'center',
          gap: 8,
          paddingLeft: 12,
          ...DRAG_STYLE
        }}
        onDoubleClick={toggleMaximize}
      >
        <ToolOutlined style={{ fontSize: 15 }} />
        <span style={{ fontSize: 12, whiteSpace: 'nowrap' }}>{t('app.name')}</span>
      </div>
      <div style={{ display: 'flex', ...NO_DRAG_STYLE }}>
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
