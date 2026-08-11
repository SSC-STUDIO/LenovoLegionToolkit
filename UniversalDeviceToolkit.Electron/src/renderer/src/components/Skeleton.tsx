export interface SkeletonCardProps {
  lines?: number
  withIcon?: boolean
  withSwitch?: boolean
  className?: string
}

function joinClass(...parts: Array<string | undefined>): string {
  return parts.filter(Boolean).join(' ')
}

export function SkeletonCard({
  lines = 2,
  withIcon = false,
  withSwitch = false,
  className
}: SkeletonCardProps): React.JSX.Element {
  const count = Math.max(1, lines)
  return (
    <div className={joinClass('udt-skeleton udt-skeleton-card', className)} role="status" aria-label="Loading">
      {withIcon && <div className="udt-skeleton udt-skeleton-icon" />}
      <div className="udt-skeleton-card__copy">
        {Array.from({ length: count }, (_, index) => (
          <div
            key={index}
            className={
              index === 0
                ? 'udt-skeleton udt-skeleton-line'
                : 'udt-skeleton udt-skeleton-line udt-skeleton-line--secondary'
            }
          />
        ))}
      </div>
      {withSwitch && <div className="udt-skeleton udt-skeleton-switch" />}
    </div>
  )
}

export function SkeletonIcon({ className }: { className?: string }): React.JSX.Element {
  return <div className={joinClass('udt-skeleton udt-skeleton-icon', className)} role="status" aria-label="Loading" />
}

export function SkeletonSwitch({ className }: { className?: string }): React.JSX.Element {
  return <div className={joinClass('udt-skeleton udt-skeleton-switch', className)} role="status" aria-label="Loading" />
}

export function SkeletonList({
  rows = 3,
  className
}: {
  rows?: number
  className?: string
}): React.JSX.Element {
  return (
    <div className={joinClass('udt-skeleton-list', className)} role="status" aria-label="Loading">
      {Array.from({ length: Math.max(1, rows) }, (_, index) => (
        <SkeletonCard key={index} lines={2} withIcon withSwitch />
      ))}
    </div>
  )
}
