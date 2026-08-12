import { useTranslation } from 'react-i18next'
import { SkeletonCard } from './Skeleton'
import './DashboardSkeleton.css'

/**
 * Dashboard page skeleton — 1:1 port of WPF DashboardPage.xaml loading chrome.
 *
 * DashboardPage owns its loading chrome (LoadingChromeOwnership.Page), so the
 * generic navigation skeleton and the global spinner never flash; this single
 * page-level skeleton mirrors the live layout:
 *   - Sensors card: 3 columns (CPU / battery / GPU), each with title+model,
 *     GaugeSizeMD (110px) + 3 metric rows (24px), trend well, 3-item legend.
 *   - Feature group cards below (list-item rows, 72px min-height).
 *
 * Staggering (0.055s/step, cap 0.32s) cascades across every skeleton block.
 */

const METRIC_ROWS = 3

interface SensorSkeletonColumnProps {
  titleWidth: number
  subtitleWidth: number
  metricWidths: number[]
  staggerBase: number
}

/** One CPU/battery/GPU skeleton column — reused by SensorSection while the
 *  first snapshot is still loading. */
export function SensorSkeletonColumn({
  titleWidth,
  subtitleWidth,
  metricWidths,
  staggerBase
}: SensorSkeletonColumnProps): React.JSX.Element {
  const labelWidths = [52, 44, 40]
  const valueWidths = [58, 48, 36]
  return (
    <div className="udt-dsk-sensors__column">
      <div className="udt-dsk-sensors__heading">
        <div
          className="udt-skeleton"
          style={{
            width: titleWidth,
            height: 20,
            borderRadius: 'var(--udt-radius-small)',
            ['--udt-shimmer-delay' as string]: `${-Math.min(staggerBase * 0.055, 0.32)}s`
          }}
        />
        <div
          className="udt-skeleton"
          style={{
            width: subtitleWidth,
            height: 14,
            marginLeft: 8,
            marginTop: 1,
            borderRadius: 'var(--udt-radius-small)',
            ['--udt-shimmer-delay' as string]: `${-Math.min((staggerBase + 1) * 0.055, 0.32)}s`
          }}
        />
      </div>
      <div className="udt-dsk-sensors__summary">
        <div
          className="udt-skeleton udt-dsk-sensors__gauge"
          style={{
            ['--udt-shimmer-delay' as string]: `${-Math.min((staggerBase + 2) * 0.055, 0.32)}s`
          }}
        />
        <div className="udt-dsk-sensors__metrics">
          {Array.from({ length: METRIC_ROWS }, (_, row) => (
            <div key={row} className="udt-dsk-sensors__metric">
              <div
                className="udt-skeleton"
                style={{
                  width: metricWidths[row] ?? labelWidths[row],
                  height: 14,
                  borderRadius: 'var(--udt-radius-small)',
                  ['--udt-shimmer-delay' as string]: `${-Math.min((staggerBase + 3 + row * 3) * 0.055, 0.32)}s`
                }}
              />
              <div
                className="udt-skeleton udt-dsk-sensors__bar"
                style={{
                  ['--udt-shimmer-delay' as string]: `${-Math.min((staggerBase + 4 + row * 3) * 0.055, 0.32)}s`
                }}
              />
              <div
                className="udt-skeleton"
                style={{
                  width: valueWidths[row],
                  height: 14,
                  borderRadius: 'var(--udt-radius-small)',
                  ['--udt-shimmer-delay' as string]: `${-Math.min((staggerBase + 5 + row * 3) * 0.055, 0.32)}s`
                }}
              />
            </div>
          ))}
        </div>
      </div>
      <div className="udt-dsk-sensors__trend">
        <div
          className="udt-skeleton"
          style={{
            width: '100%',
            height: '100%',
            borderRadius: 'var(--udt-radius-small)',
            ['--udt-shimmer-delay' as string]: `${-Math.min((staggerBase + 11) * 0.055, 0.32)}s`
          }}
        />
      </div>
      <div className="udt-dsk-sensors__legend">
        {Array.from({ length: 3 }, (_, item) => (
          <span key={item} className="udt-dsk-sensors__legend-item">
            <div
              className="udt-skeleton"
              style={{
                width: 8,
                height: 8,
                borderRadius: 'var(--udt-radius-small)',
                ['--udt-shimmer-delay' as string]: `${-Math.min((staggerBase + 12 + item) * 0.055, 0.32)}s`
              }}
            />
            <div
              className="udt-skeleton"
              style={{
                width: 48,
                height: 12,
                marginLeft: 6,
                borderRadius: 'var(--udt-radius-small)',
                ['--udt-shimmer-delay' as string]: `${-Math.min((staggerBase + 12 + item) * 0.055, 0.32)}s`
              }}
            />
          </span>
        ))}
      </div>
    </div>
  )
}

export default function DashboardSkeleton(): React.JSX.Element {
  const { t } = useTranslation()
  return (
    <div className="udt-dashboard-skeleton" role="status" aria-label={t('common.loading')}>
      <div className="udt-dsk-sensors">
        <div className="udt-dsk-sensors__grid">
          <SensorSkeletonColumn
            titleWidth={72}
            subtitleWidth={168}
            metricWidths={[52, 44, 40]}
            staggerBase={0}
          />
          <SensorSkeletonColumn
            titleWidth={64}
            subtitleWidth={140}
            metricWidths={[48, 44, 44]}
            staggerBase={16}
          />
          <SensorSkeletonColumn
            titleWidth={68}
            subtitleWidth={152}
            metricWidths={[52, 40, 44]}
            staggerBase={32}
          />
        </div>
      </div>
      <div className="udt-dsk-groups">
        <div className="udt-dsk-groups__grid">
          {Array.from({ length: 3 }, (_, group) => (
            <div key={group} className="udt-dsk-groups__column">
              {Array.from({ length: 2 }, (_, card) => (
                <SkeletonCard
                  key={card}
                  lines={2}
                  withIcon
                  withSwitch
                  staggerBase={group * 12 + card * 5}
                />
              ))}
            </div>
          ))}
        </div>
      </div>
    </div>
  )
}
