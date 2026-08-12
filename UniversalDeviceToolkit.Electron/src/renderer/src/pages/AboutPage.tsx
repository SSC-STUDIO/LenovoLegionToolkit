import { useEffect, useState } from 'react'
import { FolderOpenOutlined, LinkOutlined } from '@ant-design/icons'
import { useTranslation } from 'react-i18next'
import { invoke } from '../api/bridge'
import { openExternalUrl } from '../utils/url'
import '../components/pages/pages.css'

interface AppStatus {
  pid?: number
  version?: string
  build?: string
  logPath?: string
}

interface ThirdPartyLibrary {
  name: string
  url: string
}

const PROJECT_URL = 'https://github.com/SSC-STUDIO/UniversalDeviceToolkit'
const LATEST_RELEASE_URL = `${PROJECT_URL}/releases/latest`

const THIRD_PARTY_COLUMNS: ThirdPartyLibrary[][] = [
  [
    { name: 'Electron', url: 'https://github.com/electron/electron' },
    { name: 'React', url: 'https://react.dev' },
    { name: 'Ant Design', url: 'https://github.com/ant-design/ant-design' },
    { name: '@ant-design/icons', url: 'https://github.com/ant-design/ant-design-icons' },
    { name: 'ECharts', url: 'https://github.com/apache/echarts' },
    { name: 'Zustand', url: 'https://github.com/pmndrs/zustand' }
  ],
  [
    { name: 'i18next', url: 'https://github.com/i18next/i18next' },
    { name: 'React Router', url: 'https://github.com/remix-run/react-router' },
    { name: 'Fluent UI System Icons', url: 'https://github.com/microsoft/fluentui-system-icons' },
    { name: 'TypeScript', url: 'https://github.com/microsoft/TypeScript' },
    { name: 'Vite', url: 'https://vite.dev' },
    { name: 'electron-vite', url: 'https://electron-vite.org' }
  ],
  [
    { name: 'electron-builder', url: 'https://github.com/electron-userland/electron-builder' },
    { name: 'react-i18next', url: 'https://github.com/i18next/react-i18next' },
    { name: 'echarts-for-react', url: 'https://github.com/hustcc/echarts-for-react' },
    { name: 'esbuild', url: 'https://github.com/evanw/esbuild' },
    { name: 'ESLint', url: 'https://github.com/eslint/eslint' }
  ]
]

export default function AboutPage(): React.JSX.Element {
  const { t, i18n } = useTranslation()
  const [appStatus, setAppStatus] = useState<AppStatus | null>(null)

  useEffect(() => {
    void invoke<AppStatus>('app.getStatus').then(setAppStatus).catch(() => undefined)
  }, [])

  const version = appStatus?.version ?? '...'
  const isEnglish = i18n.language.toLowerCase().startsWith('en')

  const openApplicationDataFolder = (): void => {
    void window.bridge?.openAppFolder?.('data')
  }

  const openApplicationTempFolder = (): void => {
    void window.bridge?.openLogFolder?.()
  }

  // Opens external links via the main-process shell (http/https whitelist);
  // href stays as a degradation for non-click navigation (middle-click, etc.).
  const handleExternalLink = (url: string) => (event: React.MouseEvent<HTMLAnchorElement>): void => {
    event.preventDefault()
    void openExternalUrl(url)
  }

  return (
    <div className="udt-about-page">
      <h1 className="udt-page-title">{t('about.title')}</h1>

      <h2 className="udt-subsection-title udt-about-page__app-name">{t('app.name')}</h2>
      <p className="udt-about-page__text">
        {t('about.version')} {version}
      </p>
      {appStatus?.build != null && appStatus.build.trim() !== '' && (
        <p className="udt-about-page__text">
          {t('about.build')} {appStatus.build}
        </p>
      )}
      <p className="udt-about-page__text udt-about-page__text--wrap">
        {t('about.copyright')} © SSC-STUDIO
      </p>
      {!isEnglish && (
        <p className="udt-about-page__credit">
          {t('about.translationCredit', 'Translated by Karl Lee. 由凌卡Karl汉化，开源社区校正。')}
        </p>
      )}

      <h2 className="udt-subsection-title udt-about-page__section">
        {t('about.links')}
      </h2>
      <a className="udt-link-button" href={PROJECT_URL} target="_blank" rel="noreferrer" onClick={handleExternalLink(PROJECT_URL)}>
        <LinkOutlined />
        {t('about.projectWebsite')}
      </a>
      <a className="udt-link-button" href={LATEST_RELEASE_URL} target="_blank" rel="noreferrer" onClick={handleExternalLink(LATEST_RELEASE_URL)}>
        <LinkOutlined />
        {t('about.latestRelease')}
      </a>

      <h2 className="udt-subsection-title udt-about-page__section">{t('about.thirdParty')}</h2>
      <div className="udt-about-page__libs">
        {THIRD_PARTY_COLUMNS.map((column, index) => (
          <div key={index} className="udt-about-page__libs-column">
            {column.map((lib) => (
              <a
                key={lib.name}
                className="udt-link-button"
                href={lib.url}
                target="_blank"
                rel="noreferrer"
                onClick={handleExternalLink(lib.url)}
              >
                {lib.name}
              </a>
            ))}
          </div>
        ))}
      </div>

      <h2 className="udt-subsection-title udt-about-page__section">
        {t('about.applicationFolders')}
      </h2>
      <button type="button" className="udt-link-button" onClick={openApplicationDataFolder}>
        <FolderOpenOutlined />
        {t('about.data')}
      </button>
      <button type="button" className="udt-link-button" onClick={openApplicationTempFolder}>
        <FolderOpenOutlined />
        {t('about.temp')}
      </button>
    </div>
  )
}
