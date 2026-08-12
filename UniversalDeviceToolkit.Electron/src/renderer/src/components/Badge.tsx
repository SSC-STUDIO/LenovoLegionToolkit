import './custom.css'
import type { ReactNode } from 'react'

/**
 * Small label badge — port of Electron Controls/Custom/Badge.cs (Wpf.Ui Badge).
 * Base shape reuses the global .udt-badge chrome; appearance adds a tone.
 */

export type BadgeAppearance = 'default' | 'plain' | 'success' | 'warning' | 'danger' | 'info'

export interface BadgeProps {
  children?: ReactNode
  /** Short text content (alternative to children). */
  text?: string
  appearance?: BadgeAppearance
  className?: string
  title?: string
}

export default function Badge({
  children,
  text,
  appearance = 'default',
  className,
  title
}: BadgeProps): React.JSX.Element {
  const classes = ['udt-badge']
  if (appearance !== 'default') classes.push(`udt-badge--${appearance}`)
  if (className) classes.push(className)
  return (
    <span className={classes.join(' ')} title={title}>
      {children ?? text}
    </span>
  )
}
