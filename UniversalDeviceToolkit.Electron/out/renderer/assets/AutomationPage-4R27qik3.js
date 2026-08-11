import { r as reactExports, Z as isPlainObject, _ as _toConsumableArray, $ as React, a0 as useComposeRef, a1 as useLockFocus, f as clsx, a2 as pickAttrs, a3 as CSSMotion, a4 as useId, a5 as contains, a6 as Portal, a7 as canUseDom, K as useEvent, a8 as useLocale, a9 as getConfirmLocale, l as isFunction, aa as DisabledContextProvider, ab as RefIcon, v as genStyleHooks, y as initZoomMotion, w as merge, n as unit, ac as genFocusStyle, p as genFocusOutline, A as resetComponent, ad as initFadeMotion, ae as getMediaSize, J as useComponentConfig, e as ConfigContext, af as composeRef, H as useZIndex, N as useMergeSemantic, h as omit, ag as isNonNullable, ah as isNumber, ai as ContextIsolator, aj as ZIndexContext, ak as getTransitionName, al as Skeleton, Q as useCSSVarCls, am as genSubStyleComponent, z as clearFix, an as fallbackProp, ao as RefIcon$1, ap as RefIcon$2, aq as RefIcon$3, ar as RefIcon$4, as as isReactRenderable, at as ConfigProvider, au as useToken, av as CONTAINER_MAX_OFFSET, aw as render, ax as globalConfig, ay as unmount, az as localeValues, L as useSemanticRootStyle, i as invoke, c as create, u as useTranslation, j as jsxRuntimeExports, F as Flex, T as Typography, C as Card, s as staticMethods } from "./index-3RTipSd5.js";
import { S as Space } from "./index-Dro2pb1j.js";
import { S as Switch } from "./index-BbS3n2P6.js";
import { w as withPureRenderTheme, E as Empty, C as Collapse, S as Select } from "./index-BxBscas6.js";
import { L as List } from "./index-DaSpOuam.js";
import { B as Button, c as convertLegacyProps, u as useClosable, p as pickClosable, T as Tag } from "./index-uyL__3sF.js";
import { A as ActionButton, P as Popconfirm } from "./index-DdhF4o9H.js";
import { I as Input } from "./index-DBj245TA.js";
import "./Addon-CECo-qGW.js";
import "./index-Hdt_DTHG.js";
import "./Input-mSSMIOSE.js";
const normalizeMaskConfig = (mask, maskClosable) => {
  let maskConfig = {};
  if (isPlainObject(mask)) {
    maskConfig = mask;
  }
  if (typeof mask === "boolean") {
    maskConfig = {
      enabled: mask
    };
  }
  if (maskConfig.closable === void 0 && maskClosable !== void 0) {
    maskConfig.closable = maskClosable;
  }
  return maskConfig;
};
const useMergedMask = (mask, contextMask, prefixCls, maskClosable) => {
  return reactExports.useMemo(() => {
    const maskConfig = normalizeMaskConfig(mask, maskClosable);
    const contextMaskConfig = normalizeMaskConfig(contextMask);
    const mergedConfig = {
      blur: false,
      ...contextMaskConfig,
      ...maskConfig,
      closable: maskConfig.closable ?? maskClosable ?? contextMaskConfig.closable ?? true
    };
    const className = mergedConfig.blur ? `${prefixCls}-mask-blur` : void 0;
    return [mergedConfig.enabled !== false, {
      mask: className
    }, !!mergedConfig.closable];
  }, [mask, contextMask, prefixCls, maskClosable]);
};
const usePatchElement = () => {
  const [elements, setElements] = reactExports.useState([]);
  const patchElement = reactExports.useCallback((element) => {
    setElements((originElements) => [].concat(_toConsumableArray(originElements), [element]));
    return () => {
      setElements((originElements) => originElements.filter((ele) => ele !== element));
    };
  }, []);
  return [elements, patchElement];
};
const ModalContext = /* @__PURE__ */ React.createContext({});
const {
  Provider: ModalContextProvider
} = ModalContext;
const ConfirmCancelBtn = () => {
  const {
    autoFocusButton,
    cancelButtonProps,
    cancelTextLocale,
    isSilent,
    mergedOkCancel,
    rootPrefixCls,
    close,
    onCancel,
    onConfirm,
    onClose
  } = reactExports.useContext(ModalContext);
  return mergedOkCancel ? /* @__PURE__ */ React.createElement(ActionButton, {
    isSilent,
    actionFn: onCancel,
    close: (...args) => {
      close?.(...args);
      onConfirm?.(false);
      onClose?.();
    },
    autoFocus: autoFocusButton === "cancel",
    buttonProps: cancelButtonProps,
    prefixCls: `${rootPrefixCls}-btn`
  }, cancelTextLocale) : null;
};
const ConfirmOkBtn = () => {
  const {
    autoFocusButton,
    close,
    isSilent,
    okButtonProps,
    rootPrefixCls,
    okTextLocale,
    okType,
    onConfirm,
    onOk,
    onClose
  } = reactExports.useContext(ModalContext);
  return /* @__PURE__ */ React.createElement(ActionButton, {
    isSilent,
    type: okType || "primary",
    actionFn: onOk,
    close: (...args) => {
      close?.(...args);
      onConfirm?.(true);
      onClose?.();
    },
    autoFocus: autoFocusButton === "ok",
    buttonProps: okButtonProps,
    prefixCls: `${rootPrefixCls}-btn`
  }, okTextLocale);
};
const RefContext = /* @__PURE__ */ reactExports.createContext({});
function getMotionName(prefixCls, transitionName, animationName) {
  let motionName = transitionName;
  if (!motionName && animationName) {
    motionName = `${prefixCls}-${animationName}`;
  }
  return motionName;
}
function getScroll(w, top) {
  let ret = w[`page${top ? "Y" : "X"}Offset`];
  const method = `scroll${top ? "Top" : "Left"}`;
  if (typeof ret !== "number") {
    const d = w.document;
    ret = d.documentElement[method];
    if (typeof ret !== "number") {
      ret = d.body[method];
    }
  }
  return ret;
}
function offset(el) {
  const rect = el.getBoundingClientRect();
  const pos = {
    left: rect.left,
    top: rect.top
  };
  const doc = el.ownerDocument;
  const w = doc.defaultView || doc.parentWindow;
  pos.left += getScroll(w);
  pos.top += getScroll(w, true);
  return pos;
}
const MemoChildren = /* @__PURE__ */ reactExports.memo(({
  children
}) => children, (_, {
  shouldUpdate
}) => !shouldUpdate);
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
const Panel = /* @__PURE__ */ React.forwardRef((props, ref) => {
  const {
    prefixCls,
    className,
    style,
    title,
    ariaId,
    footer,
    closable,
    closeIcon,
    onClose,
    children,
    bodyStyle,
    bodyProps,
    modalRender,
    onMouseDown,
    onMouseUp,
    holderRef,
    visible,
    forceRender,
    width,
    height,
    classNames: modalClassNames,
    styles: modalStyles,
    isFixedPos,
    focusTrap
  } = props;
  const {
    panel: panelRef
  } = React.useContext(RefContext);
  const internalRef = reactExports.useRef(null);
  const mergedRef = useComposeRef(holderRef, panelRef, internalRef);
  const [ignoreElement] = useLockFocus(visible && isFixedPos && focusTrap !== false, () => internalRef.current);
  React.useImperativeHandle(ref, () => ({
    focus: () => {
      internalRef.current?.focus({
        preventScroll: true
      });
    }
  }));
  const contentStyle = {};
  if (width !== void 0) {
    contentStyle.width = width;
  }
  if (height !== void 0) {
    contentStyle.height = height;
  }
  const footerNode = footer ? /* @__PURE__ */ React.createElement("div", {
    className: clsx(`${prefixCls}-footer`, modalClassNames?.footer),
    style: {
      ...modalStyles?.footer
    }
  }, footer) : null;
  const headerNode = title ? /* @__PURE__ */ React.createElement("div", {
    className: clsx(`${prefixCls}-header`, modalClassNames?.header),
    style: {
      ...modalStyles?.header
    }
  }, /* @__PURE__ */ React.createElement("div", {
    className: clsx(`${prefixCls}-title`, modalClassNames?.title),
    id: ariaId,
    style: {
      ...modalStyles?.title
    }
  }, title)) : null;
  const closableObj = reactExports.useMemo(() => {
    if (typeof closable === "object" && closable !== null) {
      return closable;
    }
    if (closable) {
      return {
        closeIcon: closeIcon ?? /* @__PURE__ */ React.createElement("span", {
          className: `${prefixCls}-close-x`
        })
      };
    }
    return {};
  }, [closable, closeIcon, prefixCls]);
  const ariaProps = pickAttrs(closableObj, true);
  const closeBtnIsDisabled = typeof closable === "object" && closable.disabled;
  const closerNode = closable ? /* @__PURE__ */ React.createElement("button", _extends$4({
    type: "button",
    onClick: onClose,
    "aria-label": "Close"
  }, ariaProps, {
    className: clsx(`${prefixCls}-close`, modalClassNames?.close),
    disabled: closeBtnIsDisabled,
    style: modalStyles?.close
  }), closableObj.closeIcon) : null;
  const content = /* @__PURE__ */ React.createElement("div", {
    className: clsx(`${prefixCls}-container`, modalClassNames?.container),
    style: modalStyles?.container
  }, closerNode, headerNode, /* @__PURE__ */ React.createElement("div", _extends$4({
    className: clsx(`${prefixCls}-body`, modalClassNames?.body),
    style: {
      ...bodyStyle,
      ...modalStyles?.body
    }
  }, bodyProps), children), footerNode);
  return /* @__PURE__ */ React.createElement("div", {
    key: "dialog-element",
    role: "dialog",
    "aria-labelledby": title ? ariaId : null,
    "aria-modal": "true",
    ref: mergedRef,
    style: {
      ...style,
      ...contentStyle
    },
    className: clsx(prefixCls, className),
    onMouseDown,
    onMouseUp,
    tabIndex: -1,
    onFocus: (e) => {
      ignoreElement(e.target);
    }
  }, /* @__PURE__ */ React.createElement(MemoChildren, {
    shouldUpdate: visible || forceRender
  }, modalRender ? modalRender(content) : content));
});
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
const Content = /* @__PURE__ */ reactExports.forwardRef((props, ref) => {
  const {
    prefixCls,
    title,
    style,
    className,
    visible,
    forceRender,
    destroyOnHidden,
    motionName,
    ariaId,
    onVisibleChanged,
    mousePosition: mousePosition2
  } = props;
  const dialogRef = reactExports.useRef(null);
  const panelRef = reactExports.useRef(null);
  reactExports.useImperativeHandle(ref, () => ({
    ...panelRef.current,
    inMotion: dialogRef.current.inMotion,
    enableMotion: dialogRef.current.enableMotion
  }));
  const [transformOrigin, setTransformOrigin] = reactExports.useState();
  const contentStyle = {};
  if (transformOrigin) {
    contentStyle.transformOrigin = transformOrigin;
  }
  function onPrepare() {
    if (!dialogRef.current?.nativeElement) {
      return;
    }
    const elementOffset = offset(dialogRef.current.nativeElement);
    setTransformOrigin(mousePosition2 && (mousePosition2.x || mousePosition2.y) ? `${mousePosition2.x - elementOffset.left}px ${mousePosition2.y - elementOffset.top}px` : "");
  }
  return /* @__PURE__ */ reactExports.createElement(CSSMotion, {
    visible,
    onVisibleChanged,
    onAppearPrepare: onPrepare,
    onEnterPrepare: onPrepare,
    forceRender,
    motionName,
    removeOnLeave: destroyOnHidden,
    ref: dialogRef
  }, ({
    className: motionClassName,
    style: motionStyle
  }, motionRef) => /* @__PURE__ */ reactExports.createElement(Panel, _extends$3({}, props, {
    ref: panelRef,
    title,
    ariaId,
    prefixCls,
    holderRef: motionRef,
    style: {
      ...motionStyle,
      ...style,
      ...contentStyle
    },
    className: clsx(className, motionClassName)
  })));
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
const Mask = (props) => {
  const {
    prefixCls,
    style,
    visible,
    maskProps,
    motionName,
    className
  } = props;
  return /* @__PURE__ */ reactExports.createElement(CSSMotion, {
    key: "mask",
    visible,
    motionName,
    leavedClassName: `${prefixCls}-mask-hidden`
  }, ({
    className: motionClassName,
    style: motionStyle
  }, ref) => /* @__PURE__ */ reactExports.createElement("div", _extends$2({
    ref,
    style: {
      ...motionStyle,
      ...style
    },
    className: clsx(`${prefixCls}-mask`, motionClassName, className)
  }, maskProps)));
};
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
const Dialog = (props) => {
  const {
    prefixCls = "rc-dialog",
    zIndex,
    visible = false,
    focusTriggerAfterClose = true,
    wrapStyle,
    wrapClassName,
    wrapProps,
    onClose,
    afterOpenChange,
    afterClose,
    // Dialog
    transitionName,
    animation,
    closable = true,
    // Mask
    mask = true,
    maskTransitionName,
    maskAnimation,
    maskClosable = true,
    maskStyle,
    maskProps,
    rootClassName,
    rootStyle,
    classNames: modalClassNames,
    styles: modalStyles
  } = props;
  const lastOutSideActiveElementRef = reactExports.useRef(null);
  const wrapperRef = reactExports.useRef(null);
  const contentRef = reactExports.useRef(null);
  const [animatedVisible, setAnimatedVisible] = reactExports.useState(visible);
  const [isFixedPos, setIsFixedPos] = reactExports.useState(false);
  const ariaId = useId();
  function saveLastOutSideActiveElementRef() {
    if (!contains(wrapperRef.current, document.activeElement)) {
      lastOutSideActiveElementRef.current = document.activeElement;
    }
  }
  function focusDialogContent() {
    if (!contains(wrapperRef.current, document.activeElement)) {
      contentRef.current?.focus();
    }
  }
  function doClose() {
    setAnimatedVisible(false);
    if (mask && lastOutSideActiveElementRef.current && focusTriggerAfterClose) {
      try {
        lastOutSideActiveElementRef.current.focus({
          preventScroll: true
        });
      } catch (e) {
      }
      lastOutSideActiveElementRef.current = null;
    }
    if (animatedVisible) {
      afterClose?.();
    }
  }
  function onDialogVisibleChanged(newVisible) {
    if (newVisible) {
      focusDialogContent();
    } else {
      doClose();
    }
    afterOpenChange?.(newVisible);
  }
  function onInternalClose(e) {
    onClose?.(e);
  }
  const mouseDownOnMaskRef = reactExports.useRef(false);
  let onWrapperClick = null;
  if (maskClosable) {
    onWrapperClick = (e) => {
      if (wrapperRef.current === e.target && mouseDownOnMaskRef.current) {
        onInternalClose(e);
      }
    };
  }
  function onWrapperMouseDown(e) {
    mouseDownOnMaskRef.current = e.target === wrapperRef.current;
  }
  reactExports.useEffect(() => {
    if (visible) {
      mouseDownOnMaskRef.current = false;
      setAnimatedVisible(true);
      saveLastOutSideActiveElementRef();
      if (wrapperRef.current) {
        const computedWrapStyle = getComputedStyle(wrapperRef.current);
        setIsFixedPos(computedWrapStyle.position === "fixed");
      }
    } else if (animatedVisible && contentRef.current.enableMotion() && !contentRef.current.inMotion()) {
      doClose();
    }
  }, [visible]);
  const mergedStyle = {
    zIndex,
    ...wrapStyle,
    ...modalStyles?.wrapper,
    display: !animatedVisible ? "none" : null
  };
  return /* @__PURE__ */ reactExports.createElement("div", _extends$1({
    className: clsx(`${prefixCls}-root`, rootClassName),
    style: rootStyle
  }, pickAttrs(props, {
    data: true
  })), /* @__PURE__ */ reactExports.createElement(Mask, {
    prefixCls,
    visible: mask && visible,
    motionName: getMotionName(prefixCls, maskTransitionName, maskAnimation),
    style: {
      zIndex,
      ...maskStyle,
      ...modalStyles?.mask
    },
    maskProps,
    className: modalClassNames?.mask
  }), /* @__PURE__ */ reactExports.createElement("div", _extends$1({
    className: clsx(`${prefixCls}-wrap`, wrapClassName, modalClassNames?.wrapper),
    ref: wrapperRef,
    onClick: onWrapperClick,
    onMouseDown: onWrapperMouseDown,
    style: mergedStyle
  }, wrapProps), /* @__PURE__ */ reactExports.createElement(Content, _extends$1({}, props, {
    isFixedPos,
    ref: contentRef,
    closable,
    ariaId,
    prefixCls,
    visible: visible && animatedVisible,
    onClose: onInternalClose,
    onVisibleChanged: onDialogVisibleChanged,
    motionName: getMotionName(prefixCls, transitionName, animation)
  }))));
};
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
const DialogWrap = (props) => {
  const {
    visible,
    getContainer,
    forceRender,
    destroyOnHidden = false,
    afterClose,
    closable,
    panelRef,
    keyboard = true,
    scrollLock = true,
    onClose
  } = props;
  const {
    scrollLock: _,
    ...restProps
  } = props;
  const [animatedVisible, setAnimatedVisible] = reactExports.useState(visible);
  const refContext = reactExports.useMemo(() => ({
    panel: panelRef
  }), [panelRef]);
  const onEsc = ({
    top,
    event
  }) => {
    if (top && keyboard) {
      event.stopPropagation();
      onClose?.(event);
      return;
    }
  };
  reactExports.useEffect(() => {
    if (visible) {
      setAnimatedVisible(true);
    }
  }, [visible]);
  if (!forceRender && destroyOnHidden && !animatedVisible) {
    return null;
  }
  return /* @__PURE__ */ reactExports.createElement(RefContext.Provider, {
    value: refContext
  }, /* @__PURE__ */ reactExports.createElement(Portal, {
    open: visible || forceRender || animatedVisible,
    onEsc,
    autoDestroy: false,
    getContainer,
    autoLock: scrollLock && (visible || animatedVisible)
  }, /* @__PURE__ */ reactExports.createElement(Dialog, _extends({}, restProps, {
    destroyOnHidden,
    afterClose: () => {
      const closableObj = closable && typeof closable === "object" ? closable : {};
      const {
        afterClose: closableAfterClose
      } = closableObj || {};
      closableAfterClose?.();
      afterClose?.();
      setAnimatedVisible(false);
    }
  }))));
};
const canUseDocElement = () => canUseDom() && window.document.documentElement;
function useFocusable(focusable, defaultTrap, legacyFocusTriggerAfterClose) {
  return reactExports.useMemo(() => {
    const ret = {
      trap: defaultTrap ?? true,
      focusTriggerAfterClose: legacyFocusTriggerAfterClose ?? true
    };
    return {
      ...ret,
      ...focusable
    };
  }, [focusable, defaultTrap, legacyFocusTriggerAfterClose]);
}
function voidFunc() {
}
const WatermarkContext = /* @__PURE__ */ reactExports.createContext({
  add: voidFunc,
  remove: voidFunc
});
function usePanelRef(panelSelector) {
  const watermark = reactExports.useContext(WatermarkContext);
  const panelEleRef = reactExports.useRef(null);
  const panelRef = useEvent((ele) => {
    if (ele) {
      const innerContentEle = panelSelector ? ele.querySelector(panelSelector) : ele;
      if (innerContentEle) {
        watermark.add(innerContentEle);
        panelEleRef.current = innerContentEle;
      }
    } else {
      watermark.remove(panelEleRef.current);
    }
  });
  return panelRef;
}
const NormalCancelBtn = () => {
  const {
    cancelButtonProps,
    cancelTextLocale,
    onCancel
  } = reactExports.useContext(ModalContext);
  return /* @__PURE__ */ React.createElement(Button, {
    onClick: onCancel,
    ...cancelButtonProps
  }, cancelTextLocale);
};
const NormalOkBtn = () => {
  const {
    confirmLoading,
    okButtonProps,
    okType,
    okTextLocale,
    onOk
  } = reactExports.useContext(ModalContext);
  return /* @__PURE__ */ React.createElement(Button, {
    ...convertLegacyProps(okType),
    loading: confirmLoading,
    onClick: onOk,
    ...okButtonProps
  }, okTextLocale);
};
function renderCloseIcon(prefixCls, closeIcon) {
  return /* @__PURE__ */ React.createElement("span", {
    className: `${prefixCls}-close-x`
  }, closeIcon || /* @__PURE__ */ React.createElement(RefIcon, {
    className: `${prefixCls}-close-icon`
  }));
}
const Footer = (props) => {
  const {
    okText,
    okType = "primary",
    cancelText,
    confirmLoading,
    onOk,
    onCancel,
    okButtonProps,
    cancelButtonProps,
    footer
  } = props;
  const [locale] = useLocale("Modal", getConfirmLocale());
  const okTextLocale = okText || locale?.okText;
  const cancelTextLocale = cancelText || locale?.cancelText;
  const memoizedValue = React.useMemo(() => {
    return {
      confirmLoading,
      okButtonProps,
      cancelButtonProps,
      okTextLocale,
      cancelTextLocale,
      okType,
      onOk,
      onCancel
    };
  }, [confirmLoading, okButtonProps, cancelButtonProps, okTextLocale, cancelTextLocale, okType, onOk, onCancel]);
  let footerNode;
  if (isFunction(footer) || typeof footer === "undefined") {
    footerNode = /* @__PURE__ */ React.createElement(React.Fragment, null, /* @__PURE__ */ React.createElement(NormalCancelBtn, null), /* @__PURE__ */ React.createElement(NormalOkBtn, null));
    if (isFunction(footer)) {
      footerNode = footer(footerNode, {
        OkBtn: NormalOkBtn,
        CancelBtn: NormalCancelBtn
      });
    }
    footerNode = /* @__PURE__ */ React.createElement(ModalContextProvider, {
      value: memoizedValue
    }, footerNode);
  } else {
    footerNode = footer;
  }
  return /* @__PURE__ */ React.createElement(DisabledContextProvider, {
    disabled: false
  }, footerNode);
};
function box(position) {
  return {
    position,
    inset: 0
  };
}
const genModalMaskStyle = (token) => {
  const {
    componentCls,
    antCls
  } = token;
  return [{
    [`${componentCls}-root`]: {
      [`${componentCls}${antCls}-zoom-enter, ${componentCls}${antCls}-zoom-appear`]: {
        // reset scale avoid mousePosition bug
        transform: "none",
        opacity: 0,
        animationDuration: token.motionDurationSlow,
        // https://github.com/ant-design/ant-design/issues/11777
        userSelect: "none"
      },
      // https://github.com/ant-design/ant-design/issues/37329
      // https://github.com/ant-design/ant-design/issues/40272
      [`${componentCls}${antCls}-zoom-leave ${componentCls}-container`]: {
        pointerEvents: "none"
      },
      [`${componentCls}-mask`]: {
        ...box("fixed"),
        zIndex: token.zIndexPopupBase,
        height: "100%",
        backgroundColor: token.colorBgMask,
        pointerEvents: "none",
        [`&${componentCls}-mask-blur`]: {
          backdropFilter: "blur(4px)"
        },
        [`${componentCls}-hidden`]: {
          display: "none"
        }
      },
      [`${componentCls}-wrap`]: {
        ...box("fixed"),
        zIndex: token.zIndexPopupBase,
        overflow: "auto",
        outline: 0,
        WebkitOverflowScrolling: "touch"
      }
    }
  }, {
    [`${componentCls}-root`]: initFadeMotion(token)
  }];
};
const genModalStyle = (token) => {
  const {
    componentCls,
    motionDurationMid
  } = token;
  return [
    // ======================== Root =========================
    {
      [`${componentCls}-root`]: {
        [`${componentCls}-wrap-rtl`]: {
          direction: "rtl"
        },
        [`${componentCls}-centered`]: {
          textAlign: "center",
          "&::before": {
            display: "inline-block",
            width: 0,
            height: "100%",
            verticalAlign: "middle",
            content: '""'
          },
          [componentCls]: {
            top: 0,
            display: "inline-block",
            paddingBottom: 0,
            textAlign: "start",
            verticalAlign: "middle"
          }
        },
        [`@media (max-width: ${token.screenSMMax}px)`]: {
          [componentCls]: {
            maxWidth: "calc(100vw - 16px)",
            margin: `${unit(token.marginXS)} auto`
          },
          [`${componentCls}-centered`]: {
            [componentCls]: {
              flex: 1
            }
          }
        }
      }
    },
    // ======================== Modal ========================
    {
      [componentCls]: {
        ...resetComponent(token),
        pointerEvents: "none",
        position: "relative",
        top: 100,
        width: "auto",
        maxWidth: `calc(100vw - ${unit(token.calc(token.margin).mul(2).equal())})`,
        margin: "0 auto",
        "&:focus-visible": {
          borderRadius: token.borderRadiusLG,
          ...genFocusOutline(token)
        },
        [`${componentCls}-title`]: {
          margin: 0,
          color: token.titleColor,
          fontWeight: token.fontWeightStrong,
          fontSize: token.titleFontSize,
          lineHeight: token.titleLineHeight,
          wordWrap: "break-word"
        },
        [`${componentCls}-container`]: {
          position: "relative",
          backgroundColor: token.contentBg,
          backgroundClip: "padding-box",
          border: 0,
          borderRadius: token.borderRadiusLG,
          boxShadow: token.boxShadow,
          pointerEvents: "auto",
          padding: token.contentPadding
        },
        [`${componentCls}-close`]: {
          position: "absolute",
          top: token.calc(token.modalHeaderHeight).sub(token.modalCloseBtnSize).div(2).equal(),
          insetInlineEnd: token.calc(token.modalHeaderHeight).sub(token.modalCloseBtnSize).div(2).equal(),
          zIndex: token.calc(token.zIndexPopupBase).add(10).equal(),
          padding: 0,
          color: token.modalCloseIconColor,
          fontWeight: token.fontWeightStrong,
          lineHeight: 1,
          textDecoration: "none",
          background: "transparent",
          borderRadius: token.borderRadiusSM,
          width: token.modalCloseBtnSize,
          height: token.modalCloseBtnSize,
          border: 0,
          outline: 0,
          cursor: "pointer",
          transition: ["color", "background-color"].map((prop) => `${prop} ${motionDurationMid}`).join(", "),
          "&-x": {
            display: "flex",
            fontSize: token.fontSizeLG,
            fontStyle: "normal",
            lineHeight: unit(token.modalCloseBtnSize),
            justifyContent: "center",
            textTransform: "none",
            textRendering: "auto"
          },
          "&:disabled": {
            pointerEvents: "none"
          },
          "&:hover": {
            color: token.modalCloseIconHoverColor,
            backgroundColor: token.colorBgTextHover,
            textDecoration: "none"
          },
          "&:active": {
            backgroundColor: token.colorBgTextActive
          },
          ...genFocusStyle(token)
        },
        [`${componentCls}-header`]: {
          color: token.colorText,
          background: token.headerBg,
          borderRadius: `${unit(token.borderRadiusLG)} ${unit(token.borderRadiusLG)} 0 0`,
          marginBottom: token.headerMarginBottom,
          padding: token.headerPadding,
          borderBottom: token.headerBorderBottom
        },
        [`${componentCls}-body`]: {
          fontSize: token.fontSize,
          lineHeight: token.lineHeight,
          wordWrap: "break-word",
          padding: token.bodyPadding,
          [`${componentCls}-body-skeleton`]: {
            width: "100%",
            height: "100%",
            display: "flex",
            justifyContent: "center",
            alignItems: "center",
            margin: `${unit(token.margin)} auto`
          }
        },
        [`${componentCls}-footer`]: {
          textAlign: "end",
          background: token.footerBg,
          marginTop: token.footerMarginTop,
          padding: token.footerPadding,
          borderTop: token.footerBorderTop,
          borderRadius: token.footerBorderRadius,
          [`> ${token.antCls}-btn + ${token.antCls}-btn`]: {
            marginInlineStart: token.marginXS
          }
        },
        [`${componentCls}-open`]: {
          overflow: "hidden"
        }
      }
    },
    // ======================== Pure =========================
    {
      [`${componentCls}-pure-panel`]: {
        top: "auto",
        padding: 0,
        display: "flex",
        flexDirection: "column",
        [`${componentCls}-container,
          ${componentCls}-body,
          ${componentCls}-confirm-body-wrapper`]: {
          display: "flex",
          flexDirection: "column",
          flex: "auto"
        },
        [`${componentCls}-confirm-body`]: {
          marginBottom: "auto"
        }
      }
    }
  ];
};
const genRTLStyle = (token) => {
  const {
    componentCls
  } = token;
  return {
    [`${componentCls}-root`]: {
      [`${componentCls}-wrap-rtl`]: {
        direction: "rtl",
        [`${componentCls}-confirm-body`]: {
          direction: "rtl"
        }
      }
    }
  };
};
const genResponsiveWidthStyle = (token) => {
  const {
    componentCls
  } = token;
  const oriGridMediaSizesMap = getMediaSize(token);
  const gridMediaSizesMap = {
    ...oriGridMediaSizesMap
  };
  delete gridMediaSizesMap.xs;
  const cssVarPrefix = `--${componentCls.replace(".", "")}-`;
  const responsiveStyles = Object.keys(gridMediaSizesMap).map((key) => ({
    [`@media (min-width: ${unit(gridMediaSizesMap[key])})`]: {
      width: `var(${cssVarPrefix}${key}-width)`
    }
  }));
  return {
    [`${componentCls}-root`]: {
      [componentCls]: [].concat(_toConsumableArray(Object.keys(oriGridMediaSizesMap).map((currentKey, index) => {
        const previousKey = Object.keys(oriGridMediaSizesMap)[index - 1];
        return previousKey ? {
          [`${cssVarPrefix}${currentKey}-width`]: `var(${cssVarPrefix}${previousKey}-width)`
        } : null;
      })), [{
        width: `var(${cssVarPrefix}xs-width)`
      }], _toConsumableArray(responsiveStyles))
    }
  };
};
const prepareToken = (token) => {
  const headerPaddingVertical = token.padding;
  const headerFontSize = token.fontSizeHeading5;
  const headerLineHeight = token.lineHeightHeading5;
  const modalToken = merge(token, {
    modalHeaderHeight: token.calc(token.calc(headerLineHeight).mul(headerFontSize).equal()).add(token.calc(headerPaddingVertical).mul(2).equal()).equal(),
    modalFooterBorderColorSplit: token.colorSplit,
    modalFooterBorderStyle: token.lineType,
    modalFooterBorderWidth: token.lineWidth,
    modalCloseIconColor: token.colorIcon,
    modalCloseIconHoverColor: token.colorIconHover,
    modalCloseBtnSize: token.controlHeight,
    modalConfirmIconSize: token.fontHeight,
    modalTitleHeight: token.calc(token.titleFontSize).mul(token.titleLineHeight).equal()
  });
  return modalToken;
};
const prepareComponentToken = (token) => ({
  footerBg: "transparent",
  headerBg: "transparent",
  titleLineHeight: token.lineHeightHeading5,
  titleFontSize: token.fontSizeHeading5,
  contentBg: token.colorBgElevated,
  titleColor: token.colorTextHeading,
  // internal
  contentPadding: token.wireframe ? 0 : `${unit(token.paddingMD)} ${unit(token.paddingContentHorizontalLG)}`,
  headerPadding: token.wireframe ? `${unit(token.padding)} ${unit(token.paddingLG)}` : 0,
  headerBorderBottom: token.wireframe ? `${unit(token.lineWidth)} ${token.lineType} ${token.colorSplit}` : "none",
  headerMarginBottom: token.wireframe ? 0 : token.marginXS,
  bodyPadding: token.wireframe ? token.paddingLG : 0,
  footerPadding: token.wireframe ? `${unit(token.paddingXS)} ${unit(token.padding)}` : 0,
  footerBorderTop: token.wireframe ? `${unit(token.lineWidth)} ${token.lineType} ${token.colorSplit}` : "none",
  footerBorderRadius: token.wireframe ? `0 0 ${unit(token.borderRadiusLG)} ${unit(token.borderRadiusLG)}` : 0,
  footerMarginTop: token.wireframe ? 0 : token.marginSM,
  confirmBodyPadding: token.wireframe ? `${unit(token.padding * 2)} ${unit(token.padding * 2)} ${unit(token.paddingLG)}` : 0,
  confirmIconMarginInlineEnd: token.wireframe ? token.margin : token.marginSM,
  confirmBtnsMarginTop: token.wireframe ? token.marginLG : token.marginSM,
  mask: true
});
const useStyle = genStyleHooks("Modal", (token) => {
  const modalToken = prepareToken(token);
  return [genModalStyle(modalToken), genRTLStyle(modalToken), genModalMaskStyle(modalToken), initZoomMotion(modalToken, "zoom"), genResponsiveWidthStyle(modalToken)];
}, prepareComponentToken, {
  unitless: {
    titleLineHeight: true
  }
});
let mousePosition;
const getClickPosition = (e) => {
  mousePosition = {
    x: e.pageX,
    y: e.pageY
  };
  setTimeout(() => {
    mousePosition = null;
  }, 100);
};
if (canUseDocElement()) {
  document.documentElement.addEventListener("click", getClickPosition, true);
}
const Modal$1 = (props) => {
  const {
    prefixCls: customizePrefixCls,
    className,
    rootClassName,
    open,
    wrapClassName,
    centered,
    getContainer,
    style,
    width = 520,
    footer,
    classNames,
    styles,
    children,
    loading,
    confirmLoading,
    zIndex: customizeZIndex,
    mousePosition: customizeMousePosition,
    onOk,
    onCancel,
    okButtonProps,
    cancelButtonProps,
    destroyOnHidden,
    destroyOnClose,
    panelRef = null,
    closable,
    mask: modalMask,
    modalRender,
    maskClosable,
    _semanticOmit,
    scrollLock,
    // Focusable
    focusTriggerAfterClose,
    focusable,
    _renderSemanticContent,
    ...restProps
  } = props;
  const {
    getPopupContainer: getContextPopupContainer,
    getPrefixCls,
    direction,
    className: contextClassName,
    style: contextStyle,
    classNames: contextClassNames,
    styles: contextStyles,
    centered: contextCentered,
    cancelButtonProps: contextCancelButtonProps,
    okButtonProps: contextOkButtonProps,
    mask: contextMask,
    focusable: contextFocusable
  } = useComponentConfig("modal");
  const {
    modal: modalContext
  } = reactExports.useContext(ConfigContext);
  const [closableAfterClose, onClose] = reactExports.useMemo(() => {
    if (typeof closable === "boolean") {
      return [void 0, void 0];
    }
    return [closable?.afterClose, closable?.onClose];
  }, [closable]);
  const prefixCls = getPrefixCls("modal", customizePrefixCls);
  const rootPrefixCls = getPrefixCls();
  const [mergedMask, maskBlurClassName, mergeMaskClosable] = useMergedMask(modalMask, contextMask, prefixCls, maskClosable);
  const mergedFocusable = useFocusable({
    ...contextFocusable,
    ...focusable
  }, mergedMask, focusTriggerAfterClose);
  const handleCancel = (e) => {
    if (confirmLoading) {
      return;
    }
    onCancel?.(e);
    onClose?.();
  };
  const handleOk = (e) => {
    onOk?.(e);
    onClose?.();
  };
  const rootCls = useCSSVarCls(prefixCls);
  const [hashId, cssVarCls] = useStyle(prefixCls, rootCls);
  const wrapClassNameExtended = clsx(wrapClassName, {
    [`${prefixCls}-centered`]: centered ?? contextCentered,
    [`${prefixCls}-wrap-rtl`]: direction === "rtl"
  });
  const dialogFooter = footer !== null && !loading ? /* @__PURE__ */ reactExports.createElement(Footer, {
    ...props,
    okButtonProps: {
      ...contextOkButtonProps,
      ...okButtonProps
    },
    onOk: handleOk,
    cancelButtonProps: {
      ...contextCancelButtonProps,
      ...cancelButtonProps
    },
    onCancel: handleCancel
  }) : null;
  const [rawClosable, mergedCloseIcon, closeBtnIsDisabled, ariaProps] = useClosable(pickClosable(props), pickClosable(modalContext), {
    closable: true,
    closeIcon: /* @__PURE__ */ reactExports.createElement(RefIcon, {
      className: `${prefixCls}-close-icon`
    }),
    closeIconRender: (icon) => renderCloseIcon(prefixCls, icon)
  });
  const mergedClosable = rawClosable ? {
    disabled: closeBtnIsDisabled,
    closeIcon: mergedCloseIcon,
    afterClose: closableAfterClose,
    ...ariaProps
  } : false;
  const mergedModalRender = modalRender ? (node) => /* @__PURE__ */ reactExports.createElement("div", {
    className: `${prefixCls}-render`
  }, modalRender(node)) : void 0;
  const panelClassName = `.${prefixCls}-${modalRender ? "render" : "container"}`;
  const innerPanelRef = usePanelRef(panelClassName);
  const mergedPanelRef = composeRef(panelRef, innerPanelRef);
  const [zIndex, contextZIndex] = useZIndex("Modal", customizeZIndex);
  const mergedProps = {
    ...props,
    width,
    panelRef,
    focusTriggerAfterClose: mergedFocusable.focusTriggerAfterClose,
    focusable: mergedFocusable,
    mask: mergedMask,
    maskClosable: mergeMaskClosable,
    zIndex
  };
  const [mergedClassNames, mergedStyles] = useMergeSemantic([contextClassNames, classNames, maskBlurClassName], [contextStyles, styles], {
    props: mergedProps
  });
  const dialogClassNames = _semanticOmit ? omit(mergedClassNames, _semanticOmit) : mergedClassNames;
  const dialogStyles = _semanticOmit ? omit(mergedStyles, _semanticOmit) : mergedStyles;
  const semanticContent = _renderSemanticContent ? _renderSemanticContent({
    classNames: mergedClassNames,
    styles: mergedStyles
  }) : children;
  const [numWidth, responsiveWidth] = reactExports.useMemo(() => {
    if (isPlainObject(width)) {
      return [void 0, width];
    }
    return [width, void 0];
  }, [width]);
  const responsiveWidthVars = reactExports.useMemo(() => {
    const vars = {};
    if (responsiveWidth) {
      Object.keys(responsiveWidth).forEach((breakpoint) => {
        const breakpointWidth = responsiveWidth[breakpoint];
        if (isNonNullable(breakpointWidth)) {
          vars[`--${prefixCls}-${breakpoint}-width`] = isNumber(breakpointWidth) ? `${breakpointWidth}px` : breakpointWidth;
        }
      });
    }
    return vars;
  }, [prefixCls, responsiveWidth]);
  return /* @__PURE__ */ reactExports.createElement(ContextIsolator, {
    form: true,
    space: true
  }, /* @__PURE__ */ reactExports.createElement(ZIndexContext.Provider, {
    value: contextZIndex
  }, /* @__PURE__ */ reactExports.createElement(DialogWrap, {
    width: numWidth,
    ...restProps,
    zIndex,
    getContainer: getContainer === void 0 ? getContextPopupContainer : getContainer,
    prefixCls,
    rootClassName: clsx(hashId, rootClassName, cssVarCls, rootCls, dialogClassNames.root),
    rootStyle: dialogStyles.root,
    footer: dialogFooter,
    visible: open,
    mousePosition: customizeMousePosition ?? mousePosition,
    onClose: handleCancel,
    closable: mergedClosable,
    closeIcon: mergedCloseIcon,
    transitionName: getTransitionName(rootPrefixCls, "zoom", props.transitionName),
    maskTransitionName: getTransitionName(rootPrefixCls, "fade", props.maskTransitionName),
    mask: mergedMask,
    maskClosable: mergeMaskClosable,
    scrollLock,
    className: clsx(hashId, className, contextClassName),
    style: {
      ...contextStyle,
      ...style,
      ...responsiveWidthVars
    },
    classNames: {
      ...dialogClassNames,
      wrapper: clsx(dialogClassNames.wrapper, wrapClassNameExtended)
    },
    styles: dialogStyles,
    panelRef: mergedPanelRef,
    destroyOnHidden: destroyOnHidden ?? destroyOnClose,
    modalRender: mergedModalRender,
    // Focusable
    focusTriggerAfterClose: mergedFocusable.focusTriggerAfterClose,
    focusTrap: mergedFocusable.trap
  }, loading ? /* @__PURE__ */ reactExports.createElement(Skeleton, {
    active: true,
    title: false,
    paragraph: {
      rows: 4
    },
    className: `${prefixCls}-body-skeleton`
  }) : semanticContent)));
};
const genModalConfirmStyle = (token) => {
  const {
    componentCls,
    titleFontSize,
    titleLineHeight,
    modalConfirmIconSize,
    fontSize,
    lineHeight,
    modalTitleHeight,
    fontHeight,
    confirmBodyPadding
  } = token;
  const confirmComponentCls = `${componentCls}-confirm`;
  return {
    [confirmComponentCls]: {
      "&-rtl": {
        direction: "rtl"
      },
      [`${token.antCls}-modal-header`]: {
        display: "none"
      },
      [`${confirmComponentCls}-body-wrapper`]: {
        ...clearFix()
      },
      [`&${componentCls} ${componentCls}-body`]: {
        padding: confirmBodyPadding
      },
      // ====================== Body ======================
      [`${confirmComponentCls}-body`]: {
        display: "flex",
        flexWrap: "nowrap",
        alignItems: "start",
        [`> ${token.iconCls}`]: {
          flex: "none",
          fontSize: modalConfirmIconSize,
          marginInlineEnd: token.confirmIconMarginInlineEnd,
          marginTop: token.calc(token.calc(fontHeight).sub(modalConfirmIconSize).equal()).div(2).equal()
        },
        [`&-has-title > ${token.iconCls}`]: {
          marginTop: token.calc(token.calc(modalTitleHeight).sub(modalConfirmIconSize).equal()).div(2).equal()
        }
      },
      [`${confirmComponentCls}-paragraph`]: {
        display: "flex",
        flexDirection: "column",
        flex: "auto",
        rowGap: token.marginXS,
        // https://github.com/ant-design/ant-design/issues/51912
        maxWidth: `calc(100% - ${unit(token.marginSM)})`
      },
      [`${confirmComponentCls}-body-no-icon ${confirmComponentCls}-paragraph`]: {
        maxWidth: "100%"
      },
      // https://github.com/ant-design/ant-design/issues/48159
      [`${token.iconCls} + ${confirmComponentCls}-paragraph`]: {
        maxWidth: `calc(100% - ${unit(token.calc(token.modalConfirmIconSize).add(token.marginSM).equal())})`
      },
      [`${confirmComponentCls}-title`]: {
        color: token.colorTextHeading,
        fontWeight: token.fontWeightStrong,
        fontSize: titleFontSize,
        lineHeight: titleLineHeight
      },
      [`${confirmComponentCls}-container`]: {
        color: token.colorText,
        fontSize,
        lineHeight
      },
      // ===================== Footer =====================
      [`${confirmComponentCls}-btns`]: {
        textAlign: "end",
        marginTop: token.confirmBtnsMarginTop,
        [`${token.antCls}-btn + ${token.antCls}-btn`]: {
          marginBottom: 0,
          marginInlineStart: token.marginXS
        }
      }
    },
    [`${confirmComponentCls}-error ${confirmComponentCls}-body > ${token.iconCls}`]: {
      color: token.colorError
    },
    [`${confirmComponentCls}-warning ${confirmComponentCls}-body > ${token.iconCls},
        ${confirmComponentCls}-confirm ${confirmComponentCls}-body > ${token.iconCls}`]: {
      color: token.colorWarning
    },
    [`${confirmComponentCls}-info ${confirmComponentCls}-body > ${token.iconCls}`]: {
      color: token.colorInfo
    },
    [`${confirmComponentCls}-success ${confirmComponentCls}-body > ${token.iconCls}`]: {
      color: token.colorSuccess
    }
  };
};
const Confirm = genSubStyleComponent(["Modal", "confirm"], (token) => {
  const modalToken = prepareToken(token);
  return genModalConfirmStyle(modalToken);
}, prepareComponentToken, {
  // confirm is weak than modal since no conflict here
  order: -1e3
});
const CONFIRM_OMIT_SEMANTIC_NAMES = ["body"];
const ConfirmContent = (props) => {
  const {
    prefixCls,
    icon,
    okText,
    cancelText,
    confirmPrefixCls,
    type,
    okCancel,
    footer,
    // Legacy for static function usage
    locale: staticLocale,
    autoFocusButton,
    focusable,
    contentClassName,
    contentStyle,
    ...restProps
  } = props;
  const {
    infoIcon,
    successIcon,
    errorIcon,
    warningIcon
  } = useComponentConfig("modal");
  let mergedIcon = icon;
  if (icon === void 0) {
    switch (type) {
      case "info":
        mergedIcon = fallbackProp(infoIcon, /* @__PURE__ */ reactExports.createElement(RefIcon$4, null));
        break;
      case "success":
        mergedIcon = fallbackProp(successIcon, /* @__PURE__ */ reactExports.createElement(RefIcon$3, null));
        break;
      case "error":
        mergedIcon = fallbackProp(errorIcon, /* @__PURE__ */ reactExports.createElement(RefIcon$2, null));
        break;
      default:
        mergedIcon = fallbackProp(warningIcon, /* @__PURE__ */ reactExports.createElement(RefIcon$1, null));
    }
  }
  const mergedOkCancel = okCancel ?? type === "confirm";
  const mergedAutoFocusButton = reactExports.useMemo(() => {
    const base = focusable?.autoFocusButton || autoFocusButton;
    return base || base === null ? base : "ok";
  }, [autoFocusButton, focusable?.autoFocusButton]);
  const [locale] = useLocale("Modal");
  const mergedLocale = staticLocale || locale;
  const okTextLocale = okText || (mergedOkCancel ? mergedLocale?.okText : mergedLocale?.justOkText);
  const cancelTextLocale = cancelText || mergedLocale?.cancelText;
  const {
    closable
  } = restProps;
  const {
    onClose
  } = isPlainObject(closable) ? closable : {};
  const memoizedValue = reactExports.useMemo(() => {
    return {
      autoFocusButton: mergedAutoFocusButton,
      cancelTextLocale,
      okTextLocale,
      mergedOkCancel,
      onClose,
      ...restProps
    };
  }, [mergedAutoFocusButton, cancelTextLocale, okTextLocale, mergedOkCancel, onClose, restProps]);
  const footerOriginNode = /* @__PURE__ */ reactExports.createElement(reactExports.Fragment, null, /* @__PURE__ */ reactExports.createElement(ConfirmCancelBtn, null), /* @__PURE__ */ reactExports.createElement(ConfirmOkBtn, null));
  const hasTitle = isReactRenderable(props.title);
  const hasIcon = isReactRenderable(mergedIcon);
  const bodyCls = `${confirmPrefixCls}-body`;
  return /* @__PURE__ */ reactExports.createElement("div", {
    className: `${confirmPrefixCls}-body-wrapper`
  }, /* @__PURE__ */ reactExports.createElement("div", {
    className: clsx(bodyCls, {
      [`${bodyCls}-has-title`]: hasTitle,
      [`${bodyCls}-no-icon`]: !hasIcon
    })
  }, mergedIcon, /* @__PURE__ */ reactExports.createElement("div", {
    className: `${confirmPrefixCls}-paragraph`
  }, hasTitle && /* @__PURE__ */ reactExports.createElement("span", {
    className: `${confirmPrefixCls}-title`
  }, props.title), /* @__PURE__ */ reactExports.createElement("div", {
    className: clsx(`${confirmPrefixCls}-content`, contentClassName),
    style: contentStyle
  }, props.content))), footer === void 0 || isFunction(footer) ? /* @__PURE__ */ reactExports.createElement(ModalContextProvider, {
    value: memoizedValue
  }, /* @__PURE__ */ reactExports.createElement("div", {
    className: `${confirmPrefixCls}-btns`
  }, isFunction(footer) ? footer(footerOriginNode, {
    OkBtn: ConfirmOkBtn,
    CancelBtn: ConfirmCancelBtn
  }) : footerOriginNode)) : footer, /* @__PURE__ */ reactExports.createElement(Confirm, {
    prefixCls
  }));
};
const ConfirmDialog = (props) => {
  const {
    close,
    zIndex,
    maskStyle,
    direction,
    prefixCls,
    wrapClassName,
    rootPrefixCls,
    bodyStyle,
    closable = false,
    onConfirm,
    styles,
    title,
    mask,
    maskClosable,
    okButtonProps,
    cancelButtonProps
  } = props;
  const {
    cancelButtonProps: contextCancelButtonProps,
    okButtonProps: contextOkButtonProps
  } = useComponentConfig("modal");
  const confirmPrefixCls = `${prefixCls}-confirm`;
  const width = props.width || 416;
  const style = props.style || {};
  const semanticStyles = isFunction(styles) ? (info) => ({
    body: bodyStyle,
    mask: maskStyle,
    ...styles(info)
  }) : {
    body: bodyStyle,
    mask: maskStyle,
    ...styles
  };
  const modalProps = omit(props, ["bodyStyle", "maskStyle"]);
  const classString = clsx(confirmPrefixCls, `${confirmPrefixCls}-${props.type}`, {
    [`${confirmPrefixCls}-rtl`]: direction === "rtl"
  }, props.className);
  const mergedMask = reactExports.useMemo(() => {
    const nextMaskConfig = normalizeMaskConfig(mask, maskClosable);
    nextMaskConfig.closable ?? (nextMaskConfig.closable = false);
    return nextMaskConfig;
  }, [mask, maskClosable]);
  const [, token] = useToken();
  const mergedZIndex = reactExports.useMemo(() => {
    if (zIndex !== void 0) {
      return zIndex;
    }
    return token.zIndexPopupBase + CONTAINER_MAX_OFFSET;
  }, [zIndex, token]);
  return /* @__PURE__ */ reactExports.createElement(Modal$1, {
    ...modalProps,
    className: classString,
    wrapClassName: clsx({
      [`${confirmPrefixCls}-centered`]: !!props.centered
    }, wrapClassName),
    onCancel: () => {
      close?.({
        triggerCancel: true
      });
      onConfirm?.(false);
    },
    title,
    footer: null,
    transitionName: getTransitionName(rootPrefixCls || "", "zoom", props.transitionName),
    maskTransitionName: getTransitionName(rootPrefixCls || "", "fade", props.maskTransitionName),
    mask: mergedMask,
    style,
    styles: semanticStyles,
    width,
    zIndex: mergedZIndex,
    closable,
    _semanticOmit: CONFIRM_OMIT_SEMANTIC_NAMES,
    _renderSemanticContent: ({
      classNames: mergedClassNames,
      styles: mergedStyles
    }) => /* @__PURE__ */ reactExports.createElement(ConfirmContent, {
      ...props,
      confirmPrefixCls,
      okButtonProps: {
        ...contextOkButtonProps,
        ...okButtonProps
      },
      cancelButtonProps: {
        ...contextCancelButtonProps,
        ...cancelButtonProps
      },
      contentClassName: mergedClassNames.body,
      contentStyle: mergedStyles.body
    })
  });
};
const ConfirmDialogWrapper$1 = (props) => {
  const {
    rootPrefixCls,
    iconPrefixCls,
    direction,
    theme
  } = props;
  return /* @__PURE__ */ reactExports.createElement(ConfigProvider, {
    prefixCls: rootPrefixCls,
    iconPrefixCls,
    direction,
    theme
  }, /* @__PURE__ */ reactExports.createElement(ConfirmDialog, {
    ...props
  }));
};
const destroyFns = [];
let defaultRootPrefixCls = "";
function getRootPrefixCls() {
  return defaultRootPrefixCls;
}
const ConfirmDialogWrapper = (props) => {
  const {
    prefixCls: customizePrefixCls,
    getContainer,
    direction
  } = props;
  const runtimeLocale = getConfirmLocale();
  const config = reactExports.useContext(ConfigContext);
  const rootPrefixCls = getRootPrefixCls() || config.getPrefixCls();
  const prefixCls = customizePrefixCls || `${rootPrefixCls}-modal`;
  let mergedGetContainer = getContainer;
  if (mergedGetContainer === false) {
    mergedGetContainer = void 0;
  }
  return /* @__PURE__ */ React.createElement(ConfirmDialogWrapper$1, {
    ...props,
    rootPrefixCls,
    prefixCls,
    iconPrefixCls: config.iconPrefixCls,
    theme: config.theme,
    direction: direction ?? config.direction,
    locale: config.locale?.Modal ?? runtimeLocale,
    getContainer: mergedGetContainer
  });
};
function confirm(config) {
  const global = globalConfig();
  const container = document.createDocumentFragment();
  let currentConfig = {
    ...config,
    close,
    open: true
  };
  let timeoutId;
  function destroy(...args) {
    const triggerCancel = args.some((param) => param?.triggerCancel);
    if (triggerCancel) {
      config.onCancel?.(() => {
      }, ...args.slice(1));
    }
    for (let i = 0; i < destroyFns.length; i++) {
      const fn = destroyFns[i];
      if (fn === close) {
        destroyFns.splice(i, 1);
        break;
      }
    }
    unmount(container).then(() => {
    });
  }
  const scheduleRender = (props) => {
    clearTimeout(timeoutId);
    timeoutId = setTimeout(() => {
      const rootPrefixCls = global.getPrefixCls(void 0, getRootPrefixCls());
      const iconPrefixCls = global.getIconPrefixCls();
      const theme = global.getTheme();
      const dom = /* @__PURE__ */ React.createElement(ConfirmDialogWrapper, {
        ...props
      });
      render(/* @__PURE__ */ React.createElement(ConfigProvider, {
        prefixCls: rootPrefixCls,
        iconPrefixCls,
        theme
      }, isFunction(global.holderRender) ? global.holderRender(dom) : dom), container);
    });
  };
  function close(...args) {
    currentConfig = {
      ...currentConfig,
      open: false,
      afterClose: () => {
        if (isFunction(config.afterClose)) {
          config.afterClose();
        }
        destroy.apply(this, args);
      }
    };
    scheduleRender(currentConfig);
  }
  function update(configUpdate) {
    if (isFunction(configUpdate)) {
      currentConfig = configUpdate(currentConfig);
    } else {
      currentConfig = {
        ...currentConfig,
        ...configUpdate
      };
    }
    scheduleRender(currentConfig);
  }
  scheduleRender(currentConfig);
  destroyFns.push(close);
  return {
    destroy: close,
    update
  };
}
function withWarn(props) {
  return {
    ...props,
    type: "warning"
  };
}
function withInfo(props) {
  return {
    ...props,
    type: "info"
  };
}
function withSuccess(props) {
  return {
    ...props,
    type: "success"
  };
}
function withError(props) {
  return {
    ...props,
    type: "error"
  };
}
function withConfirm(props) {
  return {
    ...props,
    type: "confirm"
  };
}
function modalGlobalConfig({
  rootPrefixCls
}) {
  defaultRootPrefixCls = rootPrefixCls;
}
const HookModal = /* @__PURE__ */ reactExports.forwardRef((props, ref) => {
  const {
    afterClose: hookAfterClose,
    config,
    ...restProps
  } = props;
  const [open, setOpen] = reactExports.useState(true);
  const [innerConfig, setInnerConfig] = reactExports.useState(config);
  const {
    direction,
    getPrefixCls
  } = reactExports.useContext(ConfigContext);
  const prefixCls = getPrefixCls("modal");
  const rootPrefixCls = getPrefixCls();
  const afterClose = () => {
    hookAfterClose();
    innerConfig.afterClose?.();
  };
  const close = (...args) => {
    setOpen(false);
    const triggerCancel = args.some((param) => param?.triggerCancel);
    if (triggerCancel) {
      innerConfig.onCancel?.(() => {
      }, ...args.slice(1));
    }
  };
  reactExports.useImperativeHandle(ref, () => ({
    destroy: close,
    update: (newConfig) => {
      setInnerConfig((originConfig) => {
        const nextConfig = isFunction(newConfig) ? newConfig(originConfig) : newConfig;
        return {
          ...originConfig,
          ...nextConfig
        };
      });
    }
  }));
  const mergedOkCancel = innerConfig.okCancel ?? innerConfig.type === "confirm";
  const [contextLocale] = useLocale("Modal", localeValues.Modal);
  return /* @__PURE__ */ reactExports.createElement(ConfirmDialogWrapper$1, {
    prefixCls,
    rootPrefixCls,
    ...innerConfig,
    close,
    open,
    afterClose,
    okText: innerConfig.okText || (mergedOkCancel ? contextLocale?.okText : contextLocale?.justOkText),
    direction: innerConfig.direction || direction,
    cancelText: innerConfig.cancelText || contextLocale?.cancelText,
    ...restProps
  });
});
let uuid = 0;
const ElementsHolder = /* @__PURE__ */ reactExports.memo(/* @__PURE__ */ reactExports.forwardRef((_props, ref) => {
  const [elements, patchElement] = usePatchElement();
  reactExports.useImperativeHandle(ref, () => ({
    patchElement
  }), [patchElement]);
  return /* @__PURE__ */ reactExports.createElement(reactExports.Fragment, null, elements);
}));
function useModal() {
  const holderRef = reactExports.useRef(null);
  const [actionQueue, setActionQueue] = reactExports.useState([]);
  reactExports.useEffect(() => {
    if (actionQueue.length) {
      const cloneQueue = _toConsumableArray(actionQueue);
      cloneQueue.forEach((action) => {
        action();
      });
      setActionQueue([]);
    }
  }, [actionQueue]);
  const getConfirmFunc = reactExports.useCallback((withFunc) => function hookConfirm(config) {
    uuid += 1;
    const modalRef = /* @__PURE__ */ reactExports.createRef();
    let resolvePromise;
    const promise = new Promise((resolve) => {
      resolvePromise = resolve;
    });
    let silent = false;
    let closeFunc;
    const modal = /* @__PURE__ */ reactExports.createElement(HookModal, {
      key: `modal-${uuid}`,
      config: withFunc(config),
      ref: modalRef,
      afterClose: () => {
        closeFunc?.();
      },
      isSilent: () => silent,
      onConfirm: (confirmed) => {
        resolvePromise(confirmed);
      }
    });
    closeFunc = holderRef.current?.patchElement(modal);
    if (closeFunc) {
      destroyFns.push(closeFunc);
    }
    const instance = {
      destroy: () => {
        function destroyAction() {
          modalRef.current?.destroy();
        }
        if (modalRef.current) {
          destroyAction();
        } else {
          setActionQueue((prev) => [].concat(_toConsumableArray(prev), [destroyAction]));
        }
      },
      update: (newConfig) => {
        function updateAction() {
          modalRef.current?.update(newConfig);
        }
        if (modalRef.current) {
          updateAction();
        } else {
          setActionQueue((prev) => [].concat(_toConsumableArray(prev), [updateAction]));
        }
      },
      then: (resolve) => {
        silent = true;
        return promise.then(resolve);
      }
    };
    return instance;
  }, []);
  const fns = reactExports.useMemo(() => ({
    info: getConfirmFunc(withInfo),
    success: getConfirmFunc(withSuccess),
    error: getConfirmFunc(withError),
    warning: getConfirmFunc(withWarn),
    confirm: getConfirmFunc(withConfirm)
  }), [getConfirmFunc]);
  return [fns, /* @__PURE__ */ reactExports.createElement(ElementsHolder, {
    key: "modal-holder",
    ref: holderRef
  })];
}
const PurePanel = (props) => {
  const {
    prefixCls: customizePrefixCls,
    className,
    closeIcon,
    closable,
    type,
    title,
    children,
    footer,
    style,
    classNames,
    styles,
    ...restProps
  } = props;
  const {
    getPrefixCls
  } = reactExports.useContext(ConfigContext);
  const {
    className: contextClassName,
    style: contextStyle,
    classNames: contextClassNames,
    styles: contextStyles
  } = useComponentConfig("modal");
  const rootPrefixCls = getPrefixCls();
  const prefixCls = customizePrefixCls || getPrefixCls("modal");
  const rootCls = useCSSVarCls(rootPrefixCls);
  const [hashId, cssVarCls] = useStyle(prefixCls, rootCls);
  const contextStyleRoot = useSemanticRootStyle(contextStyle);
  const styleRoot = useSemanticRootStyle(style);
  const [mergedClassNames, mergedStyles] = useMergeSemantic([contextClassNames, classNames], [contextStyles, contextStyleRoot, styles, styleRoot], {
    props
  });
  const confirmPrefixCls = `${prefixCls}-confirm`;
  let additionalProps = {};
  if (type) {
    additionalProps = {
      closable: closable ?? false,
      title: "",
      footer: "",
      children: /* @__PURE__ */ reactExports.createElement(ConfirmContent, {
        ...props,
        prefixCls,
        confirmPrefixCls,
        rootPrefixCls,
        content: children
      })
    };
  } else {
    additionalProps = {
      closable: closable ?? true,
      title,
      footer: footer !== null && /* @__PURE__ */ reactExports.createElement(Footer, {
        ...props
      }),
      children
    };
  }
  return /* @__PURE__ */ reactExports.createElement(Panel, {
    prefixCls,
    className: clsx(hashId, `${prefixCls}-pure-panel`, type && confirmPrefixCls, type && `${confirmPrefixCls}-${type}`, className, contextClassName, cssVarCls, rootCls, mergedClassNames.root),
    style: mergedStyles.root,
    ...restProps,
    closeIcon: renderCloseIcon(prefixCls, closeIcon),
    closable,
    classNames: mergedClassNames,
    styles: mergedStyles,
    ...additionalProps
  });
};
const PurePanel$1 = withPureRenderTheme(PurePanel);
function modalWarn(props) {
  return confirm(withWarn(props));
}
const Modal = Modal$1;
Modal.useModal = useModal;
Modal.info = function infoFn(props) {
  return confirm(withInfo(props));
};
Modal.success = function successFn(props) {
  return confirm(withSuccess(props));
};
Modal.error = function errorFn(props) {
  return confirm(withError(props));
};
Modal.warning = modalWarn;
Modal.warn = modalWarn;
Modal.confirm = function confirmFn(props) {
  return confirm(withConfirm(props));
};
Modal.destroyAll = function destroyAllFn() {
  while (destroyFns.length) {
    const close = destroyFns.pop();
    if (close) {
      close();
    }
  }
};
Modal.config = modalGlobalConfig;
Modal._InternalPanelDoNotUseOrYouWillBeFired = PurePanel$1;
const automationApi = {
  async getState() {
    return invoke("automation.getState", {});
  },
  async setEnabled(enabled) {
    return invoke("automation.setEnabled", { enabled });
  },
  async savePipelines(pipelines, isEnabled) {
    const params = { pipelines };
    if (isEnabled !== void 0) params.isEnabled = isEnabled;
    return invoke("automation.savePipelines", params);
  },
  async runNow(pipelineId) {
    return invoke("automation.runNow", { pipelineId });
  },
  async getSupportedSteps() {
    return invoke("automation.getSupportedSteps", {});
  }
};
const defaultState = { isEnabled: false, pipelines: [] };
const useAutomationStore = create()((set, get) => ({
  state: defaultState,
  steps: [],
  loaded: false,
  loading: false,
  error: null,
  async load() {
    if (get().loading) return;
    set({ loading: true, error: null });
    try {
      const [state, supported] = await Promise.all([
        automationApi.getState(),
        automationApi.getSupportedSteps()
      ]);
      set({ state, steps: supported.steps, loaded: true });
    } catch (error) {
      set({ error: error.message });
    } finally {
      set({ loading: false });
    }
  },
  async setEnabled(enabled) {
    try {
      const res = await automationApi.setEnabled(enabled);
      if (!res.ok) return false;
      set({ state: { ...get().state, isEnabled: enabled } });
      return true;
    } catch (error) {
      set({ error: error.message });
      return false;
    }
  },
  async save(pipelines, isEnabled) {
    try {
      const res = await automationApi.savePipelines(pipelines, isEnabled);
      if (!res.saved) return false;
      await get().load();
      return true;
    } catch (error) {
      set({ error: error.message });
      return false;
    }
  },
  async runNow(pipelineId) {
    try {
      const res = await automationApi.runNow(pipelineId);
      return res.ok;
    } catch (error) {
      set({ error: error.message });
      return false;
    }
  }
}));
function shortTypeName(type) {
  return type.replace(/AutomationStep$/, "").replace(/AutomationPipelineTrigger$/, "");
}
function stepSummary(step) {
  const keys = Object.keys(step).filter((k) => k !== "$type");
  if (keys.length === 0) return "";
  const first = step[keys[0]];
  return JSON.stringify(first);
}
function AutomationPage() {
  const { t } = useTranslation();
  const { state, steps, load, setEnabled, save, runNow } = useAutomationStore();
  const [pipelines, setPipelines] = reactExports.useState([]);
  const [dirty, setDirty] = reactExports.useState(false);
  const [createOpen, setCreateOpen] = reactExports.useState(false);
  const [createName, setCreateName] = reactExports.useState("");
  const [addStepFor, setAddStepFor] = reactExports.useState(null);
  const [selectedStepType, setSelectedStepType] = reactExports.useState("");
  reactExports.useEffect(() => {
    void load().then(() => setPipelines([]));
  }, [load]);
  reactExports.useEffect(() => {
    setPipelines(state?.pipelines ?? []);
  }, [state]);
  const markDirty = (next) => {
    setPipelines(next);
    setDirty(true);
  };
  const handleSave = async () => {
    try {
      await save(pipelines, state?.isEnabled);
      setDirty(false);
      staticMethods.success(t("settings.saved"));
    } catch (error) {
      staticMethods.error(error.message);
    }
  };
  const handleRevert = () => {
    void load();
    setDirty(false);
  };
  const handleCreate = () => {
    if (!createName.trim()) return;
    const pipeline = {
      id: crypto.randomUUID(),
      name: createName.trim(),
      trigger: null,
      steps: [],
      isExclusive: false
    };
    markDirty([...pipelines, pipeline]);
    setCreateOpen(false);
    setCreateName("");
  };
  const handleDelete = (id) => {
    markDirty(pipelines.filter((p) => p.id !== id));
  };
  const handleAddStep = () => {
    if (!addStepFor || !selectedStepType) return;
    const stepType = selectedStepType.endsWith("AutomationStep") ? selectedStepType : `${selectedStepType}AutomationStep`;
    markDirty(
      pipelines.map(
        (p) => p.id === addStepFor ? { ...p, steps: [...p.steps ?? [], { $type: stepType }] } : p
      )
    );
    setAddStepFor(null);
    setSelectedStepType("");
  };
  const handleRemoveStep = (pipelineId, index) => {
    markDirty(
      pipelines.map(
        (p) => p.id === pipelineId ? { ...p, steps: (p.steps ?? []).filter((_, i) => i !== index) } : p
      )
    );
  };
  return /* @__PURE__ */ jsxRuntimeExports.jsxs(Flex, { vertical: true, gap: 16, children: [
    /* @__PURE__ */ jsxRuntimeExports.jsxs(Flex, { align: "center", justify: "space-between", children: [
      /* @__PURE__ */ jsxRuntimeExports.jsx(Typography.Title, { level: 3, style: { margin: 0 }, children: t("automation.title") }),
      /* @__PURE__ */ jsxRuntimeExports.jsxs(Space, { children: [
        /* @__PURE__ */ jsxRuntimeExports.jsx("span", { children: t("automation.enable") }),
        /* @__PURE__ */ jsxRuntimeExports.jsx(
          Switch,
          {
            checked: state?.isEnabled ?? false,
            onChange: (checked) => void setEnabled(checked)
          }
        )
      ] })
    ] }),
    pipelines.length === 0 ? /* @__PURE__ */ jsxRuntimeExports.jsx(Empty, { description: t("automation.empty") }) : /* @__PURE__ */ jsxRuntimeExports.jsx(
      List,
      {
        dataSource: pipelines,
        renderItem: (pipeline) => /* @__PURE__ */ jsxRuntimeExports.jsx(
          Card,
          {
            size: "small",
            title: /* @__PURE__ */ jsxRuntimeExports.jsxs(Space, { children: [
              pipeline.name ?? t("automation.quickAction"),
              /* @__PURE__ */ jsxRuntimeExports.jsx(Tag, { children: shortTypeName(String(pipeline.trigger?.["$type"] ?? "quickAction")) }),
              /* @__PURE__ */ jsxRuntimeExports.jsx(Tag, { children: (pipeline.steps ?? []).length })
            ] }),
            extra: /* @__PURE__ */ jsxRuntimeExports.jsxs(Space, { children: [
              /* @__PURE__ */ jsxRuntimeExports.jsx(
                Button,
                {
                  size: "small",
                  disabled: pipeline.trigger !== null && pipeline.trigger !== void 0,
                  onClick: () => void runNow(pipeline.id),
                  children: t("automation.runNow")
                }
              ),
              /* @__PURE__ */ jsxRuntimeExports.jsx(
                Popconfirm,
                {
                  title: t("automation.delete"),
                  onConfirm: () => handleDelete(pipeline.id),
                  children: /* @__PURE__ */ jsxRuntimeExports.jsx(Button, { size: "small", danger: true, children: t("automation.delete") })
                }
              )
            ] }),
            children: /* @__PURE__ */ jsxRuntimeExports.jsx(
              Collapse,
              {
                ghost: true,
                size: "small",
                items: [
                  {
                    key: pipeline.id,
                    label: `${t("automation.steps")} (${(pipeline.steps ?? []).length})`,
                    children: /* @__PURE__ */ jsxRuntimeExports.jsxs(Space, { direction: "vertical", style: { width: "100%" }, children: [
                      (pipeline.steps ?? []).map((step, index) => /* @__PURE__ */ jsxRuntimeExports.jsxs(Flex, { justify: "space-between", align: "center", children: [
                        /* @__PURE__ */ jsxRuntimeExports.jsxs(Typography.Text, { code: true, children: [
                          shortTypeName(step.$type),
                          stepSummary(step) && ` · ${stepSummary(step)}`
                        ] }),
                        /* @__PURE__ */ jsxRuntimeExports.jsx(
                          Button,
                          {
                            size: "small",
                            danger: true,
                            onClick: () => handleRemoveStep(pipeline.id, index),
                            children: t("automation.deleteStep")
                          }
                        )
                      ] }, index)),
                      /* @__PURE__ */ jsxRuntimeExports.jsx(
                        Button,
                        {
                          size: "small",
                          onClick: () => {
                            setAddStepFor(pipeline.id);
                            setSelectedStepType("");
                          },
                          children: t("automation.addStep")
                        }
                      )
                    ] })
                  }
                ]
              }
            )
          }
        )
      }
    ),
    /* @__PURE__ */ jsxRuntimeExports.jsxs(Flex, { gap: 8, children: [
      /* @__PURE__ */ jsxRuntimeExports.jsx(Button, { type: "primary", onClick: () => setCreateOpen(true), children: t("automation.addPipeline") }),
      dirty && /* @__PURE__ */ jsxRuntimeExports.jsxs(jsxRuntimeExports.Fragment, { children: [
        /* @__PURE__ */ jsxRuntimeExports.jsx(Button, { onClick: handleRevert, children: t("automation.revert") }),
        /* @__PURE__ */ jsxRuntimeExports.jsx(Button, { type: "primary", onClick: () => void handleSave(), children: t("automation.save") })
      ] })
    ] }),
    /* @__PURE__ */ jsxRuntimeExports.jsx(
      Modal,
      {
        title: t("automation.pipelineName"),
        open: createOpen,
        onOk: handleCreate,
        onCancel: () => setCreateOpen(false),
        okButtonProps: { disabled: !createName.trim() },
        children: /* @__PURE__ */ jsxRuntimeExports.jsx(
          Input,
          {
            value: createName,
            onChange: (e) => setCreateName(e.target.value),
            placeholder: t("automation.pipelineNamePlaceholder")
          }
        )
      }
    ),
    /* @__PURE__ */ jsxRuntimeExports.jsx(
      Modal,
      {
        title: t("automation.addStep"),
        open: addStepFor !== null,
        onOk: handleAddStep,
        onCancel: () => setAddStepFor(null),
        okButtonProps: { disabled: !selectedStepType },
        children: /* @__PURE__ */ jsxRuntimeExports.jsx(
          Select,
          {
            style: { width: "100%" },
            value: selectedStepType || void 0,
            placeholder: t("automation.stepType"),
            options: (steps ?? []).map((s) => ({
              value: s,
              label: shortTypeName(s)
            })),
            onChange: setSelectedStepType
          }
        )
      }
    )
  ] });
}
export {
  AutomationPage as default
};
