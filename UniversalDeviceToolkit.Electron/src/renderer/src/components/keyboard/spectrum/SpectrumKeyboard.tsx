import { useEffect, useRef, useState } from 'react'
import { SPECTRUM_KEYBOARD_LAYOUTS, type SpectrumKeyboardLayoutName } from './keyboardLayouts'

export interface SpectrumKeyboardProps {
  layout: SpectrumKeyboardLayoutName
  /** Key codes present on this device; others render dimmed and non-interactive. */
  deviceKeys: number[]
  /** Currently selected key codes (per effect). */
  selected: Set<number>
  onToggleKey?: (code: number) => void
  /** Optional per-key paint color (effect preview / animation). */
  keyColors?: Map<number, string>
}

const DESIGN_WIDTH = 660
const ZOOM_MIN = 0.5
const ZOOM_MAX = 1.5

/**
 * Spectrum keyboard grid — port of WPF SpectrumKeyboardControl + the three
 * layout XAMLs. Zones are buttons sized by the WPF zone tables; the whole
 * grid scales to fit its container (WPF Viewbox) with a manual zoom clamp.
 */
export default function SpectrumKeyboard({
  layout,
  deviceKeys,
  selected,
  onToggleKey,
  keyColors
}: SpectrumKeyboardProps): React.JSX.Element {
  const hostRef = useRef<HTMLDivElement | null>(null)
  const [fitScale, setFitScale] = useState(1)

  useEffect(() => {
    const host = hostRef.current
    if (!host) return
    const update = (): void => {
      const width = host.clientWidth
      if (width > 0) setFitScale(Math.max(ZOOM_MIN, Math.min(width / DESIGN_WIDTH, ZOOM_MAX)))
    }
    update()
    const observer = new ResizeObserver(update)
    observer.observe(host)
    return () => observer.disconnect()
  }, [])

  const scale = fitScale
  const deviceSet = new Set(deviceKeys)
  const rows = SPECTRUM_KEYBOARD_LAYOUTS[layout]

  return (
    <div className="udt-spectrum-keyboard" ref={hostRef}>
      {/* CSS zoom (Chromium) keeps the scaled size in layout, like the WPF Viewbox. */}
      <div className="udt-spectrum-keyboard__stage" style={{ zoom: scale }}>
        {rows.map((row, rowIndex) => (
          <div key={rowIndex} className="udt-spectrum-keyboard__row">
            {row.map((zone, zoneIndex) => {
              const hasCode = zone.code !== null
              const available = hasCode && deviceSet.has(zone.code!)
              const isSelected = hasCode && selected.has(zone.code!)
              const color = hasCode ? keyColors?.get(zone.code!) : undefined
              const style: React.CSSProperties = { width: zone.w, height: zone.h }
              if (color) style.backgroundColor = color
              const zoneStyle = [
                'udt-spectrum-keyboard__zone',
                !hasCode && 'udt-spectrum-keyboard__zone--spacer',
                hasCode && !available && 'udt-spectrum-keyboard__zone--unavailable',
                isSelected && 'udt-spectrum-keyboard__zone--selected',
                color && 'udt-spectrum-keyboard__zone--colored'
              ]
                .filter(Boolean)
                .join(' ')
              if (!hasCode) {
                return <span key={zoneIndex} className={zoneStyle} style={style} aria-hidden="true" />
              }
              return (
                <button
                  key={zoneIndex}
                  type="button"
                  className={zoneStyle}
                  style={style}
                  aria-pressed={isSelected}
                  title={available ? `0x${zone.code!.toString(16).toUpperCase()}` : undefined}
                  disabled={!available}
                  onClick={() => onToggleKey?.(zone.code!)}
                />
              )
            })}
          </div>
        ))}
      </div>
    </div>
  )
}
