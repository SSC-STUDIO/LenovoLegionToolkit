import './colorPicker.css'
import { CloseOutlined, PlusOutlined } from '@ant-design/icons'
import { Button } from 'antd'
import ColorPicker from './ColorPicker'

// Port of WPF Controls/MultiColorPickerControl + MultiColorPickerItemControl:
// a row of up to maxItems color swatches (each with a dismiss chip overlapping
// its top-right corner) and an add button that is disabled at the limit.
// Changes bubble up as ColorsChangedContinuous / ColorsChangedDelayed.

export interface MultiColorPickerProps {
  value?: string[]
  onChange?: (colors: string[]) => void
  onChangeDelayed?: (colors: string[]) => void
  maxItems?: number
  size?: number
  disabled?: boolean
}

const DEFAULT_MAX_ITEMS = 3

export default function MultiColorPicker({
  value = [],
  onChange,
  onChangeDelayed,
  maxItems = DEFAULT_MAX_ITEMS,
  size,
  disabled = false
}: MultiColorPickerProps): React.JSX.Element {
  const colors = value.slice(0, maxItems)
  const canAdd = colors.length < maxItems

  const handleAdd = (): void => {
    if (!canAdd) return
    const next = [...colors, '#00ffff']
    onChange?.(next)
    onChangeDelayed?.(next)
  }

  const handleDelete = (index: number): void => {
    const next = colors.filter((_, i) => i !== index)
    onChange?.(next)
    onChangeDelayed?.(next)
  }

  const replaceAt = (index: number, hex: string): string[] => {
    const next = [...colors]
    next[index] = hex
    return next
  }

  return (
    <div className="udt-multi-color-picker">
      {colors.map((color, index) => (
        <div key={index} className="udt-multi-color-picker__item">
          <ColorPicker
            value={color}
            size={size}
            disabled={disabled}
            onChangeContinuous={(hex) => onChange?.(replaceAt(index, hex))}
            onChangeDelayed={(hex) => onChangeDelayed?.(replaceAt(index, hex))}
          />
          <button
            type="button"
            className="udt-multi-color-picker__delete"
            aria-label="delete color"
            disabled={disabled}
            onClick={() => handleDelete(index)}
          >
            <CloseOutlined />
          </button>
        </div>
      ))}
      <Button
        type="primary"
        className="udt-multi-color-picker__add"
        icon={<PlusOutlined />}
        disabled={disabled || !canAdd}
        onClick={handleAdd}
      />
    </div>
  )
}
