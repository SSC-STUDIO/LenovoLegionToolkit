import { useEffect, useMemo, useRef, useState } from 'react'
import { SPECTRUM_KEYBOARD_LAYOUTS, type SpectrumKeyboardLayoutName } from './keyboardLayouts'
import { getKeyboardZoneCenters } from './deviceLayouts'
import { useBoxSelect } from './useBoxSelect'

export interface SpectrumKeyboardProps {
  layout: SpectrumKeyboardLayoutName
  /** Key codes present on this device; others render dimmed and non-interactive. */
  deviceKeys: number[]
  /** Currently selected key codes (per effect). */
  selected: Set<number>
  onToggleKey?: (code: number) => void
  /** Optional per-key paint color (effect preview / animation). */
  keyColors?: Map<number, string>
  /** Called with every selected key code when a drag-box selection ends. */
  onBoxSelect?: (codes: number[]) => void
  /**
   * Fixed zoom for the stage; when set, the fit-to-container scaling is
   * skipped. Used by SpectrumDevicePanel (the keyboard renders at zoom 1
   * inside the device canvas, which is zoomed as a whole).
   */
  fixedScale?: number
  /** Enable drag-box selection on the keyboard (default true). */
  boxSelectable?: boolean
  /**
   * External drag reference for click suppression — used when an ancestor
   * (SpectrumDevicePanel) owns the box selection.
   */
  clickSuppressRef?: React.MutableRefObject<boolean>
}

const DESIGN_WIDTH = 660
const ZOOM_MIN = 0.5
const ZOOM_MAX = 1.5

/**
 * Spectrum keyboard grid — port of Electron SpectrumKeyboardControl + the three
 * layout XAMLs. Zones are buttons sized by the Electron zone tables; the whole
 * grid scales to fit its container (Electron Viewbox) with a manual zoom clamp.
 * Drag-box selection mirrors the Electron SelectableControl wrapper: mouse-down
 * starts a selection rectangle, mouse-up collects the key centers inside it
 * (moves under 4px are clicks).
 */
export default function SpectrumKeyboard({
  layout,
  deviceKeys,
  selected,
  onToggleKey,
  keyColors,
  onBoxSelect,
  fixedScale,
  boxSelectable = true,
  clickSuppressRef
}: SpectrumKeyboardProps): React.JSX.Element {
  const hostRef = useRef<HTMLDivElement | null>(null)
  const [fitScale, setFitScale] = useState(1)

  useEffect(() => {
    if (fixedScale !== undefined) return
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
  }, [fixedScale])

  const scale = fixedScale ?? fitScale
  const deviceSet = new Set(deviceKeys)
  const rows = SPECTRUM_KEYBOARD_LAYOUTS[layout]

  // Selectable key centers in host px (already scaled), only for keys present
  // on the device — matches Electron GetVisibleButtons (visible zones only).
  const keyCenters = useMemo(
    () =>
      getKeyboardZoneCenters(layout, scale).filter((center) => deviceSet.has(center.code)),
    // eslint-disable-next-line react-hooks/exhaustive-deps
    [layout, scale, deviceKeys]
  )

  const { selection, didDragRef, onMouseDown } = useBoxSelect(
    hostRef,
    boxSelectable ? keyCenters : [],
    onBoxSelect,
    boxSelectable
  )
  const dragRef = clickSuppressRef ?? didDragRef

  return (
    <div className="udt-spectrum-keyboard" ref={hostRef} onMouseDown={onMouseDown}>
      {/* CSS zoom (Chromium) keeps the scaled size in layout, like the Electron Viewbox. */}
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
                  aria-disabled={!available}
                  title={available ? `0x${zone.code!.toString(16).toUpperCase()}` : undefined}
                  onClick={() => {
                    if (dragRef.current) return
                    if (!available) return
                    onToggleKey?.(zone.code!)
                  }}
                />
              )
            })}
          </div>
        ))}
      </div>
      {boxSelectable && selection !== null && (
        <div
          className="udt-spectrum-keyboard__selection"
          style={{
            left: selection.left,
            top: selection.top,
            width: selection.width,
            height: selection.height
          }}
        />
      )}
    </div>
  )
}
