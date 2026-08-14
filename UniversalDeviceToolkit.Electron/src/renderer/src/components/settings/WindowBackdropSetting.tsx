import { Select } from 'antd'
import { useTranslation } from 'react-i18next'
import {
  applyWindowBackdrop,
  normalizeWindowBackdropStyle,
  type WindowBackdropStyle
} from '../../theme/windowBackdrop'
import { SettingsCard } from './SettingsCard'

interface WindowBackdropSettingProps {
  application: Record<string, unknown>
  persist: (patch: Record<string, unknown>) => void
}

const PLATFORM: string = window.bridge?.platform ?? 'win32'

/** Options meaningful on the current platform (mica is Windows-only). */
const OPTIONS: WindowBackdropStyle[] =
  PLATFORM === 'win32' ? ['Windows', 'macOS', 'Off'] : ['macOS', 'Off']

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
  const rawStyle = normalizeWindowBackdropStyle(application['WindowBackdropStyle'])
  const style: WindowBackdropStyle = OPTIONS.includes(rawStyle) ? rawStyle : 'macOS'

  return (
    <SettingsCard
      title={t('wpf.settingsPagewindowBackdroptitle')}
      description={t('wpf.settingsPagewindowBackdropmessage')}
      action={
        <Select<WindowBackdropStyle>
          className="udt-settings-select"
          value={style}
          onChange={(value) => {
            applyWindowBackdrop(value)
            persist({ WindowBackdropStyle: value })
          }}
          options={OPTIONS.map((value) => ({ value, label: labels[value] }))}
        />
      }
    />
  )
}
