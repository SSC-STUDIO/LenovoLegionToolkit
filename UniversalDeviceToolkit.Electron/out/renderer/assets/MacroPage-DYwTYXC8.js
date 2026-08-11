import { i as invoke, c as create, u as useTranslation, r as reactExports, j as jsxRuntimeExports, F as Flex, T as Typography, C as Card, s as staticMethods } from "./index-3RTipSd5.js";
import { S as Space } from "./index-Dro2pb1j.js";
import { S as Switch } from "./index-BbS3n2P6.js";
import { B as Button, T as Tag } from "./index-uyL__3sF.js";
import { S as Select } from "./index-BxBscas6.js";
import "./Addon-CECo-qGW.js";
const macroApi = {
  async getState() {
    return invoke("macro.getState", {});
  },
  async setEnabled(enabled) {
    return invoke("macro.setEnabled", { enabled });
  },
  async play(key) {
    return invoke("macro.play", { key });
  },
  async startRecording(mode, key) {
    return invoke("macro.startRecording", { mode, key });
  },
  async stopRecording() {
    return invoke("macro.stopRecording", {});
  },
  async saveSequence(params) {
    return invoke("macro.saveSequence", params);
  },
  async clearSequence(key) {
    return invoke("macro.clearSequence", { key });
  }
};
const defaultState = { isEnabled: false, slots: [] };
const useMacroStore = create()((set, get) => ({
  state: defaultState,
  loaded: false,
  loading: false,
  error: null,
  async load() {
    if (get().loading) return;
    set({ loading: true, error: null });
    try {
      const state = await macroApi.getState();
      set({ state, loaded: true });
    } catch (error) {
      set({ error: error.message });
    } finally {
      set({ loading: false });
    }
  },
  async setEnabled(enabled) {
    try {
      const res = await macroApi.setEnabled(enabled);
      if (!res.ok) return false;
      set({ state: { ...get().state, isEnabled: enabled } });
      return true;
    } catch (error) {
      set({ error: error.message });
      return false;
    }
  },
  async play(key) {
    try {
      const res = await macroApi.play(key);
      return res.ok;
    } catch (error) {
      set({ error: error.message });
      return false;
    }
  },
  async startRecording(mode, key) {
    try {
      const res = await macroApi.startRecording(mode, key);
      return res.ok;
    } catch (error) {
      set({ error: error.message });
      return false;
    }
  },
  async stopRecording() {
    try {
      const res = await macroApi.stopRecording();
      return res.events;
    } catch (error) {
      set({ error: error.message });
      return null;
    }
  },
  async saveSequence(params) {
    try {
      const res = await macroApi.saveSequence(params);
      if (!res.ok) return false;
      await get().load();
      return true;
    } catch (error) {
      set({ error: error.message });
      return false;
    }
  },
  async clearSequence(key) {
    try {
      const res = await macroApi.clearSequence(key);
      if (!res.ok) return false;
      await get().load();
      return true;
    } catch (error) {
      set({ error: error.message });
      return false;
    }
  }
}));
const NUMPAD_KEYS = [
  { label: "0", code: 96 },
  { label: "1", code: 97 },
  { label: "2", code: 98 },
  { label: "3", code: 99 },
  { label: "4", code: 100 },
  { label: "5", code: 101 },
  { label: "6", code: 102 },
  { label: "7", code: 103 },
  { label: "8", code: 104 },
  { label: "9", code: 105 }
];
const REPEAT_OPTIONS = Array.from({ length: 10 }, (_, i) => ({ value: i + 1, label: `${i + 1}` }));
function MacroPage() {
  const { t } = useTranslation();
  const { state, load, setEnabled, play, saveSequence, clearSequence } = useMacroStore();
  const [selectedKey, setSelectedKey] = reactExports.useState(97);
  const [repeatCount, setRepeatCount] = reactExports.useState(1);
  const [events, setEvents] = reactExports.useState([]);
  const [savedEvents, setSavedEvents] = reactExports.useState([]);
  reactExports.useEffect(() => {
    void load();
  }, [load]);
  reactExports.useEffect(() => {
    const slot = state?.slots?.find((s) => s.key === selectedKey);
    setSavedEvents(slot?.events ?? []);
  }, [state, selectedKey]);
  const handleSave = async () => {
    try {
      await saveSequence({
        key: selectedKey,
        repeatCount,
        ignoreDelays: false,
        interruptOnOtherKey: false,
        events
      });
      setSavedEvents(events);
      staticMethods.success(t("settings.saved"));
    } catch (error) {
      staticMethods.error(error.message);
    }
  };
  const handleClear = async () => {
    try {
      await clearSequence(selectedKey);
      setEvents([]);
      setSavedEvents([]);
      void load();
    } catch (error) {
      staticMethods.error(error.message);
    }
  };
  return /* @__PURE__ */ jsxRuntimeExports.jsxs(Flex, { vertical: true, gap: 16, children: [
    /* @__PURE__ */ jsxRuntimeExports.jsxs(Flex, { align: "center", justify: "space-between", children: [
      /* @__PURE__ */ jsxRuntimeExports.jsx(Typography.Title, { level: 3, style: { margin: 0 }, children: t("macro.title") }),
      /* @__PURE__ */ jsxRuntimeExports.jsxs(Space, { children: [
        /* @__PURE__ */ jsxRuntimeExports.jsx("span", { children: t("macro.enable") }),
        /* @__PURE__ */ jsxRuntimeExports.jsx(
          Switch,
          {
            checked: state?.isEnabled ?? false,
            onChange: (checked) => void setEnabled(checked)
          }
        )
      ] })
    ] }),
    /* @__PURE__ */ jsxRuntimeExports.jsxs(Flex, { gap: 16, wrap: true, children: [
      /* @__PURE__ */ jsxRuntimeExports.jsx(Card, { title: t("macro.numpad"), style: { width: 260 }, children: /* @__PURE__ */ jsxRuntimeExports.jsx(Flex, { gap: 8, wrap: true, justify: "center", children: NUMPAD_KEYS.map((key) => /* @__PURE__ */ jsxRuntimeExports.jsx(
        Button,
        {
          type: selectedKey === key.code ? "primary" : "default",
          style: { width: 56, height: 56, fontSize: 18 },
          onClick: () => setSelectedKey(key.code),
          children: key.label
        },
        key.code
      )) }) }),
      /* @__PURE__ */ jsxRuntimeExports.jsx(
        Card,
        {
          title: `${t("macro.sequence")} - ${NUMPAD_KEYS.find((k) => k.code === selectedKey)?.label}`,
          style: { flex: 1, minWidth: 420 },
          children: /* @__PURE__ */ jsxRuntimeExports.jsxs(Space, { direction: "vertical", style: { width: "100%" }, size: "middle", children: [
            /* @__PURE__ */ jsxRuntimeExports.jsxs(Flex, { gap: 12, align: "center", children: [
              /* @__PURE__ */ jsxRuntimeExports.jsx("span", { children: t("macro.repeat") }),
              /* @__PURE__ */ jsxRuntimeExports.jsx(
                Select,
                {
                  style: { width: 90 },
                  value: repeatCount,
                  options: REPEAT_OPTIONS,
                  onChange: setRepeatCount
                }
              ),
              /* @__PURE__ */ jsxRuntimeExports.jsx(Tag, { color: "blue", children: events.length })
            ] }),
            /* @__PURE__ */ jsxRuntimeExports.jsxs(Flex, { gap: 8, children: [
              /* @__PURE__ */ jsxRuntimeExports.jsx(Button, { type: "primary", onClick: () => void handleSave(), children: t("macro.save") }),
              /* @__PURE__ */ jsxRuntimeExports.jsx(Button, { danger: true, onClick: () => void handleClear(), children: t("macro.clear") }),
              /* @__PURE__ */ jsxRuntimeExports.jsx(Button, { onClick: () => void play(selectedKey), children: t("macro.play") })
            ] }),
            /* @__PURE__ */ jsxRuntimeExports.jsxs(Typography.Text, { type: "secondary", children: [
              t("macro.events"),
              ": ",
              events.length > 0 ? events.length : savedEvents.length
            ] }),
            (events.length > 0 ? events : savedEvents).length === 0 && /* @__PURE__ */ jsxRuntimeExports.jsx(Typography.Text, { type: "secondary", children: t("macro.empty") })
          ] })
        }
      )
    ] })
  ] });
}
export {
  MacroPage as default
};
