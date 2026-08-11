import { useState } from 'react'
import { Menu, Typography } from 'antd'
import {
  ApiOutlined,
  AppstoreOutlined,
  BgColorsOutlined,
  DesktopOutlined,
  KeyOutlined,
  PoweroffOutlined,
  SyncOutlined
} from '@ant-design/icons'
import { useTranslation } from 'react-i18next'
import AppearanceSection from '../components/settings/AppearanceSection'
import ApplicationSection from '../components/settings/ApplicationSection'
import { PowerSection } from '../components/settings/PowerSection'
import { DisplaySection } from '../components/settings/DisplaySection'
import { SmartKeysSection } from '../components/settings/SmartKeysSection'
import { UpdateSection } from '../components/settings/UpdateSection'
import { IntegrationsSection } from '../components/settings/IntegrationsSection'

type SectionKey =
  | 'appearance'
  | 'application'
  | 'power'
  | 'display'
  | 'smartKeys'
  | 'update'
  | 'integrations'

const SECTION_KEYS: { key: SectionKey; labelKey: string; icon: React.JSX.Element }[] = [
  { key: 'appearance', labelKey: 'settings.nav.appearance', icon: <BgColorsOutlined /> },
  { key: 'application', labelKey: 'settings.nav.application', icon: <AppstoreOutlined /> },
  { key: 'power', labelKey: 'settings.nav.power', icon: <PoweroffOutlined /> },
  { key: 'display', labelKey: 'settings.nav.display', icon: <DesktopOutlined /> },
  { key: 'smartKeys', labelKey: 'settings.nav.smartKeys', icon: <KeyOutlined /> },
  { key: 'update', labelKey: 'settings.nav.update', icon: <SyncOutlined /> },
  { key: 'integrations', labelKey: 'settings.nav.integrations', icon: <ApiOutlined /> }
]

function renderSection(key: SectionKey): React.JSX.Element {
  switch (key) {
    case 'appearance': return <AppearanceSection />
    case 'application': return <ApplicationSection />
    case 'power': return <PowerSection />
    case 'display': return <DisplaySection />
    case 'smartKeys': return <SmartKeysSection />
    case 'update': return <UpdateSection />
    case 'integrations': return <IntegrationsSection />
  }
}

export default function SettingsPage(): React.JSX.Element {
  const { t } = useTranslation()
  const [active, setActive] = useState<SectionKey>('appearance')

  return (
    <div className="udt-settings-page">
      <header className="udt-settings-page__header">
        <Typography.Title level={3} className="udt-settings-page__title">{t('settings.title')}</Typography.Title>
        <Typography.Text className="udt-settings-page__description">
          Configure the application, devices, and integrations.
        </Typography.Text>
      </header>
      <div className="udt-settings-page__surface">
        <Menu
          className="udt-settings-page__nav"
          mode="inline"
          selectedKeys={[active]}
          items={SECTION_KEYS.map((item) => ({
            key: item.key,
            icon: item.icon,
            label: <span title={t(item.labelKey)}>{t(item.labelKey)}</span>
          }))}
          onClick={({ key }) => setActive(key as SectionKey)}
        />
        <section className="udt-settings-page__content" aria-label={t(`settings.nav.${active}`)}>
          {renderSection(active)}
        </section>
      </div>
    </div>
  )
}
