import { as as isReactRenderable, l as isFunction, v as genStyleHooks, w as merge, y as initZoomMotion, a$ as genCssVar, A as resetComponent, bU as getArrowStyle, bV as PresetColors, bW as getArrowOffsetToken, bX as getArrowToken, r as reactExports, f as clsx, e as ConfigContext, N as useMergeSemantic, bY as Popup, J as useComponentConfig, bo as useMergedArrow, L as useSemanticRootStyle, aE as useControlledState, m as Tooltip, ak as getTransitionName } from "./index-3RTipSd5.js";
const getRenderPropValue = (propValue) => {
  if (!isReactRenderable(propValue)) {
    return null;
  }
  return isFunction(propValue) ? propValue() : propValue;
};
const FALL_BACK_ORIGIN = "50%";
const genBaseStyle = (token) => {
  const {
    componentCls,
    popoverColor,
    titleMinWidth,
    fontWeightStrong,
    innerPadding,
    dropShadowPopover,
    colorTextHeading,
    borderRadiusLG,
    zIndexPopup,
    titleMarginBottom,
    colorBgElevated,
    popoverBg,
    titleBorderBottom,
    innerContentPadding,
    titlePadding,
    antCls
  } = token;
  const [varName, varRef] = genCssVar(antCls, "tooltip");
  return [
    {
      [componentCls]: {
        ...resetComponent(token),
        position: "absolute",
        top: 0,
        // use `left` to fix https://github.com/ant-design/ant-design/issues/39195
        left: {
          _skip_check_: true,
          value: 0
        },
        zIndex: zIndexPopup,
        fontWeight: "normal",
        whiteSpace: "normal",
        textAlign: "start",
        cursor: "auto",
        userSelect: "text",
        filter: dropShadowPopover,
        // When use `autoArrow`, origin will follow the arrow position
        [varName("valid-offset-x")]: varRef("arrow-offset-x", "var(--arrow-x)"),
        transformOrigin: [varRef("valid-offset-x", FALL_BACK_ORIGIN), `var(--arrow-y, ${FALL_BACK_ORIGIN})`].join(" "),
        [varName("arrow-background-color")]: colorBgElevated,
        width: "max-content",
        maxWidth: "100vw",
        "&-rtl": {
          direction: "rtl"
        },
        "&-hidden": {
          display: "none"
        },
        [`${componentCls}-content`]: {
          position: "relative"
        },
        [`${componentCls}-container`]: {
          backgroundColor: popoverBg,
          backgroundClip: "padding-box",
          borderRadius: borderRadiusLG,
          padding: innerPadding
        },
        [`${componentCls}-title`]: {
          minWidth: titleMinWidth,
          marginBottom: titleMarginBottom,
          color: colorTextHeading,
          fontWeight: fontWeightStrong,
          borderBottom: titleBorderBottom,
          padding: titlePadding
        },
        [`${componentCls}-content`]: {
          color: popoverColor,
          padding: innerContentPadding
        }
      }
    },
    // Arrow Style
    getArrowStyle(token, varRef("arrow-background-color"), {
      arrowShadow: false
    }),
    // Pure Render
    {
      [`${componentCls}-pure`]: {
        position: "relative",
        maxWidth: "none",
        margin: token.sizePopupArrow,
        display: "inline-block"
      }
    }
  ];
};
const genColorStyle = (token) => {
  const {
    componentCls,
    antCls
  } = token;
  const [varName] = genCssVar(antCls, "tooltip");
  return {
    [componentCls]: PresetColors.map((colorKey) => {
      const lightColor = token[`${colorKey}6`];
      return {
        [`&${componentCls}-${colorKey}`]: {
          [varName("arrow-background-color")]: lightColor,
          [`${componentCls}-inner`]: {
            backgroundColor: lightColor
          },
          [`${componentCls}-arrow`]: {
            background: "transparent"
          }
        }
      };
    })
  };
};
const prepareComponentToken = (token) => {
  const {
    lineWidth,
    controlHeight,
    fontHeight,
    padding,
    wireframe,
    zIndexPopupBase,
    borderRadiusLG,
    marginXS,
    lineType,
    colorSplit,
    paddingSM
  } = token;
  const titlePaddingBlockDist = controlHeight - fontHeight;
  const popoverTitlePaddingBlockTop = titlePaddingBlockDist / 2;
  const popoverTitlePaddingBlockBottom = titlePaddingBlockDist / 2 - lineWidth;
  const popoverPaddingHorizontal = padding;
  return {
    titleMinWidth: 177,
    zIndexPopup: zIndexPopupBase + 30,
    ...getArrowToken(token),
    ...getArrowOffsetToken({
      contentRadius: borderRadiusLG,
      limitVerticalRadius: true
    }),
    // internal
    innerPadding: wireframe ? 0 : 12,
    titleMarginBottom: wireframe ? 0 : marginXS,
    titlePadding: wireframe ? `${popoverTitlePaddingBlockTop}px ${popoverPaddingHorizontal}px ${popoverTitlePaddingBlockBottom}px` : 0,
    titleBorderBottom: wireframe ? `${lineWidth}px ${lineType} ${colorSplit}` : "none",
    innerContentPadding: wireframe ? `${paddingSM}px ${popoverPaddingHorizontal}px` : 0
  };
};
const useStyle = genStyleHooks("Popover", (token) => {
  const {
    colorBgElevated,
    colorText
  } = token;
  const popoverToken = merge(token, {
    popoverBg: colorBgElevated,
    popoverColor: colorText
  });
  return [genBaseStyle(popoverToken), genColorStyle(popoverToken), initZoomMotion(popoverToken, "zoom-big")];
}, prepareComponentToken, {
  resetStyle: false,
  deprecatedTokens: [["width", "titleMinWidth"], ["minWidth", "titleMinWidth"]]
});
const Overlay = (props) => {
  const {
    title,
    content,
    prefixCls,
    classNames,
    styles
  } = props;
  if (!isReactRenderable(title) && !isReactRenderable(content)) {
    return null;
  }
  return /* @__PURE__ */ reactExports.createElement(reactExports.Fragment, null, isReactRenderable(title) && /* @__PURE__ */ reactExports.createElement("div", {
    className: clsx(`${prefixCls}-title`, classNames?.title),
    style: styles?.title
  }, title), isReactRenderable(content) && /* @__PURE__ */ reactExports.createElement("div", {
    className: clsx(`${prefixCls}-content`, classNames?.content),
    style: styles?.content
  }, content));
};
const RawPurePanel = (props) => {
  const {
    hashId,
    prefixCls,
    className,
    style,
    placement = "top",
    title,
    content,
    children,
    classNames,
    styles
  } = props;
  const titleNode = getRenderPropValue(title);
  const contentNode = getRenderPropValue(content);
  const mergedProps = {
    ...props,
    placement
  };
  const [mergedClassNames, mergedStyles] = useMergeSemantic([classNames], [styles], {
    props: mergedProps
  });
  const rootClassName = clsx(hashId, prefixCls, `${prefixCls}-pure`, `${prefixCls}-placement-${placement}`, className);
  return /* @__PURE__ */ reactExports.createElement("div", {
    className: rootClassName,
    style
  }, /* @__PURE__ */ reactExports.createElement("div", {
    className: `${prefixCls}-arrow`
  }), /* @__PURE__ */ reactExports.createElement(Popup, {
    ...props,
    className: hashId,
    prefixCls,
    classNames: mergedClassNames,
    styles: mergedStyles
  }, children || /* @__PURE__ */ reactExports.createElement(Overlay, {
    prefixCls,
    title: titleNode,
    content: contentNode,
    classNames: mergedClassNames,
    styles: mergedStyles
  })));
};
const PurePanel = (props) => {
  const {
    prefixCls: customizePrefixCls,
    className,
    ...restProps
  } = props;
  const {
    getPrefixCls
  } = reactExports.useContext(ConfigContext);
  const prefixCls = getPrefixCls("popover", customizePrefixCls);
  const [hashId, cssVarCls] = useStyle(prefixCls);
  return /* @__PURE__ */ reactExports.createElement(RawPurePanel, {
    ...restProps,
    prefixCls,
    hashId,
    className: clsx(className, cssVarCls)
  });
};
const InternalPopover = /* @__PURE__ */ reactExports.forwardRef((props, ref) => {
  const {
    prefixCls: customizePrefixCls,
    title,
    content,
    overlayClassName,
    placement = "top",
    trigger,
    children,
    mouseEnterDelay,
    mouseLeaveDelay,
    onOpenChange,
    overlayStyle = {},
    styles,
    classNames,
    motion,
    arrow: popoverArrow,
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
  } = useComponentConfig("popover");
  const mergedMouseEnterDelay = mouseEnterDelay ?? contextMouseEnterDelay ?? 0.1;
  const mergedMouseLeaveDelay = mouseLeaveDelay ?? contextMouseLeaveDelay ?? 0.1;
  const prefixCls = getPrefixCls("popover", customizePrefixCls);
  const [hashId, cssVarCls] = useStyle(prefixCls);
  const rootPrefixCls = getPrefixCls();
  const mergedArrow = useMergedArrow(popoverArrow, contextArrow);
  const mergedTrigger = trigger || contextTrigger || "hover";
  const mergedProps = {
    ...props,
    placement,
    trigger: mergedTrigger,
    mouseEnterDelay: mergedMouseEnterDelay,
    mouseLeaveDelay: mergedMouseLeaveDelay,
    overlayStyle,
    styles,
    classNames
  };
  const contextStyleRoot = useSemanticRootStyle(contextStyle);
  const overlayStyleRoot = useSemanticRootStyle(overlayStyle);
  const [mergedClassNames, mergedStyles] = useMergeSemantic([contextClassNames, classNames], [contextStyles, contextStyleRoot, styles, overlayStyleRoot], {
    props: mergedProps
  });
  const rootClassNames = clsx(overlayClassName, hashId, cssVarCls, contextClassName, mergedClassNames.root);
  const [open, setOpen] = useControlledState(props.defaultOpen ?? false, props.open);
  const settingOpen = (value) => {
    setOpen(value);
    onOpenChange?.(value);
  };
  const titleNode = getRenderPropValue(title);
  const contentNode = getRenderPropValue(content);
  return /* @__PURE__ */ reactExports.createElement(Tooltip, {
    unique: false,
    arrow: mergedArrow,
    placement,
    trigger: mergedTrigger,
    mouseEnterDelay: mergedMouseEnterDelay,
    mouseLeaveDelay: mergedMouseLeaveDelay,
    ...restProps,
    prefixCls,
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
    ref,
    open,
    onOpenChange: settingOpen,
    overlay: isReactRenderable(titleNode) || isReactRenderable(contentNode) ? /* @__PURE__ */ reactExports.createElement(Overlay, {
      prefixCls,
      title: titleNode,
      content: contentNode,
      classNames: mergedClassNames,
      styles: mergedStyles
    }) : null,
    motion: {
      motionName: getTransitionName(rootPrefixCls, "zoom-big", typeof motion?.motionName === "string" ? motion?.motionName : void 0)
    },
    "data-popover-inject": true
  }, children);
});
const Popover = InternalPopover;
Popover._InternalPanelDoNotUseOrYouWillBeFired = PurePanel;
export {
  Popover as P,
  PurePanel as a,
  getRenderPropValue as g
};
