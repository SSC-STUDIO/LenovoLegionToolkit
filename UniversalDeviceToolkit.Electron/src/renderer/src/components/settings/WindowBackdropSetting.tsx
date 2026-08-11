import { Select } from 'antd'
import { useTranslation } from 'react-i18next'
import {
  applyWindowBackdrop,
  normalizeWindowBackdropStyle,
  type WindowBackdropStyle
} from '../../theme/windowBackdrop'

interface WindowBackdropSettingProps {
  application: Record<string, unknown>
  persist: (patch: Record<string, unknown>) => void
}

const OPTIONS: WindowBackdropStyle[] = ['Windows', 'macOS', 'Off']

export default function WindowBackdropSetting({
  application,
  persist
}: WindowBackdropSettingProps): React.JSX.Element {
  const { i18n } = useTranslation()
  const isChinese = i18n.language.startsWith('zh')
  const labels: Record<WindowBackdropStyle, string> = isChinese
    ? { Windows: '云母', macOS: '亚克力', Off: '关闭' }
    : { Windows: 'Mica', macOS: 'Acrylic', Off: 'Off' }
  const style = normalizeWindowBackdropStyle(application['WindowBackdropStyle'])

  return (
    <div className="udt-backdrop-setting">
      <div className="udt-backdrop-setting__copy">
        <span className="udt-backdrop-setting__title">
          {isChinese ? '窗口背景效果' : 'Window background effect'}
        </span>
        <span className="udt-backdrop-setting__description">
          {isChinese
            ? '选择应用窗口使用的 Windows 背景效果。'
            : 'Choose the Windows background effect used by the application window.'}
        </span>
      </div>
      <Select<WindowBackdropStyle>
        className="udt-backdrop-setting__select"
        value={style}
        onChange={(value) => {
          applyWindowBackdrop(value)
          persist({ WindowBackdropStyle: value })
        }}
        options={OPTIONS.map((value) => ({ value, label: labels[value] }))}
      />
    </div>
  )
}
