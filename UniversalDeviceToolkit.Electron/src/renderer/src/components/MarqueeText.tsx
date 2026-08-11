import { useEffect, useRef, useState } from 'react'
import './MarqueeText.css'

/**
 * Marquee text — port of WPF Controls/MarqueeTextBlock.cs. When the text
 * overflows its box it fades out at the right edge (TextOverflowFadeBehavior);
 * on hover it scrolls smoothly to reveal the tail.
 */

export interface MarqueeTextProps {
  text: string
  className?: string
  fade?: boolean
}

export default function MarqueeText({ text, className, fade = true }: MarqueeTextProps): React.JSX.Element {
  const containerRef = useRef<HTMLSpanElement | null>(null)
  const [overflow, setOverflow] = useState(false)

  useEffect(() => {
    const el = containerRef.current
    if (!el) return
    const measure = (): void => {
      setOverflow(el.scrollWidth > el.clientWidth + 1)
    }
    measure()
    const observer = new ResizeObserver(measure)
    observer.observe(el)
    return () => observer.disconnect()
  }, [text])

  const handleEnter = (): void => {
    const el = containerRef.current
    if (el && el.scrollWidth > el.clientWidth) {
      el.scrollLeft = el.scrollWidth - el.clientWidth
    }
  }

  const handleLeave = (): void => {
    const el = containerRef.current
    if (el) el.scrollLeft = 0
  }

  const classes = [
    'udt-marquee',
    overflow && 'udt-marquee--overflow',
    fade && overflow && 'udt-marquee--fade',
    className
  ]
    .filter(Boolean)
    .join(' ')

  return (
    <span
      ref={containerRef}
      className={classes}
      title={text}
      onMouseEnter={handleEnter}
      onMouseLeave={handleLeave}
    >
      <span className="udt-marquee__inner">{text}</span>
    </span>
  )
}
