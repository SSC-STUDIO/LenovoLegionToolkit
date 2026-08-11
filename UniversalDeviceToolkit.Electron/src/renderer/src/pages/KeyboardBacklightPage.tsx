import { useEffect, useRef, useState } from 'react'
import {
  BgColorsOutlined,
  BulbOutlined,
  ExportOutlined,
  ImportOutlined,
  KeyOutlined,
  PlusOutlined,
  RedoOutlined
} from '@ant-design/icons'
import {
  Button,
  ColorPicker,
  Dropdown,
  Empty,
  List,
  Popconfirm,
  Radio,
  Result,
  Select,
  Slider,
  Space,
  Switch,
  Tag,
  Typography,
  message
} from 'antd'
import type { Color } from 'antd/es/color-picker'
import { useTranslation } from 'react-i18next'
import type {
  RgbBrightness,
  RgbColor,
  RgbEffect,
  RgbPreset,
  RgbPresetDescription,
  RgbSpeed,
  RgbState,
  SpectrumEffect,
  SpectrumEffectType
} from '../api/keyboard'
import { useKeyboardStore } from '../stores/keyboardStore'
import { settingsApi } from '../api/settings'
import { softwareApi } from '../api/software'
import InfoBar from '../components/InfoBar'
import { normalizeKeyboardLayout } from '../components/keyboard/spectrum/keyboardLayouts'
import { normalizeSpectrumLayout } from '../components/keyboard/spectrum/deviceLayouts'
import SpectrumKeyboard from '../components/keyboard/spectrum/SpectrumKeyboard'
import SpectrumDevicePanel from '../components/keyboard/spectrum/SpectrumDevicePanel'
import SpectrumEffectModal from '../components/keyboard/spectrum/SpectrumEffectModal'
import '../components/keyboard/keyboard.css'

const RGB_PRESETS: RgbPreset[] = ['Off', 'One', 'Two', 'Three', 'Four']
const RGB_EFFECTS: RgbEffect[] = ['Static', 'Breath', 'Smooth', 'WaveRTL', 'WaveLTR']
const RGB_SPEEDS: RgbSpeed[] = ['Slowest', 'Slow', 'Fast', 'Fastest']
const RGB_BRIGHTNESS: RgbBrightness[] = ['Low', 'High']
const ZONES: ('Zone1' | 'Zone2' | 'Zone3' | 'Zone4')[] = ['Zone1', 'Zone2', 'Zone3', 'Zone4']
const SPECTRUM_PROFILES = [1, 2, 3, 4, 5, 6]

const BRIGHTNESS_MARKS: Record<number, string> = {
  0: '0',
  1: '1',
  2: '2',
  3: '3',
  4: '4',
  5: '5',
  6: '6',
  7: '7',
  8: '8',
  9: '9'
}

const DEFAULT_DESC: RgbPresetDescription = {
  Effect: 'Static',
  Speed: 'Slowest',
  Brightness: 'High',
  Zone1: { R: 255, G: 255, B: 255 },
  Zone2: { R: 255, G: 255, B: 255 },
  Zone3: { R: 255, G: 255, B: 255 },
  Zone4: { R: 255, G: 255, B: 255 }
}

const DEFAULT_EFFECT: SpectrumEffect = {
  Type: 'Always',
  Speed: 'Speed1',
  Direction: 'None',
  ClockwiseDirection: 'None',
  Colors: [{ R: 255, G: 255, B: 255 }],
  Keys: []
}

const PRESET_LABEL_KEYS: Record<RgbPreset, string> = {
  Off: 'off',
  One: 'one',
  Two: 'two',
  Three: 'three',
  Four: 'four'
}

const EFFECT_LABEL_KEYS: Record<RgbEffect, string> = {
  Static: 'static',
  Breath: 'breath',
  Smooth: 'smooth',
  WaveRTL: 'waveRtl',
  WaveLTR: 'waveLtr'
}

const SPEED_LABEL_KEYS: Record<RgbSpeed, string> = {
  Slowest: 'slowest',
  Slow: 'slow',
  Fast: 'fast',
  Fastest: 'fastest'
}

const BRIGHTNESS_LABEL_KEYS: Record<RgbBrightness, string> = {
  Low: 'low',
  High: 'high'
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

function rgbToHex(color: RgbColor): string {
  const toHex = (value: number): string => value.toString(16).padStart(2, '0')
  return `#${toHex(color.R)}${toHex(color.G)}${toHex(color.B)}`
}

function RgbSection(): React.JSX.Element {
  const { t } = useTranslation()
  const { rgbState, setRgb, setPreset } = useKeyboardStore()
  // WPF RGBKeyboardBacklightControl: while Lenovo Vantage is running the whole
  // section is disabled and a warning InfoBar is shown.
  const [vantageBlocked, setVantageBlocked] = useState(false)

  useEffect(() => {
    let cancelled = false
    softwareApi
      .getStatus('vantage')
      .then((result) => {
        if (!cancelled) setVantageBlocked(result.status === 'Enabled')
      })
      .catch(() => {
        if (!cancelled) setVantageBlocked(false)
      })
    return () => {
      cancelled = true
    }
  }, [])

  const selectedPreset = rgbState?.SelectedPreset ?? 'Off'
  const desc = rgbState?.Presets[selectedPreset] ?? DEFAULT_DESC

  const fail = (): void => {
    message.error(t('common.error'))
  }

  const handlePreset = (preset: RgbPreset): void => {
    if (vantageBlocked) return
    void setPreset(preset).then((ok) => {
      if (!ok) fail()
    })
  }

  const updateDesc = async (patch: Partial<RgbPresetDescription>): Promise<void> => {
    if (vantageBlocked) return
    if (!rgbState) return
    const nextDesc: RgbPresetDescription = { ...desc, ...patch }
    const next: RgbState = {
      ...rgbState,
      Presets: { ...rgbState.Presets, [selectedPreset]: nextDesc }
    }
    const ok = await setRgb(next)
    if (!ok) fail()
  }

  const handleZoneChange = (zone: 'Zone1' | 'Zone2' | 'Zone3' | 'Zone4') => (value: Color): void => {
    const rgb = value.toRgb()
    void updateDesc({ [zone]: { R: rgb.r, G: rgb.g, B: rgb.b } })
  }

  // WPF SynchroniseZonesMenuItem_Click: right-click a zone 鈫?all zones take its color.
  const handleSynchroniseZones = (zone: 'Zone1' | 'Zone2' | 'Zone3' | 'Zone4'): void => {
    const color = desc[zone]
    void updateDesc({ Zone1: color, Zone2: color, Zone3: color, Zone4: color })
  }

  const presetOff = selectedPreset === 'Off'
  const effectStatic = desc.Effect === 'Static'
  const speedEnabled = !presetOff && !effectStatic
  const zonesEnabled = !presetOff && (effectStatic || desc.Effect === 'Breath')

  const effectLabel = t(`keyboard.rgb.effectOptions.${EFFECT_LABEL_KEYS[desc.Effect]}`)
  const speedLabel = t(`keyboard.rgb.speedOptions.${SPEED_LABEL_KEYS[desc.Speed]}`)
  const brightnessLabel = t(`keyboard.rgb.brightnessOptions.${BRIGHTNESS_LABEL_KEYS[desc.Brightness]}`)

  return (
    <div className="udt-kb-rgb">
      {vantageBlocked && (
        <InfoBar
          severity="warning"
          title={t('keyboardvantageEnabledWarningtitle')}
          message={t('keyboardvantageEnabledWarningmessage')}
          className="udt-kb-vantage-warning"
        />
      )}
      <div className="udt-kb-presets">
        {RGB_PRESETS.map((preset) => (
          <Button
            key={preset}
            className={
              selectedPreset === preset ? 'udt-kb-preset udt-kb-preset--active' : 'udt-kb-preset'
            }
            disabled={vantageBlocked}
            onClick={() => handlePreset(preset)}
          >
            {t(`keyboard.rgb.presets.${PRESET_LABEL_KEYS[preset]}`)}
          </Button>
        ))}
      </div>

      <div
        className={`udt-kb-card udt-kb-combo-card${presetOff ? ' udt-kb-card--disabled' : ''}`}
      >
        <span className="udt-kb-card__icon"><KeyOutlined /></span>
        <div className="udt-kb-card__copy">
          <div className="udt-kb-card__title">{t('keyboard.rgb.brightness')}</div>
          <div className="udt-kb-card__subtitle">{brightnessLabel}</div>
        </div>
        <Select<RgbBrightness>
          className="udt-kb-combo-card__select"
          value={desc.Brightness}
          disabled={presetOff || vantageBlocked}
          options={RGB_BRIGHTNESS.map((brightness) => ({
            value: brightness,
            label: t(`keyboard.rgb.brightnessOptions.${BRIGHTNESS_LABEL_KEYS[brightness]}`)
          }))}
          onChange={(brightness) => void updateDesc({ Brightness: brightness })}
        />
      </div>

      <div
        className={`udt-kb-card udt-kb-combo-card${presetOff ? ' udt-kb-card--disabled' : ''}`}
      >
        <span className="udt-kb-card__icon"><KeyOutlined /></span>
        <div className="udt-kb-card__copy">
          <div className="udt-kb-card__title">{t('keyboard.rgb.effect')}</div>
          <div className="udt-kb-card__subtitle">{effectLabel}</div>
        </div>
        <Select<RgbEffect>
          className="udt-kb-combo-card__select"
          value={desc.Effect}
          disabled={presetOff || vantageBlocked}
          options={RGB_EFFECTS.map((effect) => ({
            value: effect,
            label: t(`keyboard.rgb.effectOptions.${EFFECT_LABEL_KEYS[effect]}`)
          }))}
          onChange={(effect) => void updateDesc({ Effect: effect })}
        />
      </div>

      <div
        className={`udt-kb-card udt-kb-combo-card${!speedEnabled ? ' udt-kb-card--disabled' : ''}`}
      >
        <span className="udt-kb-card__icon"><KeyOutlined /></span>
        <div className="udt-kb-card__copy">
          <div className="udt-kb-card__title">{t('keyboard.rgb.speed')}</div>
          <div className="udt-kb-card__subtitle">{speedLabel}</div>
        </div>
        <Select<RgbSpeed>
          className="udt-kb-combo-card__select"
          value={desc.Speed}
          disabled={!speedEnabled || vantageBlocked}
          options={RGB_SPEEDS.map((speed) => ({
            value: speed,
            label: t(`keyboard.rgb.speedOptions.${SPEED_LABEL_KEYS[speed]}`)
          }))}
          onChange={(speed) => void updateDesc({ Speed: speed })}
        />
      </div>

      <div className="udt-kb-zones">
        {ZONES.map((zone, index) => (
          <div
            key={zone}
            className={`udt-kb-card udt-kb-card--stack udt-kb-zone-card${!zonesEnabled ? ' udt-kb-card--disabled' : ''}`}
          >
            <div className="udt-kb-card__header">
              <span className="udt-kb-card__icon"><BgColorsOutlined /></span>
              <div className="udt-kb-card__copy">
                <div className="udt-kb-card__title">Zone {index + 1}</div>
              </div>
            </div>
            <div className="udt-kb-card__body">
              <Dropdown
                trigger={['contextMenu']}
                disabled={!zonesEnabled || vantageBlocked}
                menu={{
                  items: [
                    {
                      key: 'synchronise',
                      label: t('keyboard.rgb.synchroniseZones'),
                      onClick: () => handleSynchroniseZones(zone)
                    }
                  ]
                }}
              >
                <span className="udt-kb-zone-color-host">
                  <ColorPicker
                    value={rgbToHex(desc[zone])}
                    onChange={handleZoneChange(zone)}
                    disabled={!zonesEnabled || vantageBlocked}
                  />
                </span>
              </Dropdown>
              <span className="udt-kb-zone-card__hex">{rgbToHex(desc[zone])}</span>
            </div>
          </div>
        ))}
      </div>
    </div>
  )
}

function SpectrumSection(): React.JSX.Element {
  const { t } = useTranslation()
  const { spectrum, setBrightness, setLogo, setProfile, loadProfileDesc, saveProfileDesc } =
    useKeyboardStore()
  const importRef = useRef<HTMLInputElement | null>(null)
  const [selectedEffect, setSelectedEffect] = useState(-1)
  const [editingEffect, setEditingEffect] = useState<number | null>(null)
  const [layoutOverride, setLayoutOverride] = useState<string | null>(null)

  const fail = (): void => {
    message.error(t('common.error'))
  }

  const deviceKeys = spectrum.layout?.keys ?? []
  const effectiveLayout = layoutOverride ?? spectrum.layout?.keyboardLayout ?? 'Ansi'
  const layoutName = normalizeKeyboardLayout(effectiveLayout)
  // WPF SpectrumLayout enum values, normalized against backend casing variants.
  const spectrumLayout = normalizeSpectrumLayout(spectrum.layout?.spectrumLayout ?? 'KeyboardOnly')

  useEffect(() => {
    let cancelled = false
    settingsApi
      .get('spectrumKeyboard')
      .then((res) => {
        if (cancelled) return
        const value = (res.value ?? {}) as Record<string, unknown>
        const pref = value['KeyboardLayout'] ?? value['keyboardLayout']
        if (typeof pref === 'string' && pref !== '') setLayoutOverride(pref)
      })
      .catch(() => undefined)
    return () => {
      cancelled = true
    }
  }, [])

  const handleSwitchLayout = (): void => {
    const current = normalizeKeyboardLayout(effectiveLayout)
    const next = current === 'Ansi' ? 'Iso' : current === 'Iso' ? 'Jis' : 'Ansi'
    setLayoutOverride(next)
    settingsApi
      .get('spectrumKeyboard')
      .then((res) => {
        const value = ((res.value ?? {}) as Record<string, unknown>)
        return settingsApi
          .set('spectrumKeyboard', { ...value, KeyboardLayout: next })
          .then(() => settingsApi.save(['spectrumKeyboard']))
      })
      .catch(() => undefined)
  }

  const handleProfile = (profile: number): void => {
    void setProfile(profile).then((ok) => {
      if (ok) void loadProfileDesc(profile)
      else fail()
    })
  }

  const persistEffects = (effects: SpectrumEffect[]): void => {
    void saveProfileDesc(spectrum.profile, effects).then((ok) => {
      if (!ok) fail()
    })
  }

  const handleAddEffect = (): void => {
    setEditingEffect(spectrum.effects.length)
  }

  const handleApplyEffect = (effect: SpectrumEffect): void => {
    const isNew = editingEffect === null ? -1 : editingEffect
    if (isNew >= 0 && isNew >= spectrum.effects.length) {
      persistEffects([...spectrum.effects, effect])
    } else if (isNew >= 0) {
      const effects = spectrum.effects.map((e, i) => (i === isNew ? effect : e))
      persistEffects(effects)
    }
    setEditingEffect(null)
  }

  const handleRemoveEffect = (index: number): void => {
    const effects = spectrum.effects.filter((_, i) => i !== index)
    persistEffects(effects)
    if (selectedEffect === index) setSelectedEffect(-1)
  }

  const handleToggleKey = (code: number): void => {
    if (spectrum.effects.length === 0) {
      message.info(t('keyboard.spectrum.selectEffectHint'))
      return
    }
    const index = selectedEffect >= 0 && selectedEffect < spectrum.effects.length ? selectedEffect : 0
    const effect = spectrum.effects[index]
    const next = new Set(effect.Keys)
    if (next.has(code)) next.delete(code)
    else next.add(code)
    const effects = spectrum.effects.map((e, i) => (i === index ? { ...e, Keys: [...next] } : e))
    persistEffects(effects)
  }

  // WPF SelectableControl_Selected: box-selected zones are added to the
  // current effect without removing any already-selected ones (union).
  const handleBoxSelect = (codes: number[]): void => {
    if (spectrum.effects.length === 0) {
      message.info(t('keyboard.spectrum.selectEffectHint'))
      return
    }
    const index = selectedEffect >= 0 && selectedEffect < spectrum.effects.length ? selectedEffect : 0
    const effect = spectrum.effects[index]
    const next = new Set(effect.Keys)
    codes.forEach((code) => next.add(code))
    const effects = spectrum.effects.map((e, i) => (i === index ? { ...e, Keys: [...next] } : e))
    persistEffects(effects)
  }

  const handleSelectAll = (): void => {
    if (spectrum.effects.length === 0) return
    const index = selectedEffect >= 0 && selectedEffect < spectrum.effects.length ? selectedEffect : 0
    const effects = spectrum.effects.map((e, i) => (i === index ? { ...e, Keys: [...deviceKeys] } : e))
    persistEffects(effects)
  }

  const handleDeselectAll = (): void => {
    if (spectrum.effects.length === 0) return
    const index = selectedEffect >= 0 && selectedEffect < spectrum.effects.length ? selectedEffect : 0
    const effects = spectrum.effects.map((e, i) => (i === index ? { ...e, Keys: [] } : e))
    persistEffects(effects)
  }

  const selectedKeys = new Set(
    selectedEffect >= 0 && selectedEffect < spectrum.effects.length ? spectrum.effects[selectedEffect].Keys : []
  )

  const keyColors = new Map<number, string>()
  spectrum.effects.forEach((effect, index) => {
    if (index !== selectedEffect && selectedEffect >= 0) return
    // Multi-color effects preview their palette cyclically over their keys.
    effect.Keys.forEach((code, keyIndex) => {
      const color = effect.Colors[keyIndex % Math.max(1, effect.Colors.length)]
      if (color) keyColors.set(code, rgbToHex(color))
    })
  })

  const handleReset = (): void => {
    persistEffects([DEFAULT_EFFECT])
    setSelectedEffect(0)
  }

  const handleExport = (): void => {
    const blob = new Blob(
      [JSON.stringify({ profile: spectrum.profile, effects: spectrum.effects }, null, 2)],
      { type: 'application/json' }
    )
    const url = URL.createObjectURL(blob)
    const link = document.createElement('a')
    link.href = url
    link.download = `keyboard-profile-${spectrum.profile}.json`
    link.click()
    URL.revokeObjectURL(url)
  }

  const handleImportFile = async (file: File | null): Promise<void> => {
    if (!file) return
    try {
      const parsed: unknown = JSON.parse(await file.text())
      const effects = (parsed as { effects?: unknown }).effects
      if (!Array.isArray(effects)) throw new Error('invalid profile file')
      const ok = await saveProfileDesc(spectrum.profile, effects as SpectrumEffect[])
      if (!ok) fail()
    } catch {
      message.error(t('common.error'))
    }
  }

  return (
    <div className="udt-kb-spectrum">
      <div className="udt-kb-card udt-kb-card--stack udt-kb-spectrum-brightness-card">
        <div className="udt-kb-card__header">
          <span className="udt-kb-card__icon"><BulbOutlined /></span>
          <div className="udt-kb-card__copy">
            <div className="udt-kb-card__title">{t('keyboard.spectrum.brightness')}</div>
          </div>
        </div>
        <div className="udt-kb-card__body">
          <Slider
            key={spectrum.brightness}
            className="udt-kb-brightness-slider"
            min={0}
            max={9}
            step={1}
            marks={BRIGHTNESS_MARKS}
            defaultValue={spectrum.brightness}
            onChangeComplete={(value) => {
              void setBrightness(value).then((ok) => {
                if (!ok) fail()
              })
            }}
          />
        </div>
      </div>

      <Radio.Group
        className="udt-kb-spectrum-profiles"
        value={spectrum.profile}
        onChange={(e) => handleProfile(e.target.value as number)}
      >
        {SPECTRUM_PROFILES.map((profile) => (
          <Radio.Button key={profile} value={profile}>
            {profile}
          </Radio.Button>
        ))}
      </Radio.Group>

      <div className="udt-kb-card udt-kb-logo-card">
        <span className="udt-kb-card__icon"><BulbOutlined /></span>
        <div className="udt-kb-card__copy">
          <div className="udt-kb-card__title">{t('keyboard.spectrum.logo')}</div>
        </div>
        <Switch
          checked={spectrum.logo}
          onChange={(checked) => {
            void setLogo(checked).then((ok) => {
              if (!ok) fail()
            })
          }}
        />
      </div>

      <div className="udt-kb-spectrum-device">
        <div className="udt-kb-spectrum-device__toolbar">
          <Button
            className="udt-kb-icon-btn"
            size="small"
            title={t('keyboard.spectrum.selectAll')}
            disabled={spectrum.effects.length === 0}
            onClick={handleSelectAll}
          >
            {t('keyboard.spectrum.selectAll')}
          </Button>
          <Button
            className="udt-kb-icon-btn"
            size="small"
            title={t('keyboard.spectrum.deselectAll')}
            disabled={spectrum.effects.length === 0}
            onClick={handleDeselectAll}
          >
            {t('keyboard.spectrum.deselectAll')}
          </Button>
          <Button
            className="udt-kb-icon-btn"
            size="small"
            title={t('keyboard.spectrum.switchLayout')}
            onClick={handleSwitchLayout}
          >
            {t('keyboard.spectrum.switchLayout')} ({layoutName})
          </Button>
        </div>
        {deviceKeys.length === 0 ? (
          <div className="udt-kb-spectrum-device__empty">
            {t('keyboard.spectrum.noLayoutHint')}
          </div>
        ) : spectrumLayout === 'KeyboardOnly' ? (
          <SpectrumKeyboard
            layout={layoutName}
            deviceKeys={deviceKeys}
            selected={selectedKeys}
            onToggleKey={handleToggleKey}
            onBoxSelect={handleBoxSelect}
            keyColors={keyColors}
          />
        ) : (
          <SpectrumDevicePanel
            layout={spectrumLayout}
            keyboardLayout={layoutName}
            deviceKeys={deviceKeys}
            selected={selectedKeys}
            onToggleKey={handleToggleKey}
            onBoxSelect={handleBoxSelect}
            keyColors={keyColors}
          />
        )}
        {spectrumLayout !== 'KeyboardOnly' && (
          <div className="udt-kb-spectrum-device__hint">
            {t('keyboard.spectrum.frontPanelHint', {
              defaultValue: 'Click or drag to select keyboard and front panel zones'
            })}
          </div>
        )}
        {spectrum.effects.length > 0 && selectedEffect < 0 && (
          <div className="udt-kb-spectrum-device__hint">
            {t('keyboard.spectrum.selectEffectHint')}
          </div>
        )}
      </div>

      <div className="udt-kb-card udt-kb-card--stack udt-kb-effects-card">
        <div className="udt-kb-effects-card__toolbar">
          <h2 className="udt-kb-effects-card__title">{t('keyboard.spectrum.effects')}</h2>
          <Button className="udt-kb-icon-btn" icon={<RedoOutlined />} onClick={handleReset} />
          <Button className="udt-kb-icon-btn" icon={<ExportOutlined />} onClick={handleExport} />
          <Button
            className="udt-kb-icon-btn"
            icon={<ImportOutlined />}
            onClick={() => importRef.current?.click()}
          />
          <Button
            type="primary"
            className="udt-kb-add-effect"
            icon={<PlusOutlined />}
            onClick={handleAddEffect}
          >
            {t('keyboard.spectrum.addEffect')}
          </Button>
          <input
            ref={importRef}
            type="file"
            accept=".json,application/json"
            style={{ display: 'none' }}
            onChange={(e) => {
              void handleImportFile(e.target.files?.[0] ?? null)
              e.target.value = ''
            }}
          />
        </div>
        <div className="udt-kb-card__body">
          {spectrum.effects.length === 0 ? (
            <Empty description={t('keyboard.spectrum.noEffects')} />
          ) : (
            <List
              className="udt-kb-effects-list"
              dataSource={spectrum.effects}
              renderItem={(effect, index) => {
                const allKeys = effect.Keys.length === deviceKeys.length
                const subtitle = allKeys
                  ? t('keyboard.spectrum.allKeys')
                  : t('keyboard.spectrum.zonesCount', { count: effect.Keys.length })
                return (
                  <List.Item
                    className={`udt-kb-effect-row${index === selectedEffect ? ' udt-kb-effect-row--active' : ''}`}
                    onClick={() => setSelectedEffect(index)}
                    actions={[
                      <Button
                        key="edit"
                        size="small"
                        onClick={(e) => {
                          e.stopPropagation()
                          setEditingEffect(index)
                        }}
                      >
                        {t('keyboard.spectrum.editEffect')}
                      </Button>,
                      <Popconfirm
                        key="delete"
                        title={t('keyboard.spectrum.deleteEffect')}
                        onConfirm={() => handleRemoveEffect(index)}
                      >
                        <Button danger size="small">
                          {t('keyboard.spectrum.deleteEffect')}
                        </Button>
                      </Popconfirm>
                    ]}
                  >
                    <Space>
                      {effect.Colors.length > 0 && (
                        <span className="udt-kb-effect-row__swatch" aria-hidden="true">
                          {effect.Colors.slice(0, 3).map((color, colorIndex) => (
                            <i key={colorIndex} style={{ backgroundColor: rgbToHex(color) }} />
                          ))}
                        </span>
                      )}
                      <div className="udt-kb-effect-row__copy">
                        <div>
                          <Tag>{t(`keyboard.spectrum.effectTypes.${EFFECT_TYPE_LABEL_KEYS[effect.Type]}`)}</Tag>
                        </div>
                        <Typography.Text type="secondary">{subtitle}</Typography.Text>
                      </div>
                    </Space>
                  </List.Item>
                )
              }}
            />
          )}
        </div>
      </div>

      {editingEffect !== null && (
        <SpectrumEffectModal
          effect={
            editingEffect >= 0 && editingEffect < spectrum.effects.length
              ? spectrum.effects[editingEffect]
              : null
          }
          keyboardLayout={spectrum.layout?.keyboardLayout ?? 'Ansi'}
          deviceKeys={deviceKeys}
          previewEnabled={editingEffect !== null}
          onApply={handleApplyEffect}
          onCancel={() => setEditingEffect(null)}
        />
      )}
    </div>
  )
}

function LoadingSkeleton(): React.JSX.Element {
  return (
    <div className="udt-kb-loading" aria-busy="true">
      <div className="udt-kb-card udt-kb-loading__card">
        <div className="udt-skeleton" style={{ width: 220, height: 18 }} />
        <div className="udt-skeleton" style={{ width: 260, height: 8, marginTop: 28 }} />
        <div className="udt-skeleton" style={{ width: 300, height: 8, marginTop: 16 }} />
        <div className="udt-skeleton" style={{ width: 220, height: 32, marginTop: 24 }} />
      </div>
      <div className="udt-kb-card udt-kb-loading__row">
        <div className="udt-skeleton" style={{ width: 36, height: 36, borderRadius: 999 }} />
        <div className="udt-skeleton" style={{ width: 220, height: 16 }} />
      </div>
    </div>
  )
}

export default function KeyboardBacklightPage(): React.JSX.Element {
  const { t } = useTranslation()
  const { mode, loading, error, load } = useKeyboardStore()

  useEffect(() => {
    void load()
  }, [load])

  if (error) {
    return (
      <div className="udt-kb-page">
        <h1 className="udt-kb-page__title">{t('keyboard.title')}</h1>
        <Result status="error" title={t('common.error')} subTitle={error} />
      </div>
    )
  }

  if (loading || mode === null) {
    return (
      <div className="udt-kb-page">
        <h1 className="udt-kb-page__title">{t('keyboard.title')}</h1>
        <LoadingSkeleton />
      </div>
    )
  }

  return (
    <div className="udt-kb-page">
      <h1 className="udt-kb-page__title">{t('keyboard.title')}</h1>
      {mode === 'rgb' ? (
        <RgbSection />
      ) : mode === 'spectrum' ? (
        <SpectrumSection />
      ) : (
        <div className="udt-kb-unsupported">
          <KeyOutlined className="udt-kb-unsupported__icon" />
          <div className="udt-kb-unsupported__text">{t('keyboard.unsupported')}</div>
        </div>
      )}
    </div>
  )
}
