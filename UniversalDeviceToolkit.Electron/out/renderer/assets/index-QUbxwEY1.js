import { r as reactExports, aE as useControlledState, f as clsx, $ as React, aV as wrapperRaf, v as genStyleHooks, w as merge, n as unit, A as resetComponent, J as useComponentConfig, aL as useSize, aM as useOrientation, L as useSemanticRootStyle, N as useMergeSemantic, ah as isNumber } from "./index-3RTipSd5.js";
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
const Checkbox = /* @__PURE__ */ reactExports.forwardRef((props, ref) => {
  const {
    prefixCls = "rc-checkbox",
    className,
    style,
    checked,
    disabled,
    defaultChecked = false,
    type = "checkbox",
    title,
    onChange,
    ...inputProps
  } = props;
  const inputRef = reactExports.useRef(null);
  const holderRef = reactExports.useRef(null);
  const [rawValue, setRawValue] = useControlledState(defaultChecked, checked);
  reactExports.useImperativeHandle(ref, () => ({
    focus: (options) => {
      inputRef.current?.focus(options);
    },
    blur: () => {
      inputRef.current?.blur();
    },
    input: inputRef.current,
    nativeElement: holderRef.current
  }));
  const classString = clsx(prefixCls, className, {
    [`${prefixCls}-checked`]: rawValue,
    [`${prefixCls}-disabled`]: disabled
  });
  const handleChange = (e) => {
    if (disabled) {
      return;
    }
    if (!("checked" in props)) {
      setRawValue(e.target.checked);
    }
    onChange?.({
      target: {
        ...props,
        type,
        checked: e.target.checked
      },
      stopPropagation() {
        e.stopPropagation();
      },
      preventDefault() {
        e.preventDefault();
      },
      nativeEvent: e.nativeEvent
    });
  };
  return /* @__PURE__ */ reactExports.createElement("span", {
    className: classString,
    title,
    style,
    ref: holderRef
  }, /* @__PURE__ */ reactExports.createElement("input", _extends({}, inputProps, {
    className: `${prefixCls}-input`,
    ref: inputRef,
    onChange: handleChange,
    disabled,
    checked: !!rawValue,
    type
  })));
});
function useBubbleLock(onOriginInputClick) {
  const labelClickLockRef = React.useRef(null);
  const clearLock = () => {
    wrapperRaf.cancel(labelClickLockRef.current);
    labelClickLockRef.current = null;
  };
  const onLabelClick = () => {
    clearLock();
    labelClickLockRef.current = wrapperRaf(() => {
      labelClickLockRef.current = null;
    });
  };
  const onInputClick = (e) => {
    if (labelClickLockRef.current) {
      e.stopPropagation();
      clearLock();
    }
    onOriginInputClick?.(e);
  };
  return [onLabelClick, onInputClick];
}
const genSizeDividerStyle = (token) => {
  const {
    componentCls
  } = token;
  return {
    [componentCls]: {
      "&-horizontal": {
        [`&${componentCls}`]: {
          "&-sm": {
            marginBlock: token.marginXS
          },
          "&-md": {
            marginBlock: token.margin
          }
        }
      }
    }
  };
};
const genSharedDividerStyle = (token) => {
  const {
    componentCls,
    sizePaddingEdgeHorizontal,
    colorSplit,
    lineWidth,
    textPaddingInline,
    orientationMargin,
    verticalMarginInline
  } = token;
  const railCls = `${componentCls}-rail`;
  return {
    [componentCls]: {
      ...resetComponent(token),
      borderBlockStart: `${unit(lineWidth)} solid ${colorSplit}`,
      [railCls]: {
        borderBlockStart: `${unit(lineWidth)} solid ${colorSplit}`
      },
      // vertical
      "&-vertical": {
        position: "relative",
        top: "-0.06em",
        display: "inline-block",
        height: "0.9em",
        marginInline: verticalMarginInline,
        marginBlock: 0,
        verticalAlign: "middle",
        borderTop: 0,
        borderInlineStart: `${unit(lineWidth)} solid ${colorSplit}`
      },
      "&-horizontal": {
        display: "flex",
        clear: "both",
        width: "100%",
        minWidth: "100%",
        // Fix https://github.com/ant-design/ant-design/issues/10914
        margin: `${unit(token.marginLG)} 0`
      },
      [`&-horizontal${componentCls}-with-text`]: {
        display: "flex",
        alignItems: "center",
        margin: `${unit(token.dividerHorizontalWithTextGutterMargin)} 0`,
        color: token.colorTextHeading,
        fontWeight: 500,
        fontSize: token.fontSizeLG,
        whiteSpace: "nowrap",
        textAlign: "center",
        borderBlockStart: `0 ${colorSplit}`,
        [`${railCls}-start, ${railCls}-end`]: {
          width: "50%",
          // Chrome not accept `inherit` in `border-top`
          borderBlockStartColor: "inherit",
          borderBlockEnd: 0,
          content: "''"
        }
      },
      [`&-horizontal${componentCls}-with-text-start`]: {
        [`${railCls}-start`]: {
          width: `calc(${orientationMargin} * 100%)`
        },
        [`${railCls}-end`]: {
          width: `calc(100% - ${orientationMargin} * 100%)`
        }
      },
      [`&-horizontal${componentCls}-with-text-end`]: {
        [`${railCls}-start`]: {
          width: `calc(100% - ${orientationMargin} * 100%)`
        },
        [`${railCls}-end`]: {
          width: `calc(${orientationMargin} * 100%)`
        }
      },
      [`${componentCls}-inner-text`]: {
        display: "inline-block",
        paddingBlock: 0,
        paddingInline: textPaddingInline
      },
      "&-dashed": {
        background: "none",
        borderColor: colorSplit,
        borderStyle: "dashed",
        borderWidth: `${unit(lineWidth)} 0 0`,
        [railCls]: {
          borderBlockStart: `${unit(lineWidth)} dashed ${colorSplit}`
        }
      },
      [`&-horizontal${componentCls}-with-text${componentCls}-dashed`]: {
        [`${railCls}-start, ${railCls}-end`]: {
          borderStyle: "dashed none none"
        }
      },
      [`&-vertical${componentCls}-dashed`]: {
        borderInlineStartWidth: lineWidth,
        borderInlineEnd: 0,
        borderBlockStart: 0,
        borderBlockEnd: 0
      },
      "&-dotted": {
        background: "none",
        borderColor: colorSplit,
        borderStyle: "dotted",
        borderWidth: `${unit(lineWidth)} 0 0`,
        [railCls]: {
          borderBlockStart: `${unit(lineWidth)} dotted ${colorSplit}`
        }
      },
      [`&-horizontal${componentCls}-with-text${componentCls}-dotted`]: {
        "&::before, &::after": {
          borderStyle: "dotted none none"
        }
      },
      [`&-vertical${componentCls}-dotted`]: {
        borderInlineStartWidth: lineWidth,
        borderInlineEnd: 0,
        borderBlockStart: 0,
        borderBlockEnd: 0
      },
      [`&-plain${componentCls}-with-text`]: {
        color: token.colorText,
        fontWeight: "normal",
        fontSize: token.fontSize
      },
      [`&-horizontal${componentCls}-with-text-start${componentCls}-no-default-orientation-margin-start`]: {
        [`${railCls}-start`]: {
          width: 0
        },
        [`${railCls}-end`]: {
          width: "100%"
        },
        [`${componentCls}-inner-text`]: {
          paddingInlineStart: sizePaddingEdgeHorizontal
        }
      },
      [`&-horizontal${componentCls}-with-text-end${componentCls}-no-default-orientation-margin-end`]: {
        [`${railCls}-start`]: {
          width: "100%"
        },
        [`${railCls}-end`]: {
          width: 0
        },
        [`${componentCls}-inner-text`]: {
          paddingInlineEnd: sizePaddingEdgeHorizontal
        }
      }
    }
  };
};
const prepareComponentToken = (token) => ({
  textPaddingInline: "1em",
  orientationMargin: 0.05,
  verticalMarginInline: token.marginXS
});
const useStyle = genStyleHooks("Divider", (token) => {
  const dividerToken = merge(token, {
    dividerHorizontalWithTextGutterMargin: token.margin,
    sizePaddingEdgeHorizontal: 0
  });
  return [genSharedDividerStyle(dividerToken), genSizeDividerStyle(dividerToken)];
}, prepareComponentToken, {
  unitless: {
    orientationMargin: true
  }
});
const titlePlacementList = ["left", "right", "center", "start", "end"];
const Divider = /* @__PURE__ */ reactExports.forwardRef((props, ref) => {
  const {
    getPrefixCls,
    direction,
    className: contextClassName,
    style: contextStyle,
    classNames: contextClassNames,
    styles: contextStyles
  } = useComponentConfig("divider");
  const {
    prefixCls: customizePrefixCls,
    type,
    orientation,
    vertical,
    titlePlacement,
    orientationMargin,
    className,
    rootClassName,
    children,
    dashed,
    variant = "solid",
    plain,
    style,
    size: customSize,
    classNames,
    styles,
    ...restProps
  } = props;
  const prefixCls = getPrefixCls("divider", customizePrefixCls);
  const railCls = `${prefixCls}-rail`;
  const [hashId, cssVarCls] = useStyle(prefixCls);
  const sizeFullName = useSize(customSize);
  const hasChildren = !!children;
  const validTitlePlacement = titlePlacementList.includes(orientation || "");
  const mergedTitlePlacement = reactExports.useMemo(() => {
    const placement = titlePlacement ?? (validTitlePlacement ? orientation : "center");
    if (placement === "left") {
      return direction === "rtl" ? "end" : "start";
    }
    if (placement === "right") {
      return direction === "rtl" ? "start" : "end";
    }
    return placement;
  }, [direction, orientation, titlePlacement, validTitlePlacement]);
  const hasMarginStart = mergedTitlePlacement === "start" && orientationMargin != null;
  const hasMarginEnd = mergedTitlePlacement === "end" && orientationMargin != null;
  const [mergedOrientation, mergedVertical] = useOrientation(orientation, vertical, type);
  const mergedProps = {
    ...props,
    orientation: mergedOrientation,
    titlePlacement: mergedTitlePlacement,
    size: sizeFullName
  };
  const contextStyleRoot = useSemanticRootStyle(contextStyle);
  const [mergedClassNames, mergedStyles] = useMergeSemantic([contextClassNames, classNames], [contextStyles, contextStyleRoot, styles], {
    props: mergedProps
  });
  const classString = clsx(prefixCls, contextClassName, hashId, cssVarCls, `${prefixCls}-${mergedOrientation}`, {
    [`${prefixCls}-with-text`]: hasChildren,
    [`${prefixCls}-with-text-${mergedTitlePlacement}`]: hasChildren,
    [`${prefixCls}-dashed`]: !!dashed,
    [`${prefixCls}-${variant}`]: variant !== "solid",
    [`${prefixCls}-plain`]: !!plain,
    [`${prefixCls}-rtl`]: direction === "rtl",
    [`${prefixCls}-no-default-orientation-margin-start`]: hasMarginStart,
    [`${prefixCls}-no-default-orientation-margin-end`]: hasMarginEnd,
    [`${prefixCls}-md`]: sizeFullName === "medium" || sizeFullName === "middle",
    [`${prefixCls}-sm`]: sizeFullName === "small",
    [railCls]: !children,
    [mergedClassNames.rail]: mergedClassNames.rail && !children
  }, className, rootClassName, mergedClassNames.root);
  const memoizedPlacementMargin = reactExports.useMemo(() => {
    if (isNumber(orientationMargin)) {
      return orientationMargin;
    }
    if (/^\d+$/.test(orientationMargin)) {
      return Number(orientationMargin);
    }
    return orientationMargin;
  }, [orientationMargin]);
  const innerStyle = {
    marginInlineStart: hasMarginStart ? memoizedPlacementMargin : void 0,
    marginInlineEnd: hasMarginEnd ? memoizedPlacementMargin : void 0
  };
  const nativeElementRef = reactExports.useRef(null);
  reactExports.useImperativeHandle(ref, () => ({
    nativeElement: nativeElementRef.current
  }));
  return /* @__PURE__ */ reactExports.createElement("div", {
    ref: nativeElementRef,
    className: classString,
    style: {
      ...mergedStyles.root,
      ...children ? {} : mergedStyles.rail,
      ...style
    },
    ...restProps,
    role: "separator"
  }, children && !mergedVertical && /* @__PURE__ */ reactExports.createElement(reactExports.Fragment, null, /* @__PURE__ */ reactExports.createElement("div", {
    className: clsx(railCls, `${railCls}-start`, mergedClassNames.rail),
    style: mergedStyles.rail
  }), /* @__PURE__ */ reactExports.createElement("span", {
    className: clsx(`${prefixCls}-inner-text`, mergedClassNames.content),
    style: {
      ...innerStyle,
      ...mergedStyles.content
    }
  }, children), /* @__PURE__ */ reactExports.createElement("div", {
    className: clsx(railCls, `${railCls}-end`, mergedClassNames.rail),
    style: mergedStyles.rail
  })));
});
export {
  Checkbox as C,
  Divider as D,
  useBubbleLock as u
};
