import { useEffect, useRef, useState } from 'react'
import { DeleteOutlined, PlusOutlined } from '@ant-design/icons'
import { Button, Select, Tooltip } from 'antd'
import { useTranslation } from 'react-i18next'
import type {
  RgbColor,
  SpectrumClockwiseDirection,
  SpectrumDirection,
  SpectrumEffect,
  SpectrumEffectType,
  SpectrumSpeed
} from '../../../api/keyboard'
import { keyboardApi } from '../../../api/keyboard'
import ColorPicker from '../../ColorPicker'
import SpectrumKeyboard from './SpectrumKeyboard'
import { normalizeKeyboardLayout } from './keyboardLayouts'

export interface SpectrumEffectModalProps {
  effect: SpectrumEffect | null
  /** $type of the current keyboard layout ("Ansi" | "Iso" | "Jis" | ...). */
  keyboardLayout: string
  deviceKeys: number[]
  /** Paint the keyboard with the live device backlight state while editing. */
  previewEnabled?: boolean
  onApply: (effect: SpectrumEffect) => void
  onCancel: () => void
}

const EFFECT_TYPES: SpectrumEffectType[] = [
  'Always',
  'RainbowScrew',
  'RainbowWave',
  'ColorChange',
  'ColorWave',
  'ColorPulse',
  'Smooth',
  'Rain',
  'Ripple',
  'Type',
  'AudioBounce',
  'AudioRipple',
  'AuroraSync'
]

const SPEEDS: SpectrumSpeed[] = ['Speed1', 'Speed2', 'Speed3']
const DIRECTIONS: SpectrumDirection[] = ['BottomToTop', 'TopToBottom', 'LeftToRight', 'RightToLeft']
const CLOCKWISE_DIRECTIONS: SpectrumClockwiseDirection[] = ['Clockwise', 'CounterClockwise']

/**
 * WPF SpectrumKeyboardBacklightEffectTypeExtensions: "all lights" effects
 * ignore per-key selection; "whole keyboard" effects apply to every key.
 */
const ALL_LIGHTS_TYPES: SpectrumEffectType[] = ['AudioBounce', 'AudioRipple', 'AuroraSync']
const WHOLE_KEYBOARD_TYPES: SpectrumEffectType[] = ['Type', 'Ripple']

/** Effect types that expose the speed combo (WPF RefreshVisibility). */
const SPEED_VISIBLE_TYPES: SpectrumEffectType[] = [
  'ColorChange',
  'ColorPulse',
  'ColorWave',
  'Rain',
  'RainbowScrew',
  'RainbowWave',
  'Ripple',
  'Smooth',
  'Type'
]

/** Effect types that expose the multi-color picker (WPF RefreshVisibility). */
const MULTI_COLOR_TYPES: SpectrumEffectType[] = [
  'ColorChange',
  'ColorPulse',
  'ColorWave',
  'Rain',
  'Ripple',
  'Smooth',
  'Type'
]

const DEFAULT_SPEED: SpectrumSpeed = 'Speed2'
const DEFAULT_DIRECTION: SpectrumDirection = 'BottomToTop'
const DEFAULT_CLOCKWISE: SpectrumClockwiseDirection = 'Clockwise'

/** WPF Add-effect defaults (cards hidden until the type reveals them). */
const NEW_EFFECT: SpectrumEffect = {
  Type: 'Always',
  Speed: 'None',
  Direction: 'None',
  ClockwiseDirection: 'None',
  Colors: [{ R: 255, G: 255, B: 255 }],
  Keys: []
}

function isAllLights(type: SpectrumEffectType): boolean {
  return ALL_LIGHTS_TYPES.includes(type)
}

function isWholeKeyboard(type: SpectrumEffectType): boolean {
  return WHOLE_KEYBOARD_TYPES.includes(type)
}

function directionVisible(type: SpectrumEffectType): boolean {
  return type === 'ColorWave' || type === 'RainbowWave'
}

function clockwiseDirectionVisible(type: SpectrumEffectType): boolean {
  return type === 'RainbowScrew'
}

function speedVisible(type: SpectrumEffectType): boolean {
  return SPEED_VISIBLE_TYPES.includes(type)
}

function singleColorVisible(type: SpectrumEffectType): boolean {
  return type === 'Always'
}

function multiColorsVisible(type: SpectrumEffectType): boolean {
  return MULTI_COLOR_TYPES.includes(type)
}

const EFFECT_TYPE_LABEL_KEYS: Record<SpectrumEffectType, string> = {
  Always: 'always',
  RainbowScrew: 'rainbowScrew',
  RainbowWave: 'rainbowWave',
  ColorChange: 'colorChange',
  ColorWave: 'colorWave',
  ColorPulse: 'colorPulse',
  Smooth: 'smooth',
  Rain: 'rain',
  Ripple: 'ripple',
  Type: 'type',
  AudioBounce: 'audioBounce',
  AudioRipple: 'audioRipple',
  AuroraSync: 'auroraSync'
}

function colorToHex(color: RgbColor): string {
  const toHex = (value: number): string => value.toString(16).padStart(2, '0')
  return `#${toHex(color.R)}${toHex(color.G)}${toHex(color.B)}`
}

function toByteHex(value: number): string {
  const clamped = Math.min(255, Math.max(0, Math.round(value)))
  return clamped.toString(16).padStart(2, '0')
}

function hexToColor(hex: string): RgbColor {
  const value = hex.replace(/^#/, '')
  return {
    R: parseInt(value.slice(0, 2), 16),
    G: parseInt(value.slice(2, 4), 16),
    B: parseInt(value.slice(4, 6), 16)
  }
}

/**
 * Spectrum effect editor — port of WPF
 * SpectrumKeyboardBacklightEditEffectWindow: effect type, conditional
 * direction/clockwise/speed/color cards (RefreshVisibility), all-lights /
 * whole-keyboard warnings and the Apply key resolution.
 */
export default function SpectrumEffectModal({
  effect,
  keyboardLayout,
  deviceKeys,
  previewEnabled,
  onApply,
  onCancel
}: SpectrumEffectModalProps): React.JSX.Element | null {
  const { t } = useTranslation()
  const [draft, setDraft] = useState<SpectrumEffect>(effect ?? NEW_EFFECT)
  // Live backlight preview — WPF SpectrumKeyboardBacklightEditEffectWindow
  // polls GetStateAsync every 50ms and repaints the keycaps.
  const [previewColors, setPreviewColors] = useState<Map<number, string> | undefined>(undefined)
  const previewInFlightRef = useRef(false)

  useEffect(() => {
    if (!previewEnabled) {
      setPreviewColors(undefined)
      return
    }
    let cancelled = false
    const timer = window.setInterval(() => {
      if (previewInFlightRef.current) return
      previewInFlightRef.current = true
      keyboardApi
        .spectrumGetState()
        .then((result) => {
          if (cancelled) return
          const keys = result.keys ?? []
          if (keys.length === 0) {
            setPreviewColors(undefined)
            return
          }
          const map = new Map<number, string>()
          for (const keyColor of keys) {
            map.set(keyColor.key, `#${toByteHex(keyColor.r)}${toByteHex(keyColor.g)}${toByteHex(keyColor.b)}`)
          }
          setPreviewColors(map)
        })
        .catch(() => {
          if (!cancelled) setPreviewColors(undefined)
        })
        .finally(() => {
          previewInFlightRef.current = false
        })
    }, 50)
    return () => {
      cancelled = true
      window.clearInterval(timer)
      previewInFlightRef.current = false
    }
  }, [previewEnabled])

  useEffect(() => {
    setDraft(effect ?? NEW_EFFECT)
  }, [effect])

  useEffect(() => {
    // WPF SetItems: when the stored value is not in the combo list (a "None"
    // default written while the card was hidden), the default wins.
    const patch: Partial<SpectrumEffect> = {}
    if (speedVisible(draft.Type) && draft.Speed === 'None') patch.Speed = DEFAULT_SPEED
    if (directionVisible(draft.Type) && draft.Direction === 'None') patch.Direction = DEFAULT_DIRECTION
    if (clockwiseDirectionVisible(draft.Type) && draft.ClockwiseDirection === 'None') {
      patch.ClockwiseDirection = DEFAULT_CLOCKWISE
    }
    if (Object.keys(patch).length > 0) setDraft({ ...draft, ...patch })
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [effect])

  const allLights = isAllLights(draft.Type)
  const wholeKeyboard = isWholeKeyboard(draft.Type)
  const wholeDevice = allLights || wholeKeyboard
  const selected = new Set(wholeDevice ? deviceKeys : draft.Keys)

  const update = (patch: Partial<SpectrumEffect>): void => {
    setDraft({ ...draft, ...patch })
  }

  const handleTypeChange = (type: SpectrumEffectType): void => {
    const patch: Partial<SpectrumEffect> = { Type: type }
    if (speedVisible(type) && draft.Speed === 'None') patch.Speed = DEFAULT_SPEED
    if (directionVisible(type) && draft.Direction === 'None') patch.Direction = DEFAULT_DIRECTION
    if (clockwiseDirectionVisible(type) && draft.ClockwiseDirection === 'None') {
      patch.ClockwiseDirection = DEFAULT_CLOCKWISE
    }
    update(patch)
  }

  const handleToggleKey = (code: number): void => {
    if (wholeDevice) return
    const next = new Set(draft.Keys)
    if (next.has(code)) next.delete(code)
    else next.add(code)
    update({ Keys: [...next] })
  }

  const updateColor = (index: number, hex: string): void => {
    const colors = [...draft.Colors]
    colors[index] = hexToColor(hex)
    update({ Colors: colors })
  }

  const addColor = (): void => {
    update({ Colors: [...draft.Colors, { R: 255, G: 255, B: 255 }] })
  }

  const removeColor = (index: number): void => {
    update({ Colors: draft.Colors.filter((_, i) => i !== index) })
  }

  // WPF Apply_Click: AllLights effects carry no keys, whole-keyboard effects
  // carry every key code, everything else uses the selected keys.
  const handleApply = (): void => {
    const keys = allLights ? [] : wholeKeyboard ? [...deviceKeys] : [...selected]
    onApply({ ...draft, Keys: keys })
  }

  return (
    <div className="udt-modal-backdrop" onClick={onCancel}>
      <div className="udt-modal udt-spectrum-effect-modal" onClick={(e) => e.stopPropagation()}>
        <div className="udt-modal__title">
          {t(`keyboard.spectrum.effectEdit.${effect ? 'edit' : 'add'}Title`)}
        </div>

        <div className="udt-spectrum-effect-modal__fields">
          <label className="udt-spectrum-effect-modal__field">
            <span>{t('keyboard.spectrum.effectEdit.effect')}</span>
            <Select<SpectrumEffectType>
              size="small"
              value={draft.Type}
              onChange={handleTypeChange}
              options={EFFECT_TYPES.map((type) => ({
                value: type,
                label: t(`keyboard.spectrum.effectTypes.${EFFECT_TYPE_LABEL_KEYS[type]}`)
              }))}
            />
          </label>
          {speedVisible(draft.Type) && (
            <label className="udt-spectrum-effect-modal__field">
              <span>{t('keyboard.spectrum.effectEdit.speed')}</span>
              <Select<SpectrumSpeed>
                size="small"
                value={draft.Speed}
                onChange={(value) => update({ Speed: value })}
                options={SPEEDS.map((speed) => ({ value: speed, label: speed }))}
              />
            </label>
          )}
          {directionVisible(draft.Type) && (
            <label className="udt-spectrum-effect-modal__field">
              <span>{t('keyboard.spectrum.effectEdit.direction')}</span>
              <Select<SpectrumDirection>
                size="small"
                value={draft.Direction}
                onChange={(value) => update({ Direction: value })}
                options={DIRECTIONS.map((direction) => ({ value: direction, label: direction }))}
              />
            </label>
          )}
          {clockwiseDirectionVisible(draft.Type) && (
            <label className="udt-spectrum-effect-modal__field">
              <span>{t('keyboard.spectrum.effectEdit.clockwiseDirection')}</span>
              <Select<SpectrumClockwiseDirection>
                size="small"
                value={draft.ClockwiseDirection}
                onChange={(value) => update({ ClockwiseDirection: value })}
                options={CLOCKWISE_DIRECTIONS.map((direction) => ({
                  value: direction,
                  label: direction
                }))}
              />
            </label>
          )}
          {(singleColorVisible(draft.Type) || multiColorsVisible(draft.Type)) && (
            <label className="udt-spectrum-effect-modal__field">
              <span>
                {t(
                  `keyboard.spectrum.effectEdit.${
                    singleColorVisible(draft.Type) ? 'color' : 'colors'
                  }`
                )}
              </span>
              <div className="udt-spectrum-effect-modal__colors">
                {draft.Colors.map((color, index) => (
                  <div key={index} className="udt-spectrum-effect-modal__color">
                    <ColorPicker
                      size={30}
                      value={colorToHex(color)}
                      onChangeDelayed={(hex) => updateColor(index, hex)}
                    />
                    {multiColorsVisible(draft.Type) && draft.Colors.length > 1 && (
                      <Tooltip title={t('keyboard.spectrum.deleteEffect')}>
                        <Button
                          size="small"
                          className="udt-spectrum-effect-modal__color-remove"
                          icon={<DeleteOutlined />}
                          onClick={() => removeColor(index)}
                        />
                      </Tooltip>
                    )}
                  </div>
                ))}
                {multiColorsVisible(draft.Type) && (
                  <Tooltip title={t('keyboard.spectrum.effectEdit.addColor')}>
                    <Button size="small" icon={<PlusOutlined />} onClick={addColor} />
                  </Tooltip>
                )}
              </div>
            </label>
          )}
        </div>

        {wholeDevice && (
          <div className="udt-spectrum-effect-modal__warning">
            {t('keyboard.spectrum.effectEdit.alwaysWarning')}
          </div>
        )}

        <div className="udt-spectrum-effect-modal__keys">
          <div className="udt-spectrum-effect-modal__keys-title">
            <span>
              {t('keyboard.spectrum.effectEdit.keys')} ({selected.size})
            </span>
          </div>
          <SpectrumKeyboard
            layout={normalizeKeyboardLayout(keyboardLayout)}
            deviceKeys={deviceKeys}
            selected={selected}
            onToggleKey={handleToggleKey}
            keyColors={previewColors}
          />
        </div>

        <div className="udt-modal__actions">
          <button type="button" className="udt-btn udt-btn--secondary" onClick={onCancel}>
            {t('common.cancel', { defaultValue: 'Cancel' })}
          </button>
          <button
            type="button"
            className="udt-btn udt-btn--primary"
            disabled={selected.size === 0}
            onClick={handleApply}
          >
            {t('common.confirm', { defaultValue: 'OK' })}
          </button>
        </div>
      </div>
    </div>
  )
}
