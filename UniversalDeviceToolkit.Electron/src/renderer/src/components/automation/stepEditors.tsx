import { FolderOpenOutlined } from '@ant-design/icons'
import { Button, Checkbox, Input, Select } from 'antd'
import type { TFunction } from 'i18next'
import { useTranslation } from 'react-i18next'
import type { AutomationStepType } from '../../api/automation'
import { automationApi } from '../../api/automation'
import i18n from '../../i18n'

export interface StepEditorProps {
  step: AutomationStepType
  onChange: (next: AutomationStepType) => void
}

interface StateOption {
  value: string
  labelKey: string
}

const ON_OFF_OPTIONS: StateOption[] = [
  { value: 'Off', labelKey: 'values.off' },
  { value: 'On', labelKey: 'values.on' }
]

const STATE_STEP_OPTIONS: Record<string, StateOption[]> = {
  rgbKeyboardBacklight: [
    { value: 'Off', labelKey: 'values.off' },
    { value: 'One', labelKey: 'values.presetOne' },
    { value: 'Two', labelKey: 'values.presetTwo' },
    { value: 'Three', labelKey: 'values.presetThree' },
    { value: 'Four', labelKey: 'values.presetFour' }
  ],
  speaker: [
    { value: 'Mute', labelKey: 'values.mute' },
    { value: 'Unmute', labelKey: 'values.unmute' }
  ],
  touchpadLock: ON_OFF_OPTIONS,
  whiteKeyboardBacklight: [
    { value: 'Off', labelKey: 'values.off' },
    { value: 'Low', labelKey: 'values.low' },
    { value: 'High', labelKey: 'values.high' }
  ],
  winKey: ON_OFF_OPTIONS
}

const INT_RANGE_STEPS: Record<string, { min: number; max: number }> = {
  spectrumKeyboardBacklightBrightness: { min: 0, max: 9 },
  spectrumKeyboardBacklightProfile: { min: 1, max: 6 }
}

const STEP_DEFAULTS: Record<string, Record<string, unknown>> = {
  rgbKeyboardBacklight: { state: 'Off' },
  run: { scriptPath: '', scriptArguments: '', runSilently: true, waitUntilFinished: false },
  speaker: { state: 'Mute' },
  spectrumKeyboardBacklightBrightness: { state: 0 },
  spectrumKeyboardBacklightImportProfile: { path: '' },
  spectrumKeyboardBacklightProfile: { state: 1 },
  touchpadLock: { state: 'Off' },
  whiteKeyboardBacklight: { state: 'Off' },
  winKey: { state: 'Off' }
}

const STATE_LABEL_KEYS: Record<string, string> = {
  Off: 'off',
  On: 'on',
  Mute: 'mute',
  Unmute: 'unmute',
  Low: 'low',
  High: 'high',
  One: 'presetOne',
  Two: 'presetTwo',
  Three: 'presetThree',
  Four: 'presetFour'
}

export function defaultStepFields(type: string): Record<string, unknown> {
  return { ...(STEP_DEFAULTS[type] ?? {}) }
}

function shortTypeName(type: string): string {
  return type.replace(/AutomationStep$/, '').replace(/AutomationPipelineTrigger$/, '')
}

export function stepTitle(t: TFunction, type: string): string {
  const key = `automation.stepLabels.${type}`
  return i18n.exists(key) ? t(key) : shortTypeName(type)
}

export function stepSummary(t: TFunction, step: AutomationStepType): string {
  switch (step.$type) {
    case 'run': {
      const path = step.scriptPath
      return typeof path === 'string' && path.length > 0 ? path : ''
    }
    case 'spectrumKeyboardBacklightImportProfile': {
      const path = step.path
      return typeof path === 'string' && path.length > 0 ? path : ''
    }
    case 'rgbKeyboardBacklight':
    case 'speaker':
    case 'touchpadLock':
    case 'whiteKeyboardBacklight':
    case 'winKey': {
      const state = step.state
      if (typeof state !== 'string') return ''
      const labelKey = STATE_LABEL_KEYS[state]
      return labelKey ? t(`automation.stepLabels.${labelKey}`) : state
    }
    case 'spectrumKeyboardBacklightBrightness': {
      const state = step.state
      if (typeof state !== 'number') return ''
      return state === 0 ? t('automation.stepLabels.values.off') : String(state)
    }
    case 'spectrumKeyboardBacklightProfile': {
      const state = step.state
      return typeof state === 'number' ? String(state) : ''
    }
    default:
      return ''
  }
}

function StateSelectEditor({
  step,
  onChange,
  options
}: StepEditorProps & { options: StateOption[] }): React.JSX.Element {
  const { t } = useTranslation()
  return (
    <Select
      size="small"
      className="udt-step-editor__select"
      value={typeof step.state === 'string' ? step.state : undefined}
      onChange={(value) => onChange({ ...step, state: value })}
      options={options.map((o) => ({ value: o.value, label: t(`automation.stepLabels.${o.labelKey}`) }))}
    />
  )
}

function IntRangeEditor({
  step,
  onChange,
  range
}: StepEditorProps & { range: { min: number; max: number } }): React.JSX.Element {
  const { t } = useTranslation()
  const options: { value: number; label: string }[] = []
  for (let value = range.min; value <= range.max; value++) {
    options.push({
      value,
      label: value === 0 ? t('automation.stepLabels.values.off') : String(value)
    })
  }
  const current = typeof step.state === 'number' ? step.state : range.min
  return (
    <Select
      size="small"
      className="udt-step-editor__select"
      value={current}
      onChange={(value) => onChange({ ...step, state: value })}
      options={options}
    />
  )
}

function RunStepEditor({ step, onChange }: StepEditorProps): React.JSX.Element {
  const { t } = useTranslation()
  const scriptPath = typeof step.scriptPath === 'string' ? step.scriptPath : ''
  const scriptArguments = typeof step.scriptArguments === 'string' ? step.scriptArguments : ''
  const runSilently = step.runSilently !== false
  const waitUntilFinished = step.waitUntilFinished === true
  return (
    <div className="udt-step-editor">
      <div className="udt-step-editor__row">
        <Input
          className="udt-step-editor__input"
          placeholder={t('automation.stepLabels.scriptPath')}
          value={scriptPath}
          onChange={(e) => onChange({ ...step, scriptPath: e.target.value })}
        />
        <Input
          className="udt-step-editor__input"
          placeholder={t('automation.stepLabels.scriptArguments')}
          value={scriptArguments}
          onChange={(e) => onChange({ ...step, scriptArguments: e.target.value })}
        />
      </div>
      <div className="udt-step-editor__row">
        <Checkbox
          checked={runSilently}
          title={t('automation.stepLabels.runSilentlyDesc')}
          onChange={(e) => onChange({ ...step, runSilently: e.target.checked })}
        >
          {t('automation.stepLabels.runSilently')}
        </Checkbox>
        <Checkbox
          checked={waitUntilFinished}
          title={t('automation.stepLabels.runWaitUntilFinishedDesc')}
          onChange={(e) => onChange({ ...step, waitUntilFinished: e.target.checked })}
        >
          {t('automation.stepLabels.runWaitUntilFinished')}
        </Checkbox>
      </div>
      <div className="udt-step-editor__hint">{t('automation.stepLabels.runHint')}</div>
    </div>
  )
}

function ImportProfileStepEditor({ step, onChange }: StepEditorProps): React.JSX.Element {
  const { t } = useTranslation()
  const path = typeof step.path === 'string' ? step.path : ''
  const handleBrowse = async (): Promise<void> => {
    try {
      const filePath = await automationApi.selectProfileJson()
      if (filePath) onChange({ ...step, path: filePath })
    } catch {
      // ignored
    }
  }
  return (
    <div className="udt-step-editor udt-step-editor--row">
      <Input
        className="udt-step-editor__input"
        placeholder={t('automation.stepLabels.importProfilePath')}
        value={path}
        onChange={(e) => onChange({ ...step, path: e.target.value })}
      />
      <Button size="small" icon={<FolderOpenOutlined />} onClick={() => void handleBrowse()}>
        {t('automation.stepLabels.browse')}
      </Button>
    </div>
  )
}

export function StepEditor({ step, onChange }: StepEditorProps): React.JSX.Element | null {
  const stateOptions = STATE_STEP_OPTIONS[step.$type]
  if (stateOptions) {
    return (
      <div className="udt-step-editor">
        <StateSelectEditor step={step} onChange={onChange} options={stateOptions} />
      </div>
    )
  }
  const range = INT_RANGE_STEPS[step.$type]
  if (range) {
    return (
      <div className="udt-step-editor">
        <IntRangeEditor step={step} onChange={onChange} range={range} />
      </div>
    )
  }
  if (step.$type === 'run') {
    return <RunStepEditor step={step} onChange={onChange} />
  }
  if (step.$type === 'spectrumKeyboardBacklightImportProfile') {
    return <ImportProfileStepEditor step={step} onChange={onChange} />
  }
  return null
}
