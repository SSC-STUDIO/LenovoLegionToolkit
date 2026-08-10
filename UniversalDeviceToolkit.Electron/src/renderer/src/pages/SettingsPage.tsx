import { useState } from 'react'
import { Card, Menu, Typography } from 'antd'
import { useTranslation } from 'react-i18next'
import AppearanceSection from '../components/settings/AppearanceSection'
import ApplicationSection from '../components/settings/ApplicationSection'
// TODO: 以下 5 个子页组件由另一任务创建（固定文件 + 固定 export 名）。
// 文件就绪后启用对应 import，并把 renderSection 中的本地占位替换为真实组件：
// import PowerSection from '../components/settings/PowerSection'
// import DisplaySection from '../components/settings/DisplaySection'
// import SmartKeysSection from '../components/settings/SmartKeysSection'
// import UpdateSection from '../components/settings/UpdateSection'
// import IntegrationsSection from '../components/settings/IntegrationsSection'

type SectionKey =
  | 'appearance'
  | 'application'
  | 'power'
  | 'display'
  | 'smartKeys'
  | 'update'
  | 'integrations'

const SECTION_KEYS: { key: SectionKey; labelKey: string }[] = [
  { key: 'appearance', labelKey: 'settings.nav.appearance' },
  { key: 'application', labelKey: 'settings.nav.application' },
  { key: 'power', labelKey: 'settings.nav.power' },
  { key: 'display', labelKey: 'settings.nav.display' },
  { key: 'smartKeys', labelKey: 'settings.nav.smartKeys' },
  { key: 'update', labelKey: 'settings.nav.update' },
  { key: 'integrations', labelKey: 'settings.nav.integrations' }
]

function PlaceholderSection(): React.JSX.Element {
  const { t } = useTranslation()
  return <Typography.Text type="secondary">{t('pages.placeholder')}</Typography.Text>
}

function renderSection(key: SectionKey): React.JSX.Element {
  switch (key) {
    case 'appearance':
      return <AppearanceSection />
    case 'application':
      return <ApplicationSection />
    // TODO: 固定文件就绪后改为渲染真实子页组件
    case 'power':
    case 'display':
    case 'smartKeys':
    case 'update':
    case 'integrations':
      return <PlaceholderSection />
  }
}

export default function SettingsPage(): React.JSX.Element {
  const { t } = useTranslation()
  const [active, setActive] = useState<SectionKey>('appearance')

  return (
    <Card title={t('settings.title')}>
      <div style={{ display: 'flex' }}>
        <Menu
          mode="inline"
          selectedKeys={[active]}
          items={SECTION_KEYS.map((item) => ({ key: item.key, label: t(item.labelKey) }))}
          onClick={({ key }) => setActive(key as SectionKey)}
          style={{
            width: 200,
            borderInlineEnd: '1px solid rgba(128, 128, 128, 0.15)',
            background: 'transparent'
          }}
        />
        <div style={{ flex: 1, paddingLeft: 24, minWidth: 0 }}>{renderSection(active)}</div>
      </div>
    </Card>
  )
}
