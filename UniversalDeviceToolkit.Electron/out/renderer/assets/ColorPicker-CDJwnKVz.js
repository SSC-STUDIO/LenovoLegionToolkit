import { r as reactExports, $ as React, f as clsx, K as useEvent, aC as calculateColor, aD as calcOffset, aE as useControlledState, aF as generateColor, aG as Color, aH as ColorPickerPrefixCls, aI as defaultColor, v as genStyleHooks, w as merge, n as unit, A as resetComponent, p as genFocusOutline, J as useComponentConfig, af as composeRef, aJ as FormItemInputContext, aK as DisabledContext, L as useSemanticRootStyle, N as useMergeSemantic, as as isReactRenderable, Q as useCSSVarCls, e as ConfigContext, a4 as useId, ah as isNumber, aL as useSize, aM as useOrientation, a2 as pickAttrs, aN as useLayoutEffect, a3 as CSSMotion, h as omit, aO as _extends$5, q as textEllipsis, ac as genFocusStyle, Z as isPlainObject, m as Tooltip, aP as generateColor$1, aA as getDefaultExportFromCjs, aB as Icon, aQ as _slicedToArray, aR as _createClass, aS as _classCallCheck, aT as _defineProperty, aU as warningOnce, aV as wrapperRaf, aW as triggerFocus, aX as useLayoutUpdateEffect, B as FastColor, aY as initComponentToken, aZ as initInputToken, a_ as genCompactItemStyle, a$ as genCssVar, b0 as genPlaceholderStyle, b1 as genBorderlessStyle, b2 as genUnderlinedStyle, b3 as genFilledStyle, b4 as genOutlinedStyle, b5 as genBasicInputStyle, E as resetIcon, b6 as Compact, b7 as useCompactItemContext, b8 as useVariant, b9 as getStatusClassNames, ba as getMergedStatus, ai as ContextIsolator, bb as RefIcon$1, bc as RefIcon$2, at as ConfigProvider, bd as getColorAlpha, be as toHexFormat, bf as getRoundNumber, bg as KeyCode, bh as reactDomExports, bi as isEqual, bj as useDelayState, bk as getGradientPercentColor, _ as _toConsumableArray, bl as AggregationColor, bm as useForceUpdate, bn as genAlphaColor, l as isFunction, a8 as useLocale, bo as useMergedArrow } from "./index-3RTipSd5.js";
import { u as useBubbleLock, C as Checkbox, D as Divider } from "./index-QUbxwEY1.js";
import { a as ColorBlock, W as Wave, T as TARGET_CLS, R as RefIcon$3, S as Select, b as ColorPresets, c as genPurePanel } from "./index-BxBscas6.js";
import { P as Popover } from "./index-Hdt_DTHG.js";
import { S as SpaceAddon } from "./Addon-CECo-qGW.js";
import { I as Input } from "./Input-mSSMIOSE.js";
function proxyObject(obj, extendProps) {
  if (typeof Proxy !== "undefined" && obj) {
    return new Proxy(obj, {
      get(target, prop) {
        if (extendProps[prop]) {
          return extendProps[prop];
        }
        const originProp = target[prop];
        return typeof originProp === "function" ? originProp.bind(target) : originProp;
      }
    });
  }
  return obj;
}
function getPosition$1(e) {
  const obj = "touches" in e ? e.touches[0] : e;
  const scrollXOffset = document.documentElement.scrollLeft || document.body.scrollLeft || window.pageXOffset;
  const scrollYOffset = document.documentElement.scrollTop || document.body.scrollTop || window.pageYOffset;
  return {
    pageX: obj.pageX - scrollXOffset,
    pageY: obj.pageY - scrollYOffset
  };
}
function useColorDrag(props) {
  const {
    targetRef,
    containerRef,
    direction,
    onDragChange,
    onDragChangeComplete,
    calculate,
    color,
    disabledDrag
  } = props;
  const [offsetValue, setOffsetValue] = reactExports.useState({
    x: 0,
    y: 0
  });
  const mouseMoveRef = reactExports.useRef(null);
  const mouseUpRef = reactExports.useRef(null);
  reactExports.useEffect(() => {
    setOffsetValue(calculate());
  }, [color]);
  reactExports.useEffect(() => () => {
    document.removeEventListener("mousemove", mouseMoveRef.current);
    document.removeEventListener("mouseup", mouseUpRef.current);
    document.removeEventListener("touchmove", mouseMoveRef.current);
    document.removeEventListener("touchend", mouseUpRef.current);
    mouseMoveRef.current = null;
    mouseUpRef.current = null;
  }, []);
  const updateOffset = (e) => {
    const {
      pageX,
      pageY
    } = getPosition$1(e);
    const {
      x: rectX,
      y: rectY,
      width,
      height
    } = containerRef.current.getBoundingClientRect();
    const {
      width: targetWidth,
      height: targetHeight
    } = targetRef.current.getBoundingClientRect();
    const centerOffsetX = targetWidth / 2;
    const centerOffsetY = targetHeight / 2;
    const offsetX = Math.max(0, Math.min(pageX - rectX, width)) - centerOffsetX;
    const offsetY = Math.max(0, Math.min(pageY - rectY, height)) - centerOffsetY;
    const calcOffset2 = {
      x: offsetX,
      y: direction === "x" ? offsetValue.y : offsetY
    };
    if (targetWidth === 0 && targetHeight === 0 || targetWidth !== targetHeight) {
      return false;
    }
    onDragChange?.(calcOffset2);
  };
  const onDragMove = (e) => {
    e.preventDefault();
    updateOffset(e);
  };
  const onDragStop = (e) => {
    e.preventDefault();
    document.removeEventListener("mousemove", mouseMoveRef.current);
    document.removeEventListener("mouseup", mouseUpRef.current);
    document.removeEventListener("touchmove", mouseMoveRef.current);
    document.removeEventListener("touchend", mouseUpRef.current);
    mouseMoveRef.current = null;
    mouseUpRef.current = null;
    onDragChangeComplete?.();
  };
  const onDragStart = (e) => {
    document.removeEventListener("mousemove", mouseMoveRef.current);
    document.removeEventListener("mouseup", mouseUpRef.current);
    if (disabledDrag) {
      return;
    }
    updateOffset(e);
    document.addEventListener("mousemove", onDragMove);
    document.addEventListener("mouseup", onDragStop);
    document.addEventListener("touchmove", onDragMove);
    document.addEventListener("touchend", onDragStop);
    mouseMoveRef.current = onDragMove;
    mouseUpRef.current = onDragStop;
  };
  return [offsetValue, onDragStart];
}
const Handler = ({
  size = "default",
  color,
  prefixCls
}) => {
  return /* @__PURE__ */ React.createElement("div", {
    className: clsx(`${prefixCls}-handler`, {
      [`${prefixCls}-handler-sm`]: size === "small"
    }),
    style: {
      backgroundColor: color
    }
  });
};
const Palette = ({
  children,
  style,
  prefixCls
}) => {
  return /* @__PURE__ */ React.createElement("div", {
    className: `${prefixCls}-palette`,
    style: {
      position: "relative",
      ...style
    }
  }, children);
};
const Transform = /* @__PURE__ */ reactExports.forwardRef((props, ref) => {
  const {
    children,
    x,
    y
  } = props;
  return /* @__PURE__ */ React.createElement("div", {
    ref,
    style: {
      position: "absolute",
      left: `${x}%`,
      top: `${y}%`,
      zIndex: 1,
      transform: "translate(-50%, -50%)"
    }
  }, children);
});
const Picker = ({
  color,
  onChange,
  prefixCls,
  onChangeComplete,
  disabled
}) => {
  const pickerRef = reactExports.useRef();
  const transformRef = reactExports.useRef();
  const colorRef = reactExports.useRef(color);
  const onDragChange = useEvent((offsetValue) => {
    const calcColor = calculateColor({
      offset: offsetValue,
      targetRef: transformRef,
      containerRef: pickerRef,
      color
    });
    colorRef.current = calcColor;
    onChange(calcColor);
  });
  const [offset, dragStartHandle] = useColorDrag({
    color,
    containerRef: pickerRef,
    targetRef: transformRef,
    calculate: () => calcOffset(color),
    onDragChange,
    onDragChangeComplete: () => onChangeComplete?.(colorRef.current),
    disabledDrag: disabled
  });
  return /* @__PURE__ */ React.createElement("div", {
    ref: pickerRef,
    className: `${prefixCls}-select`,
    onMouseDown: dragStartHandle,
    onTouchStart: dragStartHandle
  }, /* @__PURE__ */ React.createElement(Palette, {
    prefixCls
  }, /* @__PURE__ */ React.createElement(Transform, {
    x: offset.x,
    y: offset.y,
    ref: transformRef
  }, /* @__PURE__ */ React.createElement(Handler, {
    color: color.toRgbString(),
    prefixCls
  })), /* @__PURE__ */ React.createElement("div", {
    className: `${prefixCls}-saturation`,
    style: {
      backgroundColor: `hsl(${color.toHsb().h},100%, 50%)`,
      backgroundImage: "linear-gradient(0deg, #000, transparent),linear-gradient(90deg, #fff, hsla(0, 0%, 100%, 0))"
    }
  })));
};
const useColorState = (defaultValue, value) => {
  const [mergedValue, setValue] = useControlledState(defaultValue, value);
  const color = reactExports.useMemo(() => generateColor(mergedValue), [mergedValue]);
  return [color, setValue];
};
const Gradient = ({
  colors,
  children,
  direction = "to right",
  type,
  prefixCls
}) => {
  const gradientColors = reactExports.useMemo(() => colors.map((color, idx) => {
    let result = generateColor(color);
    if (type === "alpha" && idx === colors.length - 1) {
      result = new Color(result.setA(1));
    }
    return result.toRgbString();
  }).join(","), [colors, type]);
  return /* @__PURE__ */ React.createElement("div", {
    className: `${prefixCls}-gradient`,
    style: {
      position: "absolute",
      inset: 0,
      background: `linear-gradient(${direction}, ${gradientColors})`
    }
  }, children);
};
const Slider$2 = (props) => {
  const {
    prefixCls,
    colors,
    disabled,
    onChange,
    onChangeComplete,
    color,
    type
  } = props;
  const sliderRef = reactExports.useRef(null);
  const transformRef = reactExports.useRef(null);
  const colorRef = reactExports.useRef(color);
  const getValue = (c) => {
    return type === "hue" ? c.getHue() : c.a * 100;
  };
  const onDragChange = useEvent((offsetValue) => {
    const calcColor = calculateColor({
      offset: offsetValue,
      targetRef: transformRef,
      containerRef: sliderRef,
      color,
      type
    });
    colorRef.current = calcColor;
    onChange(getValue(calcColor));
  });
  const [offset, dragStartHandle] = useColorDrag({
    color,
    targetRef: transformRef,
    containerRef: sliderRef,
    calculate: () => calcOffset(color, type),
    onDragChange,
    onDragChangeComplete() {
      onChangeComplete(getValue(colorRef.current));
    },
    direction: "x",
    disabledDrag: disabled
  });
  const handleColor = React.useMemo(() => {
    if (type === "hue") {
      const hsb = color.toHsb();
      hsb.s = 1;
      hsb.b = 1;
      hsb.a = 1;
      const lightColor = new Color(hsb);
      return lightColor;
    }
    return color;
  }, [color, type]);
  const gradientList = React.useMemo(() => colors.map((info) => `${info.color} ${info.percent}%`), [colors]);
  return /* @__PURE__ */ React.createElement("div", {
    ref: sliderRef,
    className: clsx(`${prefixCls}-slider`, `${prefixCls}-slider-${type}`),
    onMouseDown: dragStartHandle,
    onTouchStart: dragStartHandle
  }, /* @__PURE__ */ React.createElement(Palette, {
    prefixCls
  }, /* @__PURE__ */ React.createElement(Transform, {
    x: offset.x,
    y: offset.y,
    ref: transformRef
  }, /* @__PURE__ */ React.createElement(Handler, {
    size: "small",
    color: handleColor.toHexString(),
    prefixCls
  })), /* @__PURE__ */ React.createElement(Gradient, {
    colors: gradientList,
    type,
    prefixCls
  })));
};
function useComponent(components2) {
  return reactExports.useMemo(() => {
    const {
      slider
    } = components2 || {};
    return [slider || Slider$2];
  }, [components2]);
}
function _extends$4() {
  _extends$4 = Object.assign ? Object.assign.bind() : function(target) {
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
  return _extends$4.apply(this, arguments);
}
const HUE_COLORS = [{
  color: "rgb(255, 0, 0)",
  percent: 0
}, {
  color: "rgb(255, 255, 0)",
  percent: 17
}, {
  color: "rgb(0, 255, 0)",
  percent: 33
}, {
  color: "rgb(0, 255, 255)",
  percent: 50
}, {
  color: "rgb(0, 0, 255)",
  percent: 67
}, {
  color: "rgb(255, 0, 255)",
  percent: 83
}, {
  color: "rgb(255, 0, 0)",
  percent: 100
}];
const ColorPicker$1 = /* @__PURE__ */ reactExports.forwardRef((props, ref) => {
  const {
    value,
    defaultValue,
    prefixCls = ColorPickerPrefixCls,
    onChange,
    onChangeComplete,
    className,
    style,
    panelRender,
    disabledAlpha = false,
    disabled = false,
    components: components2
  } = props;
  const [Slider2] = useComponent(components2);
  const [colorValue, setColorValue] = useColorState(defaultValue || defaultColor, value);
  const alphaColor = reactExports.useMemo(() => colorValue.setA(1).toRgbString(), [colorValue]);
  const handleChange = (data, type) => {
    if (!value) {
      setColorValue(data);
    }
    onChange?.(data, type);
  };
  const getHueColor = (hue) => new Color(colorValue.setHue(hue));
  const getAlphaColor = (alpha) => new Color(colorValue.setA(alpha / 100));
  const onHueChange = (hue) => {
    handleChange(getHueColor(hue), {
      type: "hue",
      value: hue
    });
  };
  const onAlphaChange = (alpha) => {
    handleChange(getAlphaColor(alpha), {
      type: "alpha",
      value: alpha
    });
  };
  const onHueChangeComplete = (hue) => {
    if (onChangeComplete) {
      onChangeComplete(getHueColor(hue));
    }
  };
  const onAlphaChangeComplete = (alpha) => {
    if (onChangeComplete) {
      onChangeComplete(getAlphaColor(alpha));
    }
  };
  const mergeCls = clsx(`${prefixCls}-panel`, className, {
    [`${prefixCls}-panel-disabled`]: disabled
  });
  const sharedSliderProps = {
    prefixCls,
    disabled,
    color: colorValue
  };
  const defaultPanel = /* @__PURE__ */ React.createElement(React.Fragment, null, /* @__PURE__ */ React.createElement(Picker, _extends$4({
    onChange: handleChange
  }, sharedSliderProps, {
    onChangeComplete
  })), /* @__PURE__ */ React.createElement("div", {
    className: `${prefixCls}-slider-container`
  }, /* @__PURE__ */ React.createElement("div", {
    className: clsx(`${prefixCls}-slider-group`, {
      [`${prefixCls}-slider-group-disabled-alpha`]: disabledAlpha
    })
  }, /* @__PURE__ */ React.createElement(Slider2, _extends$4({}, sharedSliderProps, {
    type: "hue",
    colors: HUE_COLORS,
    min: 0,
    max: 359,
    value: colorValue.getHue(),
    onChange: onHueChange,
    onChangeComplete: onHueChangeComplete
  })), !disabledAlpha && /* @__PURE__ */ React.createElement(Slider2, _extends$4({}, sharedSliderProps, {
    type: "alpha",
    colors: [{
      percent: 0,
      color: "rgba(255, 0, 4, 0)"
    }, {
      percent: 100,
      color: alphaColor
    }],
    min: 0,
    max: 100,
    value: colorValue.a * 100,
    onChange: onAlphaChange,
    onChangeComplete: onAlphaChangeComplete
  }))), /* @__PURE__ */ React.createElement(ColorBlock, {
    color: colorValue.toRgbString(),
    prefixCls
  })));
  return /* @__PURE__ */ React.createElement("div", {
    className: mergeCls,
    style,
    ref
  }, typeof panelRender === "function" ? panelRender(defaultPanel) : defaultPanel);
});
function toArray(candidate) {
  if (candidate === void 0 || candidate === false) {
    return [];
  }
  return Array.isArray(candidate) ? candidate : [candidate];
}
function toNamePathStr(name) {
  const namePath = toArray(name);
  return namePath.join("_");
}
const RadioGroupContext = /* @__PURE__ */ reactExports.createContext(void 0);
const RadioGroupContextProvider = RadioGroupContext.Provider;
const RadioOptionTypeContext = /* @__PURE__ */ reactExports.createContext(void 0);
const RadioOptionTypeContextProvider = RadioOptionTypeContext.Provider;
const getGroupRadioStyle = (token) => {
  const {
    componentCls,
    antCls,
    lineWidth,
    borderRadius,
    borderRadiusLG,
    borderRadiusSM,
    calc
  } = token;
  const groupPrefixCls = `${componentCls}-group`;
  const buttonWrapperCls = `${componentCls}-button-wrapper`;
  const badgeCls = `${antCls}-badge`;
  const genVerticalBadgeButtonStyle = (radius) => ({
    [`> ${badgeCls}`]: {
      width: "auto"
    },
    [`> ${badgeCls} > ${buttonWrapperCls}`]: {
      width: "100%"
    },
    [`> ${badgeCls}:not(:last-child)`]: {
      marginBlockEnd: calc(lineWidth).mul(-1).equal()
    },
    [`> ${badgeCls} > ${buttonWrapperCls}:not(:last-child)`]: {
      marginBlockEnd: 0
    },
    [`> ${badgeCls}:first-child > ${buttonWrapperCls}`]: {
      borderStartStartRadius: radius,
      borderStartEndRadius: radius,
      borderEndStartRadius: 0,
      borderEndEndRadius: 0
    },
    [`> ${badgeCls}:last-child > ${buttonWrapperCls}`]: {
      borderStartStartRadius: 0,
      borderStartEndRadius: 0,
      borderEndStartRadius: radius,
      borderEndEndRadius: radius
    },
    [`> ${badgeCls}:not(:first-child):not(:last-child) > ${buttonWrapperCls}`]: {
      borderRadius: 0
    },
    [`> ${badgeCls}:first-child:last-child > ${buttonWrapperCls}`]: {
      borderRadius: radius
    }
  });
  return {
    [groupPrefixCls]: {
      ...resetComponent(token),
      display: "inline-block",
      fontSize: 0,
      // RTL
      [`&${groupPrefixCls}-rtl`]: {
        direction: "rtl"
      },
      [`&${groupPrefixCls}-block`]: {
        display: "flex"
      },
      [`${antCls}-badge ${antCls}-badge-count`]: {
        zIndex: 1
      },
      [`> ${antCls}-badge:not(:first-child) > ${antCls}-button-wrapper`]: {
        borderInlineStart: "none"
      },
      "&-vertical": {
        display: "flex",
        flexDirection: "column",
        rowGap: token.marginXS,
        [`&:has(> ${buttonWrapperCls}, > ${badgeCls} > ${buttonWrapperCls})`]: {
          rowGap: 0
        },
        [`${componentCls}-wrapper`]: {
          marginInlineEnd: 0
        },
        ...genVerticalBadgeButtonStyle(borderRadius),
        [`&${groupPrefixCls}-large`]: {
          ...genVerticalBadgeButtonStyle(borderRadiusLG)
        },
        [`&${groupPrefixCls}-small`]: {
          ...genVerticalBadgeButtonStyle(borderRadiusSM)
        }
      }
    }
  };
};
const getRadioBasicStyle = (token) => {
  const {
    componentCls,
    wrapperMarginInlineEnd,
    colorPrimary,
    colorPrimaryHover,
    radioSize,
    motionDurationSlow,
    motionDurationMid,
    motionEaseInOutCirc,
    colorBgContainer,
    colorBorder,
    lineWidth,
    colorBgContainerDisabled,
    colorTextDisabled,
    paddingXS,
    dotColorDisabled,
    dotSize,
    lineType,
    radioColor,
    radioBgColor
  } = token;
  return {
    [`${componentCls}-wrapper`]: {
      ...resetComponent(token),
      display: "inline-flex",
      alignItems: "baseline",
      marginInlineStart: 0,
      marginInlineEnd: wrapperMarginInlineEnd,
      cursor: "pointer",
      "&:last-child": {
        marginInlineEnd: 0
      },
      // RTL
      [`&${componentCls}-wrapper-rtl`]: {
        direction: "rtl"
      },
      "&-disabled": {
        cursor: "not-allowed",
        color: token.colorTextDisabled
      },
      "&::after": {
        display: "inline-block",
        width: 0,
        overflow: "hidden",
        content: '"\\a0"'
      },
      "&-block": {
        flex: 1,
        justifyContent: "center"
      },
      // ===================== Radio =====================
      [componentCls]: {
        ...resetComponent(token),
        position: "relative",
        whiteSpace: "nowrap",
        lineHeight: 1,
        cursor: "pointer",
        alignSelf: "center",
        // Styles moved from inner
        boxSizing: "border-box",
        display: "block",
        width: `calc(${radioSize} * 1px)`,
        height: `calc(${radioSize} * 1px)`,
        backgroundColor: colorBgContainer,
        border: `${unit(lineWidth)} ${lineType} ${colorBorder}`,
        borderRadius: "50%",
        transition: `all ${motionDurationMid}`,
        flex: "none",
        // Dot
        "&:after": {
          content: '""',
          position: "absolute",
          top: "50%",
          left: "50%",
          transform: "translate(-50%, -50%) scale(0)",
          width: `calc(${dotSize} * 1px)`,
          height: `calc(${dotSize} * 1px)`,
          backgroundColor: radioColor,
          borderRadius: "50%",
          transformOrigin: "50% 50%",
          opacity: 0,
          transition: `all ${motionDurationSlow} ${motionEaseInOutCirc}`
        },
        // Wrapper > Radio > input
        [`${componentCls}-input`]: {
          position: "absolute",
          inset: 0,
          zIndex: 1,
          cursor: "pointer",
          opacity: 0,
          margin: 0
        },
        // Focus outline on radio when input is focus-visible
        [`&:has(${componentCls}-input:focus-visible)`]: genFocusOutline(token)
      },
      // ===================== Hover =====================
      [`&:hover:not(${componentCls}-wrapper-disabled) ${componentCls}`]: {
        borderColor: colorPrimary
      },
      [`&:hover ${componentCls}-checked:not(${componentCls}-disabled)`]: {
        backgroundColor: colorPrimaryHover,
        borderColor: "transparent"
      },
      // ==================== Checked ====================
      [`${componentCls}-checked`]: {
        backgroundColor: radioBgColor,
        borderColor: colorPrimary,
        "&::after": {
          transform: `translate(-50%, -50%)`,
          opacity: 1
        }
      },
      // ==================== Disable ====================
      [`${componentCls}-disabled`]: {
        // Wrapper > Radio > input
        [`&, ${componentCls}-input`]: {
          cursor: "not-allowed",
          // Disabled for native input to enable Tooltip event handler
          pointerEvents: "none"
        },
        // Disabled radio styles
        background: colorBgContainerDisabled,
        borderColor: colorBorder,
        "&::after": {
          backgroundColor: dotColorDisabled
        }
      },
      [`${componentCls}-disabled + span`]: {
        color: colorTextDisabled,
        cursor: "not-allowed"
      },
      [`span${componentCls} + *`]: {
        paddingInlineStart: paddingXS,
        paddingInlineEnd: paddingXS
      }
    }
  };
};
const getRadioButtonStyle = (token) => {
  const {
    buttonColor,
    controlHeight,
    componentCls,
    lineWidth,
    lineType,
    colorBorder,
    motionDurationMid,
    buttonPaddingInline,
    fontSize,
    buttonBg,
    fontSizeLG,
    controlHeightLG,
    controlHeightSM,
    paddingXS,
    borderRadius,
    borderRadiusSM,
    borderRadiusLG,
    buttonCheckedBg,
    buttonSolidCheckedColor,
    colorTextDisabled,
    colorBgContainerDisabled,
    buttonCheckedBgDisabled,
    buttonCheckedColorDisabled,
    colorPrimary,
    colorPrimaryHover,
    colorPrimaryActive,
    buttonSolidCheckedBg,
    buttonSolidCheckedHoverBg,
    buttonSolidCheckedActiveBg,
    calc
  } = token;
  return {
    [`${componentCls}-button-wrapper`]: {
      position: "relative",
      display: "inline-block",
      height: controlHeight,
      margin: 0,
      paddingInline: buttonPaddingInline,
      paddingBlock: 0,
      color: buttonColor,
      fontSize,
      lineHeight: unit(calc(controlHeight).sub(calc(lineWidth).mul(2)).equal()),
      background: buttonBg,
      border: `${unit(lineWidth)} ${lineType} ${colorBorder}`,
      // strange align fix for chrome but works
      // https://gw.alipayobjects.com/zos/rmsportal/VFTfKXJuogBAXcvfAUWJ.gif
      borderBlockStartWidth: calc(lineWidth).add(0.02).equal(),
      borderInlineEndWidth: lineWidth,
      cursor: "pointer",
      transition: [`color`, `background-color`, `box-shadow`].map((prop) => `${prop} ${motionDurationMid}`).join(","),
      a: {
        color: buttonColor
      },
      [`> ${componentCls}-button`]: {
        position: "absolute",
        insetBlockStart: 0,
        insetInlineStart: 0,
        zIndex: -1,
        width: "100%",
        height: "100%"
      },
      "&:not(:last-child)": {
        marginInlineEnd: calc(lineWidth).mul(-1).equal()
      },
      "&:first-child": {
        borderInlineStart: `${unit(lineWidth)} ${lineType} ${colorBorder}`,
        borderStartStartRadius: borderRadius,
        borderEndStartRadius: borderRadius
      },
      "&:last-child": {
        borderStartEndRadius: borderRadius,
        borderEndEndRadius: borderRadius
      },
      "&:first-child:last-child": {
        borderRadius
      },
      [`${componentCls}-group-large &`]: {
        height: controlHeightLG,
        fontSize: fontSizeLG,
        lineHeight: unit(calc(controlHeightLG).sub(calc(lineWidth).mul(2)).equal()),
        "&:first-child": {
          borderStartStartRadius: borderRadiusLG,
          borderEndStartRadius: borderRadiusLG
        },
        "&:last-child": {
          borderStartEndRadius: borderRadiusLG,
          borderEndEndRadius: borderRadiusLG
        }
      },
      [`${componentCls}-group-small &`]: {
        height: controlHeightSM,
        paddingInline: calc(paddingXS).sub(lineWidth).equal(),
        paddingBlock: 0,
        lineHeight: unit(calc(controlHeightSM).sub(calc(lineWidth).mul(2)).equal()),
        "&:first-child": {
          borderStartStartRadius: borderRadiusSM,
          borderEndStartRadius: borderRadiusSM
        },
        "&:last-child": {
          borderStartEndRadius: borderRadiusSM,
          borderEndEndRadius: borderRadiusSM
        }
      },
      [`${componentCls}-group-vertical > &`]: {
        marginInlineEnd: 0,
        borderRadius: 0,
        "&:not(:last-child)": {
          marginBlockEnd: calc(lineWidth).mul(-1).equal()
        },
        "&:first-child": {
          borderStartStartRadius: borderRadius,
          borderStartEndRadius: borderRadius,
          borderEndStartRadius: 0,
          borderEndEndRadius: 0
        },
        "&:last-child": {
          borderStartStartRadius: 0,
          borderStartEndRadius: 0,
          borderEndStartRadius: borderRadius,
          borderEndEndRadius: borderRadius
        },
        "&:first-child:last-child": {
          borderRadius
        }
      },
      [`${componentCls}-group-vertical${componentCls}-group-large > &`]: {
        "&:first-child": {
          borderStartStartRadius: borderRadiusLG,
          borderStartEndRadius: borderRadiusLG
        },
        "&:last-child": {
          borderEndStartRadius: borderRadiusLG,
          borderEndEndRadius: borderRadiusLG
        },
        "&:first-child:last-child": {
          borderRadius: borderRadiusLG
        }
      },
      [`${componentCls}-group-vertical${componentCls}-group-small > &`]: {
        "&:first-child": {
          borderStartStartRadius: borderRadiusSM,
          borderStartEndRadius: borderRadiusSM
        },
        "&:last-child": {
          borderEndStartRadius: borderRadiusSM,
          borderEndEndRadius: borderRadiusSM
        },
        "&:first-child:last-child": {
          borderRadius: borderRadiusSM
        }
      },
      "&:hover": {
        position: "relative",
        color: colorPrimary
      },
      "&:has(:focus-visible)": genFocusOutline(token),
      [`${componentCls}, input[type='checkbox'], input[type='radio']`]: {
        width: 0,
        height: 0,
        opacity: 0,
        pointerEvents: "none"
      },
      [`&-checked:not(${componentCls}-button-wrapper-disabled)`]: {
        zIndex: 1,
        color: colorPrimary,
        background: buttonCheckedBg,
        borderColor: colorPrimary,
        "&::before": {
          backgroundColor: colorPrimary
        },
        "&:first-child": {
          borderColor: colorPrimary
        },
        "&:hover": {
          color: colorPrimaryHover,
          borderColor: colorPrimaryHover,
          "&::before": {
            backgroundColor: colorPrimaryHover
          }
        },
        "&:active": {
          color: colorPrimaryActive,
          borderColor: colorPrimaryActive,
          "&::before": {
            backgroundColor: colorPrimaryActive
          }
        }
      },
      [`${componentCls}-group-solid &-checked:not(${componentCls}-button-wrapper-disabled)`]: {
        color: buttonSolidCheckedColor,
        background: buttonSolidCheckedBg,
        borderColor: buttonSolidCheckedBg,
        "&:hover": {
          color: buttonSolidCheckedColor,
          background: buttonSolidCheckedHoverBg,
          borderColor: buttonSolidCheckedHoverBg
        },
        "&:active": {
          color: buttonSolidCheckedColor,
          background: buttonSolidCheckedActiveBg,
          borderColor: buttonSolidCheckedActiveBg
        }
      },
      "&-disabled": {
        color: colorTextDisabled,
        backgroundColor: colorBgContainerDisabled,
        borderColor: colorBorder,
        cursor: "not-allowed",
        "&:first-child, &:hover": {
          color: colorTextDisabled,
          backgroundColor: colorBgContainerDisabled,
          borderColor: colorBorder
        }
      },
      [`&-disabled${componentCls}-button-wrapper-checked`]: {
        color: buttonCheckedColorDisabled,
        backgroundColor: buttonCheckedBgDisabled,
        borderColor: colorBorder,
        boxShadow: "none"
      },
      "&-block": {
        flex: 1,
        textAlign: "center"
      }
    }
  };
};
const prepareComponentToken$3 = (token) => {
  const {
    wireframe,
    padding,
    marginXS,
    lineWidth,
    fontSizeLG,
    colorText,
    colorBgContainer,
    colorTextDisabled,
    controlItemBgActiveDisabled,
    colorTextLightSolid,
    colorPrimary,
    colorPrimaryHover,
    colorPrimaryActive,
    colorWhite
  } = token;
  const dotPadding = 4;
  const radioSize = fontSizeLG;
  const radioDotSize = wireframe ? radioSize - dotPadding * 2 : radioSize - (dotPadding + lineWidth) * 2;
  return {
    // Radio
    radioSize,
    dotSize: radioDotSize,
    dotColorDisabled: colorTextDisabled,
    // Radio buttons
    buttonSolidCheckedColor: colorTextLightSolid,
    buttonSolidCheckedBg: colorPrimary,
    buttonSolidCheckedHoverBg: colorPrimaryHover,
    buttonSolidCheckedActiveBg: colorPrimaryActive,
    buttonBg: colorBgContainer,
    buttonCheckedBg: colorBgContainer,
    buttonColor: colorText,
    buttonCheckedBgDisabled: controlItemBgActiveDisabled,
    buttonCheckedColorDisabled: colorTextDisabled,
    buttonPaddingInline: padding - lineWidth,
    wrapperMarginInlineEnd: marginXS,
    // internal
    radioColor: wireframe ? colorPrimary : colorWhite,
    radioBgColor: wireframe ? colorBgContainer : colorPrimary
  };
};
const useStyle$4 = genStyleHooks("Radio", (token) => {
  const {
    controlOutline,
    controlOutlineWidth
  } = token;
  const radioFocusShadow = `0 0 0 ${unit(controlOutlineWidth)} ${controlOutline}`;
  const radioButtonFocusShadow = radioFocusShadow;
  const radioToken = merge(token, {
    radioFocusShadow,
    radioButtonFocusShadow
  });
  return [getGroupRadioStyle(radioToken), getRadioBasicStyle(radioToken), getRadioButtonStyle(radioToken)];
}, prepareComponentToken$3, {
  unitless: {
    radioSize: true,
    dotSize: true
  }
});
const InternalRadio = (props, ref) => {
  const groupContext = reactExports.useContext(RadioGroupContext);
  const radioOptionTypeContext = reactExports.useContext(RadioOptionTypeContext);
  const {
    getPrefixCls,
    direction,
    className: contextClassName,
    style: contextStyle,
    classNames: contextClassNames,
    styles: contextStyles
  } = useComponentConfig("radio");
  const innerRef = reactExports.useRef(null);
  const mergedRef = composeRef(ref, innerRef);
  const {
    isFormItemInput
  } = reactExports.useContext(FormItemInputContext);
  const onChange = (e) => {
    props.onChange?.(e);
    groupContext?.onChange?.(e);
  };
  const {
    prefixCls: customizePrefixCls,
    className,
    rootClassName,
    children,
    style,
    title,
    classNames,
    styles,
    checked,
    ...restProps
  } = props;
  const radioPrefixCls = getPrefixCls("radio", customizePrefixCls);
  const isButtonType = (groupContext?.optionType || radioOptionTypeContext) === "button";
  const prefixCls = isButtonType ? `${radioPrefixCls}-button` : radioPrefixCls;
  const rootCls = useCSSVarCls(radioPrefixCls);
  const [hashId, cssVarCls] = useStyle$4(radioPrefixCls, rootCls);
  const radioProps = {
    ...restProps
  };
  const disabled = reactExports.useContext(DisabledContext);
  const hasChecked = "checked" in props;
  let mergedChecked = checked;
  if (groupContext) {
    radioProps.name = groupContext.name;
    radioProps.onChange = onChange;
    mergedChecked = props.value === groupContext.value;
    radioProps.disabled = radioProps.disabled ?? groupContext.disabled;
  }
  if (hasChecked || groupContext) {
    radioProps.checked = mergedChecked;
  }
  radioProps.disabled = radioProps.disabled ?? disabled;
  const mergedProps = {
    ...props,
    ...radioProps,
    checked: mergedChecked
  };
  const contextStyleRoot = useSemanticRootStyle(contextStyle);
  const styleRoot = useSemanticRootStyle(style);
  const [mergedClassNames, mergedStyles] = useMergeSemantic([contextClassNames, classNames], [contextStyles, contextStyleRoot, styles, styleRoot], {
    props: mergedProps
  });
  const wrapperClassString = clsx(`${prefixCls}-wrapper`, {
    [`${prefixCls}-wrapper-checked`]: mergedChecked,
    [`${prefixCls}-wrapper-disabled`]: radioProps.disabled,
    [`${prefixCls}-wrapper-rtl`]: direction === "rtl",
    [`${prefixCls}-wrapper-in-form-item`]: isFormItemInput,
    [`${prefixCls}-wrapper-block`]: !!groupContext?.block
  }, contextClassName, className, rootClassName, mergedClassNames.root, hashId, cssVarCls, rootCls);
  const [onLabelClick, onInputClick] = useBubbleLock(radioProps.onClick);
  return /* @__PURE__ */ reactExports.createElement(Wave, {
    component: "Radio",
    disabled: radioProps.disabled
  }, /* @__PURE__ */ reactExports.createElement("label", {
    className: wrapperClassString,
    style: mergedStyles.root,
    onMouseEnter: props.onMouseEnter,
    onMouseLeave: props.onMouseLeave,
    title,
    onClick: onLabelClick
  }, /* @__PURE__ */ reactExports.createElement(Checkbox, {
    ...radioProps,
    className: clsx(mergedClassNames.icon, {
      [TARGET_CLS]: !isButtonType
    }),
    style: mergedStyles.icon,
    type: "radio",
    prefixCls,
    ref: mergedRef,
    onClick: onInputClick
  }), isReactRenderable(children) ? /* @__PURE__ */ reactExports.createElement("span", {
    className: clsx(`${prefixCls}-label`, mergedClassNames.label),
    style: mergedStyles.label
  }, children) : null));
};
const Radio$1 = /* @__PURE__ */ reactExports.forwardRef(InternalRadio);
const RadioGroup = /* @__PURE__ */ reactExports.forwardRef((props, ref) => {
  const {
    getPrefixCls,
    direction
  } = reactExports.useContext(ConfigContext);
  const {
    name: formItemName
  } = reactExports.useContext(FormItemInputContext);
  const defaultName = useId(toNamePathStr(formItemName));
  const {
    prefixCls: customizePrefixCls,
    className,
    rootClassName,
    options,
    buttonStyle = "outline",
    disabled,
    children,
    size: customizeSize,
    style,
    id,
    optionType,
    name = defaultName,
    defaultValue,
    value: customizedValue,
    block = false,
    onChange,
    onMouseEnter,
    onMouseLeave,
    onFocus,
    onBlur,
    orientation,
    vertical,
    role = "radiogroup"
  } = props;
  const [value, setValue] = useControlledState(defaultValue, customizedValue);
  const onRadioChange = reactExports.useCallback((event) => {
    const lastValue = value;
    const val = event.target.value;
    if (!("value" in props)) {
      setValue(val);
    }
    if (val !== lastValue) {
      onChange?.(event);
    }
  }, [value, setValue, onChange]);
  const prefixCls = getPrefixCls("radio", customizePrefixCls);
  const groupPrefixCls = `${prefixCls}-group`;
  const rootCls = useCSSVarCls(prefixCls);
  const [hashId, cssVarCls] = useStyle$4(prefixCls, rootCls);
  let childrenToRender = children;
  if (options && options.length > 0) {
    childrenToRender = options.map((option) => {
      if (typeof option === "string" || isNumber(option)) {
        return /* @__PURE__ */ reactExports.createElement(Radio$1, {
          key: option.toString(),
          prefixCls,
          disabled,
          value: option,
          checked: value === option
        }, option);
      }
      return /* @__PURE__ */ reactExports.createElement(Radio$1, {
        key: `radio-group-value-options-${option.value}`,
        prefixCls,
        disabled: option.disabled || disabled,
        value: option.value,
        checked: value === option.value,
        title: option.title,
        style: option.style,
        className: option.className,
        id: option.id,
        required: option.required
      }, option.label);
    });
  }
  const mergedSize = useSize(customizeSize);
  const [, mergedVertical] = useOrientation(orientation, vertical);
  const classString = clsx(groupPrefixCls, `${groupPrefixCls}-${buttonStyle}`, {
    [`${groupPrefixCls}-large`]: mergedSize === "large",
    [`${groupPrefixCls}-small`]: mergedSize === "small",
    [`${groupPrefixCls}-rtl`]: direction === "rtl",
    [`${groupPrefixCls}-block`]: block
  }, className, rootClassName, hashId, cssVarCls, rootCls);
  const memoizedValue = reactExports.useMemo(() => ({
    onChange: onRadioChange,
    value,
    disabled,
    name,
    optionType,
    block
  }), [onRadioChange, value, disabled, name, optionType, block]);
  return /* @__PURE__ */ reactExports.createElement("div", {
    ...pickAttrs(props, {
      aria: true,
      data: true
    }),
    role,
    className: clsx(classString, {
      [`${prefixCls}-group-vertical`]: mergedVertical
    }),
    style,
    onMouseEnter,
    onMouseLeave,
    onFocus,
    onBlur,
    id,
    ref
  }, /* @__PURE__ */ reactExports.createElement(RadioGroupContextProvider, {
    value: memoizedValue
  }, childrenToRender));
});
const Group = /* @__PURE__ */ reactExports.memo(RadioGroup);
const RadioButton = (props, ref) => {
  const {
    getPrefixCls
  } = reactExports.useContext(ConfigContext);
  const {
    prefixCls: customizePrefixCls,
    ...radioProps
  } = props;
  const prefixCls = getPrefixCls("radio", customizePrefixCls);
  return /* @__PURE__ */ reactExports.createElement(RadioOptionTypeContextProvider, {
    value: "button"
  }, /* @__PURE__ */ reactExports.createElement(Radio$1, {
    prefixCls,
    ...radioProps,
    type: "radio",
    ref
  }));
};
const Button = /* @__PURE__ */ reactExports.forwardRef(RadioButton);
const Radio = Radio$1;
Radio.Button = Button;
Radio.Group = Group;
Radio.__ANT_RADIO = true;
const calcThumbStyle = (targetElement, vertical) => {
  if (!targetElement) return null;
  const style = {
    left: targetElement.offsetLeft,
    right: targetElement.parentElement.clientWidth - targetElement.clientWidth - targetElement.offsetLeft,
    width: targetElement.clientWidth,
    top: targetElement.offsetTop,
    bottom: targetElement.parentElement.clientHeight - targetElement.clientHeight - targetElement.offsetTop,
    height: targetElement.clientHeight
  };
  if (vertical) {
    return {
      left: 0,
      right: 0,
      width: 0,
      top: style.top,
      bottom: style.bottom,
      height: style.height
    };
  }
  return {
    left: style.left,
    right: style.right,
    width: style.width,
    top: 0,
    bottom: 0,
    height: 0
  };
};
const toPX = (value) => value !== void 0 ? `${value}px` : void 0;
function MotionThumb(props) {
  const {
    prefixCls,
    containerRef,
    value,
    getValueIndex,
    motionName,
    onMotionStart,
    onMotionEnd,
    direction,
    vertical = false
  } = props;
  const thumbRef = reactExports.useRef(null);
  const [prevValue, setPrevValue] = reactExports.useState(value);
  const findValueElement = (val) => {
    const index = getValueIndex(val);
    const ele = containerRef.current?.querySelectorAll(`.${prefixCls}-item`)[index];
    return ele?.offsetParent && ele;
  };
  const [prevStyle, setPrevStyle] = reactExports.useState(null);
  const [nextStyle, setNextStyle] = reactExports.useState(null);
  useLayoutEffect(() => {
    if (prevValue !== value) {
      const prev = findValueElement(prevValue);
      const next = findValueElement(value);
      const calcPrevStyle = calcThumbStyle(prev, vertical);
      const calcNextStyle = calcThumbStyle(next, vertical);
      setPrevValue(value);
      setPrevStyle(calcPrevStyle);
      setNextStyle(calcNextStyle);
      if (prev && next) {
        onMotionStart();
      } else {
        onMotionEnd();
      }
    }
  }, [value]);
  const thumbStart = reactExports.useMemo(() => {
    if (vertical) {
      return toPX(prevStyle?.top ?? 0);
    }
    if (direction === "rtl") {
      return toPX(-prevStyle?.right);
    }
    return toPX(prevStyle?.left);
  }, [vertical, direction, prevStyle]);
  const thumbActive = reactExports.useMemo(() => {
    if (vertical) {
      return toPX(nextStyle?.top ?? 0);
    }
    if (direction === "rtl") {
      return toPX(-nextStyle?.right);
    }
    return toPX(nextStyle?.left);
  }, [vertical, direction, nextStyle]);
  const onAppearStart = () => {
    if (vertical) {
      return {
        transform: "translateY(var(--thumb-start-top))",
        height: "var(--thumb-start-height)"
      };
    }
    return {
      transform: "translateX(var(--thumb-start-left))",
      width: "var(--thumb-start-width)"
    };
  };
  const onAppearActive = () => {
    if (vertical) {
      return {
        transform: "translateY(var(--thumb-active-top))",
        height: "var(--thumb-active-height)"
      };
    }
    return {
      transform: "translateX(var(--thumb-active-left))",
      width: "var(--thumb-active-width)"
    };
  };
  const onVisibleChanged = () => {
    setPrevStyle(null);
    setNextStyle(null);
    onMotionEnd();
  };
  if (!prevStyle || !nextStyle) {
    return null;
  }
  return /* @__PURE__ */ reactExports.createElement(CSSMotion, {
    visible: true,
    motionName,
    motionAppear: true,
    onAppearStart,
    onAppearActive,
    onVisibleChanged
  }, ({
    className: motionClassName,
    style: motionStyle
  }, ref) => {
    const mergedStyle = {
      ...motionStyle,
      "--thumb-start-left": thumbStart,
      "--thumb-start-width": toPX(prevStyle?.width),
      "--thumb-active-left": thumbActive,
      "--thumb-active-width": toPX(nextStyle?.width),
      "--thumb-start-top": thumbStart,
      "--thumb-start-height": toPX(prevStyle?.height),
      "--thumb-active-top": thumbActive,
      "--thumb-active-height": toPX(nextStyle?.height)
    };
    const motionProps = {
      ref: composeRef(thumbRef, ref),
      style: mergedStyle,
      className: clsx(`${prefixCls}-thumb`, motionClassName)
    };
    return /* @__PURE__ */ reactExports.createElement("div", motionProps);
  });
}
function getValidTitle(option) {
  if (typeof option.title !== "undefined") {
    return option.title;
  }
  if (typeof option.label !== "object") {
    return option.label?.toString();
  }
}
function normalizeOptions(options) {
  return options.map((option) => {
    if (typeof option === "object" && option !== null) {
      const validTitle = getValidTitle(option);
      return {
        ...option,
        title: validTitle
      };
    }
    return {
      label: option?.toString(),
      title: option?.toString(),
      value: option
    };
  });
}
const InternalSegmentedOption = ({
  prefixCls,
  className,
  style,
  styles,
  classNames: segmentedClassNames,
  data,
  disabled,
  checked,
  label,
  title,
  value,
  name,
  onChange,
  onFocus,
  onBlur,
  onKeyDown,
  onKeyUp,
  onMouseDown,
  itemRender = (node) => node
}) => {
  const handleChange = (event) => {
    if (disabled) {
      return;
    }
    onChange(event, value);
  };
  const itemContent = /* @__PURE__ */ reactExports.createElement("label", {
    className: clsx(className, {
      [`${prefixCls}-item-disabled`]: disabled
    }),
    style,
    onMouseDown
  }, /* @__PURE__ */ reactExports.createElement("input", {
    name,
    className: `${prefixCls}-item-input`,
    type: "radio",
    disabled,
    checked,
    onChange: handleChange,
    onFocus,
    onBlur,
    onKeyDown,
    onKeyUp
  }), /* @__PURE__ */ reactExports.createElement("div", {
    className: clsx(`${prefixCls}-item-label`, segmentedClassNames?.label),
    title,
    style: styles?.label
  }, label));
  return itemRender(itemContent, {
    item: data
  });
};
const Segmented$1 = /* @__PURE__ */ reactExports.forwardRef((props, ref) => {
  const {
    prefixCls = "rc-segmented",
    direction,
    vertical,
    options = [],
    disabled,
    defaultValue,
    value,
    name,
    onChange,
    className = "",
    style,
    styles,
    classNames: segmentedClassNames,
    motionName = "thumb-motion",
    itemRender,
    ...restProps
  } = props;
  const containerRef = reactExports.useRef(null);
  const mergedRef = reactExports.useMemo(() => composeRef(containerRef, ref), [containerRef, ref]);
  const segmentedOptions = reactExports.useMemo(() => {
    return normalizeOptions(options);
  }, [options]);
  const [rawValue, setRawValue] = useControlledState(defaultValue ?? segmentedOptions[0]?.value, value);
  const [thumbShow, setThumbShow] = reactExports.useState(false);
  const handleChange = (event, val) => {
    setRawValue(val);
    onChange?.(val);
  };
  const divProps = omit(restProps, ["children"]);
  const [isKeyboard, setIsKeyboard] = reactExports.useState(false);
  const [isFocused, setIsFocused] = reactExports.useState(false);
  const handleFocus = () => {
    setIsFocused(true);
  };
  const handleBlur = () => {
    setIsFocused(false);
  };
  const handleMouseDown = () => {
    setIsKeyboard(false);
  };
  const handleKeyUp = (event) => {
    if (event.key === "Tab") {
      setIsKeyboard(true);
    }
  };
  const onOffset = (offset) => {
    const currentIndex = segmentedOptions.findIndex((option) => option.value === rawValue);
    const total = segmentedOptions.length;
    const nextIndex = (currentIndex + offset + total) % total;
    const nextOption = segmentedOptions[nextIndex];
    if (nextOption) {
      setRawValue(nextOption.value);
      onChange?.(nextOption.value);
    }
  };
  const handleKeyDown = (event) => {
    switch (event.key) {
      case "ArrowLeft":
      case "ArrowUp":
        onOffset(-1);
        break;
      case "ArrowRight":
      case "ArrowDown":
        onOffset(1);
        break;
    }
  };
  const renderOption = (segmentedOption) => {
    const {
      value: optionValue,
      disabled: optionDisabled
    } = segmentedOption;
    return /* @__PURE__ */ reactExports.createElement(InternalSegmentedOption, _extends$5({}, segmentedOption, {
      name,
      data: segmentedOption,
      itemRender,
      key: optionValue,
      prefixCls,
      className: clsx(segmentedOption.className, `${prefixCls}-item`, segmentedClassNames?.item, {
        [`${prefixCls}-item-selected`]: optionValue === rawValue && !thumbShow,
        [`${prefixCls}-item-focused`]: isFocused && isKeyboard && optionValue === rawValue
      }),
      style: styles?.item,
      classNames: segmentedClassNames,
      styles,
      checked: optionValue === rawValue,
      onChange: handleChange,
      onFocus: handleFocus,
      onBlur: handleBlur,
      onKeyDown: handleKeyDown,
      onKeyUp: handleKeyUp,
      onMouseDown: handleMouseDown,
      disabled: !!disabled || !!optionDisabled
    }));
  };
  return /* @__PURE__ */ reactExports.createElement("div", _extends$5({
    role: "radiogroup",
    "aria-label": "segmented control",
    tabIndex: disabled ? void 0 : 0,
    "aria-orientation": vertical ? "vertical" : "horizontal",
    style
  }, divProps, {
    className: clsx(prefixCls, {
      [`${prefixCls}-rtl`]: direction === "rtl",
      [`${prefixCls}-disabled`]: disabled,
      [`${prefixCls}-vertical`]: vertical
    }, className),
    ref: mergedRef
  }), /* @__PURE__ */ reactExports.createElement("div", {
    className: `${prefixCls}-group`
  }, /* @__PURE__ */ reactExports.createElement(MotionThumb, {
    vertical,
    prefixCls,
    value: rawValue,
    containerRef,
    motionName: `${prefixCls}-${motionName}`,
    direction,
    getValueIndex: (val) => segmentedOptions.findIndex((n) => n.value === val),
    onMotionStart: () => {
      setThumbShow(true);
    },
    onMotionEnd: () => {
      setThumbShow(false);
    }
  }), segmentedOptions.map(renderOption)));
});
const TypedSegmented = Segmented$1;
function getItemDisabledStyle(cls, token) {
  return {
    [`${cls}, ${cls}:hover, ${cls}:focus`]: {
      color: token.colorTextDisabled,
      cursor: "not-allowed"
    }
  };
}
const getItemSelectedStyle = (token) => {
  return {
    background: token.itemSelectedBg,
    boxShadow: token.boxShadowTertiary
  };
};
const segmentedTextEllipsisCss = {
  overflow: "hidden",
  // handle text ellipsis
  ...textEllipsis
};
const genSegmentedStyle = (token) => {
  const {
    componentCls,
    motionDurationSlow,
    motionEaseInOut,
    motionDurationMid
  } = token;
  const labelHeight = token.calc(token.controlHeight).sub(token.calc(token.trackPadding).mul(2)).equal();
  const labelHeightLG = token.calc(token.controlHeightLG).sub(token.calc(token.trackPadding).mul(2)).equal();
  const labelHeightSM = token.calc(token.controlHeightSM).sub(token.calc(token.trackPadding).mul(2)).equal();
  return {
    [componentCls]: {
      ...resetComponent(token),
      display: "inline-block",
      padding: token.trackPadding,
      color: token.itemColor,
      background: token.trackBg,
      borderRadius: token.borderRadius,
      transition: `all ${motionDurationMid}`,
      ...genFocusStyle(token),
      [`${componentCls}-group`]: {
        position: "relative",
        display: "flex",
        alignItems: "stretch",
        justifyItems: "flex-start",
        flexDirection: "row",
        width: "100%"
      },
      // RTL styles
      [`&${componentCls}-rtl`]: {
        direction: "rtl"
      },
      [`&${componentCls}-vertical`]: {
        [`${componentCls}-group`]: {
          flexDirection: "column"
        },
        [`${componentCls}-thumb`]: {
          width: "100%",
          height: 0,
          padding: `0 ${unit(token.paddingXXS)}`
        }
      },
      // block styles
      [`&${componentCls}-block`]: {
        display: "flex"
      },
      [`&${componentCls}-block ${componentCls}-item`]: {
        flex: 1,
        minWidth: 0
      },
      // item styles
      [`${componentCls}-item`]: {
        position: "relative",
        textAlign: "center",
        cursor: "pointer",
        transition: `color ${motionDurationMid}`,
        borderRadius: token.borderRadiusSM,
        // Fix Safari render bug
        // https://github.com/ant-design/ant-design/issues/45250
        transform: "translateZ(0)",
        "&-selected": {
          ...getItemSelectedStyle(token),
          color: token.itemSelectedColor
        },
        "&-focused": genFocusOutline(token),
        "&::after": {
          content: '""',
          position: "absolute",
          zIndex: -1,
          width: "100%",
          height: "100%",
          top: 0,
          insetInlineStart: 0,
          borderRadius: "inherit",
          opacity: 0,
          // This is mandatory to make it not clickable or hoverable
          // Ref: https://github.com/ant-design/ant-design/issues/40888
          pointerEvents: "none",
          transition: ["opacity", "background-color"].map((prop) => `${prop} ${motionDurationMid}`).join(", ")
        },
        [`&:not(${componentCls}-item-selected):not(${componentCls}-item-disabled)`]: {
          "&:hover, &:active": {
            color: token.itemHoverColor
          },
          "&:hover::after": {
            opacity: 1,
            backgroundColor: token.itemHoverBg
          },
          "&:active::after": {
            opacity: 1,
            backgroundColor: token.itemActiveBg
          }
        },
        "&-label": {
          minHeight: labelHeight,
          lineHeight: unit(labelHeight),
          padding: `0 ${unit(token.segmentedPaddingHorizontal)}`,
          ...segmentedTextEllipsisCss
        },
        // syntactic sugar to add `icon` for Segmented Item
        "&-icon + *": {
          marginInlineStart: token.calc(token.marginSM).div(2).equal()
        },
        // Icons from third-party libraries render as a bare `<svg>` inside the icon wrapper,
        // which the `.anticon` reset never reaches. An `<svg>` has no baseline of its own, so it
        // is aligned by its bottom margin edge (CSS 2.1 §10.8.1) and rides above the label.
        // `display: inline-block` keeps it an atomic inline box so `vertical-align` still applies
        // even under a CSS reset that forces `svg { display: block }` (e.g. Tailwind Preflight),
        // which would otherwise drop the icon onto its own line. `vertical-align: middle` centres its
        // margin box on the x-height line; `margin-block-end` then lifts it by half its own value onto
        // the cap-height centre (capHeight − xHeight ≈ 0.2em across typical fonts), keeping it centred
        // at any icon size.
        // Only matches a bare `<svg>`: an `.anticon` keeps its `<svg>` one level deeper.
        "&-icon > svg": {
          display: "inline-block",
          verticalAlign: "middle",
          marginBlockEnd: "0.2em"
        },
        "&-input": {
          position: "absolute",
          insetBlockStart: 0,
          insetInlineStart: 0,
          width: 0,
          height: 0,
          opacity: 0,
          pointerEvents: "none"
        }
      },
      // thumb styles
      [`${componentCls}-thumb`]: {
        ...getItemSelectedStyle(token),
        position: "absolute",
        insetBlockStart: 0,
        insetInlineStart: 0,
        width: 0,
        height: "100%",
        padding: `${unit(token.paddingXXS)} 0`,
        borderRadius: token.borderRadiusSM,
        [`& ~ ${componentCls}-item:not(${componentCls}-item-selected):not(${componentCls}-item-disabled)::after`]: {
          backgroundColor: "transparent"
        }
      },
      // size styles
      [`&${componentCls}-lg`]: {
        borderRadius: token.borderRadiusLG,
        [`${componentCls}-item-label`]: {
          minHeight: labelHeightLG,
          lineHeight: unit(labelHeightLG),
          padding: `0 ${unit(token.segmentedPaddingHorizontal)}`,
          fontSize: token.fontSizeLG
        },
        [`${componentCls}-item, ${componentCls}-thumb`]: {
          borderRadius: token.borderRadius
        }
      },
      [`&${componentCls}-sm`]: {
        borderRadius: token.borderRadiusSM,
        [`${componentCls}-item-label`]: {
          minHeight: labelHeightSM,
          lineHeight: unit(labelHeightSM),
          padding: `0 ${unit(token.segmentedPaddingHorizontalSM)}`
        },
        [`${componentCls}-item, ${componentCls}-thumb`]: {
          borderRadius: token.borderRadiusXS
        }
      },
      // disabled styles
      ...getItemDisabledStyle(`&-disabled ${componentCls}-item`, token),
      ...getItemDisabledStyle(`${componentCls}-item-disabled`, token),
      // transition effect when `appear-active`
      [`${componentCls}-thumb-motion-appear-active`]: {
        willChange: "transform, width",
        transition: [`transform`, `width`].map((prop) => `${prop} ${motionDurationSlow} ${motionEaseInOut}`).join(", ")
      },
      [`&${componentCls}-shape-round`]: {
        borderRadius: 9999,
        [`${componentCls}-item, ${componentCls}-thumb`]: {
          borderRadius: 9999
        }
      }
    }
  };
};
const prepareComponentToken$2 = (token) => {
  const {
    colorTextLabel,
    colorText,
    colorFillSecondary,
    colorBgElevated,
    colorFill,
    lineWidthBold,
    colorBgLayout
  } = token;
  return {
    trackPadding: lineWidthBold,
    trackBg: colorBgLayout,
    itemColor: colorTextLabel,
    itemHoverColor: colorText,
    itemHoverBg: colorFillSecondary,
    itemSelectedBg: colorBgElevated,
    itemActiveBg: colorFill,
    itemSelectedColor: colorText
  };
};
const useStyle$3 = genStyleHooks("Segmented", (token) => {
  const {
    lineWidth,
    calc
  } = token;
  const segmentedToken = merge(token, {
    segmentedPaddingHorizontal: calc(token.controlPaddingHorizontal).sub(lineWidth).equal(),
    segmentedPaddingHorizontalSM: calc(token.controlPaddingHorizontalSM).sub(lineWidth).equal()
  });
  return genSegmentedStyle(segmentedToken);
}, prepareComponentToken$2);
function isSegmentedLabeledOptionWithIcon(option) {
  return isPlainObject(option) && !!option?.icon;
}
const InternalSegmented = /* @__PURE__ */ reactExports.forwardRef((props, ref) => {
  const defaultName = useId();
  const {
    prefixCls: customizePrefixCls,
    className,
    rootClassName,
    block,
    options = [],
    size: customSize,
    style,
    vertical,
    orientation,
    shape = "default",
    name = defaultName,
    styles,
    classNames,
    ...restProps
  } = props;
  const {
    getPrefixCls,
    direction,
    className: contextClassName,
    style: contextStyle,
    classNames: contextClassNames,
    styles: contextStyles
  } = useComponentConfig("segmented");
  const mergedProps = {
    ...props,
    options,
    size: customSize,
    shape
  };
  const contextStyleRoot = useSemanticRootStyle(contextStyle);
  const styleRoot = useSemanticRootStyle(style);
  const [mergedClassNames, mergedStyles] = useMergeSemantic([contextClassNames, classNames], [contextStyles, contextStyleRoot, styles, styleRoot], {
    props: mergedProps
  });
  const prefixCls = getPrefixCls("segmented", customizePrefixCls);
  const [hashId, cssVarCls] = useStyle$3(prefixCls);
  const mergedSize = useSize(customSize);
  const extendedOptions = reactExports.useMemo(() => options.map((option) => {
    if (isSegmentedLabeledOptionWithIcon(option)) {
      const {
        icon,
        label,
        ...restOption
      } = option;
      return {
        ...restOption,
        label: /* @__PURE__ */ reactExports.createElement(reactExports.Fragment, null, /* @__PURE__ */ reactExports.createElement("span", {
          className: clsx(`${prefixCls}-item-icon`, mergedClassNames.icon),
          style: mergedStyles.icon
        }, icon), label && /* @__PURE__ */ reactExports.createElement("span", null, label))
      };
    }
    return option;
  }), [options, prefixCls, mergedClassNames.icon, mergedStyles.icon]);
  const [, mergedVertical] = useOrientation(orientation, vertical);
  const cls = clsx(className, rootClassName, contextClassName, mergedClassNames.root, {
    [`${prefixCls}-block`]: block,
    [`${prefixCls}-sm`]: mergedSize === "small",
    [`${prefixCls}-lg`]: mergedSize === "large",
    [`${prefixCls}-vertical`]: mergedVertical,
    [`${prefixCls}-shape-${shape}`]: shape === "round"
  }, hashId, cssVarCls);
  const itemRender = (node, {
    item
  }) => {
    if (!item.tooltip) {
      return node;
    }
    const tooltipProps = isPlainObject(item.tooltip) ? item.tooltip : {
      title: item.tooltip
    };
    return /* @__PURE__ */ reactExports.createElement(Tooltip, {
      ...tooltipProps
    }, node);
  };
  return /* @__PURE__ */ reactExports.createElement(TypedSegmented, {
    ...restProps,
    name,
    className: cls,
    style: mergedStyles.root,
    classNames: mergedClassNames,
    styles: mergedStyles,
    itemRender,
    options: extendedOptions,
    ref,
    prefixCls,
    direction,
    vertical: mergedVertical
  });
});
const Segmented = InternalSegmented;
const PanelPickerContext = /* @__PURE__ */ React.createContext({});
const PanelPresetsContext = /* @__PURE__ */ React.createContext({});
const ColorClear = ({
  prefixCls,
  value,
  onChange,
  className,
  style
}) => {
  const onClick = () => {
    if (onChange && value && !value.cleared) {
      const hsba = value.toHsb();
      hsba.a = 0;
      const genColor = generateColor$1(hsba);
      genColor.cleared = true;
      onChange(genColor);
    }
  };
  const onKeyDown = (event) => {
    if (event.key === "Enter" || event.key === " ") {
      event.preventDefault();
      onClick();
    }
  };
  return /* @__PURE__ */ React.createElement("div", {
    role: "button",
    "aria-label": "Clear color",
    tabIndex: 0,
    className: clsx(`${prefixCls}-clear`, className),
    style,
    onClick,
    onKeyDown
  });
};
const FORMAT_HEX = "hex";
const FORMAT_RGB = "rgb";
const FORMAT_HSB = "hsb";
var UpOutlined$1 = {};
var hasRequiredUpOutlined;
function requireUpOutlined() {
  if (hasRequiredUpOutlined) return UpOutlined$1;
  hasRequiredUpOutlined = 1;
  Object.defineProperty(UpOutlined$1, "__esModule", { value: true });
  var UpOutlined2 = { "icon": { "tag": "svg", "attrs": { "viewBox": "64 64 896 896", "focusable": "false" }, "children": [{ "tag": "path", "attrs": { "d": "M890.5 755.3L537.9 269.2c-12.8-17.6-39-17.6-51.7 0L133.5 755.3A8 8 0 00140 768h75c5.1 0 9.9-2.5 12.9-6.6L512 369.8l284.1 391.6c3 4.1 7.8 6.6 12.9 6.6h75c6.5 0 10.3-7.4 6.5-12.7z" } }] }, "name": "up", "theme": "outlined" };
  UpOutlined$1.default = UpOutlined2;
  return UpOutlined$1;
}
var UpOutlinedExports = /* @__PURE__ */ requireUpOutlined();
const UpOutlinedSvg = /* @__PURE__ */ getDefaultExportFromCjs(UpOutlinedExports);
function _extends$3() {
  _extends$3 = Object.assign ? Object.assign.bind() : function(target) {
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
  return _extends$3.apply(this, arguments);
}
const UpOutlined = (props, ref) => /* @__PURE__ */ reactExports.createElement(Icon, _extends$3({}, props, {
  ref,
  icon: UpOutlinedSvg
}));
const RefIcon = /* @__PURE__ */ reactExports.forwardRef(UpOutlined);
function supportBigInt() {
  return typeof BigInt === "function";
}
function isEmpty(value) {
  return !value && value !== 0 && !Number.isNaN(value) || !String(value).trim();
}
function trimNumber(numStr) {
  var str = numStr.trim();
  var negative = str.startsWith("-");
  if (negative) {
    str = str.slice(1);
  }
  str = str.replace(/(\.\d*[^0])0*$/, "$1").replace(/\.0*$/, "").replace(/^0+/, "");
  if (str.startsWith(".")) {
    str = "0".concat(str);
  }
  var trimStr = str || "0";
  var splitNumber = trimStr.split(".");
  var integerStr = splitNumber[0] || "0";
  var decimalStr = splitNumber[1] || "0";
  if (integerStr === "0" && decimalStr === "0") {
    negative = false;
  }
  var negativeStr = negative ? "-" : "";
  return {
    negative,
    negativeStr,
    trimStr,
    integerStr,
    decimalStr,
    fullStr: "".concat(negativeStr).concat(trimStr)
  };
}
function isE(number) {
  var str = String(number);
  return !Number.isNaN(Number(str)) && str.includes("e");
}
function parseScientificNotation(numStr) {
  var _numStr$toLowerCase$s = numStr.toLowerCase().split("e"), _numStr$toLowerCase$s2 = _slicedToArray(_numStr$toLowerCase$s, 2), mantissa = _numStr$toLowerCase$s2[0], _numStr$toLowerCase$s3 = _numStr$toLowerCase$s2[1], exponent = _numStr$toLowerCase$s3 === void 0 ? "0" : _numStr$toLowerCase$s3;
  var negative = mantissa.startsWith("-");
  var unsignedMantissa = negative ? mantissa.slice(1) : mantissa;
  var _unsignedMantissa$spl = unsignedMantissa.split("."), _unsignedMantissa$spl2 = _slicedToArray(_unsignedMantissa$spl, 2), _unsignedMantissa$spl3 = _unsignedMantissa$spl2[0], integer = _unsignedMantissa$spl3 === void 0 ? "0" : _unsignedMantissa$spl3, _unsignedMantissa$spl4 = _unsignedMantissa$spl2[1], decimal = _unsignedMantissa$spl4 === void 0 ? "" : _unsignedMantissa$spl4;
  var digits = "".concat(integer).concat(decimal).replace(/^0+/, "") || "0";
  return {
    decimal,
    digits,
    exponent: Number(exponent),
    integer,
    negative
  };
}
function expandScientificNotation(parsed) {
  var decimal = parsed.decimal, digits = parsed.digits, exponent = parsed.exponent, integer = parsed.integer, negative = parsed.negative;
  if (digits === "0") {
    return "0";
  }
  var integerDigits = integer.replace(/^0+/, "").length;
  var leadingDecimalZeros = (decimal.match(/^0*/) || [""])[0].length;
  var initialDecimalIndex = integerDigits || -leadingDecimalZeros;
  var decimalIndex = initialDecimalIndex + exponent;
  var expanded = "";
  if (decimalIndex <= 0) {
    expanded = "0.".concat("0".repeat(-decimalIndex)).concat(digits);
  } else if (decimalIndex >= digits.length) {
    expanded = "".concat(digits).concat("0".repeat(decimalIndex - digits.length));
  } else {
    expanded = "".concat(digits.slice(0, decimalIndex), ".").concat(digits.slice(decimalIndex));
  }
  return "".concat(negative ? "-" : "").concat(expanded);
}
function getScientificPrecision(parsed) {
  if (parsed.exponent >= 0) {
    return Math.max(0, parsed.decimal.length - parsed.exponent);
  }
  return Math.abs(parsed.exponent) + parsed.decimal.length;
}
function getNumberPrecision(number) {
  var numStr = String(number);
  if (isE(number)) {
    return getScientificPrecision(parseScientificNotation(numStr));
  }
  return numStr.includes(".") && validateNumber(numStr) ? numStr.length - numStr.indexOf(".") - 1 : 0;
}
function num2str(number) {
  var numStr = String(number);
  if (isE(number)) {
    if (number > Number.MAX_SAFE_INTEGER) {
      return String(supportBigInt() ? BigInt(number).toString() : Number.MAX_SAFE_INTEGER);
    }
    if (number < Number.MIN_SAFE_INTEGER) {
      return String(supportBigInt() ? BigInt(number).toString() : Number.MIN_SAFE_INTEGER);
    }
    var parsed = parseScientificNotation(numStr);
    var precision = getScientificPrecision(parsed);
    numStr = precision > 100 ? expandScientificNotation(parsed) : number.toFixed(precision);
  }
  return trimNumber(numStr).fullStr;
}
function validateNumber(num) {
  if (typeof num === "number") {
    return !Number.isNaN(num);
  }
  if (!num) {
    return false;
  }
  return (
    // Normal type: 11.28
    /^\s*-?\d+(\.\d+)?\s*$/.test(num) || // Pre-number: 1.
    /^\s*-?\d+\.\s*$/.test(num) || // Post-number: .1
    /^\s*-?\.\d+\s*$/.test(num)
  );
}
var BigIntDecimal = /* @__PURE__ */ (function() {
  function BigIntDecimal2(value) {
    _classCallCheck(this, BigIntDecimal2);
    _defineProperty(this, "origin", "");
    _defineProperty(this, "negative", void 0);
    _defineProperty(this, "integer", void 0);
    _defineProperty(this, "decimal", void 0);
    _defineProperty(this, "decimalLen", void 0);
    _defineProperty(this, "empty", void 0);
    _defineProperty(this, "nan", void 0);
    if (isEmpty(value)) {
      this.empty = true;
      return;
    }
    this.origin = String(value);
    if (value === "-" || Number.isNaN(value)) {
      this.nan = true;
      return;
    }
    var mergedValue = value;
    if (isE(mergedValue)) {
      mergedValue = Number(mergedValue);
    }
    mergedValue = typeof mergedValue === "string" ? mergedValue : num2str(mergedValue);
    if (validateNumber(mergedValue)) {
      var trimRet = trimNumber(mergedValue);
      this.negative = trimRet.negative;
      var numbers = trimRet.trimStr.split(".");
      this.integer = BigInt(numbers[0]);
      var decimalStr = numbers[1] || "0";
      this.decimal = BigInt(decimalStr);
      this.decimalLen = decimalStr.length;
    } else {
      this.nan = true;
    }
  }
  _createClass(BigIntDecimal2, [{
    key: "getMark",
    value: function getMark() {
      return this.negative ? "-" : "";
    }
  }, {
    key: "getIntegerStr",
    value: function getIntegerStr() {
      return this.integer.toString();
    }
    /**
     * @private get decimal string
     */
  }, {
    key: "getDecimalStr",
    value: function getDecimalStr() {
      return this.decimal.toString().padStart(this.decimalLen, "0");
    }
    /**
     * @private Align BigIntDecimal with same decimal length. e.g. 12.3 + 5 = 1230000
     * This is used for add function only.
     */
  }, {
    key: "alignDecimal",
    value: function alignDecimal(decimalLength) {
      var str = "".concat(this.getMark()).concat(this.getIntegerStr()).concat(this.getDecimalStr().padEnd(decimalLength, "0"));
      return BigInt(str);
    }
  }, {
    key: "negate",
    value: function negate() {
      var clone = new BigIntDecimal2(this.toString());
      clone.negative = !clone.negative;
      return clone;
    }
  }, {
    key: "cal",
    value: function cal(offset, calculator, calDecimalLen) {
      var maxDecimalLength = Math.max(this.getDecimalStr().length, offset.getDecimalStr().length);
      var myAlignedDecimal = this.alignDecimal(maxDecimalLength);
      var offsetAlignedDecimal = offset.alignDecimal(maxDecimalLength);
      var valueStr = calculator(myAlignedDecimal, offsetAlignedDecimal).toString();
      var nextDecimalLength = calDecimalLen(maxDecimalLength);
      var _trimNumber = trimNumber(valueStr), negativeStr = _trimNumber.negativeStr, trimStr = _trimNumber.trimStr;
      var hydrateValueStr = "".concat(negativeStr).concat(trimStr.padStart(nextDecimalLength + 1, "0"));
      return new BigIntDecimal2("".concat(hydrateValueStr.slice(0, -nextDecimalLength), ".").concat(hydrateValueStr.slice(-nextDecimalLength)));
    }
  }, {
    key: "add",
    value: function add(value) {
      if (this.isInvalidate()) {
        return new BigIntDecimal2(value);
      }
      var offset = new BigIntDecimal2(value);
      if (offset.isInvalidate()) {
        return this;
      }
      return this.cal(offset, function(num1, num2) {
        return num1 + num2;
      }, function(len) {
        return len;
      });
    }
  }, {
    key: "multi",
    value: function multi(value) {
      var target = new BigIntDecimal2(value);
      if (this.isInvalidate() || target.isInvalidate()) {
        return new BigIntDecimal2(NaN);
      }
      return this.cal(target, function(num1, num2) {
        return num1 * num2;
      }, function(len) {
        return len * 2;
      });
    }
  }, {
    key: "isEmpty",
    value: function isEmpty2() {
      return this.empty;
    }
  }, {
    key: "isNaN",
    value: function isNaN() {
      return this.nan;
    }
  }, {
    key: "isInvalidate",
    value: function isInvalidate() {
      return this.isEmpty() || this.isNaN();
    }
  }, {
    key: "equals",
    value: function equals(target) {
      return this.toString() === (target === null || target === void 0 ? void 0 : target.toString());
    }
  }, {
    key: "lessEquals",
    value: function lessEquals(target) {
      return this.add(target.negate().toString()).toNumber() <= 0;
    }
  }, {
    key: "toNumber",
    value: function toNumber() {
      if (this.isNaN()) {
        return NaN;
      }
      return Number(this.toString());
    }
  }, {
    key: "toString",
    value: function toString() {
      var safe = arguments.length > 0 && arguments[0] !== void 0 ? arguments[0] : true;
      if (!safe) {
        return this.origin;
      }
      if (this.isInvalidate()) {
        return "";
      }
      return trimNumber("".concat(this.getMark()).concat(this.getIntegerStr(), ".").concat(this.getDecimalStr())).fullStr;
    }
  }]);
  return BigIntDecimal2;
})();
var NumberDecimal = /* @__PURE__ */ (function() {
  function NumberDecimal2(value) {
    _classCallCheck(this, NumberDecimal2);
    _defineProperty(this, "origin", "");
    _defineProperty(this, "number", void 0);
    _defineProperty(this, "empty", void 0);
    if (isEmpty(value)) {
      this.empty = true;
      return;
    }
    this.origin = String(value);
    this.number = Number(value);
  }
  _createClass(NumberDecimal2, [{
    key: "negate",
    value: function negate() {
      return new NumberDecimal2(-this.toNumber());
    }
  }, {
    key: "add",
    value: function add(value) {
      if (this.isInvalidate()) {
        return new NumberDecimal2(value);
      }
      var target = Number(value);
      if (Number.isNaN(target)) {
        return this;
      }
      var number = this.number + target;
      if (number > Number.MAX_SAFE_INTEGER) {
        return new NumberDecimal2(Number.MAX_SAFE_INTEGER);
      }
      if (number < Number.MIN_SAFE_INTEGER) {
        return new NumberDecimal2(Number.MIN_SAFE_INTEGER);
      }
      var maxPrecision = Math.max(getNumberPrecision(this.number), getNumberPrecision(target));
      return new NumberDecimal2(number.toFixed(maxPrecision));
    }
  }, {
    key: "multi",
    value: function multi(value) {
      var target = Number(value);
      if (this.isInvalidate() || Number.isNaN(target)) {
        return new NumberDecimal2(NaN);
      }
      var number = this.number * target;
      if (number > Number.MAX_SAFE_INTEGER) {
        return new NumberDecimal2(Number.MAX_SAFE_INTEGER);
      }
      if (number < Number.MIN_SAFE_INTEGER) {
        return new NumberDecimal2(Number.MIN_SAFE_INTEGER);
      }
      var maxPrecision = Math.max(getNumberPrecision(this.number), getNumberPrecision(target));
      return new NumberDecimal2(number.toFixed(maxPrecision));
    }
  }, {
    key: "isEmpty",
    value: function isEmpty2() {
      return this.empty;
    }
  }, {
    key: "isNaN",
    value: function isNaN() {
      return Number.isNaN(this.number);
    }
  }, {
    key: "isInvalidate",
    value: function isInvalidate() {
      return this.isEmpty() || this.isNaN();
    }
  }, {
    key: "equals",
    value: function equals(target) {
      return this.toNumber() === (target === null || target === void 0 ? void 0 : target.toNumber());
    }
  }, {
    key: "lessEquals",
    value: function lessEquals(target) {
      return this.add(target.negate().toString()).toNumber() <= 0;
    }
  }, {
    key: "toNumber",
    value: function toNumber() {
      return this.number;
    }
  }, {
    key: "toString",
    value: function toString() {
      var safe = arguments.length > 0 && arguments[0] !== void 0 ? arguments[0] : true;
      if (!safe) {
        return this.origin;
      }
      if (this.isInvalidate()) {
        return "";
      }
      if (isE(this.number) && getNumberPrecision(this.number) > 100) {
        return String(this.number);
      }
      return num2str(this.number);
    }
  }]);
  return NumberDecimal2;
})();
function getMiniDecimal(value) {
  if (supportBigInt()) {
    return new BigIntDecimal(value);
  }
  return new NumberDecimal(value);
}
function toFixed(numStr, separatorStr, precision) {
  var cutOnly = arguments.length > 3 && arguments[3] !== void 0 ? arguments[3] : false;
  if (numStr === "") {
    return "";
  }
  var _trimNumber = trimNumber(numStr), negativeStr = _trimNumber.negativeStr, integerStr = _trimNumber.integerStr, decimalStr = _trimNumber.decimalStr;
  var precisionDecimalStr = "".concat(separatorStr).concat(decimalStr);
  var numberWithoutDecimal = "".concat(negativeStr).concat(integerStr);
  if (precision >= 0) {
    var advancedNum = Number(decimalStr[precision]);
    if (advancedNum >= 5 && !cutOnly) {
      var advancedDecimal = getMiniDecimal(numStr).add("".concat(negativeStr, "0.").concat("0".repeat(precision)).concat(10 - advancedNum));
      return toFixed(advancedDecimal.toString(), separatorStr, precision, cutOnly);
    }
    if (precision === 0) {
      return numberWithoutDecimal;
    }
    return "".concat(numberWithoutDecimal).concat(separatorStr).concat(decimalStr.padEnd(precision, "0").slice(0, precision));
  }
  if (precisionDecimalStr === ".0") {
    return numberWithoutDecimal;
  }
  return "".concat(numberWithoutDecimal).concat(precisionDecimalStr);
}
function useCursor(input, focused) {
  const selectionRef = reactExports.useRef(null);
  function recordCursor() {
    try {
      const {
        selectionStart: start,
        selectionEnd: end,
        value
      } = input;
      const beforeTxt = value.substring(0, start);
      const afterTxt = value.substring(end);
      selectionRef.current = {
        start,
        end,
        value,
        beforeTxt,
        afterTxt
      };
    } catch (e) {
    }
  }
  function restoreCursor() {
    if (input && selectionRef.current && focused) {
      try {
        const {
          value
        } = input;
        const {
          beforeTxt,
          afterTxt,
          start
        } = selectionRef.current;
        let startPos = value.length;
        if (value.startsWith(beforeTxt)) {
          startPos = beforeTxt.length;
        } else if (value.endsWith(afterTxt)) {
          startPos = value.length - selectionRef.current.afterTxt.length;
        } else {
          const beforeLastChar = beforeTxt[start - 1];
          const newIndex = value.indexOf(beforeLastChar, start - 1);
          if (newIndex !== -1) {
            startPos = newIndex + 1;
          }
        }
        input.setSelectionRange(startPos, startPos);
      } catch (e) {
        warningOnce(false, `Something warning of cursor restore. Please fire issue about this: ${e.message}`);
      }
    }
  }
  return [recordCursor, restoreCursor];
}
const STEP_INTERVAL = 200;
const STEP_DELAY = 600;
function StepHandler({
  prefixCls,
  action,
  children,
  disabled,
  className,
  style,
  onStep
}) {
  const isUpAction = action === "up";
  const stepTimeoutRef = reactExports.useRef();
  const frameIds = reactExports.useRef([]);
  const onStopStep = () => {
    clearTimeout(stepTimeoutRef.current);
  };
  const onStepMouseDown = (e) => {
    e.preventDefault();
    onStopStep();
    onStep(isUpAction, "handler");
    function loopStep() {
      onStep(isUpAction, "handler");
      stepTimeoutRef.current = setTimeout(loopStep, STEP_INTERVAL);
    }
    stepTimeoutRef.current = setTimeout(loopStep, STEP_DELAY);
  };
  reactExports.useEffect(() => () => {
    onStopStep();
    frameIds.current.forEach((id) => {
      wrapperRaf.cancel(id);
    });
  }, []);
  const actionClassName = `${prefixCls}-action`;
  const mergedClassName = clsx(actionClassName, `${actionClassName}-${action}`, {
    [`${actionClassName}-${action}-disabled`]: disabled
  }, className);
  const safeOnStopStep = () => frameIds.current.push(wrapperRaf(onStopStep));
  return /* @__PURE__ */ reactExports.createElement("span", {
    unselectable: "on",
    role: "button",
    onMouseUp: safeOnStopStep,
    onMouseLeave: safeOnStopStep,
    onMouseDown: (e) => {
      onStepMouseDown(e);
    },
    "aria-label": isUpAction ? "Increase Value" : "Decrease Value",
    "aria-disabled": disabled,
    className: mergedClassName,
    style
  }, children || /* @__PURE__ */ reactExports.createElement("span", {
    unselectable: "on",
    className: `${prefixCls}-action-${action}-inner`
  }));
}
function getDecupleSteps(step) {
  const stepStr = typeof step === "number" ? num2str(step) : trimNumber(step).fullStr;
  const hasPoint = stepStr.includes(".");
  if (!hasPoint) {
    return step + "0";
  }
  return trimNumber(stepStr.replace(/(\d)\.(\d)/g, "$1$2.")).fullStr;
}
const useFrame = (() => {
  const idRef = reactExports.useRef(0);
  const cleanUp = () => {
    wrapperRaf.cancel(idRef.current);
  };
  reactExports.useEffect(() => cleanUp, []);
  return (callback) => {
    cleanUp();
    idRef.current = wrapperRaf(() => {
      callback();
    });
  };
});
function _extends$2() {
  _extends$2 = Object.assign ? Object.assign.bind() : function(target) {
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
  return _extends$2.apply(this, arguments);
}
const getDecimalValue = (stringMode, decimalValue) => {
  if (stringMode || decimalValue.isEmpty()) {
    return decimalValue.toString();
  }
  return decimalValue.toNumber();
};
const getDecimalIfValidate = (value) => {
  const decimal = getMiniDecimal(value);
  return decimal.isInvalidate() ? null : decimal;
};
const InputNumber$1 = /* @__PURE__ */ reactExports.forwardRef((props, ref) => {
  const {
    mode = "input",
    prefixCls = "rc-input-number",
    className,
    style,
    classNames,
    styles,
    min,
    max,
    step = 1,
    defaultValue,
    value,
    disabled,
    readOnly,
    upHandler,
    downHandler,
    keyboard,
    changeOnWheel = false,
    controls = true,
    prefix,
    suffix,
    stringMode,
    parser,
    formatter,
    precision,
    decimalSeparator,
    onChange,
    onInput,
    onPressEnter,
    onStep,
    // Mouse Events
    onMouseDown,
    onClick,
    onMouseUp,
    onMouseLeave,
    onMouseMove,
    onMouseEnter,
    onMouseOut,
    changeOnBlur = true,
    ...restProps
  } = props;
  const [focus, setFocus] = reactExports.useState(false);
  const userTypingRef = reactExports.useRef(false);
  const compositionRef = reactExports.useRef(false);
  const shiftKeyRef = reactExports.useRef(false);
  const rootRef = reactExports.useRef(null);
  const inputRef = reactExports.useRef(null);
  reactExports.useImperativeHandle(ref, () => proxyObject(inputRef.current, {
    focus: (option) => {
      triggerFocus(inputRef.current, option);
    },
    blur: () => {
      inputRef.current?.blur();
    },
    nativeElement: rootRef.current
  }));
  const [decimalValue, setDecimalValue] = reactExports.useState(() => getMiniDecimal(value ?? defaultValue));
  function setUncontrolledDecimalValue(newDecimal) {
    if (value === void 0) {
      setDecimalValue(newDecimal);
    }
  }
  const getPrecision = reactExports.useCallback((numStr, userTyping) => {
    if (userTyping) {
      return void 0;
    }
    if (precision >= 0) {
      return precision;
    }
    return Math.max(getNumberPrecision(numStr), getNumberPrecision(step));
  }, [precision, step]);
  const mergedParser = reactExports.useCallback((num) => {
    const numStr = String(num);
    if (parser) {
      return parser(numStr);
    }
    let parsedStr = numStr;
    if (decimalSeparator) {
      parsedStr = parsedStr.replace(decimalSeparator, ".");
    }
    return parsedStr.replace(/[^\w.-]+/g, "");
  }, [parser, decimalSeparator]);
  const inputValueRef = reactExports.useRef("");
  const mergedFormatter = reactExports.useCallback((number, userTyping) => {
    if (formatter) {
      return formatter(number, {
        userTyping,
        input: String(inputValueRef.current)
      });
    }
    let str = typeof number === "number" ? num2str(number) : number;
    if (!userTyping) {
      const mergedPrecision = getPrecision(str, userTyping);
      if (validateNumber(str) && (decimalSeparator || mergedPrecision >= 0)) {
        const separatorStr = decimalSeparator || ".";
        str = toFixed(str, separatorStr, mergedPrecision);
      }
    }
    return str;
  }, [formatter, getPrecision, decimalSeparator]);
  const [inputValue, setInternalInputValue] = reactExports.useState(() => {
    const initValue = defaultValue ?? value;
    if (decimalValue.isInvalidate() && ["string", "number"].includes(typeof initValue)) {
      return Number.isNaN(initValue) ? "" : initValue;
    }
    return mergedFormatter(decimalValue.toString(), false);
  });
  inputValueRef.current = inputValue;
  function setInputValue(newValue, userTyping) {
    setInternalInputValue(mergedFormatter(
      // Invalidate number is sometime passed by external control, we should let it go
      // Otherwise is controlled by internal interactive logic which check by userTyping
      // You can ref 'show limited value when input is not focused' test for more info.
      newValue.isInvalidate() ? newValue.toString(false) : newValue.toString(!userTyping),
      userTyping
    ));
  }
  const maxDecimal = reactExports.useMemo(() => getDecimalIfValidate(max), [max, precision]);
  const minDecimal = reactExports.useMemo(() => getDecimalIfValidate(min), [min, precision]);
  const upDisabled = reactExports.useMemo(() => {
    if (!maxDecimal || !decimalValue || decimalValue.isInvalidate()) {
      return false;
    }
    return maxDecimal.lessEquals(decimalValue);
  }, [maxDecimal, decimalValue]);
  const downDisabled = reactExports.useMemo(() => {
    if (!minDecimal || !decimalValue || decimalValue.isInvalidate()) {
      return false;
    }
    return decimalValue.lessEquals(minDecimal);
  }, [minDecimal, decimalValue]);
  const [recordCursor, restoreCursor] = useCursor(inputRef.current, focus);
  const getRangeValue = (target) => {
    if (maxDecimal && !target.lessEquals(maxDecimal)) {
      return maxDecimal;
    }
    if (minDecimal && !minDecimal.lessEquals(target)) {
      return minDecimal;
    }
    return null;
  };
  const isInRange = (target) => !getRangeValue(target);
  const triggerValueUpdate = (newValue, userTyping) => {
    let updateValue = newValue;
    let isRangeValidate = isInRange(updateValue) || updateValue.isEmpty();
    if (!updateValue.isEmpty() && !userTyping) {
      updateValue = getRangeValue(updateValue) || updateValue;
      isRangeValidate = true;
    }
    if (!readOnly && !disabled && isRangeValidate) {
      const numStr = updateValue.toString();
      const mergedPrecision = getPrecision(numStr, userTyping);
      if (mergedPrecision >= 0) {
        updateValue = getMiniDecimal(toFixed(numStr, ".", mergedPrecision));
        if (!isInRange(updateValue)) {
          updateValue = getMiniDecimal(toFixed(numStr, ".", mergedPrecision, true));
        }
      }
      if (!updateValue.equals(decimalValue)) {
        setUncontrolledDecimalValue(updateValue);
        onChange?.(updateValue.isEmpty() ? null : getDecimalValue(stringMode, updateValue));
        if (value === void 0) {
          setInputValue(updateValue, userTyping);
        }
      }
      return updateValue;
    }
    return decimalValue;
  };
  const onNextPromise = useFrame();
  const collectInputValue = (inputStr) => {
    recordCursor();
    inputValueRef.current = inputStr;
    setInternalInputValue(inputStr);
    if (!compositionRef.current) {
      const finalValue = mergedParser(inputStr);
      const finalDecimal = getMiniDecimal(finalValue);
      if (!finalDecimal.isNaN()) {
        triggerValueUpdate(finalDecimal, true);
      }
    }
    onInput?.(inputStr);
    onNextPromise(() => {
      let nextInputStr = inputStr;
      if (!parser) {
        nextInputStr = inputStr.replace(/。/g, ".");
      }
      if (nextInputStr !== inputStr) {
        collectInputValue(nextInputStr);
      }
    });
  };
  const onCompositionStart = () => {
    compositionRef.current = true;
  };
  const onCompositionEnd = () => {
    compositionRef.current = false;
    collectInputValue(inputRef.current.value);
  };
  const onInternalInput = (e) => {
    collectInputValue(e.target.value);
  };
  const onInternalStep = useEvent((up, emitter) => {
    if (up && upDisabled || !up && downDisabled) {
      return;
    }
    userTypingRef.current = false;
    let stepDecimal = getMiniDecimal(shiftKeyRef.current ? getDecupleSteps(step) : step);
    if (!up) {
      stepDecimal = stepDecimal.negate();
    }
    const target = (decimalValue || getMiniDecimal(0)).add(stepDecimal.toString());
    const updatedValue = triggerValueUpdate(target, false);
    onStep?.(getDecimalValue(stringMode, updatedValue), {
      offset: shiftKeyRef.current ? getDecupleSteps(step) : step,
      type: up ? "up" : "down",
      emitter
    });
    inputRef.current?.focus();
  });
  const flushInputValue = (userTyping) => {
    const parsedValue = getMiniDecimal(mergedParser(inputValue));
    let formatValue;
    if (!parsedValue.isNaN()) {
      formatValue = triggerValueUpdate(parsedValue, userTyping);
    } else {
      formatValue = triggerValueUpdate(decimalValue, userTyping);
    }
    if (value !== void 0) {
      setInputValue(decimalValue, false);
    } else if (!formatValue.isNaN()) {
      setInputValue(formatValue, false);
    }
  };
  const onBeforeInput = () => {
    userTypingRef.current = true;
  };
  const onKeyDown = (event) => {
    const {
      key,
      shiftKey
    } = event;
    userTypingRef.current = true;
    shiftKeyRef.current = shiftKey;
    if (key === "Enter") {
      if (!compositionRef.current) {
        userTypingRef.current = false;
      }
      flushInputValue(false);
      onPressEnter?.(event);
    }
    if (keyboard === false) {
      return;
    }
    if (!compositionRef.current && ["Up", "ArrowUp", "Down", "ArrowDown"].includes(key)) {
      onInternalStep(key === "Up" || key === "ArrowUp", "keyboard");
      event.preventDefault();
    }
  };
  const onKeyUp = () => {
    userTypingRef.current = false;
    shiftKeyRef.current = false;
  };
  reactExports.useEffect(() => {
    if (changeOnWheel && focus) {
      const onWheel = (event) => {
        onInternalStep(event.deltaY < 0, "wheel");
        event.preventDefault();
      };
      const input = inputRef.current;
      if (input) {
        input.addEventListener("wheel", onWheel, {
          passive: false
        });
        return () => input.removeEventListener("wheel", onWheel);
      }
    }
  });
  const onBlur = () => {
    if (changeOnBlur) {
      flushInputValue(false);
    }
    setFocus(false);
    userTypingRef.current = false;
  };
  const onInternalMouseDown = (event) => {
    if (inputRef.current && event.target !== inputRef.current) {
      inputRef.current.focus();
      event.preventDefault();
    }
    onMouseDown?.(event);
  };
  useLayoutUpdateEffect(() => {
    if (!decimalValue.isInvalidate()) {
      setInputValue(decimalValue, false);
    }
  }, [precision, formatter]);
  useLayoutUpdateEffect(() => {
    const newValue = getMiniDecimal(value);
    setDecimalValue(newValue);
    const currentParsedValue = getMiniDecimal(mergedParser(inputValue));
    if (!newValue.equals(currentParsedValue) || !userTypingRef.current || formatter) {
      setInputValue(newValue, userTypingRef.current);
    }
  }, [value]);
  useLayoutUpdateEffect(() => {
    if (formatter) {
      restoreCursor();
    }
  }, [inputValue]);
  const sharedHandlerProps = {
    prefixCls,
    onStep: onInternalStep,
    className: classNames?.action,
    style: styles?.action
  };
  const upNode = /* @__PURE__ */ reactExports.createElement(StepHandler, _extends$2({}, sharedHandlerProps, {
    action: "up",
    disabled: upDisabled
  }), upHandler);
  const downNode = /* @__PURE__ */ reactExports.createElement(StepHandler, _extends$2({}, sharedHandlerProps, {
    action: "down",
    disabled: downDisabled
  }), downHandler);
  return /* @__PURE__ */ reactExports.createElement("div", {
    ref: rootRef,
    className: clsx(prefixCls, `${prefixCls}-mode-${mode}`, className, classNames?.root, {
      [`${prefixCls}-focused`]: focus,
      [`${prefixCls}-disabled`]: disabled,
      [`${prefixCls}-readonly`]: readOnly,
      [`${prefixCls}-not-a-number`]: decimalValue.isNaN(),
      [`${prefixCls}-out-of-range`]: !decimalValue.isInvalidate() && !isInRange(decimalValue)
    }),
    style: {
      ...styles?.root,
      ...style
    },
    onMouseDown: onInternalMouseDown,
    onMouseUp,
    onMouseLeave,
    onMouseMove,
    onMouseEnter,
    onMouseOut,
    onClick,
    onFocus: () => {
      setFocus(true);
    },
    onBlur,
    onKeyDown,
    onKeyUp,
    onCompositionStart,
    onCompositionEnd,
    onBeforeInput
  }, mode === "spinner" && controls && downNode, prefix !== void 0 && /* @__PURE__ */ reactExports.createElement("div", {
    className: clsx(`${prefixCls}-prefix`, classNames?.prefix),
    style: styles?.prefix
  }, prefix), /* @__PURE__ */ reactExports.createElement("input", _extends$2({
    autoComplete: "off",
    role: "spinbutton",
    "aria-valuemin": min,
    "aria-valuemax": max,
    "aria-valuenow": decimalValue.isInvalidate() ? null : decimalValue.toString(),
    step,
    ref: inputRef,
    className: clsx(`${prefixCls}-input`, classNames?.input),
    style: styles?.input,
    value: inputValue,
    onChange: onInternalInput,
    disabled,
    readOnly
  }, restProps)), suffix !== void 0 && /* @__PURE__ */ reactExports.createElement("div", {
    className: clsx(`${prefixCls}-suffix`, classNames?.suffix),
    style: styles?.suffix
  }, suffix), mode === "spinner" && controls && upNode, mode === "input" && controls && /* @__PURE__ */ reactExports.createElement("div", {
    className: clsx(`${prefixCls}-actions`, classNames?.actions),
    style: styles?.actions
  }, upNode, downNode));
});
const prepareComponentToken$1 = (token) => {
  const handleVisible = token.handleVisible ?? "auto";
  const handleWidth = token.controlHeightSM - token.lineWidth * 2;
  return {
    ...initComponentToken(token),
    controlWidth: 90,
    handleWidth,
    handleFontSize: token.fontSize / 2,
    handleVisible,
    handleActiveBg: token.colorFillAlter,
    handleBg: token.colorBgContainer,
    filledHandleBg: new FastColor(token.colorFillSecondary).onBackground(token.colorBgContainer).toHexString(),
    handleHoverColor: token.colorPrimary,
    handleBorderColor: token.colorBorder,
    handleOpacity: handleVisible === true ? 1 : 0,
    handleVisibleWidth: handleVisible === true ? handleWidth : 0
  };
};
const genInputNumberStyles = (token) => {
  const {
    componentCls,
    lineWidth,
    lineType,
    borderRadius,
    inputFontSizeSM,
    inputFontSizeLG,
    colorError,
    paddingInlineSM,
    paddingBlockSM,
    paddingBlockLG,
    paddingInlineLG,
    colorIcon,
    colorTextDisabled,
    motionDurationMid,
    handleHoverColor,
    handleOpacity,
    paddingInline,
    paddingBlock,
    handleBg,
    handleActiveBg,
    inputAffixPadding,
    borderRadiusSM,
    controlWidth,
    handleBorderColor,
    filledHandleBg,
    lineHeightLG,
    antCls
  } = token;
  const borderStyle = `${unit(lineWidth)} ${lineType} ${handleBorderColor}`;
  const [varName, varRef] = genCssVar(antCls, "input-number");
  return [
    // ==========================================================
    // ==                         Base                         ==
    // ==========================================================
    {
      [componentCls]: {
        ...resetComponent(token),
        ...genBasicInputStyle(token),
        [varName("input-padding-block")]: unit(paddingBlock),
        [varName("input-padding-inline")]: unit(paddingInline),
        display: "inline-flex",
        width: controlWidth,
        margin: 0,
        paddingBlock: 0,
        borderRadius,
        // ======================= Variants =======================
        ...genOutlinedStyle(token, {
          [`${componentCls}-actions`]: {
            background: handleBg,
            [`${componentCls}-action-down`]: {
              borderBlockStart: borderStyle
            }
          }
        }),
        ...genFilledStyle(token, {
          [`${componentCls}-actions`]: {
            background: filledHandleBg,
            [`${componentCls}-action-down`]: {
              borderBlockStart: borderStyle
            }
          },
          "&:focus-within": {
            [`${componentCls}-actions`]: {
              background: handleBg
            }
          }
        }),
        ...genUnderlinedStyle(token, {
          [`${componentCls}-actions`]: {
            background: handleBg,
            [`${componentCls}-action-down`]: {
              borderBlockStart: borderStyle
            }
          }
        }),
        ...genBorderlessStyle(token),
        // InputNumber 两层结构：borderless 补偿只加在内层 input 的 CSS 变量上，避免外层+内层双重 padding 导致高度异常
        [`&${componentCls}-borderless`]: {
          paddingBlock: 0,
          [varName("input-padding-block")]: unit(token.calc(paddingBlock).add(lineWidth).equal())
        },
        [`&${componentCls}-borderless${componentCls}-sm`]: {
          paddingBlock: 0,
          [varName("input-padding-block")]: unit(token.calc(paddingBlockSM).add(lineWidth).equal())
        },
        [`&${componentCls}-borderless${componentCls}-lg`]: {
          paddingBlock: 0,
          [varName("input-padding-block")]: unit(token.calc(paddingBlockLG).add(lineWidth).equal())
        },
        // ========================= RTL ==========================
        "&-rtl": {
          direction: "rtl",
          [`${componentCls}-input`]: {
            direction: "rtl"
          }
        },
        // ===================== Out Of Range =====================
        [`&${componentCls}-out-of-range`]: {
          [`${componentCls}-input`]: {
            color: colorError
          }
        },
        // ======================== Input =========================
        [`${componentCls}-input`]: {
          ...resetComponent(token),
          width: "100%",
          paddingBlock: varRef("input-padding-block"),
          textAlign: "start",
          backgroundColor: "transparent",
          border: 0,
          borderRadius: 0,
          outline: 0,
          transition: `all ${motionDurationMid} linear`,
          appearance: "textfield",
          fontSize: "inherit",
          lineHeight: "inherit",
          ...genPlaceholderStyle(token.colorTextPlaceholder),
          '&[type="number"]::-webkit-inner-spin-button, &[type="number"]::-webkit-outer-spin-button': {
            margin: 0,
            appearance: "none"
          }
        },
        [`&:hover ${componentCls}-handler-wrap, &-focused ${componentCls}-handler-wrap`]: {
          width: token.handleWidth,
          opacity: 1
        },
        // ======================= Disabled =======================
        [`&-disabled ${componentCls}-input`]: {
          cursor: "not-allowed",
          color: token.colorTextDisabled
        }
      }
    },
    // ==========================================================
    // ==                        Action                        ==
    // ==========================================================
    {
      [componentCls]: {
        // ======================= Shared =======================
        [`${componentCls}-action`]: {
          ...resetIcon(),
          userSelect: "none",
          overflow: "hidden",
          fontWeight: "bold",
          lineHeight: 0,
          textAlign: "center",
          cursor: "pointer",
          transition: `all ${motionDurationMid} linear`,
          // Active: change background not disabled only;
          [`&:active:not(${componentCls}-action-up-disabled):not(${componentCls}-action-down-disabled)`]: {
            background: handleActiveBg
          },
          // Hover: change color not disabled only;
          [`&:hover:not(${componentCls}-action-up-disabled):not(${componentCls}-action-down-disabled)`]: {
            color: handleHoverColor
          },
          [`&${componentCls}-action-up-disabled, &${componentCls}-action-down-disabled`]: {
            cursor: "not-allowed",
            color: colorTextDisabled
          }
        },
        // ===================== Input Mode =====================
        "&-mode-input": {
          overflow: "hidden",
          [`${componentCls}-actions`]: {
            position: "absolute",
            insetBlockStart: 0,
            insetInlineEnd: 0,
            width: token.handleVisibleWidth,
            opacity: handleOpacity,
            height: "100%",
            borderRadius: 0,
            display: "flex",
            flexDirection: "column",
            alignItems: "stretch",
            transition: `all ${motionDurationMid}`,
            overflow: "hidden",
            // Fix input number inside Menu makes icon too large
            // We arise the selector priority by nest selector here
            // https://github.com/ant-design/ant-design/issues/14367
            [`${componentCls}-action`]: {
              display: "flex",
              alignItems: "center",
              justifyContent: "center",
              flex: "auto",
              height: "40%",
              marginInlineEnd: 0,
              fontSize: token.handleFontSize
            }
          },
          [`&:hover ${componentCls}-actions, &-focused ${componentCls}-actions`]: {
            width: token.handleWidth,
            opacity: 1
          },
          [`${componentCls}-action`]: {
            color: colorIcon,
            height: "50%",
            borderInlineStart: borderStyle,
            // Hover: change height not disabled only;
            [`&:hover:not(${componentCls}-action-up-disabled):not(${componentCls}-action-down-disabled)`]: {
              height: `60%`
            }
          },
          [`&${componentCls}-disabled, &${componentCls}-readonly`]: {
            [`${componentCls}-actions`]: {
              display: "none"
            }
          }
        },
        // ==================== Spinner Mode ====================
        [`&${componentCls}-mode-spinner`]: {
          padding: 0,
          width: "auto",
          [`${componentCls}-action`]: {
            flex: "none",
            paddingInline: varRef("input-padding-inline"),
            "&-up": {
              borderInlineStart: borderStyle
            },
            "&-down": {
              borderInlineEnd: borderStyle
            }
          },
          [`${componentCls}-input`]: {
            textAlign: "center",
            paddingInline: varRef("input-padding-inline")
          }
        }
      }
    },
    // ==========================================================
    // ==                         Size                         ==
    // ==========================================================
    {
      [componentCls]: {
        "&-lg": {
          [varName("input-padding-block")]: unit(paddingBlockLG),
          [varName("input-padding-inline")]: unit(paddingInlineLG),
          paddingBlock: 0,
          fontSize: inputFontSizeLG,
          lineHeight: lineHeightLG
        },
        "&-sm": {
          [varName("input-padding-block")]: unit(paddingBlockSM),
          [varName("input-padding-inline")]: unit(paddingInlineSM),
          paddingBlock: 0,
          fontSize: inputFontSizeSM,
          borderRadius: borderRadiusSM
        }
      }
    },
    // ==========================================================
    // ==                      Pre/Suffix                      ==
    // ==========================================================
    {
      [componentCls]: {
        [`${componentCls}-prefix, ${componentCls}-suffix`]: {
          display: "flex",
          flex: "none",
          alignItems: "center",
          alignSelf: "center",
          pointerEvents: "none"
        },
        [`${componentCls}-prefix`]: {
          marginInlineEnd: inputAffixPadding
        },
        [`${componentCls}-suffix`]: {
          height: "100%",
          marginInlineStart: inputAffixPadding,
          transition: `margin ${motionDurationMid}`
        },
        [`&:hover:not(${componentCls}-without-controls)`]: {
          [`${componentCls}-suffix`]: {
            marginInlineEnd: token.handleWidth
          }
        }
      }
    }
  ];
};
const genCompatibleStyles = (token) => {
  const {
    componentCls,
    antCls
  } = token;
  return {
    [`${componentCls}-addon`]: {
      [`&:has(${antCls}-select)`]: {
        border: 0,
        padding: 0
      }
    }
  };
};
const useStyle$2 = genStyleHooks("InputNumber", (token) => {
  const inputNumberToken = merge(token, initInputToken(token));
  return [
    genInputNumberStyles(inputNumberToken),
    genCompatibleStyles(inputNumberToken),
    // =====================================================
    // ==             Space Compact                       ==
    // =====================================================
    genCompactItemStyle(inputNumberToken)
  ];
}, prepareComponentToken$1, {
  unitless: {
    handleOpacity: true
  },
  resetFont: false
});
const InternalInputNumber = /* @__PURE__ */ reactExports.forwardRef((props, ref) => {
  const inputRef = reactExports.useRef(null);
  reactExports.useImperativeHandle(ref, () => inputRef.current);
  const {
    rootClassName,
    size: customizeSize,
    disabled: customDisabled,
    prefixCls,
    addonBefore: _addonBefore,
    addonAfter: _addonAfter,
    prefix,
    suffix,
    bordered,
    readOnly,
    status,
    controls = true,
    variant: customVariant,
    className,
    style,
    classNames,
    styles,
    mode,
    ...others
  } = props;
  const {
    direction,
    className: contextClassName,
    style: contextStyle,
    styles: contextStyles,
    classNames: contextClassNames
  } = useComponentConfig("inputNumber");
  const disabled = reactExports.useContext(DisabledContext);
  const mergedDisabled = customDisabled ?? disabled;
  const mergedControls = reactExports.useMemo(() => {
    if (!controls || mergedDisabled || readOnly) {
      return false;
    }
    return controls;
  }, [controls, mergedDisabled, readOnly]);
  const {
    compactSize,
    compactItemClassnames
  } = useCompactItemContext(prefixCls, direction);
  let upIcon = mode === "spinner" ? /* @__PURE__ */ reactExports.createElement(RefIcon$1, null) : /* @__PURE__ */ reactExports.createElement(RefIcon, null);
  let downIcon = mode === "spinner" ? /* @__PURE__ */ reactExports.createElement(RefIcon$2, null) : /* @__PURE__ */ reactExports.createElement(RefIcon$3, null);
  const controlsTemp = typeof mergedControls === "boolean" ? mergedControls : void 0;
  if (isPlainObject(mergedControls)) {
    upIcon = mergedControls.upIcon || upIcon;
    downIcon = mergedControls.downIcon || downIcon;
  }
  const {
    hasFeedback,
    isFormItemInput,
    feedbackIcon
  } = reactExports.useContext(FormItemInputContext);
  const mergedSize = useSize((ctx) => customizeSize ?? compactSize ?? ctx);
  const [variant, enableVariantCls] = useVariant("inputNumber", customVariant, bordered);
  const suffixNode = (hasFeedback || suffix) && /* @__PURE__ */ reactExports.createElement(reactExports.Fragment, null, suffix, hasFeedback && feedbackIcon);
  const mergedProps = {
    ...props,
    size: mergedSize,
    disabled: mergedDisabled,
    controls: mergedControls
  };
  const contextStyleRoot = useSemanticRootStyle(contextStyle);
  const styleRoot = useSemanticRootStyle(style);
  const [mergedClassNames, mergedStyles] = useMergeSemantic([contextClassNames, classNames], [contextStyles, contextStyleRoot, styles, styleRoot], {
    props: mergedProps
  });
  return /* @__PURE__ */ reactExports.createElement(InputNumber$1, {
    ref: inputRef,
    mode,
    disabled: mergedDisabled,
    className: clsx(className, rootClassName, mergedClassNames.root, contextClassName, compactItemClassnames, getStatusClassNames(prefixCls, status, hasFeedback), {
      [`${prefixCls}-${variant}`]: enableVariantCls,
      [`${prefixCls}-lg`]: mergedSize === "large",
      [`${prefixCls}-sm`]: mergedSize === "small",
      [`${prefixCls}-rtl`]: direction === "rtl",
      [`${prefixCls}-in-form-item`]: isFormItemInput,
      [`${prefixCls}-without-controls`]: !mergedControls
    }),
    style: mergedStyles.root,
    upHandler: upIcon,
    downHandler: downIcon,
    prefixCls,
    readOnly,
    controls: controlsTemp,
    prefix,
    suffix: suffixNode,
    classNames: mergedClassNames,
    styles: mergedStyles,
    ...others
  });
});
const InputNumber = /* @__PURE__ */ reactExports.forwardRef((props, ref) => {
  const {
    addonBefore,
    addonAfter,
    prefixCls: customizePrefixCls,
    className,
    status: customStatus,
    rootClassName,
    ...rest
  } = props;
  const {
    getPrefixCls
  } = useComponentConfig("inputNumber");
  const prefixCls = getPrefixCls("input-number", customizePrefixCls);
  const {
    status: contextStatus
  } = reactExports.useContext(FormItemInputContext);
  const mergedStatus = getMergedStatus(contextStatus, customStatus);
  const rootCls = useCSSVarCls(prefixCls);
  const [hashId, cssVarCls] = useStyle$2(prefixCls, rootCls);
  const hasLegacyAddon = addonBefore || addonAfter;
  const inputNumberNode = /* @__PURE__ */ reactExports.createElement(InternalInputNumber, {
    ref,
    ...rest,
    prefixCls,
    status: mergedStatus,
    className: clsx(cssVarCls, rootCls, hashId, className),
    rootClassName: !hasLegacyAddon ? rootClassName : void 0
  });
  if (hasLegacyAddon) {
    const renderAddon = (node) => {
      if (!node) {
        return null;
      }
      return /* @__PURE__ */ reactExports.createElement(SpaceAddon, {
        className: clsx(`${prefixCls}-addon`, cssVarCls, hashId),
        variant: props.variant,
        disabled: props.disabled,
        status: mergedStatus
      }, /* @__PURE__ */ reactExports.createElement(ContextIsolator, {
        form: true
      }, node));
    };
    const addonBeforeNode = renderAddon(addonBefore);
    const addonAfterNode = renderAddon(addonAfter);
    return /* @__PURE__ */ reactExports.createElement(Compact, {
      rootClassName
    }, addonBeforeNode, inputNumberNode, addonAfterNode);
  }
  return inputNumberNode;
});
const TypedInputNumber = InputNumber;
const PureInputNumber = (props) => /* @__PURE__ */ reactExports.createElement(ConfigProvider, {
  theme: {
    components: {
      InputNumber: {
        handleVisible: true
      }
    }
  }
}, /* @__PURE__ */ reactExports.createElement(InputNumber, {
  ...props
}));
TypedInputNumber._InternalPanelDoNotUseOrYouWillBeFired = PureInputNumber;
const ColorSteppers = ({
  prefixCls,
  min = 0,
  max = 100,
  value,
  onChange,
  className,
  formatter
}) => {
  const colorSteppersPrefixCls = `${prefixCls}-steppers`;
  const [internalValue, setInternalValue] = reactExports.useState(0);
  const stepValue = !Number.isNaN(value) ? value : internalValue;
  return /* @__PURE__ */ React.createElement(TypedInputNumber, {
    className: clsx(colorSteppersPrefixCls, className),
    min,
    max,
    value: stepValue,
    formatter,
    size: "small",
    onChange: (step) => {
      setInternalValue(step || 0);
      onChange?.(step);
    }
  });
};
const ColorAlphaInput = ({
  prefixCls,
  value,
  onChange
}) => {
  const colorAlphaInputPrefixCls = `${prefixCls}-alpha-input`;
  const [internalValue, setInternalValue] = reactExports.useState(() => generateColor$1(value || "#000"));
  const alphaValue = value || internalValue;
  const handleAlphaChange = (step) => {
    const hsba = alphaValue.toHsb();
    hsba.a = (step || 0) / 100;
    const genColor = generateColor$1(hsba);
    setInternalValue(genColor);
    onChange?.(genColor);
  };
  return /* @__PURE__ */ React.createElement(ColorSteppers, {
    value: getColorAlpha(alphaValue),
    prefixCls,
    formatter: (step) => `${step}%`,
    className: colorAlphaInputPrefixCls,
    onChange: handleAlphaChange
  });
};
const hexReg = /(^#[\da-f]{6}$)|(^#[\da-f]{8}$)/i;
const isHexString = (hex) => hexReg.test(`#${hex}`);
const ColorHexInput = ({
  prefixCls,
  value,
  onChange
}) => {
  const colorHexInputPrefixCls = `${prefixCls}-hex-input`;
  const [hexValue, setHexValue] = reactExports.useState(() => value ? toHexFormat(value.toHexString()) : void 0);
  reactExports.useEffect(() => {
    if (value) {
      setHexValue(toHexFormat(value.toHexString()));
    }
  }, [value]);
  const handleHexChange = (e) => {
    const originValue = e.target.value;
    setHexValue(toHexFormat(originValue));
    if (isHexString(toHexFormat(originValue, true))) {
      onChange?.(generateColor$1(originValue));
    }
  };
  return /* @__PURE__ */ React.createElement(Input, {
    className: colorHexInputPrefixCls,
    value: hexValue,
    prefix: "#",
    onChange: handleHexChange,
    size: "small"
  });
};
const ColorHsbInput = ({
  prefixCls,
  value,
  onChange
}) => {
  const colorHsbInputPrefixCls = `${prefixCls}-hsb-input`;
  const [internalValue, setInternalValue] = reactExports.useState(() => generateColor$1(value || "#000"));
  const hsbValue = value || internalValue;
  const handleHsbChange = (step, type) => {
    const hsb = hsbValue.toHsb();
    hsb[type] = type === "h" ? step : (step || 0) / 100;
    const genColor = generateColor$1(hsb);
    setInternalValue(genColor);
    onChange?.(genColor);
  };
  return /* @__PURE__ */ React.createElement("div", {
    className: colorHsbInputPrefixCls
  }, /* @__PURE__ */ React.createElement(ColorSteppers, {
    max: 360,
    min: 0,
    value: Number(hsbValue.toHsb().h),
    prefixCls,
    className: colorHsbInputPrefixCls,
    formatter: (step) => getRoundNumber(step || 0).toString(),
    onChange: (step) => handleHsbChange(Number(step), "h")
  }), /* @__PURE__ */ React.createElement(ColorSteppers, {
    max: 100,
    min: 0,
    value: Number(hsbValue.toHsb().s) * 100,
    prefixCls,
    className: colorHsbInputPrefixCls,
    formatter: (step) => `${getRoundNumber(step || 0)}%`,
    onChange: (step) => handleHsbChange(Number(step), "s")
  }), /* @__PURE__ */ React.createElement(ColorSteppers, {
    max: 100,
    min: 0,
    value: Number(hsbValue.toHsb().b) * 100,
    prefixCls,
    className: colorHsbInputPrefixCls,
    formatter: (step) => `${getRoundNumber(step || 0)}%`,
    onChange: (step) => handleHsbChange(Number(step), "b")
  }));
};
const ColorRgbInput = ({
  prefixCls,
  value,
  onChange
}) => {
  const colorRgbInputPrefixCls = `${prefixCls}-rgb-input`;
  const [internalValue, setInternalValue] = reactExports.useState(() => generateColor$1(value || "#000"));
  const rgbValue = value || internalValue;
  const handleRgbChange = (step, type) => {
    const rgb = rgbValue.toRgb();
    rgb[type] = step || 0;
    const genColor = generateColor$1(rgb);
    setInternalValue(genColor);
    onChange?.(genColor);
  };
  return /* @__PURE__ */ React.createElement("div", {
    className: colorRgbInputPrefixCls
  }, /* @__PURE__ */ React.createElement(ColorSteppers, {
    max: 255,
    min: 0,
    value: Number(rgbValue.toRgb().r),
    prefixCls,
    className: colorRgbInputPrefixCls,
    onChange: (step) => handleRgbChange(Number(step), "r")
  }), /* @__PURE__ */ React.createElement(ColorSteppers, {
    max: 255,
    min: 0,
    value: Number(rgbValue.toRgb().g),
    prefixCls,
    className: colorRgbInputPrefixCls,
    onChange: (step) => handleRgbChange(Number(step), "g")
  }), /* @__PURE__ */ React.createElement(ColorSteppers, {
    max: 255,
    min: 0,
    value: Number(rgbValue.toRgb().b),
    prefixCls,
    className: colorRgbInputPrefixCls,
    onChange: (step) => handleRgbChange(Number(step), "b")
  }));
};
const selectOptions = [FORMAT_HEX, FORMAT_HSB, FORMAT_RGB].map((format) => ({
  value: format,
  label: format.toUpperCase()
}));
const ColorInput = (props) => {
  const {
    prefixCls,
    format,
    value,
    disabledAlpha,
    onFormatChange,
    onChange,
    disabledFormat
  } = props;
  const [colorFormat, setColorFormat] = useControlledState(FORMAT_HEX, format);
  const colorInputPrefixCls = `${prefixCls}-input`;
  const triggerFormatChange = (newFormat) => {
    setColorFormat(newFormat);
    onFormatChange?.(newFormat);
  };
  const steppersNode = reactExports.useMemo(() => {
    const inputProps = {
      value,
      prefixCls,
      onChange
    };
    switch (colorFormat) {
      case FORMAT_HSB:
        return /* @__PURE__ */ React.createElement(ColorHsbInput, {
          ...inputProps
        });
      case FORMAT_RGB:
        return /* @__PURE__ */ React.createElement(ColorRgbInput, {
          ...inputProps
        });
      // case FORMAT_HEX:
      default:
        return /* @__PURE__ */ React.createElement(ColorHexInput, {
          ...inputProps
        });
    }
  }, [colorFormat, prefixCls, value, onChange]);
  return /* @__PURE__ */ React.createElement("div", {
    className: `${colorInputPrefixCls}-container`
  }, !disabledFormat && /* @__PURE__ */ React.createElement(Select, {
    value: colorFormat,
    variant: "borderless",
    getPopupContainer: (current) => current,
    popupMatchSelectWidth: 68,
    placement: "bottomRight",
    onChange: triggerFormatChange,
    className: `${prefixCls}-format-select`,
    size: "small",
    options: selectOptions
  }), /* @__PURE__ */ React.createElement("div", {
    className: colorInputPrefixCls
  }, steppersNode), !disabledAlpha && /* @__PURE__ */ React.createElement(ColorAlphaInput, {
    prefixCls,
    value,
    onChange
  }));
};
function getOffset(value, min, max) {
  return (value - min) / (max - min);
}
function getDirectionStyle(direction, value, min, max) {
  const offset = getOffset(value, min, max);
  const positionStyle = {};
  switch (direction) {
    case "rtl":
      positionStyle.right = `${offset * 100}%`;
      positionStyle.transform = "translateX(50%)";
      break;
    case "btt":
      positionStyle.bottom = `${offset * 100}%`;
      positionStyle.transform = "translateY(50%)";
      break;
    case "ttb":
      positionStyle.top = `${offset * 100}%`;
      positionStyle.transform = "translateY(-50%)";
      break;
    default:
      positionStyle.left = `${offset * 100}%`;
      positionStyle.transform = "translateX(-50%)";
      break;
  }
  return positionStyle;
}
function getIndex(value, index) {
  return Array.isArray(value) ? value[index] : value;
}
const SliderContext = /* @__PURE__ */ reactExports.createContext({
  min: 0,
  max: 0,
  direction: "ltr",
  step: 1,
  includedStart: 0,
  includedEnd: 0,
  tabIndex: 0,
  keyboard: true,
  styles: {},
  classNames: {},
  isHandleDisabled: () => false
});
const UnstableContext = /* @__PURE__ */ reactExports.createContext({});
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
const Handle = /* @__PURE__ */ reactExports.forwardRef((props, ref) => {
  const {
    prefixCls,
    value,
    valueIndex,
    onStartMove,
    onDelete,
    style,
    render,
    dragging,
    draggingDelete,
    onOffsetChange,
    onChangeComplete,
    onFocus,
    onMouseEnter,
    ...restProps
  } = props;
  const {
    min,
    max,
    direction,
    keyboard,
    range,
    tabIndex,
    ariaLabelForHandle,
    ariaLabelledByForHandle,
    ariaRequired,
    ariaValueTextFormatterForHandle,
    styles,
    classNames,
    isHandleDisabled
  } = reactExports.useContext(SliderContext);
  const mergedDisabled = isHandleDisabled(valueIndex);
  const handlePrefixCls = `${prefixCls}-handle`;
  const onInternalStartMove = (e) => {
    if (mergedDisabled) {
      e.stopPropagation();
      return;
    }
    onStartMove(e, valueIndex);
  };
  const onInternalFocus = (e) => {
    onFocus?.(e, valueIndex);
  };
  const onInternalMouseEnter = (e) => {
    onMouseEnter(e, valueIndex);
  };
  const onKeyDown = (e) => {
    if (!mergedDisabled && keyboard) {
      let offset;
      switch (e.which || e.keyCode) {
        case KeyCode.LEFT:
          offset = direction === "ltr" || direction === "btt" ? -1 : 1;
          break;
        case KeyCode.RIGHT:
          offset = direction === "ltr" || direction === "btt" ? 1 : -1;
          break;
        // Up is plus
        case KeyCode.UP:
          offset = direction !== "ttb" ? 1 : -1;
          break;
        // Down is minus
        case KeyCode.DOWN:
          offset = direction !== "ttb" ? -1 : 1;
          break;
        case KeyCode.HOME:
          offset = "min";
          break;
        case KeyCode.END:
          offset = "max";
          break;
        case KeyCode.PAGE_UP:
          offset = 2;
          break;
        case KeyCode.PAGE_DOWN:
          offset = -2;
          break;
        case KeyCode.BACKSPACE:
        case KeyCode.DELETE:
          onDelete?.(valueIndex);
          break;
      }
      if (offset !== void 0) {
        e.preventDefault();
        onOffsetChange(offset, valueIndex);
      }
    }
  };
  const handleKeyUp = (e) => {
    switch (e.which || e.keyCode) {
      case KeyCode.LEFT:
      case KeyCode.RIGHT:
      case KeyCode.UP:
      case KeyCode.DOWN:
      case KeyCode.HOME:
      case KeyCode.END:
      case KeyCode.PAGE_UP:
      case KeyCode.PAGE_DOWN:
        onChangeComplete?.();
        break;
    }
  };
  const positionStyle = getDirectionStyle(direction, value, min, max);
  let divProps = {};
  if (valueIndex !== null) {
    divProps = {
      tabIndex: mergedDisabled ? void 0 : getIndex(tabIndex, valueIndex) ?? void 0,
      role: "slider",
      "aria-valuemin": min,
      "aria-valuemax": max,
      "aria-valuenow": value,
      "aria-disabled": mergedDisabled,
      "aria-label": getIndex(ariaLabelForHandle, valueIndex),
      "aria-labelledby": getIndex(ariaLabelledByForHandle, valueIndex),
      "aria-required": getIndex(ariaRequired, valueIndex),
      "aria-valuetext": getIndex(ariaValueTextFormatterForHandle, valueIndex)?.(value),
      "aria-orientation": direction === "ltr" || direction === "rtl" ? "horizontal" : "vertical",
      onMouseDown: onInternalStartMove,
      onTouchStart: onInternalStartMove,
      onFocus: onInternalFocus,
      onMouseEnter: onInternalMouseEnter,
      onKeyDown,
      onKeyUp: handleKeyUp
    };
  }
  let handleNode = /* @__PURE__ */ reactExports.createElement("div", _extends$1({
    ref,
    className: clsx(handlePrefixCls, {
      [`${handlePrefixCls}-${valueIndex + 1}`]: valueIndex !== null && range,
      [`${handlePrefixCls}-dragging`]: dragging,
      [`${handlePrefixCls}-dragging-delete`]: draggingDelete,
      [`${handlePrefixCls}-disabled`]: mergedDisabled
    }, classNames.handle),
    style: {
      ...positionStyle,
      ...style,
      ...styles.handle
    }
  }, divProps, restProps));
  if (render) {
    handleNode = render(handleNode, {
      index: valueIndex,
      prefixCls,
      value,
      dragging,
      draggingDelete
    });
  }
  return handleNode;
});
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
const Handles = /* @__PURE__ */ reactExports.forwardRef((props, ref) => {
  const {
    prefixCls,
    style,
    onStartMove,
    onOffsetChange,
    values,
    handleRender,
    activeHandleRender,
    draggingIndex,
    draggingDelete,
    onFocus,
    ...restProps
  } = props;
  const handlesRef = reactExports.useRef({});
  const [activeVisible, setActiveVisible] = reactExports.useState(false);
  const [activeIndex, setActiveIndex] = reactExports.useState(-1);
  const onActive = (index) => {
    setActiveIndex(index);
    setActiveVisible(true);
  };
  const onHandleFocus = (e, index) => {
    onActive(index);
    onFocus?.(e);
  };
  const onHandleMouseEnter = (e, index) => {
    onActive(index);
  };
  reactExports.useImperativeHandle(ref, () => ({
    focus: (index) => {
      handlesRef.current[index]?.focus();
    },
    hideHelp: () => {
      reactDomExports.flushSync(() => {
        setActiveVisible(false);
      });
    }
  }));
  const handleProps = {
    prefixCls,
    onStartMove,
    onOffsetChange,
    render: handleRender,
    onFocus: onHandleFocus,
    onMouseEnter: onHandleMouseEnter,
    ...restProps
  };
  return /* @__PURE__ */ reactExports.createElement(reactExports.Fragment, null, values.map((value, index) => {
    const dragging = draggingIndex === index;
    return /* @__PURE__ */ reactExports.createElement(Handle, _extends({
      ref: (node) => {
        if (!node) {
          delete handlesRef.current[index];
        } else {
          handlesRef.current[index] = node;
        }
      },
      dragging,
      draggingDelete: dragging && draggingDelete,
      style: getIndex(style, index),
      key: index,
      value,
      valueIndex: index
    }, handleProps));
  }), activeHandleRender && activeVisible && /* @__PURE__ */ reactExports.createElement(Handle, _extends({
    key: "a11y"
  }, handleProps, {
    value: values[activeIndex],
    valueIndex: null,
    dragging: draggingIndex !== -1,
    draggingDelete,
    render: activeHandleRender,
    style: {
      pointerEvents: "none"
    },
    tabIndex: void 0,
    "aria-hidden": true
  })));
});
const Mark = (props) => {
  const {
    prefixCls,
    style,
    children,
    value,
    onClick
  } = props;
  const {
    min,
    max,
    direction,
    includedStart,
    includedEnd,
    included
  } = reactExports.useContext(SliderContext);
  const textCls = `${prefixCls}-text`;
  const positionStyle = getDirectionStyle(direction, value, min, max);
  return /* @__PURE__ */ reactExports.createElement("span", {
    className: clsx(textCls, {
      [`${textCls}-active`]: included && includedStart <= value && value <= includedEnd
    }),
    style: {
      ...positionStyle,
      ...style
    },
    onMouseDown: (e) => {
      e.stopPropagation();
    },
    onClick: () => {
      onClick(value);
    }
  }, children);
};
const Marks = (props) => {
  const {
    prefixCls,
    marks = [],
    onClick
  } = props;
  const markPrefixCls = `${prefixCls}-mark`;
  if (!marks.length) {
    return null;
  }
  return /* @__PURE__ */ reactExports.createElement("div", {
    className: markPrefixCls
  }, marks.map(({
    value,
    style,
    label
  }) => /* @__PURE__ */ reactExports.createElement(Mark, {
    key: value,
    prefixCls: markPrefixCls,
    style,
    value,
    onClick
  }, label)));
};
const Dot = (props) => {
  const {
    prefixCls,
    value,
    style,
    activeStyle
  } = props;
  const {
    min,
    max,
    direction,
    included,
    includedStart,
    includedEnd
  } = reactExports.useContext(SliderContext);
  const dotClassName = `${prefixCls}-dot`;
  const active = included && includedStart <= value && value <= includedEnd;
  let mergedStyle = {
    ...getDirectionStyle(direction, value, min, max),
    ...typeof style === "function" ? style(value) : style
  };
  if (active) {
    mergedStyle = {
      ...mergedStyle,
      ...typeof activeStyle === "function" ? activeStyle(value) : activeStyle
    };
  }
  return /* @__PURE__ */ reactExports.createElement("span", {
    className: clsx(dotClassName, {
      [`${dotClassName}-active`]: active
    }),
    style: mergedStyle
  });
};
const Steps = (props) => {
  const {
    prefixCls,
    marks,
    dots,
    style,
    activeStyle
  } = props;
  const {
    min,
    max,
    step
  } = reactExports.useContext(SliderContext);
  const stepDots = reactExports.useMemo(() => {
    const dotSet = /* @__PURE__ */ new Set();
    marks.forEach((mark) => {
      dotSet.add(mark.value);
    });
    if (dots && step !== null) {
      let current = min;
      while (current <= max) {
        dotSet.add(current);
        current += step;
      }
    }
    return Array.from(dotSet);
  }, [min, max, step, dots, marks]);
  return /* @__PURE__ */ reactExports.createElement("div", {
    className: `${prefixCls}-step`
  }, stepDots.map((dotValue) => /* @__PURE__ */ reactExports.createElement(Dot, {
    prefixCls,
    key: dotValue,
    value: dotValue,
    style,
    activeStyle
  })));
};
const Track = (props) => {
  const {
    prefixCls,
    style,
    start,
    end,
    index,
    onStartMove,
    replaceCls
  } = props;
  const {
    direction,
    min,
    max,
    disabled,
    range,
    classNames
  } = reactExports.useContext(SliderContext);
  const trackPrefixCls = `${prefixCls}-track`;
  const offsetStart = getOffset(start, min, max);
  const offsetEnd = getOffset(end, min, max);
  const onInternalStartMove = (e) => {
    if (!disabled && onStartMove) {
      onStartMove(e, -1);
    }
  };
  const positionStyle = {};
  switch (direction) {
    case "rtl":
      positionStyle.right = `${offsetStart * 100}%`;
      positionStyle.width = `${offsetEnd * 100 - offsetStart * 100}%`;
      break;
    case "btt":
      positionStyle.bottom = `${offsetStart * 100}%`;
      positionStyle.height = `${offsetEnd * 100 - offsetStart * 100}%`;
      break;
    case "ttb":
      positionStyle.top = `${offsetStart * 100}%`;
      positionStyle.height = `${offsetEnd * 100 - offsetStart * 100}%`;
      break;
    default:
      positionStyle.left = `${offsetStart * 100}%`;
      positionStyle.width = `${offsetEnd * 100 - offsetStart * 100}%`;
  }
  const className = replaceCls || clsx(trackPrefixCls, {
    [`${trackPrefixCls}-${index + 1}`]: index !== null && range,
    [`${prefixCls}-track-draggable`]: onStartMove
  }, classNames.track);
  return /* @__PURE__ */ reactExports.createElement("div", {
    className,
    style: {
      ...positionStyle,
      ...style
    },
    onMouseDown: onInternalStartMove,
    onTouchStart: onInternalStartMove
  });
};
const Tracks = (props) => {
  const {
    prefixCls,
    style,
    values,
    startPoint,
    onStartMove: propsOnStartMove
  } = props;
  const {
    included,
    range,
    min,
    styles,
    classNames,
    isHandleDisabled
  } = reactExports.useContext(SliderContext);
  const hasDisabledHandle = reactExports.useMemo(() => values.some((_, index) => isHandleDisabled(index)), [isHandleDisabled, values]);
  const onStartMove = hasDisabledHandle ? void 0 : propsOnStartMove;
  const trackList = reactExports.useMemo(() => {
    if (!range) {
      if (values.length === 0) {
        return [];
      }
      const startValue = startPoint ?? min;
      const endValue = values[0];
      return [{
        start: Math.min(startValue, endValue),
        end: Math.max(startValue, endValue)
      }];
    }
    const list = [];
    for (let i = 0; i < values.length - 1; i += 1) {
      list.push({
        start: values[i],
        end: values[i + 1]
      });
    }
    return list;
  }, [values, range, startPoint, min]);
  if (!included) {
    return null;
  }
  const tracksNode = trackList?.length && (classNames.tracks || styles.tracks) ? /* @__PURE__ */ reactExports.createElement(Track, {
    index: null,
    prefixCls,
    start: trackList[0].start,
    end: trackList[trackList.length - 1].end,
    replaceCls: clsx(classNames.tracks, `${prefixCls}-tracks`),
    style: styles.tracks
  }) : null;
  return /* @__PURE__ */ reactExports.createElement(reactExports.Fragment, null, tracksNode, trackList.map(({
    start,
    end
  }, index) => /* @__PURE__ */ reactExports.createElement(Track, {
    index,
    prefixCls,
    style: {
      ...getIndex(style, index),
      ...styles.track
    },
    start,
    end,
    key: index,
    onStartMove
  })));
};
const useDisabled = (rawDisabled) => {
  const isHandleDisabled = reactExports.useCallback((index) => {
    if (typeof rawDisabled === "boolean") {
      return rawDisabled;
    }
    return rawDisabled[index] ?? false;
  }, [rawDisabled]);
  const getDisabledState = reactExports.useCallback((rawValues) => {
    if (typeof rawDisabled === "boolean") {
      return [rawDisabled, rawDisabled && rawValues.length > 0];
    }
    return [rawValues.length > 0 && rawValues.every((_, index) => isHandleDisabled(index)), rawValues.some((_, index) => isHandleDisabled(index))];
  }, [rawDisabled, isHandleDisabled]);
  return reactExports.useMemo(() => [isHandleDisabled, getDisabledState], [isHandleDisabled, getDisabledState]);
};
const REMOVE_DIST = 130;
function getPosition(e) {
  const obj = "targetTouches" in e ? e.targetTouches[0] : e;
  return {
    pageX: obj.pageX,
    pageY: obj.pageY
  };
}
function useDrag(containerRef, direction, rawValues, min, max, formatValue, triggerChange, finishChange, offsetValues, editable, minCount, isHandleDisabled) {
  const [draggingValue, setDraggingValue] = reactExports.useState(null);
  const [draggingIndex, setDraggingIndex] = reactExports.useState(-1);
  const [draggingDelete, setDraggingDelete] = reactExports.useState(false);
  const [cacheValues, setCacheValues] = reactExports.useState(rawValues);
  const [originValues, setOriginValues] = reactExports.useState(rawValues);
  const mouseMoveEventRef = reactExports.useRef(null);
  const mouseUpEventRef = reactExports.useRef(null);
  const touchEventTargetRef = reactExports.useRef(null);
  const {
    onDragStart,
    onDragChange
  } = reactExports.useContext(UnstableContext);
  useLayoutEffect(() => {
    if (draggingIndex === -1) {
      setCacheValues(rawValues);
    }
  }, [rawValues, draggingIndex]);
  reactExports.useEffect(() => () => {
    if (mouseMoveEventRef.current) {
      document.removeEventListener("mousemove", mouseMoveEventRef.current);
    }
    if (mouseUpEventRef.current) {
      document.removeEventListener("mouseup", mouseUpEventRef.current);
    }
    if (touchEventTargetRef.current) {
      if (mouseMoveEventRef.current) {
        touchEventTargetRef.current.removeEventListener("touchmove", mouseMoveEventRef.current);
      }
      if (mouseUpEventRef.current) {
        touchEventTargetRef.current.removeEventListener("touchend", mouseUpEventRef.current);
      }
    }
  }, []);
  const flushValues = (nextValues, nextValue, deleteMark) => {
    if (nextValue !== void 0) {
      setDraggingValue(nextValue);
    }
    setCacheValues(nextValues);
    let changeValues = nextValues;
    if (deleteMark) {
      changeValues = nextValues.filter((_, i) => i !== draggingIndex);
    }
    triggerChange(changeValues);
    if (onDragChange) {
      onDragChange({
        rawValues: nextValues,
        deleteIndex: deleteMark ? draggingIndex : -1,
        draggingIndex,
        draggingValue: nextValue
      });
    }
  };
  const updateCacheValue = useEvent((valueIndex, offsetPercent, deleteMark) => {
    if (valueIndex === -1) {
      if (originValues.some((_, index) => isHandleDisabled(index))) {
        return;
      }
      const startValue = originValues[0];
      const endValue = originValues[originValues.length - 1];
      const maxStartOffset = min - startValue;
      const maxEndOffset = max - endValue;
      let offset = offsetPercent * (max - min);
      offset = Math.max(offset, maxStartOffset);
      offset = Math.min(offset, maxEndOffset);
      const formatStartValue = formatValue(startValue + offset);
      offset = formatStartValue - startValue;
      const cloneCacheValues = originValues.map((val) => val + offset);
      flushValues(cloneCacheValues);
    } else {
      const offsetDist = (max - min) * offsetPercent;
      const cloneValues = [...cacheValues];
      cloneValues[valueIndex] = originValues[valueIndex];
      const next = offsetValues(cloneValues, offsetDist, valueIndex, "dist");
      flushValues(next.values, next.value, deleteMark);
    }
  });
  const onStartMove = (e, valueIndex, startValues) => {
    e.stopPropagation();
    const initialValues = startValues || rawValues;
    if (isHandleDisabled(valueIndex)) {
      return;
    }
    const originValue = initialValues[valueIndex];
    setDraggingIndex(valueIndex);
    setDraggingValue(originValue);
    setOriginValues(initialValues);
    setCacheValues(initialValues);
    setDraggingDelete(false);
    const {
      pageX: startX,
      pageY: startY
    } = getPosition(e);
    let deleteMark = false;
    if (onDragStart) {
      onDragStart({
        rawValues: initialValues,
        draggingIndex: valueIndex,
        draggingValue: originValue
      });
    }
    const onMouseMove = (event) => {
      event.preventDefault();
      const {
        pageX: moveX,
        pageY: moveY
      } = getPosition(event);
      const offsetX = moveX - startX;
      const offsetY = moveY - startY;
      const {
        width,
        height
      } = containerRef.current.getBoundingClientRect();
      let offSetPercent;
      let removeDist;
      switch (direction) {
        case "btt":
          offSetPercent = -offsetY / height;
          removeDist = offsetX;
          break;
        case "ttb":
          offSetPercent = offsetY / height;
          removeDist = offsetX;
          break;
        case "rtl":
          offSetPercent = -offsetX / width;
          removeDist = offsetY;
          break;
        default:
          offSetPercent = offsetX / width;
          removeDist = offsetY;
      }
      deleteMark = editable ? Math.abs(removeDist) > REMOVE_DIST && minCount < cacheValues.length : false;
      setDraggingDelete(deleteMark);
      updateCacheValue(valueIndex, offSetPercent, deleteMark);
    };
    const onMouseUp = (event) => {
      event.preventDefault();
      document.removeEventListener("mouseup", onMouseUp);
      document.removeEventListener("mousemove", onMouseMove);
      if (touchEventTargetRef.current) {
        if (mouseMoveEventRef.current) {
          touchEventTargetRef.current.removeEventListener("touchmove", mouseMoveEventRef.current);
        }
        if (mouseUpEventRef.current) {
          touchEventTargetRef.current.removeEventListener("touchend", mouseUpEventRef.current);
        }
      }
      mouseMoveEventRef.current = null;
      mouseUpEventRef.current = null;
      touchEventTargetRef.current = null;
      finishChange(deleteMark);
      setDraggingIndex(-1);
      setDraggingDelete(false);
    };
    document.addEventListener("mouseup", onMouseUp);
    document.addEventListener("mousemove", onMouseMove);
    e.currentTarget.addEventListener("touchend", onMouseUp);
    e.currentTarget.addEventListener("touchmove", onMouseMove);
    mouseMoveEventRef.current = onMouseMove;
    mouseUpEventRef.current = onMouseUp;
    touchEventTargetRef.current = e.currentTarget;
  };
  const returnValues = reactExports.useMemo(() => {
    const sourceValues = [...rawValues].sort((a, b) => a - b);
    const targetValues = [...cacheValues].sort((a, b) => a - b);
    const counts = {};
    targetValues.forEach((val) => {
      counts[val] = (counts[val] || 0) + 1;
    });
    sourceValues.forEach((val) => {
      counts[val] = (counts[val] || 0) - 1;
    });
    const maxDiffCount = editable ? 1 : 0;
    const diffCount = Object.values(counts).reduce((prev, next) => prev + Math.abs(next), 0);
    return diffCount <= maxDiffCount ? cacheValues : rawValues;
  }, [rawValues, cacheValues, editable]);
  return [draggingIndex, draggingValue, draggingDelete, returnValues, onStartMove];
}
const getDisabledBoundaryValues = (values, valueIndex, min, max, pushable, isHandleDisabled) => {
  const pushGap = typeof pushable === "number" ? pushable : 0;
  let minBound = min;
  let maxBound = max;
  for (let i = valueIndex - 1; i >= 0; i -= 1) {
    if (isHandleDisabled(i)) {
      minBound = values[i] + pushGap;
      break;
    }
  }
  for (let i = valueIndex + 1; i < values.length; i += 1) {
    if (isHandleDisabled(i)) {
      maxBound = values[i] - pushGap;
      break;
    }
  }
  return [minBound, maxBound];
};
const getClosestEnabledHandleIndex = (values, targetValue, min, max, pushable, isHandleDisabled) => {
  let closestIndex = -1;
  let closestDist = max - min;
  values.forEach((value, index) => {
    if (isHandleDisabled(index)) {
      return;
    }
    const [minBound, maxBound] = getDisabledBoundaryValues(values, index, min, max, pushable, isHandleDisabled);
    if (minBound <= targetValue && targetValue <= maxBound) {
      const dist = Math.abs(targetValue - value);
      if (dist <= closestDist) {
        closestDist = dist;
        closestIndex = index;
      }
    }
  });
  return closestIndex;
};
function useOffset(min, max, step, markList, allowCross, pushable, isHandleDisabled) {
  const formatRangeValue = reactExports.useCallback((val) => Math.max(min, Math.min(max, val)), [min, max]);
  const formatStepValue = reactExports.useCallback((val) => {
    if (step !== null) {
      const stepValue = min + Math.round((formatRangeValue(val) - min) / step) * step;
      const getDecimal = (num) => (String(num).split(".")[1] || "").length;
      const maxDecimal = Math.max(getDecimal(step), getDecimal(max), getDecimal(min));
      const fixedValue = Number(stepValue.toFixed(maxDecimal));
      return min <= fixedValue && fixedValue <= max ? fixedValue : null;
    }
    return null;
  }, [step, min, max, formatRangeValue]);
  const formatValue = reactExports.useCallback((val) => {
    const formatNextValue = formatRangeValue(val);
    const alignValues = markList.map((mark) => mark.value);
    if (step !== null) {
      alignValues.push(formatStepValue(val));
    }
    alignValues.push(min, max);
    let closeValue = alignValues[0];
    let closeDist = max - min;
    alignValues.forEach((alignValue) => {
      const dist = Math.abs(formatNextValue - alignValue);
      if (dist <= closeDist) {
        closeValue = alignValue;
        closeDist = dist;
      }
    });
    return closeValue;
  }, [min, max, markList, step, formatRangeValue, formatStepValue]);
  const offsetValue = (values, offset, valueIndex, mode = "unit") => {
    if (typeof offset === "number") {
      let nextValue;
      const originValue = values[valueIndex];
      const targetDistValue = originValue + offset;
      let potentialValues = [];
      markList.forEach((mark) => {
        potentialValues.push(mark.value);
      });
      potentialValues.push(min, max);
      potentialValues.push(formatStepValue(originValue));
      const sign = offset > 0 ? 1 : -1;
      if (mode === "unit") {
        potentialValues.push(formatStepValue(originValue + sign * step));
      } else {
        potentialValues.push(formatStepValue(targetDistValue));
      }
      potentialValues = potentialValues.filter((val) => val !== null).filter((val) => offset < 0 ? val <= originValue : val >= originValue);
      if (mode === "unit") {
        potentialValues = potentialValues.filter((val) => val !== originValue);
      }
      const compareValue = mode === "unit" ? originValue : targetDistValue;
      nextValue = potentialValues[0];
      let valueDist = Math.abs(nextValue - compareValue);
      potentialValues.forEach((potentialValue) => {
        const dist = Math.abs(potentialValue - compareValue);
        if (dist < valueDist) {
          nextValue = potentialValue;
          valueDist = dist;
        }
      });
      if (nextValue === void 0) {
        return offset < 0 ? min : max;
      }
      if (mode === "dist") {
        return nextValue;
      }
      if (Math.abs(offset) > 1) {
        const cloneValues = [...values];
        cloneValues[valueIndex] = nextValue;
        return offsetValue(cloneValues, offset - sign, valueIndex, mode);
      }
      return nextValue;
    } else if (offset === "min") {
      return min;
    } else if (offset === "max") {
      return max;
    }
    return max;
  };
  const offsetChangedValue = (values, offset, valueIndex, mode = "unit") => {
    const originValue = values[valueIndex];
    const nextValue = offsetValue(values, offset, valueIndex, mode);
    return {
      value: nextValue,
      changed: nextValue !== originValue
    };
  };
  const needPush = (dist) => {
    return pushable === null && dist === 0 || typeof pushable === "number" && dist < pushable;
  };
  const offsetValues = (values, offset, valueIndex, mode = "unit") => {
    const nextValues = values.map(formatValue);
    const originValue = nextValues[valueIndex];
    const [minBound, maxBound] = getDisabledBoundaryValues(nextValues, valueIndex, min, max, pushable, isHandleDisabled);
    const nextValue = offsetValue(nextValues, offset, valueIndex, mode);
    nextValues[valueIndex] = nextValue;
    if (minBound <= maxBound) {
      nextValues[valueIndex] = Math.max(minBound, Math.min(maxBound, nextValues[valueIndex]));
    } else {
      nextValues[valueIndex] = originValue;
    }
    if (allowCross === false) {
      const pushNum = pushable || 0;
      if (valueIndex > 0 && nextValues[valueIndex - 1] !== originValue) {
        nextValues[valueIndex] = Math.max(nextValues[valueIndex], nextValues[valueIndex - 1] + pushNum);
      }
      if (valueIndex < nextValues.length - 1 && nextValues[valueIndex + 1] !== originValue) {
        nextValues[valueIndex] = Math.min(nextValues[valueIndex], nextValues[valueIndex + 1] - pushNum);
      }
    } else if (typeof pushable === "number" || pushable === null) {
      for (let i = valueIndex + 1; i < nextValues.length; i += 1) {
        if (isHandleDisabled(i)) {
          break;
        }
        let changed = true;
        while (needPush(nextValues[i] - nextValues[i - 1]) && changed) {
          ({
            value: nextValues[i],
            changed
          } = offsetChangedValue(nextValues, 1, i));
        }
        const [, itemMaxBound] = getDisabledBoundaryValues(nextValues, i, min, max, pushable, isHandleDisabled);
        nextValues[i] = Math.min(nextValues[i], itemMaxBound);
      }
      for (let i = valueIndex; i > 0; i -= 1) {
        if (isHandleDisabled(i - 1)) {
          break;
        }
        let changed = true;
        while (needPush(nextValues[i] - nextValues[i - 1]) && changed) {
          ({
            value: nextValues[i - 1],
            changed
          } = offsetChangedValue(nextValues, -1, i - 1));
        }
        const [itemMinBound] = getDisabledBoundaryValues(nextValues, i - 1, min, max, pushable, isHandleDisabled);
        nextValues[i - 1] = Math.max(nextValues[i - 1], itemMinBound);
      }
      for (let i = nextValues.length - 1; i > 0; i -= 1) {
        if (isHandleDisabled(i) || isHandleDisabled(i - 1)) {
          continue;
        }
        let changed = true;
        while (needPush(nextValues[i] - nextValues[i - 1]) && changed) {
          ({
            value: nextValues[i - 1],
            changed
          } = offsetChangedValue(nextValues, -1, i - 1));
        }
        const [itemMinBound] = getDisabledBoundaryValues(nextValues, i - 1, min, max, pushable, isHandleDisabled);
        nextValues[i - 1] = Math.max(nextValues[i - 1], itemMinBound);
      }
      for (let i = 0; i < nextValues.length - 1; i += 1) {
        if (isHandleDisabled(i) || isHandleDisabled(i + 1)) {
          continue;
        }
        let changed = true;
        while (needPush(nextValues[i + 1] - nextValues[i]) && changed) {
          ({
            value: nextValues[i + 1],
            changed
          } = offsetChangedValue(nextValues, 1, i + 1));
        }
        const [, itemMaxBound] = getDisabledBoundaryValues(nextValues, i + 1, min, max, pushable, isHandleDisabled);
        nextValues[i + 1] = Math.min(nextValues[i + 1], itemMaxBound);
      }
    }
    return {
      value: nextValues[valueIndex],
      values: nextValues
    };
  };
  return [formatValue, offsetValues];
}
function useRange(range) {
  return reactExports.useMemo(() => {
    if (range === true || !range) {
      return [!!range, false, false, 0];
    }
    const {
      editable,
      draggableTrack,
      minCount,
      maxCount
    } = range;
    return [true, !!editable, !editable && !!draggableTrack, minCount || 0, maxCount];
  }, [range]);
}
const Slider$1 = /* @__PURE__ */ reactExports.forwardRef((props, ref) => {
  const {
    prefixCls = "rc-slider",
    className,
    style,
    classNames,
    styles,
    id,
    disabled: rawDisabled = false,
    keyboard = true,
    autoFocus,
    onFocus,
    onBlur,
    // Value
    min = 0,
    max = 100,
    step = 1,
    value,
    defaultValue,
    range,
    count,
    onChange,
    onBeforeChange,
    onAfterChange,
    onChangeComplete,
    // Cross
    allowCross = true,
    pushable = false,
    // Direction
    reverse,
    vertical,
    // Style
    included = true,
    startPoint,
    trackStyle,
    handleStyle,
    railStyle,
    dotStyle,
    activeDotStyle,
    // Decorations
    marks,
    dots,
    // Components
    handleRender,
    activeHandleRender,
    track,
    // Accessibility
    tabIndex = 0,
    ariaLabelForHandle,
    ariaLabelledByForHandle,
    ariaRequired,
    ariaValueTextFormatterForHandle
  } = props;
  const handlesRef = reactExports.useRef(null);
  const containerRef = reactExports.useRef(null);
  const [mergedValue, setValue] = useControlledState(defaultValue, value);
  const direction = reactExports.useMemo(() => {
    if (vertical) {
      return reverse ? "ttb" : "btt";
    }
    return reverse ? "rtl" : "ltr";
  }, [reverse, vertical]);
  const [rangeEnabled, rangeEditable, rangeDraggableTrack, minCount, maxCount] = useRange(range);
  const mergedMin = reactExports.useMemo(() => isFinite(min) ? min : 0, [min]);
  const mergedMax = reactExports.useMemo(() => isFinite(max) ? max : 100, [max]);
  const mergedStep = reactExports.useMemo(() => step !== null && step <= 0 ? 1 : step, [step]);
  const mergedPush = reactExports.useMemo(() => {
    if (typeof pushable === "boolean") {
      return pushable ? mergedStep : false;
    }
    return pushable >= 0 ? pushable : false;
  }, [pushable, mergedStep]);
  const markList = reactExports.useMemo(() => {
    const markRecord = marks || {};
    return Object.keys(markRecord).map((key) => {
      const mark = markRecord[key];
      const markObj = {
        value: Number(key)
      };
      if (mark && typeof mark === "object" && !/* @__PURE__ */ reactExports.isValidElement(mark) && ("label" in mark || "style" in mark)) {
        markObj.style = mark.style;
        markObj.label = mark.label;
      } else {
        markObj.label = mark;
      }
      return markObj;
    }).filter(({
      label
    }) => label || typeof label === "number").sort((a, b) => a.value - b.value);
  }, [marks]);
  const [isHandleDisabled, getDisabledState] = useDisabled(rawDisabled);
  const [formatValue, offsetValues] = useOffset(mergedMin, mergedMax, mergedStep, markList, allowCross, mergedPush, isHandleDisabled);
  const rawValues = reactExports.useMemo(() => {
    const valueList = mergedValue === null || mergedValue === void 0 ? [] : Array.isArray(mergedValue) ? mergedValue : [mergedValue];
    const [val0 = mergedMin] = valueList;
    let returnValues = mergedValue === null ? [] : [val0];
    if (rangeEnabled) {
      returnValues = [...valueList];
      if (count || mergedValue === void 0) {
        const pointCount = count !== void 0 && count >= 0 ? count + 1 : 2;
        returnValues = returnValues.slice(0, pointCount);
        while (returnValues.length < pointCount) {
          returnValues.push(returnValues[returnValues.length - 1] ?? mergedMin);
        }
      }
      returnValues.sort((a, b) => a - b);
    }
    returnValues.forEach((val, index) => {
      returnValues[index] = formatValue(val);
    });
    return returnValues;
  }, [mergedValue, rangeEnabled, mergedMin, count, formatValue]);
  const [disabled, hasDisabledHandle] = reactExports.useMemo(() => getDisabledState(rawValues), [getDisabledState, rawValues]);
  const effectiveRangeEditable = rangeEditable && !hasDisabledHandle;
  const getTriggerValue = (triggerValues) => rangeEnabled ? triggerValues : triggerValues[0];
  const triggerChange = useEvent((nextValues) => {
    const cloneNextValues = [...nextValues].sort((a, b) => a - b);
    if (onChange && !isEqual(cloneNextValues, rawValues, true)) {
      onChange(getTriggerValue(cloneNextValues));
    }
    setValue(cloneNextValues);
  });
  const finishChange = useEvent((draggingDelete2) => {
    if (draggingDelete2) {
      handlesRef.current.hideHelp();
    }
    const finishValue = getTriggerValue(rawValues);
    onAfterChange?.(finishValue);
    warningOnce(!onAfterChange, "[rc-slider] `onAfterChange` is deprecated. Please use `onChangeComplete` instead.");
    onChangeComplete?.(finishValue);
  });
  const onDelete = (index) => {
    if (disabled || !effectiveRangeEditable || rawValues.length <= minCount) {
      return;
    }
    const cloneNextValues = [...rawValues];
    cloneNextValues.splice(index, 1);
    onBeforeChange?.(getTriggerValue(cloneNextValues));
    triggerChange(cloneNextValues);
    const nextFocusIndex = Math.max(0, index - 1);
    handlesRef.current.hideHelp();
    handlesRef.current.focus(nextFocusIndex);
  };
  const [draggingIndex, draggingValue, draggingDelete, cacheValues, onStartDrag] = useDrag(containerRef, direction, rawValues, mergedMin, mergedMax, formatValue, triggerChange, finishChange, offsetValues, effectiveRangeEditable, minCount, isHandleDisabled);
  const changeToCloseValue = (newValue, e) => {
    if (!disabled) {
      const valueIndex = rawValues.length ? getClosestEnabledHandleIndex(rawValues, newValue, mergedMin, mergedMax, mergedPush, isHandleDisabled) : 0;
      if (valueIndex === -1) {
        return;
      }
      const cloneNextValues = [...rawValues];
      let valueBeforeIndex = 0;
      const valueDist = rawValues.length ? Math.abs(newValue - rawValues[valueIndex]) : mergedMax - mergedMin;
      rawValues.forEach((val, index) => {
        if (val < newValue) {
          valueBeforeIndex = index;
        }
      });
      let focusIndex = valueIndex;
      if (effectiveRangeEditable && valueDist !== 0 && (!maxCount || rawValues.length < maxCount)) {
        cloneNextValues.splice(valueBeforeIndex + 1, 0, newValue);
        focusIndex = valueBeforeIndex + 1;
      } else {
        cloneNextValues[valueIndex] = newValue;
        focusIndex = valueIndex;
      }
      if (rangeEnabled && !rawValues.length && count === void 0) {
        cloneNextValues.push(newValue);
      }
      const nextValue = getTriggerValue(cloneNextValues);
      onBeforeChange?.(nextValue);
      triggerChange(cloneNextValues);
      if (e) {
        document.activeElement?.blur?.();
        handlesRef.current.focus(focusIndex);
        onStartDrag(e, focusIndex, cloneNextValues);
      } else {
        onAfterChange?.(nextValue);
        warningOnce(!onAfterChange, "[rc-slider] `onAfterChange` is deprecated. Please use `onChangeComplete` instead.");
        onChangeComplete?.(nextValue);
      }
    }
  };
  const onSliderMouseDown = (e) => {
    e.preventDefault();
    const {
      width,
      height,
      left,
      top,
      bottom,
      right
    } = containerRef.current.getBoundingClientRect();
    const {
      clientX,
      clientY
    } = e;
    let percent;
    switch (direction) {
      case "btt":
        percent = (bottom - clientY) / height;
        break;
      case "ttb":
        percent = (clientY - top) / height;
        break;
      case "rtl":
        percent = (right - clientX) / width;
        break;
      default:
        percent = (clientX - left) / width;
    }
    const nextValue = mergedMin + percent * (mergedMax - mergedMin);
    changeToCloseValue(formatValue(nextValue), e);
  };
  const [keyboardValue, setKeyboardValue] = reactExports.useState(null);
  const onHandleOffsetChange = (offset, valueIndex) => {
    if (!disabled && !isHandleDisabled(valueIndex)) {
      const next = offsetValues(rawValues, offset, valueIndex);
      onBeforeChange?.(getTriggerValue(rawValues));
      triggerChange(next.values);
      setKeyboardValue({
        value: next.value,
        index: valueIndex
      });
    }
  };
  reactExports.useEffect(() => {
    if (keyboardValue !== null) {
      const {
        value: nextKeyboardValue,
        index
      } = keyboardValue;
      const valueIndex = rawValues[index] === nextKeyboardValue ? index : rawValues.indexOf(nextKeyboardValue);
      if (valueIndex >= 0) {
        handlesRef.current.focus(valueIndex);
      }
    }
    setKeyboardValue(null);
  }, [keyboardValue]);
  const mergedDraggableTrack = reactExports.useMemo(() => {
    if (rangeDraggableTrack && mergedStep === null) {
      return false;
    }
    return rangeDraggableTrack;
  }, [rangeDraggableTrack, mergedStep]);
  const onStartMove = useEvent((e, valueIndex) => {
    onStartDrag(e, valueIndex);
    onBeforeChange?.(getTriggerValue(rawValues));
  });
  const dragging = draggingIndex !== -1;
  reactExports.useEffect(() => {
    if (!dragging) {
      const valueIndex = rawValues.lastIndexOf(draggingValue);
      handlesRef.current.focus(valueIndex);
    }
  }, [dragging]);
  const sortedCacheValues = reactExports.useMemo(() => [...cacheValues].sort((a, b) => a - b), [cacheValues]);
  const [includedStart, includedEnd] = reactExports.useMemo(() => {
    if (!rangeEnabled) {
      return [mergedMin, sortedCacheValues[0]];
    }
    return [sortedCacheValues[0], sortedCacheValues[sortedCacheValues.length - 1]];
  }, [sortedCacheValues, rangeEnabled, mergedMin]);
  reactExports.useImperativeHandle(ref, () => ({
    focus: () => {
      handlesRef.current.focus(0);
    },
    blur: () => {
      const {
        activeElement
      } = document;
      if (containerRef.current?.contains(activeElement)) {
        activeElement?.blur();
      }
    }
  }));
  const autoFocusRef = reactExports.useRef(autoFocus);
  reactExports.useEffect(() => {
    if (autoFocusRef.current) {
      handlesRef.current.focus(0);
    }
  }, []);
  const context = reactExports.useMemo(() => ({
    min: mergedMin,
    max: mergedMax,
    direction,
    disabled,
    keyboard,
    step: mergedStep,
    included,
    includedStart,
    includedEnd,
    range: rangeEnabled,
    tabIndex,
    ariaLabelForHandle,
    ariaLabelledByForHandle,
    ariaRequired,
    ariaValueTextFormatterForHandle,
    styles: styles || {},
    classNames: classNames || {},
    isHandleDisabled
  }), [mergedMin, mergedMax, direction, disabled, keyboard, mergedStep, included, includedStart, includedEnd, rangeEnabled, tabIndex, ariaLabelForHandle, ariaLabelledByForHandle, ariaRequired, ariaValueTextFormatterForHandle, styles, classNames, isHandleDisabled]);
  return /* @__PURE__ */ reactExports.createElement(SliderContext.Provider, {
    value: context
  }, /* @__PURE__ */ reactExports.createElement("div", {
    ref: containerRef,
    className: clsx(prefixCls, className, {
      [`${prefixCls}-disabled`]: disabled,
      [`${prefixCls}-vertical`]: vertical,
      [`${prefixCls}-horizontal`]: !vertical,
      [`${prefixCls}-with-marks`]: markList.length
    }),
    style,
    onMouseDown: onSliderMouseDown,
    id
  }, /* @__PURE__ */ reactExports.createElement("div", {
    className: clsx(`${prefixCls}-rail`, classNames?.rail),
    style: {
      ...railStyle,
      ...styles?.rail
    }
  }), track !== false && /* @__PURE__ */ reactExports.createElement(Tracks, {
    prefixCls,
    style: trackStyle,
    values: rawValues,
    startPoint,
    onStartMove: mergedDraggableTrack ? onStartMove : void 0
  }), /* @__PURE__ */ reactExports.createElement(Steps, {
    prefixCls,
    marks: markList,
    dots,
    style: dotStyle,
    activeStyle: activeDotStyle
  }), /* @__PURE__ */ reactExports.createElement(Handles, {
    ref: handlesRef,
    prefixCls,
    style: handleStyle,
    values: cacheValues,
    draggingIndex,
    draggingDelete,
    onStartMove,
    onOffsetChange: onHandleOffsetChange,
    onFocus,
    onBlur,
    handleRender,
    activeHandleRender,
    onChangeComplete: finishChange,
    onDelete: effectiveRangeEditable ? onDelete : void 0
  }), /* @__PURE__ */ reactExports.createElement(Marks, {
    prefixCls,
    marks: markList,
    onClick: changeToCloseValue
  })));
});
const SliderInternalContext = /* @__PURE__ */ reactExports.createContext({});
const SliderTooltip = /* @__PURE__ */ reactExports.forwardRef((props, ref) => {
  const {
    open,
    draggingDelete,
    value
  } = props;
  const innerRef = reactExports.useRef(null);
  const mergedOpen = open && !draggingDelete;
  const rafRef = reactExports.useRef(null);
  function cancelKeepAlign() {
    wrapperRaf.cancel(rafRef.current);
    rafRef.current = null;
  }
  function keepAlign() {
    rafRef.current = wrapperRaf(() => {
      innerRef.current?.forceAlign();
      rafRef.current = null;
    });
  }
  reactExports.useEffect(() => {
    if (mergedOpen) {
      keepAlign();
    } else {
      cancelKeepAlign();
    }
    return cancelKeepAlign;
  }, [mergedOpen, props.title, value]);
  return /* @__PURE__ */ reactExports.createElement(Tooltip, {
    ref: composeRef(innerRef, ref),
    ...props,
    open: mergedOpen
  });
});
const genBaseStyle = (token) => {
  const {
    componentCls,
    antCls,
    controlSize,
    dotSize,
    marginFull,
    marginPart,
    colorFillContentHover,
    handleColorDisabled,
    calc,
    handleSize,
    handleSizeHover,
    handleActiveColor,
    handleActiveOutlineColor,
    handleLineWidth,
    handleLineWidthHover,
    motionDurationMid
  } = token;
  const disabledHandle = {
    backgroundColor: token.colorBgElevated,
    cursor: "not-allowed",
    width: handleSize,
    height: handleSize,
    boxShadow: `0 0 0 ${unit(handleLineWidth)} ${handleColorDisabled}`,
    insetInlineStart: 0,
    insetBlockStart: 0
  };
  return {
    [componentCls]: {
      ...resetComponent(token),
      position: "relative",
      height: controlSize,
      margin: `${unit(marginPart)} ${unit(marginFull)}`,
      padding: 0,
      cursor: "pointer",
      touchAction: "none",
      // https://github.com/ant-design/ant-design/issues/55686
      // Prevent text selection on adjacent content when dragging the handle in Safari.
      userSelect: "none",
      "&-vertical": {
        margin: `${unit(marginFull)} ${unit(marginPart)}`
      },
      [`${componentCls}-rail`]: {
        position: "absolute",
        backgroundColor: token.railBg,
        borderRadius: token.borderRadiusXS,
        transition: `background-color ${motionDurationMid}`
      },
      [`${componentCls}-track,${componentCls}-tracks`]: {
        position: "absolute",
        transition: `background-color ${motionDurationMid}`
      },
      [`${componentCls}-track`]: {
        backgroundColor: token.trackBg,
        borderRadius: token.borderRadiusXS
      },
      [`${componentCls}-track-draggable`]: {
        boxSizing: "content-box",
        backgroundClip: "content-box",
        border: "solid rgba(0,0,0,0)"
      },
      "&:hover": {
        [`${componentCls}-rail`]: {
          backgroundColor: token.railHoverBg
        },
        [`${componentCls}-track`]: {
          backgroundColor: token.trackHoverBg
        },
        [`${componentCls}-dot`]: {
          borderColor: colorFillContentHover
        },
        [`${componentCls}-handle:not(${componentCls}-handle-disabled)::after`]: {
          boxShadow: `0 0 0 ${unit(handleLineWidth)} ${token.colorPrimaryBorderHover}`
        },
        [`${componentCls}-dot-active`]: {
          borderColor: token.dotActiveBorderColor
        }
      },
      [`${componentCls}-handle`]: {
        position: "absolute",
        width: handleSize,
        height: handleSize,
        outline: "none",
        userSelect: "none",
        // Dragging status
        "&-dragging-delete": {
          opacity: 0
        },
        // 扩大选区
        "&::before": {
          content: '""',
          position: "absolute",
          insetInlineStart: calc(handleLineWidth).mul(-1).equal(),
          insetBlockStart: calc(handleLineWidth).mul(-1).equal(),
          width: calc(handleSize).add(calc(handleLineWidth).mul(2)).equal(),
          height: calc(handleSize).add(calc(handleLineWidth).mul(2)).equal(),
          backgroundColor: "transparent"
        },
        "&::after": {
          content: '""',
          position: "absolute",
          insetBlockStart: 0,
          insetInlineStart: 0,
          width: handleSize,
          height: handleSize,
          backgroundColor: token.colorBgElevated,
          boxShadow: `0 0 0 ${unit(handleLineWidth)} ${token.handleColor}`,
          outline: `0px solid transparent`,
          borderRadius: "50%",
          cursor: "pointer",
          transition: ["inset-inline-start", "inset-block-start", "width", "height", "box-shadow", "outline"].map((prop) => `${prop} ${motionDurationMid}`).join(", ")
        },
        "&:hover, &:active, &:focus": {
          [`&:not(${componentCls}-handle-disabled)::before`]: {
            insetInlineStart: calc(handleSizeHover).sub(handleSize).div(2).add(handleLineWidthHover).mul(-1).equal(),
            insetBlockStart: calc(handleSizeHover).sub(handleSize).div(2).add(handleLineWidthHover).mul(-1).equal(),
            width: calc(handleSizeHover).add(calc(handleLineWidthHover).mul(2)).equal(),
            height: calc(handleSizeHover).add(calc(handleLineWidthHover).mul(2)).equal()
          },
          [`&:not(${componentCls}-handle-disabled)::after`]: {
            boxShadow: `0 0 0 ${unit(handleLineWidthHover)} ${handleActiveColor}`,
            outline: `6px solid ${handleActiveOutlineColor}`,
            width: handleSizeHover,
            height: handleSizeHover,
            insetInlineStart: token.calc(handleSize).sub(handleSizeHover).div(2).equal(),
            insetBlockStart: token.calc(handleSize).sub(handleSizeHover).div(2).equal()
          }
        }
      },
      [`&-lock ${componentCls}-handle`]: {
        "&::before, &::after": {
          transition: "none"
        }
      },
      [`${componentCls}-mark`]: {
        position: "absolute",
        fontSize: token.fontSize
      },
      [`${componentCls}-mark-text`]: {
        position: "absolute",
        display: "inline-block",
        color: token.colorTextDescription,
        textAlign: "center",
        wordBreak: "keep-all",
        cursor: "pointer",
        userSelect: "none",
        "&-active": {
          color: token.colorText
        }
      },
      [`${componentCls}-step`]: {
        position: "absolute",
        background: "transparent",
        pointerEvents: "none"
      },
      [`${componentCls}-dot`]: {
        position: "absolute",
        width: dotSize,
        height: dotSize,
        backgroundColor: token.colorBgElevated,
        border: `${unit(handleLineWidth)} solid ${token.dotBorderColor}`,
        borderRadius: "50%",
        cursor: "pointer",
        transition: `border-color ${token.motionDurationSlow}`,
        pointerEvents: "auto",
        "&-active": {
          borderColor: token.dotActiveBorderColor
        }
      },
      [`&${componentCls}-disabled`]: {
        cursor: "not-allowed",
        [`${componentCls}-rail`]: {
          backgroundColor: `${token.railBg} !important`
        },
        [`${componentCls}-track`]: {
          backgroundColor: `${token.trackBgDisabled} !important`
        },
        [`
          ${componentCls}-dot
        `]: {
          backgroundColor: token.colorBgElevated,
          borderColor: token.trackBgDisabled,
          boxShadow: "none",
          cursor: "not-allowed"
        },
        [`${componentCls}-handle::after`]: {
          ...disabledHandle
        },
        [`
          ${componentCls}-mark-text,
          ${componentCls}-dot
        `]: {
          cursor: `not-allowed !important`
        }
      },
      [`${componentCls}-handle-disabled::after`]: {
        ...disabledHandle
      },
      [`&-tooltip ${antCls}-tooltip-container`]: {
        minWidth: "unset"
      }
    }
  };
};
const genDirectionStyle = (token, horizontal) => {
  const {
    componentCls,
    railSize,
    handleSize,
    dotSize,
    marginFull,
    calc
  } = token;
  const railPadding = horizontal ? "paddingBlock" : "paddingInline";
  const full = horizontal ? "width" : "height";
  const part = horizontal ? "height" : "width";
  const handlePos = horizontal ? "insetBlockStart" : "insetInlineStart";
  const markInset = horizontal ? "top" : "insetInlineStart";
  const handlePosSize = calc(railSize).mul(3).sub(handleSize).div(2).equal();
  const draggableBorderSize = calc(handleSize).sub(railSize).div(2).equal();
  const draggableBorder = horizontal ? {
    borderWidth: `${unit(draggableBorderSize)} 0`,
    transform: `translateY(${unit(calc(draggableBorderSize).mul(-1).equal())})`
  } : {
    borderWidth: `0 ${unit(draggableBorderSize)}`,
    transform: `translateX(${unit(token.calc(draggableBorderSize).mul(-1).equal())})`
  };
  return {
    [railPadding]: railSize,
    [part]: calc(railSize).mul(3).equal(),
    [`${componentCls}-rail`]: {
      [full]: "100%",
      [part]: railSize
    },
    [`${componentCls}-track,${componentCls}-tracks`]: {
      [part]: railSize
    },
    [`${componentCls}-track-draggable`]: {
      ...draggableBorder
    },
    [`${componentCls}-handle`]: {
      [handlePos]: handlePosSize
    },
    [`${componentCls}-mark`]: {
      // Reset all
      insetInlineStart: 0,
      top: 0,
      // https://github.com/ant-design/ant-design/issues/43731
      [markInset]: calc(railSize).mul(3).add(horizontal ? 0 : marginFull).equal(),
      [full]: "100%"
    },
    [`${componentCls}-step`]: {
      // Reset all
      insetInlineStart: 0,
      top: 0,
      [markInset]: railSize,
      [full]: "100%",
      [part]: railSize
    },
    [`${componentCls}-dot`]: {
      position: "absolute",
      [handlePos]: calc(railSize).sub(dotSize).div(2).equal()
    }
  };
};
const genHorizontalStyle = (token) => {
  const {
    componentCls,
    marginPartWithMark
  } = token;
  return {
    [`${componentCls}-horizontal`]: {
      ...genDirectionStyle(token, true),
      [`&${componentCls}-with-marks`]: {
        marginBottom: marginPartWithMark
      }
    }
  };
};
const genVerticalStyle = (token) => {
  const {
    componentCls
  } = token;
  return {
    [`${componentCls}-vertical`]: {
      ...genDirectionStyle(token, false),
      height: "100%"
    }
  };
};
const prepareComponentToken = (token) => {
  const increaseHandleWidth = 1;
  const controlSize = token.controlHeightLG / 4;
  const controlSizeHover = token.controlHeightSM / 2;
  const handleLineWidth = token.lineWidth + increaseHandleWidth;
  const handleLineWidthHover = token.lineWidth + increaseHandleWidth * 1.5;
  const handleActiveColor = token.colorPrimary;
  const handleActiveOutlineColor = new FastColor(handleActiveColor).setA(0.2).toRgbString();
  return {
    controlSize,
    railSize: 4,
    handleSize: controlSize,
    handleSizeHover: controlSizeHover,
    dotSize: 8,
    handleLineWidth,
    handleLineWidthHover,
    railBg: token.colorFillTertiary,
    railHoverBg: token.colorFillSecondary,
    trackBg: token.colorPrimaryBorder,
    trackHoverBg: token.colorPrimaryBorderHover,
    handleColor: token.colorPrimaryBorder,
    handleActiveColor,
    handleActiveOutlineColor,
    handleColorDisabled: new FastColor(token.colorTextDisabled).onBackground(token.colorBgContainer).toHexString(),
    dotBorderColor: token.colorBorderSecondary,
    dotActiveBorderColor: token.colorPrimaryBorder,
    trackBgDisabled: token.colorBgContainerDisabled
  };
};
const useStyle$1 = genStyleHooks("Slider", (token) => {
  const sliderToken = merge(token, {
    marginPart: token.calc(token.controlHeight).sub(token.controlSize).div(2).equal(),
    marginFull: token.calc(token.controlSize).div(2).equal(),
    marginPartWithMark: token.calc(token.controlHeightLG).sub(token.controlSize).equal()
  });
  return [genBaseStyle(sliderToken), genHorizontalStyle(sliderToken), genVerticalStyle(sliderToken)];
}, prepareComponentToken);
function getTipFormatter(tipFormatter) {
  if (tipFormatter || tipFormatter === null) {
    return tipFormatter;
  }
  return (val) => isNumber(val) ? val.toString() : "";
}
const Slider = /* @__PURE__ */ React.forwardRef((props, ref) => {
  const {
    prefixCls: customizePrefixCls,
    range,
    className,
    rootClassName,
    style,
    disabled,
    // Deprecated Props
    tooltip = {},
    onChangeComplete,
    classNames,
    styles,
    vertical,
    orientation,
    ...restProps
  } = props;
  const [, mergedVertical] = useOrientation(orientation, vertical);
  const {
    getPrefixCls,
    direction: contextDirection,
    className: contextClassName,
    style: contextStyle,
    classNames: contextClassNames,
    styles: contextStyles,
    getPopupContainer
  } = useComponentConfig("slider");
  const contextDisabled = React.useContext(DisabledContext);
  const mergedDisabled = disabled ?? contextDisabled;
  const mergedProps = {
    ...props,
    disabled: mergedDisabled,
    vertical: mergedVertical
  };
  const contextStyleRoot = useSemanticRootStyle(contextStyle);
  const styleRoot = useSemanticRootStyle(style);
  const [mergedClassNames, mergedStyles] = useMergeSemantic([contextClassNames, classNames], [contextStyles, contextStyleRoot, styles, styleRoot], {
    props: mergedProps
  });
  const {
    handleRender: contextHandleRender,
    direction: internalContextDirection
  } = React.useContext(SliderInternalContext);
  const mergedDirection = internalContextDirection || contextDirection;
  const isRTL = mergedDirection === "rtl";
  const [hoverOpen, setHoverOpen] = useDelayState(false);
  const [focusOpen, setFocusOpen] = useDelayState(false);
  const tooltipProps = {
    ...tooltip
  };
  const {
    open: tooltipOpen,
    placement: tooltipPlacement,
    getPopupContainer: getTooltipPopupContainer,
    prefixCls: customizeTooltipPrefixCls,
    formatter: tipFormatter
  } = tooltipProps;
  const lockOpen = tooltipOpen;
  const activeOpen = (hoverOpen || focusOpen) && lockOpen !== false;
  const mergedTipFormatter = getTipFormatter(tipFormatter);
  const [dragging, setDragging] = useDelayState(false);
  const onInternalChangeComplete = (nextValues) => {
    onChangeComplete?.(nextValues);
    setDragging(false);
  };
  const getTooltipPlacement = (placement, vert) => {
    if (placement) {
      return placement;
    }
    if (!vert) {
      return "top";
    }
    return isRTL ? "left" : "right";
  };
  const prefixCls = getPrefixCls("slider", customizePrefixCls);
  const [hashId, cssVarCls] = useStyle$1(prefixCls);
  const rootClassNames = clsx(className, contextClassName, mergedClassNames.root, rootClassName, {
    [`${prefixCls}-rtl`]: isRTL,
    [`${prefixCls}-lock`]: dragging
  }, hashId, cssVarCls);
  if (isRTL && !mergedVertical) {
    restProps.reverse = !restProps.reverse;
  }
  React.useEffect(() => {
    const onMouseUp = () => {
      wrapperRaf(() => {
        setFocusOpen(false);
      }, 1);
    };
    document.addEventListener("mouseup", onMouseUp);
    return () => {
      document.removeEventListener("mouseup", onMouseUp);
    };
  }, []);
  const useActiveTooltipHandle = range && !lockOpen;
  const handleRender = contextHandleRender || ((node, info) => {
    const {
      index
    } = info;
    const nodeProps = node.props;
    function proxyEvent(eventName, event) {
      nodeProps[eventName]?.(event);
    }
    const passedProps = {
      ...nodeProps,
      onMouseEnter: (e) => {
        setHoverOpen(true, true);
        proxyEvent("onMouseEnter", e);
      },
      onMouseLeave: (e) => {
        setHoverOpen(false);
        proxyEvent("onMouseLeave", e);
      },
      onMouseDown: (e) => {
        setFocusOpen(true, true);
        setDragging(true, true);
        proxyEvent("onMouseDown", e);
      },
      onFocus: (e) => {
        setFocusOpen(true, true);
        proxyEvent("onFocus", e);
      },
      onBlur: (e) => {
        setFocusOpen(false);
        proxyEvent("onBlur", e);
      }
    };
    const cloneNode = /* @__PURE__ */ React.cloneElement(node, passedProps);
    const open = (!!lockOpen || activeOpen) && mergedTipFormatter !== null;
    if (!useActiveTooltipHandle) {
      return /* @__PURE__ */ React.createElement(SliderTooltip, {
        ...tooltipProps,
        prefixCls: getPrefixCls("tooltip", customizeTooltipPrefixCls),
        title: mergedTipFormatter ? mergedTipFormatter(info.value) : void 0,
        value: info.value,
        open,
        placement: getTooltipPlacement(tooltipPlacement, mergedVertical),
        key: index,
        classNames: {
          root: `${prefixCls}-tooltip`
        },
        getPopupContainer: getTooltipPopupContainer || getPopupContainer
      }, cloneNode);
    }
    return cloneNode;
  });
  const activeHandleRender = useActiveTooltipHandle ? (handle, info) => {
    const cloneNode = /* @__PURE__ */ React.cloneElement(handle, {
      style: {
        ...handle.props.style,
        visibility: "hidden"
      }
    });
    return /* @__PURE__ */ React.createElement(SliderTooltip, {
      ...tooltipProps,
      prefixCls: getPrefixCls("tooltip", customizeTooltipPrefixCls),
      title: mergedTipFormatter ? mergedTipFormatter(info.value) : void 0,
      open: mergedTipFormatter !== null && activeOpen,
      placement: getTooltipPlacement(tooltipPlacement, mergedVertical),
      key: "tooltip",
      classNames: {
        root: `${prefixCls}-tooltip`
      },
      getPopupContainer: getTooltipPopupContainer || getPopupContainer,
      draggingDelete: info.draggingDelete
    }, cloneNode);
  } : void 0;
  const rootStyle = {
    ...mergedStyles.root
  };
  return /* @__PURE__ */ React.createElement(Slider$1, {
    ...restProps,
    classNames: mergedClassNames,
    styles: mergedStyles,
    step: restProps.step,
    range,
    className: rootClassNames,
    style: rootStyle,
    disabled: mergedDisabled,
    vertical: mergedVertical,
    ref,
    prefixCls,
    handleRender,
    activeHandleRender,
    onChangeComplete: onInternalChangeComplete
  });
});
const GradientColorSlider = (props) => {
  const {
    prefixCls,
    colors,
    type,
    color,
    range = false,
    className,
    activeIndex,
    onActive,
    onDragStart,
    onDragChange,
    onKeyDelete,
    ...restProps
  } = props;
  const sliderProps = {
    ...restProps,
    track: false
  };
  const linearCss = reactExports.useMemo(() => {
    const colorsStr = colors.map((c) => `${c.color} ${c.percent}%`).join(", ");
    return `linear-gradient(90deg, ${colorsStr})`;
  }, [colors]);
  const pointColor = reactExports.useMemo(() => {
    if (!color || !type) {
      return null;
    }
    if (type === "alpha") {
      return color.toRgbString();
    }
    return `hsl(${color.toHsb().h}, 100%, 50%)`;
  }, [color, type]);
  const onInternalDragStart = useEvent(onDragStart);
  const onInternalDragChange = useEvent(onDragChange);
  const unstableContext = reactExports.useMemo(() => ({
    onDragStart: onInternalDragStart,
    onDragChange: onInternalDragChange
  }), []);
  const handleRender = useEvent((ori, info) => {
    const {
      onFocus,
      style,
      className: handleCls,
      onKeyDown
    } = ori.props;
    const mergedStyle = {
      ...style
    };
    if (type === "gradient") {
      mergedStyle.background = getGradientPercentColor(colors, info.value);
    }
    return /* @__PURE__ */ reactExports.cloneElement(ori, {
      onFocus: (e) => {
        onActive?.(info.index);
        onFocus?.(e);
      },
      style: mergedStyle,
      className: clsx(handleCls, {
        [`${prefixCls}-slider-handle-active`]: activeIndex === info.index
      }),
      onKeyDown: (e) => {
        if ((e.key === "Delete" || e.key === "Backspace") && onKeyDelete) {
          onKeyDelete(info.index);
        }
        onKeyDown?.(e);
      }
    });
  });
  const sliderContext = reactExports.useMemo(() => ({
    direction: "ltr",
    handleRender
  }), []);
  return /* @__PURE__ */ reactExports.createElement(SliderInternalContext.Provider, {
    value: sliderContext
  }, /* @__PURE__ */ reactExports.createElement(UnstableContext.Provider, {
    value: unstableContext
  }, /* @__PURE__ */ reactExports.createElement(Slider, {
    ...sliderProps,
    className: clsx(className, `${prefixCls}-slider`),
    tooltip: {
      open: false
    },
    range: {
      editable: range,
      minCount: 2
    },
    styles: {
      rail: {
        background: linearCss
      },
      handle: pointColor ? {
        background: pointColor
      } : {}
    },
    classNames: {
      rail: `${prefixCls}-slider-rail`,
      handle: `${prefixCls}-slider-handle`
    }
  })));
};
const SingleColorSlider = (props) => {
  const {
    value,
    onChange,
    onChangeComplete
  } = props;
  const singleOnChange = (v) => onChange(v[0]);
  const singleOnChangeComplete = (v) => onChangeComplete(v[0]);
  return /* @__PURE__ */ reactExports.createElement(GradientColorSlider, {
    ...props,
    value: [value],
    onChange: singleOnChange,
    onChangeComplete: singleOnChangeComplete
  });
};
function sortColors(colors) {
  return _toConsumableArray(colors).sort((a, b) => a.percent - b.percent);
}
const GradientColorBar = (props) => {
  const {
    prefixCls,
    mode,
    onChange,
    onChangeComplete,
    onActive,
    activeIndex,
    onGradientDragging,
    colors
  } = props;
  const isGradient = mode === "gradient";
  const colorList = reactExports.useMemo(() => colors.map((info) => ({
    percent: info.percent,
    color: info.color.toRgbString()
  })), [colors]);
  const values = reactExports.useMemo(() => colorList.map((info) => info.percent), [colorList]);
  const colorsRef = reactExports.useRef(colorList);
  const onDragStart = ({
    rawValues,
    draggingIndex,
    draggingValue
  }) => {
    if (rawValues.length > colorList.length) {
      const newPointColor = getGradientPercentColor(colorList, draggingValue);
      const nextColors = _toConsumableArray(colorList);
      nextColors.splice(draggingIndex, 0, {
        percent: draggingValue,
        color: newPointColor
      });
      colorsRef.current = nextColors;
    } else {
      colorsRef.current = colorList;
    }
    onGradientDragging(true);
    onChange(new AggregationColor(sortColors(colorsRef.current)), true);
  };
  const onDragChange = ({
    deleteIndex,
    draggingIndex,
    draggingValue
  }) => {
    let nextColors = _toConsumableArray(colorsRef.current);
    if (deleteIndex !== -1) {
      nextColors.splice(deleteIndex, 1);
    } else {
      nextColors[draggingIndex] = {
        ...nextColors[draggingIndex],
        percent: draggingValue
      };
      nextColors = sortColors(nextColors);
    }
    onChange(new AggregationColor(nextColors), true);
  };
  const onKeyDelete = (index) => {
    const nextColors = _toConsumableArray(colorList);
    nextColors.splice(index, 1);
    const nextColor = new AggregationColor(nextColors);
    onChange(nextColor);
    onChangeComplete(nextColor);
  };
  const onInternalChangeComplete = (nextValues) => {
    onChangeComplete(new AggregationColor(colorList));
    if (activeIndex >= nextValues.length) {
      onActive(nextValues.length - 1);
    }
    onGradientDragging(false);
  };
  if (!isGradient) {
    return null;
  }
  return /* @__PURE__ */ reactExports.createElement(GradientColorSlider, {
    min: 0,
    max: 100,
    prefixCls,
    className: `${prefixCls}-gradient-slider`,
    colors: colorList,
    color: null,
    value: values,
    range: true,
    onChangeComplete: onInternalChangeComplete,
    disabled: false,
    type: "gradient",
    // Active
    activeIndex,
    onActive,
    // Drag
    onDragStart,
    onDragChange,
    onKeyDelete
  });
};
const GradientColorBar$1 = /* @__PURE__ */ reactExports.memo(GradientColorBar);
const components = {
  slider: SingleColorSlider
};
const PanelPicker = () => {
  const panelPickerContext = reactExports.useContext(PanelPickerContext);
  const {
    mode,
    onModeChange,
    modeOptions,
    prefixCls,
    allowClear,
    value,
    disabledAlpha,
    onChange,
    onClear,
    onChangeComplete,
    activeIndex,
    gradientDragging,
    ...injectProps
  } = panelPickerContext;
  const colors = React.useMemo(() => {
    if (!value.cleared) {
      return value.getColors();
    }
    return [{
      percent: 0,
      color: new AggregationColor("")
    }, {
      percent: 100,
      color: new AggregationColor("")
    }];
  }, [value]);
  const isSingle = !value.isGradient();
  const [lockedColor, setLockedColor] = React.useState(value);
  useLayoutEffect(() => {
    if (!isSingle) {
      setLockedColor(colors[activeIndex]?.color);
    }
  }, [isSingle, colors, gradientDragging, activeIndex]);
  const activeColor = React.useMemo(() => {
    if (isSingle) {
      return value;
    }
    if (gradientDragging) {
      return lockedColor;
    }
    return colors[activeIndex]?.color;
  }, [colors, value, activeIndex, isSingle, lockedColor, gradientDragging]);
  const [pickerColor, setPickerColor] = React.useState(activeColor);
  const [forceSync, setForceSync] = useForceUpdate();
  const mergedPickerColor = pickerColor?.equals(activeColor) ? activeColor : pickerColor;
  useLayoutEffect(() => {
    setPickerColor(activeColor);
  }, [forceSync, activeColor?.toHexString()]);
  const fillColor = (nextColor, info) => {
    let submitColor = generateColor$1(nextColor);
    if (value.cleared) {
      const rgb = submitColor.toRgb();
      if (!rgb.r && !rgb.g && !rgb.b && info) {
        const {
          type: infoType,
          value: infoValue = 0
        } = info;
        submitColor = new AggregationColor({
          h: infoType === "hue" ? infoValue : 0,
          s: 1,
          b: 1,
          a: infoType === "alpha" ? infoValue / 100 : 1
        });
      } else {
        submitColor = genAlphaColor(submitColor);
      }
    }
    if (mode === "single") {
      return submitColor;
    }
    const nextColors = _toConsumableArray(colors);
    nextColors[activeIndex] = {
      ...nextColors[activeIndex],
      color: submitColor
    };
    return new AggregationColor(nextColors);
  };
  const onPickerChange = (colorValue, fromPicker, info) => {
    const nextColor = fillColor(colorValue, info);
    setPickerColor(nextColor.isGradient() ? nextColor.getColors()[activeIndex].color : nextColor);
    onChange(nextColor, fromPicker);
  };
  const onInternalChangeComplete = (nextColor, info) => {
    onChangeComplete(fillColor(nextColor, info));
    setForceSync();
  };
  const onInputChange = (colorValue) => {
    onChange(fillColor(colorValue));
  };
  let operationNode = null;
  const showMode = modeOptions.length > 1;
  if (allowClear || showMode) {
    operationNode = /* @__PURE__ */ React.createElement("div", {
      className: `${prefixCls}-operation`
    }, showMode && /* @__PURE__ */ React.createElement(Segmented, {
      size: "small",
      options: modeOptions,
      value: mode,
      onChange: onModeChange
    }), /* @__PURE__ */ React.createElement(ColorClear, {
      prefixCls,
      value,
      onChange: (clearColor) => {
        onChange(clearColor);
        onClear?.();
      },
      ...injectProps
    }));
  }
  return /* @__PURE__ */ React.createElement(React.Fragment, null, operationNode, /* @__PURE__ */ React.createElement(GradientColorBar$1, {
    ...panelPickerContext,
    colors
  }), /* @__PURE__ */ React.createElement(ColorPicker$1, {
    prefixCls,
    value: mergedPickerColor?.toHsb(),
    disabledAlpha,
    onChange: (colorValue, info) => {
      onPickerChange(colorValue, true, info);
    },
    onChangeComplete: (colorValue, info) => {
      onInternalChangeComplete(colorValue, info);
    },
    components
  }), /* @__PURE__ */ React.createElement(ColorInput, {
    value: activeColor,
    onChange: onInputChange,
    prefixCls,
    disabledAlpha,
    ...injectProps
  }));
};
const PanelPresets = () => {
  const {
    prefixCls,
    value,
    presets,
    onChange
  } = reactExports.useContext(PanelPresetsContext);
  return Array.isArray(presets) ? /* @__PURE__ */ React.createElement(ColorPresets, {
    value,
    presets,
    prefixCls,
    onChange
  }) : null;
};
const ColorPickerPanel = (props) => {
  const {
    prefixCls,
    presets,
    panelRender,
    value,
    onChange,
    onClear,
    allowClear,
    disabledAlpha,
    mode,
    onModeChange,
    modeOptions,
    onChangeComplete,
    activeIndex,
    onActive,
    format,
    onFormatChange,
    gradientDragging,
    onGradientDragging,
    disabledFormat
  } = props;
  const colorPickerPanelPrefixCls = `${prefixCls}-inner`;
  const panelContext = React.useMemo(() => ({
    prefixCls,
    value,
    onChange,
    onClear,
    allowClear,
    disabledAlpha,
    mode,
    onModeChange,
    modeOptions,
    onChangeComplete,
    activeIndex,
    onActive,
    format,
    onFormatChange,
    gradientDragging,
    onGradientDragging,
    disabledFormat
  }), [prefixCls, value, onChange, onClear, allowClear, disabledAlpha, mode, onModeChange, modeOptions, onChangeComplete, activeIndex, onActive, format, onFormatChange, gradientDragging, onGradientDragging, disabledFormat]);
  const presetContext = React.useMemo(() => ({
    prefixCls,
    value,
    presets,
    onChange
  }), [prefixCls, value, presets, onChange]);
  const innerPanel = /* @__PURE__ */ React.createElement("div", {
    className: `${colorPickerPanelPrefixCls}-content`
  }, /* @__PURE__ */ React.createElement(PanelPicker, null), Array.isArray(presets) && /* @__PURE__ */ React.createElement(Divider, null), /* @__PURE__ */ React.createElement(PanelPresets, null));
  return /* @__PURE__ */ React.createElement(PanelPickerContext.Provider, {
    value: panelContext
  }, /* @__PURE__ */ React.createElement(PanelPresetsContext.Provider, {
    value: presetContext
  }, /* @__PURE__ */ React.createElement("div", {
    className: colorPickerPanelPrefixCls
  }, isFunction(panelRender) ? panelRender(innerPanel, {
    components: {
      Picker: PanelPicker,
      Presets: PanelPresets
    }
  }) : innerPanel)));
};
const ColorTrigger = /* @__PURE__ */ reactExports.forwardRef((props, ref) => {
  const {
    color,
    prefixCls,
    open,
    disabled,
    format,
    className,
    style,
    classNames,
    styles,
    showText,
    activeIndex,
    ...rest
  } = props;
  const colorTriggerPrefixCls = `${prefixCls}-trigger`;
  const colorTextPrefixCls = `${colorTriggerPrefixCls}-text`;
  const colorTextCellPrefixCls = `${colorTextPrefixCls}-cell`;
  const [locale] = useLocale("ColorPicker");
  const desc = React.useMemo(() => {
    if (!showText) {
      return "";
    }
    if (isFunction(showText)) {
      return showText(color);
    }
    if (color.cleared) {
      return locale.transparent;
    }
    if (color.isGradient()) {
      return color.getColors().map((c, index) => {
        const inactive = activeIndex !== -1 && activeIndex !== index;
        return /* @__PURE__ */ React.createElement("span", {
          key: index,
          className: clsx(colorTextCellPrefixCls, inactive && `${colorTextCellPrefixCls}-inactive`)
        }, c.color.toRgbString(), " ", c.percent, "%");
      });
    }
    const hexString = color.toHexString().toUpperCase();
    const alpha = getColorAlpha(color);
    switch (format) {
      case "rgb":
        return color.toRgbString();
      case "hsb":
        return color.toHsbString();
      // case 'hex':
      default:
        return alpha < 100 ? `${hexString.slice(0, 7)},${alpha}%` : hexString;
    }
  }, [color, format, showText, activeIndex, locale.transparent, colorTextCellPrefixCls]);
  const containerNode = reactExports.useMemo(() => color.cleared ? /* @__PURE__ */ React.createElement(ColorClear, {
    prefixCls,
    className: classNames.body,
    style: styles.body
  }) : /* @__PURE__ */ React.createElement(ColorBlock, {
    prefixCls,
    color: color.toCssString(),
    className: classNames.body,
    innerClassName: classNames.content,
    style: styles.body,
    innerStyle: styles.content
  }), [color, prefixCls, classNames.body, classNames.content, styles.body, styles.content]);
  return /* @__PURE__ */ React.createElement("div", {
    ref,
    className: clsx(colorTriggerPrefixCls, className, classNames.root, {
      [`${colorTriggerPrefixCls}-active`]: open,
      [`${colorTriggerPrefixCls}-disabled`]: disabled
    }),
    style: {
      ...styles.root,
      ...style
    },
    ...pickAttrs(rest)
  }, containerNode, showText && /* @__PURE__ */ React.createElement("div", {
    className: clsx(colorTextPrefixCls, classNames.description),
    style: styles.description
  }, desc));
});
function useModeColor(defaultValue, value, mode) {
  const [locale] = useLocale("ColorPicker");
  const [mergedColor, setMergedColor] = useControlledState(defaultValue, value);
  const [modeState, setModeState] = reactExports.useState("single");
  const [modeOptionList, modeSet] = reactExports.useMemo(() => {
    const list = (Array.isArray(mode) ? mode : [mode]).filter((m) => m);
    if (!list.length) {
      list.push("single");
    }
    const modes = new Set(list);
    const optionList = [];
    const pushOption = (modeType, localeTxt) => {
      if (modes.has(modeType)) {
        optionList.push({
          label: localeTxt,
          value: modeType
        });
      }
    };
    pushOption("single", locale.singleColor);
    pushOption("gradient", locale.gradientColor);
    return [optionList, modes];
  }, [mode, locale.singleColor, locale.gradientColor]);
  const [cacheColor, setCacheColor] = reactExports.useState(null);
  const setColor = useEvent((nextColor) => {
    setCacheColor(nextColor);
    setMergedColor(nextColor);
  });
  const postColor = reactExports.useMemo(() => {
    const colorObj = generateColor$1(mergedColor || "");
    return colorObj.equals(cacheColor) ? cacheColor : colorObj;
  }, [mergedColor, cacheColor]);
  const postMode = reactExports.useMemo(() => {
    if (modeSet.has(modeState)) {
      return modeState;
    }
    return modeOptionList[0]?.value;
  }, [modeSet, modeState, modeOptionList]);
  reactExports.useEffect(() => {
    setModeState(postColor.isGradient() ? "gradient" : "single");
  }, [postColor]);
  return [postColor, setColor, postMode, setModeState, modeOptionList];
}
const getTransBg = (size, colorFill) => ({
  backgroundImage: `conic-gradient(${colorFill} 25%, transparent 25% 50%, ${colorFill} 50% 75%, transparent 75% 100%)`,
  backgroundSize: `${size} ${size}`
});
const genColorBlockStyle = (token, size) => {
  const {
    componentCls,
    borderRadiusSM,
    colorPickerInsetShadow,
    lineWidth,
    colorFillSecondary
  } = token;
  return {
    [`${componentCls}-color-block`]: {
      position: "relative",
      borderRadius: borderRadiusSM,
      width: size,
      height: size,
      boxShadow: colorPickerInsetShadow,
      flex: "none",
      ...getTransBg("50%", token.colorFillSecondary),
      [`${componentCls}-color-block-inner`]: {
        width: "100%",
        height: "100%",
        boxShadow: `inset 0 0 0 ${unit(lineWidth)} ${colorFillSecondary}`,
        borderRadius: "inherit"
      }
    }
  };
};
const genInputStyle = (token) => {
  const {
    componentCls,
    antCls,
    fontSizeSM,
    lineHeightSM,
    colorPickerAlphaInputWidth,
    marginXXS,
    paddingXXS,
    controlHeightSM,
    marginXS,
    fontSizeIcon,
    paddingXS,
    colorTextPlaceholder,
    colorPickerInputNumberHandleWidth,
    lineWidth
  } = token;
  return {
    [`${componentCls}-input-container`]: {
      display: "flex",
      [`${componentCls}-steppers${antCls}-input-number`]: {
        fontSize: fontSizeSM,
        lineHeight: lineHeightSM,
        padding: 0,
        [`${antCls}-input-number-input`]: {
          paddingInlineStart: paddingXXS,
          paddingInlineEnd: 0
        },
        [`${antCls}-input-number-handler-wrap`]: {
          width: colorPickerInputNumberHandleWidth
        }
      },
      [`${componentCls}-steppers${componentCls}-alpha-input`]: {
        flex: `0 0 ${unit(colorPickerAlphaInputWidth)}`,
        marginInlineStart: marginXXS
      },
      [`${componentCls}-format-select${antCls}-select`]: {
        marginInlineEnd: marginXS,
        width: "auto",
        "&-single": {
          [`${antCls}-select-selector`]: {
            padding: 0,
            border: 0
          },
          [`${antCls}-select-arrow`]: {
            insetInlineEnd: 0
          },
          [`${antCls}-select-selection-item`]: {
            paddingInlineEnd: token.calc(fontSizeIcon).add(marginXXS).equal(),
            fontSize: fontSizeSM,
            lineHeight: unit(controlHeightSM)
          },
          [`${antCls}-select-item-option-content`]: {
            fontSize: fontSizeSM,
            lineHeight: lineHeightSM
          },
          [`${antCls}-select-dropdown`]: {
            [`${antCls}-select-item`]: {
              minHeight: "auto"
            }
          }
        }
      },
      [`${componentCls}-input`]: {
        gap: marginXXS,
        alignItems: "center",
        flex: 1,
        width: 0,
        [`${componentCls}-hsb-input,${componentCls}-rgb-input`]: {
          height: controlHeightSM,
          display: "flex",
          gap: marginXXS,
          alignItems: "center"
        },
        [`${componentCls}-steppers`]: {
          flex: 1
        },
        [`${componentCls}-hex-input${antCls}-input-affix-wrapper`]: {
          flex: 1,
          padding: `0 ${unit(paddingXS)}`,
          [`${antCls}-input`]: {
            fontSize: fontSizeSM,
            textTransform: "uppercase",
            lineHeight: unit(token.calc(controlHeightSM).sub(token.calc(lineWidth).mul(2)).equal())
          },
          [`${antCls}-input-prefix`]: {
            color: colorTextPlaceholder
          }
        }
      }
    }
  };
};
const genPickerStyle = (token) => {
  const {
    componentCls,
    controlHeightLG,
    borderRadiusSM,
    colorPickerInsetShadow,
    marginSM,
    colorBgElevated,
    colorFillSecondary,
    lineWidthBold,
    colorPickerHandlerSize
  } = token;
  return {
    userSelect: "none",
    [`${componentCls}-select`]: {
      [`${componentCls}-palette`]: {
        minHeight: token.calc(controlHeightLG).mul(4).equal(),
        overflow: "hidden",
        borderRadius: borderRadiusSM
      },
      [`${componentCls}-saturation`]: {
        position: "absolute",
        borderRadius: "inherit",
        boxShadow: colorPickerInsetShadow,
        inset: 0
      },
      marginBottom: marginSM
    },
    // ======================== Panel =========================
    [`${componentCls}-handler`]: {
      width: colorPickerHandlerSize,
      height: colorPickerHandlerSize,
      border: `${unit(lineWidthBold)} solid ${colorBgElevated}`,
      position: "relative",
      borderRadius: "50%",
      cursor: "pointer",
      boxShadow: `${colorPickerInsetShadow}, 0 0 0 1px ${colorFillSecondary}`
    }
  };
};
const genPresetsStyle = (token) => {
  const {
    componentCls,
    antCls,
    colorTextQuaternary,
    paddingXXS,
    colorPickerPresetColorSize,
    fontSizeSM,
    colorText,
    lineHeightSM,
    lineWidth,
    borderRadius,
    colorFill,
    colorWhite,
    marginXXS,
    paddingXS,
    fontHeightSM
  } = token;
  return {
    [`${componentCls}-presets`]: {
      [`${antCls}-collapse-item > ${antCls}-collapse-header`]: {
        padding: 0,
        [`${antCls}-collapse-expand-icon`]: {
          height: fontHeightSM,
          color: colorTextQuaternary,
          paddingInlineEnd: paddingXXS
        }
      },
      [`${antCls}-collapse`]: {
        display: "flex",
        flexDirection: "column",
        gap: marginXXS
      },
      [`${antCls}-collapse-item > ${antCls}-collapse-panel > ${antCls}-collapse-body`]: {
        padding: `${unit(paddingXS)} 0`
      },
      "&-label": {
        fontSize: fontSizeSM,
        color: colorText,
        lineHeight: lineHeightSM
      },
      "&-items": {
        display: "flex",
        flexWrap: "wrap",
        gap: token.calc(marginXXS).mul(1.5).equal(),
        [`${componentCls}-presets-color`]: {
          position: "relative",
          cursor: "pointer",
          width: colorPickerPresetColorSize,
          height: colorPickerPresetColorSize,
          "&::before": {
            content: '""',
            pointerEvents: "none",
            width: token.calc(colorPickerPresetColorSize).add(token.calc(lineWidth).mul(4)).equal(),
            height: token.calc(colorPickerPresetColorSize).add(token.calc(lineWidth).mul(4)).equal(),
            position: "absolute",
            top: token.calc(lineWidth).mul(-2).equal(),
            insetInlineStart: token.calc(lineWidth).mul(-2).equal(),
            borderRadius,
            border: `${unit(lineWidth)} solid transparent`,
            transition: `border-color ${token.motionDurationMid} ${token.motionEaseInBack}`
          },
          "&:hover::before": {
            borderColor: colorFill
          },
          "&::after": {
            boxSizing: "border-box",
            position: "absolute",
            top: "50%",
            insetInlineStart: "21.5%",
            display: "table",
            width: token.calc(colorPickerPresetColorSize).div(13).mul(5).equal(),
            height: token.calc(colorPickerPresetColorSize).div(13).mul(8).equal(),
            border: `${unit(token.lineWidthBold)} solid ${token.colorWhite}`,
            borderTop: 0,
            borderInlineStart: 0,
            transform: "rotate(45deg) scale(0) translate(-50%,-50%)",
            opacity: 0,
            content: '""',
            transition: `all ${token.motionDurationFast} ${token.motionEaseInBack}, opacity ${token.motionDurationFast}`
          },
          [`&${componentCls}-presets-color-checked`]: {
            "&::after": {
              opacity: 1,
              borderColor: colorWhite,
              transform: "rotate(45deg) scale(1) translate(-50%,-50%)",
              transition: `transform ${token.motionDurationMid} ${token.motionEaseOutBack} ${token.motionDurationFast}`
            },
            [`&${componentCls}-presets-color-bright`]: {
              "&::after": {
                borderColor: "rgba(0, 0, 0, 0.45)"
              }
            }
          }
        }
      },
      "&-empty": {
        fontSize: fontSizeSM,
        color: colorTextQuaternary
      }
    }
  };
};
const genSliderStyle = (token) => {
  const {
    componentCls,
    colorPickerInsetShadow,
    colorBgElevated,
    colorFillSecondary,
    lineWidthBold,
    colorPickerHandlerSizeSM,
    colorPickerSliderHeight,
    marginSM,
    marginXS
  } = token;
  const handleInnerSize = token.calc(colorPickerHandlerSizeSM).sub(token.calc(lineWidthBold).mul(2).equal()).equal();
  const handleHoverSize = token.calc(colorPickerHandlerSizeSM).add(token.calc(lineWidthBold).mul(2).equal()).equal();
  const activeHandleStyle = {
    "&:after": {
      transform: "scale(1)",
      boxShadow: `${colorPickerInsetShadow}, 0 0 0 1px ${token.colorPrimaryActive}`
    }
  };
  return {
    // ======================== Slider ========================
    [`${componentCls}-slider`]: [getTransBg(unit(colorPickerSliderHeight), token.colorFillSecondary), {
      margin: 0,
      padding: 0,
      height: colorPickerSliderHeight,
      borderRadius: token.calc(colorPickerSliderHeight).div(2).equal(),
      "&-rail": {
        height: colorPickerSliderHeight,
        borderRadius: token.calc(colorPickerSliderHeight).div(2).equal(),
        boxShadow: colorPickerInsetShadow
      },
      [`& ${componentCls}-slider-handle`]: {
        width: handleInnerSize,
        height: handleInnerSize,
        top: 0,
        borderRadius: "100%",
        "&:before": {
          display: "block",
          position: "absolute",
          background: "transparent",
          left: {
            _skip_check_: true,
            value: "50%"
          },
          top: "50%",
          transform: "translate(-50%, -50%)",
          width: handleHoverSize,
          height: handleHoverSize,
          borderRadius: "100%"
        },
        "&:after": {
          width: colorPickerHandlerSizeSM,
          height: colorPickerHandlerSizeSM,
          border: `${unit(lineWidthBold)} solid ${colorBgElevated}`,
          boxShadow: `${colorPickerInsetShadow}, 0 0 0 1px ${colorFillSecondary}`,
          outline: "none",
          insetInlineStart: token.calc(lineWidthBold).mul(-1).equal(),
          top: token.calc(lineWidthBold).mul(-1).equal(),
          background: "transparent",
          transition: "none"
        },
        "&:focus": activeHandleStyle
      }
    }],
    // ======================== Layout ========================
    [`${componentCls}-slider-container`]: {
      display: "flex",
      gap: marginSM,
      marginBottom: marginSM,
      // Group
      [`${componentCls}-slider-group`]: {
        flex: 1,
        flexDirection: "column",
        justifyContent: "space-between",
        display: "flex",
        "&-disabled-alpha": {
          justifyContent: "center"
        }
      }
    },
    [`${componentCls}-gradient-slider`]: {
      marginBottom: marginXS,
      [`& ${componentCls}-slider-handle`]: {
        "&:after": {
          transform: "scale(0.8)"
        },
        "&-active, &:focus": activeHandleStyle
      }
    }
  };
};
const genActiveStyle = (token, borderColor, outlineColor) => ({
  borderInlineEndWidth: token.lineWidth,
  borderColor,
  boxShadow: `0 0 0 ${unit(token.controlOutlineWidth)} ${outlineColor}`,
  outline: 0
});
const genRtlStyle = (token) => {
  const {
    componentCls
  } = token;
  return {
    "&-rtl": {
      [`${componentCls}-presets-color`]: {
        "&::after": {
          direction: "ltr"
        }
      },
      [`${componentCls}-clear`]: {
        "&::after": {
          direction: "ltr"
        }
      }
    }
  };
};
const genClearStyle = (token, size, extraStyle) => {
  const {
    componentCls,
    borderRadiusSM,
    lineWidth,
    lineType,
    colorSplit,
    colorBorder,
    red6
  } = token;
  return {
    [`${componentCls}-clear`]: {
      width: size,
      height: size,
      borderRadius: borderRadiusSM,
      border: `${unit(lineWidth)} ${lineType} ${colorSplit}`,
      position: "relative",
      overflow: "hidden",
      cursor: "inherit",
      transition: `all ${token.motionDurationFast}`,
      ...extraStyle,
      "&::after": {
        content: '""',
        position: "absolute",
        insetInlineEnd: token.calc(lineWidth).mul(-1).equal(),
        top: token.calc(lineWidth).mul(-1).equal(),
        display: "block",
        width: 40,
        // maximum
        height: 2,
        // fixed
        transformOrigin: `calc(100% - 1px) 1px`,
        transform: "rotate(-45deg)",
        backgroundColor: red6
      },
      "&:hover": {
        borderColor: colorBorder
      }
    }
  };
};
const genStatusStyle = (token) => {
  const {
    componentCls,
    colorError,
    colorWarning,
    colorErrorHover,
    colorWarningHover,
    colorErrorOutline,
    colorWarningOutline
  } = token;
  return {
    [`&${componentCls}-status-error`]: {
      borderColor: colorError,
      "&:hover": {
        borderColor: colorErrorHover
      },
      [`&${componentCls}-trigger-active`]: {
        ...genActiveStyle(token, colorError, colorErrorOutline)
      }
    },
    [`&${componentCls}-status-warning`]: {
      borderColor: colorWarning,
      "&:hover": {
        borderColor: colorWarningHover
      },
      [`&${componentCls}-trigger-active`]: {
        ...genActiveStyle(token, colorWarning, colorWarningOutline)
      }
    }
  };
};
const genSizeStyle = (token) => {
  const {
    componentCls,
    controlHeightLG,
    controlHeightSM,
    controlHeight,
    controlHeightXS,
    borderRadius,
    borderRadiusSM,
    borderRadiusXS,
    borderRadiusLG,
    fontSizeLG
  } = token;
  return {
    [`&${componentCls}-lg`]: {
      minWidth: controlHeightLG,
      minHeight: controlHeightLG,
      borderRadius: borderRadiusLG,
      [`${componentCls}-color-block, ${componentCls}-clear`]: {
        width: controlHeight,
        height: controlHeight,
        borderRadius
      },
      [`${componentCls}-trigger-text`]: {
        fontSize: fontSizeLG
      }
    },
    [`&${componentCls}-sm`]: {
      minWidth: controlHeightSM,
      minHeight: controlHeightSM,
      borderRadius: borderRadiusSM,
      [`${componentCls}-color-block, ${componentCls}-clear`]: {
        width: controlHeightXS,
        height: controlHeightXS,
        borderRadius: borderRadiusXS
      },
      [`${componentCls}-trigger-text`]: {
        lineHeight: unit(controlHeightXS)
      }
    }
  };
};
const genColorPickerStyle = (token) => {
  const {
    antCls,
    componentCls,
    colorPickerWidth,
    colorPrimary,
    motionDurationMid,
    colorBgElevated,
    colorTextDisabled,
    colorText,
    colorBgContainerDisabled,
    borderRadius,
    marginXS,
    marginSM,
    controlHeight,
    controlHeightSM,
    colorBgTextActive,
    colorPickerPresetColorSize,
    colorPickerPreviewSize,
    lineWidth,
    lineType,
    colorBorder,
    paddingXXS,
    fontSize,
    colorPrimaryHover,
    controlOutline
  } = token;
  return [{
    [componentCls]: {
      [`${componentCls}-inner`]: {
        "&-content": {
          display: "flex",
          flexDirection: "column",
          width: colorPickerWidth,
          [`& > ${antCls}-divider`]: {
            margin: `${unit(marginSM)} 0 ${unit(marginXS)}`
          }
        },
        [`${componentCls}-panel`]: {
          ...genPickerStyle(token)
        },
        ...genSliderStyle(token),
        ...genColorBlockStyle(token, colorPickerPreviewSize),
        ...genInputStyle(token),
        ...genPresetsStyle(token),
        ...genClearStyle(token, colorPickerPresetColorSize, {
          marginInlineStart: "auto"
        }),
        // Operation bar
        [`${componentCls}-operation`]: {
          display: "flex",
          justifyContent: "space-between",
          marginBottom: marginXS
        }
      },
      "&-trigger": {
        minWidth: controlHeight,
        minHeight: controlHeight,
        borderRadius,
        border: `${unit(lineWidth)} ${lineType} ${colorBorder}`,
        cursor: "pointer",
        display: "inline-flex",
        alignItems: "flex-start",
        justifyContent: "center",
        transition: `all ${motionDurationMid}`,
        background: colorBgElevated,
        padding: token.calc(paddingXXS).sub(lineWidth).equal(),
        [`${componentCls}-trigger-text`]: {
          marginInlineStart: marginXS,
          marginInlineEnd: token.calc(marginXS).sub(token.calc(paddingXXS).sub(lineWidth)).equal(),
          fontSize,
          color: colorText,
          alignSelf: "center",
          "&-cell": {
            "&:not(:last-child):after": {
              content: '", "'
            },
            "&-inactive": {
              color: colorTextDisabled
            }
          }
        },
        "&:hover": {
          borderColor: colorPrimaryHover
        },
        [`&${componentCls}-trigger-active`]: {
          ...genActiveStyle(token, colorPrimary, controlOutline)
        },
        "&-disabled": {
          color: colorTextDisabled,
          background: colorBgContainerDisabled,
          cursor: "not-allowed",
          "&:hover": {
            borderColor: colorBgTextActive
          },
          [`${componentCls}-trigger-text`]: {
            color: colorTextDisabled
          }
        },
        ...genClearStyle(token, controlHeightSM),
        ...genColorBlockStyle(token, controlHeightSM),
        ...genStatusStyle(token),
        ...genSizeStyle(token)
      },
      ...genRtlStyle(token)
    }
  }, genCompactItemStyle(token, {
    focusElCls: `${componentCls}-trigger-active`
  })];
};
const useStyle = genStyleHooks("ColorPicker", (token) => {
  const {
    colorTextQuaternary,
    marginSM
  } = token;
  const colorPickerSliderHeight = 8;
  const colorPickerToken = merge(token, {
    colorPickerWidth: 234,
    colorPickerHandlerSize: 16,
    colorPickerHandlerSizeSM: 12,
    colorPickerAlphaInputWidth: 44,
    colorPickerInputNumberHandleWidth: 16,
    colorPickerPresetColorSize: 24,
    colorPickerInsetShadow: `inset 0 0 1px 0 ${colorTextQuaternary}`,
    colorPickerSliderHeight,
    colorPickerPreviewSize: token.calc(colorPickerSliderHeight).mul(2).add(marginSM).equal()
  });
  return genColorPickerStyle(colorPickerToken);
});
const ColorPicker = (props) => {
  const {
    mode,
    value,
    defaultValue,
    format,
    defaultFormat,
    allowClear = false,
    presets,
    children,
    trigger = "click",
    open,
    disabled,
    placement = "bottomLeft",
    arrow,
    panelRender,
    showText,
    style,
    className,
    size: customizeSize,
    rootClassName,
    prefixCls: customizePrefixCls,
    styles,
    classNames,
    disabledAlpha = false,
    onFormatChange,
    onChange,
    onClear,
    onOpenChange,
    onChangeComplete,
    getPopupContainer,
    autoAdjustOverflow = true,
    destroyTooltipOnHide,
    destroyOnHidden,
    disabledFormat,
    ...rest
  } = props;
  const {
    getPrefixCls,
    direction,
    className: contextClassName,
    style: contextStyle,
    classNames: contextClassNames,
    styles: contextStyles,
    arrow: contextArrow
  } = useComponentConfig("colorPicker");
  const contextDisabled = reactExports.useContext(DisabledContext);
  const mergedDisabled = disabled ?? contextDisabled;
  const prefixCls = getPrefixCls("color-picker", customizePrefixCls);
  const mergedArrow = useMergedArrow(arrow, contextArrow);
  const {
    compactSize,
    compactItemClassnames
  } = useCompactItemContext(prefixCls, direction);
  const mergedSize = useSize((ctx) => customizeSize ?? compactSize ?? ctx);
  const mergedProps = {
    ...props,
    trigger,
    allowClear,
    autoAdjustOverflow,
    disabledAlpha,
    arrow: mergedArrow,
    placement,
    disabled: mergedDisabled,
    size: mergedSize
  };
  const contextStyleRoot = useSemanticRootStyle(contextStyle);
  const styleRoot = useSemanticRootStyle(style);
  const [mergedClassNames, mergedStyles] = useMergeSemantic([contextClassNames, classNames], [contextStyles, contextStyleRoot, styles, styleRoot], {
    props: mergedProps
  }, {
    popup: {
      _default: "root"
    }
  });
  const [internalPopupOpen, setPopupOpen] = useControlledState(false, open);
  const popupOpen = !mergedDisabled && internalPopupOpen;
  const [formatValue, setFormatValue] = useControlledState(defaultFormat, format);
  const triggerFormatChange = (newFormat) => {
    setFormatValue(newFormat);
    if (formatValue !== newFormat) {
      onFormatChange?.(newFormat);
    }
  };
  const triggerOpenChange = (visible) => {
    if (!visible || !mergedDisabled) {
      setPopupOpen(visible);
      onOpenChange?.(visible);
    }
  };
  const [mergedColor, setColor, modeState, setModeState, modeOptions] = useModeColor(defaultValue, value, mode);
  const isAlphaColor = reactExports.useMemo(() => getColorAlpha(mergedColor) < 100, [mergedColor]);
  const [cachedGradientColor, setCachedGradientColor] = React.useState(null);
  const onInternalChangeComplete = (color) => {
    if (onChangeComplete) {
      let changeColor = generateColor$1(color);
      if (disabledAlpha && isAlphaColor) {
        changeColor = genAlphaColor(color);
      }
      onChangeComplete(changeColor);
    }
  };
  const onInternalChange = (data, changeFromPickerDrag) => {
    let color = generateColor$1(data);
    if (disabledAlpha && isAlphaColor) {
      color = genAlphaColor(color);
    }
    setColor(color);
    setCachedGradientColor(null);
    if (onChange) {
      onChange(color, color.toCssString());
    }
    if (!changeFromPickerDrag) {
      onInternalChangeComplete(color);
    }
  };
  const [activeIndex, setActiveIndex] = React.useState(0);
  const [gradientDragging, setGradientDragging] = React.useState(false);
  const onInternalModeChange = (newMode) => {
    setModeState(newMode);
    if (newMode === "single" && mergedColor.isGradient()) {
      setActiveIndex(0);
      onInternalChange(new AggregationColor(mergedColor.getColors()[0].color));
      setCachedGradientColor(mergedColor);
    } else if (newMode === "gradient" && !mergedColor.isGradient()) {
      const baseColor = isAlphaColor ? genAlphaColor(mergedColor) : mergedColor;
      onInternalChange(new AggregationColor(cachedGradientColor || [{
        percent: 0,
        color: baseColor
      }, {
        percent: 100,
        color: baseColor
      }]));
    }
  };
  const {
    status: contextStatus
  } = React.useContext(FormItemInputContext);
  const rootCls = useCSSVarCls(prefixCls);
  const [hashId, cssVarCls] = useStyle(prefixCls, rootCls);
  const rtlCls = {
    [`${prefixCls}-rtl`]: direction
  };
  const mergedRootCls = clsx(rootClassName, cssVarCls, rootCls, rtlCls);
  const mergedCls = clsx(getStatusClassNames(prefixCls, contextStatus), {
    [`${prefixCls}-sm`]: mergedSize === "small",
    [`${prefixCls}-lg`]: mergedSize === "large"
  }, compactItemClassnames, contextClassName, mergedRootCls, className, hashId);
  const mergedPopupCls = clsx(prefixCls, mergedRootCls, mergedClassNames.popup?.root);
  const popoverProps = {
    open: popupOpen,
    trigger,
    placement,
    arrow: mergedArrow,
    rootClassName,
    getPopupContainer,
    autoAdjustOverflow,
    destroyOnHidden: destroyOnHidden ?? !!destroyTooltipOnHide
  };
  return /* @__PURE__ */ React.createElement(Popover, {
    classNames: {
      root: mergedPopupCls
    },
    styles: {
      root: mergedStyles.popup?.root,
      container: styles?.popupOverlayInner
    },
    onOpenChange: triggerOpenChange,
    content: /* @__PURE__ */ React.createElement(ContextIsolator, {
      form: true
    }, /* @__PURE__ */ React.createElement(ColorPickerPanel, {
      mode: modeState,
      onModeChange: onInternalModeChange,
      modeOptions,
      prefixCls,
      value: mergedColor,
      allowClear,
      disabled: mergedDisabled,
      disabledAlpha,
      presets,
      panelRender,
      format: formatValue,
      onFormatChange: triggerFormatChange,
      onChange: onInternalChange,
      onChangeComplete: onInternalChangeComplete,
      onClear,
      activeIndex,
      onActive: setActiveIndex,
      gradientDragging,
      onGradientDragging: setGradientDragging,
      disabledFormat
    })),
    ...popoverProps
  }, children || /* @__PURE__ */ React.createElement(ColorTrigger, {
    activeIndex: popupOpen ? activeIndex : -1,
    open: popupOpen,
    className: mergedCls,
    classNames: mergedClassNames,
    styles: mergedStyles,
    prefixCls,
    disabled: mergedDisabled,
    showText,
    format: formatValue,
    ...rest,
    color: mergedColor
  }));
};
const PurePanel = genPurePanel(
  ColorPicker,
  void 0,
  (props) => ({
    ...props,
    placement: "bottom",
    autoAdjustOverflow: false
  }),
  "color-picker",
  /* istanbul ignore next */
  (prefixCls) => prefixCls
);
ColorPicker._InternalPanelDoNotUseOrYouWillBeFired = PurePanel;
export {
  ColorPicker as C,
  Radio as R,
  Slider as S
};
