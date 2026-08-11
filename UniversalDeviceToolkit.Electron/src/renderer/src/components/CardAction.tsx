import './custom.css'
import type { ReactNode } from 'react'

/**
 * Clickable action card row — port of WPF Controls/Custom/CardAction.cs
 * (Wpf.Ui CardAction: icon / title / description / click + accessory slot).
 */

export interface CardActionProps {
  icon?: ReactNode
  title?: string
  description?: string
  onClick?: () => void
  disabled?: boolean
  /** Right-side accessory (e.g. chevron, badge, button). */
  accessory?: ReactNode
  className?: string
  titleAttr?: string
}

export default function CardAction({
  icon,
  title,
  description,
  onClick,
  disabled = false,
  accessory,
  className,
  titleAttr
}: CardActionProps): React.JSX.Element {
  const classes = ['udt-card-action']
  if (className) classes.push(className)
  return (
    <button
      type="button"
      className={classes.join(' ')}
      onClick={onClick}
      disabled={disabled}
      title={titleAttr ?? (typeof title === 'string' ? title : undefined)}
    >
      {icon != null && <span className="udt-card-action__icon" aria-hidden="true">{icon}</span>}
      <span className="udt-card-action__copy">
        {title != null && title !== '' && <span className="udt-card-action__title">{title}</span>}
        {description != null && description !== '' && (
          <span className="udt-card-action__desc">{description}</span>
        )}
      </span>
      {accessory != null && <span className="udt-card-action__accessory">{accessory}</span>}
    </button>
  )
}
