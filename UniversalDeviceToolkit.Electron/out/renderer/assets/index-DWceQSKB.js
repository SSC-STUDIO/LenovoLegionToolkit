import { v as genStyleHooks, n as unit, A as resetComponent, ac as genFocusStyle, r as reactExports, J as useComponentConfig, Z as isPlainObject, ag as isNonNullable, L as useSemanticRootStyle, N as useMergeSemantic, f as clsx, a2 as pickAttrs, a3 as CSSMotion, af as composeRef, ao as RefIcon, ap as RefIcon$1, ar as RefIcon$2, aq as RefIcon$3, ab as RefIcon$4, bv as _getPrototypeOf, bw as _possibleConstructorReturn, bx as _isNativeReflectConstruct, by as _inherits, aR as _createClass, aS as _classCallCheck } from "./index-3RTipSd5.js";
const genAlertTypeStyle = (bgColor, iconColor, alertCls) => ({
  background: bgColor,
  [`${alertCls}-icon`]: {
    color: iconColor
  }
});
const genBaseStyle = (token) => {
  const {
    componentCls,
    motionDurationSlow: duration,
    marginXS,
    marginSM,
    fontSize,
    fontSizeLG,
    lineHeight,
    motionEaseInOutCirc,
    borderRadius,
    withDescriptionIconSize,
    colorText,
    colorTextHeading,
    withDescriptionPadding,
    defaultPadding,
    lineWidth,
    lineType,
    colorSuccessBorder,
    colorWarningBorder,
    colorErrorBorder,
    colorInfoBorder
  } = token;
  return {
    [componentCls]: {
      ...resetComponent(token),
      position: "relative",
      display: "flex",
      alignItems: "center",
      padding: defaultPadding,
      wordWrap: "break-word",
      borderRadius,
      borderWidth: unit(lineWidth),
      borderStyle: lineType,
      [`&${componentCls}-success`]: {
        borderColor: colorSuccessBorder
      },
      [`&${componentCls}-info`]: {
        borderColor: colorInfoBorder
      },
      [`&${componentCls}-warning`]: {
        borderColor: colorWarningBorder
      },
      [`&${componentCls}-error`]: {
        borderColor: colorErrorBorder
      },
      [`&${componentCls}-filled`]: {
        borderColor: "transparent"
      },
      [`&${componentCls}-rtl`]: {
        direction: "rtl"
      },
      [`${componentCls}-section`]: {
        flex: 1,
        minWidth: 0
      },
      [`${componentCls}-icon`]: {
        marginInlineEnd: marginXS,
        lineHeight: 0
      },
      "&-description": {
        display: "none",
        fontSize,
        lineHeight
      },
      "&-title": {
        color: colorTextHeading
      },
      [`&${componentCls}-motion-leave`]: {
        overflow: "hidden",
        opacity: 1,
        transition: [`max-height`, `opacity`, `padding-top`, `padding-bottom`, `margin-bottom`].map((prop) => `${prop} ${duration} ${motionEaseInOutCirc}`).join(", ")
      },
      [`&${componentCls}-motion-leave-active`]: {
        maxHeight: 0,
        marginBottom: "0 !important",
        paddingTop: 0,
        paddingBottom: 0,
        opacity: 0
      },
      [`&${componentCls}-with-description`]: {
        alignItems: "flex-start",
        padding: withDescriptionPadding,
        [`${componentCls}-icon`]: {
          marginInlineEnd: marginSM,
          fontSize: withDescriptionIconSize,
          lineHeight: 0
        },
        [`${componentCls}-title`]: {
          display: "block",
          marginBottom: marginXS,
          color: colorTextHeading,
          fontSize: fontSizeLG
        },
        [`${componentCls}-description`]: {
          display: "block",
          color: colorText
        }
      },
      [`&${componentCls}-banner`]: {
        marginBottom: 0,
        border: "0 !important",
        borderRadius: 0
      }
    }
  };
};
const genTypeStyle = (token) => {
  const {
    componentCls,
    colorSuccess,
    colorSuccessBg,
    colorWarning,
    colorWarningBg,
    colorError,
    colorErrorBg,
    colorInfo,
    colorInfoBg
  } = token;
  return {
    [componentCls]: {
      "&-success": genAlertTypeStyle(colorSuccessBg, colorSuccess, componentCls),
      "&-info": genAlertTypeStyle(colorInfoBg, colorInfo, componentCls),
      "&-warning": genAlertTypeStyle(colorWarningBg, colorWarning, componentCls),
      "&-error": {
        ...genAlertTypeStyle(colorErrorBg, colorError, componentCls),
        [`${componentCls}-description > pre`]: {
          margin: 0,
          padding: 0
        }
      }
    }
  };
};
const genActionStyle = (token) => {
  const {
    componentCls,
    iconCls,
    motionDurationMid,
    marginXS,
    fontSizeIcon,
    colorIcon,
    colorIconHover
  } = token;
  return {
    [componentCls]: {
      [`${componentCls}-actions`]: {
        marginInlineStart: marginXS
      },
      [`${componentCls}-close-icon`]: {
        marginInlineStart: marginXS,
        padding: 0,
        overflow: "hidden",
        fontSize: fontSizeIcon,
        lineHeight: unit(fontSizeIcon),
        backgroundColor: "transparent",
        border: "none",
        cursor: "pointer",
        ...genFocusStyle(token),
        [`${iconCls}-close`]: {
          color: colorIcon,
          transition: `color ${motionDurationMid}`,
          "&:hover": {
            color: colorIconHover
          }
        }
      },
      "&-close-text": {
        color: colorIcon,
        transition: `color ${motionDurationMid}`,
        "&:hover": {
          color: colorIconHover
        }
      }
    }
  };
};
const prepareComponentToken = (token) => {
  const paddingHorizontal = 12;
  return {
    borderRadius: token.borderRadiusLG,
    withDescriptionIconSize: token.fontSizeHeading3,
    defaultPadding: `${token.paddingContentVerticalSM}px ${paddingHorizontal}px`,
    withDescriptionPadding: `${token.paddingMD}px ${token.paddingContentHorizontalLG}px`
  };
};
const useStyle = genStyleHooks("Alert", (token) => [genBaseStyle(token), genTypeStyle(token), genActionStyle(token)], prepareComponentToken);
const IconNode = (props) => {
  const {
    icon,
    type,
    className,
    style,
    successIcon,
    infoIcon,
    warningIcon,
    errorIcon
  } = props;
  const iconMapFilled = {
    success: successIcon ?? /* @__PURE__ */ reactExports.createElement(RefIcon$3, null),
    info: infoIcon ?? /* @__PURE__ */ reactExports.createElement(RefIcon$2, null),
    error: errorIcon ?? /* @__PURE__ */ reactExports.createElement(RefIcon$1, null),
    warning: warningIcon ?? /* @__PURE__ */ reactExports.createElement(RefIcon, null)
  };
  return /* @__PURE__ */ reactExports.createElement("span", {
    className,
    style
  }, icon ?? iconMapFilled[type]);
};
const CloseIconNode = (props) => {
  const {
    isClosable,
    prefixCls,
    closeIcon,
    handleClose,
    ariaProps,
    className,
    style
  } = props;
  const mergedCloseIcon = closeIcon === true || closeIcon === void 0 ? /* @__PURE__ */ reactExports.createElement(RefIcon$4, null) : closeIcon;
  return isClosable ? /* @__PURE__ */ reactExports.createElement("button", {
    type: "button",
    onClick: handleClose,
    className: clsx(`${prefixCls}-close-icon`, className),
    tabIndex: 0,
    style,
    ...ariaProps
  }, mergedCloseIcon) : null;
};
const Alert$1 = /* @__PURE__ */ reactExports.forwardRef((props, ref) => {
  const {
    description,
    prefixCls: customizePrefixCls,
    message,
    title,
    banner,
    className,
    rootClassName,
    style,
    onMouseEnter,
    onMouseLeave,
    onClick,
    afterClose,
    showIcon,
    closable,
    closeText,
    closeIcon,
    action,
    id,
    styles,
    classNames,
    ...otherProps
  } = props;
  const mergedTitle = title ?? message;
  const [closed, setClosed] = reactExports.useState(false);
  const internalRef = reactExports.useRef(null);
  reactExports.useImperativeHandle(ref, () => ({
    nativeElement: internalRef.current
  }));
  const {
    getPrefixCls,
    direction,
    variant: contextVariant,
    closable: contextClosable,
    closeIcon: contextCloseIcon,
    className: contextClassName,
    style: contextStyle,
    classNames: contextClassNames,
    styles: contextStyles,
    successIcon,
    infoIcon,
    warningIcon,
    errorIcon
  } = useComponentConfig("alert");
  const prefixCls = getPrefixCls("alert", customizePrefixCls);
  const [hashId, cssVarCls] = useStyle(prefixCls);
  const {
    onClose: closableOnClose,
    afterClose: closableAfterClose
  } = isPlainObject(closable) ? closable : {};
  const handleClose = (e) => {
    setClosed(true);
    (closableOnClose ?? props.onClose)?.(e);
  };
  const type = reactExports.useMemo(() => {
    if (props.type !== void 0) {
      return props.type;
    }
    return banner ? "warning" : "info";
  }, [props.type, banner]);
  const mergedVariant = props.variant ?? contextVariant ?? "outlined";
  const isClosable = reactExports.useMemo(() => {
    if (isPlainObject(closable)) {
      return true;
    }
    if (closeText) {
      return true;
    }
    if (typeof closable === "boolean") {
      return closable;
    }
    if (closeIcon !== false && isNonNullable(closeIcon)) {
      return true;
    }
    return !!contextClosable;
  }, [closeText, closeIcon, closable, contextClosable]);
  const isShowIcon = banner && showIcon === void 0 ? true : showIcon;
  const mergedProps = {
    ...props,
    prefixCls,
    variant: mergedVariant,
    type,
    showIcon: isShowIcon,
    closable: isClosable
  };
  const contextStyleRoot = useSemanticRootStyle(contextStyle);
  const styleRoot = useSemanticRootStyle(style);
  const [mergedClassNames, mergedStyles] = useMergeSemantic([contextClassNames, classNames], [contextStyles, contextStyleRoot, styles, styleRoot], {
    props: mergedProps
  });
  const alertCls = clsx(prefixCls, `${prefixCls}-${type}`, `${prefixCls}-${mergedVariant}`, {
    [`${prefixCls}-with-description`]: !!description,
    [`${prefixCls}-no-icon`]: !isShowIcon,
    [`${prefixCls}-banner`]: !!banner,
    [`${prefixCls}-rtl`]: direction === "rtl"
  }, contextClassName, className, rootClassName, mergedClassNames.root, cssVarCls, hashId);
  const restProps = pickAttrs(otherProps, {
    aria: true,
    data: true
  });
  const mergedCloseIcon = reactExports.useMemo(() => {
    if (isPlainObject(closable) && closable.closeIcon) {
      return closable.closeIcon;
    }
    if (closeText) {
      return closeText;
    }
    if (closeIcon !== void 0) {
      return closeIcon;
    }
    if (isPlainObject(contextClosable) && contextClosable.closeIcon) {
      return contextClosable.closeIcon;
    }
    return contextCloseIcon;
  }, [closeIcon, closable, contextClosable, closeText, contextCloseIcon]);
  const mergedAriaProps = reactExports.useMemo(() => {
    const merged = closable ?? contextClosable;
    if (isPlainObject(merged)) {
      return pickAttrs(merged, {
        data: true,
        aria: true
      });
    }
    return {};
  }, [closable, contextClosable]);
  return /* @__PURE__ */ reactExports.createElement(CSSMotion, {
    visible: !closed,
    motionName: `${prefixCls}-motion`,
    motionAppear: false,
    motionEnter: false,
    onLeaveStart: (node) => ({
      maxHeight: node.offsetHeight
    }),
    onLeaveEnd: closableAfterClose ?? afterClose
  }, ({
    className: motionClassName,
    style: motionStyle
  }, setRef) => /* @__PURE__ */ reactExports.createElement("div", {
    id,
    ref: composeRef(internalRef, setRef),
    "data-show": !closed,
    className: clsx(alertCls, motionClassName),
    style: {
      ...mergedStyles.root,
      ...motionStyle
    },
    onMouseEnter,
    onMouseLeave,
    onClick,
    role: "alert",
    ...restProps
  }, isShowIcon ? /* @__PURE__ */ reactExports.createElement(IconNode, {
    className: clsx(`${prefixCls}-icon`, mergedClassNames.icon),
    style: mergedStyles.icon,
    description,
    icon: props.icon,
    prefixCls,
    type,
    successIcon,
    infoIcon,
    warningIcon,
    errorIcon
  }) : null, /* @__PURE__ */ reactExports.createElement("div", {
    className: clsx(`${prefixCls}-section`, mergedClassNames.section),
    style: mergedStyles.section
  }, mergedTitle ? /* @__PURE__ */ reactExports.createElement("div", {
    className: clsx(`${prefixCls}-title`, mergedClassNames.title),
    style: mergedStyles.title
  }, mergedTitle) : null, description ? /* @__PURE__ */ reactExports.createElement("div", {
    className: clsx(`${prefixCls}-description`, mergedClassNames.description),
    style: mergedStyles.description
  }, description) : null), action ? /* @__PURE__ */ reactExports.createElement("div", {
    className: clsx(`${prefixCls}-actions`, mergedClassNames.actions),
    style: mergedStyles.actions
  }, action) : null, /* @__PURE__ */ reactExports.createElement(CloseIconNode, {
    className: mergedClassNames.close,
    style: mergedStyles.close,
    isClosable,
    prefixCls,
    closeIcon: mergedCloseIcon,
    handleClose,
    ariaProps: mergedAriaProps
  })));
});
function _callSuper(t, o, e) {
  return o = _getPrototypeOf(o), _possibleConstructorReturn(t, _isNativeReflectConstruct() ? Reflect.construct(o, e || [], _getPrototypeOf(t).constructor) : o.apply(t, e));
}
let ErrorBoundary = /* @__PURE__ */ (function(_React$PureComponent) {
  function ErrorBoundary2() {
    var _this;
    _classCallCheck(this, ErrorBoundary2);
    _this = _callSuper(this, ErrorBoundary2, arguments);
    _this.state = {
      error: void 0,
      info: {}
    };
    return _this;
  }
  _inherits(ErrorBoundary2, _React$PureComponent);
  return _createClass(ErrorBoundary2, [{
    key: "componentDidCatch",
    value: function componentDidCatch(error, info) {
      this.setState({
        error,
        info
      });
    }
  }, {
    key: "render",
    value: function render() {
      const {
        message,
        title,
        description,
        id,
        children
      } = this.props;
      const {
        error,
        info
      } = this.state;
      const mergedTitle = title ?? message;
      const componentStack = info?.componentStack || null;
      const errorMessage = isNonNullable(mergedTitle) ? mergedTitle : error?.toString();
      const errorDescription = isNonNullable(description) ? description : componentStack;
      if (error) {
        return /* @__PURE__ */ reactExports.createElement(Alert$1, {
          id,
          type: "error",
          title: errorMessage,
          description: /* @__PURE__ */ reactExports.createElement("pre", {
            style: {
              fontSize: "0.9em",
              overflowX: "auto"
            }
          }, errorDescription)
        });
      }
      return children;
    }
  }]);
})(reactExports.PureComponent);
const Alert = Alert$1;
Alert.ErrorBoundary = ErrorBoundary;
export {
  Alert as A
};
