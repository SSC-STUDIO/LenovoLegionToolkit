import { v as genStyleHooks, w as merge, p as genFocusOutline, n as unit, A as resetComponent, $ as React, r as reactExports, J as useComponentConfig, aJ as FormItemInputContext, aK as DisabledContext, aE as useControlledState, K as useEvent, a0 as useComposeRef, L as useSemanticRootStyle, N as useMergeSemantic, f as clsx, as as isReactRenderable, Q as useCSSVarCls, e as ConfigContext, bp as isString, ah as isNumber, ag as isNonNullable, h as omit, _ as _toConsumableArray, i as invoke, c as create, u as useTranslation, j as jsxRuntimeExports, F as Flex, T as Typography, bq as Tabs, b as Row, d as Col, S as Spin, C as Card, s as staticMethods } from "./index-3RTipSd5.js";
import { u as useBubbleLock, C as Checkbox$2, D as Divider } from "./index-QUbxwEY1.js";
import { d as genNoMotionRawStyle, e as genNoMotionStyle, T as TARGET_CLS, W as Wave, E as Empty, S as Select } from "./index-BxBscas6.js";
import { S as Space } from "./index-Dro2pb1j.js";
import { T as Tag, B as Button } from "./index-uyL__3sF.js";
import { P as Popconfirm } from "./index-DdhF4o9H.js";
import { S as Switch } from "./index-BbS3n2P6.js";
import "./Addon-CECo-qGW.js";
import "./index-Hdt_DTHG.js";
const genCheckboxStyle = (token) => {
  const {
    checkboxCls,
    checkboxSize,
    lineWidth
  } = token;
  const wrapperCls = `${checkboxCls}-wrapper`;
  const hoverMediaQuery = "@media (hover: hover) and (pointer: fine)";
  return [
    // ===================== Basic =====================
    {
      // Group
      [`${checkboxCls}-group`]: {
        ...resetComponent(token),
        display: "inline-flex",
        flexWrap: "wrap",
        columnGap: token.marginXS,
        // Group > Grid
        [`> ${token.antCls}-row`]: {
          flex: 1
        }
      },
      // Wrapper
      [wrapperCls]: {
        ...resetComponent(token),
        display: "inline-flex",
        alignItems: "baseline",
        cursor: "pointer",
        // Fix checkbox & radio in flex align #30260
        "&:after": {
          display: "inline-block",
          width: 0,
          overflow: "hidden",
          content: "'\\a0'"
        },
        // Checkbox near checkbox
        [`& + ${wrapperCls}`]: {
          marginInlineStart: 0
        }
      },
      // Wrapper > Checkbox
      [checkboxCls]: {
        ...resetComponent(token),
        position: "relative",
        whiteSpace: "nowrap",
        lineHeight: 1,
        cursor: "pointer",
        // To make alignment right when `controlHeight` is changed
        // Ref: https://github.com/ant-design/ant-design/issues/41564
        alignSelf: "center",
        // Styles moved from inner
        boxSizing: "border-box",
        display: "block",
        width: checkboxSize,
        height: checkboxSize,
        direction: "ltr",
        backgroundColor: token.colorBgContainer,
        border: `${unit(lineWidth)} ${token.lineType} ${token.colorBorder}`,
        borderRadius: token.borderRadiusSM,
        borderCollapse: "separate",
        transition: `all ${token.motionDurationSlow}`,
        flex: "none",
        ...genNoMotionStyle(),
        // Checkmark
        "&:after": {
          boxSizing: "border-box",
          position: "absolute",
          top: `calc(${checkboxSize} / 2 - ${lineWidth})`,
          insetInlineStart: `calc(${checkboxSize} / 4 - ${lineWidth})`,
          display: "table",
          width: token.calc(checkboxSize).div(14).mul(5).equal(),
          height: token.calc(checkboxSize).div(14).mul(8).equal(),
          border: `${unit(token.lineWidthBold)} solid ${token.colorWhite}`,
          borderTop: 0,
          borderInlineStart: 0,
          transform: "rotate(45deg) scale(0) translate(-50%,-50%)",
          opacity: 0,
          content: '""',
          transition: `all ${token.motionDurationFast} ${token.motionEaseInBack}, opacity ${token.motionDurationFast}`,
          ...genNoMotionRawStyle()
        },
        // Wrapper > Checkbox > input
        [`${checkboxCls}-input`]: {
          position: "absolute",
          // Since baseline align will get additional space offset,
          // we need to move input to top to make it align with text.
          // Ref: https://github.com/ant-design/ant-design/issues/38926#issuecomment-1486137799
          inset: `calc(-1 * (${lineWidth}))`,
          zIndex: 1,
          cursor: "pointer",
          opacity: 0,
          margin: 0
        },
        // Focus outline on checkbox when input is focus-visible
        [`&:has(${checkboxCls}-input:focus-visible)`]: genFocusOutline(token),
        // Wrapper > Checkbox + Text
        "& + span": {
          paddingInlineStart: token.paddingXS,
          paddingInlineEnd: token.paddingXS
        }
      }
    },
    // ===================== Hover =====================
    {
      [hoverMediaQuery]: {
        // Wrapper & Wrapper > Checkbox
        [`
          ${wrapperCls}:not(${wrapperCls}-disabled),
          ${checkboxCls}:not(${checkboxCls}-disabled)
        `]: {
          [`&:hover ${checkboxCls}`]: {
            borderColor: token.colorPrimary
          }
        },
        [`${wrapperCls}:not(${wrapperCls}-disabled)`]: {
          [`&:hover ${checkboxCls}-checked:not(${checkboxCls}-disabled)`]: {
            backgroundColor: token.colorPrimaryHover,
            borderColor: "transparent"
          }
        }
      }
    },
    // ==================== Checked ====================
    {
      // Wrapper > Checkbox
      [`${checkboxCls}-checked`]: {
        backgroundColor: token.colorPrimary,
        borderColor: token.colorPrimary,
        "&:after": {
          opacity: 1,
          transform: "rotate(45deg) scale(1) translate(-50%,-50%)",
          transition: `all ${token.motionDurationMid} ${token.motionEaseOutBack} ${token.motionDurationFast}`,
          ...genNoMotionRawStyle()
        },
        [hoverMediaQuery]: {
          // Hover on checked checkbox directly
          [`&:not(${checkboxCls}-disabled):hover`]: {
            backgroundColor: token.colorPrimaryHover,
            borderColor: "transparent"
          }
        }
      }
    },
    // ================= Indeterminate =================
    {
      [checkboxCls]: {
        "&-indeterminate": {
          backgroundColor: token.colorBgContainer,
          borderColor: token.colorBorder,
          "&:after": {
            top: "50%",
            insetInlineStart: "50%",
            width: token.calc(token.fontSizeLG).div(2).equal(),
            height: token.calc(token.fontSizeLG).div(2).equal(),
            backgroundColor: token.colorPrimary,
            border: 0,
            transform: "translate(-50%, -50%) scale(1)",
            opacity: 1,
            content: '""'
          },
          [hoverMediaQuery]: {
            // https://github.com/ant-design/ant-design/issues/50074
            [`&:not(${checkboxCls}-disabled):hover`]: {
              backgroundColor: token.colorBgContainer,
              borderColor: token.colorPrimary
            }
          }
        }
      }
    },
    // ==================== Disable ====================
    {
      // Wrapper
      [`${wrapperCls}-disabled`]: {
        cursor: "not-allowed"
      },
      // Wrapper > Checkbox
      [`${checkboxCls}-disabled`]: {
        // Wrapper > Checkbox > input
        [`&, ${checkboxCls}-input`]: {
          cursor: "not-allowed",
          // Disabled for native input to enable Tooltip event handler
          // ref: https://github.com/ant-design/ant-design/issues/39822#issuecomment-1365075901
          pointerEvents: "none"
        },
        // Disabled checkbox styles
        background: token.colorBgContainerDisabled,
        borderColor: token.colorBorder,
        "&:after": {
          borderColor: token.colorTextDisabled
        },
        "& + span": {
          color: token.colorTextDisabled
        },
        [`&${checkboxCls}-indeterminate::after`]: {
          background: token.colorTextDisabled
        }
      }
    }
  ];
};
function getStyle(prefixCls, token) {
  const checkboxToken = merge(token, {
    checkboxCls: `.${prefixCls}`,
    checkboxSize: token.controlInteractiveSize
  });
  return genCheckboxStyle(checkboxToken);
}
const useStyle = genStyleHooks("Checkbox", (token, {
  prefixCls
}) => [getStyle(prefixCls, token)]);
const GroupContext = /* @__PURE__ */ React.createContext(null);
const InternalCheckbox = (props, ref) => {
  const {
    prefixCls: customizePrefixCls,
    children,
    indeterminate = false,
    onMouseEnter,
    onMouseLeave,
    skipGroup = false,
    disabled,
    // Style
    rootClassName,
    className,
    style,
    classNames,
    styles,
    // Name
    name,
    // Value
    value,
    // Checked
    checked,
    defaultChecked,
    onChange,
    ...restProps
  } = props;
  const {
    getPrefixCls,
    direction,
    className: contextClassName,
    style: contextStyle,
    classNames: contextClassNames,
    styles: contextStyles
  } = useComponentConfig("checkbox");
  const checkboxGroup = reactExports.useContext(GroupContext);
  const {
    isFormItemInput
  } = reactExports.useContext(FormItemInputContext);
  const contextDisabled = reactExports.useContext(DisabledContext);
  const mergedDisabled = (checkboxGroup?.disabled || disabled) ?? contextDisabled;
  const [innerChecked, setInnerChecked] = useControlledState(defaultChecked, checked);
  let mergedChecked = innerChecked;
  const onInternalChange = useEvent((event) => {
    setInnerChecked(event.target.checked);
    onChange?.(event);
    if (!skipGroup && checkboxGroup?.toggleOption) {
      checkboxGroup.toggleOption({
        label: children,
        value
      });
    }
  });
  if (checkboxGroup && !skipGroup) {
    mergedChecked = checkboxGroup.value.includes(value);
  }
  const checkboxRef = reactExports.useRef(null);
  const mergedRef = useComposeRef(ref, checkboxRef);
  reactExports.useEffect(() => {
    if (skipGroup || !checkboxGroup) {
      return;
    }
    checkboxGroup.registerValue(value);
    return () => {
      checkboxGroup.cancelValue(value);
    };
  }, [value, skipGroup]);
  reactExports.useEffect(() => {
    if (checkboxRef.current?.input) {
      checkboxRef.current.input.indeterminate = indeterminate;
    }
  }, [indeterminate]);
  const prefixCls = getPrefixCls("checkbox", customizePrefixCls);
  const rootCls = useCSSVarCls(prefixCls);
  const [hashId, cssVarCls] = useStyle(prefixCls, rootCls);
  const checkboxProps = {
    ...restProps
  };
  const mergedProps = {
    ...props,
    indeterminate,
    disabled: mergedDisabled,
    checked: mergedChecked
  };
  const contextStyleRoot = useSemanticRootStyle(contextStyle);
  const styleRoot = useSemanticRootStyle(style);
  const [mergedClassNames, mergedStyles] = useMergeSemantic([contextClassNames, classNames], [contextStyles, contextStyleRoot, styles, styleRoot], {
    props: mergedProps
  });
  const classString = clsx(`${prefixCls}-wrapper`, {
    [`${prefixCls}-rtl`]: direction === "rtl",
    [`${prefixCls}-wrapper-checked`]: mergedChecked,
    [`${prefixCls}-wrapper-disabled`]: mergedDisabled,
    [`${prefixCls}-wrapper-in-form-item`]: isFormItemInput
  }, contextClassName, className, mergedClassNames.root, rootClassName, cssVarCls, rootCls, hashId);
  const checkboxClass = clsx(mergedClassNames.icon, {
    [`${prefixCls}-indeterminate`]: indeterminate
  }, TARGET_CLS, hashId);
  const [onLabelClick, onInputClick] = useBubbleLock(checkboxProps.onClick);
  return /* @__PURE__ */ reactExports.createElement(Wave, {
    component: "Checkbox",
    disabled: mergedDisabled
  }, /* @__PURE__ */ reactExports.createElement("label", {
    className: classString,
    style: mergedStyles.root,
    onMouseEnter,
    onMouseLeave,
    onClick: onLabelClick
  }, /* @__PURE__ */ reactExports.createElement(Checkbox$2, {
    ...checkboxProps,
    name: !skipGroup && checkboxGroup ? checkboxGroup.name : name,
    checked: mergedChecked,
    onClick: onInputClick,
    onChange: onInternalChange,
    prefixCls,
    className: checkboxClass,
    style: mergedStyles.icon,
    disabled: mergedDisabled,
    ref: mergedRef,
    value
  }), isReactRenderable(children) && /* @__PURE__ */ reactExports.createElement("span", {
    className: clsx(`${prefixCls}-label`, mergedClassNames.label),
    style: mergedStyles.label
  }, children)));
};
const Checkbox$1 = /* @__PURE__ */ reactExports.forwardRef(InternalCheckbox);
const CheckboxGroup = /* @__PURE__ */ reactExports.forwardRef((props, ref) => {
  const {
    defaultValue,
    children,
    options = [],
    prefixCls: customizePrefixCls,
    className,
    rootClassName,
    style,
    onChange,
    role = "group",
    ...restProps
  } = props;
  const {
    getPrefixCls,
    direction
  } = reactExports.useContext(ConfigContext);
  const [value, setValue] = reactExports.useState(restProps.value || defaultValue || []);
  const [registeredValues, setRegisteredValues] = reactExports.useState([]);
  reactExports.useEffect(() => {
    if ("value" in restProps) {
      setValue(restProps.value || []);
    }
  }, [restProps.value]);
  const memoizedOptions = reactExports.useMemo(() => {
    return options.map((option) => {
      if (isString(option) || isNumber(option)) {
        return {
          label: option,
          value: option
        };
      }
      return option;
    }).filter((item) => isNonNullable(item) && isNonNullable(item.value));
  }, [options]);
  const cancelValue = (val) => {
    setRegisteredValues((prevValues) => prevValues.filter((v) => v !== val));
  };
  const registerValue = (val) => {
    setRegisteredValues((prevValues) => [].concat(_toConsumableArray(prevValues), [val]));
  };
  const toggleOption = (option) => {
    const optionIndex = value.indexOf(option.value);
    const newValue = _toConsumableArray(value);
    if (optionIndex === -1) {
      newValue.push(option.value);
    } else {
      newValue.splice(optionIndex, 1);
    }
    if (!("value" in restProps)) {
      setValue(newValue);
    }
    onChange?.(newValue.filter((val) => registeredValues.includes(val)).sort((a, b) => {
      const indexA = memoizedOptions.findIndex((opt) => opt.value === a);
      const indexB = memoizedOptions.findIndex((opt) => opt.value === b);
      return indexA - indexB;
    }));
  };
  const prefixCls = getPrefixCls("checkbox", customizePrefixCls);
  const groupPrefixCls = `${prefixCls}-group`;
  const rootCls = useCSSVarCls(prefixCls);
  const [hashId, cssVarCls] = useStyle(prefixCls, rootCls);
  const domProps = omit(restProps, ["value", "disabled"]);
  const childrenNode = Array.isArray(memoizedOptions) && memoizedOptions.length > 0 ? memoizedOptions.map((option) => /* @__PURE__ */ reactExports.createElement(Checkbox$1, {
    prefixCls,
    key: option.value.toString(),
    disabled: "disabled" in option ? option.disabled : restProps.disabled,
    value: option.value,
    checked: value.includes(option.value),
    onChange: option.onChange,
    className: clsx(`${groupPrefixCls}-item`, option.className),
    style: option.style,
    title: option.title,
    id: option.id,
    required: option.required
  }, option.label)) : children;
  const memoizedContext = reactExports.useMemo(() => ({
    toggleOption,
    value,
    disabled: restProps.disabled,
    name: restProps.name,
    // https://github.com/ant-design/ant-design/issues/16376
    registerValue,
    cancelValue
  }), [toggleOption, value, restProps.disabled, restProps.name, registerValue, cancelValue]);
  const classString = clsx(groupPrefixCls, {
    [`${groupPrefixCls}-rtl`]: direction === "rtl"
  }, className, rootClassName, cssVarCls, rootCls, hashId);
  return /* @__PURE__ */ reactExports.createElement("div", {
    className: classString,
    style,
    role,
    ...domProps,
    ref
  }, /* @__PURE__ */ reactExports.createElement(GroupContext.Provider, {
    value: memoizedContext
  }, childrenNode));
});
const Checkbox = Checkbox$1;
Checkbox.Group = CheckboxGroup;
Checkbox.__ANT_CHECKBOX = true;
const optimizationApi = {
  async getCategories() {
    return invoke("optimization.getCategories", {});
  },
  async apply(actionKeys) {
    return invoke("optimization.apply", { actionKeys });
  },
  async revert(actionKeys) {
    return invoke("optimization.revert", { actionKeys });
  },
  async applyRecommended() {
    return invoke("optimization.applyRecommended", {});
  },
  async getActionStatus(actionKey) {
    return invoke("optimization.getActionStatus", { actionKey });
  },
  async estimateCleanup(actionKeys) {
    return invoke("cleanup.estimate", { actionKeys });
  },
  async runCleanup(actionKeys) {
    return invoke("cleanup.run", { actionKeys });
  },
  async networkGetStatus() {
    return invoke("network.getStatus", {});
  },
  async networkSaveConfig(config) {
    return invoke("network.saveConfig", { config });
  },
  async networkStart() {
    return invoke("network.start", {});
  },
  async networkStop() {
    return invoke("network.stop", {});
  }
};
const useOptimizationStore = create((set, get) => ({
  categories: [],
  networkStatus: null,
  loading: false,
  error: null,
  async load() {
    if (get().loading) return;
    set({ loading: true, error: null });
    try {
      const { categories } = await optimizationApi.getCategories();
      set({ categories });
    } catch (error) {
      set({ error: error.message });
    } finally {
      set({ loading: false });
    }
  },
  async apply(keys) {
    if (keys.length === 0) return true;
    try {
      const res = await optimizationApi.apply(keys);
      if (!res.applied) return false;
      await get().load();
      return true;
    } catch (error) {
      set({ error: error.message });
      return false;
    }
  },
  async revert(keys) {
    if (keys.length === 0) return true;
    try {
      const res = await optimizationApi.revert(keys);
      if (!res.reverted) return false;
      await get().load();
      return true;
    } catch (error) {
      set({ error: error.message });
      return false;
    }
  },
  async applyRecommended() {
    try {
      const res = await optimizationApi.applyRecommended();
      if (!res.applied) return false;
      await get().load();
      return true;
    } catch (error) {
      set({ error: error.message });
      return false;
    }
  },
  async estimate(keys) {
    if (keys.length === 0) return 0;
    try {
      const res = await optimizationApi.estimateCleanup(keys);
      return res.bytes;
    } catch (error) {
      set({ error: error.message });
      return 0;
    }
  },
  async runCleanup(keys) {
    if (keys.length === 0) return true;
    try {
      const res = await optimizationApi.runCleanup(keys);
      if (!res.done) return false;
      await get().load();
      return true;
    } catch (error) {
      set({ error: error.message });
      return false;
    }
  },
  async loadNetwork() {
    try {
      const status = await optimizationApi.networkGetStatus();
      set({ networkStatus: status });
    } catch (error) {
      set({ error: error.message });
    }
  },
  async saveNetworkConfig(config) {
    try {
      const res = await optimizationApi.networkSaveConfig(config);
      if (!res.saved) return false;
      await get().loadNetwork();
      return true;
    } catch (error) {
      set({ error: error.message });
      return false;
    }
  },
  async startNetwork() {
    try {
      const res = await optimizationApi.networkStart();
      if (!res.ok) return false;
      await get().loadNetwork();
      return true;
    } catch (error) {
      set({ error: error.message });
      return false;
    }
  },
  async stopNetwork() {
    try {
      const res = await optimizationApi.networkStop();
      if (!res.ok) return false;
      await get().loadNetwork();
      return true;
    } catch (error) {
      set({ error: error.message });
      return false;
    }
  }
}));
function formatBytes(bytes) {
  if (!Number.isFinite(bytes) || bytes <= 0) return "0 B";
  const gb = bytes / 1024 ** 3;
  if (gb >= 1) return `${gb.toFixed(2)} GB`;
  const mb = bytes / 1024 ** 2;
  if (mb >= 1) return `${mb.toFixed(1)} MB`;
  return `${bytes.toFixed(0)} B`;
}
function findAction(categories, key) {
  for (const category of categories) {
    const action = category.actions.find((a) => a.key === key);
    if (action) return action;
  }
  return null;
}
function OptimizationTab() {
  const { t } = useTranslation();
  const categories = useOptimizationStore((s) => s.categories);
  const loading = useOptimizationStore((s) => s.loading);
  const apply = useOptimizationStore((s) => s.apply);
  const revert = useOptimizationStore((s) => s.revert);
  const applyRecommended = useOptimizationStore((s) => s.applyRecommended);
  const [selectedKeys, setSelectedKeys] = reactExports.useState([]);
  const [busy, setBusy] = reactExports.useState(false);
  const optimizationCategories = categories.filter((c) => !c.key.startsWith("cleanup."));
  const selectedActions = selectedKeys.map((key) => findAction(categories, key)).filter((action) => action !== null);
  const toggleSelection = (key) => {
    setSelectedKeys((prev) => prev.includes(key) ? prev.filter((k) => k !== key) : [...prev, key]);
  };
  const handleSelectRecommended = () => {
    const keys = optimizationCategories.flatMap(
      (category) => category.actions.filter((action) => action.recommended).map((action) => action.key)
    );
    setSelectedKeys(keys);
  };
  const handleApply = async () => {
    if (selectedKeys.length === 0) return;
    setBusy(true);
    const ok = await apply(selectedKeys);
    setBusy(false);
    if (ok) {
      setSelectedKeys([]);
      staticMethods.success(t("optimization.applied"));
    } else {
      staticMethods.error(t("optimization.applyFailed"));
    }
  };
  const handleClear = async () => {
    if (selectedKeys.length === 0) return;
    setBusy(true);
    const ok = await revert(selectedKeys);
    setBusy(false);
    if (ok) {
      setSelectedKeys([]);
      staticMethods.success(t("optimization.reverted"));
    } else {
      staticMethods.error(t("optimization.revertFailed"));
    }
  };
  const handleApplyRecommended = async () => {
    setBusy(true);
    const ok = await applyRecommended();
    setBusy(false);
    if (ok) staticMethods.success(t("optimization.applied"));
    else staticMethods.error(t("optimization.applyFailed"));
  };
  return /* @__PURE__ */ jsxRuntimeExports.jsxs(Row, { gutter: 16, children: [
    /* @__PURE__ */ jsxRuntimeExports.jsx(Col, { xs: 24, lg: 12, children: /* @__PURE__ */ jsxRuntimeExports.jsxs(Flex, { vertical: true, gap: 12, children: [
      loading && /* @__PURE__ */ jsxRuntimeExports.jsx(Spin, {}),
      optimizationCategories.map((category) => /* @__PURE__ */ jsxRuntimeExports.jsxs(Card, { size: "small", title: category.title, children: [
        /* @__PURE__ */ jsxRuntimeExports.jsx(Typography.Paragraph, { type: "secondary", style: { marginBottom: 8 }, children: category.description }),
        /* @__PURE__ */ jsxRuntimeExports.jsx(Flex, { vertical: true, gap: 4, children: category.actions.map((action) => {
          const selected = selectedKeys.includes(action.key);
          return /* @__PURE__ */ jsxRuntimeExports.jsxs(Flex, { align: "center", justify: "space-between", children: [
            /* @__PURE__ */ jsxRuntimeExports.jsx(
              Checkbox,
              {
                checked: action.applied === true,
                indeterminate: action.applied === null,
                onChange: () => toggleSelection(action.key),
                children: action.title
              }
            ),
            /* @__PURE__ */ jsxRuntimeExports.jsxs(Space, { size: 4, children: [
              action.recommended && /* @__PURE__ */ jsxRuntimeExports.jsxs(Tag, { color: "gold", children: [
                "★ ",
                t("optimization.recommended")
              ] }),
              selected && /* @__PURE__ */ jsxRuntimeExports.jsx(Tag, { color: "blue", children: t("optimization.selected") })
            ] })
          ] }, action.key);
        }) })
      ] }, category.key))
    ] }) }),
    /* @__PURE__ */ jsxRuntimeExports.jsx(Col, { xs: 24, lg: 12, children: /* @__PURE__ */ jsxRuntimeExports.jsx(
      Card,
      {
        size: "small",
        title: t("optimization.selectedActions"),
        extra: /* @__PURE__ */ jsxRuntimeExports.jsx(Typography.Text, { type: "secondary", children: selectedActions.length }),
        children: selectedActions.length === 0 ? /* @__PURE__ */ jsxRuntimeExports.jsx(Empty, { description: t("optimization.noSelection") }) : /* @__PURE__ */ jsxRuntimeExports.jsxs(Flex, { vertical: true, gap: 8, children: [
          selectedActions.map((action) => /* @__PURE__ */ jsxRuntimeExports.jsxs(Flex, { align: "center", justify: "space-between", children: [
            /* @__PURE__ */ jsxRuntimeExports.jsx(Typography.Text, { children: action.title }),
            action.recommended && /* @__PURE__ */ jsxRuntimeExports.jsx(Tag, { color: "gold", children: "★" })
          ] }, action.key)),
          /* @__PURE__ */ jsxRuntimeExports.jsx(Divider, { style: { margin: "8px 0" } }),
          /* @__PURE__ */ jsxRuntimeExports.jsxs(Flex, { gap: 8, wrap: true, children: [
            /* @__PURE__ */ jsxRuntimeExports.jsx(Button, { onClick: handleSelectRecommended, children: t("optimization.selectRecommended") }),
            /* @__PURE__ */ jsxRuntimeExports.jsx(
              Button,
              {
                type: "primary",
                loading: busy,
                disabled: selectedActions.length === 0,
                onClick: () => void handleApply(),
                children: t("optimization.apply")
              }
            ),
            /* @__PURE__ */ jsxRuntimeExports.jsx(
              Button,
              {
                danger: true,
                loading: busy,
                disabled: selectedActions.length === 0,
                onClick: () => void handleClear(),
                children: t("optimization.clear")
              }
            ),
            /* @__PURE__ */ jsxRuntimeExports.jsx(Button, { onClick: () => void handleApplyRecommended(), children: t("optimization.applyRecommended") })
          ] })
        ] })
      }
    ) })
  ] });
}
function CleanupTab() {
  const { t } = useTranslation();
  const categories = useOptimizationStore((s) => s.categories);
  const estimate = useOptimizationStore((s) => s.estimate);
  const runCleanup = useOptimizationStore((s) => s.runCleanup);
  const [selectedKeys, setSelectedKeys] = reactExports.useState([]);
  const [estimateBytes, setEstimateBytes] = reactExports.useState(null);
  const [estimating, setEstimating] = reactExports.useState(false);
  const [cleaning, setCleaning] = reactExports.useState(false);
  const cleanupCategories = categories.filter((c) => c.key.startsWith("cleanup."));
  const toggleSelection = (key) => {
    setSelectedKeys((prev) => prev.includes(key) ? prev.filter((k) => k !== key) : [...prev, key]);
  };
  const handleEstimate = async () => {
    if (selectedKeys.length === 0) return;
    setEstimating(true);
    const bytes = await estimate(selectedKeys);
    setEstimating(false);
    setEstimateBytes(bytes);
  };
  const handleRun = async () => {
    setCleaning(true);
    const ok = await runCleanup(selectedKeys);
    setCleaning(false);
    if (ok) {
      staticMethods.success(t("optimization.cleanupDone"));
      setSelectedKeys([]);
      setEstimateBytes(null);
    } else {
      staticMethods.error(t("optimization.cleanupFailed"));
    }
  };
  return /* @__PURE__ */ jsxRuntimeExports.jsxs(Flex, { vertical: true, gap: 16, children: [
    /* @__PURE__ */ jsxRuntimeExports.jsx(Typography.Paragraph, { type: "secondary", children: t("optimization.cleanupHint") }),
    cleanupCategories.map((category) => /* @__PURE__ */ jsxRuntimeExports.jsxs(Card, { size: "small", title: category.title, children: [
      /* @__PURE__ */ jsxRuntimeExports.jsx(Typography.Paragraph, { type: "secondary", style: { marginBottom: 8 }, children: category.description }),
      /* @__PURE__ */ jsxRuntimeExports.jsx(Flex, { vertical: true, gap: 4, children: category.actions.map((action) => /* @__PURE__ */ jsxRuntimeExports.jsx(
        Checkbox,
        {
          checked: selectedKeys.includes(action.key),
          onChange: () => toggleSelection(action.key),
          children: action.title
        },
        action.key
      )) })
    ] }, category.key)),
    /* @__PURE__ */ jsxRuntimeExports.jsxs(Flex, { align: "center", gap: 12, wrap: true, children: [
      /* @__PURE__ */ jsxRuntimeExports.jsx(Button, { loading: estimating, disabled: selectedKeys.length === 0, onClick: () => void handleEstimate(), children: t("optimization.estimate") }),
      estimateBytes !== null && /* @__PURE__ */ jsxRuntimeExports.jsxs(Typography.Text, { strong: true, children: [
        t("optimization.estimateResult"),
        ": ",
        formatBytes(estimateBytes)
      ] }),
      /* @__PURE__ */ jsxRuntimeExports.jsx(Popconfirm, { title: t("optimization.cleanupConfirm"), onConfirm: () => void handleRun(), children: /* @__PURE__ */ jsxRuntimeExports.jsx(Button, { type: "primary", danger: true, loading: cleaning, disabled: selectedKeys.length === 0, children: t("optimization.runCleanup") }) })
    ] })
  ] });
}
function DriverDownloadTab() {
  const { t } = useTranslation();
  return /* @__PURE__ */ jsxRuntimeExports.jsx(Card, { children: /* @__PURE__ */ jsxRuntimeExports.jsx(Empty, { description: t("optimization.driverDownload.comingSoon") }) });
}
const NETWORK_MODES = ["Off", "SystemProxy", "Hosts", "DiagnosticsOnly"];
const NETWORK_MODE_I18N_KEYS = {
  Off: "optimization.network.modes.off",
  SystemProxy: "optimization.network.modes.systemProxy",
  Hosts: "optimization.network.modes.hosts",
  DiagnosticsOnly: "optimization.network.modes.diagnosticsOnly"
};
function NetworkTab() {
  const { t } = useTranslation();
  const networkStatus = useOptimizationStore((s) => s.networkStatus);
  const saveNetworkConfig = useOptimizationStore((s) => s.saveNetworkConfig);
  const startNetwork = useOptimizationStore((s) => s.startNetwork);
  const stopNetwork = useOptimizationStore((s) => s.stopNetwork);
  const [config, setConfig] = reactExports.useState(null);
  const [saving, setSaving] = reactExports.useState(false);
  const [starting, setStarting] = reactExports.useState(false);
  const [stopping, setStopping] = reactExports.useState(false);
  reactExports.useEffect(() => {
    if (networkStatus) {
      setConfig({ ...networkStatus.config, domainGroups: [...networkStatus.config.domainGroups] });
    }
  }, [networkStatus]);
  const handleSave = async () => {
    if (!config) return;
    setSaving(true);
    const ok = await saveNetworkConfig(config);
    setSaving(false);
    if (ok) staticMethods.success(t("optimization.network.saved"));
    else staticMethods.error(t("optimization.network.saveFailed"));
  };
  const handleStart = async () => {
    setStarting(true);
    const ok = await startNetwork();
    setStarting(false);
    if (!ok) staticMethods.error(t("optimization.network.startFailed"));
  };
  const handleStop = async () => {
    setStopping(true);
    const ok = await stopNetwork();
    setStopping(false);
    if (!ok) staticMethods.error(t("optimization.network.stopFailed"));
  };
  if (!networkStatus || !config) return /* @__PURE__ */ jsxRuntimeExports.jsx(Spin, {});
  return /* @__PURE__ */ jsxRuntimeExports.jsxs(Flex, { vertical: true, gap: 16, children: [
    /* @__PURE__ */ jsxRuntimeExports.jsx(Card, { size: "small", title: t("optimization.network.status"), children: /* @__PURE__ */ jsxRuntimeExports.jsxs(Flex, { gap: 16, align: "center", wrap: true, children: [
      /* @__PURE__ */ jsxRuntimeExports.jsx(Tag, { color: networkStatus.isRunning ? "green" : "default", children: networkStatus.isRunning ? t("optimization.network.running") : t("optimization.network.stopped") }),
      /* @__PURE__ */ jsxRuntimeExports.jsx(Typography.Text, { type: "secondary", children: networkStatus.statusText }),
      /* @__PURE__ */ jsxRuntimeExports.jsx(Tag, { color: networkStatus.isBackendReady ? "blue" : "red", children: networkStatus.isBackendReady ? t("optimization.network.backendReady") : t("optimization.network.backendNotReady") })
    ] }) }),
    /* @__PURE__ */ jsxRuntimeExports.jsx(Card, { size: "small", title: t("optimization.network.config"), children: /* @__PURE__ */ jsxRuntimeExports.jsxs(Flex, { vertical: true, gap: 12, children: [
      /* @__PURE__ */ jsxRuntimeExports.jsxs(Flex, { align: "center", gap: 8, children: [
        /* @__PURE__ */ jsxRuntimeExports.jsx(Typography.Text, { children: t("optimization.network.accelerationEnabled") }),
        /* @__PURE__ */ jsxRuntimeExports.jsx(
          Switch,
          {
            checked: config.accelerationEnabled,
            onChange: (checked) => setConfig({ ...config, accelerationEnabled: checked })
          }
        )
      ] }),
      /* @__PURE__ */ jsxRuntimeExports.jsxs(Flex, { align: "center", gap: 8, children: [
        /* @__PURE__ */ jsxRuntimeExports.jsx(Typography.Text, { children: t("optimization.network.mode") }),
        /* @__PURE__ */ jsxRuntimeExports.jsx(
          Select,
          {
            style: { width: 220 },
            value: config.mode,
            options: NETWORK_MODES.map((mode) => ({
              value: mode,
              label: t(NETWORK_MODE_I18N_KEYS[mode])
            })),
            onChange: (mode) => setConfig({ ...config, mode })
          }
        )
      ] }),
      /* @__PURE__ */ jsxRuntimeExports.jsxs(Flex, { gap: 8, wrap: true, children: [
        /* @__PURE__ */ jsxRuntimeExports.jsx(Button, { type: "primary", loading: saving, onClick: () => void handleSave(), children: t("optimization.network.save") }),
        /* @__PURE__ */ jsxRuntimeExports.jsx(
          Button,
          {
            loading: starting,
            disabled: !networkStatus.isBackendReady,
            onClick: () => void handleStart(),
            children: t("optimization.network.start")
          }
        ),
        /* @__PURE__ */ jsxRuntimeExports.jsx(Button, { danger: true, loading: stopping, onClick: () => void handleStop(), children: t("optimization.network.stop") })
      ] })
    ] }) })
  ] });
}
function WindowsOptimizationPage() {
  const { t } = useTranslation();
  const load = useOptimizationStore((s) => s.load);
  const loadNetwork = useOptimizationStore((s) => s.loadNetwork);
  reactExports.useEffect(() => {
    void load();
    void loadNetwork();
  }, [load, loadNetwork]);
  return /* @__PURE__ */ jsxRuntimeExports.jsxs(Flex, { vertical: true, gap: 16, children: [
    /* @__PURE__ */ jsxRuntimeExports.jsx(Typography.Title, { level: 3, style: { margin: 0 }, children: t("optimization.title") }),
    /* @__PURE__ */ jsxRuntimeExports.jsx(
      Tabs,
      {
        items: [
          {
            key: "optimization",
            label: t("optimization.tabs.optimization"),
            children: /* @__PURE__ */ jsxRuntimeExports.jsx(OptimizationTab, {})
          },
          {
            key: "cleanup",
            label: t("optimization.tabs.cleanup"),
            children: /* @__PURE__ */ jsxRuntimeExports.jsx(CleanupTab, {})
          },
          {
            key: "driverDownload",
            label: t("optimization.tabs.driverDownload"),
            children: /* @__PURE__ */ jsxRuntimeExports.jsx(DriverDownloadTab, {})
          },
          {
            key: "networkAcceleration",
            label: t("optimization.tabs.networkAcceleration"),
            children: /* @__PURE__ */ jsxRuntimeExports.jsx(NetworkTab, {})
          }
        ]
      }
    )
  ] });
}
export {
  WindowsOptimizationPage as default
};
