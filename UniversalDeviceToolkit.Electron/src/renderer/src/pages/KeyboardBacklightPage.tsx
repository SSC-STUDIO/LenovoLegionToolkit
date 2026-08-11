import { useEffect, useState } from 'react'
import {
  Button,
  Card,
  ColorPicker,
  Empty,
  Flex,
  List,
  Popconfirm,
  Radio,
  Result,
  Select,
  Slider,
  Space,
  Spin,
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

const RGB_PRESETS: RgbPreset[] = ['Off', 'One', 'Two', 'Three', 'Four']
const RGB_EFFECTS: RgbEffect[] = ['Static', 'Breath', 'Smooth', 'WaveRTL', 'WaveLTR']
const RGB_SPEEDS: RgbSpeed[] = ['Slowest', 'Slow', 'Fast', 'Fastest']
const RGB_BRIGHTNESS: RgbBrightness[] = ['Low', 'High']
const ZONES: ('Zone1' | 'Zone2' | 'Zone3' | 'Zone4')[] = ['Zone1', 'Zone2', 'Zone3', 'Zone4']
const SPECTRUM_PROFILES = [1, 2, 3, 4, 5, 6]

const DEFAULT_DESC: RgbPresetDescription = {
  Effect: 'Static',
  Speed: 'Slowest',
  Brightness: 'High',
  Zone1: { R: 255, G: 255, B: 255 },
  Zone2: { R: 255, G: 255, B: 255 },
  Zone3: { R: 255, G: 255, B: 255 },
  Zone4: { R: 255, G: 255, B: 255 }
}

const EMPTY_EFFECT: SpectrumEffect = {
  Type: 'Always',
  Speed: 'Speed1',
  Direction: 'None',
  ClockwiseDirection: 'None',
  Colors: [],
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

  const selectedPreset = rgbState?.SelectedPreset ?? 'Off'
  const desc = rgbState?.Presets[selectedPreset] ?? DEFAULT_DESC

  const fail = (): void => {
    message.error(t('common.error'))
  }

  const handlePreset = (preset: RgbPreset): void => {
    void setPreset(preset).then((ok) => {
      if (!ok) fail()
    })
  }

  const updateDesc = async (patch: Partial<RgbPresetDescription>): Promise<void> => {
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

  return (
    <Flex vertical gap={16}>
      <Card title={t('keyboard.rgb.preset')}>
        <Space wrap>
          {RGB_PRESETS.map((preset) => (
            <Button
              key={preset}
              type={selectedPreset === preset ? 'primary' : 'default'}
              onClick={() => handlePreset(preset)}
            >
              {t(`keyboard.rgb.presets.${PRESET_LABEL_KEYS[preset]}`)}
            </Button>
          ))}
        </Space>
      </Card>

      <Card title={t('keyboard.rgb.settings')}>
        <Flex vertical gap={16}>
          <Space wrap size={24}>
            <Space>
              <Typography.Text>{t('keyboard.rgb.effect')}</Typography.Text>
              <Select<RgbEffect>
                value={desc.Effect}
                options={RGB_EFFECTS.map((effect) => ({
                  value: effect,
                  label: t(`keyboard.rgb.effectOptions.${EFFECT_LABEL_KEYS[effect]}`)
                }))}
                onChange={(effect) => void updateDesc({ Effect: effect })}
                style={{ width: 160 }}
              />
            </Space>
            <Space>
              <Typography.Text>{t('keyboard.rgb.speed')}</Typography.Text>
              <Select<RgbSpeed>
                value={desc.Speed}
                options={RGB_SPEEDS.map((speed) => ({
                  value: speed,
                  label: t(`keyboard.rgb.speedOptions.${SPEED_LABEL_KEYS[speed]}`)
                }))}
                onChange={(speed) => void updateDesc({ Speed: speed })}
                style={{ width: 120 }}
              />
            </Space>
            <Space>
              <Typography.Text>{t('keyboard.rgb.brightness')}</Typography.Text>
              <Select<RgbBrightness>
                value={desc.Brightness}
                options={RGB_BRIGHTNESS.map((brightness) => ({
                  value: brightness,
                  label: t(`keyboard.rgb.brightnessOptions.${BRIGHTNESS_LABEL_KEYS[brightness]}`)
                }))}
                onChange={(brightness) => void updateDesc({ Brightness: brightness })}
                style={{ width: 100 }}
              />
            </Space>
          </Space>

          <Space size={24} wrap>
            <Typography.Text>{t('keyboard.rgb.zones')}</Typography.Text>
            {ZONES.map((zone) => (
              <ColorPicker
                key={zone}
                value={rgbToHex(desc[zone])}
                onChange={handleZoneChange(zone)}
                showText
              />
            ))}
          </Space>
        </Flex>
      </Card>
    </Flex>
  )
}

function SpectrumSection(): React.JSX.Element {
  const { t } = useTranslation()
  const { spectrum, setBrightness, setLogo, setProfile, loadProfileDesc, saveProfileDesc } =
    useKeyboardStore()
  const [brightnessDraft, setBrightnessDraft] = useState(spectrum.brightness)

  useEffect(() => {
    setBrightnessDraft(spectrum.brightness)
  }, [spectrum.brightness])

  const fail = (): void => {
    message.error(t('common.error'))
  }

  const handleProfile = (profile: number): void => {
    void setProfile(profile).then((ok) => {
      if (ok) void loadProfileDesc(profile)
      else fail()
    })
  }

  const handleAddEffect = (): void => {
    void saveProfileDesc(spectrum.profile, [...spectrum.effects, EMPTY_EFFECT]).then((ok) => {
      if (!ok) fail()
    })
  }

  const handleRemoveEffect = (index: number): void => {
    const effects = spectrum.effects.filter((_, i) => i !== index)
    void saveProfileDesc(spectrum.profile, effects).then((ok) => {
      if (!ok) fail()
    })
  }

  return (
    <Flex vertical gap={16}>
      <Card title={t('keyboard.spectrum.brightness')}>
        <Slider
          min={0}
          max={9}
          value={brightnessDraft}
          onChange={setBrightnessDraft}
          onChangeComplete={(value) => {
            void setBrightness(value).then((ok) => {
              if (!ok) fail()
            })
          }}
          style={{ width: 320 }}
        />
      </Card>

      <Card title={t('keyboard.spectrum.profile')}>
        <Radio.Group
          value={spectrum.profile}
          onChange={(e) => handleProfile(e.target.value as number)}
          options={SPECTRUM_PROFILES.map((profile) => ({ value: profile, label: `${profile}` }))}
        />
      </Card>

      <Card title={t('keyboard.spectrum.logo')}>
        <Switch
          checked={spectrum.logo}
          onChange={(checked) => {
            void setLogo(checked).then((ok) => {
              if (!ok) fail()
            })
          }}
        />
      </Card>

      <Card
        title={t('keyboard.spectrum.effects')}
        extra={
          <Button type="primary" onClick={handleAddEffect}>
            {t('keyboard.spectrum.addEffect')}
          </Button>
        }
      >
        {spectrum.effects.length === 0 ? (
          <Empty description={t('keyboard.spectrum.noEffects')} />
        ) : (
          <List
            dataSource={spectrum.effects}
            renderItem={(effect, index) => (
              <List.Item
                actions={[
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
                  <Tag>{t(`keyboard.spectrum.effectTypes.${EFFECT_TYPE_LABEL_KEYS[effect.Type]}`)}</Tag>
                  <Typography.Text type="secondary">
                    {t('keyboard.spectrum.colors')}: {effect.Colors.length}
                  </Typography.Text>
                </Space>
              </List.Item>
            )}
          />
        )}
      </Card>
    </Flex>
  )
}

export default function KeyboardBacklightPage(): React.JSX.Element {
  const { t } = useTranslation()
  const { mode, loading, error, load } = useKeyboardStore()

  useEffect(() => {
    void load()
  }, [load])

  if (loading || mode === null) {
    return (
      <div
        style={{
          display: 'flex',
          justifyContent: 'center',
          alignItems: 'center',
          minHeight: 320
        }}
      >
        <Spin size="large" />
      </div>
    )
  }

  if (error) {
    return <Result status="error" title={t('common.error')} subTitle={error} />
  }

  return (
    <div>
      <Typography.Title level={3} style={{ marginTop: 0 }}>
        {t('keyboard.title')}
      </Typography.Title>
      {mode === 'rgb' ? (
        <RgbSection />
      ) : mode === 'spectrum' ? (
        <SpectrumSection />
      ) : (
        <Empty description={t('keyboard.unsupported')} />
      )}
    </div>
  )
}
