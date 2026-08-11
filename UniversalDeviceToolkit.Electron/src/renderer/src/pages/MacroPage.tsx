import { useEffect, useMemo, useState } from 'react'
import {
  ArrowDownOutlined,
  ArrowUpOutlined,
  ClockCircleOutlined,
  CloseOutlined,
  DragOutlined,
  EnterOutlined,
  RetweetOutlined,
  RotateRightOutlined,
  SettingOutlined
} from '@ant-design/icons'
import { Button, Select, Switch, message } from 'antd'
import { useTranslation } from 'react-i18next'
import type { MacroEvent, MacroRecordingMode, MacroSlot } from '../api/macro'
import { useMacroStore } from '../stores/macroStore'
import { useMacroRecorder } from '../hooks/useMacroRecorder'
import MacroRecordingModal from '../components/macro/MacroRecordingModal'
import { SkeletonList } from '../components/Skeleton'
import '../components/macro/macro.css'

interface MacroTexts {
  subtitle: string
  enableSubtitle: string
  ignoreDelays: string
  interruptOnOtherKey: string
  recordingOptions: string
  dontRepeat: string
  keyboardOnly: string
  keyboardMouse: string
  allInputs: string
  record: string
  recording: string
  recordingInterrupted: string
  keyboard: string
  mouse: string
  move: string
  wheelUp: string
  wheelDown: string
  wheelLeft: string
  wheelRight: string
  leftButton: string
  rightButton: string
  middleButton: string
  xButton: string
  button: string
}

const NUMPAD_LAYOUT: (number | null)[][] = [
  [0x67, 0x68, 0x69],
  [0x64, 0x65, 0x66],
  [0x61, 0x62, 0x63],
  [null, 0x60, null]
]

const REPEAT_OPTIONS = Array.from({ length: 10 }, (_, i) => ({ value: i + 1, label: `${i + 1}` }))

const RECORDING_OPTIONS: {
  value: MacroRecordingMode
  labelKey: 'keyboardOnly' | 'keyboardMouse' | 'allInputs'
}[] = [
  { value: 'Keyboard', labelKey: 'keyboardOnly' },
  { value: 'KeyboardMouse', labelKey: 'keyboardMouse' },
  { value: 'KeyboardMouseMovement', labelKey: 'allInputs' }
]

const VK_NAMES: Record<number, string> = {
  0x08: 'Backspace',
  0x09: 'Tab',
  0x0d: 'Enter',
  0x10: 'Shift',
  0x11: 'Ctrl',
  0x12: 'Alt',
  0x13: 'Pause',
  0x14: 'CapsLock',
  0x1b: 'Esc',
  0x20: 'Space',
  0x21: 'PageUp',
  0x22: 'PageDown',
  0x23: 'End',
  0x24: 'Home',
  0x25: 'Left',
  0x26: 'Up',
  0x27: 'Right',
  0x28: 'Down',
  0x2d: 'Insert',
  0x2e: 'Delete',
  0x5b: 'LWin',
  0x5c: 'RWin',
  0x5d: 'Menu',
  0x90: 'NumLock',
  0x91: 'ScrollLock',
  0xba: ';',
  0xbb: '=',
  0xbc: ',',
  0xbd: '-',
  0xbe: '.',
  0xbf: '/',
  0xc0: '`',
  0xdb: '[',
  0xdc: '\\',
  0xdd: ']',
  0xde: "'"
}

function vkKeyName(code: number): string {
  const named = VK_NAMES[code]
  if (named) return named
  if (code >= 0x41 && code <= 0x5a) return String.fromCharCode(code)
  if (code >= 0x30 && code <= 0x39) return String.fromCharCode(code)
  if (code >= 0x60 && code <= 0x69) return `NumPad ${code - 0x60}`
  if (code >= 0x70 && code <= 0x7b) return `F${code - 0x6f}`
  return `Key ${code}`
}

function formatDelay(delayMs: number): string {
  if (delayMs < 1000) return `${Math.round(delayMs)} ms`
  return `${(delayMs / 1000).toFixed(1)} s`
}

function eventTitle(ev: MacroEvent, tx: MacroTexts): string {
  if (ev.source === 'Keyboard') return vkKeyName(ev.key)
  if (ev.direction === 'Move') return tx.move
  if (ev.direction === 'Wheel') return ev.key < 0 ? tx.wheelDown : tx.wheelUp
  if (ev.direction === 'HorizontalWheel') return ev.key < 0 ? tx.wheelLeft : tx.wheelRight
  if (ev.key >= 0xff) return `${tx.xButton} ${ev.key >> 16}`
  if (ev.key === 1) return tx.leftButton
  if (ev.key === 2) return tx.rightButton
  if (ev.key === 3) return tx.middleButton
  return `${tx.button} ${ev.key}`
}

function eventSubtitle(ev: MacroEvent, tx: MacroTexts): string {
  const source = ev.source === 'Keyboard' ? tx.keyboard : tx.mouse
  return `${source} → ${formatDelay(ev.delayMs)}`
}

function eventIcon(ev: MacroEvent): React.JSX.Element | null {
  switch (ev.direction) {
    case 'Up':
      return <ArrowUpOutlined />
    case 'Down':
      return <ArrowDownOutlined />
    case 'Wheel':
    case 'HorizontalWheel':
      return <RotateRightOutlined />
    case 'Move':
      return <DragOutlined />
    default:
      return null
  }
}

export default function MacroPage(): React.JSX.Element {
  const { t } = useTranslation()
  const { state, loading: macroLoading, load, setEnabled, play, saveSequence, clearSequence } = useMacroStore()
  const tx = useMemo<MacroTexts>(
    () => ({
      subtitle: t('macro.subtitle'),
      enableSubtitle: t('macro.enableDesc'),
      ignoreDelays: t('macro.ignoreDelays'),
      interruptOnOtherKey: t('macro.interruptOnOtherKey'),
      recordingOptions: t('macro.recordingOptions'),
      dontRepeat: t('macro.dontRepeat'),
      keyboardOnly: t('macro.keyboardOnly'),
      keyboardMouse: t('macro.keyboardMouse'),
      allInputs: t('macro.allInputs'),
      record: t('macro.record'),
      recording: t('macro.recording.title'),
      recordingInterrupted: t('macro.recordingInterrupted'),
      keyboard: t('macro.keyboard'),
      mouse: t('macro.mouse'),
      move: t('macro.move'),
      wheelUp: t('macro.wheelUp'),
      wheelDown: t('macro.wheelDown'),
      wheelLeft: t('macro.wheelLeft'),
      wheelRight: t('macro.wheelRight'),
      leftButton: t('macro.leftButton'),
      rightButton: t('macro.rightButton'),
      middleButton: t('macro.middleButton'),
      xButton: t('macro.xButton'),
      button: t('macro.button')
    }),
    [t]
  )

  const [selectedKey, setSelectedKey] = useState<number>(0x60)
  const [repeatCount, setRepeatCount] = useState(1)
  const [ignoreDelays, setIgnoreDelays] = useState(false)
  const [interruptOnOtherKey, setInterruptOnOtherKey] = useState(false)
  const [recordingMode, setRecordingMode] = useState<MacroRecordingMode>('Keyboard')
  const [events, setEvents] = useState<MacroEvent[]>([])

  const recorder = useMacroRecorder((captured, interrupted) => {
    if (interrupted) {
      message.info(tx.recordingInterrupted)
      return
    }
    if (captured.length > 0) setEvents((prev) => [...prev, ...captured])
  })
  const recording = recorder.state !== 'idle'

  const slot = (state?.slots ?? []).find((s) => s.key === selectedKey)
  const savedEvents = (slot?.events as MacroEvent[]) ?? []
  const list = events.length > 0 ? events : savedEvents
  const hasEvents = list.length > 0

  // Sync the editable sequence settings whenever the selected slot changes.
  const [syncedSlot, setSyncedSlot] = useState<MacroSlot | undefined>(undefined)
  if (syncedSlot !== slot) {
    setSyncedSlot(slot)
    setRepeatCount(slot?.repeatCount ?? 1)
    setIgnoreDelays(slot?.ignoreDelays ?? false)
    setInterruptOnOtherKey(slot?.interruptOnOtherKey ?? false)
  }

  useEffect(() => {
    void load()
  }, [load])

  const selectKey = (code: number): void => {
    if (recorder.state !== 'idle') recorder.stop()
    setSelectedKey(code)
  }

  const handleSave = async (): Promise<void> => {
    try {
      await saveSequence({
        key: selectedKey,
        repeatCount,
        ignoreDelays,
        interruptOnOtherKey,
        events
      })
      setEvents([])
      void load()
      message.success(t('settings.saved'))
    } catch (error) {
      message.error((error as Error).message)
    }
  }

  const handleClear = async (): Promise<void> => {
    try {
      await clearSequence(selectedKey)
      setEvents([])
    } catch (error) {
      message.error((error as Error).message)
    }
  }

  const handleRecord = (): void => {
    if (recorder.state !== 'idle') {
      recorder.stop()
      return
    }
    recorder.start(recordingMode)
  }

  const showSkeleton = macroLoading && state?.slots == null

  return (
    <div className="udt-macro-page">
      <h1 className="udt-macro-page__title">{t('macro.title')}</h1>
      <p className="udt-macro-page__subtitle">{tx.subtitle}</p>

      {showSkeleton ? (
        <SkeletonList rows={3} />
      ) : (
        <>
      <div className="udt-macro-card udt-macro-card--enable">
        <div className="udt-macro-card__body udt-macro-card__body--row">
          <div className="udt-macro-card__copy">
            <div className="udt-macro-card__title">{t('macro.enable')}</div>
            <div className="udt-macro-card__subtitle">{tx.enableSubtitle}</div>
          </div>
          <span className="udt-macro-switch">
            <Switch
              checked={state?.isEnabled ?? false}
              onChange={(checked) => void setEnabled(checked)}
            />
          </span>
        </div>
      </div>

      <div className="udt-macro-main">
        <div className="udt-macro-numpad">
          {NUMPAD_LAYOUT.flat().map((code, index) =>
            code === null ? (
              <span key={`placeholder-${index}`} />
            ) : (
              <button
                key={code}
                type="button"
                className={`udt-macro-numpad__key${
                  selectedKey === code ? ' udt-macro-numpad__key--selected' : ''
                }`}
                onClick={() => selectKey(code)}
              >
                {code - 0x60}
              </button>
            )
          )}
        </div>

        <div className="udt-macro-sequence">
          <div
            className={`udt-macro-card udt-macro-card--gap${
              hasEvents ? '' : ' udt-macro-card--disabled'
            }`}
          >
            <div className="udt-macro-card__header">
              <span className="udt-macro-card__icon">
                <RetweetOutlined />
              </span>
              <div className="udt-macro-card__copy">
                <div className="udt-macro-card__title">{t('macro.repeat')}</div>
              </div>
            </div>
            <div className="udt-macro-card__body">
              <Select
                className="udt-macro-select"
                value={repeatCount}
                disabled={!hasEvents}
                options={REPEAT_OPTIONS.map((o) => ({
                  ...o,
                  label: o.value === 1 ? tx.dontRepeat : o.label
                }))}
                onChange={setRepeatCount}
              />
            </div>
          </div>

          <div className="udt-macro-row">
            <div
              className={`udt-macro-card udt-macro-card--gap-lg${
                hasEvents ? '' : ' udt-macro-card--disabled'
              }`}
            >
              <div className="udt-macro-card__header">
                <span className="udt-macro-card__icon">
                  <ClockCircleOutlined />
                </span>
              </div>
              <div className="udt-macro-card__body udt-macro-card__body--row">
                <div className="udt-macro-card__title">{tx.ignoreDelays}</div>
                <span className="udt-macro-switch">
                  <Switch checked={ignoreDelays} disabled={!hasEvents} onChange={setIgnoreDelays} />
                </span>
              </div>
            </div>
            <div
              className={`udt-macro-card udt-macro-card--gap-lg${
                hasEvents ? '' : ' udt-macro-card--disabled'
              }`}
            >
              <div className="udt-macro-card__header">
                <span className="udt-macro-card__icon">
                  <EnterOutlined />
                </span>
              </div>
              <div className="udt-macro-card__body udt-macro-card__body--row">
                <div className="udt-macro-card__title">{tx.interruptOnOtherKey}</div>
                <span className="udt-macro-switch">
                  <Switch
                    checked={interruptOnOtherKey}
                    disabled={!hasEvents}
                    onChange={setInterruptOnOtherKey}
                  />
                </span>
              </div>
            </div>
          </div>

          <div className="udt-macro-card udt-macro-card--gap-lg">
            <div className="udt-macro-card__header">
              <span className="udt-macro-card__icon">
                <SettingOutlined />
              </span>
              <div className="udt-macro-card__copy">
                <div className="udt-macro-card__title">{tx.recordingOptions}</div>
              </div>
            </div>
            <div className="udt-macro-card__body">
              <Select
                className="udt-macro-select"
                value={recordingMode}
                options={RECORDING_OPTIONS.map((o) => ({ value: o.value, label: tx[o.labelKey] }))}
                onChange={setRecordingMode}
              />
            </div>
          </div>

          <div className="udt-macro-actions">
            <Button onClick={() => void handleSave()}>{t('macro.save')}</Button>
            <Button onClick={() => void play(selectedKey)}>{t('macro.play')}</Button>
            <Button
              icon={<CloseOutlined />}
              title={t('macro.clear')}
              onClick={() => void handleClear()}
            />
            <Button
              type="primary"
              className="udt-macro-actions__record"
              onClick={handleRecord}
            >
              {recording ? tx.recording : tx.record}
            </Button>
          </div>

          <div className="udt-macro-events">
            {hasEvents ? (
              list.map((ev, index) => (
                <div key={index} className="udt-macro-event">
                  <span className="udt-macro-event__icon">{eventIcon(ev)}</span>
                  <div className="udt-macro-card__copy">
                    <div className="udt-macro-event__title">{eventTitle(ev, tx)}</div>
                    <div className="udt-macro-event__subtitle">{eventSubtitle(ev, tx)}</div>
                  </div>
                </div>
              ))
            ) : (
              <div className="udt-macro-events__empty">{t('macro.empty')}</div>
            )}
          </div>
        </div>
      </div>

      {recorder.state !== 'idle' && (
        <MacroRecordingModal preparing={recorder.state === 'preparing'} />
      )}
        </>
      )}
    </div>
  )
}
