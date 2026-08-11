import { r as reactExports, e as ConfigContext, bz as useStyle$2, f as clsx, aJ as FormItemInputContext, v as genStyleHooks, w as merge, aZ as initInputToken, aY as initComponentToken, aV as wrapperRaf, J as useComponentConfig, b8 as useVariant, L as useSemanticRootStyle, N as useMergeSemantic, a2 as pickAttrs, aL as useSize, ba as getMergedStatus, K as useEvent, _ as _toConsumableArray, l as isFunction, aA as getDefaultExportFromCjs, aB as Icon, a8 as useLocale, aK as DisabledContext, Z as isPlainObject, af as composeRef, a$ as genCssVar, n as unit, b7 as useCompactItemContext, an as fallbackProp, k as cloneElement, h as omit, b6 as Compact, bA as TextArea } from "./index-3RTipSd5.js";
import { S as Space } from "./index-Dro2pb1j.js";
import { I as Input$1, u as useRemovePasswordTimeout } from "./Input-mSSMIOSE.js";
import { f as RefIcon$2 } from "./index-BxBscas6.js";
import { B as Button } from "./index-uyL__3sF.js";
const Group = (props) => {
  const {
    getPrefixCls,
    direction
  } = reactExports.useContext(ConfigContext);
  const {
    prefixCls: customizePrefixCls,
    className
  } = props;
  const prefixCls = getPrefixCls("input-group", customizePrefixCls);
  const inputPrefixCls = getPrefixCls("input");
  const [hashId, cssVarCls] = useStyle$2(inputPrefixCls);
  const cls = clsx(prefixCls, cssVarCls, {
    [`${prefixCls}-lg`]: props.size === "large",
    [`${prefixCls}-sm`]: props.size === "small",
    [`${prefixCls}-compact`]: props.compact,
    [`${prefixCls}-rtl`]: direction === "rtl"
  }, hashId, className);
  const formItemContext = reactExports.useContext(FormItemInputContext);
  const groupFormItemContext = reactExports.useMemo(() => ({
    ...formItemContext,
    isFormItemInput: false
  }), [formItemContext]);
  return /* @__PURE__ */ reactExports.createElement(FormItemInputContext.Provider, {
    value: groupFormItemContext
  }, /* @__PURE__ */ reactExports.createElement(Space.Compact, {
    className: cls,
    style: props.style,
    onMouseEnter: props.onMouseEnter,
    onMouseLeave: props.onMouseLeave,
    onFocus: props.onFocus,
    onBlur: props.onBlur
  }, props.children));
};
const genOTPStyle = (token) => {
  const {
    componentCls,
    paddingXS
  } = token;
  return {
    [componentCls]: {
      display: "inline-flex",
      alignItems: "center",
      flexWrap: "nowrap",
      columnGap: paddingXS,
      [`${componentCls}-input-wrapper`]: {
        position: "relative",
        [`${componentCls}-mask-icon`]: {
          position: "absolute",
          zIndex: "1",
          top: "50%",
          right: "50%",
          transform: "translate(50%, -50%)",
          pointerEvents: "none"
        },
        [`${componentCls}-mask-input`]: {
          color: "transparent",
          caretColor: token.colorText,
          "&::selection": {
            color: "transparent"
          }
        },
        [`${componentCls}-mask-input[type=number]::-webkit-inner-spin-button`]: {
          "-webkit-appearance": "none",
          margin: 0
        },
        [`${componentCls}-mask-input[type=number]`]: {
          "-moz-appearance": "textfield"
        }
      },
      "&-rtl": {
        direction: "rtl"
      },
      [`${componentCls}-input`]: {
        textAlign: "center",
        paddingInline: token.paddingXXS
      },
      // ================= Size =================
      [`&${componentCls}-sm ${componentCls}-input`]: {
        paddingInline: token.calc(token.paddingXXS).div(2).equal()
      },
      [`&${componentCls}-lg ${componentCls}-input`]: {
        paddingInline: token.paddingXS
      }
    }
  };
};
const useStyle$1 = genStyleHooks(["Input", "OTP"], (token) => {
  const inputToken = merge(token, initInputToken(token));
  return genOTPStyle(inputToken);
}, initComponentToken);
const DEFAULT_MASK_VALUE = "•";
const OTPInput = /* @__PURE__ */ reactExports.forwardRef((props, ref) => {
  const {
    className,
    value,
    onChange,
    onActiveChange,
    index,
    mask,
    onFocus,
    type,
    ...restProps
  } = props;
  const {
    getPrefixCls
  } = reactExports.useContext(ConfigContext);
  const prefixCls = getPrefixCls("otp");
  const maskValue = typeof mask === "string" ? mask : DEFAULT_MASK_VALUE;
  const inputRef = reactExports.useRef(null);
  reactExports.useImperativeHandle(ref, () => inputRef.current);
  const onInternalChange = (e) => {
    onChange(index, e.target.value);
  };
  const syncSelection = () => {
    wrapperRaf(() => {
      const inputEle = inputRef.current?.input;
      if (document.activeElement === inputEle && inputEle) {
        inputEle.select();
      }
    });
  };
  const onInternalFocus = (e) => {
    onFocus?.(e);
    syncSelection();
  };
  const onInternalKeyDown = (event) => {
    const {
      key,
      ctrlKey,
      metaKey
    } = event;
    if (key === "ArrowLeft") {
      onActiveChange(index - 1);
    } else if (key === "ArrowRight") {
      onActiveChange(index + 1);
    } else if (key === "z" && (ctrlKey || metaKey)) {
      event.preventDefault();
    } else if (key === "Backspace" && !value) {
      onActiveChange(index - 1);
    }
    syncSelection();
  };
  return /* @__PURE__ */ reactExports.createElement("span", {
    className: `${prefixCls}-input-wrapper`,
    role: "presentation"
  }, mask && value !== "" && value !== void 0 && /* @__PURE__ */ reactExports.createElement("span", {
    className: `${prefixCls}-mask-icon`,
    "aria-hidden": "true"
  }, maskValue), /* @__PURE__ */ reactExports.createElement(Input$1, {
    "aria-label": `OTP Input ${index + 1}`,
    ...restProps,
    type: type ?? (mask ? "password" : "text"),
    ref: inputRef,
    value,
    onInput: onInternalChange,
    onFocus: onInternalFocus,
    onKeyDown: onInternalKeyDown,
    onMouseDown: syncSelection,
    onMouseUp: syncSelection,
    className: clsx(className, {
      [`${prefixCls}-mask-input`]: mask
    })
  }));
});
function strToArr(str) {
  return (str || "").split("");
}
const Separator = (props) => {
  const {
    index,
    prefixCls,
    separator,
    className: semanticClassName,
    style: semanticStyle
  } = props;
  const separatorNode = isFunction(separator) ? separator(index) : separator;
  if (!separatorNode) {
    return null;
  }
  return /* @__PURE__ */ reactExports.createElement("span", {
    className: clsx(`${prefixCls}-separator`, semanticClassName),
    style: semanticStyle
  }, separatorNode);
};
const OTP = /* @__PURE__ */ reactExports.forwardRef((props, ref) => {
  const {
    prefixCls: customizePrefixCls,
    length = 6,
    size: customSize,
    defaultValue,
    value,
    onChange,
    formatter,
    separator,
    variant: customizeVariant,
    disabled,
    status: customStatus,
    autoFocus,
    mask,
    type,
    autoComplete,
    onInput,
    onFocus,
    inputMode,
    classNames,
    styles,
    className,
    style,
    ...restProps
  } = props;
  const {
    classNames: contextClassNames,
    styles: contextStyles,
    getPrefixCls,
    direction,
    style: contextStyle,
    className: contextClassName
  } = useComponentConfig("otp");
  const prefixCls = getPrefixCls("otp", customizePrefixCls);
  const [variant] = useVariant("otp", customizeVariant, void 0, "input");
  const mergedProps = {
    ...props,
    length,
    variant
  };
  const contextStyleRoot = useSemanticRootStyle(contextStyle);
  const styleRoot = useSemanticRootStyle(style);
  const [mergedClassNames, mergedStyles] = useMergeSemantic([contextClassNames, classNames], [contextStyles, contextStyleRoot, styles, styleRoot], {
    props: mergedProps
  });
  const domAttrs = pickAttrs(restProps, {
    aria: true,
    data: true,
    attr: true
  });
  const [hashId, cssVarCls] = useStyle$1(prefixCls);
  const mergedSize = useSize((ctx) => customSize ?? ctx);
  const formContext = reactExports.useContext(FormItemInputContext);
  const mergedStatus = getMergedStatus(formContext.status, customStatus);
  const proxyFormContext = reactExports.useMemo(() => ({
    ...formContext,
    status: mergedStatus,
    hasFeedback: false,
    feedbackIcon: null
  }), [formContext, mergedStatus]);
  const containerRef = reactExports.useRef(null);
  const inputsRef = reactExports.useRef({});
  reactExports.useImperativeHandle(ref, () => ({
    focus: () => {
      inputsRef.current[0]?.focus();
    },
    blur: () => {
      for (let i = 0; i < length; i += 1) {
        inputsRef.current[i]?.blur();
      }
    },
    nativeElement: containerRef.current
  }));
  const internalFormatter = (txt) => formatter ? formatter(txt) : txt;
  const [valueCells, setValueCells] = reactExports.useState(() => strToArr(internalFormatter(defaultValue || "")));
  reactExports.useEffect(() => {
    if (value !== void 0) {
      setValueCells(strToArr(value));
    }
  }, [value]);
  const triggerValueCellsChange = useEvent((nextValueCells) => {
    setValueCells(nextValueCells);
    if (onInput) {
      onInput(nextValueCells);
    }
    if (onChange && nextValueCells.length === length && nextValueCells.every((c) => c) && nextValueCells.some((c, index) => valueCells[index] !== c)) {
      onChange(nextValueCells.join(""));
    }
  });
  const patchValue = useEvent((index, txt) => {
    let nextCells = _toConsumableArray(valueCells);
    for (let i = 0; i < index; i += 1) {
      if (!nextCells[i]) {
        nextCells[i] = "";
      }
    }
    if (txt.length <= 1) {
      nextCells[index] = txt;
    } else {
      nextCells = nextCells.slice(0, index).concat(strToArr(txt));
    }
    nextCells = nextCells.slice(0, length);
    for (let i = nextCells.length - 1; i >= 0; i -= 1) {
      if (nextCells[i]) {
        break;
      }
      nextCells.pop();
    }
    const formattedValue = internalFormatter(nextCells.map((c) => c || " ").join(""));
    nextCells = strToArr(formattedValue).map((c, i) => {
      if (c === " " && !nextCells[i]) {
        return nextCells[i];
      }
      return c;
    });
    return nextCells;
  });
  const onInputChange = (index, txt) => {
    const nextCells = patchValue(index, txt);
    const nextIndex = Math.min(index + txt.length, length - 1);
    if (nextIndex !== index && nextCells[index] !== void 0) {
      inputsRef.current[nextIndex]?.focus();
    }
    triggerValueCellsChange(nextCells);
  };
  const onInputActiveChange = (nextIndex) => {
    inputsRef.current[nextIndex]?.focus();
  };
  const onInputFocus = (event, index) => {
    for (let i = 0; i < index; i += 1) {
      if (!inputsRef.current[i]?.input?.value) {
        inputsRef.current[i]?.focus();
        break;
      }
    }
    onFocus?.(event);
  };
  const inputSharedProps = {
    variant,
    disabled,
    status: mergedStatus,
    mask,
    type,
    inputMode,
    autoComplete
  };
  return /* @__PURE__ */ reactExports.createElement("div", {
    ...domAttrs,
    ref: containerRef,
    className: clsx(className, prefixCls, {
      [`${prefixCls}-sm`]: mergedSize === "small",
      [`${prefixCls}-lg`]: mergedSize === "large",
      [`${prefixCls}-rtl`]: direction === "rtl"
    }, cssVarCls, hashId, contextClassName, mergedClassNames.root),
    style: mergedStyles.root,
    role: "group"
  }, /* @__PURE__ */ reactExports.createElement(FormItemInputContext.Provider, {
    value: proxyFormContext
  }, Array.from({
    length
  }).map((_, index) => {
    const key = `otp-${index}`;
    const singleValue = valueCells[index] || "";
    return /* @__PURE__ */ reactExports.createElement(reactExports.Fragment, {
      key
    }, /* @__PURE__ */ reactExports.createElement(OTPInput, {
      ref: (inputEle) => {
        inputsRef.current[index] = inputEle;
      },
      index,
      size: mergedSize,
      htmlSize: 1,
      className: clsx(mergedClassNames.input, `${prefixCls}-input`),
      style: mergedStyles.input,
      onChange: onInputChange,
      value: singleValue,
      onActiveChange: onInputActiveChange,
      autoFocus: index === 0 && autoFocus,
      onFocus: (event) => onInputFocus(event, index),
      ...inputSharedProps
    }), index < length - 1 && /* @__PURE__ */ reactExports.createElement(Separator, {
      separator,
      index,
      prefixCls,
      className: clsx(mergedClassNames.separator),
      style: mergedStyles.separator
    }));
  })));
});
var EyeInvisibleOutlined$1 = {};
var hasRequiredEyeInvisibleOutlined;
function requireEyeInvisibleOutlined() {
  if (hasRequiredEyeInvisibleOutlined) return EyeInvisibleOutlined$1;
  hasRequiredEyeInvisibleOutlined = 1;
  Object.defineProperty(EyeInvisibleOutlined$1, "__esModule", { value: true });
  var EyeInvisibleOutlined2 = { "icon": { "tag": "svg", "attrs": { "viewBox": "64 64 896 896", "focusable": "false" }, "children": [{ "tag": "path", "attrs": { "d": "M942.2 486.2Q889.47 375.11 816.7 305l-50.88 50.88C807.31 395.53 843.45 447.4 874.7 512 791.5 684.2 673.4 766 512 766q-72.67 0-133.87-22.38L323 798.75Q408 838 512 838q288.3 0 430.2-300.3a60.29 60.29 0 000-51.5zm-63.57-320.64L836 122.88a8 8 0 00-11.32 0L715.31 232.2Q624.86 186 512 186q-288.3 0-430.2 300.3a60.3 60.3 0 000 51.5q56.69 119.4 136.5 191.41L112.48 835a8 8 0 000 11.31L155.17 889a8 8 0 0011.31 0l712.15-712.12a8 8 0 000-11.32zM149.3 512C232.6 339.8 350.7 258 512 258c54.54 0 104.13 9.36 149.12 28.39l-70.3 70.3a176 176 0 00-238.13 238.13l-83.42 83.42C223.1 637.49 183.3 582.28 149.3 512zm246.7 0a112.11 112.11 0 01146.2-106.69L401.31 546.2A112 112 0 01396 512z" } }, { "tag": "path", "attrs": { "d": "M508 624c-3.46 0-6.87-.16-10.25-.47l-52.82 52.82a176.09 176.09 0 00227.42-227.42l-52.82 52.82c.31 3.38.47 6.79.47 10.25a111.94 111.94 0 01-112 112z" } }] }, "name": "eye-invisible", "theme": "outlined" };
  EyeInvisibleOutlined$1.default = EyeInvisibleOutlined2;
  return EyeInvisibleOutlined$1;
}
var EyeInvisibleOutlinedExports = /* @__PURE__ */ requireEyeInvisibleOutlined();
const EyeInvisibleOutlinedSvg = /* @__PURE__ */ getDefaultExportFromCjs(EyeInvisibleOutlinedExports);
function _extends$1() {
  _extends$1 = Object.assign ? Object.assign.bind() : function(target) {
    for (var i = 1; i < arguments.length; i++) {
      var source = arguments[i];
      for (var key in source) {
        if (Object.prototype.hasOwnProperty.call(source, key)) {
          target[key] = source[key];
        }
      }
    }
    return target;
  };
  return _extends$1.apply(this, arguments);
}
const EyeInvisibleOutlined = (props, ref) => /* @__PURE__ */ reactExports.createElement(Icon, _extends$1({}, props, {
  ref,
  icon: EyeInvisibleOutlinedSvg
}));
const RefIcon$1 = /* @__PURE__ */ reactExports.forwardRef(EyeInvisibleOutlined);
var EyeOutlined$1 = {};
var hasRequiredEyeOutlined;
function requireEyeOutlined() {
  if (hasRequiredEyeOutlined) return EyeOutlined$1;
  hasRequiredEyeOutlined = 1;
  Object.defineProperty(EyeOutlined$1, "__esModule", { value: true });
  var EyeOutlined2 = { "icon": { "tag": "svg", "attrs": { "viewBox": "64 64 896 896", "focusable": "false" }, "children": [{ "tag": "path", "attrs": { "d": "M942.2 486.2C847.4 286.5 704.1 186 512 186c-192.2 0-335.4 100.5-430.2 300.3a60.3 60.3 0 000 51.5C176.6 737.5 319.9 838 512 838c192.2 0 335.4-100.5 430.2-300.3 7.7-16.2 7.7-35 0-51.5zM512 766c-161.3 0-279.4-81.8-362.7-254C232.6 339.8 350.7 258 512 258c161.3 0 279.4 81.8 362.7 254C791.5 684.2 673.4 766 512 766zm-4-430c-97.2 0-176 78.8-176 176s78.8 176 176 176 176-78.8 176-176-78.8-176-176-176zm0 288c-61.9 0-112-50.1-112-112s50.1-112 112-112 112 50.1 112 112-50.1 112-112 112z" } }] }, "name": "eye", "theme": "outlined" };
  EyeOutlined$1.default = EyeOutlined2;
  return EyeOutlined$1;
}
var EyeOutlinedExports = /* @__PURE__ */ requireEyeOutlined();
const EyeOutlinedSvg = /* @__PURE__ */ getDefaultExportFromCjs(EyeOutlinedExports);
function _extends() {
  _extends = Object.assign ? Object.assign.bind() : function(target) {
    for (var i = 1; i < arguments.length; i++) {
      var source = arguments[i];
      for (var key in source) {
        if (Object.prototype.hasOwnProperty.call(source, key)) {
          target[key] = source[key];
        }
      }
    }
    return target;
  };
  return _extends.apply(this, arguments);
}
const EyeOutlined = (props, ref) => /* @__PURE__ */ reactExports.createElement(Icon, _extends({}, props, {
  ref,
  icon: EyeOutlinedSvg
}));
const RefIcon = /* @__PURE__ */ reactExports.forwardRef(EyeOutlined);
const defaultIconRender = (visible) => visible ? /* @__PURE__ */ reactExports.createElement(RefIcon, null) : /* @__PURE__ */ reactExports.createElement(RefIcon$1, null);
const actionMap = {
  click: "onClick",
  hover: "onMouseOver"
};
const Password = /* @__PURE__ */ reactExports.forwardRef((props, ref) => {
  const {
    disabled: customDisabled,
    action = "click",
    visibilityToggle = true,
    iconRender,
    prefixCls: customizePrefixCls,
    inputPrefixCls: customizeInputPrefixCls,
    suffix,
    className,
    style,
    classNames,
    styles,
    variant: customizeVariant,
    ...restProps
  } = props;
  const {
    getPrefixCls,
    className: contextClassName,
    style: contextStyle,
    classNames: contextClassNames,
    styles: contextStyles,
    iconRender: contextIconRender
  } = useComponentConfig("inputPassword");
  const [variant] = useVariant("inputPassword", customizeVariant, props.bordered, "input");
  const [locale] = useLocale("global");
  const disabled = reactExports.useContext(DisabledContext);
  const mergedDisabled = customDisabled ?? disabled;
  const mergedProps = {
    ...props,
    disabled: mergedDisabled,
    variant
  };
  const contextStyleRoot = useSemanticRootStyle(contextStyle);
  const styleRoot = useSemanticRootStyle(style);
  const [mergedClassNames, mergedStyles] = useMergeSemantic([contextClassNames, classNames], [contextStyles, contextStyleRoot, styles, styleRoot], {
    props: mergedProps
  });
  const visibilityControlled = isPlainObject(visibilityToggle) && visibilityToggle.visible !== void 0;
  const [visible, setVisible] = reactExports.useState(() => visibilityControlled ? visibilityToggle.visible : false);
  const inputRef = reactExports.useRef(null);
  reactExports.useEffect(() => {
    if (visibilityControlled) {
      setVisible(visibilityToggle.visible);
    }
  }, [visibilityControlled, visibilityToggle]);
  const removePasswordTimeout = useRemovePasswordTimeout(inputRef);
  const onVisibleChange = () => {
    if (mergedDisabled) {
      return;
    }
    if (visible) {
      removePasswordTimeout();
    }
    const nextVisible = !visible;
    setVisible(nextVisible);
    if (isPlainObject(visibilityToggle)) {
      visibilityToggle.onVisibleChange?.(nextVisible);
    }
  };
  const getIcon = (prefixCls2) => {
    const iconTrigger = actionMap[action] || "";
    const iconRenderer = iconRender || contextIconRender || defaultIconRender;
    const icon = iconRenderer(visible);
    const iconTabIndex = isPlainObject(visibilityToggle) ? visibilityToggle.tabIndex : void 0;
    return /* @__PURE__ */ reactExports.createElement("span", {
      key: "passwordIcon",
      role: "button",
      tabIndex: mergedDisabled ? -1 : iconTabIndex ?? 0,
      className: `${prefixCls2}-icon`,
      "aria-disabled": mergedDisabled,
      "aria-pressed": visible,
      "aria-label": visible ? locale.hide : locale.show,
      onMouseDown: (e) => {
        e.preventDefault();
      },
      onMouseUp: (e) => {
        e.preventDefault();
      },
      onKeyDown: (e) => {
        if (e.key === "Enter" || e.key === " ") {
          e.preventDefault();
          onVisibleChange();
        }
      },
      [iconTrigger]: onVisibleChange
    }, icon);
  };
  const inputPrefixCls = getPrefixCls("input", customizeInputPrefixCls);
  const prefixCls = getPrefixCls("input-password", customizePrefixCls);
  const suffixIcon = visibilityToggle && getIcon(prefixCls);
  const inputClassName = clsx(prefixCls, contextClassName, className, {
    [`${prefixCls}-${props.size}`]: !!props.size
  });
  const inputProps = {
    ...restProps,
    type: visible ? "text" : "password",
    prefixCls: inputPrefixCls,
    suffix: /* @__PURE__ */ reactExports.createElement(reactExports.Fragment, null, suffixIcon, suffix),
    disabled: mergedDisabled,
    className: inputClassName,
    classNames: mergedClassNames,
    styles: mergedStyles,
    variant
  };
  return /* @__PURE__ */ reactExports.createElement(Input$1, {
    ref: composeRef(ref, inputRef),
    ...inputProps
  });
});
const genSearchStyle = (token) => {
  const {
    componentCls,
    antCls,
    calc,
    max
  } = token;
  const btnCls = `${componentCls}-btn`;
  const [varName, varRef] = genCssVar(antCls, "input-search");
  const inputFontSizeSM = token.inputFontSizeSM ?? token.fontSize;
  const smallButtonHeight = max(token.controlHeightSM, calc(inputFontSizeSM).mul(token.lineHeight).add(calc(token.paddingBlockSM).mul(2)).add(calc(token.lineWidth).mul(2)).equal());
  return {
    [componentCls]: {
      [varName("btn-height")]: unit(token.controlHeight),
      width: "100%",
      // =========================== Button ===========================
      [btnCls]: {
        height: varRef("btn-height"),
        "&:focus-visible": {
          zIndex: 5
        },
        [`&${antCls}-btn-icon-only`]: {
          width: varRef("btn-height")
        },
        "&-filled": {
          background: token.colorFillTertiary,
          "&:not(:disabled)": {
            "&:hover": {
              background: token.colorFillSecondary
            },
            "&:active": {
              background: token.colorFill
            }
          }
        }
      },
      [`&${componentCls}-large`]: {
        [varName("btn-height")]: unit(token.controlHeightLG)
      },
      [`&${componentCls}-small`]: {
        [varName("btn-height")]: unit(token.controlHeightSM)
      },
      [`&${componentCls}-small ${btnCls}`]: {
        minHeight: smallButtonHeight,
        [`&${token.antCls}-btn-icon-only`]: {
          minWidth: smallButtonHeight
        }
      }
    }
  };
};
const useStyle = genStyleHooks(["Input", "Search"], (token) => {
  const inputToken = merge(token, initInputToken(token));
  return genSearchStyle(inputToken);
}, initComponentToken);
const Search = /* @__PURE__ */ reactExports.forwardRef((props, ref) => {
  const {
    prefixCls: customizePrefixCls,
    inputPrefixCls: customizeInputPrefixCls,
    className,
    size: customizeSize,
    style,
    enterButton = false,
    searchIcon: customizeSearchIcon,
    addonAfter,
    loading,
    disabled,
    onSearch: customOnSearch,
    onChange: customOnChange,
    onCompositionStart,
    onCompositionEnd,
    variant: customizeVariant,
    onPressEnter: customOnPressEnter,
    classNames,
    styles,
    hidden,
    ...restProps
  } = props;
  const {
    direction,
    getPrefixCls,
    className: contextClassName,
    style: contextStyle,
    classNames: contextClassNames,
    styles: contextStyles,
    searchIcon: contextSearchIcon
  } = useComponentConfig("inputSearch");
  const contextDisabled = reactExports.useContext(DisabledContext);
  const mergedDisabled = disabled ?? contextDisabled;
  const [mergedVariant, , isVariantConfigured] = useVariant("inputSearch", customizeVariant, props.bordered);
  const variant = isVariantConfigured ? mergedVariant : void 0;
  const [inputVariant] = useVariant("inputSearch", customizeVariant, props.bordered, "input");
  const mergedProps = {
    ...props,
    enterButton,
    variant
  };
  const contextStyleRoot = useSemanticRootStyle(contextStyle);
  const styleRoot = useSemanticRootStyle(style);
  const [mergedClassNames, mergedStyles] = useMergeSemantic([contextClassNames, classNames], [contextStyles, contextStyleRoot, styles, styleRoot], {
    props: mergedProps
  }, {
    button: {
      _default: "root"
    }
  });
  const composedRef = reactExports.useRef(false);
  const prefixCls = getPrefixCls("input-search", customizePrefixCls);
  const inputPrefixCls = getPrefixCls("input", customizeInputPrefixCls);
  const [hashId, cssVarCls] = useStyle(prefixCls);
  const {
    compactSize
  } = useCompactItemContext(prefixCls, direction);
  const size = useSize((ctx) => customizeSize ?? compactSize ?? ctx);
  const inputRef = reactExports.useRef(null);
  const onChange = (e) => {
    if (e?.target && e.type === "click" && customOnSearch) {
      customOnSearch(e.target.value, e, {
        source: "clear"
      });
    }
    customOnChange?.(e);
  };
  const onMouseDown = (e) => {
    if (document.activeElement === inputRef.current?.input) {
      e.preventDefault();
    }
  };
  const onSearch = (e) => {
    if (customOnSearch) {
      customOnSearch(inputRef.current?.input?.value, e, {
        source: "input"
      });
    }
  };
  const onPressEnter = (e) => {
    if (composedRef.current || loading) {
      return;
    }
    customOnPressEnter?.(e);
    onSearch(e);
  };
  const searchIcon = typeof enterButton === "boolean" ? fallbackProp(customizeSearchIcon, contextSearchIcon, /* @__PURE__ */ reactExports.createElement(RefIcon$2, null)) : null;
  const btnPrefixCls = `${prefixCls}-btn`;
  const btnClassName = clsx(btnPrefixCls, {
    [`${btnPrefixCls}-${variant}`]: variant
  });
  let button;
  const enterButtonAsElement = enterButton || {};
  const isAntdButton = enterButtonAsElement.type && enterButtonAsElement.type.__ANT_BUTTON === true;
  if (isAntdButton || enterButtonAsElement.type === "button") {
    const enterButtonProps = enterButtonAsElement.props;
    button = cloneElement(enterButtonAsElement, {
      disabled: mergedDisabled || enterButtonProps.disabled || !isAntdButton && loading,
      onMouseDown,
      onClick: (e) => {
        enterButtonAsElement?.props?.onClick?.(e);
        onSearch(e);
      },
      key: "enterButton",
      ...isAntdButton ? {
        className: clsx(btnClassName, enterButtonProps.className),
        loading: loading || enterButtonProps.loading,
        size
      } : {}
    });
  } else {
    button = /* @__PURE__ */ reactExports.createElement(Button, {
      classNames: mergedClassNames.button,
      styles: mergedStyles.button,
      className: btnClassName,
      color: enterButton ? "primary" : "default",
      size,
      disabled,
      key: "enterButton",
      onMouseDown,
      onClick: onSearch,
      loading,
      icon: searchIcon,
      variant: variant === "borderless" || variant === "filled" || variant === "underlined" ? "text" : enterButton ? "solid" : void 0
    }, enterButton);
  }
  if (addonAfter) {
    button = [button, cloneElement(addonAfter, {
      key: "addonAfter"
    })];
  }
  const mergedClassName = clsx(prefixCls, cssVarCls, {
    [`${prefixCls}-rtl`]: direction === "rtl",
    [`${prefixCls}-${size}`]: !!size,
    [`${prefixCls}-with-button`]: !!enterButton
  }, className, contextClassName, hashId, mergedClassNames.root);
  const handleOnCompositionStart = (e) => {
    composedRef.current = true;
    onCompositionStart?.(e);
  };
  const handleOnCompositionEnd = (e) => {
    composedRef.current = false;
    onCompositionEnd?.(e);
  };
  const rootProps = pickAttrs(restProps, {
    data: true
  });
  const inputProps = omit({
    ...restProps,
    classNames: omit(mergedClassNames, ["button", "root"]),
    styles: omit(mergedStyles, ["button", "root"]),
    prefixCls: inputPrefixCls,
    type: "search",
    size,
    variant: inputVariant,
    onPressEnter,
    onCompositionStart: handleOnCompositionStart,
    onCompositionEnd: handleOnCompositionEnd,
    onChange,
    disabled
  }, Object.keys(rootProps));
  return /* @__PURE__ */ reactExports.createElement(Compact, {
    className: mergedClassName,
    style: mergedStyles.root,
    ...rootProps,
    hidden
  }, /* @__PURE__ */ reactExports.createElement(Input$1, {
    ref: composeRef(inputRef, ref),
    ...inputProps
  }), button);
});
const Input = Input$1;
Input.Group = Group;
Input.Search = Search;
Input.TextArea = TextArea;
Input.Password = Password;
Input.OTP = OTP;
export {
  Input as I
};
