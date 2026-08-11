import { c6 as genComponentStyleHook, a$ as genCssVar, c7 as defaultPrefixCls, bp as isString, aw as render, r as reactExports, e as ConfigContext, aV as wrapperRaf, a3 as CSSMotion, c8 as isTransitionEvent, ay as unmount, f as clsx, af as composeRef, au as useToken, K as useEvent, $ as React, c9 as supportRef, ca as getNodeRef, k as cloneElement, cb as isVisible, bt as Keyframe, cc as initMotion, aO as _extends$b, bg as KeyCode, g as toArray$1, aE as useControlledState, aU as warningOnce, a2 as pickAttrs, v as genStyleHooks, w as merge, n as unit, E as resetIcon, ac as genFocusStyle, A as resetComponent, J as useComponentConfig, aL as useSize, L as useSemanticRootStyle, N as useMergeSemantic, l as isFunction, bI as RefIcon$2, U as initCollapseMotion, h as omit, aG as Color, a8 as useLocale, aP as generateColor, at as ConfigProvider, cd as Trigger, aN as useLayoutEffect, ce as ForwardOverflow, cf as getDOM, cg as RefResizeObserver, bh as reactDomExports, ch as useMemo, a4 as useId, B as FastColor, q as textEllipsis, ci as slideDownOut, cj as slideUpOut, ck as slideDownIn, cl as slideUpIn, x as initSlideMotion, a_ as genCompactItemStyle, aA as getDefaultExportFromCjs, aB as Icon, an as fallbackProp, ap as RefIcon$3, br as RefIcon$4, bu as RefIcon$5, ab as RefIcon$6, ai as ContextIsolator, b7 as useCompactItemContext, b8 as useVariant, aJ as FormItemInputContext, aK as DisabledContext, b9 as getStatusClassNames, H as useZIndex, ak as getTransitionName, Q as useCSSVarCls, ba as getMergedStatus } from "./index-3RTipSd5.js";
const genWaveStyle = (token) => {
  const {
    componentCls,
    colorPrimary,
    motionDurationSlow,
    motionEaseInOut,
    motionEaseOutCirc,
    antCls
  } = token;
  const [, varRef] = genCssVar(antCls, "wave");
  return {
    [componentCls]: {
      position: "absolute",
      background: "transparent",
      pointerEvents: "none",
      boxSizing: "border-box",
      color: varRef("color", colorPrimary),
      boxShadow: `0 0 0 0 currentcolor`,
      opacity: 0.2,
      // =================== Motion ===================
      "&.wave-motion-appear": {
        transition: [`box-shadow 0.4s`, `opacity 2s`].map((prop) => `${prop} ${motionEaseOutCirc}`).join(","),
        "&-active": {
          boxShadow: `0 0 0 6px currentcolor`,
          opacity: 0
        },
        "&.wave-quick": {
          transition: [`box-shadow`, `opacity`].map((prop) => `${prop} ${motionDurationSlow} ${motionEaseInOut}`).join(",")
        }
      }
    }
  };
};
const useStyle$2 = genComponentStyleHook("Wave", genWaveStyle);
const TARGET_CLS = `${defaultPrefixCls}-wave-target`;
const isValidWaveColor = (color) => {
  if (!color) {
    return false;
  }
  return isString(color) && color !== "#fff" && color !== "#ffffff" && color !== "rgb(255, 255, 255)" && color !== "rgba(255, 255, 255, 1)" && !/rgba\((?:\d*, ){3}0\)/i.test(color) && // any transparent rgba color
  !/^#(?:[0-9a-f]{3}0|[0-9a-f]{6}00)$/i.test(color) && // any transparent hex color
  color !== "transparent" && color !== "canvastext";
};
function getTargetWaveColor(node, colorSource = null) {
  const style = getComputedStyle(node);
  const {
    borderTopColor,
    borderColor,
    backgroundColor
  } = style;
  if (colorSource && isValidWaveColor(style[colorSource])) {
    return style[colorSource];
  }
  return [borderTopColor, borderColor, backgroundColor].find(isValidWaveColor) ?? null;
}
function validateNum(value) {
  return Number.isNaN(value) ? 0 : value;
}
const WaveEffect = (props) => {
  const {
    className,
    target,
    component,
    colorSource
  } = props;
  const divRef = reactExports.useRef(null);
  const {
    getPrefixCls
  } = reactExports.useContext(ConfigContext);
  const rootPrefixCls = getPrefixCls();
  const [varName] = genCssVar(rootPrefixCls, "wave");
  const [waveColor, setWaveColor] = reactExports.useState(null);
  const [borderRadius, setBorderRadius] = reactExports.useState([]);
  const [left, setLeft] = reactExports.useState(0);
  const [top, setTop] = reactExports.useState(0);
  const [width, setWidth] = reactExports.useState(0);
  const [height, setHeight] = reactExports.useState(0);
  const [enabled, setEnabled] = reactExports.useState(false);
  const waveStyle = {
    left,
    top,
    width,
    height,
    borderRadius: borderRadius.map((radius) => `${radius}px`).join(" ")
  };
  if (waveColor) {
    waveStyle[varName("color")] = waveColor;
  }
  function syncPos() {
    const nodeStyle = getComputedStyle(target);
    setWaveColor(getTargetWaveColor(target, colorSource));
    const isStatic = nodeStyle.position === "static";
    const {
      borderLeftWidth,
      borderTopWidth
    } = nodeStyle;
    setLeft(isStatic ? target.offsetLeft : validateNum(-Number.parseFloat(borderLeftWidth)));
    setTop(isStatic ? target.offsetTop : validateNum(-Number.parseFloat(borderTopWidth)));
    setWidth(target.offsetWidth);
    setHeight(target.offsetHeight);
    const {
      borderTopLeftRadius,
      borderTopRightRadius,
      borderBottomLeftRadius,
      borderBottomRightRadius
    } = nodeStyle;
    setBorderRadius([borderTopLeftRadius, borderTopRightRadius, borderBottomRightRadius, borderBottomLeftRadius].map((radius) => validateNum(Number.parseFloat(radius))));
  }
  reactExports.useEffect(() => {
    if (target) {
      const id = wrapperRaf(() => {
        syncPos();
        setEnabled(true);
      });
      let resizeObserver;
      if (typeof ResizeObserver !== "undefined") {
        resizeObserver = new ResizeObserver(syncPos);
        resizeObserver.observe(target);
      }
      return () => {
        wrapperRaf.cancel(id);
        resizeObserver?.disconnect();
      };
    }
  }, [target]);
  if (!enabled) {
    return null;
  }
  const isSmallComponent = (component === "Checkbox" || component === "Radio") && target?.classList.contains(TARGET_CLS);
  return /* @__PURE__ */ reactExports.createElement(CSSMotion, {
    visible: true,
    motionAppear: true,
    motionName: "wave-motion",
    motionDeadline: 5e3,
    onAppearEnd: (_, event) => {
      if (event.deadline || isTransitionEvent(event) && event.propertyName === "opacity") {
        const holder = divRef.current?.parentElement;
        unmount(holder).then(() => {
          holder?.remove();
        });
      }
      return false;
    }
  }, ({
    className: motionClassName
  }, ref) => /* @__PURE__ */ reactExports.createElement("div", {
    ref: composeRef(divRef, ref),
    className: clsx(className, motionClassName, {
      "wave-quick": isSmallComponent
    }),
    style: waveStyle
  }));
};
const showWaveEffect = (target, info) => {
  const {
    component
  } = info;
  if (component === "Checkbox" && !target.querySelector("input")?.checked) {
    return;
  }
  const holder = document.createElement("div");
  holder.style.position = "absolute";
  holder.style.left = "0px";
  holder.style.top = "0px";
  target?.insertBefore(holder, target?.firstChild);
  render(/* @__PURE__ */ reactExports.createElement(WaveEffect, {
    ...info,
    target
  }), holder);
};
const useWave = (nodeRef, className, component, colorSource) => {
  const {
    wave
  } = reactExports.useContext(ConfigContext);
  const [, token, hashId] = useToken();
  const showWave = useEvent((event) => {
    const node = nodeRef.current;
    if (wave?.disabled || !node) {
      return;
    }
    const targetNode = node.querySelector(`.${TARGET_CLS}`) || node;
    const {
      showEffect
    } = wave || {};
    (showEffect || showWaveEffect)(targetNode, {
      className,
      token,
      component,
      event,
      hashId,
      colorSource
    });
  });
  const rafIdRef = reactExports.useRef(null);
  reactExports.useEffect(() => () => {
    wrapperRaf.cancel(rafIdRef.current);
  }, []);
  const showDebounceWave = (event) => {
    wrapperRaf.cancel(rafIdRef.current);
    rafIdRef.current = wrapperRaf(() => {
      showWave(event);
    });
  };
  return showDebounceWave;
};
const TRIGGER_TYPE_TO_EVENT_MAP = {
  click: "click",
  mousedown: "mousedown",
  mouseup: "mouseup",
  pointerdown: "pointerdown",
  pointerup: "pointerup"
};
const Wave = (props) => {
  const {
    children,
    disabled,
    component,
    colorSource
  } = props;
  const {
    getPrefixCls,
    wave
  } = reactExports.useContext(ConfigContext);
  const containerRef = reactExports.useRef(null);
  const prefixCls = getPrefixCls("wave");
  const hashId = useStyle$2(prefixCls);
  const showWave = useWave(containerRef, clsx(prefixCls, hashId), component, colorSource);
  React.useEffect(() => {
    const node = containerRef.current;
    if (!node || node.nodeType !== window.Node.ELEMENT_NODE || disabled) {
      return;
    }
    const onClick = (e) => {
      if (!isVisible(e.target) || !node.getAttribute || node.getAttribute("disabled") || node.disabled || node.className.includes("disabled") && !node.className.includes("disabled:") || node.getAttribute("aria-disabled") === "true" || node.className.includes("-leave")) {
        return;
      }
      showWave(e);
    };
    const triggerType = wave?.triggerType;
    const eventName = triggerType && triggerType in TRIGGER_TYPE_TO_EVENT_MAP ? TRIGGER_TYPE_TO_EVENT_MAP[triggerType] : "click";
    node.addEventListener(eventName, onClick, true);
    return () => {
      node.removeEventListener(eventName, onClick, true);
    };
  }, [disabled, wave?.triggerType]);
  if (!/* @__PURE__ */ React.isValidElement(children)) {
    return children ?? null;
  }
  const ref = supportRef(children) ? composeRef(getNodeRef(children), containerRef) : containerRef;
  return cloneElement(children, {
    ref
  });
};
const genCollapseMotion = (token) => {
  const {
    componentCls,
    antCls,
    motionDurationMid,
    motionEaseInOut
  } = token;
  return {
    [componentCls]: {
      // For common/openAnimation
      [`${antCls}-motion-collapse-legacy`]: {
        overflow: "hidden",
        "&-active": {
          transition: `${["height", "opacity"].map((prop) => `${prop} ${motionDurationMid} ${motionEaseInOut}`).join(", ")} !important`
        }
      },
      [`${antCls}-motion-collapse`]: {
        overflow: "hidden",
        transition: `${["height", "opacity"].map((prop) => `${prop} ${motionDurationMid} ${motionEaseInOut}`).join(", ")} !important`
      }
    }
  };
};
const moveDownIn = new Keyframe("antMoveDownIn", {
  "0%": {
    transform: "translate3d(0, 100%, 0)",
    transformOrigin: "0 0",
    opacity: 0
  },
  "100%": {
    transform: "translate3d(0, 0, 0)",
    transformOrigin: "0 0",
    opacity: 1
  }
});
const moveDownOut = new Keyframe("antMoveDownOut", {
  "0%": {
    transform: "translate3d(0, 0, 0)",
    transformOrigin: "0 0",
    opacity: 1
  },
  "100%": {
    transform: "translate3d(0, 100%, 0)",
    transformOrigin: "0 0",
    opacity: 0
  }
});
const moveLeftIn = new Keyframe("antMoveLeftIn", {
  "0%": {
    transform: "translate3d(-100%, 0, 0)",
    transformOrigin: "0 0",
    opacity: 0
  },
  "100%": {
    transform: "translate3d(0, 0, 0)",
    transformOrigin: "0 0",
    opacity: 1
  }
});
const moveLeftOut = new Keyframe("antMoveLeftOut", {
  "0%": {
    transform: "translate3d(0, 0, 0)",
    transformOrigin: "0 0",
    opacity: 1
  },
  "100%": {
    transform: "translate3d(-100%, 0, 0)",
    transformOrigin: "0 0",
    opacity: 0
  }
});
const moveRightIn = new Keyframe("antMoveRightIn", {
  "0%": {
    transform: "translate3d(100%, 0, 0)",
    transformOrigin: "0 0",
    opacity: 0
  },
  "100%": {
    transform: "translate3d(0, 0, 0)",
    transformOrigin: "0 0",
    opacity: 1
  }
});
const moveRightOut = new Keyframe("antMoveRightOut", {
  "0%": {
    transform: "translate3d(0, 0, 0)",
    transformOrigin: "0 0",
    opacity: 1
  },
  "100%": {
    transform: "translate3d(100%, 0, 0)",
    transformOrigin: "0 0",
    opacity: 0
  }
});
const moveUpIn = new Keyframe("antMoveUpIn", {
  "0%": {
    transform: "translate3d(0, -100%, 0)",
    transformOrigin: "0 0",
    opacity: 0
  },
  "100%": {
    transform: "translate3d(0, 0, 0)",
    transformOrigin: "0 0",
    opacity: 1
  }
});
const moveUpOut = new Keyframe("antMoveUpOut", {
  "0%": {
    transform: "translate3d(0, 0, 0)",
    transformOrigin: "0 0",
    opacity: 1
  },
  "100%": {
    transform: "translate3d(0, -100%, 0)",
    transformOrigin: "0 0",
    opacity: 0
  }
});
const moveMotion = {
  "move-up": {
    inKeyframes: moveUpIn,
    outKeyframes: moveUpOut
  },
  "move-down": {
    inKeyframes: moveDownIn,
    outKeyframes: moveDownOut
  },
  "move-left": {
    inKeyframes: moveLeftIn,
    outKeyframes: moveLeftOut
  },
  "move-right": {
    inKeyframes: moveRightIn,
    outKeyframes: moveRightOut
  }
};
const initMoveMotion = (token, motionName) => {
  const {
    antCls
  } = token;
  const motionCls = `${antCls}-${motionName}`;
  const {
    inKeyframes,
    outKeyframes
  } = moveMotion[motionName];
  return [initMotion(motionCls, inKeyframes, outKeyframes, token.motionDurationMid), {
    [`
        ${motionCls}-enter,
        ${motionCls}-appear
      `]: {
      opacity: 0,
      animationTimingFunction: token.motionEaseOutCirc
    },
    [`${motionCls}-leave`]: {
      animationTimingFunction: token.motionEaseInOutCirc
    }
  }];
};
const genNoMotionStyle = () => {
  return {
    "@media (prefers-reduced-motion: reduce)": {
      "&, &::before, &::after": {
        transition: "none",
        animation: "none"
      }
    }
  };
};
const genNoMotionRawStyle = () => {
  return {
    "@media (prefers-reduced-motion: reduce)": {
      transition: "none",
      animation: "none"
    }
  };
};
const ColorBlock = ({
  color,
  prefixCls,
  className,
  style,
  innerClassName,
  innerStyle,
  onClick
}) => {
  const colorBlockCls = `${prefixCls}-color-block`;
  return /* @__PURE__ */ React.createElement("div", {
    className: clsx(colorBlockCls, className),
    style,
    onClick
  }, /* @__PURE__ */ React.createElement("div", {
    className: clsx(`${colorBlockCls}-inner`, innerClassName),
    style: {
      background: color,
      ...innerStyle
    }
  }));
};
const PanelContent = /* @__PURE__ */ React.forwardRef((props, ref) => {
  const {
    prefixCls,
    forceRender,
    className,
    style,
    children,
    isActive,
    role,
    classNames: customizeClassNames,
    styles
  } = props;
  const [rendered, setRendered] = React.useState(isActive || forceRender);
  React.useEffect(() => {
    if (forceRender || isActive) {
      setRendered(true);
    }
  }, [forceRender, isActive]);
  if (!rendered) {
    return null;
  }
  return /* @__PURE__ */ React.createElement("div", {
    ref,
    className: clsx(`${prefixCls}-panel`, {
      [`${prefixCls}-panel-active`]: isActive,
      [`${prefixCls}-panel-inactive`]: !isActive
    }, className),
    style,
    role
  }, /* @__PURE__ */ React.createElement("div", {
    className: clsx(`${prefixCls}-body`, customizeClassNames?.body),
    style: styles?.body
  }, children));
});
const CollapsePanel$1 = /* @__PURE__ */ React.forwardRef((props, ref) => {
  const {
    showArrow = true,
    headerClass,
    isActive,
    onItemClick,
    forceRender,
    className,
    classNames: customizeClassNames = {},
    styles = {},
    prefixCls,
    collapsible,
    accordion,
    panelKey,
    extra,
    header,
    expandIcon,
    openMotion,
    destroyOnHidden,
    children,
    ...resetProps
  } = props;
  const disabled = collapsible === "disabled";
  const ifExtraExist = extra !== null && extra !== void 0 && typeof extra !== "boolean";
  const collapsibleProps = {
    onClick: () => {
      onItemClick?.(panelKey);
    },
    onKeyDown: (e) => {
      if (e.key === "Enter" || e.keyCode === KeyCode.ENTER || e.which === KeyCode.ENTER) {
        onItemClick?.(panelKey);
      }
    },
    role: accordion ? "tab" : "button",
    ["aria-expanded"]: isActive,
    ["aria-disabled"]: disabled,
    tabIndex: disabled ? -1 : 0
  };
  const iconNodeInner = typeof expandIcon === "function" ? expandIcon(props) : /* @__PURE__ */ React.createElement("i", {
    className: "arrow"
  });
  const iconNode = iconNodeInner && /* @__PURE__ */ React.createElement("div", _extends$b({
    className: clsx(`${prefixCls}-expand-icon`, customizeClassNames?.icon),
    style: styles?.icon
  }, ["header", "icon"].includes(collapsible) ? collapsibleProps : {}), iconNodeInner);
  const collapsePanelClassNames = clsx(`${prefixCls}-item`, {
    [`${prefixCls}-item-active`]: isActive,
    [`${prefixCls}-item-disabled`]: disabled
  }, className);
  const headerClassName = clsx(headerClass, `${prefixCls}-header`, {
    [`${prefixCls}-collapsible-${collapsible}`]: !!collapsible
  }, customizeClassNames?.header);
  const headerProps = {
    className: headerClassName,
    style: styles?.header,
    ...["header", "icon"].includes(collapsible) ? {} : collapsibleProps
  };
  return /* @__PURE__ */ React.createElement("div", _extends$b({}, resetProps, {
    ref,
    className: collapsePanelClassNames
  }), /* @__PURE__ */ React.createElement("div", headerProps, showArrow && iconNode, /* @__PURE__ */ React.createElement("span", _extends$b({
    className: clsx(`${prefixCls}-title`, customizeClassNames?.title),
    style: styles?.title
  }, collapsible === "header" ? collapsibleProps : {}), header), ifExtraExist && /* @__PURE__ */ React.createElement("div", {
    className: `${prefixCls}-extra`
  }, extra)), /* @__PURE__ */ React.createElement(CSSMotion, _extends$b({
    visible: isActive,
    leavedClassName: `${prefixCls}-panel-hidden`
  }, openMotion, {
    forceRender,
    removeOnLeave: destroyOnHidden
  }), ({
    className: motionClassName,
    style: motionStyle
  }, motionRef) => {
    return /* @__PURE__ */ React.createElement(PanelContent, {
      ref: motionRef,
      prefixCls,
      className: motionClassName,
      classNames: customizeClassNames,
      style: motionStyle,
      styles,
      isActive,
      forceRender,
      role: accordion ? "tabpanel" : void 0
    }, children);
  }));
});
function mergeSemantic(src, tgt, mergeFn) {
  if (!src || !tgt) {
    return src || tgt;
  }
  const keys = Array.from(/* @__PURE__ */ new Set([...Object.keys(src), ...Object.keys(tgt)]));
  const result = {};
  keys.forEach((key) => {
    result[key] = mergeFn(src[key], tgt[key]);
  });
  return result;
}
function mergeSemanticClassNames(src, tgt) {
  return mergeSemantic(src, tgt, (a, b) => clsx(a, b));
}
function mergeSemanticStyles(src, tgt) {
  return mergeSemantic(src, tgt, (a, b) => ({
    ...a,
    ...b
  }));
}
const convertItemsToNodes = (items, props) => {
  const {
    prefixCls,
    accordion,
    collapsible,
    destroyOnHidden,
    onItemClick,
    activeKey,
    openMotion,
    expandIcon,
    classNames: collapseClassNames,
    styles: collapseStyles
  } = props;
  return items.map((item, index) => {
    const {
      children,
      label,
      key: rawKey,
      collapsible: rawCollapsible,
      onItemClick: rawOnItemClick,
      destroyOnHidden: rawDestroyOnHidden,
      classNames,
      styles,
      ...restProps
    } = item;
    const key = String(rawKey ?? index);
    const mergeCollapsible = rawCollapsible ?? collapsible;
    const mergedDestroyOnHidden = rawDestroyOnHidden ?? destroyOnHidden;
    const handleItemClick = (value) => {
      if (mergeCollapsible === "disabled") {
        return;
      }
      onItemClick(value);
      rawOnItemClick?.(value);
    };
    let isActive = false;
    if (accordion) {
      isActive = activeKey[0] === key;
    } else {
      isActive = activeKey.indexOf(key) > -1;
    }
    return /* @__PURE__ */ React.createElement(CollapsePanel$1, _extends$b({}, restProps, {
      classNames: mergeSemanticClassNames(collapseClassNames, classNames),
      styles: mergeSemanticStyles(collapseStyles, styles),
      prefixCls,
      key,
      panelKey: key,
      isActive,
      accordion,
      openMotion,
      expandIcon,
      header: label,
      collapsible: mergeCollapsible,
      onItemClick: handleItemClick,
      destroyOnHidden: mergedDestroyOnHidden
    }), children);
  });
};
const getNewChild = (child, index, props) => {
  if (!child) {
    return null;
  }
  const {
    prefixCls,
    accordion,
    collapsible,
    destroyOnHidden,
    onItemClick,
    activeKey,
    openMotion,
    expandIcon,
    classNames: collapseClassNames,
    styles
  } = props;
  const key = child.key || String(index);
  const {
    header,
    headerClass,
    destroyOnHidden: childDestroyOnHidden,
    collapsible: childCollapsible,
    onItemClick: childOnItemClick
  } = child.props;
  let isActive = false;
  if (accordion) {
    isActive = activeKey[0] === key;
  } else {
    isActive = activeKey.indexOf(key) > -1;
  }
  const mergeCollapsible = childCollapsible ?? collapsible;
  const handleItemClick = (value) => {
    if (mergeCollapsible === "disabled") {
      return;
    }
    onItemClick(value);
    childOnItemClick?.(value);
  };
  const childProps = {
    key,
    panelKey: key,
    header,
    headerClass,
    classNames: collapseClassNames,
    styles,
    isActive,
    prefixCls,
    destroyOnHidden: childDestroyOnHidden ?? destroyOnHidden,
    openMotion,
    accordion,
    children: child.props.children,
    onItemClick: handleItemClick,
    expandIcon,
    collapsible: mergeCollapsible
  };
  if (typeof child.type === "string") {
    return child;
  }
  Object.keys(childProps).forEach((propName) => {
    if (typeof childProps[propName] === "undefined") {
      delete childProps[propName];
    }
  });
  return /* @__PURE__ */ React.cloneElement(child, childProps);
};
function useItems(items, rawChildren, props) {
  if (Array.isArray(items)) {
    return convertItemsToNodes(items, props);
  }
  return toArray$1(rawChildren).map((child, index) => getNewChild(child, index, props));
}
function getActiveKeysArray(activeKey) {
  let currentActiveKey = activeKey;
  if (!Array.isArray(currentActiveKey)) {
    const activeKeyType = typeof currentActiveKey;
    currentActiveKey = activeKeyType === "number" || activeKeyType === "string" ? [currentActiveKey] : [];
  }
  return currentActiveKey.map((key) => String(key));
}
const Collapse$2 = /* @__PURE__ */ React.forwardRef((props, ref) => {
  const {
    prefixCls = "rc-collapse",
    destroyOnHidden = false,
    style,
    accordion,
    className,
    children,
    collapsible,
    openMotion,
    expandIcon,
    activeKey: rawActiveKey,
    defaultActiveKey,
    onChange,
    items,
    classNames: customizeClassNames,
    styles
  } = props;
  const collapseClassName = clsx(prefixCls, className);
  const [internalActiveKey, setActiveKey] = useControlledState(defaultActiveKey, rawActiveKey);
  const activeKey = getActiveKeysArray(internalActiveKey);
  const triggerActiveKey = useEvent((next) => {
    const nextKeys = getActiveKeysArray(next);
    setActiveKey(nextKeys);
    onChange?.(nextKeys);
  });
  const onItemClick = (key) => {
    if (accordion) {
      triggerActiveKey(activeKey[0] === key ? [] : [key]);
    } else {
      triggerActiveKey(activeKey.includes(key) ? activeKey.filter((item) => item !== key) : [...activeKey, key]);
    }
  };
  warningOnce(!children, "[rc-collapse] `children` will be removed in next major version. Please use `items` instead.");
  const mergedChildren = useItems(items, children, {
    prefixCls,
    accordion,
    openMotion,
    expandIcon,
    collapsible,
    destroyOnHidden,
    onItemClick,
    activeKey,
    classNames: customizeClassNames,
    styles
  });
  return /* @__PURE__ */ React.createElement("div", _extends$b({
    ref,
    className: collapseClassName,
    style,
    role: accordion ? "tablist" : void 0
  }, pickAttrs(props, {
    aria: true,
    data: true
  })), mergedChildren);
});
const Collapse$3 = Object.assign(Collapse$2, {
  /**
   * @deprecated use `items` instead, will be removed in `v4.0.0`
   */
  Panel: CollapsePanel$1
});
const {
  Panel
} = Collapse$3;
const CollapsePanel = /* @__PURE__ */ reactExports.forwardRef((props, ref) => {
  const {
    getPrefixCls
  } = reactExports.useContext(ConfigContext);
  const {
    prefixCls: customizePrefixCls,
    className,
    showArrow = true
  } = props;
  const prefixCls = getPrefixCls("collapse", customizePrefixCls);
  const collapsePanelClassName = clsx({
    [`${prefixCls}-no-arrow`]: !showArrow
  }, className);
  return /* @__PURE__ */ reactExports.createElement(Collapse$3.Panel, {
    ref,
    ...props,
    prefixCls,
    className: collapsePanelClassName
  });
});
const genBaseStyle$1 = (token) => {
  const {
    componentCls,
    contentBg,
    padding,
    headerBg,
    headerPadding,
    headerPaddingSM,
    headerPaddingLG,
    collapsePanelBorderRadius,
    lineWidth,
    lineType,
    colorBorder,
    colorText,
    colorTextHeading,
    colorTextDisabled,
    fontSizeLG,
    lineHeight,
    lineHeightLG,
    marginSM,
    paddingSM,
    paddingLG,
    paddingXS,
    motionDurationSlow,
    fontSizeIcon,
    contentPadding,
    contentPaddingSM,
    contentPaddingLG,
    fontHeight,
    fontHeightLG
  } = token;
  const borderBase = `${unit(lineWidth)} ${lineType} ${colorBorder}`;
  return {
    [componentCls]: {
      ...resetComponent(token),
      backgroundColor: headerBg,
      border: borderBase,
      borderRadius: collapsePanelBorderRadius,
      "&-rtl": {
        direction: "rtl"
      },
      [`& > ${componentCls}-item`]: {
        borderBottom: borderBase,
        "&:first-child": {
          [`
            &,
            & > ${componentCls}-header`]: {
            borderRadius: `${unit(collapsePanelBorderRadius)} ${unit(collapsePanelBorderRadius)} 0 0`
          }
        },
        "&:last-child": {
          [`
            &,
            & > ${componentCls}-header`]: {
            borderRadius: `0 0 ${unit(collapsePanelBorderRadius)} ${unit(collapsePanelBorderRadius)}`
          }
        },
        [`> ${componentCls}-header`]: {
          position: "relative",
          // Compatible with old version of antd, should remove in next version
          display: "flex",
          flexWrap: "nowrap",
          alignItems: "flex-start",
          padding: headerPadding,
          color: colorTextHeading,
          lineHeight,
          cursor: "pointer",
          transition: `all ${motionDurationSlow}, visibility 0s`,
          ...genFocusStyle(token),
          [`> ${componentCls}-title`]: {
            flex: "auto"
          },
          // >>>>> Arrow
          [`${componentCls}-expand-icon`]: {
            height: fontHeight,
            display: "flex",
            alignItems: "center",
            marginInlineEnd: marginSM
          },
          [`${componentCls}-arrow`]: {
            ...resetIcon(),
            fontSize: fontSizeIcon,
            // when `transform: rotate()` is applied to icon's root element
            transition: `transform ${motionDurationSlow}`,
            // when `transform: rotate()` is applied to icon's child element
            svg: {
              transition: `transform ${motionDurationSlow}`
            }
          },
          // >>>>> Text
          [`${componentCls}-title`]: {
            marginInlineEnd: "auto",
            // Icons from third-party libraries render as a bare `<svg>`, which the `.anticon`
            // reset never reaches. An `<svg>` has no baseline of its own, so it is aligned by its
            // bottom margin edge (CSS 2.1 §10.8.1) and rides above the title text.
            // `display: inline-block` keeps it an atomic inline box so `vertical-align` still applies
            // even under a CSS reset that forces `svg { display: block }` (e.g. Tailwind Preflight),
            // which would otherwise drop the icon onto its own line. `vertical-align: middle` centres
            // its margin box on the x-height line; `margin-block-end` then lifts it by half its own
            // value onto the cap-height centre (capHeight − xHeight ≈ 0.2em across typical fonts),
            // keeping it centred at any icon size.
            // Only matches a bare `<svg>`: an `.anticon` keeps its `<svg>` one level deeper.
            "> svg": {
              display: "inline-block",
              verticalAlign: "middle",
              marginBlockEnd: "0.2em"
            }
          }
        },
        [`${componentCls}-collapsible-header`]: {
          cursor: "default",
          [`${componentCls}-title`]: {
            flex: "none",
            cursor: "pointer"
          },
          [`${componentCls}-expand-icon`]: {
            cursor: "pointer"
          }
        },
        [`${componentCls}-collapsible-icon`]: {
          cursor: "unset",
          [`${componentCls}-expand-icon`]: {
            cursor: "pointer"
          }
        }
      },
      [`${componentCls}-panel`]: {
        color: colorText,
        backgroundColor: contentBg,
        borderTop: borderBase,
        [`& > ${componentCls}-body`]: {
          padding: contentPadding
        },
        "&-hidden": {
          display: "none"
        }
      },
      "&-small": {
        [`> ${componentCls}-item`]: {
          [`> ${componentCls}-header`]: {
            padding: headerPaddingSM,
            [`> ${componentCls}-expand-icon`]: {
              // Arrow offset
              marginInlineStart: token.calc(paddingSM).sub(paddingXS).equal()
            }
          },
          [`> ${componentCls}-panel > ${componentCls}-body`]: {
            padding: contentPaddingSM
          }
        }
      },
      "&-large": {
        [`> ${componentCls}-item`]: {
          fontSize: fontSizeLG,
          lineHeight: lineHeightLG,
          [`> ${componentCls}-header`]: {
            padding: headerPaddingLG,
            [`> ${componentCls}-expand-icon`]: {
              height: fontHeightLG,
              // Arrow offset
              marginInlineStart: token.calc(paddingLG).sub(padding).equal()
            }
          },
          [`> ${componentCls}-panel > ${componentCls}-body`]: {
            padding: contentPaddingLG
          }
        }
      },
      [`${componentCls}-item:last-child`]: {
        borderBottom: 0,
        [`> ${componentCls}-panel`]: {
          borderRadius: `0 0 ${unit(collapsePanelBorderRadius)} ${unit(collapsePanelBorderRadius)}`
        }
      },
      [`& ${componentCls}-item-disabled > ${componentCls}-header`]: {
        "&, & > .arrow": {
          color: colorTextDisabled,
          cursor: "not-allowed"
        }
      },
      // ========================== Icon Placement ==========================
      [`&${componentCls}-icon-placement-end`]: {
        [`& > ${componentCls}-item`]: {
          [`> ${componentCls}-header`]: {
            [`${componentCls}-expand-icon`]: {
              order: 1,
              marginInlineEnd: 0,
              marginInlineStart: marginSM
            }
          }
        }
      }
    }
  };
};
const genArrowStyle = (token) => {
  const {
    componentCls
  } = token;
  const fixedSelector = `> ${componentCls}-item > ${componentCls}-header ${componentCls}-arrow`;
  return {
    [`${componentCls}-rtl`]: {
      [fixedSelector]: {
        transform: `rotate(180deg)`
      }
    }
  };
};
const genBorderlessStyle = (token) => {
  const {
    componentCls,
    headerBg,
    borderlessContentPadding,
    borderlessContentBg,
    colorBorder
  } = token;
  return {
    [`${componentCls}-borderless`]: {
      backgroundColor: headerBg,
      border: 0,
      [`> ${componentCls}-item`]: {
        borderBottom: `${unit(token.lineWidth)} ${token.lineType} ${colorBorder}`
      },
      [`
        > ${componentCls}-item:last-child,
        > ${componentCls}-item:last-child ${componentCls}-header
      `]: {
        borderRadius: 0
      },
      [`> ${componentCls}-item:last-child`]: {
        borderBottom: 0
      },
      [`> ${componentCls}-item > ${componentCls}-panel`]: {
        backgroundColor: borderlessContentBg,
        borderTop: 0
      },
      [`> ${componentCls}-item > ${componentCls}-panel > ${componentCls}-body`]: {
        padding: borderlessContentPadding
      }
    }
  };
};
const genGhostStyle = (token) => {
  const {
    componentCls,
    paddingSM
  } = token;
  return {
    [`${componentCls}-ghost`]: {
      backgroundColor: "transparent",
      border: 0,
      [`> ${componentCls}-item`]: {
        borderBottom: 0,
        [`> ${componentCls}-panel`]: {
          backgroundColor: "transparent",
          border: 0,
          [`> ${componentCls}-body`]: {
            paddingBlock: paddingSM
          }
        }
      }
    }
  };
};
const prepareComponentToken$1 = (token) => {
  const componentToken = {
    headerPadding: `${unit(token.paddingSM)} ${unit(token.padding)}`,
    headerPaddingSM: `${unit(token.paddingXS)} ${unit(token.paddingSM)} ${unit(token.paddingXS)} ${unit(token.paddingXS)}`,
    headerPaddingLG: `${unit(token.padding)} ${unit(token.paddingLG)} ${unit(token.padding)} ${unit(token.padding)}`,
    headerBg: token.colorFillAlter,
    contentPadding: `${unit(token.padding)} ${unit(16)}`,
    // Fixed Value
    contentPaddingSM: token.paddingSM,
    contentPaddingLG: token.paddingLG,
    contentBg: token.colorBgContainer,
    borderlessContentPadding: `${unit(token.paddingXXS)} ${unit(16)} ${unit(token.padding)}`,
    borderlessContentBg: "transparent"
  };
  return componentToken;
};
const useStyle$1 = genStyleHooks("Collapse", (token) => {
  const collapseToken = merge(token, {
    collapsePanelBorderRadius: token.borderRadiusLG
  });
  return [genBaseStyle$1(collapseToken), genBorderlessStyle(collapseToken), genGhostStyle(collapseToken), genArrowStyle(collapseToken), genCollapseMotion(collapseToken)];
}, prepareComponentToken$1);
const Collapse = /* @__PURE__ */ reactExports.forwardRef((props, ref) => {
  const {
    getPrefixCls,
    direction,
    expandIcon: contextExpandIcon,
    className: contextClassName,
    style: contextStyle,
    classNames: contextClassNames,
    styles: contextStyles
  } = useComponentConfig("collapse");
  const {
    prefixCls: customizePrefixCls,
    className,
    rootClassName,
    style,
    bordered = true,
    ghost,
    size: customizeSize,
    expandIconPlacement,
    expandIconPosition,
    children,
    destroyInactivePanel,
    destroyOnHidden,
    expandIcon,
    classNames,
    styles
  } = props;
  const mergedSize = useSize((ctx) => customizeSize ?? ctx ?? "middle");
  const prefixCls = getPrefixCls("collapse", customizePrefixCls);
  const rootPrefixCls = getPrefixCls();
  const [hashId, cssVarCls] = useStyle$1(prefixCls);
  const mergedPlacement = expandIconPlacement ?? expandIconPosition ?? "start";
  const mergedProps = {
    ...props,
    size: mergedSize,
    bordered,
    expandIconPlacement: mergedPlacement
  };
  const contextStyleRoot = useSemanticRootStyle(contextStyle);
  const styleRoot = useSemanticRootStyle(style);
  const [mergedClassNames, mergedStyles] = useMergeSemantic([contextClassNames, classNames], [contextStyles, contextStyleRoot, styles, styleRoot], {
    props: mergedProps
  });
  const mergedExpandIcon = expandIcon ?? contextExpandIcon;
  const renderExpandIcon = reactExports.useCallback((panelProps = {}) => {
    const icon = isFunction(mergedExpandIcon) ? mergedExpandIcon(panelProps) : /* @__PURE__ */ reactExports.createElement(RefIcon$2, {
      rotate: panelProps.isActive ? direction === "rtl" ? -90 : 90 : void 0,
      "aria-label": panelProps.isActive ? "expanded" : "collapsed"
    });
    return cloneElement(icon, (oriProps) => ({
      className: clsx(oriProps.className, `${prefixCls}-arrow`)
    }));
  }, [mergedExpandIcon, prefixCls, direction]);
  const collapseClassName = clsx(`${prefixCls}-icon-placement-${mergedPlacement}`, {
    [`${prefixCls}-borderless`]: !bordered,
    [`${prefixCls}-rtl`]: direction === "rtl",
    [`${prefixCls}-ghost`]: !!ghost,
    [`${prefixCls}-large`]: mergedSize === "large",
    [`${prefixCls}-small`]: mergedSize === "small"
  }, contextClassName, className, rootClassName, hashId, cssVarCls, mergedClassNames.root);
  const openMotion = reactExports.useMemo(() => ({
    ...initCollapseMotion(rootPrefixCls),
    motionAppear: false,
    leavedClassName: `${prefixCls}-panel-hidden`
  }), [rootPrefixCls, prefixCls]);
  const items = reactExports.useMemo(() => {
    if (children) {
      return toArray$1(children).map((child) => child);
    }
    return null;
  }, [children]);
  return /* @__PURE__ */ reactExports.createElement(Collapse$3, {
    ref,
    openMotion,
    ...omit(props, ["rootClassName"]),
    expandIcon: renderExpandIcon,
    prefixCls,
    className: collapseClassName,
    style: mergedStyles.root,
    classNames: mergedClassNames,
    styles: mergedStyles,
    destroyOnHidden: destroyOnHidden ?? destroyInactivePanel
  }, items);
});
const Collapse$1 = Object.assign(Collapse, {
  Panel: CollapsePanel
});
const genPresetColor = (list) => list.map((value) => {
  value.colors = value.colors.map(generateColor);
  return value;
});
const isBright = (value, bgColorToken) => {
  const {
    r,
    g,
    b,
    a
  } = value.toRgb();
  const hsv = new Color(value.toRgbString()).onBackground(bgColorToken).toHsv();
  if (a <= 0.5) {
    return hsv.v > 0.5;
  }
  return r * 0.299 + g * 0.587 + b * 0.114 > 192;
};
const genCollapsePanelKey = (preset, index) => {
  const mergedKey = preset.key ?? index;
  return `panel-${mergedKey}`;
};
const ColorPresets = ({
  prefixCls,
  presets,
  value: color,
  onChange
}) => {
  const [locale] = useLocale("ColorPicker");
  const [, token] = useToken();
  const presetsValue = reactExports.useMemo(() => genPresetColor(presets), [presets]);
  const colorPresetsPrefixCls = `${prefixCls}-presets`;
  const activeKeys = reactExports.useMemo(() => presetsValue.reduce((acc, preset, index) => {
    const {
      defaultOpen = true
    } = preset;
    if (defaultOpen) {
      acc.push(genCollapsePanelKey(preset, index));
    }
    return acc;
  }, []), [presetsValue]);
  const handleClick = (colorValue) => {
    onChange?.(colorValue);
  };
  const items = presetsValue.map((preset, index) => ({
    key: genCollapsePanelKey(preset, index),
    label: /* @__PURE__ */ React.createElement("div", {
      className: `${colorPresetsPrefixCls}-label`
    }, preset?.label),
    children: /* @__PURE__ */ React.createElement("div", {
      className: `${colorPresetsPrefixCls}-items`
    }, Array.isArray(preset?.colors) && preset.colors?.length > 0 ? preset.colors.map((presetColor, index2) => {
      const colorInst = generateColor(presetColor);
      return /* @__PURE__ */ React.createElement(
        ColorBlock,
        {
          // eslint-disable-next-line react/no-array-index-key
          key: `preset-${index2}-${presetColor.toHexString()}`,
          color: colorInst.toCssString(),
          prefixCls,
          className: clsx(`${colorPresetsPrefixCls}-color`, {
            [`${colorPresetsPrefixCls}-color-checked`]: presetColor.toCssString() === color?.toCssString(),
            [`${colorPresetsPrefixCls}-color-bright`]: isBright(presetColor, token.colorBgElevated)
          }),
          onClick: () => handleClick(presetColor)
        }
      );
    }) : /* @__PURE__ */ React.createElement("span", {
      className: `${colorPresetsPrefixCls}-empty`
    }, locale.presetEmpty))
  }));
  return /* @__PURE__ */ React.createElement("div", {
    className: colorPresetsPrefixCls
  }, /* @__PURE__ */ React.createElement(Collapse$1, {
    defaultActiveKey: activeKeys,
    ghost: true,
    items
  }));
};
function withPureRenderTheme(Component) {
  return (props) => /* @__PURE__ */ reactExports.createElement(ConfigProvider, {
    theme: {
      token: {
        motion: false,
        zIndexPopupBase: 0
      }
    }
  }, /* @__PURE__ */ reactExports.createElement(Component, {
    ...props
  }));
}
const genPurePanel = (Component, alignPropName, postProps, defaultPrefixCls2, getDropdownCls) => {
  const PurePanel2 = (props) => {
    const {
      prefixCls: customizePrefixCls,
      style
    } = props;
    const holderRef = reactExports.useRef(null);
    const [popupHeight, setPopupHeight] = reactExports.useState(0);
    const [popupWidth, setPopupWidth] = reactExports.useState(0);
    const [open, setOpen] = useControlledState(false, props.open);
    const {
      getPrefixCls
    } = reactExports.useContext(ConfigContext);
    const prefixCls = getPrefixCls(defaultPrefixCls2 || "select", customizePrefixCls);
    reactExports.useEffect(() => {
      setOpen(true);
      if (typeof ResizeObserver !== "undefined") {
        const resizeObserver = new ResizeObserver((entries) => {
          const element = entries[0].target;
          setPopupHeight(element.offsetHeight + 8);
          setPopupWidth(element.offsetWidth);
        });
        const interval = setInterval(() => {
          const dropdownCls = getDropdownCls ? `.${getDropdownCls(prefixCls)}` : `.${prefixCls}-dropdown`;
          const popup = holderRef.current?.querySelector(dropdownCls);
          if (popup) {
            clearInterval(interval);
            resizeObserver.observe(popup);
          }
        }, 10);
        return () => {
          clearInterval(interval);
          resizeObserver.disconnect();
        };
      }
    }, [prefixCls]);
    let mergedProps = {
      ...props,
      style: {
        ...style,
        margin: 0
      },
      open,
      getPopupContainer: () => holderRef.current
    };
    if (postProps) {
      mergedProps = postProps(mergedProps);
    }
    if (alignPropName) {
      mergedProps = {
        ...mergedProps,
        [alignPropName]: {
          overflow: {
            adjustX: false,
            adjustY: false
          }
        }
      };
    }
    const mergedStyle = {
      paddingBottom: popupHeight,
      position: "relative",
      minWidth: popupWidth
    };
    return /* @__PURE__ */ reactExports.createElement("div", {
      ref: holderRef,
      style: mergedStyle
    }, /* @__PURE__ */ reactExports.createElement(Component, {
      ...mergedProps
    }));
  };
  return withPureRenderTheme(PurePanel2);
};
const useAllowClear = (prefixCls, displayValues, allowClear, clearIcon, disabled = false, mergedSearchValue, mode) => {
  const allowClearConfig = reactExports.useMemo(() => {
    if (typeof allowClear === "boolean") {
      return {
        allowClear
      };
    }
    if (allowClear && typeof allowClear === "object") {
      return allowClear;
    }
    return {
      allowClear: false
    };
  }, [allowClear]);
  return reactExports.useMemo(() => {
    const mergedAllowClear = !disabled && allowClearConfig.allowClear !== false && (displayValues.length || mergedSearchValue) && !(mode === "combobox" && mergedSearchValue === "");
    return {
      allowClear: mergedAllowClear,
      clearIcon: mergedAllowClear ? allowClearConfig.clearIcon || clearIcon || "×" : null,
      label: mergedAllowClear ? allowClearConfig.label ?? "Clear" : ""
    };
  }, [allowClearConfig, clearIcon, disabled, displayValues.length, mergedSearchValue, mode]);
};
const BaseSelectContext = /* @__PURE__ */ reactExports.createContext(null);
function useBaseProps() {
  return reactExports.useContext(BaseSelectContext);
}
function useLock(duration = 250) {
  const lockRef = reactExports.useRef(null);
  const timeoutRef = reactExports.useRef(null);
  reactExports.useEffect(() => () => {
    window.clearTimeout(timeoutRef.current);
  }, []);
  function doLock(locked) {
    if (locked || lockRef.current === null) {
      lockRef.current = locked;
    }
    window.clearTimeout(timeoutRef.current);
    timeoutRef.current = window.setTimeout(() => {
      lockRef.current = null;
    }, duration);
  }
  return [() => lockRef.current, doLock];
}
function isInside(elements, target) {
  return elements.filter((element) => element).some((element) => element.contains(target) || element === target);
}
function useSelectTriggerControl(elements, open, triggerOpen, customizedTrigger) {
  const onGlobalMouseDown = useEvent((event) => {
    if (customizedTrigger) {
      return;
    }
    let target = event.target;
    if (target.shadowRoot && event.composed) {
      target = event.composedPath()[0] || target;
    }
    if (event._ori_target) {
      target = event._ori_target;
    }
    if (open && // Marked by SelectInput mouseDown event
    !isInside(elements(), target)) {
      triggerOpen(false);
    }
  });
  reactExports.useEffect(() => {
    window.addEventListener("mousedown", onGlobalMouseDown);
    return () => window.removeEventListener("mousedown", onGlobalMouseDown);
  }, [onGlobalMouseDown]);
}
function _extends$a() {
  _extends$a = Object.assign ? Object.assign.bind() : function(target) {
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
  return _extends$a.apply(this, arguments);
}
const getBuiltInPlacements$1 = (popupMatchSelectWidth) => {
  const adjustX = popupMatchSelectWidth === true ? 0 : 1;
  return {
    bottomLeft: {
      points: ["tl", "bl"],
      offset: [0, 4],
      overflow: {
        adjustX,
        adjustY: 1
      },
      htmlRegion: "scroll"
    },
    bottomRight: {
      points: ["tr", "br"],
      offset: [0, 4],
      overflow: {
        adjustX,
        adjustY: 1
      },
      htmlRegion: "scroll"
    },
    topLeft: {
      points: ["bl", "tl"],
      offset: [0, -4],
      overflow: {
        adjustX,
        adjustY: 1
      },
      htmlRegion: "scroll"
    },
    topRight: {
      points: ["br", "tr"],
      offset: [0, -4],
      overflow: {
        adjustX,
        adjustY: 1
      },
      htmlRegion: "scroll"
    }
  };
};
const SelectTrigger = (props, ref) => {
  const {
    prefixCls,
    disabled,
    visible,
    children,
    popupElement,
    animation,
    transitionName,
    popupStyle,
    popupClassName,
    direction = "ltr",
    placement,
    builtinPlacements,
    popupMatchSelectWidth,
    popupRender,
    popupAlign,
    getPopupContainer,
    empty,
    onPopupVisibleChange,
    onPopupMouseEnter,
    onPopupMouseDown,
    onPopupBlur,
    ...restProps
  } = props;
  const popupPrefixCls = `${prefixCls}-dropdown`;
  let popupNode = popupElement;
  if (popupRender) {
    popupNode = popupRender(popupElement);
  }
  const mergedBuiltinPlacements2 = reactExports.useMemo(() => builtinPlacements || getBuiltInPlacements$1(popupMatchSelectWidth), [builtinPlacements, popupMatchSelectWidth]);
  const mergedTransitionName = animation ? `${popupPrefixCls}-${animation}` : transitionName;
  const isNumberPopupWidth = typeof popupMatchSelectWidth === "number";
  const stretch = reactExports.useMemo(() => {
    return popupMatchSelectWidth === false || isNumberPopupWidth ? "minWidth" : "width";
  }, [popupMatchSelectWidth, isNumberPopupWidth]);
  let mergedPopupStyle = popupStyle;
  if (isNumberPopupWidth) {
    mergedPopupStyle = {
      ...popupStyle,
      width: popupMatchSelectWidth
    };
  }
  const triggerPopupRef = reactExports.useRef(null);
  reactExports.useImperativeHandle(ref, () => ({
    getPopupElement: () => triggerPopupRef.current?.popupElement
  }));
  return /* @__PURE__ */ reactExports.createElement(Trigger, _extends$a({}, restProps, {
    showAction: onPopupVisibleChange ? ["click"] : [],
    hideAction: onPopupVisibleChange ? ["click"] : [],
    popupPlacement: placement || (direction === "rtl" ? "bottomRight" : "bottomLeft"),
    builtinPlacements: mergedBuiltinPlacements2,
    prefixCls: popupPrefixCls,
    popupMotion: {
      motionName: mergedTransitionName
    },
    popup: /* @__PURE__ */ reactExports.createElement("div", {
      onMouseEnter: onPopupMouseEnter,
      onMouseDown: onPopupMouseDown,
      onBlur: onPopupBlur
    }, popupNode),
    ref: triggerPopupRef,
    stretch,
    popupAlign,
    popupVisible: visible,
    getPopupContainer,
    popupClassName: clsx(popupClassName, {
      [`${popupPrefixCls}-empty`]: empty
    }),
    popupStyle: mergedPopupStyle,
    onPopupVisibleChange
  }), children);
};
const RefSelectTrigger = /* @__PURE__ */ reactExports.forwardRef(SelectTrigger);
function getKey(data, index) {
  const {
    key
  } = data;
  let value;
  if ("value" in data) {
    ({
      value
    } = data);
  }
  if (key !== null && key !== void 0) {
    return key;
  }
  if (value !== void 0) {
    return value;
  }
  return `rc-index-key-${index}`;
}
function isValidCount(value) {
  return typeof value !== "undefined" && !Number.isNaN(value);
}
function fillFieldNames(fieldNames, childrenAsData) {
  const {
    label,
    value,
    options,
    groupLabel
  } = fieldNames || {};
  const mergedLabel = label || (childrenAsData ? "children" : "label");
  return {
    label: mergedLabel,
    value: value || "value",
    options: options || "options",
    groupLabel: groupLabel || mergedLabel
  };
}
function flattenOptions(options, {
  fieldNames,
  childrenAsData
} = {}) {
  const flattenList = [];
  const {
    label: fieldLabel,
    value: fieldValue,
    options: fieldOptions,
    groupLabel
  } = fillFieldNames(fieldNames, false);
  function dig(list, isGroupOption) {
    if (!Array.isArray(list)) {
      return;
    }
    list.forEach((data) => {
      if (isGroupOption || !(fieldOptions in data)) {
        const value = data[fieldValue];
        flattenList.push({
          key: getKey(data, flattenList.length),
          groupOption: isGroupOption,
          data,
          label: data[fieldLabel],
          value
        });
      } else {
        let grpLabel = data[groupLabel];
        if (grpLabel === void 0 && childrenAsData) {
          grpLabel = data.label;
        }
        flattenList.push({
          key: getKey(data, flattenList.length),
          group: true,
          data,
          label: grpLabel
        });
        dig(data[fieldOptions], true);
      }
    });
  }
  dig(options, false);
  return flattenList;
}
function injectPropsWithOption(option) {
  const newOption = {
    ...option
  };
  if (!("props" in newOption)) {
    Object.defineProperty(newOption, "props", {
      get() {
        warningOnce(false, "Return type is option instead of Option instance. Please read value directly instead of reading from `props`.");
        return newOption;
      }
    });
  }
  return newOption;
}
const getSeparatedContent = (text, tokens, end) => {
  if (!tokens || !tokens.length) {
    return null;
  }
  let match = false;
  const separate = (str, [token, ...restTokens]) => {
    if (!token) {
      return [str];
    }
    const list2 = str.split(token);
    match = match || list2.length > 1;
    return list2.reduce((prevList, unitStr) => [...prevList, ...separate(unitStr, restTokens)], []).filter(Boolean);
  };
  const list = separate(text, tokens);
  if (match) {
    return typeof end !== "undefined" ? list.slice(0, end) : list;
  } else {
    return null;
  }
};
function Polite(props) {
  const {
    visible,
    values
  } = props;
  if (!visible) {
    return null;
  }
  const MAX_COUNT = 50;
  return /* @__PURE__ */ reactExports.createElement("span", {
    "aria-live": "polite",
    style: {
      width: 0,
      height: 0,
      position: "absolute",
      overflow: "hidden",
      opacity: 0
    }
  }, `${values.slice(0, MAX_COUNT).map(({
    label,
    value
  }) => ["number", "string"].includes(typeof label) ? label : value).join(", ")}`, values.length > MAX_COUNT ? ", ..." : null);
}
const internalMacroTask = (fn) => {
  const channel = new MessageChannel();
  channel.port1.onmessage = fn;
  channel.port2.postMessage(null);
};
const macroTask = (fn, times = 1) => {
  if (times <= 0) {
    fn();
    return;
  }
  internalMacroTask(() => {
    macroTask(fn, times - 1);
  });
};
function useOpen(defaultOpen, propOpen, onOpen, postOpen) {
  const [rendered, setRendered] = reactExports.useState(false);
  reactExports.useEffect(() => {
    setRendered(true);
  }, []);
  const [stateOpen, internalSetOpen] = useControlledState(defaultOpen, propOpen);
  const [lock, setLock] = reactExports.useState(false);
  const ssrSafeOpen = rendered ? stateOpen : false;
  const mergedOpen = postOpen(ssrSafeOpen);
  const taskIdRef = reactExports.useRef(0);
  const triggerEvent = useEvent((nextOpen) => {
    if (onOpen && mergedOpen !== nextOpen) {
      onOpen(nextOpen);
    }
    internalSetOpen(nextOpen);
  });
  const toggleOpen = useEvent((nextOpen, config = {}) => {
    const {
      cancelFun
    } = config;
    taskIdRef.current += 1;
    const id = taskIdRef.current;
    const nextOpenVal = typeof nextOpen === "boolean" ? nextOpen : !mergedOpen;
    setLock(!nextOpenVal);
    function triggerUpdate() {
      if (
        // Always check if id is match
        id === taskIdRef.current && // Check if need to cancel
        !cancelFun?.()
      ) {
        triggerEvent(nextOpenVal);
        setLock(false);
      }
    }
    if (nextOpenVal) {
      triggerUpdate();
    } else {
      macroTask(() => {
        triggerUpdate();
      });
    }
  });
  return [ssrSafeOpen, mergedOpen, toggleOpen, lock];
}
function Affix(props) {
  const {
    children,
    ...restProps
  } = props;
  if (!children) {
    return null;
  }
  return /* @__PURE__ */ reactExports.createElement("div", restProps, children);
}
const SelectInputContext = /* @__PURE__ */ reactExports.createContext(null);
function useSelectInputContext() {
  return reactExports.useContext(SelectInputContext);
}
const Input = /* @__PURE__ */ reactExports.forwardRef((props, ref) => {
  const {
    onChange,
    onKeyDown,
    onBlur,
    style,
    syncWidth,
    value,
    className,
    autoComplete,
    ...restProps
  } = props;
  const {
    prefixCls,
    mode,
    onSearch,
    onSearchSubmit,
    onInputBlur,
    autoFocus,
    tokenWithEnter,
    placeholder,
    components: {
      input: InputComponent = "input"
    }
  } = useSelectInputContext();
  const {
    id,
    classNames,
    styles,
    open,
    activeDescendantId,
    role,
    disabled
  } = useBaseProps() || {};
  const inputCls = clsx(`${prefixCls}-input`, classNames?.input, className);
  const compositionStatusRef = reactExports.useRef(false);
  const pastedTextRef = reactExports.useRef(null);
  const inputRef = reactExports.useRef(null);
  reactExports.useImperativeHandle(ref, () => inputRef.current);
  const handleChange = (event) => {
    let {
      value: nextVal
    } = event.target;
    if (tokenWithEnter && pastedTextRef.current && /[\r\n]/.test(pastedTextRef.current)) {
      const replacedText = pastedTextRef.current.replace(/[\r\n]+$/, "").replace(/\r\n/g, " ").replace(/[\r\n]/g, " ");
      nextVal = nextVal.replace(replacedText, pastedTextRef.current);
    }
    pastedTextRef.current = null;
    if (onSearch) {
      onSearch(nextVal, true, compositionStatusRef.current);
    }
    onChange?.(event);
  };
  const handleKeyDown = (event) => {
    const {
      key
    } = event;
    const {
      value: nextVal
    } = event.currentTarget;
    if (key === "Enter" && mode === "tags" && !open && !compositionStatusRef.current && onSearchSubmit) {
      onSearchSubmit(nextVal);
    }
    onKeyDown?.(event);
  };
  const handleBlur = (event) => {
    onInputBlur?.();
    onBlur?.(event);
  };
  const handleCompositionStart = () => {
    compositionStatusRef.current = true;
  };
  const handleCompositionEnd = (event) => {
    compositionStatusRef.current = false;
    if (mode !== "combobox") {
      const {
        value: nextVal
      } = event.currentTarget;
      onSearch?.(nextVal, true, false);
    }
  };
  const handlePaste = (event) => {
    const {
      clipboardData
    } = event;
    const pastedValue = clipboardData?.getData("text");
    pastedTextRef.current = pastedValue || "";
  };
  const [widthCssVar, setWidthCssVar] = reactExports.useState(void 0);
  useLayoutEffect(() => {
    const input = inputRef.current;
    if (syncWidth && input) {
      input.style.width = "0px";
      const scrollWidth = input.scrollWidth;
      setWidthCssVar(scrollWidth);
      input.style.width = "";
    }
  }, [syncWidth, value]);
  const sharedInputProps = {
    id,
    type: "text",
    ...restProps,
    ref: inputRef,
    style: {
      ...styles?.input,
      ...style,
      "--select-input-width": widthCssVar
    },
    autoFocus,
    autoComplete: autoComplete || "new-password",
    className: inputCls,
    disabled,
    value: value || "",
    onChange: handleChange,
    onKeyDown: handleKeyDown,
    onBlur: handleBlur,
    onPaste: handlePaste,
    onCompositionStart: handleCompositionStart,
    onCompositionEnd: handleCompositionEnd,
    // Accessibility attributes
    role: role || "combobox",
    "aria-expanded": open || false,
    "aria-haspopup": "listbox",
    "aria-owns": open ? `${id}_list` : void 0,
    "aria-autocomplete": "list",
    "aria-controls": open ? `${id}_list` : void 0,
    "aria-activedescendant": open ? activeDescendantId : void 0
  };
  if (/* @__PURE__ */ reactExports.isValidElement(InputComponent)) {
    const existingProps = InputComponent.props || {};
    const mergedProps = {
      placeholder: props.placeholder || placeholder,
      ...sharedInputProps,
      ...existingProps
    };
    Object.keys(existingProps).forEach((key) => {
      const existingValue = existingProps[key];
      if (typeof existingValue === "function") {
        mergedProps[key] = (...args) => {
          existingValue(...args);
          sharedInputProps[key]?.(...args);
        };
      }
    });
    mergedProps.ref = composeRef(InputComponent.ref, sharedInputProps.ref);
    return /* @__PURE__ */ reactExports.cloneElement(InputComponent, mergedProps);
  }
  const Component = InputComponent;
  return /* @__PURE__ */ reactExports.createElement(Component, sharedInputProps);
});
function Placeholder(props) {
  const {
    prefixCls,
    placeholder,
    displayValues
  } = useSelectInputContext();
  const {
    classNames,
    styles
  } = useBaseProps();
  const {
    show = true
  } = props;
  if (displayValues.length) {
    return null;
  }
  return /* @__PURE__ */ reactExports.createElement("div", {
    className: clsx(`${prefixCls}-placeholder`, classNames?.placeholder),
    style: {
      ...show ? {} : {
        visibility: "hidden"
      },
      ...styles?.placeholder
    }
  }, placeholder);
}
const SelectContext = /* @__PURE__ */ reactExports.createContext(null);
function toArray(value) {
  if (Array.isArray(value)) {
    return value;
  }
  return value !== void 0 ? [value] : [];
}
function hasValue(value) {
  return value !== void 0 && value !== null;
}
function isComboNoValue(value) {
  return !value && value !== 0;
}
function isTitleType$1(title) {
  return ["string", "number"].includes(typeof title);
}
function getTitle(item) {
  let title = void 0;
  if (item) {
    if (isTitleType$1(item.title)) {
      title = item.title.toString();
    } else if (isTitleType$1(item.label)) {
      title = item.label.toString();
    }
  }
  return title;
}
function _extends$9() {
  _extends$9 = Object.assign ? Object.assign.bind() : function(target) {
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
  return _extends$9.apply(this, arguments);
}
const SingleContent = /* @__PURE__ */ reactExports.forwardRef(({
  inputProps
}, ref) => {
  const {
    prefixCls,
    searchValue,
    activeValue,
    displayValues,
    maxLength,
    mode,
    components
  } = useSelectInputContext();
  const {
    triggerOpen,
    title: rootTitle,
    showSearch,
    classNames,
    styles
  } = useBaseProps();
  const selectContext = reactExports.useContext(SelectContext);
  const [inputChanged, setInputChanged] = reactExports.useState(false);
  const combobox = mode === "combobox";
  const displayValue = displayValues[0];
  const mergedSearchValue = reactExports.useMemo(() => {
    if (combobox && activeValue && !inputChanged && triggerOpen) {
      return activeValue;
    }
    return showSearch ? searchValue : "";
  }, [combobox, activeValue, inputChanged, triggerOpen, searchValue, showSearch]);
  const [optionClassName, optionStyle, optionTitle, hasOptionStyle] = reactExports.useMemo(() => {
    let className;
    let style;
    let titleValue;
    if (displayValue && selectContext?.flattenOptions) {
      const option = selectContext.flattenOptions.find((opt) => opt.value === displayValue.value);
      if (option?.data) {
        className = option.data.className;
        style = option.data.style;
        titleValue = getTitle(option.data);
      }
    }
    if (displayValue && !titleValue) {
      titleValue = getTitle(displayValue);
    }
    if (rootTitle !== void 0) {
      titleValue = rootTitle;
    }
    const nextHasStyle = !!className || !!style;
    return [className, style, titleValue, nextHasStyle];
  }, [displayValue, selectContext?.flattenOptions, rootTitle]);
  reactExports.useEffect(() => {
    if (combobox) {
      setInputChanged(false);
    }
  }, [combobox, activeValue]);
  const showHasValueCls = displayValue && displayValue.label !== null && displayValue.label !== void 0 && String(displayValue.label).trim() !== "";
  const shouldRenderValue = !(combobox && components?.input);
  const renderValue = shouldRenderValue ? displayValue ? hasOptionStyle ? /* @__PURE__ */ reactExports.createElement("div", {
    className: clsx(`${prefixCls}-content-value`, optionClassName),
    style: {
      ...mergedSearchValue ? {
        visibility: "hidden"
      } : {},
      ...optionStyle
    },
    title: optionTitle
  }, displayValue.label) : displayValue.label : /* @__PURE__ */ reactExports.createElement(Placeholder, {
    show: !mergedSearchValue
  }) : null;
  return /* @__PURE__ */ reactExports.createElement("div", {
    className: clsx(`${prefixCls}-content`, showHasValueCls && `${prefixCls}-content-has-value`, mergedSearchValue && `${prefixCls}-content-has-search-value`, hasOptionStyle && `${prefixCls}-content-has-option-style`, classNames?.content),
    style: styles?.content,
    title: hasOptionStyle ? void 0 : optionTitle
  }, renderValue, /* @__PURE__ */ reactExports.createElement(Input, _extends$9({
    ref
  }, inputProps, {
    value: mergedSearchValue,
    maxLength: mode === "combobox" ? maxLength : void 0,
    onChange: (e) => {
      setInputChanged(true);
      inputProps.onChange?.(e);
    }
  })));
});
const TransBtn = (props) => {
  const {
    className,
    style,
    customizeIcon,
    customizeIconProps,
    children,
    onMouseDown,
    onClick
  } = props;
  const icon = typeof customizeIcon === "function" ? customizeIcon(customizeIconProps) : customizeIcon;
  return /* @__PURE__ */ reactExports.createElement("span", {
    className,
    onMouseDown: (event) => {
      event.preventDefault();
      onMouseDown?.(event);
    },
    style: {
      userSelect: "none",
      WebkitUserSelect: "none",
      ...style
    },
    unselectable: "on",
    onClick,
    "aria-hidden": true
  }, icon !== void 0 ? icon : /* @__PURE__ */ reactExports.createElement("span", {
    className: clsx(className.split(/\s+/).map((cls) => `${cls}-icon`))
  }, children));
};
function _extends$8() {
  _extends$8 = Object.assign ? Object.assign.bind() : function(target) {
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
  return _extends$8.apply(this, arguments);
}
function itemKey(value) {
  return value.key ?? value.value;
}
const onPreventMouseDown = (event) => {
  event.preventDefault();
  event.stopPropagation();
};
const MultipleContent = /* @__PURE__ */ reactExports.forwardRef(function MultipleContent2({
  inputProps
}, ref) {
  const {
    prefixCls,
    displayValues,
    searchValue,
    mode,
    onSelectorRemove,
    removeIcon: removeIconFromContext
  } = useSelectInputContext();
  const {
    disabled,
    showSearch,
    triggerOpen,
    rawOpen,
    toggleOpen,
    autoClearSearchValue,
    tagRender: tagRenderFromContext,
    maxTagPlaceholder: maxTagPlaceholderFromContext,
    maxTagTextLength,
    maxTagCount,
    classNames,
    styles
  } = useBaseProps();
  const selectionItemPrefixCls = `${prefixCls}-selection-item`;
  let computedSearchValue = searchValue;
  if (!rawOpen && mode === "multiple" && autoClearSearchValue !== false) {
    computedSearchValue = "";
  }
  const inputValue = showSearch ? computedSearchValue || "" : "";
  const inputEditable = showSearch && !disabled;
  const removeIcon = removeIconFromContext ?? "×";
  const maxTagPlaceholder = maxTagPlaceholderFromContext ?? ((omittedValues) => `+ ${omittedValues.length} ...`);
  const tagRender = tagRenderFromContext;
  const onToggleOpen = (newOpen) => {
    toggleOpen(newOpen);
  };
  const onRemove = (value) => {
    onSelectorRemove?.(value);
  };
  const defaultRenderSelector = (item, content, itemDisabled, closable, onClose) => /* @__PURE__ */ reactExports.createElement("span", {
    title: getTitle(item),
    className: clsx(selectionItemPrefixCls, {
      [`${selectionItemPrefixCls}-disabled`]: itemDisabled
    }, classNames?.item),
    style: styles?.item
  }, /* @__PURE__ */ reactExports.createElement("span", {
    className: clsx(`${selectionItemPrefixCls}-content`, classNames?.itemContent),
    style: styles?.itemContent
  }, content), closable && /* @__PURE__ */ reactExports.createElement(TransBtn, {
    className: clsx(`${selectionItemPrefixCls}-remove`, classNames?.itemRemove),
    style: styles?.itemRemove,
    onMouseDown: onPreventMouseDown,
    onClick: onClose,
    customizeIcon: removeIcon
  }, "×"));
  const customizeRenderSelector = (value, content, itemDisabled, closable, onClose, isMaxTag, info) => {
    const onMouseDown = (e) => {
      onPreventMouseDown(e);
      onToggleOpen(!triggerOpen);
    };
    return /* @__PURE__ */ reactExports.createElement("span", {
      onMouseDown
    }, tagRender({
      label: content,
      value,
      index: info?.index,
      disabled: itemDisabled,
      closable,
      onClose,
      isMaxTag: !!isMaxTag
    }));
  };
  const renderItem = (valueItem, info) => {
    const {
      disabled: itemDisabled,
      label,
      value
    } = valueItem;
    const closable = !disabled && !itemDisabled;
    let displayLabel = label;
    if (typeof maxTagTextLength === "number") {
      if (typeof label === "string" || typeof label === "number") {
        const strLabel = String(displayLabel);
        if (strLabel.length > maxTagTextLength) {
          displayLabel = `${strLabel.slice(0, maxTagTextLength)}...`;
        }
      }
    }
    const onClose = (event) => {
      if (event) {
        event.stopPropagation();
      }
      onRemove(valueItem);
    };
    return typeof tagRender === "function" ? customizeRenderSelector(value, displayLabel, itemDisabled, closable, onClose, void 0, info) : defaultRenderSelector(valueItem, displayLabel, itemDisabled, closable, onClose);
  };
  const renderRest = (omittedValues) => {
    if (!displayValues.length) {
      return null;
    }
    const content = typeof maxTagPlaceholder === "function" ? maxTagPlaceholder(omittedValues) : maxTagPlaceholder;
    return typeof tagRender === "function" ? customizeRenderSelector(void 0, content, false, false, void 0, true) : defaultRenderSelector({
      title: content
    }, content, false);
  };
  return /* @__PURE__ */ reactExports.createElement(ForwardOverflow, {
    prefixCls: `${prefixCls}-content`,
    className: classNames?.content,
    style: styles?.content,
    prefix: !displayValues.length && !inputValue && /* @__PURE__ */ reactExports.createElement(Placeholder, null),
    data: displayValues,
    renderItem,
    renderRest,
    suffix: /* @__PURE__ */ reactExports.createElement(Input, _extends$8({
      ref,
      disabled,
      readOnly: !inputEditable
    }, inputProps, {
      value: inputValue || "",
      syncWidth: true
    })),
    itemKey,
    maxCount: maxTagCount
  });
});
const SelectContent = /* @__PURE__ */ reactExports.forwardRef(function SelectContent2(_, ref) {
  const {
    multiple,
    onInputKeyDown,
    tabIndex
  } = useSelectInputContext();
  const baseProps = useBaseProps();
  const {
    showSearch
  } = baseProps;
  const ariaProps = pickAttrs(baseProps, {
    aria: true
  });
  const sharedInputProps = {
    ...ariaProps,
    onKeyDown: onInputKeyDown,
    readOnly: !showSearch,
    tabIndex
  };
  if (multiple) {
    return /* @__PURE__ */ reactExports.createElement(MultipleContent, {
      ref,
      inputProps: sharedInputProps
    });
  }
  return /* @__PURE__ */ reactExports.createElement(SingleContent, {
    ref,
    inputProps: sharedInputProps
  });
});
function isValidateOpenKey(currentKeyCode) {
  return (
    // Undefined for Edge bug:
    // https://github.com/ant-design/ant-design/issues/51292
    currentKeyCode && // Other keys
    ![
      // System function button
      KeyCode.ESC,
      KeyCode.SHIFT,
      KeyCode.BACKSPACE,
      KeyCode.TAB,
      KeyCode.WIN_KEY,
      KeyCode.ALT,
      KeyCode.META,
      KeyCode.WIN_KEY_RIGHT,
      KeyCode.CTRL,
      KeyCode.SEMICOLON,
      KeyCode.EQUALS,
      KeyCode.CAPS_LOCK,
      KeyCode.CONTEXT_MENU,
      // Arrow keys - should not trigger open when navigating in input
      KeyCode.UP,
      // KeyCode.DOWN,
      KeyCode.LEFT,
      KeyCode.RIGHT,
      // F1-F12
      KeyCode.F1,
      KeyCode.F2,
      KeyCode.F3,
      KeyCode.F4,
      KeyCode.F5,
      KeyCode.F6,
      KeyCode.F7,
      KeyCode.F8,
      KeyCode.F9,
      KeyCode.F10,
      KeyCode.F11,
      KeyCode.F12
    ].includes(currentKeyCode)
  );
}
function _extends$7() {
  _extends$7 = Object.assign ? Object.assign.bind() : function(target) {
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
  return _extends$7.apply(this, arguments);
}
const DEFAULT_OMIT_PROPS = ["value", "onChange", "removeIcon", "placeholder", "maxTagCount", "maxTagTextLength", "maxTagPlaceholder", "choiceTransitionName", "onInputKeyDown", "onPopupScroll", "tabIndex", "activeValue", "onSelectorRemove", "focused"];
const SelectInput = /* @__PURE__ */ reactExports.forwardRef(function SelectInput2(props, ref) {
  const {
    // Style
    prefixCls,
    className,
    style,
    // UI
    prefix,
    suffix,
    clearIcon,
    clearLabel,
    children,
    // Data
    multiple,
    displayValues,
    placeholder,
    mode,
    // Search
    searchValue,
    onSearch,
    onSearchSubmit,
    onInputBlur,
    // Input
    maxLength,
    autoFocus,
    // Events
    onMouseDown,
    onClearMouseDown,
    onInputKeyDown,
    onSelectorRemove,
    // Token handling
    tokenWithEnter,
    // Components
    components,
    ...restProps
  } = props;
  const {
    triggerOpen,
    toggleOpen,
    showSearch,
    disabled,
    loading,
    classNames,
    styles
  } = useBaseProps();
  const rootRef = reactExports.useRef(null);
  const inputRef = reactExports.useRef(null);
  const onInternalInputKeyDown = useEvent((event) => {
    const {
      which
    } = event;
    const isTextAreaElement = inputRef.current instanceof HTMLTextAreaElement;
    if (!isTextAreaElement && triggerOpen && (which === KeyCode.UP || which === KeyCode.DOWN)) {
      event.preventDefault();
    }
    if (onInputKeyDown) {
      onInputKeyDown(event);
    }
    if (isTextAreaElement && !triggerOpen && ~[KeyCode.UP, KeyCode.DOWN, KeyCode.LEFT, KeyCode.RIGHT].indexOf(which)) {
      return;
    }
    const isModifier = event.ctrlKey || event.altKey || event.metaKey;
    if (!isModifier && isValidateOpenKey(which)) {
      toggleOpen(true);
    }
  });
  reactExports.useImperativeHandle(ref, () => {
    return {
      focus: (options) => {
        (inputRef.current || rootRef.current).focus?.(options);
      },
      blur: () => {
        (inputRef.current || rootRef.current).blur?.();
      },
      // Use getDOM to handle nested nativeElement structure (e.g., when RootComponent is antd Input)
      nativeElement: getDOM(rootRef.current)
    };
  });
  const onInternalMouseDown = useEvent((event) => {
    if (!disabled) {
      const inputDOM = getDOM(inputRef.current);
      event.nativeEvent._ori_target = inputDOM;
      const isClickOnInput = inputDOM === event.target || inputDOM?.contains(event.target);
      if (inputDOM && !isClickOnInput) {
        event.preventDefault();
      }
      const shouldPreventCloseOnSingle = triggerOpen && !multiple && (mode === "combobox" || showSearch);
      const shouldPreventCloseOnMultipleInput = triggerOpen && multiple && isClickOnInput;
      const shouldPreventClose = shouldPreventCloseOnSingle || shouldPreventCloseOnMultipleInput;
      if (!event.nativeEvent._select_lazy) {
        inputRef.current?.focus();
        if (!shouldPreventClose) {
          toggleOpen();
        }
      } else if (triggerOpen && !multiple) {
        toggleOpen(false);
      }
    }
    onMouseDown?.(event);
  });
  const {
    root: RootComponent
  } = components;
  const domProps = omit(restProps, DEFAULT_OMIT_PROPS);
  const ariaProps = pickAttrs(domProps, {
    aria: true
  });
  const ariaKeys = Object.keys(ariaProps);
  const contextValue = {
    ...props,
    onInputKeyDown: onInternalInputKeyDown
  };
  if (RootComponent) {
    const originProps = RootComponent.props || {};
    const mergedProps = {
      ...originProps,
      ...domProps
    };
    Object.keys(originProps).forEach((key) => {
      const originVal = originProps[key];
      const domVal = domProps[key];
      if (typeof originVal === "function" && typeof domVal === "function") {
        mergedProps[key] = (...args) => {
          domVal(...args);
          originVal(...args);
        };
      }
    });
    if (/* @__PURE__ */ reactExports.isValidElement(RootComponent)) {
      return /* @__PURE__ */ reactExports.cloneElement(RootComponent, {
        ...mergedProps,
        ref: composeRef(RootComponent.ref, rootRef)
      });
    }
    return /* @__PURE__ */ reactExports.createElement(RootComponent, _extends$7({}, mergedProps, {
      ref: rootRef
    }));
  }
  return /* @__PURE__ */ reactExports.createElement(SelectInputContext.Provider, {
    value: contextValue
  }, /* @__PURE__ */ reactExports.createElement("div", _extends$7({}, omit(domProps, ariaKeys), {
    // Style
    ref: rootRef,
    className,
    style,
    onMouseDown: onInternalMouseDown
  }), /* @__PURE__ */ reactExports.createElement(Affix, {
    className: clsx(`${prefixCls}-prefix`, classNames?.prefix),
    style: styles?.prefix
  }, prefix), /* @__PURE__ */ reactExports.createElement(SelectContent, {
    ref: inputRef
  }), /* @__PURE__ */ reactExports.createElement(Affix, {
    className: clsx(`${prefixCls}-suffix`, {
      [`${prefixCls}-suffix-loading`]: loading
    }, classNames?.suffix),
    style: styles?.suffix
  }, suffix), clearIcon && /* @__PURE__ */ reactExports.createElement("button", {
    type: "button",
    "aria-label": clearLabel,
    className: clsx(`${prefixCls}-clear`, classNames?.clear),
    style: styles?.clear,
    onMouseDown: (e) => {
      e.preventDefault();
      e.nativeEvent._select_lazy = true;
    },
    onClick: onClearMouseDown
  }, clearIcon), children));
});
function useComponents(components, getInputElement, getRawInputElement) {
  return reactExports.useMemo(() => {
    let {
      root,
      input
    } = components || {};
    if (getRawInputElement) {
      root = getRawInputElement();
    }
    if (getInputElement) {
      input = getInputElement();
    }
    return {
      root,
      input
    };
  }, [components, getInputElement, getRawInputElement]);
}
function _extends$6() {
  _extends$6 = Object.assign ? Object.assign.bind() : function(target) {
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
  return _extends$6.apply(this, arguments);
}
const isMultiple = (mode) => mode === "tags" || mode === "multiple";
const BaseSelect = /* @__PURE__ */ reactExports.forwardRef((props, ref) => {
  const {
    id,
    prefixCls,
    className,
    styles,
    classNames,
    showSearch,
    tagRender,
    showScrollBar = "optional",
    direction,
    omitDomProps,
    // Value
    displayValues,
    onDisplayValuesChange,
    emptyOptions,
    notFoundContent = "Not Found",
    onClear,
    maxCount,
    placeholder,
    // Mode
    mode,
    // Status
    disabled,
    loading,
    // Customize Input
    getInputElement,
    getRawInputElement,
    // Open
    open,
    defaultOpen,
    onPopupVisibleChange,
    // Active
    activeValue,
    onActiveValueChange,
    activeDescendantId,
    // Search
    searchValue,
    autoClearSearchValue,
    onSearch,
    onSearchSplit,
    tokenSeparators,
    // Icons
    allowClear,
    prefix,
    suffix,
    suffixIcon,
    clearIcon,
    // Dropdown
    OptionList: OptionList2,
    animation,
    transitionName,
    popupStyle,
    popupClassName,
    popupMatchSelectWidth,
    popupRender,
    popupAlign,
    placement,
    builtinPlacements,
    getPopupContainer,
    // Focus
    showAction = [],
    onFocus,
    onBlur,
    // Rest Events
    onKeyUp,
    onKeyDown,
    onMouseDown,
    // Components
    components,
    // Rest Props
    ...restProps
  } = props;
  const multiple = isMultiple(mode);
  const containerRef = reactExports.useRef(null);
  const triggerRef = reactExports.useRef(null);
  const listRef = reactExports.useRef(null);
  const [focused, setFocused] = reactExports.useState(false);
  reactExports.useImperativeHandle(ref, () => ({
    focus: containerRef.current?.focus,
    blur: containerRef.current?.blur,
    scrollTo: (arg) => listRef.current?.scrollTo(arg),
    nativeElement: getDOM(containerRef.current)
  }));
  const mergedComponents = useComponents(components, getInputElement, getRawInputElement);
  const mergedSearchValue = reactExports.useMemo(() => {
    if (mode !== "combobox") {
      return searchValue;
    }
    const val = displayValues[0]?.value;
    return typeof val === "string" || typeof val === "number" ? String(val) : "";
  }, [searchValue, mode, displayValues]);
  const customizeInputElement = mode === "combobox" && typeof getInputElement === "function" && getInputElement() || null;
  const emptyListContent = !notFoundContent && emptyOptions;
  const [rawOpen, mergedOpen, triggerOpen, lockOptions] = useOpen(defaultOpen || false, open, onPopupVisibleChange, (nextOpen) => disabled || emptyListContent ? false : nextOpen);
  const tokenWithEnter = reactExports.useMemo(() => typeof tokenSeparators === "function" || (tokenSeparators || []).some((tokenSeparator) => ["\n", "\r\n"].includes(tokenSeparator)), [tokenSeparators]);
  const splitByTokenSeparators = reactExports.useMemo(() => {
    if (typeof tokenSeparators === "function") {
      return (input, end) => {
        const tokens = tokenSeparators(input);
        const isUnchanged = Array.isArray(tokens) && tokens.length === 1 && tokens[0] === input;
        if (!Array.isArray(tokens) || !tokens.length || isUnchanged) {
          return null;
        }
        return typeof end !== "undefined" ? tokens.slice(0, end) : tokens;
      };
    }
    return (input, end) => getSeparatedContent(input, tokenSeparators, end);
  }, [tokenSeparators]);
  const onInternalSearch = (searchText, fromTyping, isCompositing) => {
    if (multiple && isValidCount(maxCount) && displayValues.length >= maxCount) {
      return;
    }
    let ret = true;
    let newSearchText = searchText;
    onActiveValueChange?.(null);
    const cap = isValidCount(maxCount) ? maxCount - displayValues.length : void 0;
    const patchLabels = isCompositing ? null : splitByTokenSeparators(searchText, cap);
    if (mode !== "combobox" && patchLabels) {
      newSearchText = "";
      onSearchSplit?.(patchLabels);
      triggerOpen(false);
      ret = false;
    }
    if (onSearch && mergedSearchValue !== newSearchText) {
      onSearch(newSearchText, {
        source: fromTyping ? "typing" : "effect"
      });
    }
    if (searchText && fromTyping && ret) {
      triggerOpen(true);
    }
    return ret;
  };
  const onInternalSearchSubmit = (searchText) => {
    if (!searchText || !searchText.trim()) {
      return;
    }
    onSearch(searchText, {
      source: "submit"
    });
  };
  reactExports.useEffect(() => {
    if (!rawOpen && !multiple && mode !== "combobox") {
      onInternalSearch("", false, false);
    }
  }, [rawOpen]);
  reactExports.useEffect(() => {
    if (disabled) {
      triggerOpen(false);
      setFocused(false);
    }
  }, [disabled, mergedOpen]);
  const [getClearLock, setClearLock] = useLock();
  const keyLockRef = reactExports.useRef(false);
  const onInternalKeyDown = (event) => {
    const clearLock = getClearLock();
    const {
      key
    } = event;
    const isEnterKey = key === "Enter";
    const isSpaceKey = key === " ";
    if (isEnterKey || isSpaceKey) {
      const isCombobox = mode === "combobox";
      const isEditable = isCombobox || showSearch;
      if (isSpaceKey && !isEditable || isEnterKey && !isCombobox) {
        event.preventDefault();
      }
      if (!mergedOpen) {
        triggerOpen(true);
      }
    }
    setClearLock(!!mergedSearchValue);
    if (key === "Backspace" && !clearLock && multiple && !mergedSearchValue && displayValues.length) {
      const cloneDisplayValues = [...displayValues];
      let removedDisplayValue = null;
      for (let i = cloneDisplayValues.length - 1; i >= 0; i -= 1) {
        const current = cloneDisplayValues[i];
        if (!current.disabled) {
          cloneDisplayValues.splice(i, 1);
          removedDisplayValue = current;
          break;
        }
      }
      if (removedDisplayValue) {
        onDisplayValuesChange(cloneDisplayValues, {
          type: "remove",
          values: [removedDisplayValue]
        });
      }
    }
    if (mergedOpen && (!isEnterKey || !keyLockRef.current) && !isSpaceKey) {
      if (isEnterKey) {
        keyLockRef.current = true;
      }
      listRef.current?.onKeyDown(event);
    }
    onKeyDown?.(event);
  };
  const onInternalKeyUp = (event, ...rest) => {
    if (mergedOpen) {
      listRef.current?.onKeyUp(event, ...rest);
    }
    if (event.key === "Enter") {
      keyLockRef.current = false;
    }
    onKeyUp?.(event, ...rest);
  };
  const onSelectorRemove = useEvent((val) => {
    const newValues = displayValues.filter((i) => i !== val);
    onDisplayValuesChange(newValues, {
      type: "remove",
      values: [val]
    });
  });
  const onInputBlur = () => {
    keyLockRef.current = false;
  };
  const getSelectElements = () => [getDOM(containerRef.current), triggerRef.current?.getPopupElement()];
  useSelectTriggerControl(getSelectElements, mergedOpen, triggerOpen, !!mergedComponents.root);
  const internalMouseDownRef = reactExports.useRef(false);
  const onInternalFocus = (event) => {
    setFocused(true);
    if (!disabled) {
      if (showAction.includes("focus")) {
        triggerOpen(true);
      }
      onFocus?.(event);
    }
  };
  const onRootBlur = () => {
    if (mergedOpen && !internalMouseDownRef.current) {
      triggerOpen(false, {
        cancelFun: () => isInside(getSelectElements(), document.activeElement)
      });
    }
  };
  const onInternalBlur = (event) => {
    setFocused(false);
    if (mergedSearchValue) {
      if (mode === "tags") {
        onSearch(mergedSearchValue, {
          source: "submit"
        });
      } else if (mode === "multiple") {
        onSearch("", {
          source: "blur"
        });
      }
    }
    onRootBlur();
    if (!disabled) {
      onBlur?.(event);
    }
  };
  const onRootMouseDown = (event, ...restArgs) => {
    const {
      target
    } = event;
    const popupElement = triggerRef.current?.getPopupElement();
    if (popupElement?.contains(target) && triggerOpen) {
      triggerOpen(true);
    }
    onMouseDown?.(event, ...restArgs);
    internalMouseDownRef.current = true;
    macroTask(() => {
      internalMouseDownRef.current = false;
    });
  };
  const [, forceUpdate] = reactExports.useState({});
  function onPopupMouseEnter() {
    forceUpdate({});
  }
  let onTriggerVisibleChange;
  if (!!mergedComponents.root) {
    onTriggerVisibleChange = (newOpen) => {
      triggerOpen(newOpen);
    };
  }
  const baseSelectContext = reactExports.useMemo(() => ({
    ...props,
    notFoundContent,
    open: mergedOpen,
    triggerOpen: mergedOpen,
    rawOpen,
    id,
    showSearch,
    multiple,
    toggleOpen: triggerOpen,
    showScrollBar,
    styles,
    classNames,
    lockOptions
  }), [props, notFoundContent, triggerOpen, id, showSearch, multiple, mergedOpen, rawOpen, showScrollBar, styles, classNames, lockOptions]);
  const mergedSuffixIcon = reactExports.useMemo(() => {
    const nextSuffix = suffix ?? suffixIcon;
    if (typeof nextSuffix === "function") {
      return nextSuffix({
        searchValue: mergedSearchValue,
        open: mergedOpen,
        focused,
        showSearch,
        loading
      });
    }
    return nextSuffix;
  }, [suffix, suffixIcon, mergedSearchValue, mergedOpen, focused, showSearch, loading]);
  const onClearMouseDown = () => {
    onClear?.();
    containerRef.current?.focus();
    onDisplayValuesChange([], {
      type: "clear",
      values: displayValues
    });
    onInternalSearch("", false, false);
  };
  const {
    allowClear: mergedAllowClear,
    clearIcon: clearNode,
    label: clearLabel
  } = useAllowClear(prefixCls, displayValues, allowClear, clearIcon, disabled, mergedSearchValue, mode);
  const optionList = /* @__PURE__ */ reactExports.createElement(OptionList2, {
    ref: listRef
  });
  const mergedClassName = clsx(prefixCls, className, {
    [`${prefixCls}-focused`]: focused,
    [`${prefixCls}-multiple`]: multiple,
    [`${prefixCls}-single`]: !multiple,
    [`${prefixCls}-allow-clear`]: mergedAllowClear,
    [`${prefixCls}-show-arrow`]: mergedSuffixIcon !== void 0 && mergedSuffixIcon !== null,
    [`${prefixCls}-disabled`]: disabled,
    [`${prefixCls}-loading`]: loading,
    [`${prefixCls}-open`]: mergedOpen,
    [`${prefixCls}-customize-input`]: customizeInputElement,
    [`${prefixCls}-show-search`]: showSearch
  });
  let renderNode = /* @__PURE__ */ reactExports.createElement(SelectInput, _extends$6({}, restProps, {
    // Ref
    ref: containerRef,
    prefixCls,
    className: mergedClassName,
    focused,
    prefix,
    suffix: mergedSuffixIcon,
    clearIcon: clearNode,
    clearLabel,
    multiple,
    mode,
    displayValues,
    placeholder,
    searchValue: mergedSearchValue,
    activeValue,
    onSearch: onInternalSearch,
    onSearchSubmit: onInternalSearchSubmit,
    onInputBlur,
    onFocus: onInternalFocus,
    onBlur: onInternalBlur,
    onClearMouseDown,
    onKeyDown: onInternalKeyDown,
    onKeyUp: onInternalKeyUp,
    onSelectorRemove,
    tokenWithEnter,
    onMouseDown: onRootMouseDown,
    components: mergedComponents
  }));
  renderNode = /* @__PURE__ */ reactExports.createElement(RefSelectTrigger, {
    ref: triggerRef,
    disabled,
    prefixCls,
    visible: mergedOpen,
    popupElement: optionList,
    animation,
    transitionName,
    popupStyle,
    popupClassName,
    direction,
    popupMatchSelectWidth,
    popupRender,
    popupAlign,
    placement,
    builtinPlacements,
    getPopupContainer,
    empty: emptyOptions,
    onPopupVisibleChange: onTriggerVisibleChange,
    onPopupMouseEnter,
    onPopupMouseDown: onRootMouseDown,
    onPopupBlur: onRootBlur
  }, renderNode);
  return /* @__PURE__ */ reactExports.createElement(BaseSelectContext.Provider, {
    value: baseSelectContext
  }, /* @__PURE__ */ reactExports.createElement(Polite, {
    visible: focused && !mergedOpen,
    values: displayValues
  }), renderNode);
});
const OptGroup = () => null;
OptGroup.isSelectOptGroup = true;
const Option = () => null;
Option.isSelectOption = true;
function _extends$5() {
  _extends$5 = Object.assign ? Object.assign.bind() : function(target) {
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
  return _extends$5.apply(this, arguments);
}
const Filler = /* @__PURE__ */ reactExports.forwardRef(({
  height,
  offsetY,
  offsetX,
  children,
  prefixCls,
  onInnerResize,
  innerProps,
  rtl,
  extra
}, ref) => {
  let outerStyle = {};
  let innerStyle = {
    display: "flex",
    flexDirection: "column"
  };
  if (offsetY !== void 0) {
    outerStyle = {
      height,
      position: "relative",
      overflow: "hidden"
    };
    innerStyle = {
      ...innerStyle,
      transform: `translateY(${offsetY}px)`,
      [rtl ? "marginRight" : "marginLeft"]: -offsetX,
      position: "absolute",
      left: 0,
      right: 0,
      top: 0
    };
  }
  return /* @__PURE__ */ reactExports.createElement("div", {
    style: outerStyle
  }, /* @__PURE__ */ reactExports.createElement(RefResizeObserver, {
    onResize: ({
      offsetHeight
    }) => {
      if (offsetHeight && onInnerResize) {
        onInnerResize();
      }
    }
  }, /* @__PURE__ */ reactExports.createElement("div", _extends$5({
    style: innerStyle,
    className: clsx({
      [`${prefixCls}-holder-inner`]: prefixCls
    }),
    ref
  }, innerProps), children, extra)));
});
Filler.displayName = "Filler";
function Item({
  children,
  setRef
}) {
  const refFunc = reactExports.useCallback((node) => {
    setRef(node);
  }, [setRef]);
  return /* @__PURE__ */ reactExports.cloneElement(children, {
    ref: refFunc
  });
}
function useChildren(list, startIndex, endIndex, scrollWidth, offsetX, setNodeRef, renderFunc, {
  getKey: getKey2
}) {
  return list.slice(startIndex, endIndex + 1).map((item, index) => {
    const eleIndex = startIndex + index;
    const node = renderFunc(item, eleIndex, {
      style: {
        width: scrollWidth
      },
      offsetX
    });
    const key = getKey2(item);
    return /* @__PURE__ */ reactExports.createElement(Item, {
      key,
      setRef: (ele) => setNodeRef(item, ele)
    }, node);
  });
}
function findListDiffIndex(originList, targetList, getKey2) {
  const originLen = originList.length;
  const targetLen = targetList.length;
  let shortList;
  let longList;
  if (originLen === 0 && targetLen === 0) {
    return null;
  }
  if (originLen < targetLen) {
    shortList = originList;
    longList = targetList;
  } else {
    shortList = targetList;
    longList = originList;
  }
  const notExistKey = {
    __EMPTY_ITEM__: true
  };
  function getItemKey(item) {
    if (item !== void 0) {
      return getKey2(item);
    }
    return notExistKey;
  }
  let diffIndex = null;
  let multiple = Math.abs(originLen - targetLen) !== 1;
  for (let i = 0; i < longList.length; i += 1) {
    const shortKey = getItemKey(shortList[i]);
    const longKey = getItemKey(longList[i]);
    if (shortKey !== longKey) {
      diffIndex = i;
      multiple = multiple || shortKey !== getItemKey(longList[i + 1]);
      break;
    }
  }
  return diffIndex === null ? null : {
    index: diffIndex,
    multiple
  };
}
function useDiffItem(data, getKey2, onDiff) {
  const [prevData, setPrevData] = reactExports.useState(data);
  const [diffItem, setDiffItem] = reactExports.useState(null);
  reactExports.useEffect(() => {
    const diff = findListDiffIndex(prevData || [], data || [], getKey2);
    if (diff?.index !== void 0) {
      setDiffItem(data[diff.index]);
    }
    setPrevData(data);
  }, [data]);
  return [diffItem];
}
const isFF = typeof navigator === "object" && /Firefox/i.test(navigator.userAgent);
const useOriginScroll = ((isScrollAtTop, isScrollAtBottom, isScrollAtLeft, isScrollAtRight) => {
  const lockRef = reactExports.useRef(false);
  const lockTimeoutRef = reactExports.useRef(null);
  function lockScroll() {
    clearTimeout(lockTimeoutRef.current);
    lockRef.current = true;
    lockTimeoutRef.current = setTimeout(() => {
      lockRef.current = false;
    }, 50);
  }
  const scrollPingRef = reactExports.useRef({
    top: isScrollAtTop,
    bottom: isScrollAtBottom,
    left: isScrollAtLeft,
    right: isScrollAtRight
  });
  scrollPingRef.current.top = isScrollAtTop;
  scrollPingRef.current.bottom = isScrollAtBottom;
  scrollPingRef.current.left = isScrollAtLeft;
  scrollPingRef.current.right = isScrollAtRight;
  return (isHorizontal, delta, smoothOffset = false) => {
    const originScroll = isHorizontal ? (
      // Pass origin wheel when on the left
      delta < 0 && scrollPingRef.current.left || // Pass origin wheel when on the right
      delta > 0 && scrollPingRef.current.right
    ) : delta < 0 && scrollPingRef.current.top || // Pass origin wheel when on the bottom
    delta > 0 && scrollPingRef.current.bottom;
    if (smoothOffset && originScroll) {
      clearTimeout(lockTimeoutRef.current);
      lockRef.current = false;
    } else if (!originScroll || lockRef.current) {
      lockScroll();
    }
    return !lockRef.current && originScroll;
  };
});
function useFrameWheel(inVirtual, isScrollAtTop, isScrollAtBottom, isScrollAtLeft, isScrollAtRight, horizontalScroll, onWheelDelta) {
  const offsetRef = reactExports.useRef(0);
  const nextFrameRef = reactExports.useRef(null);
  const wheelValueRef = reactExports.useRef(null);
  const isMouseScrollRef = reactExports.useRef(false);
  const originScroll = useOriginScroll(isScrollAtTop, isScrollAtBottom, isScrollAtLeft, isScrollAtRight);
  function onWheelY(e, deltaY) {
    wrapperRaf.cancel(nextFrameRef.current);
    if (originScroll(false, deltaY)) return;
    const event = e;
    if (!event._virtualHandled) {
      event._virtualHandled = true;
    } else {
      return;
    }
    offsetRef.current += deltaY;
    wheelValueRef.current = deltaY;
    if (!isFF) {
      event.preventDefault();
    }
    nextFrameRef.current = wrapperRaf(() => {
      const patchMultiple = isMouseScrollRef.current ? 10 : 1;
      onWheelDelta(offsetRef.current * patchMultiple, false);
      offsetRef.current = 0;
    });
  }
  function onWheelX(event, deltaX) {
    onWheelDelta(deltaX, true);
    if (!isFF) {
      event.preventDefault();
    }
  }
  const wheelDirectionRef = reactExports.useRef(null);
  const wheelDirectionCleanRef = reactExports.useRef(null);
  function onWheel(event) {
    if (!inVirtual) return;
    wrapperRaf.cancel(wheelDirectionCleanRef.current);
    wheelDirectionCleanRef.current = wrapperRaf(() => {
      wheelDirectionRef.current = null;
    }, 2);
    const {
      deltaX,
      deltaY,
      shiftKey
    } = event;
    let mergedDeltaX = deltaX;
    let mergedDeltaY = deltaY;
    if (wheelDirectionRef.current === "sx" || !wheelDirectionRef.current && (shiftKey || false) && deltaY && !deltaX) {
      mergedDeltaX = deltaY;
      mergedDeltaY = 0;
      wheelDirectionRef.current = "sx";
    }
    const absX = Math.abs(mergedDeltaX);
    const absY = Math.abs(mergedDeltaY);
    if (wheelDirectionRef.current === null) {
      wheelDirectionRef.current = horizontalScroll && absX > absY ? "x" : "y";
    }
    if (wheelDirectionRef.current === "y") {
      onWheelY(event, mergedDeltaY);
    } else {
      onWheelX(event, mergedDeltaX);
    }
  }
  function onFireFoxScroll(event) {
    if (!inVirtual) return;
    isMouseScrollRef.current = event.detail === wheelValueRef.current;
  }
  return [onWheel, onFireFoxScroll];
}
function useGetSize(mergedData, getKey2, heights, itemHeight) {
  const [key2Index, bottomList] = reactExports.useMemo(() => [/* @__PURE__ */ new Map(), []], [mergedData, heights.id, itemHeight]);
  const getSize = (startKey, endKey = startKey) => {
    let startIndex = key2Index.get(startKey);
    let endIndex = key2Index.get(endKey);
    if (startIndex === void 0 || endIndex === void 0) {
      const dataLen = mergedData.length;
      for (let i = bottomList.length; i < dataLen; i += 1) {
        const item = mergedData[i];
        const key = getKey2(item);
        key2Index.set(key, i);
        const cacheHeight = heights.get(key) ?? itemHeight;
        bottomList[i] = (bottomList[i - 1] || 0) + cacheHeight;
        if (key === startKey) {
          startIndex = i;
        }
        if (key === endKey) {
          endIndex = i;
        }
        if (startIndex !== void 0 && endIndex !== void 0) {
          break;
        }
      }
    }
    return {
      top: bottomList[startIndex - 1] || 0,
      bottom: bottomList[endIndex]
    };
  };
  return getSize;
}
class CacheMap {
  maps;
  // Used for cache key
  // `useMemo` no need to update if `id` not change
  id = 0;
  diffRecords = /* @__PURE__ */ new Map();
  constructor() {
    this.maps = /* @__PURE__ */ Object.create(null);
  }
  set(key, value) {
    this.diffRecords.set(key, this.maps[key]);
    this.maps[key] = value;
    this.id += 1;
  }
  get(key) {
    return this.maps[key];
  }
  /**
   * CacheMap will record the key changed.
   * To help to know what's update in the next render.
   */
  resetRecord() {
    this.diffRecords.clear();
  }
  getRecord() {
    return this.diffRecords;
  }
}
function parseNumber(value) {
  const num = parseFloat(value);
  return isNaN(num) ? 0 : num;
}
function useHeights(getKey2, onItemAdd, onItemRemove) {
  const [updatedMark, setUpdatedMark] = reactExports.useState(0);
  const instanceRef = reactExports.useRef(/* @__PURE__ */ new Map());
  const heightsRef = reactExports.useRef(new CacheMap());
  const promiseIdRef = reactExports.useRef(0);
  function cancelRaf() {
    promiseIdRef.current += 1;
  }
  function collectHeight(sync = false) {
    cancelRaf();
    const doCollect = () => {
      let changed = false;
      instanceRef.current.forEach((element, key) => {
        if (element && element.offsetParent) {
          const {
            offsetHeight
          } = element;
          const {
            marginTop,
            marginBottom
          } = getComputedStyle(element);
          const marginTopNum = parseNumber(marginTop);
          const marginBottomNum = parseNumber(marginBottom);
          const totalHeight = offsetHeight + marginTopNum + marginBottomNum;
          if (heightsRef.current.get(key) !== totalHeight) {
            heightsRef.current.set(key, totalHeight);
            changed = true;
          }
        }
      });
      if (changed) {
        setUpdatedMark((c) => c + 1);
      }
    };
    if (sync) {
      doCollect();
    } else {
      promiseIdRef.current += 1;
      const id = promiseIdRef.current;
      Promise.resolve().then(() => {
        if (id === promiseIdRef.current) {
          doCollect();
        }
      });
    }
  }
  function setInstanceRef(item, instance) {
    const key = getKey2(item);
    instanceRef.current.get(key);
    if (instance) {
      instanceRef.current.set(key, instance);
      collectHeight();
    } else {
      instanceRef.current.delete(key);
    }
  }
  reactExports.useEffect(() => {
    return cancelRaf;
  }, []);
  return [setInstanceRef, collectHeight, heightsRef.current, updatedMark];
}
const SMOOTH_PTG = 14 / 15;
function useMobileTouchMove(inVirtual, listRef, callback) {
  const touchedRef = reactExports.useRef(false);
  const touchXRef = reactExports.useRef(0);
  const touchYRef = reactExports.useRef(0);
  const elementRef = reactExports.useRef(null);
  const intervalRef = reactExports.useRef(null);
  let cleanUpEvents;
  const onTouchMove = (e) => {
    if (touchedRef.current) {
      const currentX = Math.ceil(e.touches[0].pageX);
      const currentY = Math.ceil(e.touches[0].pageY);
      let offsetX = touchXRef.current - currentX;
      let offsetY = touchYRef.current - currentY;
      const isHorizontal = Math.abs(offsetX) > Math.abs(offsetY);
      if (isHorizontal) {
        touchXRef.current = currentX;
      } else {
        touchYRef.current = currentY;
      }
      const scrollHandled = callback(isHorizontal, isHorizontal ? offsetX : offsetY, false, e);
      if (scrollHandled) {
        e.preventDefault();
      }
      clearInterval(intervalRef.current);
      if (scrollHandled) {
        intervalRef.current = setInterval(() => {
          if (isHorizontal) {
            offsetX *= SMOOTH_PTG;
          } else {
            offsetY *= SMOOTH_PTG;
          }
          const offset = Math.floor(isHorizontal ? offsetX : offsetY);
          if (!callback(isHorizontal, offset, true) || Math.abs(offset) <= 0.1) {
            clearInterval(intervalRef.current);
          }
        }, 16);
      }
    }
  };
  const onTouchEnd = () => {
    touchedRef.current = false;
    cleanUpEvents();
  };
  const onTouchStart = (e) => {
    cleanUpEvents();
    if (e.touches.length === 1 && !touchedRef.current) {
      touchedRef.current = true;
      touchXRef.current = Math.ceil(e.touches[0].pageX);
      touchYRef.current = Math.ceil(e.touches[0].pageY);
      elementRef.current = e.target;
      elementRef.current.addEventListener("touchmove", onTouchMove, {
        passive: false
      });
      elementRef.current.addEventListener("touchend", onTouchEnd, {
        passive: true
      });
    }
  };
  cleanUpEvents = () => {
    if (elementRef.current) {
      elementRef.current.removeEventListener("touchmove", onTouchMove);
      elementRef.current.removeEventListener("touchend", onTouchEnd);
    }
  };
  useLayoutEffect(() => {
    if (inVirtual) {
      listRef.current.addEventListener("touchstart", onTouchStart, {
        passive: true
      });
    }
    return () => {
      listRef.current?.removeEventListener("touchstart", onTouchStart);
      cleanUpEvents();
      clearInterval(intervalRef.current);
    };
  }, [inVirtual]);
}
function smoothScrollOffset(offset) {
  return Math.floor(offset ** 0.5);
}
function getPageXY(e, horizontal) {
  const obj = "touches" in e ? e.touches[0] : e;
  return obj[horizontal ? "pageX" : "pageY"] - window[horizontal ? "scrollX" : "scrollY"];
}
function useScrollDrag(inVirtual, componentRef, onScrollOffset) {
  reactExports.useEffect(() => {
    const ele = componentRef.current;
    if (inVirtual && ele) {
      let mouseDownLock = false;
      let rafId;
      let offset;
      const stopScroll = () => {
        wrapperRaf.cancel(rafId);
      };
      const continueScroll = () => {
        stopScroll();
        rafId = wrapperRaf(() => {
          onScrollOffset(offset);
          continueScroll();
        });
      };
      const clearDragState = () => {
        mouseDownLock = false;
        stopScroll();
      };
      const onMouseDown = (e) => {
        if (e.target.draggable || e.button !== 0) {
          return;
        }
        const event = e;
        if (!event._virtualHandled) {
          event._virtualHandled = true;
          mouseDownLock = true;
        }
      };
      const onMouseMove = (e) => {
        if (mouseDownLock) {
          const mouseY = getPageXY(e, false);
          const {
            top,
            bottom
          } = ele.getBoundingClientRect();
          if (mouseY <= top) {
            const diff = top - mouseY;
            offset = -smoothScrollOffset(diff);
            continueScroll();
          } else if (mouseY >= bottom) {
            const diff = mouseY - bottom;
            offset = smoothScrollOffset(diff);
            continueScroll();
          } else {
            stopScroll();
          }
        }
      };
      ele.addEventListener("mousedown", onMouseDown);
      ele.ownerDocument.addEventListener("mouseup", clearDragState);
      ele.ownerDocument.addEventListener("mousemove", onMouseMove);
      ele.ownerDocument.addEventListener("dragend", clearDragState);
      return () => {
        ele.removeEventListener("mousedown", onMouseDown);
        ele.ownerDocument.removeEventListener("mouseup", clearDragState);
        ele.ownerDocument.removeEventListener("mousemove", onMouseMove);
        ele.ownerDocument.removeEventListener("dragend", clearDragState);
        stopScroll();
      };
    }
  }, [inVirtual]);
}
const MAX_TIMES = 10;
function getOffset(rawOffset, info) {
  const resolvedOffset = typeof rawOffset === "function" ? rawOffset(info) : rawOffset;
  return Number.isFinite(resolvedOffset) ? resolvedOffset : 0;
}
function useScrollTo(containerRef, data, heights, itemHeight, getKey2, getSize, collectHeight, syncScrollTop, triggerFlash) {
  const scrollRef = reactExports.useRef(void 0);
  const [syncState, setSyncState] = reactExports.useState(null);
  useLayoutEffect(() => {
    if (syncState && syncState.times < MAX_TIMES) {
      if (!containerRef.current) {
        setSyncState((ori) => ({
          ...ori
        }));
        return;
      }
      collectHeight();
      const {
        targetAlign,
        originAlign,
        offset: rawOffset
      } = syncState;
      const index = syncState.index >= 0 ? syncState.index : data.findIndex((item) => getKey2(item) === syncState.key);
      const mergedAlign = targetAlign || originAlign;
      const offset = getOffset(rawOffset, {
        getSize,
        align: mergedAlign
      });
      const height = containerRef.current.clientHeight;
      let needCollectHeight = index < 0;
      let newTargetAlign = targetAlign;
      let targetTop = null;
      if (height && index >= 0) {
        let stackTop = 0;
        let itemTop = 0;
        let itemBottom = 0;
        const maxLen = Math.min(data.length - 1, index);
        for (let i = 0; i <= maxLen; i += 1) {
          const key = getKey2(data[i]);
          itemTop = stackTop;
          const cacheHeight = heights.get(key);
          itemBottom = itemTop + (cacheHeight === void 0 ? itemHeight : cacheHeight);
          stackTop = itemBottom;
        }
        let leftHeight = mergedAlign === "top" ? offset : height - offset;
        for (let i = maxLen; i >= 0; i -= 1) {
          const key = getKey2(data[i]);
          const cacheHeight = heights.get(key);
          if (cacheHeight === void 0) {
            needCollectHeight = true;
            break;
          }
          leftHeight -= cacheHeight;
          if (leftHeight <= 0) {
            break;
          }
        }
        switch (mergedAlign) {
          case "top":
            targetTop = itemTop - offset;
            break;
          case "bottom":
            targetTop = itemBottom - height + offset;
            break;
          default: {
            const {
              scrollTop
            } = containerRef.current;
            const scrollBottom = scrollTop + height;
            if (itemTop < scrollTop) {
              newTargetAlign = "top";
            } else if (itemBottom > scrollBottom) {
              newTargetAlign = "bottom";
            }
          }
        }
        if (targetTop !== null) {
          syncScrollTop(targetTop);
        }
        if (targetTop !== syncState.lastTop) {
          needCollectHeight = true;
        }
      }
      if (needCollectHeight) {
        setSyncState((prev) => ({
          ...prev,
          times: prev.times + 1,
          index,
          targetAlign: newTargetAlign,
          lastTop: targetTop
        }));
      }
    }
  }, [syncState, containerRef.current]);
  return (arg) => {
    if (arg === null || arg === void 0) {
      triggerFlash();
      return;
    }
    wrapperRaf.cancel(scrollRef.current);
    if (typeof arg === "number") {
      syncScrollTop(arg);
    } else if (arg && typeof arg === "object") {
      let index;
      let key;
      const {
        align
      } = arg;
      if ("index" in arg) {
        ({
          index
        } = arg);
      } else {
        key = arg.key;
        index = data.findIndex((item) => getKey2(item) === key);
      }
      const {
        offset: rawOffset = 0
      } = arg;
      setSyncState({
        times: 0,
        index,
        key,
        offset: rawOffset,
        originAlign: align
      });
    }
  };
}
function getScrollOffsetByThumbTop(thumbTop, enabledScrollRange, enabledOffsetRange) {
  if (enabledScrollRange <= 0 || enabledOffsetRange <= 0) {
    return 0;
  }
  const mergedThumbTop = Math.max(Math.min(thumbTop, enabledOffsetRange), 0);
  const ptg = mergedThumbTop / enabledOffsetRange;
  let nextScrollOffset = Math.ceil(ptg * enabledScrollRange);
  nextScrollOffset = Math.max(nextScrollOffset, 0);
  nextScrollOffset = Math.min(nextScrollOffset, enabledScrollRange);
  return nextScrollOffset;
}
const ScrollBar = /* @__PURE__ */ reactExports.forwardRef((props, ref) => {
  const {
    prefixCls,
    rtl,
    scrollOffset,
    scrollRange,
    onStartMove,
    onStopMove,
    onScroll,
    horizontal,
    spinSize,
    containerSize,
    style,
    thumbStyle: propsThumbStyle,
    showScrollBar
  } = props;
  const [dragging, setDragging] = reactExports.useState(false);
  const [pageXY, setPageXY] = reactExports.useState(null);
  const [startTop, setStartTop] = reactExports.useState(null);
  const isLTR = !rtl;
  const scrollbarRef = reactExports.useRef(null);
  const thumbRef = reactExports.useRef(null);
  const [visible, setVisible] = reactExports.useState(showScrollBar);
  const visibleTimeoutRef = reactExports.useRef(void 0);
  const delayHidden = () => {
    if (showScrollBar === true || showScrollBar === false) return;
    clearTimeout(visibleTimeoutRef.current);
    setVisible(true);
    visibleTimeoutRef.current = setTimeout(() => {
      setVisible(false);
    }, 3e3);
  };
  const enableScrollRange = scrollRange - containerSize || 0;
  const enableOffsetRange = containerSize - spinSize || 0;
  const top = reactExports.useMemo(() => {
    if (scrollOffset === 0 || enableScrollRange === 0) {
      return 0;
    }
    const ptg = scrollOffset / enableScrollRange;
    return ptg * enableOffsetRange;
  }, [scrollOffset, enableScrollRange, enableOffsetRange]);
  const isThumbTarget = (target) => {
    return !!target && thumbRef.current?.contains(target);
  };
  const scrollToTrackPosition = (e) => {
    const scrollbarEle = scrollbarRef.current;
    if (!scrollbarEle) {
      return;
    }
    const rect = scrollbarEle.getBoundingClientRect();
    const pagePosition = getPageXY(e, horizontal);
    let nextTop;
    if (!Number.isFinite(pagePosition)) {
      return;
    }
    if (horizontal) {
      const horizontalStart = isLTR ? rect.left : rect.right;
      if (!Number.isFinite(horizontalStart)) {
        return;
      }
      nextTop = (isLTR ? pagePosition - horizontalStart : horizontalStart - pagePosition) - spinSize / 2;
    } else {
      if (!Number.isFinite(rect.top)) {
        return;
      }
      nextTop = pagePosition - rect.top - spinSize / 2;
    }
    onScroll(getScrollOffsetByThumbTop(nextTop, enableScrollRange, enableOffsetRange), horizontal);
  };
  const onContainerMouseDown = (e) => {
    e.stopPropagation();
    e.preventDefault();
    if (e.button !== 0 || isThumbTarget(e.target)) {
      return;
    }
    scrollToTrackPosition(e);
  };
  const stateRef = reactExports.useRef({
    top,
    dragging,
    pageY: pageXY,
    startTop
  });
  stateRef.current = {
    top,
    dragging,
    pageY: pageXY,
    startTop
  };
  const onThumbMouseDown = useEvent((e) => {
    setDragging(true);
    setPageXY(getPageXY(e, horizontal));
    setStartTop(stateRef.current.top);
    onStartMove();
    e.stopPropagation();
    e.preventDefault();
  });
  reactExports.useEffect(() => {
    const onScrollbarTouchStart = (e) => {
      e.preventDefault();
    };
    const scrollbarEle = scrollbarRef.current;
    const thumbEle = thumbRef.current;
    scrollbarEle.addEventListener("touchstart", onScrollbarTouchStart, {
      passive: false
    });
    thumbEle.addEventListener("touchstart", onThumbMouseDown, {
      passive: false
    });
    return () => {
      scrollbarEle.removeEventListener("touchstart", onScrollbarTouchStart);
      thumbEle.removeEventListener("touchstart", onThumbMouseDown);
    };
  }, [onThumbMouseDown]);
  const enableScrollRangeRef = reactExports.useRef(void 0);
  enableScrollRangeRef.current = enableScrollRange;
  const enableOffsetRangeRef = reactExports.useRef(void 0);
  enableOffsetRangeRef.current = enableOffsetRange;
  reactExports.useEffect(() => {
    if (dragging) {
      let moveRafId;
      const onMouseMove = (e) => {
        const {
          dragging: stateDragging,
          pageY: statePageY,
          startTop: stateStartTop
        } = stateRef.current;
        wrapperRaf.cancel(moveRafId);
        const rect = scrollbarRef.current.getBoundingClientRect();
        const scale = containerSize / (horizontal ? rect.width : rect.height);
        if (stateDragging) {
          const offset = (getPageXY(e, horizontal) - statePageY) * scale;
          let newTop = stateStartTop;
          if (!isLTR && horizontal) {
            newTop -= offset;
          } else {
            newTop += offset;
          }
          const tmpEnableScrollRange = enableScrollRangeRef.current;
          const tmpEnableOffsetRange = enableOffsetRangeRef.current;
          const newScrollTop = getScrollOffsetByThumbTop(newTop, tmpEnableScrollRange, tmpEnableOffsetRange);
          moveRafId = wrapperRaf(() => {
            onScroll(newScrollTop, horizontal);
          });
        }
      };
      const onMouseUp = () => {
        setDragging(false);
        onStopMove();
      };
      window.addEventListener("mousemove", onMouseMove, {
        passive: true
      });
      window.addEventListener("touchmove", onMouseMove, {
        passive: true
      });
      window.addEventListener("mouseup", onMouseUp, {
        passive: true
      });
      window.addEventListener("touchend", onMouseUp, {
        passive: true
      });
      return () => {
        window.removeEventListener("mousemove", onMouseMove);
        window.removeEventListener("touchmove", onMouseMove);
        window.removeEventListener("mouseup", onMouseUp);
        window.removeEventListener("touchend", onMouseUp);
        wrapperRaf.cancel(moveRafId);
      };
    }
  }, [dragging]);
  reactExports.useEffect(() => {
    delayHidden();
    return () => {
      clearTimeout(visibleTimeoutRef.current);
    };
  }, [scrollOffset]);
  reactExports.useImperativeHandle(ref, () => ({
    delayHidden
  }));
  const scrollbarPrefixCls = `${prefixCls}-scrollbar`;
  const containerStyle = {
    position: "absolute",
    visibility: visible ? null : "hidden"
  };
  const thumbStyle = {
    position: "absolute",
    borderRadius: 99,
    background: "var(--rc-virtual-list-scrollbar-bg, rgba(0, 0, 0, 0.5))",
    cursor: "pointer",
    userSelect: "none"
  };
  if (horizontal) {
    Object.assign(containerStyle, {
      height: 8,
      left: 0,
      right: 0,
      bottom: 0
    });
    Object.assign(thumbStyle, {
      height: "100%",
      width: spinSize,
      [isLTR ? "left" : "right"]: top
    });
  } else {
    Object.assign(containerStyle, {
      width: 8,
      top: 0,
      bottom: 0,
      [isLTR ? "right" : "left"]: 0
    });
    Object.assign(thumbStyle, {
      width: "100%",
      height: spinSize,
      top
    });
  }
  return /* @__PURE__ */ reactExports.createElement("div", {
    ref: scrollbarRef,
    className: clsx(scrollbarPrefixCls, {
      [`${scrollbarPrefixCls}-horizontal`]: horizontal,
      [`${scrollbarPrefixCls}-vertical`]: !horizontal,
      [`${scrollbarPrefixCls}-visible`]: visible
    }),
    style: {
      ...containerStyle,
      ...style
    },
    onMouseDown: onContainerMouseDown,
    onMouseMove: delayHidden
  }, /* @__PURE__ */ reactExports.createElement("div", {
    ref: thumbRef,
    className: clsx(`${scrollbarPrefixCls}-thumb`, {
      [`${scrollbarPrefixCls}-thumb-moving`]: dragging
    }),
    style: {
      ...thumbStyle,
      ...propsThumbStyle
    },
    onMouseDown: onThumbMouseDown
  }));
});
const MIN_SIZE = 20;
function getSpinSize(containerSize = 0, scrollRange = 0) {
  let baseSize = containerSize / scrollRange * containerSize;
  if (isNaN(baseSize)) {
    baseSize = 0;
  }
  baseSize = Math.max(baseSize, MIN_SIZE);
  return Math.floor(baseSize);
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
const EMPTY_DATA = [];
const ScrollStyle = {
  overflowY: "auto",
  overflowAnchor: "none"
};
function RawList(props, ref) {
  const {
    prefixCls = "rc-virtual-list",
    className,
    height,
    itemHeight,
    fullHeight = true,
    style,
    data,
    children,
    itemKey: itemKey2,
    virtual,
    direction,
    scrollWidth,
    component: Component = "div",
    onScroll,
    onVirtualScroll,
    onVisibleChange,
    innerProps,
    extraRender,
    styles,
    showScrollBar = "optional",
    ...restProps
  } = props;
  const getKey2 = reactExports.useCallback((item) => {
    if (typeof itemKey2 === "function") {
      return itemKey2(item);
    }
    return item?.[itemKey2];
  }, [itemKey2]);
  const [setInstanceRef, collectHeight, heights, heightUpdatedMark] = useHeights(getKey2);
  const useVirtual = !!(virtual !== false && height && itemHeight);
  const containerHeight = reactExports.useMemo(() => Object.values(heights.maps).reduce((total, curr) => total + curr, 0), [heights.id, heights.maps]);
  const inVirtual = useVirtual && data && (Math.max(itemHeight * data.length, containerHeight) > height || !!scrollWidth);
  const isRTL = direction === "rtl";
  const mergedClassName = clsx(prefixCls, {
    [`${prefixCls}-rtl`]: isRTL
  }, className);
  const mergedData = data || EMPTY_DATA;
  const componentRef = reactExports.useRef(null);
  const fillerInnerRef = reactExports.useRef(null);
  const containerRef = reactExports.useRef(null);
  const [offsetTop, setOffsetTop] = reactExports.useState(0);
  const [offsetLeft, setOffsetLeft] = reactExports.useState(0);
  const [scrollMoving, setScrollMoving] = reactExports.useState(false);
  const onScrollbarStartMove = () => {
    setScrollMoving(true);
  };
  const onScrollbarStopMove = () => {
    setScrollMoving(false);
  };
  const sharedConfig = {
    getKey: getKey2
  };
  function syncScrollTop(newTop) {
    setOffsetTop((origin) => {
      let value;
      if (typeof newTop === "function") {
        value = newTop(origin);
      } else {
        value = newTop;
      }
      const alignedTop = keepInRange(value);
      componentRef.current.scrollTop = alignedTop;
      return alignedTop;
    });
  }
  const rangeRef = reactExports.useRef({
    start: 0,
    end: mergedData.length
  });
  const diffItemRef = reactExports.useRef(void 0);
  const [diffItem] = useDiffItem(mergedData, getKey2);
  diffItemRef.current = diffItem;
  const {
    scrollHeight,
    start,
    end,
    offset: fillerOffset
  } = reactExports.useMemo(() => {
    if (!useVirtual) {
      return {
        scrollHeight: void 0,
        start: 0,
        end: mergedData.length - 1,
        offset: void 0
      };
    }
    if (!inVirtual) {
      return {
        scrollHeight: fillerInnerRef.current?.offsetHeight || 0,
        start: 0,
        end: mergedData.length - 1,
        offset: void 0
      };
    }
    let itemTop = 0;
    let startIndex;
    let startOffset;
    let endIndex;
    const dataLen = mergedData.length;
    for (let i = 0; i < dataLen; i += 1) {
      const item = mergedData[i];
      const key = getKey2(item);
      const cacheHeight = heights.get(key);
      const currentItemBottom = itemTop + (cacheHeight === void 0 ? itemHeight : cacheHeight);
      if (currentItemBottom >= offsetTop && startIndex === void 0) {
        startIndex = i;
        startOffset = itemTop;
      }
      if (currentItemBottom > offsetTop + height && endIndex === void 0) {
        endIndex = i;
      }
      itemTop = currentItemBottom;
    }
    if (startIndex === void 0) {
      startIndex = 0;
      startOffset = 0;
      endIndex = Math.ceil(height / itemHeight);
    }
    if (endIndex === void 0) {
      endIndex = mergedData.length - 1;
    }
    endIndex = Math.min(endIndex + 1, mergedData.length - 1);
    return {
      scrollHeight: itemTop,
      start: startIndex,
      end: endIndex,
      offset: startOffset
    };
  }, [inVirtual, useVirtual, offsetTop, mergedData, heightUpdatedMark, height]);
  rangeRef.current.start = start;
  rangeRef.current.end = end;
  reactExports.useLayoutEffect(() => {
    const changedRecord = heights.getRecord();
    if (changedRecord.size === 1) {
      const recordKey = Array.from(changedRecord.keys())[0];
      const prevCacheHeight = changedRecord.get(recordKey);
      const startItem = mergedData[start];
      if (startItem && prevCacheHeight === void 0) {
        const startIndexKey = getKey2(startItem);
        if (startIndexKey === recordKey) {
          const realStartHeight = heights.get(recordKey);
          const diffHeight = realStartHeight - itemHeight;
          syncScrollTop((ori) => {
            return ori + diffHeight;
          });
        }
      }
    }
    heights.resetRecord();
  }, [scrollHeight]);
  const [size, setSize] = reactExports.useState({
    width: 0,
    height
  });
  const onHolderResize = (sizeInfo) => {
    setSize({
      width: sizeInfo.offsetWidth,
      height: sizeInfo.offsetHeight
    });
  };
  const verticalScrollBarRef = reactExports.useRef(null);
  const horizontalScrollBarRef = reactExports.useRef(null);
  const horizontalScrollBarSpinSize = reactExports.useMemo(() => getSpinSize(size.width, scrollWidth), [size.width, scrollWidth]);
  const verticalScrollBarSpinSize = reactExports.useMemo(() => getSpinSize(size.height, scrollHeight), [size.height, scrollHeight]);
  const maxScrollHeight = scrollHeight - height;
  const maxScrollHeightRef = reactExports.useRef(maxScrollHeight);
  maxScrollHeightRef.current = maxScrollHeight;
  function keepInRange(newScrollTop) {
    let newTop = newScrollTop;
    if (!Number.isNaN(maxScrollHeightRef.current)) {
      newTop = Math.min(newTop, maxScrollHeightRef.current);
    }
    newTop = Math.max(newTop, 0);
    return newTop;
  }
  const isScrollAtTop = offsetTop <= 0;
  const isScrollAtBottom = offsetTop >= maxScrollHeight;
  const isScrollAtLeft = offsetLeft <= 0;
  const isScrollAtRight = offsetLeft >= scrollWidth;
  const originScroll = useOriginScroll(isScrollAtTop, isScrollAtBottom, isScrollAtLeft, isScrollAtRight);
  const getVirtualScrollInfo = () => ({
    x: isRTL ? -offsetLeft : offsetLeft,
    y: offsetTop
  });
  const lastVirtualScrollInfoRef = reactExports.useRef(getVirtualScrollInfo());
  const triggerScroll = useEvent((params) => {
    if (onVirtualScroll) {
      const nextInfo = {
        ...getVirtualScrollInfo(),
        ...params
      };
      if (lastVirtualScrollInfoRef.current.x !== nextInfo.x || lastVirtualScrollInfoRef.current.y !== nextInfo.y) {
        onVirtualScroll(nextInfo);
        lastVirtualScrollInfoRef.current = nextInfo;
      }
    }
  });
  function onScrollBar(newScrollOffset, horizontal) {
    const newOffset = newScrollOffset;
    if (horizontal) {
      reactDomExports.flushSync(() => {
        setOffsetLeft(newOffset);
      });
      triggerScroll();
    } else {
      syncScrollTop(newOffset);
    }
  }
  function onFallbackScroll(e) {
    const {
      scrollTop: newScrollTop
    } = e.currentTarget;
    if (newScrollTop !== offsetTop) {
      syncScrollTop(newScrollTop);
    }
    onScroll?.(e);
    triggerScroll();
  }
  const keepInHorizontalRange = (nextOffsetLeft) => {
    let tmpOffsetLeft = nextOffsetLeft;
    const max = !!scrollWidth ? scrollWidth - size.width : 0;
    tmpOffsetLeft = Math.max(tmpOffsetLeft, 0);
    tmpOffsetLeft = Math.min(tmpOffsetLeft, max);
    return tmpOffsetLeft;
  };
  const onWheelDelta = useEvent((offsetXY, fromHorizontal) => {
    if (fromHorizontal) {
      reactDomExports.flushSync(() => {
        setOffsetLeft((left) => {
          const nextOffsetLeft = left + (isRTL ? -offsetXY : offsetXY);
          return keepInHorizontalRange(nextOffsetLeft);
        });
      });
      triggerScroll();
    } else {
      syncScrollTop((top) => {
        const newTop = top + offsetXY;
        return newTop;
      });
    }
  });
  const [onRawWheel, onFireFoxScroll] = useFrameWheel(useVirtual, isScrollAtTop, isScrollAtBottom, isScrollAtLeft, isScrollAtRight, !!scrollWidth, onWheelDelta);
  useMobileTouchMove(useVirtual, componentRef, (isHorizontal, delta, smoothOffset, e) => {
    const event = e;
    if (originScroll(isHorizontal, delta, smoothOffset)) {
      return false;
    }
    if (!event || !event._virtualHandled) {
      if (event) {
        event._virtualHandled = true;
      }
      onRawWheel({
        preventDefault() {
        },
        deltaX: isHorizontal ? delta : 0,
        deltaY: isHorizontal ? 0 : delta
      });
      return true;
    }
    return false;
  });
  useScrollDrag(inVirtual, componentRef, (offset) => {
    syncScrollTop((top) => top + offset);
  });
  useLayoutEffect(() => {
    function onMozMousePixelScroll(e) {
      const scrollingUpAtTop = isScrollAtTop && e.detail < 0;
      const scrollingDownAtBottom = isScrollAtBottom && e.detail > 0;
      if (useVirtual && !scrollingUpAtTop && !scrollingDownAtBottom) {
        e.preventDefault();
      }
    }
    const componentEle = componentRef.current;
    componentEle.addEventListener("wheel", onRawWheel, {
      passive: false
    });
    componentEle.addEventListener("DOMMouseScroll", onFireFoxScroll, {
      passive: true
    });
    componentEle.addEventListener("MozMousePixelScroll", onMozMousePixelScroll, {
      passive: false
    });
    return () => {
      componentEle.removeEventListener("wheel", onRawWheel);
      componentEle.removeEventListener("DOMMouseScroll", onFireFoxScroll);
      componentEle.removeEventListener("MozMousePixelScroll", onMozMousePixelScroll);
    };
  }, [useVirtual, isScrollAtTop, isScrollAtBottom]);
  useLayoutEffect(() => {
    if (scrollWidth) {
      const newOffsetLeft = keepInHorizontalRange(offsetLeft);
      setOffsetLeft(newOffsetLeft);
      triggerScroll({
        x: newOffsetLeft
      });
    }
  }, [size.width, scrollWidth]);
  const delayHideScrollBar = () => {
    verticalScrollBarRef.current?.delayHidden();
    horizontalScrollBarRef.current?.delayHidden();
  };
  const getSize = useGetSize(mergedData, getKey2, heights, itemHeight);
  const scrollTo = useScrollTo(componentRef, mergedData, heights, itemHeight, getKey2, getSize, () => collectHeight(true), syncScrollTop, delayHideScrollBar);
  reactExports.useImperativeHandle(ref, () => ({
    nativeElement: containerRef.current,
    getScrollInfo: getVirtualScrollInfo,
    scrollTo: (config) => {
      function isPosScroll(arg) {
        return arg && typeof arg === "object" && ("left" in arg || "top" in arg);
      }
      if (isPosScroll(config)) {
        if (config.left !== void 0) {
          setOffsetLeft(keepInHorizontalRange(config.left));
        }
        scrollTo(config.top);
      } else {
        scrollTo(config);
      }
    }
  }));
  useLayoutEffect(() => {
    if (onVisibleChange) {
      const renderList = mergedData.slice(start, end + 1);
      onVisibleChange(renderList, mergedData);
    }
  }, [start, end, mergedData]);
  const extraContent = extraRender?.({
    start,
    end,
    virtual: inVirtual,
    offsetX: offsetLeft,
    scrollTop: offsetTop,
    offsetY: fillerOffset,
    rtl: isRTL,
    getSize
  });
  const listChildren = useChildren(mergedData, start, end, scrollWidth, offsetLeft, setInstanceRef, children, sharedConfig);
  let componentStyle = null;
  if (height) {
    componentStyle = {
      [fullHeight ? "height" : "maxHeight"]: height,
      ...ScrollStyle
    };
    if (useVirtual) {
      componentStyle.overflowY = "hidden";
      if (scrollWidth) {
        componentStyle.overflowX = "hidden";
      }
      if (scrollMoving) {
        componentStyle.pointerEvents = "none";
      }
    }
  }
  const containerProps = {};
  if (isRTL) {
    containerProps.dir = "rtl";
  }
  return /* @__PURE__ */ reactExports.createElement("div", _extends$4({
    ref: containerRef,
    style: {
      ...style,
      position: "relative"
    },
    className: mergedClassName
  }, containerProps, restProps), /* @__PURE__ */ reactExports.createElement(RefResizeObserver, {
    onResize: onHolderResize
  }, /* @__PURE__ */ reactExports.createElement(Component, {
    className: `${prefixCls}-holder`,
    style: componentStyle,
    ref: componentRef,
    onScroll: onFallbackScroll,
    onMouseEnter: delayHideScrollBar
  }, /* @__PURE__ */ reactExports.createElement(Filler, {
    prefixCls,
    height: scrollHeight,
    offsetX: offsetLeft,
    offsetY: fillerOffset,
    scrollWidth,
    onInnerResize: collectHeight,
    ref: fillerInnerRef,
    innerProps,
    rtl: isRTL,
    extra: extraContent
  }, listChildren))), inVirtual && scrollHeight > height && /* @__PURE__ */ reactExports.createElement(ScrollBar, {
    ref: verticalScrollBarRef,
    prefixCls,
    scrollOffset: offsetTop,
    scrollRange: scrollHeight,
    rtl: isRTL,
    onScroll: onScrollBar,
    onStartMove: onScrollbarStartMove,
    onStopMove: onScrollbarStopMove,
    spinSize: verticalScrollBarSpinSize,
    containerSize: size.height,
    style: styles?.verticalScrollBar,
    thumbStyle: styles?.verticalScrollBarThumb,
    showScrollBar
  }), inVirtual && scrollWidth > size.width && /* @__PURE__ */ reactExports.createElement(ScrollBar, {
    ref: horizontalScrollBarRef,
    prefixCls,
    scrollOffset: offsetLeft,
    scrollRange: scrollWidth,
    rtl: isRTL,
    onScroll: onScrollBar,
    onStartMove: onScrollbarStartMove,
    onStopMove: onScrollbarStopMove,
    spinSize: horizontalScrollBarSpinSize,
    containerSize: size.width,
    horizontal: true,
    style: styles?.horizontalScrollBar,
    thumbStyle: styles?.horizontalScrollBarThumb,
    showScrollBar
  }));
}
const List$1 = /* @__PURE__ */ reactExports.forwardRef(RawList);
List$1.displayName = "List";
const List = /* @__PURE__ */ reactExports.forwardRef((props, ref) => RawList({
  ...props,
  virtual: false
}, ref));
List.displayName = "List";
function isPlatformMac() {
  return /(mac\sos|macintosh)/i.test(navigator.appVersion);
}
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
function isTitleType(content) {
  return typeof content === "string" || typeof content === "number";
}
const OptionList = (_, ref) => {
  const {
    prefixCls,
    id,
    open,
    multiple,
    mode,
    searchValue,
    toggleOpen,
    notFoundContent,
    onPopupScroll,
    showScrollBar,
    lockOptions
  } = useBaseProps();
  const {
    maxCount,
    flattenOptions: flattenOptions2,
    onActiveValue,
    defaultActiveFirstOption,
    onSelect,
    menuItemSelectedIcon,
    rawValues,
    fieldNames,
    virtual,
    direction,
    listHeight,
    listItemHeight,
    optionRender,
    classNames: contextClassNames,
    styles: contextStyles
  } = reactExports.useContext(SelectContext);
  const itemPrefixCls = `${prefixCls}-item`;
  const memoFlattenOptions = useMemo(() => flattenOptions2, [open, lockOptions], (prev, next) => next[0] && !next[1]);
  const listRef = reactExports.useRef(null);
  const overMaxCount = reactExports.useMemo(() => multiple && isValidCount(maxCount) && rawValues?.size >= maxCount, [multiple, maxCount, rawValues?.size]);
  const onListMouseDown = (event) => {
    event.preventDefault();
  };
  const scrollIntoView = (args) => {
    listRef.current?.scrollTo(typeof args === "number" ? {
      index: args
    } : args);
  };
  const isSelected = reactExports.useCallback((value) => {
    if (mode === "combobox") {
      return false;
    }
    return rawValues.has(value);
  }, [mode, [...rawValues].toString(), rawValues.size]);
  const getEnabledActiveIndex = (index, offset = 1) => {
    const len = memoFlattenOptions.length;
    for (let i = 0; i < len; i += 1) {
      const current = (index + i * offset + len) % len;
      const {
        group,
        data
      } = memoFlattenOptions[current] || {};
      if (!group && !data?.disabled && (isSelected(data.value) || !overMaxCount)) {
        return current;
      }
    }
    return -1;
  };
  const [activeIndex, setActiveIndex] = reactExports.useState(() => getEnabledActiveIndex(0));
  const setActive = (index, fromKeyboard = false) => {
    setActiveIndex(index);
    const info = {
      source: fromKeyboard ? "keyboard" : "mouse"
    };
    const flattenItem = memoFlattenOptions[index];
    if (!flattenItem) {
      onActiveValue(null, -1, info);
      return;
    }
    onActiveValue(flattenItem.value, index, info);
  };
  reactExports.useEffect(() => {
    setActive(defaultActiveFirstOption !== false ? getEnabledActiveIndex(0) : -1);
  }, [memoFlattenOptions.length, searchValue]);
  const isAriaSelected = reactExports.useCallback((value) => {
    if (mode === "combobox") {
      return String(value).toLowerCase() === searchValue.toLowerCase();
    }
    return rawValues.has(value);
  }, [mode, searchValue, [...rawValues].toString(), rawValues.size]);
  reactExports.useEffect(() => {
    let timeoutId;
    if (!multiple && open && rawValues.size === 1) {
      const value = Array.from(rawValues)[0];
      const index = memoFlattenOptions.findIndex(({
        data
      }) => searchValue ? String(data.value).startsWith(searchValue) : data.value === value);
      if (index !== -1) {
        setActive(index);
        timeoutId = setTimeout(() => {
          scrollIntoView(index);
        });
      }
    }
    if (open) {
      listRef.current?.scrollTo(void 0);
    }
    return () => clearTimeout(timeoutId);
  }, [open, searchValue]);
  const onSelectValue = (value) => {
    if (value !== void 0) {
      onSelect(value, {
        selected: !rawValues.has(value)
      });
    }
    if (!multiple) {
      toggleOpen(false);
    }
  };
  reactExports.useImperativeHandle(ref, () => ({
    onKeyDown: (event) => {
      const {
        which,
        ctrlKey
      } = event;
      switch (which) {
        // >>> Arrow keys & ctrl + n/p on Mac
        case KeyCode.N:
        case KeyCode.P:
        case KeyCode.UP:
        case KeyCode.DOWN: {
          let offset = 0;
          if (which === KeyCode.UP) {
            offset = -1;
          } else if (which === KeyCode.DOWN) {
            offset = 1;
          } else if (isPlatformMac() && ctrlKey) {
            if (which === KeyCode.N) {
              offset = 1;
            } else if (which === KeyCode.P) {
              offset = -1;
            }
          }
          if (offset !== 0) {
            const nextActiveIndex = getEnabledActiveIndex(activeIndex + offset, offset);
            scrollIntoView(nextActiveIndex);
            setActive(nextActiveIndex, true);
          }
          break;
        }
        // >>> Select (Tab / Enter)
        case KeyCode.TAB:
        case KeyCode.ENTER: {
          const item = memoFlattenOptions[activeIndex];
          if (!item || item.data.disabled) {
            return onSelectValue(void 0);
          }
          if (!overMaxCount || rawValues.has(item.value)) {
            onSelectValue(item.value);
          } else {
            onSelectValue(void 0);
          }
          if (open) {
            event.preventDefault();
          }
          break;
        }
        // >>> Close
        case KeyCode.ESC: {
          toggleOpen(false);
          if (open) {
            event.stopPropagation();
          }
        }
      }
    },
    onKeyUp: () => {
    },
    scrollTo: (index) => {
      scrollIntoView(index);
    }
  }));
  if (memoFlattenOptions.length === 0) {
    return /* @__PURE__ */ reactExports.createElement("div", {
      role: "listbox",
      id: `${id}_list`,
      className: `${itemPrefixCls}-empty`,
      onMouseDown: onListMouseDown
    }, notFoundContent);
  }
  const omitFieldNameList = Object.keys(fieldNames).map((key) => fieldNames[key]);
  const getLabel = (item) => item.label;
  function getItemAriaProps(item, index) {
    const {
      group
    } = item;
    return {
      role: group ? "presentation" : "option",
      id: `${id}_list_${index}`
    };
  }
  const renderItem = (index) => {
    const item = memoFlattenOptions[index];
    if (!item) {
      return null;
    }
    const itemData = item.data || {};
    const {
      value,
      disabled
    } = itemData;
    const {
      group
    } = item;
    const attrs = pickAttrs(itemData, true);
    const mergedLabel = getLabel(item);
    return item ? /* @__PURE__ */ reactExports.createElement("div", _extends$3({
      "aria-label": typeof mergedLabel === "string" && !group ? mergedLabel : null
    }, attrs, {
      key: index
    }, getItemAriaProps(item, index), {
      "aria-selected": isAriaSelected(value),
      "aria-disabled": disabled
    }), value) : null;
  };
  const a11yProps = {
    role: "listbox",
    id: `${id}_list`
  };
  return /* @__PURE__ */ reactExports.createElement(reactExports.Fragment, null, virtual && /* @__PURE__ */ reactExports.createElement("div", _extends$3({}, a11yProps, {
    style: {
      height: 0,
      width: 0,
      overflow: "hidden"
    }
  }), renderItem(activeIndex - 1), renderItem(activeIndex), renderItem(activeIndex + 1)), /* @__PURE__ */ reactExports.createElement(List$1, {
    prefixCls: `${prefixCls}-dropdown-list`,
    itemKey: "key",
    ref: listRef,
    data: memoFlattenOptions,
    height: listHeight,
    itemHeight: listItemHeight,
    fullHeight: false,
    onMouseDown: onListMouseDown,
    onScroll: onPopupScroll,
    virtual,
    direction,
    innerProps: virtual ? null : a11yProps,
    showScrollBar,
    className: contextClassNames?.popup?.list,
    style: contextStyles?.popup?.list
  }, (item, itemIndex) => {
    const {
      group,
      groupOption,
      data,
      label,
      value
    } = item;
    const {
      key
    } = data;
    if (group) {
      const groupTitle = data.title ?? (isTitleType(label) ? label.toString() : void 0);
      return /* @__PURE__ */ reactExports.createElement("div", {
        className: clsx(itemPrefixCls, `${itemPrefixCls}-group`, data.className),
        title: groupTitle
      }, label !== void 0 ? label : key);
    }
    const {
      disabled,
      title,
      children,
      style,
      className,
      ...otherProps
    } = data;
    const passedProps = omit(otherProps, omitFieldNameList);
    const selected = isSelected(value);
    const mergedDisabled = disabled || !selected && overMaxCount;
    const optionPrefixCls = `${itemPrefixCls}-option`;
    const optionClassName = clsx(itemPrefixCls, optionPrefixCls, className, contextClassNames?.popup?.listItem, {
      [`${optionPrefixCls}-grouped`]: groupOption,
      [`${optionPrefixCls}-active`]: activeIndex === itemIndex && !mergedDisabled,
      [`${optionPrefixCls}-disabled`]: mergedDisabled,
      [`${optionPrefixCls}-selected`]: selected
    });
    const mergedLabel = getLabel(item);
    const iconVisible = !menuItemSelectedIcon || typeof menuItemSelectedIcon === "function" || selected;
    const content = typeof mergedLabel === "number" ? mergedLabel : mergedLabel || value;
    let optionTitle = isTitleType(content) ? content.toString() : void 0;
    if (title !== void 0) {
      optionTitle = title;
    }
    return /* @__PURE__ */ reactExports.createElement("div", _extends$3({}, pickAttrs(passedProps), !virtual ? getItemAriaProps(item, itemIndex) : {}, {
      "aria-selected": virtual ? void 0 : isAriaSelected(value),
      "aria-disabled": mergedDisabled,
      className: optionClassName,
      title: optionTitle,
      onMouseMove: () => {
        if (activeIndex === itemIndex || mergedDisabled) {
          return;
        }
        setActive(itemIndex);
      },
      onClick: () => {
        if (!mergedDisabled) {
          onSelectValue(value);
        }
      },
      style: {
        ...contextStyles?.popup?.listItem,
        ...style
      }
    }), /* @__PURE__ */ reactExports.createElement("div", {
      className: `${optionPrefixCls}-content`
    }, typeof optionRender === "function" ? optionRender(item, {
      index: itemIndex
    }) : content), /* @__PURE__ */ reactExports.isValidElement(menuItemSelectedIcon) || selected, iconVisible && /* @__PURE__ */ reactExports.createElement(TransBtn, {
      className: `${itemPrefixCls}-option-state`,
      customizeIcon: menuItemSelectedIcon,
      customizeIconProps: {
        value,
        disabled: mergedDisabled,
        isSelected: selected
      }
    }, selected ? "✓" : null));
  }));
};
const RefOptionList = /* @__PURE__ */ reactExports.forwardRef(OptionList);
const useCache = ((labeledValues, valueOptions) => {
  const cacheRef = reactExports.useRef({
    values: /* @__PURE__ */ new Map(),
    options: /* @__PURE__ */ new Map()
  });
  const filledLabeledValues = reactExports.useMemo(() => {
    const {
      values: prevValueCache,
      options: prevOptionCache
    } = cacheRef.current;
    const patchedValues = labeledValues.map((item) => {
      if (item.label === void 0) {
        return {
          ...item,
          label: prevValueCache.get(item.value)?.label
        };
      }
      return item;
    });
    const valueCache = /* @__PURE__ */ new Map();
    const optionCache = /* @__PURE__ */ new Map();
    patchedValues.forEach((item) => {
      valueCache.set(item.value, item);
      optionCache.set(item.value, valueOptions.get(item.value) || prevOptionCache.get(item.value));
    });
    cacheRef.current.values = valueCache;
    cacheRef.current.options = optionCache;
    return patchedValues;
  }, [labeledValues, valueOptions]);
  const getOption = reactExports.useCallback((val) => valueOptions.get(val) || cacheRef.current.options.get(val), [valueOptions]);
  return [filledLabeledValues, getOption];
});
function includes(test, search) {
  return toArray(test).join("").toUpperCase().includes(search);
}
const useFilterOptions = ((options, fieldNames, searchValue, filterOption, optionFilterProp) => {
  return reactExports.useMemo(() => {
    if (!searchValue || filterOption === false) {
      return options;
    }
    const {
      options: fieldOptions,
      label: fieldLabel,
      value: fieldValue
    } = fieldNames;
    const filteredOptions = [];
    const customizeFilter = typeof filterOption === "function";
    const upperSearch = searchValue.toUpperCase();
    const filterFunc = customizeFilter ? filterOption : (_, option) => {
      if (optionFilterProp && optionFilterProp.length) {
        return optionFilterProp.some((prop) => includes(option[prop], upperSearch));
      }
      if (option[fieldOptions]) {
        return includes(option[fieldLabel !== "children" ? fieldLabel : "label"], upperSearch);
      }
      return includes(option[fieldValue], upperSearch);
    };
    const wrapOption = customizeFilter ? (opt) => injectPropsWithOption(opt) : (opt) => opt;
    options.forEach((item) => {
      if (item[fieldOptions]) {
        const matchGroup = filterFunc(searchValue, wrapOption(item));
        if (matchGroup) {
          filteredOptions.push(item);
        } else {
          const subOptions = item[fieldOptions].filter((subItem) => filterFunc(searchValue, wrapOption(subItem)));
          if (subOptions.length) {
            filteredOptions.push({
              ...item,
              [fieldOptions]: subOptions
            });
          }
        }
        return;
      }
      if (filterFunc(searchValue, wrapOption(item))) {
        filteredOptions.push(item);
      }
    });
    return filteredOptions;
  }, [options, filterOption, optionFilterProp, searchValue, fieldNames]);
});
function convertNodeToOption(node) {
  const {
    key,
    props: {
      children,
      value,
      ...restProps
    }
  } = node;
  return {
    key,
    value: value !== void 0 ? value : key,
    children,
    ...restProps
  };
}
function convertChildrenToData(nodes, optionOnly = false) {
  return toArray$1(nodes).map((node, index) => {
    if (!/* @__PURE__ */ reactExports.isValidElement(node) || !node.type) {
      return null;
    }
    const {
      type: {
        isSelectOptGroup
      },
      key,
      props: {
        children,
        ...restProps
      }
    } = node;
    if (optionOnly || !isSelectOptGroup) {
      return convertNodeToOption(node);
    }
    return {
      key: `__RC_SELECT_GRP__${key === null ? index : key}__`,
      label: key,
      ...restProps,
      options: convertChildrenToData(children)
    };
  }).filter((data) => data);
}
const useOptions = (options, children, fieldNames, optionFilterProp, optionLabelProp) => {
  return reactExports.useMemo(() => {
    let mergedOptions = options;
    const childrenAsData = !options;
    if (childrenAsData) {
      mergedOptions = convertChildrenToData(children);
    }
    const valueOptions = /* @__PURE__ */ new Map();
    const labelOptions = /* @__PURE__ */ new Map();
    const setLabelOptions = (labelOptionsMap, option, key) => {
      if (key && typeof key === "string") {
        labelOptionsMap.set(option[key], option);
      }
    };
    const dig = (optionList, isChildren = false) => {
      for (let i = 0; i < optionList.length; i += 1) {
        const option = optionList[i];
        if (!option[fieldNames.options] || isChildren) {
          valueOptions.set(option[fieldNames.value], option);
          setLabelOptions(labelOptions, option, fieldNames.label);
          optionFilterProp.forEach((prop) => {
            setLabelOptions(labelOptions, option, prop);
          });
          setLabelOptions(labelOptions, option, optionLabelProp);
        } else {
          dig(option[fieldNames.options], true);
        }
      }
    };
    dig(mergedOptions);
    return {
      options: mergedOptions,
      valueOptions,
      labelOptions
    };
  }, [options, children, fieldNames, optionFilterProp, optionLabelProp]);
};
function useRefFunc(callback) {
  const funcRef = reactExports.useRef(callback);
  funcRef.current = callback;
  const cacheFn = reactExports.useCallback((...args) => {
    return funcRef.current(...args);
  }, []);
  return cacheFn;
}
function useSearchConfig(showSearch, props, mode) {
  const {
    filterOption,
    searchValue,
    optionFilterProp,
    filterSort,
    onSearch,
    autoClearSearchValue
  } = props;
  return reactExports.useMemo(() => {
    const isObject = typeof showSearch === "object";
    const searchConfig = {
      filterOption,
      searchValue,
      optionFilterProp,
      filterSort,
      onSearch,
      autoClearSearchValue,
      ...isObject ? showSearch : {}
    };
    return [isObject || mode === "combobox" || mode === "tags" || mode === "multiple" && showSearch === void 0 ? true : showSearch, searchConfig];
  }, [mode, showSearch, filterOption, searchValue, optionFilterProp, filterSort, onSearch, autoClearSearchValue]);
}
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
const OMIT_DOM_PROPS = ["inputValue"];
function isRawValue(value) {
  return !value || typeof value !== "object";
}
const Select$1 = /* @__PURE__ */ reactExports.forwardRef((props, ref) => {
  const {
    id,
    mode,
    prefixCls = "rc-select",
    backfill,
    fieldNames,
    // Search
    showSearch,
    searchValue: legacySearchValue,
    onSearch: legacyOnSearch,
    autoClearSearchValue: legacyAutoClearSearchValue,
    filterOption: legacyFilterOption,
    optionFilterProp: legacyOptionFilterProp,
    filterSort: legacyFilterSort,
    // Select
    onSelect,
    onDeselect,
    onActive,
    popupMatchSelectWidth = true,
    optionLabelProp,
    options,
    optionRender,
    children,
    defaultActiveFirstOption,
    menuItemSelectedIcon,
    virtual,
    direction,
    listHeight = 200,
    listItemHeight = 20,
    labelRender,
    // Value
    value,
    defaultValue,
    labelInValue,
    onChange,
    maxCount,
    classNames,
    styles,
    ...restProps
  } = props;
  const searchProps = {
    searchValue: legacySearchValue,
    onSearch: legacyOnSearch,
    autoClearSearchValue: legacyAutoClearSearchValue,
    filterOption: legacyFilterOption,
    optionFilterProp: legacyOptionFilterProp,
    filterSort: legacyFilterSort
  };
  const [mergedShowSearch, searchConfig] = useSearchConfig(showSearch, searchProps, mode);
  const {
    filterOption,
    searchValue,
    optionFilterProp,
    filterSort,
    onSearch,
    autoClearSearchValue = true
  } = searchConfig;
  const normalizedOptionFilterProp = reactExports.useMemo(() => {
    if (!optionFilterProp) return [];
    return Array.isArray(optionFilterProp) ? optionFilterProp : [optionFilterProp];
  }, [optionFilterProp]);
  const mergedId = useId(id);
  const multiple = isMultiple(mode);
  const childrenAsData = !!(!options && children);
  const mergedFilterOption = reactExports.useMemo(() => {
    if (filterOption === void 0 && mode === "combobox") {
      return false;
    }
    return filterOption;
  }, [filterOption, mode]);
  const mergedFieldNames = reactExports.useMemo(
    () => fillFieldNames(fieldNames, childrenAsData),
    /* eslint-disable react-hooks/exhaustive-deps */
    [
      // We stringify fieldNames to avoid unnecessary re-renders.
      JSON.stringify(fieldNames),
      childrenAsData
    ]
    /* eslint-enable react-hooks/exhaustive-deps */
  );
  const [internalSearchValue, setSearchValue] = useControlledState("", searchValue);
  const mergedSearchValue = internalSearchValue || "";
  const parsedOptions = useOptions(options, children, mergedFieldNames, normalizedOptionFilterProp, optionLabelProp);
  const {
    valueOptions,
    labelOptions,
    options: mergedOptions
  } = parsedOptions;
  const convert2LabelValues = reactExports.useCallback((draftValues) => {
    const valueList = toArray(draftValues);
    return valueList.map((val) => {
      let rawValue;
      let rawLabel;
      let rawDisabled;
      let rawTitle;
      if (isRawValue(val)) {
        rawValue = val;
      } else {
        rawLabel = val.label;
        rawValue = val.value;
      }
      const option = valueOptions.get(rawValue);
      if (option) {
        if (rawLabel === void 0) rawLabel = option?.[optionLabelProp || mergedFieldNames.label];
        rawDisabled = option?.disabled;
        rawTitle = option?.title;
      }
      return {
        label: rawLabel,
        value: rawValue,
        key: rawValue,
        disabled: rawDisabled,
        title: rawTitle
      };
    });
  }, [mergedFieldNames, optionLabelProp, valueOptions]);
  const [internalValue, setInternalValue] = useControlledState(defaultValue, value);
  const rawLabeledValues = reactExports.useMemo(() => {
    const newInternalValue = multiple && internalValue === null ? [] : internalValue;
    const values = convert2LabelValues(newInternalValue);
    if (mode === "combobox" && isComboNoValue(values[0]?.value)) {
      return [];
    }
    return values;
  }, [internalValue, convert2LabelValues, mode, multiple]);
  const [mergedValues, getMixedOption] = useCache(rawLabeledValues, valueOptions);
  const displayValues = reactExports.useMemo(() => {
    if (!mode && mergedValues.length === 1) {
      const firstValue = mergedValues[0];
      if (firstValue.value === null && (firstValue.label === null || firstValue.label === void 0)) {
        return [];
      }
    }
    return mergedValues.map((item) => ({
      ...item,
      label: (typeof labelRender === "function" ? labelRender(item) : item.label) ?? item.value
    }));
  }, [mode, mergedValues, labelRender]);
  const rawValues = reactExports.useMemo(() => new Set(mergedValues.map((val) => val.value)), [mergedValues]);
  reactExports.useEffect(() => {
    if (mode === "combobox") {
      const strValue = mergedValues[0]?.value;
      setSearchValue(hasValue(strValue) ? String(strValue) : "");
    }
  }, [mergedValues]);
  const createTagOption = useRefFunc((val, label) => {
    const mergedLabel = label ?? val;
    return {
      [mergedFieldNames.value]: val,
      [mergedFieldNames.label]: mergedLabel
    };
  });
  const filledTagOptions = reactExports.useMemo(() => {
    if (mode !== "tags") {
      return mergedOptions;
    }
    const cloneOptions = [...mergedOptions];
    const existOptions = (val) => valueOptions.has(val);
    [...mergedValues].sort((a, b) => a.value < b.value ? -1 : 1).forEach((item) => {
      const val = item.value;
      if (!existOptions(val)) {
        cloneOptions.push(createTagOption(val, item.label));
      }
    });
    return cloneOptions;
  }, [createTagOption, mergedOptions, valueOptions, mergedValues, mode]);
  const filteredOptions = useFilterOptions(filledTagOptions, mergedFieldNames, mergedSearchValue, mergedFilterOption, normalizedOptionFilterProp);
  const filledSearchOptions = reactExports.useMemo(() => {
    const hasItemMatchingSearch = (item) => {
      if (normalizedOptionFilterProp.length) {
        return normalizedOptionFilterProp.some((prop) => item?.[prop] === mergedSearchValue);
      }
      return item?.value === mergedSearchValue;
    };
    if (mode !== "tags" || !mergedSearchValue || filteredOptions.some((item) => hasItemMatchingSearch(item))) {
      return filteredOptions;
    }
    if (filteredOptions.some((item) => item[mergedFieldNames.value] === mergedSearchValue)) {
      return filteredOptions;
    }
    if (valueOptions.get(mergedSearchValue)?.disabled) {
      return filteredOptions;
    }
    return [createTagOption(mergedSearchValue), ...filteredOptions];
  }, [createTagOption, normalizedOptionFilterProp, mode, filteredOptions, mergedSearchValue, mergedFieldNames, valueOptions]);
  const sorter = (inputOptions) => {
    const sortedOptions = [...inputOptions].sort((a, b) => filterSort(a, b, {
      searchValue: mergedSearchValue
    }));
    return sortedOptions.map((item) => {
      if (Array.isArray(item.options)) {
        return {
          ...item,
          options: item.options.length > 0 ? sorter(item.options) : item.options
        };
      }
      return item;
    });
  };
  const orderedFilteredOptions = reactExports.useMemo(() => {
    if (!filterSort) {
      return filledSearchOptions;
    }
    return sorter(filledSearchOptions);
  }, [filledSearchOptions, filterSort, mergedSearchValue]);
  const displayOptions = reactExports.useMemo(() => flattenOptions(orderedFilteredOptions, {
    fieldNames: mergedFieldNames,
    childrenAsData
  }), [orderedFilteredOptions, mergedFieldNames, childrenAsData]);
  const triggerChange = (values) => {
    const labeledValues = convert2LabelValues(values);
    setInternalValue(labeledValues);
    if (onChange && // Trigger event only when value changed
    (labeledValues.length !== mergedValues.length || labeledValues.some((newVal, index) => mergedValues[index]?.value !== newVal?.value))) {
      const returnValues = labelInValue ? labeledValues.map(({
        label: l,
        value: v
      }) => ({
        label: l,
        value: v
      })) : labeledValues.map((v) => v.value);
      const returnOptions = labeledValues.map((v) => injectPropsWithOption(getMixedOption(v.value)));
      onChange(
        // Value
        multiple ? returnValues : returnValues[0],
        // Option
        multiple ? returnOptions : returnOptions[0]
      );
    }
  };
  const [activeValue, setActiveValue] = reactExports.useState(null);
  const [accessibilityIndex, setAccessibilityIndex] = reactExports.useState(0);
  const mergedDefaultActiveFirstOption = defaultActiveFirstOption !== void 0 ? defaultActiveFirstOption : mode !== "combobox";
  const activeEventRef = reactExports.useRef(void 0);
  const onActiveValue = reactExports.useCallback((active, index, {
    source = "keyboard"
  } = {}) => {
    setAccessibilityIndex(index);
    if (backfill && mode === "combobox" && active !== null && source === "keyboard") {
      setActiveValue(String(active));
    }
    const promise = Promise.resolve().then(() => {
      if (activeEventRef.current === promise) {
        onActive?.(active);
      }
    });
    activeEventRef.current = promise;
  }, [backfill, mode, onActive]);
  const triggerSelect = (val, selected, type) => {
    const getSelectEnt = () => {
      const option = getMixedOption(val);
      return [labelInValue ? {
        label: option?.[mergedFieldNames.label],
        value: val
      } : val, injectPropsWithOption(option)];
    };
    if (selected && onSelect) {
      const [wrappedValue, option] = getSelectEnt();
      onSelect(wrappedValue, option);
    } else if (!selected && onDeselect && type !== "clear") {
      const [wrappedValue, option] = getSelectEnt();
      onDeselect(wrappedValue, option);
    }
  };
  const onInternalSelect = useRefFunc((val, info) => {
    let cloneValues;
    const mergedSelect = multiple ? info.selected : true;
    if (mergedSelect) {
      cloneValues = multiple ? [...mergedValues, val] : [val];
    } else {
      cloneValues = mergedValues.filter((v) => v.value !== val);
    }
    triggerChange(cloneValues);
    triggerSelect(val, mergedSelect);
    if (mode === "combobox") {
      setActiveValue("");
    } else if (!isMultiple || autoClearSearchValue) {
      setSearchValue("");
      setActiveValue("");
    }
  });
  const onDisplayValuesChange = (nextValues, info) => {
    triggerChange(nextValues);
    const {
      type,
      values
    } = info;
    if (type === "remove" || type === "clear") {
      values.forEach((item) => {
        triggerSelect(item.value, false, type);
      });
    }
  };
  const onInternalSearch = (searchText, info) => {
    setSearchValue(searchText);
    setActiveValue(null);
    if (info.source === "submit") {
      const formatted = (searchText || "").trim();
      if (formatted) {
        if (valueOptions.get(formatted)?.disabled) {
          setSearchValue("");
          return;
        }
        const newRawValues = Array.from(/* @__PURE__ */ new Set([...rawValues, formatted]));
        triggerChange(newRawValues);
        triggerSelect(formatted, true);
        setSearchValue("");
      }
      return;
    }
    if (info.source !== "blur") {
      if (mode === "combobox") {
        triggerChange(searchText);
      }
      onSearch?.(searchText);
    }
  };
  const onInternalSearchSplit = (words) => {
    let patchValues = words;
    if (mode !== "tags") {
      patchValues = words.map((word) => {
        const opt = labelOptions.get(word);
        return opt?.value;
      }).filter((val) => val !== void 0);
    }
    if (mode === "tags") {
      patchValues = patchValues.filter((val) => !valueOptions.get(val)?.disabled);
    }
    const newRawValues = Array.from(/* @__PURE__ */ new Set([...rawValues, ...patchValues]));
    triggerChange(newRawValues);
    newRawValues.forEach((newRawValue) => {
      triggerSelect(newRawValue, true);
    });
  };
  const selectContext = reactExports.useMemo(() => {
    const realVirtual = virtual !== false && popupMatchSelectWidth !== false;
    return {
      ...parsedOptions,
      flattenOptions: displayOptions,
      onActiveValue,
      defaultActiveFirstOption: mergedDefaultActiveFirstOption,
      onSelect: onInternalSelect,
      menuItemSelectedIcon,
      rawValues,
      fieldNames: mergedFieldNames,
      virtual: realVirtual,
      direction,
      listHeight,
      listItemHeight,
      childrenAsData,
      maxCount,
      optionRender,
      classNames,
      styles
    };
  }, [maxCount, parsedOptions, displayOptions, onActiveValue, mergedDefaultActiveFirstOption, onInternalSelect, menuItemSelectedIcon, rawValues, mergedFieldNames, virtual, popupMatchSelectWidth, direction, listHeight, listItemHeight, childrenAsData, optionRender, classNames, styles]);
  return /* @__PURE__ */ reactExports.createElement(SelectContext.Provider, {
    value: selectContext
  }, /* @__PURE__ */ reactExports.createElement(BaseSelect, _extends$2({}, restProps, {
    // >>> MISC
    id: mergedId,
    prefixCls,
    ref,
    omitDomProps: OMIT_DOM_PROPS,
    mode,
    classNames,
    styles,
    displayValues,
    onDisplayValuesChange,
    maxCount,
    direction,
    showSearch: mergedShowSearch,
    searchValue: mergedSearchValue,
    onSearch: onInternalSearch,
    autoClearSearchValue,
    onSearchSplit: onInternalSearchSplit,
    popupMatchSelectWidth,
    OptionList: RefOptionList,
    emptyOptions: !displayOptions.length,
    activeValue,
    activeDescendantId: `${mergedId}_list_${accessibilityIndex}`
  })));
});
const TypedSelect = Select$1;
TypedSelect.Option = Option;
TypedSelect.OptGroup = OptGroup;
const normalizeIcon = (value, key, fallback) => {
  if (value === false) {
    return null;
  }
  if (value === true) {
    return fallback;
  }
  if (value && key && value[key] !== void 0) {
    return value[key];
  }
  return fallback;
};
const getAsSolidColor = (color, background) => {
  if (color?.startsWith("var(") || background?.startsWith("var(")) {
    return color;
  }
  return new FastColor(color).onBackground(background).toHexString();
};
const Empty$1 = () => {
  const [, token] = useToken();
  const [locale] = useLocale("Empty");
  const {
    colorBgContainer,
    colorFill,
    colorFillSecondary,
    colorFillTertiary,
    colorTextQuaternary
  } = token;
  const {
    panelBgColor,
    borderColor,
    detailColor,
    shadowColor,
    iconColor
  } = reactExports.useMemo(() => ({
    panelBgColor: getAsSolidColor(colorFillTertiary, colorBgContainer),
    borderColor: getAsSolidColor(colorTextQuaternary, colorBgContainer),
    detailColor: getAsSolidColor(colorFill, colorBgContainer),
    shadowColor: getAsSolidColor(colorFillSecondary, colorBgContainer),
    iconColor: colorBgContainer
  }), [colorBgContainer, colorFill, colorFillSecondary, colorFillTertiary, colorTextQuaternary]);
  return /* @__PURE__ */ reactExports.createElement("svg", {
    width: "184",
    height: "152",
    viewBox: "0 0 184 152",
    xmlns: "http://www.w3.org/2000/svg"
  }, /* @__PURE__ */ reactExports.createElement("title", null, locale?.description || "Empty"), /* @__PURE__ */ reactExports.createElement("g", {
    fill: "none",
    fillRule: "evenodd"
  }, /* @__PURE__ */ reactExports.createElement("g", {
    transform: "translate(24 31.7)"
  }, /* @__PURE__ */ reactExports.createElement("ellipse", {
    fillOpacity: ".8",
    fill: shadowColor,
    cx: "67.8",
    cy: "106.9",
    rx: "67.8",
    ry: "12.7"
  }), /* @__PURE__ */ reactExports.createElement("path", {
    fill: borderColor,
    d: "M122 69.7 98.1 40.2a6 6 0 0 0-4.6-2.2H42.1a6 6 0 0 0-4.6 2.2l-24 29.5V85H122z"
  }), /* @__PURE__ */ reactExports.createElement("path", {
    fill: panelBgColor,
    d: "M33.8 0h68a4 4 0 0 1 4 4v93.3a4 4 0 0 1-4 4h-68a4 4 0 0 1-4-4V4a4 4 0 0 1 4-4"
  }), /* @__PURE__ */ reactExports.createElement("path", {
    fill: detailColor,
    d: "M42.7 10h50.2a2 2 0 0 1 2 2v25a2 2 0 0 1-2 2H42.7a2 2 0 0 1-2-2V12a2 2 0 0 1 2-2m.2 39.8h49.8a2.3 2.3 0 1 1 0 4.5H42.9a2.3 2.3 0 0 1 0-4.5m0 11.7h49.8a2.3 2.3 0 1 1 0 4.6H42.9a2.3 2.3 0 0 1 0-4.6m79 43.5a7 7 0 0 1-6.8 5.4H20.5a7 7 0 0 1-6.7-5.4l-.2-1.8V69.7h26.3c2.9 0 5.2 2.4 5.2 5.4s2.4 5.4 5.3 5.4h34.8c2.9 0 5.3-2.4 5.3-5.4s2.3-5.4 5.2-5.4H122v33.5q0 1-.2 1.8"
  })), /* @__PURE__ */ reactExports.createElement("path", {
    fill: detailColor,
    d: "m149.1 33.3-6.8 2.6a1 1 0 0 1-1.3-1.2l2-6.2q-4.1-4.5-4.2-10.4c0-10 10.1-18.1 22.6-18.1S184 8.1 184 18.1s-10.1 18-22.6 18q-6.8 0-12.3-2.8"
  }), /* @__PURE__ */ reactExports.createElement("g", {
    fill: iconColor,
    transform: "translate(149.7 15.4)"
  }, /* @__PURE__ */ reactExports.createElement("circle", {
    cx: "20.7",
    cy: "3.2",
    r: "2.8"
  }), /* @__PURE__ */ reactExports.createElement("path", {
    d: "M5.7 5.6H0L2.9.7zM9.3.7h5v5h-5z"
  }))));
};
const Simple = () => {
  const [, token] = useToken();
  const [locale] = useLocale("Empty");
  const {
    colorFill,
    colorFillTertiary,
    colorFillQuaternary,
    colorBgContainer
  } = token;
  const {
    borderColor,
    shadowColor,
    contentColor
  } = reactExports.useMemo(() => ({
    borderColor: getAsSolidColor(colorFill, colorBgContainer),
    shadowColor: getAsSolidColor(colorFillTertiary, colorBgContainer),
    contentColor: getAsSolidColor(colorFillQuaternary, colorBgContainer)
  }), [colorFill, colorFillTertiary, colorFillQuaternary, colorBgContainer]);
  return /* @__PURE__ */ reactExports.createElement("svg", {
    width: "64",
    height: "41",
    viewBox: "0 0 64 41",
    xmlns: "http://www.w3.org/2000/svg"
  }, /* @__PURE__ */ reactExports.createElement("title", null, locale?.description || "Empty"), /* @__PURE__ */ reactExports.createElement("g", {
    transform: "translate(0 1)",
    fill: "none",
    fillRule: "evenodd"
  }, /* @__PURE__ */ reactExports.createElement("ellipse", {
    fill: shadowColor,
    cx: "32",
    cy: "33",
    rx: "32",
    ry: "7"
  }), /* @__PURE__ */ reactExports.createElement("g", {
    fillRule: "nonzero",
    stroke: borderColor
  }, /* @__PURE__ */ reactExports.createElement("path", {
    d: "M55 12.8 44.9 1.3Q44 0 42.9 0H21.1q-1.2 0-2 1.3L9 12.8V22h46z"
  }), /* @__PURE__ */ reactExports.createElement("path", {
    d: "M41.6 16c0-1.7 1-3 2.2-3H55v18.1c0 2.2-1.3 3.9-3 3.9H12c-1.7 0-3-1.7-3-3.9V13h11.2c1.2 0 2.2 1.3 2.2 3s1 2.9 2.2 2.9h14.8c1.2 0 2.2-1.4 2.2-3",
    fill: contentColor
  }))));
};
const genSharedEmptyStyle = (token) => {
  const {
    componentCls,
    margin,
    marginXS,
    marginXL,
    fontSize,
    lineHeight
  } = token;
  return {
    [componentCls]: {
      marginInline: marginXS,
      fontSize,
      lineHeight,
      textAlign: "center",
      // 原来 &-image 没有父子结构，现在为了外层承担我们的 hashId，改成父子结构
      [`${componentCls}-image`]: {
        height: token.emptyImgHeight,
        marginBottom: marginXS,
        opacity: token.opacityImage,
        img: {
          height: "100%"
        },
        svg: {
          maxWidth: "100%",
          height: "100%",
          margin: "auto"
        }
      },
      [`${componentCls}-description`]: {
        color: token.colorTextDescription
      },
      // 原来 &-footer 没有父子结构，现在为了外层承担我们的 hashId，改成父子结构
      [`${componentCls}-footer`]: {
        marginTop: margin
      },
      "&-normal": {
        marginBlock: marginXL,
        color: token.colorTextDescription,
        [`${componentCls}-description`]: {
          color: token.colorTextDescription
        },
        [`${componentCls}-image`]: {
          height: token.emptyImgHeightMD
        }
      },
      "&-small": {
        marginBlock: marginXS,
        color: token.colorTextDescription,
        [`${componentCls}-image`]: {
          height: token.emptyImgHeightSM
        }
      }
    }
  };
};
const useStyle = genStyleHooks("Empty", (token) => {
  const {
    componentCls,
    controlHeightLG,
    calc
  } = token;
  const emptyToken = merge(token, {
    emptyImgCls: `${componentCls}-img`,
    emptyImgHeight: calc(controlHeightLG).mul(2.5).equal(),
    emptyImgHeightMD: controlHeightLG,
    emptyImgHeightSM: calc(controlHeightLG).mul(0.875).equal()
  });
  return genSharedEmptyStyle(emptyToken);
});
const defaultEmptyImg = /* @__PURE__ */ reactExports.createElement(Empty$1, null);
const simpleEmptyImg = /* @__PURE__ */ reactExports.createElement(Simple, null);
const Empty = /* @__PURE__ */ reactExports.forwardRef((props, ref) => {
  const {
    className,
    rootClassName,
    prefixCls: customizePrefixCls,
    image,
    description,
    children,
    imageStyle,
    style,
    classNames,
    styles,
    ...restProps
  } = props;
  const {
    getPrefixCls,
    direction,
    className: contextClassName,
    style: contextStyle,
    classNames: contextClassNames,
    styles: contextStyles,
    image: contextImage
  } = useComponentConfig("empty");
  const prefixCls = getPrefixCls("empty", customizePrefixCls);
  const [hashId, cssVarCls] = useStyle(prefixCls);
  const contextStyleRoot = useSemanticRootStyle(contextStyle);
  const styleRoot = useSemanticRootStyle(style);
  const [mergedClassNames, mergedStyles] = useMergeSemantic([contextClassNames, classNames], [contextStyles, contextStyleRoot, styles, styleRoot], {
    props
  });
  const [locale] = useLocale("Empty");
  const des = typeof description !== "undefined" ? description : locale?.description;
  const alt = typeof des === "string" ? des : "empty";
  const mergedImage = image ?? contextImage ?? defaultEmptyImg;
  let imageNode = null;
  if (typeof mergedImage === "string") {
    imageNode = /* @__PURE__ */ reactExports.createElement("img", {
      draggable: false,
      alt,
      src: mergedImage
    });
  } else {
    imageNode = mergedImage;
  }
  const nativeElementRef = reactExports.useRef(null);
  reactExports.useImperativeHandle(ref, () => ({
    nativeElement: nativeElementRef.current
  }));
  return /* @__PURE__ */ reactExports.createElement("div", {
    ref: nativeElementRef,
    className: clsx(hashId, cssVarCls, prefixCls, contextClassName, {
      [`${prefixCls}-normal`]: mergedImage === simpleEmptyImg,
      [`${prefixCls}-rtl`]: direction === "rtl"
    }, className, rootClassName, mergedClassNames.root),
    style: mergedStyles.root,
    ...restProps
  }, /* @__PURE__ */ reactExports.createElement("div", {
    className: clsx(`${prefixCls}-image`, mergedClassNames.image),
    style: {
      ...imageStyle,
      ...mergedStyles.image
    }
  }, imageNode), des && /* @__PURE__ */ reactExports.createElement("div", {
    className: clsx(`${prefixCls}-description`, mergedClassNames.description),
    style: mergedStyles.description
  }, des), children && /* @__PURE__ */ reactExports.createElement("div", {
    className: clsx(`${prefixCls}-footer`, mergedClassNames.footer),
    style: mergedStyles.footer
  }, children));
});
Empty.PRESENTED_IMAGE_DEFAULT = defaultEmptyImg;
Empty.PRESENTED_IMAGE_SIMPLE = simpleEmptyImg;
const DefaultRenderEmpty = (props) => {
  const {
    componentName
  } = props;
  const {
    getPrefixCls
  } = reactExports.useContext(ConfigContext);
  const prefix = getPrefixCls("empty");
  switch (componentName) {
    case "Table":
    case "List":
      return /* @__PURE__ */ React.createElement(Empty, {
        image: Empty.PRESENTED_IMAGE_SIMPLE
      });
    case "Select":
    case "TreeSelect":
    case "Cascader":
    case "Transfer":
    case "Mentions":
      return /* @__PURE__ */ React.createElement(Empty, {
        image: Empty.PRESENTED_IMAGE_SIMPLE,
        className: `${prefix}-small`
      });
    /**
     * This type of component should satisfy the nullish coalescing operator(??) on the left-hand side.
     * to let the component itself implement the logic.
     * For example `Table.filter`.
     */
    case "Table.filter":
      return null;
    default:
      return /* @__PURE__ */ React.createElement(Empty, null);
  }
};
const getBuiltInPlacements = (popupOverflow) => {
  const htmlRegion = popupOverflow === "scroll" ? "scroll" : "visible";
  const sharedConfig = {
    overflow: {
      adjustX: true,
      adjustY: true,
      shiftY: true
    },
    htmlRegion,
    dynamicInset: true
  };
  return {
    bottomLeft: {
      ...sharedConfig,
      points: ["tl", "bl"],
      offset: [0, 4]
    },
    bottomRight: {
      ...sharedConfig,
      points: ["tr", "br"],
      offset: [0, 4]
    },
    topLeft: {
      ...sharedConfig,
      points: ["bl", "tl"],
      offset: [0, -4]
    },
    topRight: {
      ...sharedConfig,
      points: ["br", "tr"],
      offset: [0, -4]
    }
  };
};
function mergedBuiltinPlacements(buildInPlacements, popupOverflow) {
  return buildInPlacements || getBuiltInPlacements(popupOverflow);
}
const genItemStyle = (token) => {
  const {
    optionHeight,
    optionFontSize,
    optionLineHeight,
    optionPadding
  } = token;
  return {
    position: "relative",
    display: "block",
    minHeight: optionHeight,
    padding: optionPadding,
    color: token.colorText,
    fontWeight: "normal",
    fontSize: optionFontSize,
    lineHeight: optionLineHeight,
    boxSizing: "border-box"
  };
};
const genSingleStyle = (token) => {
  const {
    antCls,
    componentCls
  } = token;
  const selectItemCls = `${componentCls}-item`;
  const slideUpEnterActive = `&${antCls}-slide-up-enter${antCls}-slide-up-enter-active`;
  const slideUpAppearActive = `&${antCls}-slide-up-appear${antCls}-slide-up-appear-active`;
  const slideUpLeaveActive = `&${antCls}-slide-up-leave${antCls}-slide-up-leave-active`;
  const dropdownPlacementCls = `${componentCls}-dropdown-placement-`;
  const selectedItemCls = `${selectItemCls}-option-selected`;
  return [
    {
      [`${componentCls}-dropdown`]: {
        // ========================== Popup ==========================
        ...resetComponent(token),
        position: "absolute",
        top: -9999,
        zIndex: token.zIndexPopup,
        boxSizing: "border-box",
        padding: token.paddingXXS,
        overflow: "hidden",
        fontSize: token.fontSize,
        // Fix select render lag of long text in chrome
        // https://github.com/ant-design/ant-design/issues/11456
        // https://github.com/ant-design/ant-design/issues/11843
        fontVariant: "initial",
        backgroundColor: token.colorBgElevated,
        borderRadius: token.borderRadiusLG,
        outline: "none",
        boxShadow: token.boxShadowSecondary,
        [`
          ${slideUpEnterActive}${dropdownPlacementCls}bottomLeft,
          ${slideUpAppearActive}${dropdownPlacementCls}bottomLeft
        `]: {
          animationName: slideUpIn
        },
        [`
          ${slideUpEnterActive}${dropdownPlacementCls}topLeft,
          ${slideUpAppearActive}${dropdownPlacementCls}topLeft,
          ${slideUpEnterActive}${dropdownPlacementCls}topRight,
          ${slideUpAppearActive}${dropdownPlacementCls}topRight
        `]: {
          animationName: slideDownIn
        },
        [`${slideUpLeaveActive}${dropdownPlacementCls}bottomLeft`]: {
          animationName: slideUpOut
        },
        [`
          ${slideUpLeaveActive}${dropdownPlacementCls}topLeft,
          ${slideUpLeaveActive}${dropdownPlacementCls}topRight
        `]: {
          animationName: slideDownOut
        },
        "&-hidden": {
          display: "none"
        },
        [`${componentCls}-dropdown-list-scrollbar`]: {
          cursor: "pointer",
          "&:hover": {
            backgroundColor: token.colorFillQuaternary
          }
        },
        [selectItemCls]: {
          ...genItemStyle(token),
          cursor: "pointer",
          transition: `background-color ${token.motionDurationSlow} ease`,
          borderRadius: token.borderRadiusSM,
          // =========== Group ============
          "&-group": {
            color: token.colorTextDescription,
            fontSize: token.fontSizeSM,
            cursor: "default"
          },
          // =========== Option ===========
          "&-option": {
            display: "flex",
            "&-content": {
              flex: "auto",
              ...textEllipsis
            },
            "&-state": {
              flex: "none",
              display: "flex",
              alignItems: "center"
            },
            [`&-selected:not(${selectItemCls}-option-disabled)`]: {
              color: token.optionSelectedColor,
              fontWeight: token.optionSelectedFontWeight,
              backgroundColor: token.optionSelectedBg,
              [`${selectItemCls}-option-state`]: {
                color: token.colorPrimary
              }
            },
            [`&-active:not(${selectItemCls}-option-disabled)`]: {
              backgroundColor: token.optionActiveBg
            },
            [`&-selected${selectItemCls}-option-active:not(${selectItemCls}-option-disabled)`]: {
              backgroundColor: token.controlItemBgActiveHover
            },
            "&-disabled": {
              [`&${selectItemCls}-option-selected`]: {
                backgroundColor: token.colorBgContainerDisabled
              },
              color: token.colorTextDisabled,
              cursor: "not-allowed"
            },
            "&-grouped": {
              paddingInlineStart: token.calc(token.controlPaddingHorizontal).mul(2).equal()
            }
          },
          "&-empty": {
            ...genItemStyle(token),
            color: token.colorTextDisabled
          }
        },
        // https://github.com/ant-design/ant-design/pull/46646
        [`${selectedItemCls}:has(+ ${selectedItemCls})`]: {
          borderEndStartRadius: 0,
          borderEndEndRadius: 0,
          [`& + ${selectedItemCls}`]: {
            borderStartStartRadius: 0,
            borderStartEndRadius: 0
          }
        },
        // =========================== RTL ===========================
        "&-rtl": {
          direction: "rtl"
        }
      }
    },
    // Follow code may reuse in other components
    initSlideMotion(token, "slide-up"),
    initSlideMotion(token, "slide-down"),
    initMoveMotion(token, "move-up"),
    initMoveMotion(token, "move-down")
  ];
};
const genSelectInputCustomizeStyle = (token) => {
  const {
    antCls,
    componentCls
  } = token;
  const transparentBackground = {
    background: "transparent"
  };
  const disabledCustomizedInputSelector = ["> input[disabled]", "> textarea[disabled]", `> ${componentCls}-input`, `> ${antCls}-input-affix-wrapper-disabled`, `> ${antCls}-input-search`].join(", ");
  return {
    [`&${componentCls}-customize`]: {
      border: 0,
      padding: 0,
      fontSize: "inherit",
      lineHeight: "inherit",
      [`${componentCls}-placeholder`]: {
        display: "none"
      },
      [`${componentCls}-content`]: {
        margin: 0,
        padding: 0,
        "&-value": {
          display: "none"
        }
      },
      [`&${componentCls}-filled ${componentCls}-content`]: {
        [`${antCls}-input-filled`]: transparentBackground
      },
      [`&${componentCls}-disabled ${componentCls}-content`]: {
        [disabledCustomizedInputSelector]: transparentBackground,
        "input[disabled], textarea[disabled]": transparentBackground
      }
    }
  };
};
const FIXED_INPUT_MIN_WIDTH = 4;
const genSelectInputMultipleStyle = (token) => {
  const {
    componentCls,
    calc,
    iconCls,
    paddingXS,
    paddingXXS,
    INTERNAL_FIXED_ITEM_MARGIN,
    lineWidth,
    lineType,
    colorIcon,
    colorIconHover,
    inputPaddingHorizontalBase,
    antCls
  } = token;
  const [varName, varRef] = genCssVar(antCls, "select");
  return {
    "&-multiple": {
      [varName("multi-item-background")]: token.multipleItemBg,
      [varName("multi-item-border-color")]: "transparent",
      [varName("multi-item-border-radius")]: token.borderRadiusSM,
      [varName("multi-item-height")]: token.multipleItemHeight,
      [varName("multi-padding-base")]: `calc((${varRef("height")} - ${varRef("multi-item-height")}) / 2)`,
      [varName("multi-padding-vertical")]: `calc(${varRef("multi-padding-base")} - ${INTERNAL_FIXED_ITEM_MARGIN} - ${lineWidth})`,
      [varName("multi-item-padding-horizontal")]: `calc(${inputPaddingHorizontalBase} - ${varRef("multi-padding-vertical")} - ${lineWidth} * 2)`,
      // ========================================================
      // ==                        Base                        ==
      // ========================================================
      // ========================= Root =========================
      paddingBlock: varRef("multi-padding-vertical"),
      paddingInlineStart: `calc(${varRef("multi-padding-base")} - ${lineWidth})`,
      // ======================== Prefix ========================
      [`${componentCls}-prefix`]: {
        marginInlineStart: varRef("multi-item-padding-horizontal")
      },
      [`${componentCls}-prefix + ${componentCls}-content`]: {
        [`${componentCls}-placeholder`]: {
          insetInlineStart: 0
        },
        [`${componentCls}-content-item${componentCls}-content-item-suffix`]: {
          marginInlineStart: 0
        }
      },
      // ===================== Placeholder ======================
      [`${componentCls}-placeholder`]: {
        position: "absolute",
        lineHeight: varRef("line-height"),
        insetInlineStart: varRef("multi-item-padding-horizontal"),
        width: `calc(100% - ${varRef("multi-item-padding-horizontal")})`,
        top: "50%",
        transform: "translateY(-50%)"
      },
      // ======================= Content ========================
      [`${componentCls}-content`]: {
        flexWrap: "wrap",
        alignItems: "center",
        lineHeight: 1,
        "&-item-prefix": {
          height: varRef("font-size")
        },
        "&-item": {
          lineHeight: 1,
          maxWidth: `calc(100% - ${FIXED_INPUT_MIN_WIDTH}px)`
        },
        [`${componentCls}-content-item-prefix + ${componentCls}-content-item-suffix,
          ${componentCls}-content-item-suffix:first-child`]: {
          marginInlineStart: varRef("multi-item-padding-horizontal")
        },
        [`${componentCls}-selection-item`]: {
          lineHeight: `calc(${varRef("multi-item-height")} - ${lineWidth} * 2)`,
          border: `${lineWidth} ${lineType} ${varRef("multi-item-border-color")}`,
          display: "flex",
          marginBlock: INTERNAL_FIXED_ITEM_MARGIN,
          marginInlineEnd: calc(INTERNAL_FIXED_ITEM_MARGIN).mul(2).equal(),
          background: varRef("multi-item-background"),
          borderRadius: varRef("multi-item-border-radius"),
          paddingInlineStart: paddingXS,
          paddingInlineEnd: paddingXXS,
          transition: ["height", "line-height", "padding"].map((key) => `${key} ${token.motionDurationSlow}`).join(","),
          // >>> Content
          "&-content": {
            ...textEllipsis,
            marginInlineEnd: paddingXXS
          },
          // >>> Remove
          "&-remove": {
            ...resetIcon(),
            display: "inline-flex",
            alignItems: "center",
            color: colorIcon,
            fontWeight: "bold",
            fontSize: 10,
            lineHeight: "inherit",
            cursor: "pointer",
            [`> ${iconCls}`]: {
              verticalAlign: "-0.2em"
            },
            "&:hover": {
              color: colorIconHover
            }
          }
        },
        [`${componentCls}-input`]: {
          lineHeight: calc(INTERNAL_FIXED_ITEM_MARGIN).mul(2).add(varRef("multi-item-height")).equal(),
          width: `calc(var(--select-input-width, 0) * 1px)`,
          minWidth: FIXED_INPUT_MIN_WIDTH,
          maxWidth: "100%",
          transition: `line-height ${token.motionDurationSlow}`
        }
      },
      // ========================================================
      // ==                        Size                        ==
      // ========================================================
      [`&${componentCls}-sm`]: {
        [varName("multi-item-height")]: token.multipleItemHeightSM,
        [varName("multi-item-border-radius")]: token.borderRadiusXS
      },
      [`&${componentCls}-lg`]: {
        [varName("multi-item-height")]: token.multipleItemHeightLG,
        [varName("multi-item-border-radius")]: token.borderRadius
      },
      // ========================================================
      // ==                      Variants                      ==
      // ========================================================
      [`&${componentCls}-filled`]: {
        [varName("multi-item-border-color")]: token.colorSplit,
        [varName("multi-item-background")]: token.colorBgContainer,
        [`&${componentCls}-disabled`]: {
          [varName("multi-item-border-color")]: "transparent"
        }
      }
    }
  };
};
const genSelectInputVariableStyle = (token, colors) => {
  const {
    componentCls,
    antCls
  } = token;
  const [varName] = genCssVar(antCls, "select");
  const {
    border,
    borderHover,
    borderActive,
    borderOutline
  } = colors;
  const baseBG = colors.background || token.selectorBg || token.colorBgContainer;
  return {
    [varName("border-color")]: border,
    [varName("background-color")]: baseBG,
    [varName("affix-color")]: colors.affixColor,
    [`&:not(${componentCls}-disabled)`]: {
      "&:hover": {
        [varName("border-color")]: borderHover,
        [varName("background-color")]: colors.backgroundHover || baseBG
      },
      [`&${componentCls}-focused`]: {
        [varName("border-color")]: borderActive,
        [varName("background-color")]: colors.backgroundActive || baseBG,
        boxShadow: `0 0 0 ${unit(token.controlOutlineWidth)} ${borderOutline}`
      }
    },
    [`&${componentCls}-disabled`]: {
      [varName("border-color")]: colors.borderDisabled || colors.border,
      [varName("background-color")]: colors.backgroundDisabled || colors.background
    }
  };
};
const genSelectInputVariantStyle = (token, variant, colors, errorColors, warningColors, patchStyle) => {
  const {
    componentCls
  } = token;
  return {
    [`&${componentCls}-${variant}`]: [genSelectInputVariableStyle(token, colors), {
      [`&${componentCls}-status-error`]: genSelectInputVariableStyle(token, {
        ...colors,
        ...errorColors
      }),
      [`&${componentCls}-status-warning`]: genSelectInputVariableStyle(token, {
        ...colors,
        ...warningColors
      })
    }, patchStyle]
  };
};
const genSelectInputFocusVisibleStyle = (token, outlineColor) => ({
  outline: `${unit(token.lineWidthFocus)} ${token.lineType} ${outlineColor}`,
  outlineOffset: unit(token.calc(token.lineWidth).mul(-1).equal()),
  transition: [`outline-offset`, `outline`].map((prop) => `${prop} 0s`).join(", ")
});
const genSelectInputStyle = (token) => {
  const {
    componentCls,
    fontHeight,
    controlHeight,
    fontSizeIcon,
    showArrowPaddingInlineEnd,
    iconCls,
    antCls,
    max,
    calc
  } = token;
  const [varName, varRef] = genCssVar(antCls, "select");
  const contentMarginInlineEnd = max(calc(showArrowPaddingInlineEnd).sub(fontSizeIcon).equal(), 0);
  return {
    [componentCls]: [
      {
        // Border
        [varName("border-radius")]: token.borderRadius,
        [varName("border-color")]: "#000",
        [varName("border-size")]: token.lineWidth,
        // Background
        [varName("background-color")]: token.colorBgContainer,
        // Font
        [varName("font-size")]: token.fontSize,
        [varName("line-height")]: token.lineHeight,
        [varName("font-height")]: fontHeight,
        [varName("color")]: token.colorText,
        [varName("affix-color")]: token.colorText,
        // Size
        [varName("height")]: controlHeight,
        [varName("padding-horizontal")]: calc(token.paddingSM).sub(token.lineWidth).equal(),
        [varName("padding-vertical")]: `calc((${varRef("height")} - ${varRef("font-height")}) / 2 - ${varRef("border-size")})`,
        // ==========================================================
        // ==                         Base                         ==
        // ==========================================================
        ...resetComponent(token),
        display: "inline-flex",
        // gap: calc(token.paddingXXS).mul(1.5).equal(),
        flexWrap: "nowrap",
        position: "relative",
        transition: `all ${token.motionDurationSlow}`,
        alignItems: "flex-start",
        outline: 0,
        cursor: "pointer",
        // Border
        borderRadius: varRef("border-radius"),
        borderWidth: varRef("border-size"),
        borderStyle: token.lineType,
        borderColor: varRef("border-color"),
        // Background
        background: varRef("background-color"),
        // Font
        fontSize: varRef("font-size"),
        lineHeight: varRef("line-height"),
        color: varRef("color"),
        // Padding
        paddingInline: varRef("padding-horizontal"),
        paddingBlock: varRef("padding-vertical"),
        // ========================= Prefix =========================
        [`${componentCls}-prefix`]: {
          color: varRef("affix-color"),
          flex: "none",
          lineHeight: 1
        },
        // ====================== Placeholder =======================
        [`${componentCls}-placeholder`]: {
          ...textEllipsis,
          color: token.colorTextPlaceholder,
          pointerEvents: "none",
          zIndex: 1
        },
        // ======================== Content =========================
        [`${componentCls}-content`]: {
          flex: "auto",
          minWidth: 0,
          position: "relative",
          display: "flex",
          marginInlineEnd: contentMarginInlineEnd,
          "&:before": {
            content: '"\\a0"',
            width: 0,
            overflow: "hidden"
          },
          // >>> Value
          "&-value": {
            visibility: "inherit"
          },
          // >>> Input: should only take effect for not customize mode
          // input element with readOnly use cursor pointer
          "input[readonly]": {
            cursor: "inherit",
            caretColor: "transparent"
          }
        },
        // ========================= Suffix =========================
        [`${componentCls}-suffix`]: {
          flex: "none",
          color: token.colorTextQuaternary,
          fontSize: token.fontSizeIcon,
          lineHeight: 1,
          transition: ["opacity", "color"].map((prop) => `${prop} ${token.motionDurationMid} ease`).join(", "),
          "> :not(:last-child)": {
            marginInlineEnd: token.marginXS
          }
        },
        [`${componentCls}-prefix, ${componentCls}-suffix`]: {
          alignSelf: "center",
          [iconCls]: {
            verticalAlign: "top"
          }
        },
        // ==========================================================
        // ==                       Disabled                       ==
        // ==========================================================
        "&-disabled": {
          background: token.colorBgContainerDisabled,
          [varName("color")]: token.colorTextDisabled,
          cursor: "not-allowed",
          input: {
            cursor: "not-allowed"
          }
        },
        // ==========================================================
        // ==                         Size                         ==
        // ==========================================================
        "&-sm": {
          [varName("height")]: token.controlHeightSM,
          [varName("padding-horizontal")]: calc(token.paddingXS).sub(token.lineWidth).equal(),
          [varName("border-radius")]: token.borderRadiusSM,
          [`${componentCls}-clear`]: {
            insetInlineEnd: varRef("padding-horizontal")
          }
        },
        "&-lg": {
          [varName("height")]: token.controlHeightLG,
          [varName("font-size")]: token.fontSizeLG,
          [varName("line-height")]: token.lineHeightLG,
          [varName("font-height")]: token.fontHeightLG,
          [varName("border-radius")]: token.borderRadiusLG
        }
      },
      // ============================================================
      // ==                         Input                          ==
      // ============================================================
      {
        [`&:not(${componentCls}-customize)`]: {
          [`${componentCls}-input`]: {
            outline: "none",
            background: "transparent",
            appearance: "none",
            border: 0,
            margin: 0,
            padding: 0,
            color: varRef("color"),
            fontFamily: "inherit",
            fontSize: "inherit",
            "&::-webkit-search-cancel-button": {
              display: "none",
              appearance: "none"
            }
          }
        }
      },
      // ============================================================
      // ==                         Single                         ==
      // ============================================================
      {
        [`&-single:not(${componentCls}-customize)`]: {
          [`${componentCls}-input`]: {
            position: "absolute",
            inset: 0,
            lineHeight: "inherit"
          },
          // Content center align
          [`${componentCls}-content`]: {
            ...textEllipsis,
            alignSelf: "center",
            "&-has-value": {
              display: "block",
              "&:before": {
                display: "none"
              }
            },
            "&-has-search-value": {
              color: "transparent",
              [`> *:not(${componentCls}-input)`]: {
                opacity: 0
              }
            },
            // >>> Value
            "&-value": {
              transition: `all ${token.motionDurationMid} ${token.motionEaseInOut}`,
              zIndex: 1,
              opacity: 1
            }
          },
          // Dim the selected content while the dropdown is open. Shared by all select-like
          // components (Select / Cascader / TreeSelect) since they render through the same
          // `content` structure.
          [`&${componentCls}-open ${componentCls}-content`]: {
            "&-has-value": {
              opacity: 0.25
            },
            "&-has-search-value": {
              opacity: 1,
              transition: `opacity ${token.motionDurationMid} ${token.motionEaseInOut}`,
              color: "transparent",
              [`> *:not(${componentCls}-input)`]: {
                opacity: 0
              }
            }
          }
        }
      },
      // ======================== Show Search =======================
      {
        [`&-show-search:not(${componentCls}-customize-input):not(${componentCls}-disabled)`]: {
          cursor: "text"
        }
      },
      // ============================================================
      // ==                        Multiple                        ==
      // ============================================================
      genSelectInputMultipleStyle(token),
      // ========================= Variant ==========================
      // >>> Outlined
      genSelectInputVariantStyle(
        token,
        "outlined",
        {
          border: token.colorBorder,
          borderHover: token.hoverBorderColor,
          borderActive: token.activeBorderColor,
          borderOutline: token.activeOutlineColor,
          borderDisabled: token.colorBorderDisabled
        },
        // Error
        {
          border: token.colorError,
          borderHover: token.colorErrorBorderHover,
          borderActive: token.colorError,
          borderOutline: token.colorErrorOutline,
          affixColor: token.colorErrorAffix
        },
        // Warning
        {
          border: token.colorWarning,
          borderHover: token.colorWarningHover,
          borderActive: token.colorWarning,
          borderOutline: token.colorWarningOutline,
          affixColor: token.colorWarningAffix
        }
      ),
      // >>> Filled
      genSelectInputVariantStyle(
        token,
        "filled",
        {
          border: "transparent",
          borderHover: "transparent",
          borderActive: token.activeBorderColor,
          borderOutline: "transparent",
          borderDisabled: token.colorBorderDisabled,
          background: token.colorFillTertiary,
          backgroundHover: token.colorFillSecondary,
          backgroundActive: token.colorBgContainer
        },
        // Error
        {
          color: token.colorErrorText,
          background: token.colorErrorBg,
          backgroundHover: token.colorErrorBgHover,
          borderActive: token.colorError
        },
        // Warning
        {
          background: token.colorWarningBg,
          backgroundHover: token.colorWarningBgHover,
          borderActive: token.colorWarning
        }
      ),
      // >>> Borderless
      genSelectInputVariantStyle(token, "borderless", {
        border: "transparent",
        borderHover: "transparent",
        borderActive: "transparent",
        borderOutline: "transparent",
        background: "transparent"
      }, {}, {}, {
        [`&:not(${componentCls}-disabled):has(input:focus-visible), &:not(${componentCls}-disabled):has(textarea:focus-visible)`]: genSelectInputFocusVisibleStyle(token, token.activeBorderColor),
        [`&${componentCls}-status-error:not(${componentCls}-disabled):has(input:focus-visible), &${componentCls}-status-error:not(${componentCls}-disabled):has(textarea:focus-visible)`]: genSelectInputFocusVisibleStyle(token, token.colorError),
        [`&${componentCls}-status-warning:not(${componentCls}-disabled):has(input:focus-visible), &${componentCls}-status-warning:not(${componentCls}-disabled):has(textarea:focus-visible)`]: genSelectInputFocusVisibleStyle(token, token.colorWarning)
      }),
      // Underlined
      genSelectInputVariantStyle(
        token,
        "underlined",
        {
          border: token.colorBorder,
          borderHover: token.hoverBorderColor,
          borderActive: token.activeBorderColor,
          borderOutline: "transparent"
        },
        // Error
        {
          border: token.colorError,
          borderHover: token.colorErrorBorderHover,
          borderActive: token.colorError
        },
        // Warning
        {
          border: token.colorWarning,
          borderHover: token.colorWarningHover,
          borderActive: token.colorWarning
        },
        {
          borderRadius: 0,
          borderTopColor: "transparent",
          borderInlineColor: "transparent"
        }
      ),
      // ============================================================
      // ==                         Custom                         ==
      // ============================================================
      genSelectInputCustomizeStyle(token)
    ]
  };
};
const prepareComponentToken = (token) => {
  const {
    fontSize,
    lineHeight,
    lineWidth,
    lineWidthFocus,
    controlHeight,
    controlHeightSM,
    controlHeightLG,
    paddingXXS,
    controlPaddingHorizontal,
    zIndexPopupBase,
    colorText,
    fontWeightStrong,
    controlItemBgActive,
    controlItemBgHover,
    colorBgContainer,
    colorFillSecondary,
    colorBgContainerDisabled,
    colorTextDisabled,
    colorPrimaryHover,
    colorPrimary,
    controlOutline
  } = token;
  const dblPaddingXXS = paddingXXS * 2;
  const dblLineWidth = lineWidth * 2;
  const multipleItemHeight = Math.min(controlHeight - dblPaddingXXS, controlHeight - dblLineWidth);
  const multipleItemHeightSM = Math.min(controlHeightSM - dblPaddingXXS, controlHeightSM - dblLineWidth);
  const multipleItemHeightLG = Math.min(controlHeightLG - dblPaddingXXS, controlHeightLG - dblLineWidth);
  const INTERNAL_FIXED_ITEM_MARGIN = Math.floor(paddingXXS / 2);
  const componentToken = {
    lineWidthFocus: lineWidthFocus === 0 ? 0 : lineWidth,
    INTERNAL_FIXED_ITEM_MARGIN,
    zIndexPopup: zIndexPopupBase + 50,
    optionSelectedColor: colorText,
    optionSelectedFontWeight: fontWeightStrong,
    optionSelectedBg: controlItemBgActive,
    optionActiveBg: controlItemBgHover,
    optionPadding: `${(controlHeight - fontSize * lineHeight) / 2}px ${controlPaddingHorizontal}px`,
    optionFontSize: fontSize,
    optionLineHeight: lineHeight,
    optionHeight: controlHeight,
    selectorBg: colorBgContainer,
    clearBg: colorBgContainer,
    singleItemHeightLG: controlHeightLG,
    multipleItemBg: colorFillSecondary,
    multipleItemBorderColor: "transparent",
    multipleItemHeight,
    multipleItemHeightSM,
    multipleItemHeightLG,
    multipleSelectorBgDisabled: colorBgContainerDisabled,
    multipleItemColorDisabled: colorTextDisabled,
    multipleItemBorderColorDisabled: "transparent",
    showArrowPaddingInlineEnd: Math.ceil(token.fontSize * 1.25),
    hoverBorderColor: colorPrimaryHover,
    activeBorderColor: colorPrimary,
    activeOutlineColor: controlOutline,
    selectAffixPadding: paddingXXS
  };
  return componentToken;
};
const genBaseStyle = (token) => {
  const {
    antCls,
    componentCls,
    motionDurationMid,
    inputPaddingHorizontalBase
  } = token;
  const hoverShowClearStyle = {
    [`${componentCls}-clear`]: {
      opacity: 1
    },
    [`${componentCls}-suffix:not(:last-child)`]: {
      opacity: 0,
      pointerEvents: "none"
    },
    [`&${componentCls}-allow-clear:not(${componentCls}-show-arrow) ${componentCls}-content`]: {
      marginInlineEnd: token.showArrowPaddingInlineEnd
    }
  };
  return {
    [componentCls]: {
      ...resetComponent(token),
      // ======================== Selection ========================
      [`${componentCls}-selection-item`]: {
        flex: 1,
        fontWeight: "normal",
        position: "relative",
        userSelect: "none",
        ...textEllipsis,
        // https://github.com/ant-design/ant-design/issues/40421
        [`> ${antCls}-typography`]: {
          display: "inline"
        }
      },
      // ========================= Prefix ==========================
      [`${componentCls}-prefix`]: {
        flex: "none",
        marginInlineEnd: token.selectAffixPadding
      },
      // ========================== Clear ==========================
      [`${componentCls}-clear`]: {
        position: "absolute",
        top: "50%",
        insetInlineStart: "auto",
        insetInlineEnd: inputPaddingHorizontalBase,
        zIndex: 1,
        display: "inline-block",
        width: token.fontSizeIcon,
        height: token.fontSizeIcon,
        marginTop: token.calc(token.fontSizeIcon).mul(-1).div(2).equal(),
        padding: 0,
        background: "transparent",
        color: token.colorTextQuaternary,
        fontSize: token.fontSizeIcon,
        fontFamily: "inherit",
        fontStyle: "normal",
        lineHeight: 1,
        textAlign: "center",
        textTransform: "none",
        appearance: "none",
        border: 0,
        cursor: "pointer",
        opacity: 0,
        transition: ["color", "opacity"].map((prop) => `${prop} ${motionDurationMid} ease`).join(", "),
        textRendering: "auto",
        // https://github.com/ant-design/ant-design/issues/54205
        // Force GPU compositing on Safari to prevent flickering on opacity/transform transitions
        transform: "translateZ(0)",
        "&:before": {
          display: "block"
        },
        "&:hover": {
          color: token.colorIcon
        }
      },
      "@media(hover:none)": hoverShowClearStyle,
      "&:hover": hoverShowClearStyle
    },
    // ========================= Feedback ==========================
    [`${componentCls}-status`]: {
      "&-error, &-warning, &-success, &-validating": {
        [`&${componentCls}-has-feedback`]: {
          [`${componentCls}-clear`]: {
            insetInlineEnd: token.calc(inputPaddingHorizontalBase).add(token.fontSize).add(token.paddingXS).equal()
          }
        }
      }
    }
  };
};
const genSelectStyle = (token) => {
  const {
    componentCls
  } = token;
  return [
    {
      [componentCls]: {
        // ==================== In Form ====================
        [`&${componentCls}-in-form-item`]: {
          width: "100%"
        }
      }
    },
    // =====================================================
    // ==                       LTR                       ==
    // =====================================================
    // Base
    genBaseStyle(token),
    // Dropdown
    genSingleStyle(token),
    // =====================================================
    // ==                       RTL                       ==
    // =====================================================
    {
      [`${componentCls}-rtl`]: {
        direction: "rtl"
      }
    },
    // =====================================================
    // ==             Space Compact                       ==
    // =====================================================
    genCompactItemStyle(token, {
      focusElCls: `${componentCls}-focused`
    })
  ];
};
const useSelectStyle = genStyleHooks("Select", (token, {
  rootPrefixCls
}) => {
  const selectToken = merge(token, {
    rootPrefixCls,
    inputPaddingHorizontalBase: token.calc(token.paddingSM).sub(token.lineWidth).equal(),
    multipleSelectItemHeight: token.multipleItemHeight,
    selectHeight: token.controlHeight
  });
  return [genSelectStyle(selectToken), genSelectInputStyle(selectToken)];
}, prepareComponentToken, {
  unitless: {
    optionLineHeight: true,
    optionSelectedFontWeight: true
  }
});
var DownOutlined$1 = {};
var hasRequiredDownOutlined;
function requireDownOutlined() {
  if (hasRequiredDownOutlined) return DownOutlined$1;
  hasRequiredDownOutlined = 1;
  Object.defineProperty(DownOutlined$1, "__esModule", { value: true });
  var DownOutlined2 = { "icon": { "tag": "svg", "attrs": { "viewBox": "64 64 896 896", "focusable": "false" }, "children": [{ "tag": "path", "attrs": { "d": "M884 256h-75c-5.1 0-9.9 2.5-12.9 6.6L512 654.2 227.9 262.6c-3-4.1-7.8-6.6-12.9-6.6h-75c-6.5 0-10.3 7.4-6.5 12.7l352.6 486.1c12.8 17.6 39 17.6 51.7 0l352.6-486.1c3.9-5.3.1-12.7-6.4-12.7z" } }] }, "name": "down", "theme": "outlined" };
  DownOutlined$1.default = DownOutlined2;
  return DownOutlined$1;
}
var DownOutlinedExports = /* @__PURE__ */ requireDownOutlined();
const DownOutlinedSvg = /* @__PURE__ */ getDefaultExportFromCjs(DownOutlinedExports);
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
const DownOutlined = (props, ref) => /* @__PURE__ */ reactExports.createElement(Icon, _extends$1({}, props, {
  ref,
  icon: DownOutlinedSvg
}));
const RefIcon$1 = /* @__PURE__ */ reactExports.forwardRef(DownOutlined);
var SearchOutlined$1 = {};
var hasRequiredSearchOutlined;
function requireSearchOutlined() {
  if (hasRequiredSearchOutlined) return SearchOutlined$1;
  hasRequiredSearchOutlined = 1;
  Object.defineProperty(SearchOutlined$1, "__esModule", { value: true });
  var SearchOutlined2 = { "icon": { "tag": "svg", "attrs": { "viewBox": "64 64 896 896", "focusable": "false" }, "children": [{ "tag": "path", "attrs": { "d": "M909.6 854.5L649.9 594.8C690.2 542.7 712 479 712 412c0-80.2-31.3-155.4-87.9-212.1-56.6-56.7-132-87.9-212.1-87.9s-155.5 31.3-212.1 87.9C143.2 256.5 112 331.8 112 412c0 80.1 31.3 155.5 87.9 212.1C256.5 680.8 331.8 712 412 712c67 0 130.6-21.8 182.7-62l259.7 259.6a8.2 8.2 0 0011.6 0l43.6-43.5a8.2 8.2 0 000-11.6zM570.4 570.4C528 612.7 471.8 636 412 636s-116-23.3-158.4-65.6C211.3 528 188 471.8 188 412s23.3-116.1 65.6-158.4C296 211.3 352.2 188 412 188s116.1 23.2 158.4 65.6S636 352.2 636 412s-23.3 116.1-65.6 158.4z" } }] }, "name": "search", "theme": "outlined" };
  SearchOutlined$1.default = SearchOutlined2;
  return SearchOutlined$1;
}
var SearchOutlinedExports = /* @__PURE__ */ requireSearchOutlined();
const SearchOutlinedSvg = /* @__PURE__ */ getDefaultExportFromCjs(SearchOutlinedExports);
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
const SearchOutlined = (props, ref) => /* @__PURE__ */ reactExports.createElement(Icon, _extends({}, props, {
  ref,
  icon: SearchOutlinedSvg
}));
const RefIcon = /* @__PURE__ */ reactExports.forwardRef(SearchOutlined);
function useIcons({
  suffixIcon,
  contextSuffixIcon,
  clearIcon,
  contextClearIcon,
  menuItemSelectedIcon,
  contextMenuItemSelectedIcon,
  removeIcon,
  contextRemoveIcon,
  loading,
  loadingIcon,
  contextLoadingIcon,
  searchIcon,
  contextSearchIcon,
  multiple,
  hasFeedback,
  showSuffixIcon,
  feedbackIcon,
  showArrow,
  componentName
}) {
  return reactExports.useMemo(() => {
    const mergedClearIcon = fallbackProp(clearIcon, contextClearIcon, /* @__PURE__ */ reactExports.createElement(RefIcon$3, null));
    const getSuffixIconNode = (arrowIcon) => {
      if (suffixIcon === null && !hasFeedback && !showArrow) {
        return null;
      }
      return /* @__PURE__ */ reactExports.createElement(reactExports.Fragment, null, showSuffixIcon !== false && arrowIcon, hasFeedback && feedbackIcon);
    };
    let mergedSuffixIcon = null;
    if (suffixIcon !== void 0) {
      mergedSuffixIcon = getSuffixIconNode(suffixIcon);
    } else if (loading) {
      mergedSuffixIcon = getSuffixIconNode(fallbackProp(loadingIcon, contextLoadingIcon, /* @__PURE__ */ reactExports.createElement(RefIcon$4, {
        spin: true
      })));
    } else {
      mergedSuffixIcon = ({
        open,
        showSearch
      }) => {
        if (open && showSearch) {
          return getSuffixIconNode(fallbackProp(searchIcon, contextSearchIcon, /* @__PURE__ */ reactExports.createElement(RefIcon, null)));
        }
        return getSuffixIconNode(fallbackProp(contextSuffixIcon, /* @__PURE__ */ reactExports.createElement(RefIcon$1, null)));
      };
    }
    const mergedItemIcon = fallbackProp(menuItemSelectedIcon, contextMenuItemSelectedIcon, multiple ? /* @__PURE__ */ reactExports.createElement(RefIcon$5, null) : null);
    const mergedRemoveIcon = fallbackProp(removeIcon, contextRemoveIcon, /* @__PURE__ */ reactExports.createElement(RefIcon$6, null));
    return {
      clearIcon: mergedClearIcon,
      suffixIcon: mergedSuffixIcon,
      itemIcon: mergedItemIcon,
      removeIcon: mergedRemoveIcon
    };
  }, [suffixIcon, contextSuffixIcon, clearIcon, contextClearIcon, menuItemSelectedIcon, contextMenuItemSelectedIcon, removeIcon, contextRemoveIcon, loading, loadingIcon, contextLoadingIcon, searchIcon, contextSearchIcon, multiple, hasFeedback, showSuffixIcon, feedbackIcon, showArrow]);
}
function usePopupRender(renderFn) {
  return React.useMemo(() => {
    if (!renderFn) {
      return void 0;
    }
    return (...args) => /* @__PURE__ */ React.createElement(ContextIsolator, {
      space: true
    }, renderFn.apply(void 0, args));
  }, [renderFn]);
}
function useShowArrow(suffixIcon, showArrow) {
  return showArrow !== void 0 ? showArrow : suffixIcon !== null;
}
const SECRET_COMBOBOX_MODE_DO_NOT_USE = "SECRET_COMBOBOX_MODE_DO_NOT_USE";
const InternalSelect = (props, ref) => {
  const {
    prefixCls: customizePrefixCls,
    bordered,
    className,
    rootClassName,
    getPopupContainer,
    popupClassName,
    dropdownClassName,
    listHeight = 256,
    placement,
    listItemHeight: customListItemHeight,
    size: customizeSize,
    disabled: customDisabled,
    notFoundContent,
    status: customStatus,
    builtinPlacements,
    dropdownMatchSelectWidth,
    popupMatchSelectWidth,
    direction: propDirection,
    style,
    allowClear,
    variant: customizeVariant,
    popupStyle,
    dropdownStyle,
    transitionName,
    tagRender,
    maxCount,
    prefix,
    dropdownRender,
    /**
     * @since 5.25.0
     */
    popupRender,
    onDropdownVisibleChange,
    onOpenChange,
    styles,
    classNames,
    clearIcon,
    showSearch,
    ...rest
  } = props;
  const {
    getPopupContainer: getContextPopupContainer,
    getPrefixCls,
    renderEmpty,
    direction: contextDirection,
    virtual,
    popupMatchSelectWidth: contextPopupMatchSelectWidth,
    popupOverflow
  } = reactExports.useContext(ConfigContext);
  const {
    showSearch: contextShowSearch,
    allowClear: contextAllowClear,
    style: contextStyle,
    styles: contextStyles,
    className: contextClassName,
    classNames: contextClassNames,
    clearIcon: contextClearIcon,
    loadingIcon: contextLoadingIcon,
    menuItemSelectedIcon: contextMenuItemSelectedIcon,
    removeIcon: contextRemoveIcon,
    suffixIcon: contextSuffixIcon
  } = useComponentConfig("select");
  const [, token] = useToken();
  const listItemHeight = customListItemHeight ?? token?.controlHeight;
  const prefixCls = getPrefixCls("select", customizePrefixCls);
  const rootPrefixCls = getPrefixCls();
  const direction = propDirection ?? contextDirection;
  const {
    compactSize,
    compactItemClassnames
  } = useCompactItemContext(prefixCls, direction);
  const [variant, enableVariantCls] = useVariant("select", customizeVariant, bordered);
  const rootCls = useCSSVarCls(prefixCls);
  const [hashId, cssVarCls] = useSelectStyle(prefixCls, rootCls);
  const mode = reactExports.useMemo(() => {
    const {
      mode: m
    } = props;
    if (m === "combobox") {
      return void 0;
    }
    if (m === SECRET_COMBOBOX_MODE_DO_NOT_USE) {
      return "combobox";
    }
    return m;
  }, [props.mode]);
  const isMultiple2 = mode === "multiple" || mode === "tags";
  const showSuffixIcon = useShowArrow(props.suffixIcon, props.showArrow);
  const mergedPopupMatchSelectWidth = popupMatchSelectWidth ?? dropdownMatchSelectWidth ?? contextPopupMatchSelectWidth;
  const mergedPopupRender = usePopupRender(popupRender || dropdownRender);
  const mergedOnOpenChange = onOpenChange || onDropdownVisibleChange;
  const {
    status: contextStatus,
    hasFeedback,
    isFormItemInput,
    feedbackIcon
  } = reactExports.useContext(FormItemInputContext);
  const mergedStatus = getMergedStatus(contextStatus, customStatus);
  let mergedNotFound;
  if (notFoundContent !== void 0) {
    mergedNotFound = notFoundContent;
  } else if (mode === "combobox") {
    mergedNotFound = null;
  } else {
    mergedNotFound = renderEmpty?.("Select") || /* @__PURE__ */ reactExports.createElement(DefaultRenderEmpty, {
      componentName: "Select"
    });
  }
  const {
    suffixIcon,
    itemIcon,
    removeIcon,
    clearIcon: mergedClearIcon
  } = useIcons({
    ...rest,
    multiple: isMultiple2,
    hasFeedback,
    feedbackIcon,
    showSuffixIcon,
    componentName: "Select",
    clearIcon,
    searchIcon: normalizeIcon(showSearch, "searchIcon"),
    contextClearIcon,
    contextLoadingIcon,
    contextMenuItemSelectedIcon,
    contextRemoveIcon,
    contextSearchIcon: normalizeIcon(contextShowSearch, "searchIcon"),
    contextSuffixIcon
  });
  const finalAllowClear = allowClear ?? contextAllowClear;
  const mergedAllowClear = finalAllowClear === true ? {
    clearIcon: mergedClearIcon
  } : finalAllowClear;
  const mergedShowSearch = showSearch ?? contextShowSearch;
  const selectProps = omit(rest, ["suffixIcon", "itemIcon"]);
  const mergedSize = useSize((ctx) => customizeSize ?? compactSize ?? ctx);
  const disabled = reactExports.useContext(DisabledContext);
  const mergedDisabled = customDisabled ?? disabled;
  const mergedProps = {
    ...props,
    variant,
    status: mergedStatus,
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
  const mergedPopupClassName = clsx(mergedClassNames.popup.root, popupClassName, dropdownClassName, {
    [`${prefixCls}-dropdown-${direction}`]: direction === "rtl"
  }, rootClassName, cssVarCls, rootCls, hashId);
  const mergedPopupStyle = {
    ...mergedStyles.popup?.root,
    ...popupStyle ?? dropdownStyle
  };
  const mergedClassName = clsx({
    [`${prefixCls}-lg`]: mergedSize === "large",
    [`${prefixCls}-sm`]: mergedSize === "small",
    [`${prefixCls}-rtl`]: direction === "rtl",
    [`${prefixCls}-${variant}`]: enableVariantCls,
    [`${prefixCls}-in-form-item`]: isFormItemInput
  }, getStatusClassNames(prefixCls, mergedStatus, hasFeedback), compactItemClassnames, contextClassName, className, mergedClassNames.root, rootClassName, cssVarCls, rootCls, hashId);
  const memoPlacement = reactExports.useMemo(() => {
    if (placement !== void 0) {
      return placement;
    }
    return direction === "rtl" ? "bottomRight" : "bottomLeft";
  }, [placement, direction]);
  const [zIndex] = useZIndex("SelectLike", mergedStyles.popup.root?.zIndex ?? mergedPopupStyle.zIndex);
  return /* @__PURE__ */ reactExports.createElement(TypedSelect, {
    ref,
    virtual,
    classNames: mergedClassNames,
    styles: mergedStyles,
    showSearch: mergedShowSearch,
    ...selectProps,
    style: mergedStyles.root,
    popupMatchSelectWidth: mergedPopupMatchSelectWidth,
    transitionName: getTransitionName(rootPrefixCls, "slide-up", transitionName),
    builtinPlacements: mergedBuiltinPlacements(builtinPlacements, popupOverflow),
    listHeight,
    listItemHeight,
    mode,
    prefixCls,
    placement: memoPlacement,
    direction,
    prefix,
    suffixIcon,
    menuItemSelectedIcon: itemIcon,
    removeIcon,
    allowClear: mergedAllowClear,
    notFoundContent: mergedNotFound,
    className: mergedClassName,
    getPopupContainer: getPopupContainer || getContextPopupContainer,
    popupClassName: mergedPopupClassName,
    disabled: mergedDisabled,
    popupStyle: {
      ...mergedStyles.popup.root,
      ...mergedPopupStyle,
      zIndex
    },
    maxCount: isMultiple2 ? maxCount : void 0,
    tagRender: isMultiple2 ? tagRender : void 0,
    popupRender: mergedPopupRender,
    onPopupVisibleChange: mergedOnOpenChange
  });
};
const Select = /* @__PURE__ */ reactExports.forwardRef(InternalSelect);
const PurePanel = genPurePanel(Select, "popupAlign");
Select.SECRET_COMBOBOX_MODE_DO_NOT_USE = SECRET_COMBOBOX_MODE_DO_NOT_USE;
Select.Option = Option;
Select.OptGroup = OptGroup;
Select._InternalPanelDoNotUseOrYouWillBeFired = PurePanel;
export {
  Collapse$1 as C,
  DefaultRenderEmpty as D,
  Empty as E,
  RefIcon$1 as R,
  Select as S,
  TARGET_CLS as T,
  Wave as W,
  ColorBlock as a,
  ColorPresets as b,
  genPurePanel as c,
  genNoMotionRawStyle as d,
  genNoMotionStyle as e,
  RefIcon as f,
  genCollapseMotion as g,
  isBright as i,
  withPureRenderTheme as w
};
