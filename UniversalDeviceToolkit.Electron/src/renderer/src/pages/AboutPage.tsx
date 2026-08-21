import { useCallback, useEffect, useState } from 'react'
import { FolderOpen24Regular, Link24Regular } from '../components/icons/fluent'
import { useTranslation } from 'react-i18next'
import { invoke } from '../api/bridge'
import { openExternalUrl } from '../utils/url'
import appMarkUrl from '../assets/app-mark.png'
import './pages.css'

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

const THIRD_PARTY_LIBRARIES: ThirdPartyLibrary[] = [
  { name: 'Electron', url: 'https://github.com/electron/electron' },
  { name: 'React', url: 'https://react.dev' },
  { name: 'Ant Design', url: 'https://github.com/ant-design/ant-design' },
  { name: 'ECharts', url: 'https://github.com/apache/echarts' },
  { name: 'Zustand', url: 'https://github.com/pmndrs/zustand' },
  { name: 'i18next', url: 'https://github.com/i18next/i18next' },
  { name: 'React Router', url: 'https://github.com/remix-run/react-router' },
  { name: 'Fluent UI System Icons', url: 'https://github.com/microsoft/fluentui-system-icons' },
  { name: 'TypeScript', url: 'https://github.com/microsoft/TypeScript' },
  { name: 'Vite', url: 'https://vite.dev' },
  { name: 'electron-vite', url: 'https://electron-vite.org' },
  { name: 'electron-builder', url: 'https://github.com/electron-userland/electron-builder' },
  { name: 'react-i18next', url: 'https://github.com/i18next/react-i18next' },
  { name: 'echarts-for-react', url: 'https://github.com/hustcc/echarts-for-react' },
  { name: 'esbuild', url: 'https://github.com/evanw/esbuild' },
  { name: 'ESLint', url: 'https://github.com/eslint/eslint' }
]

function AboutHeroPattern(): React.JSX.Element {
  return (
    <svg
      className="udt-about-page__pattern"
      viewBox="0 0 720 280"
      preserveAspectRatio="xMaxYMid slice"
      aria-hidden="true"
    >
      <defs>
        <radialGradient id="udt-hero-glow" cx="60%" cy="50%" r="50%">
          <stop offset="0%" stopColor="currentColor" stopOpacity="0.22" />
          <stop offset="60%" stopColor="currentColor" stopOpacity="0.06" />
          <stop offset="100%" stopColor="currentColor" stopOpacity="0" />
        </radialGradient>
        <linearGradient id="udt-hero-line-grad" x1="0%" y1="0%" x2="100%" y2="100%">
          <stop offset="0%" stopColor="currentColor" stopOpacity="0.1" />
          <stop offset="50%" stopColor="currentColor" stopOpacity="0.5" />
          <stop offset="100%" stopColor="currentColor" stopOpacity="0.15" />
        </linearGradient>
      </defs>

      {/* Ambient background glow */}
      <circle cx="560" cy="140" r="160" fill="url(#udt-hero-glow)" />

      <g opacity="0.85">
        {/* Radar & Concentric Tech Rings */}
        <g fill="none" stroke="currentColor">
          <circle cx="560" cy="140" r="115" strokeWidth="1" strokeDasharray="4 8" opacity="0.25" />
          <circle cx="560" cy="140" r="88" strokeWidth="1.2" opacity="0.2" />
          <circle cx="560" cy="140" r="58" strokeWidth="1.5" strokeDasharray="18 6 6 6" opacity="0.32" />
          <circle cx="560" cy="140" r="28" strokeWidth="1" opacity="0.2" />

          {/* Accent arc sweeps */}
          <path
            d="M 560 25 A 115 115 0 0 1 675 140"
            stroke="url(#udt-hero-line-grad)"
            strokeWidth="3"
            strokeLinecap="round"
            strokeDasharray="45 10 15 10"
            opacity="0.6"
          />
          <path
            d="M 472 140 A 88 88 0 0 1 560 52"
            stroke="currentColor"
            strokeWidth="2.5"
            strokeLinecap="round"
            opacity="0.4"
          />
          <path
            d="M 560 228 A 88 88 0 0 1 472 140"
            stroke="currentColor"
            strokeWidth="1.5"
            strokeDasharray="6 6"
            opacity="0.3"
          />
          <path
            d="M 648 140 A 88 88 0 0 1 560 228"
            stroke="url(#udt-hero-line-grad)"
            strokeWidth="2"
            strokeDasharray="24 8"
            opacity="0.45"
          />
        </g>

        {/* Central target core & orbital nodes */}
        <g fill="currentColor">
          <circle cx="560" cy="140" r="4" opacity="0.5" />
          <circle cx="560" cy="25" r="3.5" opacity="0.65" />
          <circle cx="675" cy="140" r="3" opacity="0.5" />
          <circle cx="472" cy="140" r="2.5" opacity="0.4" />
          <circle cx="622" cy="78" r="2" opacity="0.4" />
          <circle cx="498" cy="202" r="2" opacity="0.35" />
        </g>

        {/* PCB Hardware Circuit / Bus Traces */}
        <g fill="none" stroke="currentColor" strokeLinecap="round" strokeLinejoin="round">
          {/* Top Bus Line */}
          <path d="M 370 45 L 440 45 L 475 80 L 515 80" strokeWidth="1.5" opacity="0.3" />
          <path d="M 390 32 L 450 32 L 485 67 L 535 67" strokeWidth="1" strokeDasharray="8 4" opacity="0.22" />

          {/* Middle Connecting Line */}
          <path d="M 340 140 L 410 140 L 435 165 L 465 165" strokeWidth="1.2" opacity="0.25" />

          {/* Bottom Bus Line */}
          <path d="M 360 235 L 430 235 L 465 200 L 505 200" strokeWidth="1.5" opacity="0.32" />
          <path d="M 380 248 L 440 248 L 475 213 L 525 213" strokeWidth="1" strokeDasharray="6 4" opacity="0.2" />

          {/* Right Escaping Lines */}
          <path d="M 625 60 L 655 30 L 705 30" strokeWidth="1.5" opacity="0.3" />
          <path d="M 640 200 L 670 230 L 710 230" strokeWidth="1.5" opacity="0.3" />
          <path d="M 660 170 L 690 170" strokeWidth="1.2" opacity="0.22" />
        </g>

        {/* Solder Pads & Circuit Endpoints */}
        <g fill="currentColor">
          <circle cx="515" cy="80" r="2.5" opacity="0.45" />
          <circle cx="535" cy="67" r="2" opacity="0.35" />
          <circle cx="465" cy="165" r="2.5" opacity="0.4" />
          <circle cx="505" cy="200" r="2.5" opacity="0.45" />
          <circle cx="525" cy="213" r="2" opacity="0.35" />
          <circle cx="705" cy="30" r="2.5" opacity="0.4" />
          <circle cx="710" cy="230" r="2.5" opacity="0.4" />
          <circle cx="690" cy="170" r="2" opacity="0.3" />
        </g>

        {/* Tech Geometric Isometric Nodes */}
        <g fill="currentColor">
          <polygon points="630,75 655,60 680,75 655,90" opacity="0.08" />
          <polygon
            points="630,75 655,60 680,75 655,90"
            fill="none"
            stroke="currentColor"
            strokeWidth="1"
            opacity="0.25"
          />

          <polygon points="635,185 660,170 685,185 660,200" opacity="0.06" />
          <polygon
            points="635,185 660,170 685,185 660,200"
            fill="none"
            stroke="currentColor"
            strokeWidth="1"
            opacity="0.2"
          />
        </g>

        {/* Micro HUD Telemetry Bars & Micro Dots */}
        <g fill="currentColor">
          {/* Telemetry bar cluster */}
          <rect x="660" y="44" width="2" height="6" rx="1" opacity="0.2" />
          <rect x="665" y="42" width="2" height="8" rx="1" opacity="0.3" />
          <rect x="670" y="39" width="2" height="11" rx="1" opacity="0.45" />
          <rect x="675" y="41" width="2" height="9" rx="1" opacity="0.35" />
          <rect x="680" y="45" width="2" height="5" rx="1" opacity="0.2" />

          {/* Micro dots matrix */}
          <circle cx="420" cy="85" r="1.5" opacity="0.2" />
          <circle cx="435" cy="85" r="1.5" opacity="0.2" />
          <circle cx="420" cy="98" r="1.5" opacity="0.15" />
          <circle cx="435" cy="98" r="1.5" opacity="0.15" />

          <circle cx="610" cy="225" r="1.5" opacity="0.2" />
          <circle cx="625" cy="225" r="1.5" opacity="0.2" />
        </g>

        {/* Precision Crosshairs (+) */}
        <g stroke="currentColor" strokeWidth="1" opacity="0.35">
          <path d="M 440 60 L 440 68 M 436 64 L 444 64" />
          <path d="M 615 110 L 615 118 M 611 114 L 619 114" />
          <path d="M 455 215 L 455 223 M 451 219 L 459 219" />
          <path d="M 670 245 L 670 253 M 666 249 L 674 249" />
        </g>
      </g>
    </svg>
  )
}

export default function AboutPage(): React.JSX.Element {
  const { t, i18n } = useTranslation()
  const [appStatus, setAppStatus] = useState<AppStatus | null>(null)
  const [statusError, setStatusError] = useState(false)

  const loadAppStatus = useCallback((): void => {
    void invoke<AppStatus>('app.getStatus')
      .then((status) => {
        setStatusError(false)
        setAppStatus(status)
      })
      .catch(() => {
        setStatusError(true)
      })
  }, [])

  useEffect(() => {
    loadAppStatus()
  }, [loadAppStatus])

  const version = statusError
    ? t('common.error')
    : (appStatus?.version ?? t('common.loading'))
  const build = appStatus?.build?.trim() ?? ''
  const isEnglish = i18n.language.toLowerCase().startsWith('en')

  const openApplicationDataFolder = (): void => {
    void window.bridge?.openAppFolder?.('data')
  }

  const openApplicationTempFolder = (): void => {
    void window.bridge?.openLogFolder?.()
  }

  const handleExternalLink = (url: string) => (event: React.MouseEvent<HTMLAnchorElement>): void => {
    event.preventDefault()
    void openExternalUrl(url)
  }

  return (
    <div className="udt-about-page udt-content-column udt-content-fill">
      <section className="udt-about-page__hero">
        <AboutHeroPattern />
        <div className="udt-about-page__brand">
          <img className="udt-about-page__mark" src={appMarkUrl} alt="" width={96} height={96} />
          <div className="udt-about-page__identity">
            <h1 className="udt-page-title">{t('about.title')}</h1>
            <p className="udt-about-page__app-name">{t('app.name')}</p>
            <div className="udt-about-page__badges">
              <span className="udt-about-page__badge" aria-live="polite">
                {t('about.version')} {version}
              </span>
              {build !== '' && (
                <span className="udt-about-page__badge">
                  {t('about.build')} {build}
                </span>
              )}
              {statusError && (
                <button type="button" className="udt-link-button" onClick={loadAppStatus}>
                  {t('common.retry')}
                </button>
              )}
            </div>
            <p className="udt-about-page__copyright">
              {t('about.copyright')} © SSC-STUDIO
            </p>
            {!isEnglish && (
              <p className="udt-about-page__credit">
                {t('about.translationCredit', 'Translated by Karl Lee.')}
              </p>
            )}
          </div>
        </div>
      </section>

      <div className="udt-about-page__grid">
        <section className="udt-about-page__card udt-about-page__card--links">
          <h2 className="udt-subsection-title">{t('about.links')}</h2>
          <a
            className="udt-about-page__tile"
            href={PROJECT_URL}
            target="_blank"
            rel="noreferrer"
            onClick={handleExternalLink(PROJECT_URL)}
          >
            <span className="udt-about-page__tile-icon">
              <Link24Regular />
            </span>
            {t('about.projectWebsite')}
          </a>
          <a
            className="udt-about-page__tile"
            href={LATEST_RELEASE_URL}
            target="_blank"
            rel="noreferrer"
            onClick={handleExternalLink(LATEST_RELEASE_URL)}
          >
            <span className="udt-about-page__tile-icon">
              <Link24Regular />
            </span>
            {t('about.latestRelease')}
          </a>
        </section>

        <section className="udt-about-page__card udt-about-page__card--libs">
          <h2 className="udt-subsection-title">{t('about.thirdParty')}</h2>
          <div className="udt-about-page__chips">
            {THIRD_PARTY_LIBRARIES.map((lib) => (
              <a
                key={lib.name}
                className="udt-about-page__chip"
                href={lib.url}
                target="_blank"
                rel="noreferrer"
                onClick={handleExternalLink(lib.url)}
              >
                {lib.name}
              </a>
            ))}
          </div>
        </section>

        <section className="udt-about-page__card udt-about-page__card--folders">
          <h2 className="udt-subsection-title">{t('about.applicationFolders')}</h2>
          <button type="button" className="udt-about-page__tile" onClick={openApplicationDataFolder}>
            <span className="udt-about-page__tile-icon">
              <FolderOpen24Regular />
            </span>
            {t('about.data')}
          </button>
          <button type="button" className="udt-about-page__tile" onClick={openApplicationTempFolder}>
            <span className="udt-about-page__tile-icon">
              <FolderOpen24Regular />
            </span>
            {t('about.temp')}
          </button>
        </section>
      </div>
    </div>
  )
}
