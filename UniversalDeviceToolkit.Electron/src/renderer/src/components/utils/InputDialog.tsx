import { useEffect, useRef, useState } from 'react'
import { create } from 'zustand'
import { useTranslation } from 'react-i18next'
import './utils.css'

/**
 * Port of Electron InputDialogWindow: a small modal with a single text input,
 * optional validation, debounced confirm-button refresh and Enter/Escape
 * handling. Returns the trimmed text, '' when empty is allowed, null on
 * cancel.
 */

export interface InputDialogOptions {
  title: string
  message?: string
  /** Initial text. */
  text?: string
  primaryButton?: string
  secondaryButton?: string
  allowEmpty?: boolean
  maxLength?: number
}

interface InputDialogRequest {
  id: number
  options: InputDialogOptions
}

let requestSeq = 0
let pendingResolve: ((value: string | null) => void) | null = null

interface InputDialogState {
  request: InputDialogRequest | null
  show: (options: InputDialogOptions) => void
  settle: (value: string | null) => void
}

const useInputDialogStore = create<InputDialogState>((set) => ({
  request: null,
  show: (options) => set({ request: { id: ++requestSeq, options } }),
  settle: (value) => {
    pendingResolve?.(value)
    pendingResolve = null
    set({ request: null })
  }
}))

export function openInputDialog(options: InputDialogOptions): Promise<string | null> {
  return new Promise((resolve) => {
    pendingResolve = resolve
    useInputDialogStore.getState().show(options)
  })
}

const DEBOUNCE_MS = 300

export default function InputDialogHost(): React.JSX.Element {
  const { t } = useTranslation()
  const request = useInputDialogStore((s) => s.request)
  const settle = useInputDialogStore((s) => s.settle)
  const [value, setValue] = useState('')
  const [confirmEnabled, setConfirmEnabled] = useState(false)
  const inputRef = useRef<HTMLInputElement>(null)
  const debounceRef = useRef<number | undefined>(undefined)

  useEffect(() => {
    if (!request) return
    const allowEmpty = request.options.allowEmpty === true
    setValue(request.options.text ?? '')
    setConfirmEnabled(allowEmpty || (request.options.text ?? '').trim().length > 0)
    const timer = window.setTimeout(() => {
      const el = inputRef.current
      if (el) {
        el.focus()
        el.setSelectionRange(el.value.length, el.value.length)
      }
    }, 0)
    return () => window.clearTimeout(timer)
  }, [request])

  useEffect(() => {
    return () => window.clearTimeout(debounceRef.current)
  }, [])

  if (!request) return <></>

  const { title, message, primaryButton, secondaryButton, allowEmpty, maxLength } = request.options

  const handleChange = (text: string): void => {
    setValue(text)
    window.clearTimeout(debounceRef.current)
    debounceRef.current = window.setTimeout(() => {
      setConfirmEnabled(allowEmpty === true || text.trim().length > 0)
    }, DEBOUNCE_MS)
  }

  const handleConfirm = (): void => {
    if (!confirmEnabled) return
    const trimmed = value.trim()
    settle(trimmed.length === 0 ? '' : trimmed)
  }

  const handleCancel = (): void => {
    settle(null)
  }

  return (
    <div className="udt-utils-backdrop" onClick={handleCancel}>
      <div
        className="udt-utils-modal"
        style={{ width: 420, maxWidth: 520 }}
        onClick={(event) => event.stopPropagation()}
      >
        <div className="udt-utils-modal__title">{title}</div>
        <div className="udt-utils-modal__body">
          <p className="udt-utils-text" style={{ marginTop: 0 }}>
            {message}
          </p>
          <input
            ref={inputRef}
            className="udt-utils-input"
            value={value}
            maxLength={maxLength}
            onChange={(event) => handleChange(event.target.value)}
            onKeyDown={(event) => {
              if (event.key === 'Enter') {
                event.preventDefault()
                handleConfirm()
              } else if (event.key === 'Escape') {
                event.preventDefault()
                handleCancel()
              }
            }}
          />
        </div>
        <div className="udt-utils-modal__actions">
          <button type="button" className="udt-utils-button" onClick={handleCancel}>
            {secondaryButton ?? t('common.cancel')}
          </button>
          <button
            type="button"
            className="udt-utils-button udt-utils-button--primary"
            disabled={!confirmEnabled}
            onClick={handleConfirm}
          >
            {primaryButton ?? t('common.ok')}
          </button>
        </div>
      </div>
    </div>
  )
}
