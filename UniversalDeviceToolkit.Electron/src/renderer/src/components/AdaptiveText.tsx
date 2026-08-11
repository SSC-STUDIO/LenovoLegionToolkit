import { useEffect, useRef, useState } from 'react'

/**
 * Adaptive text block — port of WPF Controls/AdaptiveTextBlock.cs. Shrinks the
 * font size in fixed steps until the text fits its container (or the minimum
 * size is reached).
 */

export interface AdaptiveTextProps {
  text: string
  className?: string
  fontSize?: number
  minFontSize?: number
  step?: number
  maxLines?: number
  title?: string
}

export default function AdaptiveText({
  text,
  className,
  fontSize = 14,
  minFontSize = 8,
  step = 1,
  maxLines = 1,
  title
}: AdaptiveTextProps): React.JSX.Element {
  const ref = useRef<HTMLSpanElement | null>(null)
  const [size, setSize] = useState(fontSize)

  useEffect(() => {
    setSize(fontSize)
  }, [fontSize, text])

  useEffect(() => {
    const el = ref.current
    if (!el) return
    let current = fontSize
    let fits = false
    while (current > minFontSize && !fits) {
      el.style.fontSize = `${current}px`
      const overflowing = el.scrollWidth > el.clientWidth + 1 || el.scrollHeight > el.clientHeight + 1
      if (!overflowing) {
        fits = true
      } else {
        current -= step
      }
    }
    if (!fits) el.style.fontSize = `${minFontSize}px`
    setSize(current)
  }, [fontSize, minFontSize, step, text])

  return (
    <span
      ref={ref}
      className={`udt-adaptive-text${className ? ` ${className}` : ''}`}
      style={{
        fontSize: `${size}px`,
        lineHeight: 1.3,
        display: '-webkit-box',
        WebkitLineClamp: maxLines,
        WebkitBoxOrient: 'vertical',
        overflow: 'hidden',
        textOverflow: 'ellipsis',
        wordBreak: 'break-word'
      }}
      title={title ?? text}
    >
      {text}
    </span>
  )
}
