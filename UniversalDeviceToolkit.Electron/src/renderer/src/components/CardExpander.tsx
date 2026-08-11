import './custom.css'
import { useEffect, useState } from 'react'
import type { ReactNode } from 'react'
import { DownOutlined } from '@ant-design/icons'

/**
 * Expandable card — port of WPF Controls/Custom/CardExpander.cs
 * (Wpf.Ui CardExpander: clickable header toggles the body).
 */

export interface CardExpanderProps {
  /** Header content; when a string, rendered as the card title. */
  header?: ReactNode
  /** Secondary text under a string header. */
  description?: string
  defaultExpanded?: boolean
  expanded?: boolean
  onExpandedChange?: (expanded: boolean) => void
  children?: ReactNode
  className?: string
}

export default function CardExpander({
  header,
  description,
  defaultExpanded = false,
  expanded,
  onExpandedChange,
  children,
  className
}: CardExpanderProps): React.JSX.Element {
  const [internal, setInternal] = useState(defaultExpanded)
  const isExpanded = expanded ?? internal

  useEffect(() => {
    if (expanded !== undefined) setInternal(expanded)
  }, [expanded])

  const toggle = (): void => {
    const next = !isExpanded
    setInternal(next)
    onExpandedChange?.(next)
  }

  const classes = ['udt-card-expander']
  if (isExpanded) classes.push('udt-card-expander--expanded')
  if (className) classes.push(className)

  return (
    <div className={classes.join(' ')}>
      <button type="button" className="udt-card-expander__header" onClick={toggle} aria-expanded={isExpanded}>
        <span className="udt-card-expander__copy">
          {typeof header === 'string' ? (
            <>
              <span className="udt-card-expander__title">{header}</span>
              {description != null && description !== '' && (
                <span className="udt-card-expander__desc">{description}</span>
              )}
            </>
          ) : (
            header
          )}
        </span>
        <span className="udt-card-expander__chevron" aria-hidden="true">
          <DownOutlined />
        </span>
      </button>
      {isExpanded && <div className="udt-card-expander__body">{children}</div>}
    </div>
  )
}
