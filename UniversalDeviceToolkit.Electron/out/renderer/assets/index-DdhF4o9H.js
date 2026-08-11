import { r as reactExports, bS as useSafeState, bT as isThenable, v as genStyleHooks, ao as RefIcon, e as ConfigContext, a8 as useLocale, az as localeValues, f as clsx, as as isReactRenderable, J as useComponentConfig, aE as useControlledState, bo as useMergedArrow, L as useSemanticRootStyle, N as useMergeSemantic, h as omit } from "./index-3RTipSd5.js";
import { g as getRenderPropValue, a as PurePanel$1, P as Popover } from "./index-Hdt_DTHG.js";
import { B as Button, c as convertLegacyProps } from "./index-uyL__3sF.js";
const ActionButton = (props) => {
  const {
    type,
    children,
    prefixCls,
    buttonProps,
    close,
    autoFocus,
    emitEvent,
    isSilent,
    quitOnNullishReturnValue,
    actionFn
  } = props;
  const clickedRef = reactExports.useRef(false);
  const buttonRef = reactExports.useRef(null);
  const [loading, setLoading] = useSafeState(false);
  const onInternalClose = (...args) => {
    close?.(...args);
  };
  reactExports.useEffect(() => {
    let timeoutId = null;
    if (autoFocus) {
      timeoutId = setTimeout(() => {
        buttonRef.current?.focus({
          preventScroll: true
        });
      });
    }
    return () => {
      if (timeoutId) {
        clearTimeout(timeoutId);
      }
    };
  }, [autoFocus]);
  const handlePromiseOnOk = (returnValueOfOnOk) => {
    if (!isThenable(returnValueOfOnOk)) {
      return;
    }
    setLoading(true);
    returnValueOfOnOk.then((...args) => {
      setLoading(false, true);
      onInternalClose.apply(void 0, args);
      clickedRef.current = false;
    }, (e) => {
      setLoading(false, true);
      clickedRef.current = false;
      if (isSilent?.()) {
        return;
      }
      return Promise.reject(e);
    });
  };
  const onClick = (e) => {
    if (clickedRef.current) {
      return;
    }
    clickedRef.current = true;
    if (!actionFn) {
      onInternalClose();
      return;
    }
    let returnValueOfOnOk;
    if (emitEvent) {
      returnValueOfOnOk = actionFn(e);
      if (quitOnNullishReturnValue && !isThenable(returnValueOfOnOk)) {
        clickedRef.current = false;
        onInternalClose(e);
        return;
      }
    } else if (actionFn.length) {
      returnValueOfOnOk = actionFn(close);
      clickedRef.current = false;
    } else {
      returnValueOfOnOk = actionFn();
      if (!isThenable(returnValueOfOnOk)) {
        onInternalClose();
        return;
      }
    }
    handlePromiseOnOk(returnValueOfOnOk);
  };
  return /* @__PURE__ */ reactExports.createElement(Button, {
    ...convertLegacyProps(type),
    onClick,
    loading,
    prefixCls,
    ...buttonProps,
    ref: buttonRef
  }, children);
};
const genBaseStyle = (token) => {
  const {
    componentCls,
    iconCls,
    antCls,
    zIndexPopup,
    colorText,
    colorWarning,
    marginXXS,
    marginXS,
    fontSize,
    fontWeightStrong,
    colorTextHeading
  } = token;
  return {
    [componentCls]: {
      zIndex: zIndexPopup,
      [`&${antCls}-popover`]: {
        fontSize
      },
      [`${componentCls}-message`]: {
        marginBottom: marginXS,
        display: "flex",
        flexWrap: "nowrap",
        alignItems: "start",
        [`> ${componentCls}-message-icon`]: {
          color: colorWarning
        },
        [`> ${componentCls}-message-icon ${iconCls}`]: {
          fontSize,
          lineHeight: 1,
          marginInlineEnd: marginXS
        },
        [`${componentCls}-title`]: {
          fontWeight: fontWeightStrong,
          color: colorTextHeading,
          "&:only-child": {
            fontWeight: "normal"
          }
        },
        [`${componentCls}-description`]: {
          marginTop: marginXXS,
          color: colorText
        }
      },
      [`${componentCls}-buttons`]: {
        textAlign: "end",
        whiteSpace: "nowrap",
        button: {
          marginInlineStart: marginXS
        }
      }
    }
  };
};
const prepareComponentToken = (token) => {
  const {
    zIndexPopupBase
  } = token;
  return {
    zIndexPopup: zIndexPopupBase + 60
  };
};
const useStyle = genStyleHooks("Popconfirm", genBaseStyle, prepareComponentToken, {
  resetStyle: false
});
const Overlay = (props) => {
  const {
    prefixCls,
    okButtonProps,
    cancelButtonProps,
    title,
    description,
    cancelText,
    okText,
    okType = "primary",
    icon = /* @__PURE__ */ reactExports.createElement(RefIcon, null),
    showCancel = true,
    close,
    onConfirm,
    onCancel,
    onPopupClick,
    classNames,
    styles
  } = props;
  const {
    getPrefixCls
  } = reactExports.useContext(ConfigContext);
  const [contextLocale] = useLocale("Popconfirm", localeValues.Popconfirm);
  const titleNode = getRenderPropValue(title);
  const descriptionNode = getRenderPropValue(description);
  return /* @__PURE__ */ reactExports.createElement("div", {
    className: `${prefixCls}-inner-content`,
    onClick: onPopupClick
  }, /* @__PURE__ */ reactExports.createElement("div", {
    className: `${prefixCls}-message`
  }, icon && /* @__PURE__ */ reactExports.createElement("span", {
    className: clsx(`${prefixCls}-message-icon`, classNames?.icon),
    style: styles?.icon
  }, icon), /* @__PURE__ */ reactExports.createElement("div", {
    className: `${prefixCls}-message-text`
  }, isReactRenderable(titleNode) && /* @__PURE__ */ reactExports.createElement("div", {
    className: clsx(`${prefixCls}-title`, classNames?.title),
    style: styles?.title
  }, titleNode), isReactRenderable(descriptionNode) && /* @__PURE__ */ reactExports.createElement("div", {
    className: clsx(`${prefixCls}-description`, classNames?.content),
    style: styles?.content
  }, descriptionNode))), /* @__PURE__ */ reactExports.createElement("div", {
    className: `${prefixCls}-buttons`
  }, showCancel && /* @__PURE__ */ reactExports.createElement(Button, {
    onClick: onCancel,
    size: "small",
    ...cancelButtonProps
  }, cancelText || contextLocale?.cancelText), /* @__PURE__ */ reactExports.createElement(ActionButton, {
    buttonProps: {
      size: "small",
      ...convertLegacyProps(okType),
      ...okButtonProps
    },
    actionFn: onConfirm,
    close,
    prefixCls: getPrefixCls("btn"),
    quitOnNullishReturnValue: true,
    emitEvent: true
  }, okText || contextLocale?.okText)));
};
const PurePanel = (props) => {
  const {
    prefixCls: customizePrefixCls,
    placement,
    className,
    style,
    ...restProps
  } = props;
  const {
    getPrefixCls
  } = reactExports.useContext(ConfigContext);
  const prefixCls = getPrefixCls("popconfirm", customizePrefixCls);
  useStyle(prefixCls);
  return /* @__PURE__ */ reactExports.createElement(PurePanel$1, {
    placement,
    className: clsx(prefixCls, className),
    style,
    content: /* @__PURE__ */ reactExports.createElement(Overlay, {
      prefixCls,
      ...restProps
    })
  });
};
const InternalPopconfirm = /* @__PURE__ */ reactExports.forwardRef((props, ref) => {
  const {
    prefixCls: customizePrefixCls,
    placement = "top",
    trigger,
    okType = "primary",
    icon = /* @__PURE__ */ reactExports.createElement(RefIcon, null),
    children,
    overlayClassName,
    onOpenChange,
    overlayStyle,
    styles,
    arrow: popconfirmArrow,
    classNames,
    disabled = false,
    mouseEnterDelay,
    mouseLeaveDelay,
    ...restProps
  } = props;
  const {
    getPrefixCls,
    className: contextClassName,
    style: contextStyle,
    classNames: contextClassNames,
    styles: contextStyles,
    arrow: contextArrow,
    trigger: contextTrigger,
    mouseEnterDelay: contextMouseEnterDelay,
    mouseLeaveDelay: contextMouseLeaveDelay
  } = useComponentConfig("popconfirm");
  const [open, setOpen] = useControlledState(props.defaultOpen ?? false, props.open);
  const mergedArrow = useMergedArrow(popconfirmArrow, contextArrow);
  const mergedTrigger = trigger || contextTrigger || "click";
  const mergedMouseEnterDelay = mouseEnterDelay ?? contextMouseEnterDelay ?? 0.1;
  const mergedMouseLeaveDelay = mouseLeaveDelay ?? contextMouseLeaveDelay ?? 0.1;
  const settingOpen = (value) => {
    setOpen(value);
    onOpenChange?.(value);
  };
  const close = () => {
    settingOpen(false);
  };
  const onConfirm = (e) => props.onConfirm?.call(void 0, e);
  const onCancel = (e) => {
    settingOpen(false);
    props.onCancel?.call(void 0, e);
  };
  const onInternalOpenChange = (value) => {
    if (disabled) {
      return;
    }
    settingOpen(value);
  };
  const prefixCls = getPrefixCls("popconfirm", customizePrefixCls);
  const mergedProps = {
    ...props,
    placement,
    trigger: mergedTrigger,
    okType,
    overlayStyle,
    styles,
    classNames,
    mouseEnterDelay: mergedMouseEnterDelay,
    mouseLeaveDelay: mergedMouseLeaveDelay
  };
  const contextStyleRoot = useSemanticRootStyle(contextStyle);
  const overlayStyleRoot = useSemanticRootStyle(overlayStyle);
  const [mergedClassNames, mergedStyles] = useMergeSemantic([contextClassNames, classNames], [contextStyles, contextStyleRoot, styles, overlayStyleRoot], {
    props: mergedProps
  });
  const rootClassNames = clsx(prefixCls, contextClassName, overlayClassName, mergedClassNames.root);
  useStyle(prefixCls);
  return /* @__PURE__ */ reactExports.createElement(Popover, {
    arrow: mergedArrow,
    ...omit(restProps, ["title"]),
    trigger: mergedTrigger,
    placement,
    onOpenChange: onInternalOpenChange,
    open,
    ref,
    mouseEnterDelay: mergedMouseEnterDelay,
    mouseLeaveDelay: mergedMouseLeaveDelay,
    classNames: {
      root: rootClassNames,
      container: mergedClassNames.container,
      arrow: mergedClassNames.arrow
    },
    styles: {
      root: mergedStyles.root,
      container: mergedStyles.container,
      arrow: mergedStyles.arrow
    },
    content: /* @__PURE__ */ reactExports.createElement(Overlay, {
      okType,
      icon,
      ...props,
      prefixCls,
      close,
      onConfirm,
      onCancel,
      classNames: mergedClassNames,
      styles: mergedStyles
    }),
    "data-popover-inject": true
  }, children);
});
const Popconfirm = InternalPopconfirm;
Popconfirm._InternalPanelDoNotUseOrYouWillBeFired = PurePanel;
export {
  ActionButton as A,
  Popconfirm as P
};
