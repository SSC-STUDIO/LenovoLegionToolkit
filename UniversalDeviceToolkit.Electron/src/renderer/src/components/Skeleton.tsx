import { useTranslation } from 'react-i18next'

// Electron SkeletonAnimationTokens parity: 1.65s cycle, 0.055s stagger step capped
// at 0.32s. Delays are negative so the phase offset is permanent (Loading.xaml
// uses SkeletonShimmer.DelaySeconds = -1) — the shimmer wave never re-syncs.
const STAGGER_STEP_S = 0.055
const STAGGER_MAX_S = 0.32
// Elements per SkeletonList row: icon + 2 lines + switch (fixed usage below).
const LIST_ROW_ELEMENTS = 4

type ShimmerDelayStyle = React.CSSProperties & { '--udt-shimmer-delay': string }

function staggerDelay(step: number): ShimmerDelayStyle {
  const seconds = -Math.min(step * STAGGER_STEP_S, STAGGER_MAX_S)
  return { '--udt-shimmer-delay': `${seconds}s` }
}

export interface SkeletonCardProps {
  lines?: number
  withIcon?: boolean
  withSwitch?: boolean
  className?: string
  /** Stagger step offset (in 0.055s steps) so cards in a list cascade. */
  staggerBase?: number
}

function joinClass(...parts: Array<string | undefined>): string {
  return parts.filter(Boolean).join(' ')
}

export function SkeletonCard({
  lines = 2,
  withIcon = false,
  withSwitch = false,
  className,
  staggerBase = 0
}: SkeletonCardProps): React.JSX.Element {
  const { t } = useTranslation()
  const count = Math.max(1, lines)
  const lineStart = staggerBase + (withIcon ? 1 : 0)
  const switchStep = lineStart + count
  return (
    <div
      className={joinClass('udt-skeleton udt-skeleton-card', className)}
      style={staggerDelay(staggerBase)}
      role="status"
      aria-label={t('common.loading')}
    >
      {withIcon && (
        <div style={staggerDelay(staggerBase)} className="udt-skeleton udt-skeleton-icon" />
      )}
      <div className="udt-skeleton-card__copy">
        {Array.from({ length: count }, (_, index) => (
          <div
            key={index}
            style={staggerDelay(lineStart + index)}
            className={
              index === 0
                ? 'udt-skeleton udt-skeleton-line'
                : 'udt-skeleton udt-skeleton-line udt-skeleton-line--secondary'
            }
          />
        ))}
      </div>
      {withSwitch && (
        <div style={staggerDelay(switchStep)} className="udt-skeleton udt-skeleton-switch" />
      )}
    </div>
  )
}

export function SkeletonIcon({ className }: { className?: string }): React.JSX.Element {
  const { t } = useTranslation()
  return <div className={joinClass('udt-skeleton udt-skeleton-icon', className)} role="status" aria-label={t('common.loading')} />
}

export function SkeletonSwitch({ className }: { className?: string }): React.JSX.Element {
  const { t } = useTranslation()
  return <div className={joinClass('udt-skeleton udt-skeleton-switch', className)} role="status" aria-label={t('common.loading')} />
}

export function SkeletonList({
  rows = 3,
  className
}: {
  rows?: number
  className?: string
}): React.JSX.Element {
  const { t } = useTranslation()
  return (
    <div className={joinClass('udt-skeleton-list', className)} role="status" aria-label={t('common.loading')}>
      {Array.from({ length: Math.max(1, rows) }, (_, index) => (
        <SkeletonCard key={index} lines={2} withIcon withSwitch staggerBase={index * LIST_ROW_ELEMENTS} />
      ))}
    </div>
  )
}
