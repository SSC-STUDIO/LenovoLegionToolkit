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
    { name: 'AsyncLock', url: 'https://github.com/neosmart/AsyncLock' },
    { name: 'Autofac', url: 'https://github.com/autofac/Autofac' },
    { name: 'Ben.Demystifier', url: 'https://github.com/benaadams/Ben.Demystifier' },
    { name: 'ColorPicker', url: 'https://github.com/PixiEditor/ColorPicker' },
    { name: 'CsWin32', url: 'https://github.com/microsoft/CsWin32' },
    { name: 'Humanizer', url: 'https://github.com/Humanizr/Humanizer' }
  ],
  [
    { name: 'ManagedNativeWifi', url: 'https://github.com/emoacht/ManagedNativeWifi' },
    { name: 'Markdig', url: 'https://github.com/xoofx/markdig' },
    { name: 'Markdig.Wpf', url: 'https://github.com/Kryptos-FR/markdig.wpf' },
    { name: 'Microsoft.CSharp', url: 'https://github.com/dotnet/runtime' },
    { name: 'NAudio.Wasapi', url: 'https://github.com/naudio/NAudio' },
    { name: 'Newtonsoft.Json', url: 'https://github.com/JamesNK/Newtonsoft.Json' }
  ],
  [
    { name: 'Octokit', url: 'https://github.com/octokit/octokit.net' },
    { name: 'System.Management', url: 'https://github.com/dotnet/runtime' },
    { name: 'TaskScheduler', url: 'https://github.com/dahall/TaskScheduler' },
    { name: 'WindowsDisplayAPI', url: 'https://github.com/falahati/WindowsDisplayAPI' },
    { name: 'WPF-UI', url: 'https://github.com/lepoco/wpfui' }
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
