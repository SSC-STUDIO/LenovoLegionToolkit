import { r as reactExports, bK as useMergedValue, bL as useCount, bM as useCountDisplay, bN as useCountExceed, $ as React, bO as BaseInput, f as clsx, aW as triggerFocus, h as omit, bP as resolveOnChange, J as useComponentConfig, bQ as useSharedStyle, bz as useStyle, b7 as useCompactItemContext, aL as useSize, aK as DisabledContext, L as useSemanticRootStyle, N as useMergeSemantic, aJ as FormItemInputContext, bR as useAllowClear, b8 as useVariant, b9 as getStatusClassNames, ai as ContextIsolator, af as composeRef, Q as useCSSVarCls, ba as getMergedStatus } from "./index-3RTipSd5.js";
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
const Input$1 = /* @__PURE__ */ reactExports.forwardRef((props, ref) => {
  const {
    autoComplete,
    onChange,
    onFocus,
    onBlur,
    onPressEnter,
    onKeyDown,
    onKeyUp,
    prefixCls = "rc-input",
    disabled,
    htmlSize,
    className,
    maxLength,
    suffix,
    showCount,
    count,
    type = "text",
    classes,
    classNames,
    styles,
    onCompositionStart,
    onCompositionEnd,
    ...rest
  } = props;
  const [focused, setFocused] = reactExports.useState(false);
  const compositionRef = reactExports.useRef(false);
  const keyLockRef = reactExports.useRef(false);
  const inputRef = reactExports.useRef(null);
  const holderRef = reactExports.useRef(null);
  const focus = (option) => {
    if (inputRef.current) {
      triggerFocus(inputRef.current, option);
    }
  };
  const {
    setValue,
    formatValue
  } = useMergedValue(props.defaultValue, props.value);
  const countConfig = useCount(count, showCount);
  const {
    isOutOfRange,
    dataCount
  } = useCountDisplay({
    countConfig,
    value: formatValue,
    maxLength
  });
  const getExceedValue = useCountExceed({
    countConfig,
    getTarget: () => inputRef.current
  });
  reactExports.useImperativeHandle(ref, () => ({
    focus,
    blur: () => {
      inputRef.current?.blur();
    },
    setSelectionRange: (start, end, direction) => {
      inputRef.current?.setSelectionRange(start, end, direction);
    },
    select: () => {
      inputRef.current?.select();
    },
    input: inputRef.current,
    nativeElement: holderRef.current?.nativeElement || inputRef.current
  }));
  reactExports.useEffect(() => {
    if (keyLockRef.current) {
      keyLockRef.current = false;
    }
    setFocused((prev) => prev && disabled ? false : prev);
  }, [disabled]);
  const triggerChange = (e, currentValue, info) => {
    const cutValue = getExceedValue(currentValue, compositionRef.current);
    if (info.source === "compositionEnd" && currentValue === cutValue) {
      return;
    }
    setValue(cutValue);
    if (inputRef.current) {
      resolveOnChange(inputRef.current, e, onChange, cutValue);
    }
  };
  const onInternalChange = (e) => {
    triggerChange(e, e.target.value, {
      source: "change"
    });
  };
  const onInternalCompositionEnd = (e) => {
    compositionRef.current = false;
    triggerChange(e, e.currentTarget.value, {
      source: "compositionEnd"
    });
    onCompositionEnd?.(e);
  };
  const handleKeyDown = (e) => {
    if (onPressEnter && e.key === "Enter" && !keyLockRef.current && !e.nativeEvent.isComposing) {
      keyLockRef.current = true;
      onPressEnter(e);
    }
    onKeyDown?.(e);
  };
  const handleKeyUp = (e) => {
    if (e.key === "Enter") {
      keyLockRef.current = false;
    }
    onKeyUp?.(e);
  };
  const handleFocus = (e) => {
    setFocused(true);
    onFocus?.(e);
  };
  const handleBlur = (e) => {
    if (keyLockRef.current) {
      keyLockRef.current = false;
    }
    setFocused(false);
    onBlur?.(e);
  };
  const handleReset = (e) => {
    setValue("");
    focus();
    if (inputRef.current) {
      resolveOnChange(inputRef.current, e, onChange);
    }
  };
  const outOfRangeCls = isOutOfRange && `${prefixCls}-out-of-range`;
  const getInputElement = () => {
    const otherProps = omit(props, [
      "prefixCls",
      "onPressEnter",
      "addonBefore",
      "addonAfter",
      "prefix",
      "suffix",
      "allowClear",
      // Input elements must be either controlled or uncontrolled,
      // specify either the value prop, or the defaultValue prop, but not both.
      "defaultValue",
      "showCount",
      "count",
      "classes",
      "htmlSize",
      "styles",
      "classNames",
      "onClear"
    ]);
    return /* @__PURE__ */ React.createElement("input", _extends({
      autoComplete
    }, otherProps, {
      onChange: onInternalChange,
      onFocus: handleFocus,
      onBlur: handleBlur,
      onKeyDown: handleKeyDown,
      onKeyUp: handleKeyUp,
      className: clsx(prefixCls, {
        [`${prefixCls}-disabled`]: disabled
      }, classNames?.input),
      style: styles?.input,
      ref: inputRef,
      size: htmlSize,
      type,
      onCompositionStart: (e) => {
        compositionRef.current = true;
        onCompositionStart?.(e);
      },
      onCompositionEnd: onInternalCompositionEnd
    }));
  };
  const getSuffix = () => {
    if (suffix || countConfig.show) {
      return /* @__PURE__ */ React.createElement(React.Fragment, null, countConfig.show && /* @__PURE__ */ React.createElement("span", {
        className: clsx(`${prefixCls}-show-count-suffix`, {
          [`${prefixCls}-show-count-has-suffix`]: !!suffix
        }, classNames?.count),
        style: {
          ...styles?.count
        }
      }, dataCount), suffix);
    }
    return null;
  };
  return /* @__PURE__ */ React.createElement(BaseInput, _extends({}, rest, {
    prefixCls,
    className: clsx(className, outOfRangeCls),
    handleReset,
    value: formatValue,
    focused,
    triggerFocus: focus,
    suffix: getSuffix(),
    disabled,
    classes,
    classNames,
    styles,
    ref: holderRef
  }), getInputElement());
});
function useRemovePasswordTimeout(inputRef, triggerOnMount) {
  const removePasswordTimeoutRef = reactExports.useRef([]);
  const removePasswordTimeout = () => {
    removePasswordTimeoutRef.current.push(setTimeout(() => {
      if (inputRef.current?.input && inputRef.current?.input.getAttribute("type") === "password" && inputRef.current?.input.hasAttribute("value")) {
        inputRef.current?.input.removeAttribute("value");
      }
    }));
  };
  reactExports.useEffect(() => {
    if (triggerOnMount) {
      removePasswordTimeout();
    }
    return () => removePasswordTimeoutRef.current.forEach((timer) => {
      if (timer) {
        clearTimeout(timer);
      }
    });
  }, [triggerOnMount]);
  return removePasswordTimeout;
}
function hasPrefixSuffix(props) {
  return !!(props.prefix || props.suffix || props.allowClear || props.showCount);
}
const Input = /* @__PURE__ */ reactExports.forwardRef((props, ref) => {
  const {
    prefixCls: customizePrefixCls,
    bordered = true,
    status: customStatus,
    size: customSize,
    disabled: customDisabled,
    onBlur,
    onFocus,
    suffix,
    allowClear,
    addonAfter,
    addonBefore,
    className,
    style,
    styles,
    rootClassName,
    onChange,
    classNames,
    variant: customVariant,
    ...rest
  } = props;
  const {
    getPrefixCls,
    direction,
    allowClear: contextAllowClear,
    autoComplete: contextAutoComplete,
    className: contextClassName,
    style: contextStyle,
    classNames: contextClassNames,
    styles: contextStyles
  } = useComponentConfig("input");
  const prefixCls = getPrefixCls("input", customizePrefixCls);
  const inputRef = reactExports.useRef(null);
  const rootCls = useCSSVarCls(prefixCls);
  const [hashId, cssVarCls] = useSharedStyle(prefixCls, rootClassName);
  useStyle(prefixCls, rootCls);
  const {
    compactSize,
    compactItemClassnames
  } = useCompactItemContext(prefixCls, direction);
  const mergedSize = useSize((ctx) => customSize ?? compactSize ?? ctx);
  const disabled = React.useContext(DisabledContext);
  const mergedDisabled = customDisabled ?? disabled;
  const mergedProps = {
    ...props,
    size: mergedSize,
    disabled: mergedDisabled
  };
  const contextStyleRoot = useSemanticRootStyle(contextStyle);
  const styleRoot = useSemanticRootStyle(style);
  const [mergedClassNames, mergedStyles] = useMergeSemantic([contextClassNames, classNames], [contextStyles, contextStyleRoot, styles, styleRoot], {
    props: mergedProps
  });
  const {
    status: contextStatus,
    hasFeedback,
    feedbackIcon
  } = reactExports.useContext(FormItemInputContext);
  const mergedStatus = getMergedStatus(contextStatus, customStatus);
  const inputHasPrefixSuffix = hasPrefixSuffix(props) || !!hasFeedback;
  reactExports.useRef(inputHasPrefixSuffix);
  const removePasswordTimeout = useRemovePasswordTimeout(inputRef, true);
  const handleBlur = (e) => {
    removePasswordTimeout();
    onBlur?.(e);
  };
  const handleFocus = (e) => {
    removePasswordTimeout();
    onFocus?.(e);
  };
  const handleChange = (e) => {
    removePasswordTimeout();
    onChange?.(e);
  };
  const suffixNode = (hasFeedback || suffix) && /* @__PURE__ */ React.createElement(React.Fragment, null, suffix, hasFeedback && feedbackIcon);
  const mergedAllowClear = useAllowClear({
    allowClear,
    contextAllowClear,
    componentName: "Input"
  });
  const [variant, enableVariantCls] = useVariant("input", customVariant, bordered);
  return /* @__PURE__ */ React.createElement(Input$1, {
    ref: composeRef(ref, inputRef),
    prefixCls,
    autoComplete: contextAutoComplete,
    ...rest,
    disabled: mergedDisabled,
    onBlur: handleBlur,
    onFocus: handleFocus,
    style: mergedStyles.root,
    styles: mergedStyles,
    suffix: suffixNode,
    allowClear: mergedAllowClear,
    className: clsx(className, rootClassName, cssVarCls, rootCls, compactItemClassnames, contextClassName, mergedClassNames.root),
    onChange: handleChange,
    addonBefore: addonBefore && /* @__PURE__ */ React.createElement(ContextIsolator, {
      form: true,
      space: true
    }, addonBefore),
    addonAfter: addonAfter && /* @__PURE__ */ React.createElement(ContextIsolator, {
      form: true,
      space: true
    }, addonAfter),
    classNames: {
      ...mergedClassNames,
      input: clsx({
        [`${prefixCls}-sm`]: mergedSize === "small",
        [`${prefixCls}-lg`]: mergedSize === "large",
        [`${prefixCls}-rtl`]: direction === "rtl"
      }, mergedClassNames.input, hashId),
      variant: clsx({
        [`${prefixCls}-${variant}`]: enableVariantCls
      }, getStatusClassNames(prefixCls, mergedStatus)),
      affixWrapper: clsx({
        [`${prefixCls}-affix-wrapper-sm`]: mergedSize === "small",
        [`${prefixCls}-affix-wrapper-lg`]: mergedSize === "large",
        [`${prefixCls}-affix-wrapper-rtl`]: direction === "rtl"
      }, hashId),
      wrapper: clsx({
        [`${prefixCls}-group-rtl`]: direction === "rtl"
      }, hashId),
      groupWrapper: clsx({
        [`${prefixCls}-group-wrapper-sm`]: mergedSize === "small",
        [`${prefixCls}-group-wrapper-lg`]: mergedSize === "large",
        [`${prefixCls}-group-wrapper-rtl`]: direction === "rtl",
        [`${prefixCls}-group-wrapper-${variant}`]: enableVariantCls
      }, getStatusClassNames(`${prefixCls}-group-wrapper`, mergedStatus, hasFeedback), hashId)
    }
  });
});
export {
  Input as I,
  useRemovePasswordTimeout as u
};
