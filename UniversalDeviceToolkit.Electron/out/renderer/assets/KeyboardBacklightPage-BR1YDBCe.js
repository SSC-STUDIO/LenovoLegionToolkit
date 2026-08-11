import { i as invoke, c as create, u as useTranslation, r as reactExports, j as jsxRuntimeExports, S as Spin, T as Typography, F as Flex, C as Card, s as staticMethods } from "./index-3RTipSd5.js";
import { R as Result } from "./index-wvXrY1Ff.js";
import { E as Empty, S as Select } from "./index-BxBscas6.js";
import { S as Space } from "./index-Dro2pb1j.js";
import { B as Button, T as Tag } from "./index-uyL__3sF.js";
import { C as ColorPicker, S as Slider, R as Radio } from "./ColorPicker-CDJwnKVz.js";
import { S as Switch } from "./index-BbS3n2P6.js";
import { L as List } from "./index-DaSpOuam.js";
import { P as Popconfirm } from "./index-DdhF4o9H.js";
import "./Addon-CECo-qGW.js";
import "./index-QUbxwEY1.js";
import "./index-Hdt_DTHG.js";
import "./Input-mSSMIOSE.js";
const keyboardApi = {
  async detect() {
    return invoke("keyboard.detect", {});
  },
  async getRgbState() {
    return invoke("rgb.getState", {});
  },
  async setRgbState(state) {
    return invoke("rgb.setState", { state });
  },
  async setPreset(preset) {
    return invoke("rgb.setPreset", { preset });
  },
  async nextPreset() {
    return invoke("rgb.nextPreset", {});
  },
  async takeOwnership(enable, restorePreset) {
    return invoke(
      "rgb.takeOwnership",
      restorePreset === void 0 ? { enable } : { enable, restorePreset }
    );
  },
  async spectrumGetLayout() {
    return invoke("spectrum.getLayout", {});
  },
  async spectrumGetBrightness() {
    return invoke("spectrum.getBrightness", {});
  },
  async spectrumSetBrightness(brightness) {
    return invoke("spectrum.setBrightness", { brightness });
  },
  async spectrumGetLogo() {
    return invoke("spectrum.getLogoStatus", {});
  },
  async spectrumSetLogo(isOn) {
    return invoke("spectrum.setLogoStatus", { isOn });
  },
  async spectrumGetProfile() {
    return invoke("spectrum.getProfile", {});
  },
  async spectrumSetProfile(profile) {
    return invoke("spectrum.setProfile", { profile });
  },
  async spectrumGetProfileDesc(profile) {
    return invoke("spectrum.getProfileDescription", { profile });
  },
  async spectrumSetProfileDesc(profile, effects) {
    return invoke("spectrum.setProfileDescription", { profile, effects });
  }
};
const EMPTY_SPECTRUM = {
  layout: null,
  brightness: 0,
  logo: false,
  profile: 1,
  effects: []
};
const useKeyboardStore = create()((set, get) => ({
  mode: null,
  rgbState: null,
  spectrum: EMPTY_SPECTRUM,
  loading: false,
  error: null,
  async load() {
    if (get().loading) return;
    set({ loading: true, error: null });
    try {
      const { mode } = await keyboardApi.detect();
      if (mode === "rgb") {
        const { state } = await keyboardApi.getRgbState();
        set({ mode, rgbState: state });
      } else if (mode === "spectrum") {
        const [layout, brightness, logo, profile] = await Promise.all([
          keyboardApi.spectrumGetLayout(),
          keyboardApi.spectrumGetBrightness(),
          keyboardApi.spectrumGetLogo(),
          keyboardApi.spectrumGetProfile()
        ]);
        let effects = [];
        try {
          const desc = await keyboardApi.spectrumGetProfileDesc(profile.profile);
          effects = desc.effects;
        } catch {
        }
        set({
          mode,
          spectrum: {
            layout,
            brightness: brightness.brightness,
            logo: logo.isOn,
            profile: profile.profile,
            effects
          }
        });
      } else {
        set({ mode: "none" });
      }
    } catch (error) {
      set({ error: error.message });
    } finally {
      set({ loading: false });
    }
  },
  async setRgb(state) {
    try {
      await keyboardApi.setRgbState(state);
      set({ rgbState: state });
      return true;
    } catch (error) {
      set({ error: error.message });
      return false;
    }
  },
  async setPreset(preset) {
    try {
      const { state } = await keyboardApi.setPreset(preset);
      set({ rgbState: state });
      return true;
    } catch (error) {
      set({ error: error.message });
      return false;
    }
  },
  async nextPreset() {
    try {
      const { state } = await keyboardApi.nextPreset();
      set({ rgbState: state });
      return true;
    } catch (error) {
      set({ error: error.message });
      return false;
    }
  },
  async setBrightness(value) {
    try {
      await keyboardApi.spectrumSetBrightness(value);
      set({ spectrum: { ...get().spectrum, brightness: value } });
      return true;
    } catch (error) {
      set({ error: error.message });
      return false;
    }
  },
  async setLogo(value) {
    try {
      await keyboardApi.spectrumSetLogo(value);
      set({ spectrum: { ...get().spectrum, logo: value } });
      return true;
    } catch (error) {
      set({ error: error.message });
      return false;
    }
  },
  async setProfile(profile) {
    try {
      await keyboardApi.spectrumSetProfile(profile);
      set({ spectrum: { ...get().spectrum, profile } });
      return true;
    } catch (error) {
      set({ error: error.message });
      return false;
    }
  },
  async loadProfileDesc(profile) {
    try {
      const desc = await keyboardApi.spectrumGetProfileDesc(profile);
      set({ spectrum: { ...get().spectrum, profile: desc.profile, effects: desc.effects } });
      return true;
    } catch (error) {
      set({ error: error.message });
      return false;
    }
  },
  async saveProfileDesc(profile, effects) {
    try {
      await keyboardApi.spectrumSetProfileDesc(profile, effects);
      set({ spectrum: { ...get().spectrum, profile, effects } });
      return true;
    } catch (error) {
      set({ error: error.message });
      return false;
    }
  }
}));
const RGB_PRESETS = ["Off", "One", "Two", "Three", "Four"];
const RGB_EFFECTS = ["Static", "Breath", "Smooth", "WaveRTL", "WaveLTR"];
const RGB_SPEEDS = ["Slowest", "Slow", "Fast", "Fastest"];
const RGB_BRIGHTNESS = ["Low", "High"];
const ZONES = ["Zone1", "Zone2", "Zone3", "Zone4"];
const SPECTRUM_PROFILES = [1, 2, 3, 4, 5, 6];
const DEFAULT_DESC = {
  Effect: "Static",
  Speed: "Slowest",
  Brightness: "High",
  Zone1: { R: 255, G: 255, B: 255 },
  Zone2: { R: 255, G: 255, B: 255 },
  Zone3: { R: 255, G: 255, B: 255 },
  Zone4: { R: 255, G: 255, B: 255 }
};
const EMPTY_EFFECT = {
  Type: "Always",
  Speed: "Speed1",
  Direction: "None",
  ClockwiseDirection: "None",
  Colors: [],
  Keys: []
};
const PRESET_LABEL_KEYS = {
  Off: "off",
  One: "one",
  Two: "two",
  Three: "three",
  Four: "four"
};
const EFFECT_LABEL_KEYS = {
  Static: "static",
  Breath: "breath",
  Smooth: "smooth",
  WaveRTL: "waveRtl",
  WaveLTR: "waveLtr"
};
const SPEED_LABEL_KEYS = {
  Slowest: "slowest",
  Slow: "slow",
  Fast: "fast",
  Fastest: "fastest"
};
const BRIGHTNESS_LABEL_KEYS = {
  Low: "low",
  High: "high"
};
const EFFECT_TYPE_LABEL_KEYS = {
  Always: "always",
  RainbowScrew: "rainbowScrew",
  RainbowWave: "rainbowWave",
  ColorChange: "colorChange",
  ColorWave: "colorWave",
  ColorPulse: "colorPulse",
  Smooth: "smooth",
  Rain: "rain",
  Ripple: "ripple",
  Type: "type",
  AudioBounce: "audioBounce",
  AudioRipple: "audioRipple",
  AuroraSync: "auroraSync"
};
function rgbToHex(color) {
  const toHex = (value) => value.toString(16).padStart(2, "0");
  return `#${toHex(color.R)}${toHex(color.G)}${toHex(color.B)}`;
}
function RgbSection() {
  const { t } = useTranslation();
  const { rgbState, setRgb, setPreset } = useKeyboardStore();
  const selectedPreset = rgbState?.SelectedPreset ?? "Off";
  const desc = rgbState?.Presets[selectedPreset] ?? DEFAULT_DESC;
  const fail = () => {
    staticMethods.error(t("common.error"));
  };
  const handlePreset = (preset) => {
    void setPreset(preset).then((ok) => {
      if (!ok) fail();
    });
  };
  const updateDesc = async (patch) => {
    if (!rgbState) return;
    const nextDesc = { ...desc, ...patch };
    const next = {
      ...rgbState,
      Presets: { ...rgbState.Presets, [selectedPreset]: nextDesc }
    };
    const ok = await setRgb(next);
    if (!ok) fail();
  };
  const handleZoneChange = (zone) => (value) => {
    const rgb = value.toRgb();
    void updateDesc({ [zone]: { R: rgb.r, G: rgb.g, B: rgb.b } });
  };
  return /* @__PURE__ */ jsxRuntimeExports.jsxs(Flex, { vertical: true, gap: 16, children: [
    /* @__PURE__ */ jsxRuntimeExports.jsx(Card, { title: t("keyboard.rgb.preset"), children: /* @__PURE__ */ jsxRuntimeExports.jsx(Space, { wrap: true, children: RGB_PRESETS.map((preset) => /* @__PURE__ */ jsxRuntimeExports.jsx(
      Button,
      {
        type: selectedPreset === preset ? "primary" : "default",
        onClick: () => handlePreset(preset),
        children: t(`keyboard.rgb.presets.${PRESET_LABEL_KEYS[preset]}`)
      },
      preset
    )) }) }),
    /* @__PURE__ */ jsxRuntimeExports.jsx(Card, { title: t("keyboard.rgb.settings"), children: /* @__PURE__ */ jsxRuntimeExports.jsxs(Flex, { vertical: true, gap: 16, children: [
      /* @__PURE__ */ jsxRuntimeExports.jsxs(Space, { wrap: true, size: 24, children: [
        /* @__PURE__ */ jsxRuntimeExports.jsxs(Space, { children: [
          /* @__PURE__ */ jsxRuntimeExports.jsx(Typography.Text, { children: t("keyboard.rgb.effect") }),
          /* @__PURE__ */ jsxRuntimeExports.jsx(
            Select,
            {
              value: desc.Effect,
              options: RGB_EFFECTS.map((effect) => ({
                value: effect,
                label: t(`keyboard.rgb.effectOptions.${EFFECT_LABEL_KEYS[effect]}`)
              })),
              onChange: (effect) => void updateDesc({ Effect: effect }),
              style: { width: 160 }
            }
          )
        ] }),
        /* @__PURE__ */ jsxRuntimeExports.jsxs(Space, { children: [
          /* @__PURE__ */ jsxRuntimeExports.jsx(Typography.Text, { children: t("keyboard.rgb.speed") }),
          /* @__PURE__ */ jsxRuntimeExports.jsx(
            Select,
            {
              value: desc.Speed,
              options: RGB_SPEEDS.map((speed) => ({
                value: speed,
                label: t(`keyboard.rgb.speedOptions.${SPEED_LABEL_KEYS[speed]}`)
              })),
              onChange: (speed) => void updateDesc({ Speed: speed }),
              style: { width: 120 }
            }
          )
        ] }),
        /* @__PURE__ */ jsxRuntimeExports.jsxs(Space, { children: [
          /* @__PURE__ */ jsxRuntimeExports.jsx(Typography.Text, { children: t("keyboard.rgb.brightness") }),
          /* @__PURE__ */ jsxRuntimeExports.jsx(
            Select,
            {
              value: desc.Brightness,
              options: RGB_BRIGHTNESS.map((brightness) => ({
                value: brightness,
                label: t(`keyboard.rgb.brightnessOptions.${BRIGHTNESS_LABEL_KEYS[brightness]}`)
              })),
              onChange: (brightness) => void updateDesc({ Brightness: brightness }),
              style: { width: 100 }
            }
          )
        ] })
      ] }),
      /* @__PURE__ */ jsxRuntimeExports.jsxs(Space, { size: 24, wrap: true, children: [
        /* @__PURE__ */ jsxRuntimeExports.jsx(Typography.Text, { children: t("keyboard.rgb.zones") }),
        ZONES.map((zone) => /* @__PURE__ */ jsxRuntimeExports.jsx(
          ColorPicker,
          {
            value: rgbToHex(desc[zone]),
            onChange: handleZoneChange(zone),
            showText: true
          },
          zone
        ))
      ] })
    ] }) })
  ] });
}
function SpectrumSection() {
  const { t } = useTranslation();
  const { spectrum, setBrightness, setLogo, setProfile, loadProfileDesc, saveProfileDesc } = useKeyboardStore();
  const [brightnessDraft, setBrightnessDraft] = reactExports.useState(spectrum.brightness);
  reactExports.useEffect(() => {
    setBrightnessDraft(spectrum.brightness);
  }, [spectrum.brightness]);
  const fail = () => {
    staticMethods.error(t("common.error"));
  };
  const handleProfile = (profile) => {
    void setProfile(profile).then((ok) => {
      if (ok) void loadProfileDesc(profile);
      else fail();
    });
  };
  const handleAddEffect = () => {
    void saveProfileDesc(spectrum.profile, [...spectrum.effects, EMPTY_EFFECT]).then((ok) => {
      if (!ok) fail();
    });
  };
  const handleRemoveEffect = (index) => {
    const effects = spectrum.effects.filter((_, i) => i !== index);
    void saveProfileDesc(spectrum.profile, effects).then((ok) => {
      if (!ok) fail();
    });
  };
  return /* @__PURE__ */ jsxRuntimeExports.jsxs(Flex, { vertical: true, gap: 16, children: [
    /* @__PURE__ */ jsxRuntimeExports.jsx(Card, { title: t("keyboard.spectrum.brightness"), children: /* @__PURE__ */ jsxRuntimeExports.jsx(
      Slider,
      {
        min: 0,
        max: 9,
        value: brightnessDraft,
        onChange: setBrightnessDraft,
        onChangeComplete: (value) => {
          void setBrightness(value).then((ok) => {
            if (!ok) fail();
          });
        },
        style: { width: 320 }
      }
    ) }),
    /* @__PURE__ */ jsxRuntimeExports.jsx(Card, { title: t("keyboard.spectrum.profile"), children: /* @__PURE__ */ jsxRuntimeExports.jsx(
      Radio.Group,
      {
        value: spectrum.profile,
        onChange: (e) => handleProfile(e.target.value),
        options: SPECTRUM_PROFILES.map((profile) => ({ value: profile, label: `${profile}` }))
      }
    ) }),
    /* @__PURE__ */ jsxRuntimeExports.jsx(Card, { title: t("keyboard.spectrum.logo"), children: /* @__PURE__ */ jsxRuntimeExports.jsx(
      Switch,
      {
        checked: spectrum.logo,
        onChange: (checked) => {
          void setLogo(checked).then((ok) => {
            if (!ok) fail();
          });
        }
      }
    ) }),
    /* @__PURE__ */ jsxRuntimeExports.jsx(
      Card,
      {
        title: t("keyboard.spectrum.effects"),
        extra: /* @__PURE__ */ jsxRuntimeExports.jsx(Button, { type: "primary", onClick: handleAddEffect, children: t("keyboard.spectrum.addEffect") }),
        children: spectrum.effects.length === 0 ? /* @__PURE__ */ jsxRuntimeExports.jsx(Empty, { description: t("keyboard.spectrum.noEffects") }) : /* @__PURE__ */ jsxRuntimeExports.jsx(
          List,
          {
            dataSource: spectrum.effects,
            renderItem: (effect, index) => /* @__PURE__ */ jsxRuntimeExports.jsx(
              List.Item,
              {
                actions: [
                  /* @__PURE__ */ jsxRuntimeExports.jsx(
                    Popconfirm,
                    {
                      title: t("keyboard.spectrum.deleteEffect"),
                      onConfirm: () => handleRemoveEffect(index),
                      children: /* @__PURE__ */ jsxRuntimeExports.jsx(Button, { danger: true, size: "small", children: t("keyboard.spectrum.deleteEffect") })
                    },
                    "delete"
                  )
                ],
                children: /* @__PURE__ */ jsxRuntimeExports.jsxs(Space, { children: [
                  /* @__PURE__ */ jsxRuntimeExports.jsx(Tag, { children: t(`keyboard.spectrum.effectTypes.${EFFECT_TYPE_LABEL_KEYS[effect.Type]}`) }),
                  /* @__PURE__ */ jsxRuntimeExports.jsxs(Typography.Text, { type: "secondary", children: [
                    t("keyboard.spectrum.colors"),
                    ": ",
                    effect.Colors.length
                  ] })
                ] })
              }
            )
          }
        )
      }
    )
  ] });
}
function KeyboardBacklightPage() {
  const { t } = useTranslation();
  const { mode, loading, error, load } = useKeyboardStore();
  reactExports.useEffect(() => {
    void load();
  }, [load]);
  if (loading || mode === null) {
    return /* @__PURE__ */ jsxRuntimeExports.jsx(
      "div",
      {
        style: {
          display: "flex",
          justifyContent: "center",
          alignItems: "center",
          minHeight: 320
        },
        children: /* @__PURE__ */ jsxRuntimeExports.jsx(Spin, { size: "large" })
      }
    );
  }
  if (error) {
    return /* @__PURE__ */ jsxRuntimeExports.jsx(Result, { status: "error", title: t("common.error"), subTitle: error });
  }
  return /* @__PURE__ */ jsxRuntimeExports.jsxs("div", { children: [
    /* @__PURE__ */ jsxRuntimeExports.jsx(Typography.Title, { level: 3, style: { marginTop: 0 }, children: t("keyboard.title") }),
    mode === "rgb" ? /* @__PURE__ */ jsxRuntimeExports.jsx(RgbSection, {}) : mode === "spectrum" ? /* @__PURE__ */ jsxRuntimeExports.jsx(SpectrumSection, {}) : /* @__PURE__ */ jsxRuntimeExports.jsx(Empty, { description: t("keyboard.unsupported") })
  ] });
}
export {
  KeyboardBacklightPage as default
};
