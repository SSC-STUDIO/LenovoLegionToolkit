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
  const { t } = useTranslation()
  const labels: Record<WindowBackdropStyle, string> = {
    Windows: t('wpf.settingsPagewindowBackdropmica'),
    macOS: t('wpf.settingsPagewindowBackdropacrylic'),
    Off: t('wpf.settingsPagewindowBackdropoff')
  }
  const style = normalizeWindowBackdropStyle(application['WindowBackdropStyle'])

  return (
    <div className="udt-backdrop-setting">
      <div className="udt-backdrop-setting__copy">
        <span className="udt-backdrop-setting__title">
          {t('wpf.settingsPagewindowBackdroptitle')}
        </span>
        <span className="udt-backdrop-setting__description">
          {t('wpf.settingsPagewindowBackdropmessage')}
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
