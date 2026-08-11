import './custom.css'
import type { ReactNode } from 'react'

/**
 * Shared card container — port of WPF Controls/Custom/CardControl.cs
 * (Wpf.Ui CardControl: card chrome with optional header copy).
 */

export interface CardControlProps {
  title?: string
  description?: string
  /** Header accessory rendered on the right (e.g. a switch, badge or button). */
  accessory?: ReactNode
  children?: ReactNode
  className?: string
}

export default function CardControl({
  title,
  description,
  accessory,
  children,
  className
}: CardControlProps): React.JSX.Element {
  const classes = ['udt-card-control']
  if (className) classes.push(className)
  return (
    <section className={classes.join(' ')}>
      {(title != null || accessory != null) && (
        <div className="udt-card-control__header">
          {(title != null || description != null) && (
            <div className="udt-card-control__copy">
              {title != null && title !== '' && <div className="udt-card-control__title">{title}</div>}
              {description != null && description !== '' && (
                <div className="udt-card-control__desc">{description}</div>
              )}
            </div>
          )}
          {accessory != null && <div>{accessory}</div>}
        </div>
      )}
      {children}
    </section>
  )
}
