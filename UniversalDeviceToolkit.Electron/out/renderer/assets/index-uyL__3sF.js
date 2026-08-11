import { a8 as useLocale, $ as React, az as localeValues, ag as isNonNullable, Z as isPlainObject, a2 as pickAttrs, ab as RefIcon, r as reactExports, e as ConfigContext, au as useToken, f as clsx, as as isReactRenderable, bp as isString, ah as isNumber, k as cloneElement, b$ as isFragment, _ as _toConsumableArray, bV as PresetColors, a3 as CSSMotion, br as RefIcon$1, w as merge, c0 as getLineHeight, bl as AggregationColor, n as unit, c1 as getAlphaColor, a$ as genCssVar, v as genStyleHooks, ac as genFocusStyle, E as resetIcon, am as genSubStyleComponent, a_ as genCompactItemStyle, g as toArray, J as useComponentConfig, aK as DisabledContext, bj as useDelayState, a0 as useComposeRef, aN as useLayoutEffect, b7 as useCompactItemContext, aL as useSize, h as omit, L as useSemanticRootStyle, N as useMergeSemantic, A as resetComponent, B as FastColor, aE as useControlledState, Q as useCSSVarCls, c2 as isPresetColor, c3 as isPresetStatusColor, c4 as genPresetColor, l as isFunction, c5 as replaceElement } from "./index-3RTipSd5.js";
import { i as isBright, e as genNoMotionStyle, W as Wave } from "./index-BxBscas6.js";
function mergeProps(...items) {
  const ret = {};
  for (const item of items) {
    if (item) {
      for (const key of Object.keys(item)) {
        if (item[key] !== void 0) {
          ret[key] = item[key];
        }
      }
    }
  }
  return ret;
}
const pickClosable = (context) => {
  if (!context) {
    return void 0;
  }
  const {
    closable,
    closeIcon
  } = context;
  return {
    closable,
    closeIcon
  };
};
const EmptyFallbackCloseCollection = {};
const computeClosableConfig = (closable, closeIcon) => {
  if (!closable && (closable === false || closeIcon === false || closeIcon === null)) {
    return false;
  }
  if (!isNonNullable(closable) && !isNonNullable(closeIcon)) {
    return null;
  }
  let closableConfig = {
    closeIcon: typeof closeIcon !== "boolean" && isNonNullable(closeIcon) ? closeIcon : void 0
  };
  if (isPlainObject(closable)) {
    closableConfig = {
      ...closableConfig,
      ...closable
    };
  }
  return closableConfig;
};
const mergeClosableConfigs = (propConfig, contextConfig, fallbackConfig) => {
  if (propConfig === false) {
    return false;
  }
  if (propConfig) {
    return mergeProps(fallbackConfig, contextConfig, propConfig);
  }
  if (contextConfig === false) {
    return false;
  }
  if (contextConfig) {
    return mergeProps(fallbackConfig, contextConfig);
  }
  return fallbackConfig.closable ? fallbackConfig : false;
};
const computeCloseIcon = (mergedConfig, fallbackCloseCollection, closeLabel) => {
  const {
    closeIconRender
  } = fallbackCloseCollection;
  const {
    closeIcon,
    ...restConfig
  } = mergedConfig;
  let finalCloseIcon = closeIcon;
  const ariaOrDataProps = pickAttrs(restConfig, true);
  if (isNonNullable(finalCloseIcon)) {
    if (closeIconRender) {
      finalCloseIcon = closeIconRender(finalCloseIcon);
    }
    finalCloseIcon = /* @__PURE__ */ React.isValidElement(finalCloseIcon) ? /* @__PURE__ */ React.cloneElement(finalCloseIcon, {
      "aria-label": closeLabel,
      ...finalCloseIcon.props,
      ...ariaOrDataProps
    }) : /* @__PURE__ */ React.createElement("span", {
      "aria-label": closeLabel,
      ...ariaOrDataProps
    }, finalCloseIcon);
  }
  return [finalCloseIcon, ariaOrDataProps];
};
const computeClosable = (propCloseCollection, contextCloseCollection, fallbackCloseCollection = EmptyFallbackCloseCollection, closeLabel = "Close") => {
  const propConfig = computeClosableConfig(propCloseCollection?.closable, propCloseCollection?.closeIcon);
  const contextConfig = computeClosableConfig(contextCloseCollection?.closable, contextCloseCollection?.closeIcon);
  const mergedFallback = {
    closeIcon: /* @__PURE__ */ React.createElement(RefIcon, null),
    ...fallbackCloseCollection
  };
  const mergedConfig = mergeClosableConfigs(propConfig, contextConfig, mergedFallback);
  const closeBtnIsDisabled = typeof mergedConfig !== "boolean" ? !!mergedConfig?.disabled : false;
  if (mergedConfig === false) {
    return [false, null, closeBtnIsDisabled, {}];
  }
  const [closeIcon, ariaProps] = computeCloseIcon(mergedConfig, mergedFallback, closeLabel);
  return [true, closeIcon, closeBtnIsDisabled, ariaProps];
};
const useClosable = (propCloseCollection, contextCloseCollection, fallbackCloseCollection = EmptyFallbackCloseCollection) => {
  const [contextLocale] = useLocale("global", localeValues.global);
  return React.useMemo(() => {
    return computeClosable(propCloseCollection, contextCloseCollection, {
      closeIcon: /* @__PURE__ */ React.createElement(RefIcon, null),
      ...fallbackCloseCollection
    }, contextLocale.close);
  }, [propCloseCollection, contextCloseCollection, fallbackCloseCollection, contextLocale.close]);
};
const GroupSizeContext = /* @__PURE__ */ reactExports.createContext(void 0);
const ButtonGroup = (props) => {
  const {
    getPrefixCls,
    direction
  } = reactExports.useContext(ConfigContext);
  const {
    prefixCls: customizePrefixCls,
    size,
    className,
    ...others
  } = props;
  const prefixCls = getPrefixCls("btn-group", customizePrefixCls);
  const [, , hashId] = useToken();
  const sizeCls = reactExports.useMemo(() => {
    switch (size) {
      case "large":
        return "lg";
      case "small":
        return "sm";
      default:
        return "";
    }
  }, [size]);
  const classes = clsx(prefixCls, {
    [`${prefixCls}-${sizeCls}`]: sizeCls,
    [`${prefixCls}-rtl`]: direction === "rtl"
  }, className, hashId);
  return /* @__PURE__ */ reactExports.createElement(GroupSizeContext.Provider, {
    value: size
  }, /* @__PURE__ */ reactExports.createElement("div", {
    ...others,
    className: classes
  }));
};
const rxTwoCNChar = /^[\u4E00-\u9FA5]{2}$/;
const isTwoCNChar = rxTwoCNChar.test.bind(rxTwoCNChar);
function convertLegacyProps(type) {
  if (type === "danger") {
    return {
      danger: true
    };
  }
  return {
    type
  };
}
function isUnBorderedButtonVariant(type) {
  return type === "text" || type === "link";
}
function splitCNCharsBySpace(child, needInserted, style, className) {
  if (!isReactRenderable(child)) {
    return;
  }
  const SPACE = needInserted ? " " : "";
  if (!isString(child) && !isNumber(child) && isString(child.type) && isTwoCNChar(child.props.children)) {
    return cloneElement(child, (oriProps) => {
      const mergedCls = clsx(oriProps.className, className) || void 0;
      const mergedStyle = {
        ...style,
        ...oriProps.style
      };
      return {
        ...oriProps,
        children: oriProps.children.split("").join(SPACE),
        className: mergedCls,
        style: mergedStyle
      };
    });
  }
  if (isString(child)) {
    return /* @__PURE__ */ React.createElement("span", {
      className,
      style
    }, isTwoCNChar(child) ? child.split("").join(SPACE) : child);
  }
  if (isFragment(child)) {
    return /* @__PURE__ */ React.createElement("span", {
      className,
      style
    }, child);
  }
  return cloneElement(child, (oriProps) => ({
    ...oriProps,
    className: clsx(oriProps.className, className) || void 0,
    style: {
      ...oriProps.style,
      ...style
    }
  }));
}
function spaceChildren(children, needInserted, style, className) {
  let isPrevChildPure = false;
  const childList = [];
  React.Children.forEach(children, (child) => {
    const isCurrentChildPure = isString(child) || isNumber(child);
    if (isPrevChildPure && isCurrentChildPure) {
      const lastIndex = childList.length - 1;
      const lastChild = childList[lastIndex];
      childList[lastIndex] = `${lastChild}${child}`;
    } else {
      childList.push(child);
    }
    isPrevChildPure = isCurrentChildPure;
  });
  return React.Children.map(childList, (child) => splitCNCharsBySpace(child, needInserted, style, className));
}
["default", "primary", "danger"].concat(_toConsumableArray(PresetColors));
const IconWrapper = /* @__PURE__ */ reactExports.forwardRef((props, ref) => {
  const {
    className,
    style,
    children,
    prefixCls
  } = props;
  const iconWrapperCls = clsx(`${prefixCls}-icon`, className);
  return /* @__PURE__ */ React.createElement("span", {
    ref,
    className: iconWrapperCls,
    style
  }, children);
});
const InnerLoadingIcon = /* @__PURE__ */ reactExports.forwardRef((props, ref) => {
  const {
    prefixCls,
    className,
    style,
    iconClassName
  } = props;
  const mergedIconCls = clsx(`${prefixCls}-loading-icon`, className);
  return /* @__PURE__ */ React.createElement(IconWrapper, {
    prefixCls,
    className: mergedIconCls,
    style,
    ref
  }, /* @__PURE__ */ React.createElement(RefIcon$1, {
    className: iconClassName
  }));
});
const getCollapsedWidth = () => ({
  width: 0,
  opacity: 0,
  transform: "scale(0)"
});
const getRealWidth = (node) => ({
  width: node.scrollWidth,
  opacity: 1,
  transform: "scale(1)"
});
const DefaultLoadingIcon = (props) => {
  const {
    prefixCls,
    loading,
    existIcon,
    className,
    style,
    mount
  } = props;
  const visible = !!loading;
  if (existIcon) {
    return /* @__PURE__ */ React.createElement(InnerLoadingIcon, {
      prefixCls,
      className,
      style
    });
  }
  return /* @__PURE__ */ React.createElement(CSSMotion, {
    visible,
    // Used for minus flex gap style only
    motionName: `${prefixCls}-loading-icon-motion`,
    motionAppear: !mount,
    motionEnter: !mount,
    motionLeave: !mount,
    removeOnLeave: true,
    onAppearStart: getCollapsedWidth,
    onAppearActive: getRealWidth,
    onEnterStart: getCollapsedWidth,
    onEnterActive: getRealWidth,
    onLeaveStart: getRealWidth,
    onLeaveActive: getCollapsedWidth
  }, ({
    className: motionCls,
    style: motionStyle
  }, ref) => {
    const mergedStyle = {
      ...style,
      ...motionStyle
    };
    return /* @__PURE__ */ React.createElement(InnerLoadingIcon, {
      prefixCls,
      className: clsx(className, motionCls),
      style: mergedStyle,
      ref
    });
  });
};
const genButtonBorderStyle = (buttonTypeCls, borderColor) => ({
  // Border
  [`> span, > ${buttonTypeCls}`]: {
    "&:not(:last-child)": {
      [`&, & > ${buttonTypeCls}`]: {
        "&:not(:disabled)": {
          borderInlineEndColor: borderColor
        }
      }
    },
    "&:not(:first-child)": {
      [`&, & > ${buttonTypeCls}`]: {
        "&:not(:disabled)": {
          borderInlineStartColor: borderColor
        }
      }
    }
  }
});
const genGroupStyle = (token) => {
  const {
    componentCls,
    fontSize,
    lineWidth,
    groupBorderColor,
    colorErrorHover
  } = token;
  return {
    [`${componentCls}-group`]: [
      {
        position: "relative",
        display: "inline-flex",
        // Border
        [`> span, > ${componentCls}`]: {
          "&:not(:last-child)": {
            [`&, & > ${componentCls}`]: {
              borderStartEndRadius: 0,
              borderEndEndRadius: 0
            }
          },
          "&:not(:first-child)": {
            marginInlineStart: token.calc(lineWidth).mul(-1).equal(),
            [`&, & > ${componentCls}`]: {
              borderStartStartRadius: 0,
              borderEndStartRadius: 0
            }
          }
        },
        [componentCls]: {
          position: "relative",
          zIndex: 1,
          "&:hover, &:focus, &:active": {
            zIndex: 2
          },
          "&[disabled]": {
            zIndex: 0
          }
        },
        [`${componentCls}-icon-only`]: {
          fontSize
        }
      },
      // Border Color
      genButtonBorderStyle(`${componentCls}-primary`, groupBorderColor),
      genButtonBorderStyle(`${componentCls}-danger`, colorErrorHover)
    ]
  };
};
const prepareToken$1 = (token) => {
  const {
    paddingInline,
    onlyIconSize,
    borderColorDisabled
  } = token;
  const buttonToken = merge(token, {
    buttonPaddingHorizontal: paddingInline,
    buttonPaddingVertical: 0,
    buttonIconOnlyFontSize: onlyIconSize,
    colorBorderDisabled: borderColorDisabled
  });
  return buttonToken;
};
const prepareComponentToken$1 = (token) => {
  const contentFontSize = token.contentFontSize ?? token.fontSize;
  const contentFontSizeSM = token.contentFontSizeSM ?? token.fontSize;
  const contentFontSizeLG = token.contentFontSizeLG ?? token.fontSizeLG;
  const contentLineHeight = token.contentLineHeight ?? getLineHeight(contentFontSize);
  const contentLineHeightSM = token.contentLineHeightSM ?? getLineHeight(contentFontSizeSM);
  const contentLineHeightLG = token.contentLineHeightLG ?? getLineHeight(contentFontSizeLG);
  const solidTextColor = isBright(new AggregationColor(token.colorBgSolid), "#fff") ? "#000" : "#fff";
  const shadowColorTokens = PresetColors.reduce((prev, colorKey) => ({
    ...prev,
    [`${colorKey}ShadowColor`]: `0 ${unit(token.controlOutlineWidth)} 0 ${getAlphaColor(token[`${colorKey}1`], token.colorBgContainer)}`
  }), {});
  const defaultBgDisabled = token.colorBgContainerDisabled;
  const dashedBgDisabled = token.colorBgContainerDisabled;
  return {
    ...shadowColorTokens,
    fontWeight: 400,
    iconGap: token.marginXS,
    defaultShadow: `0 ${token.controlOutlineWidth}px 0 ${token.controlTmpOutline}`,
    primaryShadow: `0 ${token.controlOutlineWidth}px 0 ${token.controlOutline}`,
    dangerShadow: `0 ${token.controlOutlineWidth}px 0 ${token.colorErrorOutline}`,
    primaryColor: token.colorTextLightSolid,
    dangerColor: token.colorTextLightSolid,
    borderColorDisabled: token.colorBorderDisabled,
    defaultGhostColor: token.colorBgContainer,
    ghostBg: "transparent",
    defaultGhostBorderColor: token.colorBgContainer,
    paddingInline: token.paddingContentHorizontal - token.lineWidth,
    paddingInlineLG: token.paddingContentHorizontal - token.lineWidth,
    paddingInlineSM: 8 - token.lineWidth,
    onlyIconSize: "inherit",
    onlyIconSizeSM: "inherit",
    onlyIconSizeLG: "inherit",
    groupBorderColor: token.colorPrimaryHover,
    linkHoverBg: "transparent",
    textTextColor: token.colorText,
    textTextHoverColor: token.colorText,
    textTextActiveColor: token.colorText,
    textHoverBg: token.colorFillTertiary,
    defaultColor: token.colorText,
    defaultBg: token.colorBgContainer,
    defaultBorderColor: token.colorBorder,
    defaultBorderColorDisabled: token.colorBorder,
    defaultHoverBg: token.colorBgContainer,
    defaultHoverColor: token.colorPrimaryHover,
    defaultHoverBorderColor: token.colorPrimaryHover,
    defaultActiveBg: token.colorBgContainer,
    defaultActiveColor: token.colorPrimaryActive,
    defaultActiveBorderColor: token.colorPrimaryActive,
    solidTextColor,
    contentFontSize,
    contentFontSizeSM,
    contentFontSizeLG,
    contentLineHeight,
    contentLineHeightSM,
    contentLineHeightLG,
    paddingBlock: Math.max((token.controlHeight - contentFontSize * contentLineHeight) / 2 - token.lineWidth, 0),
    paddingBlockSM: Math.max((token.controlHeightSM - contentFontSizeSM * contentLineHeightSM) / 2 - token.lineWidth, 0),
    paddingBlockLG: Math.max((token.controlHeightLG - contentFontSizeLG * contentLineHeightLG) / 2 - token.lineWidth, 0),
    defaultBgDisabled,
    dashedBgDisabled
  };
};
const genVariantStyle = (token) => {
  const {
    componentCls,
    antCls,
    lineWidth,
    lineType
  } = token;
  const [varName, varRef] = genCssVar(antCls, "btn");
  return {
    [componentCls]: [
      // ==============================================================
      // ==                         Variable                         ==
      // ==============================================================
      {
        // Border
        [varName("border-width")]: lineWidth,
        [varName("border-color")]: "#000",
        [varName("border-color-hover")]: varRef("border-color"),
        [varName("border-color-active")]: varRef("border-color"),
        [varName("border-color-disabled")]: varRef("border-color"),
        [varName("border-style")]: lineType,
        // Text
        [varName("text-color")]: "#000",
        [varName("text-color-hover")]: varRef("text-color"),
        [varName("text-color-active")]: varRef("text-color"),
        [varName("text-color-disabled")]: varRef("text-color"),
        // Background
        [varName("bg-color")]: "#ddd",
        [varName("bg-color-hover")]: varRef("bg-color"),
        [varName("bg-color-active")]: varRef("bg-color"),
        [varName("bg-color-disabled")]: token.colorBgContainerDisabled,
        [varName("bg-color-container")]: token.colorBgContainer,
        // Shadow
        [varName("shadow")]: "none"
      },
      // ==============================================================
      // ==                         Template                         ==
      // ==============================================================
      {
        // Basic
        border: [varRef("border-width"), varRef("border-style"), varRef("border-color")].join(" "),
        color: varRef("text-color"),
        backgroundColor: varRef("bg-color"),
        // Status
        [`&:not(:disabled):not(${componentCls}-disabled)`]: {
          // Hover
          "&:hover": {
            border: [varRef("border-width"), varRef("border-style"), varRef("border-color-hover")].join(" "),
            color: varRef("text-color-hover"),
            backgroundColor: varRef("bg-color-hover")
          },
          // Active
          "&:active": {
            border: [varRef("border-width"), varRef("border-style"), varRef("border-color-active")].join(" "),
            color: varRef("text-color-active"),
            backgroundColor: varRef("bg-color-active")
          }
        }
      },
      // ==============================================================
      // ==                         Variants                         ==
      // ==============================================================
      {
        // >>>>> Solid
        [`&${componentCls}-variant-solid`]: {
          // Solid Variables
          [varName("solid-bg-color")]: varRef("color-base"),
          [varName("solid-bg-color-hover")]: varRef("color-hover"),
          [varName("solid-bg-color-active")]: varRef("color-active"),
          // Variables
          [varName("border-color")]: "transparent",
          [varName("text-color")]: token.colorTextLightSolid,
          [varName("bg-color")]: varRef("solid-bg-color"),
          [varName("bg-color-hover")]: varRef("solid-bg-color-hover"),
          [varName("bg-color-active")]: varRef("solid-bg-color-active"),
          // Box Shadow
          boxShadow: varRef("shadow")
        },
        // >>>>> Outlined & Dashed
        [`&${componentCls}-variant-outlined, &${componentCls}-variant-dashed`]: {
          [varName("border-color")]: varRef("color-base"),
          [varName("border-color-hover")]: varRef("color-hover"),
          [varName("border-color-active")]: varRef("color-active"),
          [varName("bg-color")]: varRef("bg-color-container"),
          [varName("text-color")]: varRef("color-base"),
          [varName("text-color-hover")]: varRef("color-hover"),
          [varName("text-color-active")]: varRef("color-active"),
          // Box Shadow
          boxShadow: varRef("shadow")
        },
        // >>>>> Dashed
        [`&${componentCls}-variant-dashed`]: {
          [varName("border-style")]: "dashed",
          [varName("bg-color-disabled")]: token.dashedBgDisabled
        },
        // >>>>> Filled
        [`&${componentCls}-variant-filled`]: {
          [varName("border-color")]: "transparent",
          [varName("text-color")]: varRef("color-base"),
          [varName("bg-color")]: varRef("color-light"),
          [varName("bg-color-hover")]: varRef("color-light-hover"),
          [varName("bg-color-active")]: varRef("color-light-active")
        },
        // >>>>> Text & Link
        [`&${componentCls}-variant-text, &${componentCls}-variant-link`]: {
          [varName("border-color")]: "transparent",
          [varName("text-color")]: varRef("color-base"),
          [varName("text-color-hover")]: varRef("color-hover"),
          [varName("text-color-active")]: varRef("color-active"),
          [varName("bg-color")]: "transparent",
          [varName("bg-color-hover")]: "transparent",
          [varName("bg-color-active")]: "transparent",
          [`&:disabled, &${token.componentCls}-disabled`]: {
            background: "transparent",
            borderColor: "transparent"
          }
        },
        // >>>>> Text
        [`&${componentCls}-variant-text`]: {
          [varName("bg-color-hover")]: varRef("color-light"),
          [varName("bg-color-active")]: varRef("color-light-active")
        }
      },
      // ==============================================================
      // ==                          Colors                          ==
      // ==============================================================
      {
        // ======================== By Default ========================
        // >>>>> Link
        [`&${componentCls}-variant-link`]: {
          [varName("color-base")]: token.colorLink,
          [varName("color-hover")]: token.colorLinkHover,
          [varName("color-active")]: token.colorLinkActive,
          [varName("bg-color-hover")]: token.linkHoverBg
        },
        // ======================== Compatible ========================
        // >>>>> Primary
        [`&${componentCls}-color-primary`]: {
          [varName("color-base")]: token.colorPrimary,
          [varName("color-hover")]: token.colorPrimaryHover,
          [varName("color-active")]: token.colorPrimaryActive,
          [varName("color-light")]: token.colorPrimaryBg,
          [varName("color-light-hover")]: token.colorPrimaryBgHover,
          [varName("color-light-active")]: token.colorPrimaryBorder,
          [varName("shadow")]: token.primaryShadow,
          [`&${componentCls}-variant-solid`]: {
            [varName("text-color")]: token.primaryColor,
            [varName("text-color-hover")]: varRef("text-color"),
            [varName("text-color-active")]: varRef("text-color")
          }
        },
        // >>>>> Danger
        [`&${componentCls}-color-dangerous`]: {
          [varName("color-base")]: token.colorError,
          [varName("color-hover")]: token.colorErrorHover,
          [varName("color-active")]: token.colorErrorActive,
          [varName("color-light")]: token.colorErrorBg,
          [varName("color-light-hover")]: token.colorErrorBgFilledHover,
          [varName("color-light-active")]: token.colorErrorBgActive,
          [varName("shadow")]: token.dangerShadow,
          [`&${componentCls}-variant-solid`]: {
            [varName("text-color")]: token.dangerColor,
            [varName("text-color-hover")]: varRef("text-color"),
            [varName("text-color-active")]: varRef("text-color")
          }
        },
        // >>>>> Default
        [`&${componentCls}-color-default`]: {
          [varName("solid-bg-color")]: token.colorBgSolid,
          [varName("solid-bg-color-hover")]: token.colorBgSolidHover,
          [varName("solid-bg-color-active")]: token.colorBgSolidActive,
          [varName("color-base")]: token.defaultBorderColor,
          [varName("color-hover")]: token.defaultHoverBorderColor,
          [varName("color-active")]: token.defaultActiveBorderColor,
          [varName("color-light")]: token.colorFillTertiary,
          [varName("color-light-hover")]: token.colorFillSecondary,
          [varName("color-light-active")]: token.colorFill,
          [varName("text-color")]: token.defaultColor,
          [varName("text-color-hover")]: token.defaultHoverColor,
          [varName("text-color-active")]: token.defaultActiveColor,
          [varName("shadow")]: token.defaultShadow,
          [`&${componentCls}-variant-outlined`]: {
            [varName("bg-color-disabled")]: token.defaultBgDisabled
          },
          [`&${componentCls}-variant-solid`]: {
            [varName("text-color")]: token.solidTextColor,
            [varName("text-color-hover")]: varRef("text-color"),
            [varName("text-color-active")]: varRef("text-color")
          },
          [`&${componentCls}-variant-filled, &${componentCls}-variant-text`]: {
            [varName("text-color-hover")]: varRef("text-color"),
            [varName("text-color-active")]: varRef("text-color")
          },
          [`&${componentCls}-variant-outlined, &${componentCls}-variant-dashed`]: {
            [varName("text-color")]: token.defaultColor,
            [varName("text-color-hover")]: token.defaultHoverColor,
            [varName("text-color-active")]: token.defaultActiveColor,
            [varName("bg-color-container")]: token.defaultBg,
            [varName("bg-color-hover")]: token.defaultHoverBg,
            [varName("bg-color-active")]: token.defaultActiveBg
          },
          [`&${componentCls}-variant-text`]: {
            [varName("text-color")]: token.textTextColor,
            [varName("text-color-hover")]: token.textTextHoverColor,
            [varName("text-color-active")]: token.textTextActiveColor,
            [varName("bg-color-hover")]: token.textHoverBg
          },
          [`&${componentCls}-background-ghost`]: {
            [`&${componentCls}-variant-outlined, &${componentCls}-variant-dashed`]: {
              [varName("text-color")]: token.defaultGhostColor,
              [varName("border-color")]: token.defaultGhostBorderColor
            }
          }
        }
      },
      // >>>>> Preset Colors
      PresetColors.map((colorKey) => {
        const darkColor = token[`${colorKey}6`];
        const lightColor = token[`${colorKey}1`];
        const hoverColor = token[`${colorKey}Hover`];
        const lightHoverColor = token[`${colorKey}2`];
        const lightActiveColor = token[`${colorKey}3`];
        const activeColor = token[`${colorKey}Active`];
        const shadowColor = token[`${colorKey}ShadowColor`];
        return {
          [`&${componentCls}-color-${colorKey}`]: {
            [varName("color-base")]: darkColor,
            [varName("color-hover")]: hoverColor,
            [varName("color-active")]: activeColor,
            [varName("color-light")]: lightColor,
            [varName("color-light-hover")]: lightHoverColor,
            [varName("color-light-active")]: lightActiveColor,
            [varName("shadow")]: shadowColor
          }
        };
      }),
      // ==============================================================
      // ==                         Disabled                         ==
      // ==============================================================
      {
        // Disabled
        [`&:disabled, &${token.componentCls}-disabled`]: {
          cursor: "not-allowed",
          borderColor: token.colorBorderDisabled,
          background: varRef("bg-color-disabled"),
          color: token.colorTextDisabled,
          boxShadow: "none"
        }
      },
      // ==============================================================
      // ==                          Ghost                           ==
      // ==============================================================
      {
        // Ghost
        [`&${componentCls}-background-ghost`]: {
          [varName("bg-color")]: token.ghostBg,
          [varName("bg-color-hover")]: token.ghostBg,
          [varName("bg-color-active")]: token.ghostBg,
          [varName("shadow")]: "none",
          [`&${componentCls}-variant-outlined, &${componentCls}-variant-dashed`]: {
            [varName("bg-color-hover")]: token.ghostBg,
            [varName("bg-color-active")]: token.ghostBg
          }
        }
      }
    ]
  };
};
const genSharedButtonStyle = (token) => {
  const {
    componentCls,
    iconCls,
    fontWeight,
    opacityLoading,
    motionDurationSlow,
    motionEaseInOut,
    iconGap,
    calc
  } = token;
  return {
    [componentCls]: {
      outline: "none",
      position: "relative",
      display: "inline-flex",
      gap: iconGap,
      alignItems: "center",
      justifyContent: "center",
      fontWeight,
      whiteSpace: "nowrap",
      textAlign: "center",
      backgroundImage: "none",
      cursor: "pointer",
      transition: `all ${token.motionDurationMid} ${token.motionEaseInOut}`,
      userSelect: "none",
      touchAction: "manipulation",
      ...genNoMotionStyle(),
      "&:disabled > *": {
        pointerEvents: "none"
      },
      // https://github.com/ant-design/ant-design/issues/51380
      [`${componentCls}-icon > svg`]: resetIcon(),
      // https://github.com/ant-design/ant-design/issues/57727
      [`${componentCls}-icon`]: {
        display: "inline-flex",
        alignItems: "center",
        [iconCls]: {
          verticalAlign: "middle",
          // Baseline will align the first element.
          // So the Button with SVG will make the baseline to be the bottom of the SVG.
          // Let's use `:before` to add a space to make the baseline to be the center of the Button.
          // https://github.com/ant-design/ant-design/issues/58428
          "&:before": {
            content: '"\\a0"',
            display: "inline-block",
            width: 0
          }
        }
      },
      "> a": {
        color: "currentColor"
      },
      "&:not(:disabled)": genFocusStyle(token),
      [`&${componentCls}-two-chinese-chars::first-letter`]: {
        letterSpacing: "0.34em"
      },
      [`&${componentCls}-two-chinese-chars > *:not(${iconCls})`]: {
        marginInlineEnd: "-0.34em",
        letterSpacing: "0.34em"
      },
      [`&${componentCls}-icon-only`]: {
        paddingInline: 0,
        // make `btn-icon-only` not too narrow
        [`&${componentCls}-compact-item`]: {
          flex: "none"
        }
      },
      // Loading
      [`&${componentCls}-loading`]: {
        opacity: opacityLoading,
        cursor: "default"
      },
      [`${componentCls}-loading-icon`]: {
        transition: ["width", "opacity", "margin"].map((prop) => `${prop} ${motionDurationSlow} ${motionEaseInOut}`).join(",")
      },
      // iconPlacement
      [`&:not(${componentCls}-icon-end)`]: {
        [`${componentCls}-loading-icon-motion`]: {
          "&-appear-start, &-enter-start, &-appear-prepare, &-enter-prepare": {
            marginInlineEnd: calc(iconGap).mul(-1).equal(),
            opacity: 0
          },
          "&-appear-active, &-enter-active": {
            marginInlineEnd: 0
          },
          "&-leave-start": {
            marginInlineEnd: 0
          },
          "&-leave-active": {
            marginInlineEnd: calc(iconGap).mul(-1).equal()
          }
        }
      },
      "&-icon-end": {
        flexDirection: "row-reverse",
        [`${componentCls}-loading-icon-motion`]: {
          "&-appear-start, &-enter-start, &-appear-prepare, &-enter-prepare": {
            marginInlineStart: calc(iconGap).mul(-1).equal(),
            opacity: 0
          },
          "&-appear-active, &-enter-active": {
            marginInlineStart: 0
          },
          "&-leave-start": {
            marginInlineStart: 0
          },
          "&-leave-active": {
            marginInlineStart: calc(iconGap).mul(-1).equal()
          }
        }
      }
    }
  };
};
const genCircleButtonStyle = (token) => ({
  minWidth: token.controlHeight,
  paddingInline: 0,
  borderRadius: "50%"
});
const genButtonStyle = (token, prefixCls = "") => {
  const {
    componentCls,
    controlHeight,
    fontSize,
    borderRadius,
    buttonPaddingHorizontal,
    iconCls,
    buttonPaddingVertical,
    buttonIconOnlyFontSize
  } = token;
  return [
    {
      [prefixCls]: {
        fontSize,
        height: controlHeight,
        padding: `${unit(buttonPaddingVertical)} ${unit(buttonPaddingHorizontal)}`,
        borderRadius,
        [`&${componentCls}-icon-only`]: {
          width: controlHeight,
          [iconCls]: {
            fontSize: buttonIconOnlyFontSize
          }
        }
      }
    },
    // Shape - patch prefixCls again to override solid border radius style
    {
      [`${componentCls}${componentCls}-circle${prefixCls}`]: genCircleButtonStyle(token)
    },
    {
      [`${componentCls}${componentCls}-round${prefixCls}`]: {
        borderRadius: token.controlHeight,
        [`&:not(${componentCls}-icon-only)`]: {
          paddingInline: token.buttonPaddingHorizontal
        }
      }
    }
  ];
};
const genSizeBaseButtonStyle = (token) => {
  const baseToken = merge(token, {
    fontSize: token.contentFontSize
  });
  return genButtonStyle(baseToken, token.componentCls);
};
const genSizeSmallButtonStyle = (token) => {
  const smallToken = merge(token, {
    controlHeight: token.controlHeightSM,
    fontSize: token.contentFontSizeSM,
    padding: token.paddingXS,
    buttonPaddingHorizontal: token.paddingInlineSM,
    buttonPaddingVertical: 0,
    borderRadius: token.borderRadiusSM,
    buttonIconOnlyFontSize: token.onlyIconSizeSM
  });
  return genButtonStyle(smallToken, `${token.componentCls}-sm`);
};
const genSizeLargeButtonStyle = (token) => {
  const largeToken = merge(token, {
    controlHeight: token.controlHeightLG,
    fontSize: token.contentFontSizeLG,
    buttonPaddingHorizontal: token.paddingInlineLG,
    buttonPaddingVertical: 0,
    borderRadius: token.borderRadiusLG,
    buttonIconOnlyFontSize: token.onlyIconSizeLG
  });
  return genButtonStyle(largeToken, `${token.componentCls}-lg`);
};
const genBlockButtonStyle = (token) => {
  const {
    componentCls
  } = token;
  return {
    [componentCls]: {
      [`&${componentCls}-block`]: {
        width: "100%"
      }
    }
  };
};
const useStyle$1 = genStyleHooks("Button", (token) => {
  const buttonToken = prepareToken$1(token);
  return [
    // Shared
    genSharedButtonStyle(buttonToken),
    // Size
    genSizeBaseButtonStyle(buttonToken),
    genSizeSmallButtonStyle(buttonToken),
    genSizeLargeButtonStyle(buttonToken),
    // Block
    genBlockButtonStyle(buttonToken),
    // Variant
    genVariantStyle(buttonToken),
    // Button Group
    genGroupStyle(buttonToken)
  ];
}, prepareComponentToken$1, {
  unitless: {
    fontWeight: true,
    contentLineHeight: true,
    contentLineHeightSM: true,
    contentLineHeightLG: true
  }
});
function compactItemVerticalBorder(token, parentCls, prefixCls) {
  return {
    // border collapse
    [`&-item:not(${parentCls}-last-item)`]: {
      marginBottom: token.calc(token.lineWidth).mul(-1).equal()
    },
    [`&-item:not(${prefixCls}-status-success)`]: {
      zIndex: 2
    },
    "&-item": {
      "&:focus,&:active": {
        zIndex: 3
      },
      "&:hover": {
        zIndex: 4
      },
      "&[disabled]": {
        zIndex: 0
      }
    }
  };
}
function compactItemBorderVerticalRadius(prefixCls, parentCls) {
  return {
    [`&-item:not(${parentCls}-first-item):not(${parentCls}-last-item)`]: {
      borderRadius: 0
    },
    [`&-item${parentCls}-first-item:not(${parentCls}-last-item)`]: {
      [`&, &${prefixCls}-sm, &${prefixCls}-lg`]: {
        borderEndEndRadius: 0,
        borderEndStartRadius: 0
      }
    },
    [`&-item${parentCls}-last-item:not(${parentCls}-first-item)`]: {
      [`&, &${prefixCls}-sm, &${prefixCls}-lg`]: {
        borderStartStartRadius: 0,
        borderStartEndRadius: 0
      }
    }
  };
}
function genCompactItemVerticalStyle(token) {
  const compactCls = `${token.componentCls}-compact-vertical`;
  return {
    [compactCls]: {
      ...compactItemVerticalBorder(token, compactCls, token.componentCls),
      ...compactItemBorderVerticalRadius(token.componentCls, compactCls)
    }
  };
}
const genButtonCompactStyle = (token) => {
  const {
    antCls,
    componentCls,
    lineWidth,
    calc,
    colorBgContainer
  } = token;
  const solidSelector = `${componentCls}-variant-solid:not([disabled])`;
  const insetOffset = calc(lineWidth).mul(-1).equal();
  const [varName, varRef] = genCssVar(antCls, "btn");
  const getCompactBorderStyle = (vertical) => {
    const itemCls = `${componentCls}-compact${vertical ? "-vertical" : ""}-item`;
    return {
      // TODO: Border color transition should be not cover when has color.
      [itemCls]: {
        [varName("compact-connect-border-color")]: varRef("bg-color-hover"),
        [`&${solidSelector}`]: {
          transition: `none`,
          [`& + ${solidSelector}:before`]: [{
            position: "absolute",
            backgroundColor: varRef("compact-connect-border-color"),
            content: '""'
          }, vertical ? {
            top: insetOffset,
            insetInline: insetOffset,
            height: lineWidth
          } : {
            insetBlock: insetOffset,
            insetInlineStart: insetOffset,
            width: lineWidth
          }],
          "&:hover:before": {
            display: "none"
          }
        }
      }
    };
  };
  return [getCompactBorderStyle(), getCompactBorderStyle(true), {
    [`${solidSelector}${componentCls}-color-default`]: {
      [varName("compact-connect-border-color")]: `color-mix(in srgb, ${varRef("bg-color-hover")} 75%, ${colorBgContainer})`
    }
  }];
};
const Compact = genSubStyleComponent(["Button", "compact"], (token) => {
  const buttonToken = prepareToken$1(token);
  return [
    // Space Compact
    genCompactItemStyle(buttonToken),
    genCompactItemVerticalStyle(buttonToken),
    genButtonCompactStyle(buttonToken)
  ];
}, prepareComponentToken$1);
function getLoadingConfig(loading) {
  if (isPlainObject(loading)) {
    let delay = loading?.delay;
    delay = isNumber(delay) ? delay : 0;
    return {
      loading: delay <= 0,
      delay
    };
  }
  return {
    loading: !!loading,
    delay: 0
  };
}
const ButtonTypeMap = {
  default: ["default", "outlined"],
  primary: ["primary", "solid"],
  dashed: ["default", "dashed"],
  // `link` is not a real color but we should compatible with it
  link: ["link", "link"],
  text: ["default", "text"]
};
const InternalCompoundedButton = /* @__PURE__ */ React.forwardRef((props, ref) => {
  const {
    _skipSemantic,
    loading = false,
    prefixCls: customizePrefixCls,
    color,
    variant,
    type,
    danger = false,
    shape: customizeShape,
    size: customizeSize,
    disabled: customDisabled,
    className,
    rootClassName,
    children,
    icon,
    iconPosition,
    iconPlacement,
    ghost = false,
    block = false,
    // React does not recognize the `htmlType` prop on a DOM element. Here we pick it out of `rest`.
    htmlType = "button",
    classNames,
    styles,
    style,
    autoInsertSpace,
    autoFocus,
    ...rest
  } = props;
  const childNodes = toArray(children);
  const mergedType = type || "default";
  const {
    getPrefixCls,
    direction,
    autoInsertSpace: contextAutoInsertSpace,
    className: contextClassName,
    style: contextStyle,
    classNames: contextClassNames,
    styles: contextStyles,
    loadingIcon: contextLoadingIcon,
    shape: contextShape,
    color: contextColor,
    variant: contextVariant
  } = useComponentConfig("button");
  const mergedShape = customizeShape || contextShape || "default";
  const [parsedColor, parsedVariant] = reactExports.useMemo(() => {
    if (color && variant) {
      return [color, variant];
    }
    if (type || danger) {
      const colorVariantPair = ButtonTypeMap[mergedType] || [];
      if (danger) {
        return ["danger", colorVariantPair[1]];
      }
      return colorVariantPair;
    }
    if (variant === "solid") {
      return ["primary", variant];
    }
    if (contextColor && contextVariant) {
      return [contextColor, contextVariant];
    }
    if (contextVariant === "solid") {
      return ["primary", contextVariant];
    }
    return ["default", "outlined"];
  }, [color, variant, type, danger, contextColor, contextVariant, mergedType]);
  const [mergedColor, mergedVariant] = reactExports.useMemo(() => {
    if (ghost && parsedVariant === "solid") {
      return [parsedColor, "outlined"];
    }
    return [parsedColor, parsedVariant];
  }, [parsedColor, parsedVariant, ghost]);
  const isDanger = mergedColor === "danger";
  const mergedColorText = isDanger ? "dangerous" : mergedColor;
  const mergedInsertSpace = autoInsertSpace ?? contextAutoInsertSpace ?? true;
  const prefixCls = getPrefixCls("btn", customizePrefixCls);
  const [hashId, cssVarCls] = useStyle$1(prefixCls);
  const disabled = reactExports.useContext(DisabledContext);
  const mergedDisabled = customDisabled ?? disabled;
  const groupSize = reactExports.useContext(GroupSizeContext);
  const loadingOrDelay = reactExports.useMemo(() => getLoadingConfig(loading), [loading]);
  const [innerLoading, setInnerLoading] = useDelayState(loadingOrDelay.loading);
  const [hasTwoCNChar, setHasTwoCNChar] = reactExports.useState(false);
  const buttonRef = reactExports.useRef(null);
  const mergedRef = useComposeRef(ref, buttonRef);
  const needInserted = childNodes.length === 1 && !icon && !isUnBorderedButtonVariant(mergedVariant);
  const isMountRef = reactExports.useRef(true);
  React.useEffect(() => {
    isMountRef.current = false;
    return () => {
      isMountRef.current = true;
    };
  }, []);
  useLayoutEffect(() => {
    if (loadingOrDelay.delay > 0) {
      setInnerLoading(true, {
        ms: loadingOrDelay.delay
      });
    } else {
      setInnerLoading(loadingOrDelay.loading, true);
    }
  }, [loadingOrDelay.delay, loadingOrDelay.loading]);
  reactExports.useEffect(() => {
    if (!buttonRef.current || !mergedInsertSpace) {
      return;
    }
    const buttonText = buttonRef.current.textContent || "";
    if (needInserted && isTwoCNChar(buttonText)) {
      if (!hasTwoCNChar) {
        setHasTwoCNChar(true);
      }
    } else if (hasTwoCNChar) {
      setHasTwoCNChar(false);
    }
  });
  reactExports.useEffect(() => {
    if (autoFocus) {
      buttonRef.current?.focus();
    }
  }, []);
  const handleClick = React.useCallback((e) => {
    if (innerLoading || mergedDisabled) {
      e.preventDefault();
      return;
    }
    props.onClick?.("href" in props ? e : e);
  }, [props.onClick, innerLoading, mergedDisabled]);
  const {
    compactSize,
    compactItemClassnames
  } = useCompactItemContext(prefixCls, direction);
  const sizeFullName = useSize((ctxSize) => customizeSize ?? compactSize ?? groupSize ?? ctxSize);
  const iconType = innerLoading ? "loading" : icon;
  const mergedIconPlacement = iconPlacement ?? iconPosition ?? "start";
  const linkButtonRestProps = omit(rest, ["navigate"]);
  const mergedProps = {
    ...props,
    type: mergedType,
    color: mergedColor,
    variant: mergedVariant,
    danger: isDanger,
    shape: mergedShape,
    size: sizeFullName,
    disabled: mergedDisabled,
    loading: innerLoading,
    iconPlacement: mergedIconPlacement
  };
  const contextStyleRoot = useSemanticRootStyle(contextStyle);
  const styleRoot = useSemanticRootStyle(style);
  const [mergedClassNames, mergedStyles] = useMergeSemantic([_skipSemantic ? void 0 : contextClassNames, classNames], [_skipSemantic ? void 0 : contextStyles, contextStyleRoot, styles, styleRoot], {
    props: mergedProps
  });
  const classes = clsx(prefixCls, hashId, cssVarCls, {
    [`${prefixCls}-${mergedShape}`]: mergedShape !== "default" && mergedShape !== "square" && mergedShape,
    // Compatible with versions earlier than 5.21.0
    [`${prefixCls}-${mergedType}`]: mergedType,
    [`${prefixCls}-dangerous`]: danger,
    [`${prefixCls}-color-${mergedColorText}`]: mergedColorText,
    [`${prefixCls}-variant-${mergedVariant}`]: mergedVariant,
    [`${prefixCls}-lg`]: sizeFullName === "large",
    [`${prefixCls}-sm`]: sizeFullName === "small",
    [`${prefixCls}-icon-only`]: !children && children !== 0 && !!iconType,
    [`${prefixCls}-background-ghost`]: ghost && !isUnBorderedButtonVariant(mergedVariant),
    [`${prefixCls}-loading`]: innerLoading,
    [`${prefixCls}-two-chinese-chars`]: hasTwoCNChar && mergedInsertSpace && !innerLoading,
    [`${prefixCls}-block`]: block,
    [`${prefixCls}-rtl`]: direction === "rtl",
    [`${prefixCls}-icon-end`]: mergedIconPlacement === "end"
  }, compactItemClassnames, className, rootClassName, contextClassName, mergedClassNames.root);
  const iconSharedProps = {
    className: mergedClassNames.icon,
    style: mergedStyles.icon
  };
  const iconWrapperElement = (child) => /* @__PURE__ */ React.createElement(IconWrapper, {
    prefixCls,
    ...iconSharedProps
  }, child);
  const defaultLoadingIconElement = /* @__PURE__ */ React.createElement(DefaultLoadingIcon, {
    existIcon: !!icon,
    prefixCls,
    loading: innerLoading,
    mount: isMountRef.current,
    ...iconSharedProps
  });
  const mergedLoadingIcon = isPlainObject(loading) ? loading.icon || contextLoadingIcon : contextLoadingIcon;
  let iconNode;
  if (icon && !innerLoading) {
    iconNode = iconWrapperElement(icon);
  } else if (loading && mergedLoadingIcon) {
    iconNode = iconWrapperElement(mergedLoadingIcon);
  } else {
    iconNode = defaultLoadingIconElement;
  }
  const contentNode = isReactRenderable(children) ? spaceChildren(children, needInserted && mergedInsertSpace, mergedStyles.content, mergedClassNames.content) : null;
  if (linkButtonRestProps.href !== void 0) {
    return /* @__PURE__ */ React.createElement("a", {
      ...linkButtonRestProps,
      className: clsx(classes, {
        [`${prefixCls}-disabled`]: mergedDisabled
      }),
      href: mergedDisabled ? void 0 : linkButtonRestProps.href,
      style: mergedStyles.root,
      onClick: handleClick,
      ref: mergedRef,
      tabIndex: mergedDisabled ? -1 : 0,
      "aria-disabled": mergedDisabled
    }, iconNode, contentNode);
  }
  let buttonNode = /* @__PURE__ */ React.createElement("button", {
    ...rest,
    type: htmlType,
    className: classes,
    style: mergedStyles.root,
    onClick: handleClick,
    disabled: mergedDisabled,
    ref: mergedRef
  }, iconNode, contentNode, compactItemClassnames && /* @__PURE__ */ React.createElement(Compact, {
    prefixCls
  }));
  if (!isUnBorderedButtonVariant(mergedVariant)) {
    buttonNode = /* @__PURE__ */ React.createElement(Wave, {
      component: "Button",
      disabled: innerLoading
    }, buttonNode);
  }
  return buttonNode;
});
const Button = InternalCompoundedButton;
Button.Group = ButtonGroup;
Button.__ANT_BUTTON = true;
const genBaseStyle = (token) => {
  const {
    paddingXXS,
    lineWidth,
    tagPaddingHorizontal,
    componentCls,
    calc
  } = token;
  const paddingInline = calc(tagPaddingHorizontal).sub(lineWidth).equal();
  const iconMarginInline = calc(paddingXXS).sub(lineWidth).equal();
  return {
    // Result
    [componentCls]: {
      ...resetComponent(token),
      display: "inline-block",
      height: "auto",
      paddingInline,
      fontSize: token.tagFontSize,
      lineHeight: token.tagLineHeight,
      whiteSpace: "nowrap",
      backgroundColor: token.defaultBg,
      border: `${unit(token.lineWidth)} ${token.lineType} ${token.colorBorder}`,
      borderRadius: token.borderRadiusSM,
      opacity: 1,
      transition: `all ${token.motionDurationMid}`,
      textAlign: "start",
      position: "relative",
      // RTL
      [`&${componentCls}-rtl`]: {
        direction: "rtl"
      },
      "&, a, a:hover": {
        color: token.defaultColor
      },
      [`${componentCls}-close-icon`]: {
        marginInlineStart: iconMarginInline,
        fontSize: token.tagIconSize,
        color: token.colorIcon,
        cursor: "pointer",
        transition: `all ${token.motionDurationMid}`,
        "&:hover": {
          color: token.colorTextHeading
        }
      },
      "&-checkable": {
        backgroundColor: "transparent",
        borderColor: "transparent",
        cursor: "pointer",
        [`&:not(${componentCls}-checkable-checked):hover`]: {
          color: token.colorPrimary,
          backgroundColor: token.colorFillSecondary
        },
        "&:active, &-checked": {
          color: token.colorTextLightSolid
        },
        "&-checked": {
          backgroundColor: token.colorPrimary,
          "&:hover": {
            backgroundColor: token.colorPrimaryHover
          }
        },
        "&:active": {
          backgroundColor: token.colorPrimaryActive
        },
        "&-disabled": {
          cursor: "not-allowed",
          [`&:not(${componentCls}-checkable-checked)`]: {
            color: token.colorTextDisabled,
            "&:hover": {
              backgroundColor: "transparent"
            }
          },
          [`&${componentCls}-checkable-checked`]: {
            color: token.colorTextDisabled,
            backgroundColor: token.colorBgContainerDisabled
          },
          "&:hover, &:active": {
            backgroundColor: token.colorBgContainerDisabled,
            color: token.colorTextDisabled
          },
          [`&:not(${componentCls}-checkable-checked):hover`]: {
            color: token.colorTextDisabled
          }
        },
        "&-group": {
          display: "flex",
          flexWrap: "wrap",
          gap: token.paddingXS
        }
      },
      "&-hidden": {
        display: "none"
      },
      // Icons from third-party libraries are a bare `<svg>`, which none of the `.anticon`
      // rules reach. An `<svg>` has no baseline of its own, so it is aligned by its bottom
      // margin edge (CSS 2.1 §10.8.1) and rides above the text. Centre it instead: unlike an
      // `.anticon` (whose `<svg>` is always `1em`, so a fixed `-0.125em` nudge suffices), a
      // third-party `<svg>` may be sized in `px`, so the correction must not depend on size.
      // `display: inline-block` keeps it an atomic inline box so `vertical-align` still applies even
      // under a CSS reset that forces `svg { display: block }` (e.g. Tailwind Preflight), which would
      // otherwise drop the icon onto its own line. `vertical-align: middle` centres the margin box on
      // the x-height line; `margin-block-end` then lifts it by half its own value onto the cap-height
      // centre (capHeight − xHeight ≈ 0.2em across typical fonts), keeping it centred at any icon size.
      // Only matches a bare `<svg>`: an `.anticon` keeps its `<svg>` one level deeper.
      "> svg": {
        display: "inline-block",
        verticalAlign: "middle",
        marginBlockEnd: "0.2em"
      },
      // To ensure that a space will be placed between character and `Icon`.
      [`> ${token.iconCls} + span, > span + ${token.iconCls}, > svg + span, > span + svg`]: {
        marginInlineStart: paddingInline
      }
    },
    [`&${token.componentCls}-solid`]: {
      borderColor: "transparent",
      color: token.colorTextLightSolid,
      backgroundColor: token.colorBgSolid,
      [`&${componentCls}-default`]: {
        color: token.solidTextColor
      }
    },
    [`${componentCls}-filled`]: {
      borderColor: "transparent",
      backgroundColor: token.tagBorderlessBg
    },
    [`&${componentCls}-disabled`]: {
      color: token.colorTextDisabled,
      cursor: "not-allowed",
      backgroundColor: token.colorBgContainerDisabled,
      a: {
        cursor: "not-allowed",
        pointerEvents: "none",
        color: token.colorTextDisabled,
        "&:hover": {
          color: token.colorTextDisabled
        }
      },
      "a&": {
        "&:hover, &:active": {
          color: token.colorTextDisabled
        }
      },
      [`&${componentCls}-outlined`]: {
        borderColor: token.colorBorderDisabled
      },
      [`&${componentCls}-solid, &${componentCls}-filled`]: {
        color: token.colorTextDisabled,
        [`${componentCls}-close-icon`]: {
          color: token.colorTextDisabled
        }
      },
      [`${componentCls}-close-icon`]: {
        cursor: "not-allowed",
        color: token.colorTextDisabled,
        "&:hover": {
          color: token.colorTextDisabled
        }
      }
    }
  };
};
const prepareToken = (token) => {
  const {
    lineWidth,
    fontSizeIcon,
    calc
  } = token;
  const tagFontSize = token.fontSizeSM;
  const tagToken = merge(token, {
    tagFontSize,
    tagLineHeight: unit(calc(token.lineHeightSM).mul(tagFontSize).equal()),
    tagIconSize: calc(fontSizeIcon).sub(calc(lineWidth).mul(2)).equal(),
    // Tag icon is much smaller
    tagPaddingHorizontal: 8,
    // Fixed padding.
    tagBorderlessBg: token.defaultBg
  });
  return tagToken;
};
const prepareComponentToken = (token) => {
  const solidTextColor = isBright(new AggregationColor(token.colorBgSolid), "#fff") ? "#000" : "#fff";
  return {
    defaultBg: new FastColor(token.colorFillTertiary).onBackground(token.colorBgContainer).toHexString(),
    defaultColor: token.colorText,
    solidTextColor
  };
};
const useStyle = genStyleHooks("Tag", (token) => {
  const tagToken = prepareToken(token);
  return genBaseStyle(tagToken);
}, prepareComponentToken);
const CheckableTag = /* @__PURE__ */ reactExports.forwardRef((props, ref) => {
  const {
    prefixCls: customizePrefixCls,
    style,
    className,
    checked,
    children,
    icon,
    onChange,
    onClick,
    onKeyDown,
    disabled: customDisabled,
    ...restProps
  } = props;
  const {
    getPrefixCls,
    tag
  } = reactExports.useContext(ConfigContext);
  const disabled = reactExports.useContext(DisabledContext);
  const mergedDisabled = customDisabled ?? disabled;
  const handleClick = (e) => {
    if (mergedDisabled) {
      return;
    }
    onChange?.(!checked);
    onClick?.(e);
  };
  const handleKeyDown = (e) => {
    onKeyDown?.(e);
    if (e.defaultPrevented || mergedDisabled) {
      return;
    }
    if (e.key === " ") {
      e.preventDefault();
      onChange?.(!checked);
    }
  };
  const prefixCls = getPrefixCls("tag", customizePrefixCls);
  const [hashId, cssVarCls] = useStyle(prefixCls);
  const cls = clsx(prefixCls, `${prefixCls}-checkable`, {
    [`${prefixCls}-checkable-checked`]: checked,
    [`${prefixCls}-checkable-disabled`]: mergedDisabled
  }, tag?.className, className, hashId, cssVarCls);
  return /* @__PURE__ */ reactExports.createElement("span", {
    ...restProps,
    ref,
    role: "checkbox",
    "aria-checked": checked,
    "aria-disabled": mergedDisabled || void 0,
    tabIndex: mergedDisabled ? -1 : 0,
    style: {
      ...style,
      ...tag?.style
    },
    className: cls,
    onClick: handleClick,
    onKeyDown: handleKeyDown
  }, icon, /* @__PURE__ */ reactExports.createElement("span", null, children));
});
const CheckableTagGroup = /* @__PURE__ */ React.forwardRef((props, ref) => {
  const {
    id,
    prefixCls: customizePrefixCls,
    rootClassName,
    className,
    style,
    classNames,
    styles,
    disabled,
    options,
    value,
    defaultValue,
    onChange,
    multiple,
    ...restProps
  } = props;
  const {
    getPrefixCls,
    direction,
    className: contextClassName,
    style: contextStyle,
    classNames: contextClassNames,
    styles: contextStyles
  } = useComponentConfig("tag");
  const prefixCls = getPrefixCls("tag", customizePrefixCls);
  const groupPrefixCls = `${prefixCls}-checkable-group`;
  const rootCls = useCSSVarCls(prefixCls);
  const [hashId, cssVarCls] = useStyle(prefixCls, rootCls);
  const contextStyleRoot = useSemanticRootStyle(contextStyle);
  const styleRoot = useSemanticRootStyle(style);
  const [mergedClassNames, mergedStyles] = useMergeSemantic([contextClassNames, classNames], [contextStyles, contextStyleRoot, styles, styleRoot], {
    props
  });
  const parsedOptions = reactExports.useMemo(() => {
    if (!Array.isArray(options)) {
      return [];
    }
    return options.map((option) => {
      if (isPlainObject(option)) {
        return option;
      }
      return {
        value: option,
        label: option
      };
    });
  }, [options]);
  const [mergedValue, setMergedValue] = useControlledState(defaultValue, value);
  const handleChange = (checked, option) => {
    let newValue = null;
    if (multiple) {
      const valueList = mergedValue || [];
      newValue = checked ? [].concat(_toConsumableArray(valueList), [option.value]) : valueList.filter((item) => item !== option.value);
    } else {
      newValue = checked ? option.value : null;
    }
    setMergedValue(newValue);
    onChange?.(newValue);
  };
  const divRef = React.useRef(null);
  reactExports.useImperativeHandle(ref, () => ({
    nativeElement: divRef.current
  }));
  const ariaProps = pickAttrs(restProps, {
    aria: true,
    data: true
  });
  return /* @__PURE__ */ React.createElement("div", {
    ...ariaProps,
    className: clsx(groupPrefixCls, contextClassName, rootClassName, {
      [`${groupPrefixCls}-disabled`]: disabled,
      [`${groupPrefixCls}-rtl`]: direction === "rtl"
    }, hashId, cssVarCls, className, mergedClassNames.root),
    style: mergedStyles.root,
    id,
    ref: divRef
  }, parsedOptions.map((option) => /* @__PURE__ */ React.createElement(CheckableTag, {
    key: option.value,
    className: clsx(`${groupPrefixCls}-item`, mergedClassNames.item, option.className),
    style: {
      ...mergedStyles.item,
      ...option.style
    },
    checked: multiple ? (mergedValue || []).includes(option.value) : mergedValue === option.value,
    onChange: (checked) => handleChange(checked, option),
    disabled
  }, option.label)));
});
function useColor(props, contextVariant) {
  const {
    color,
    variant,
    bordered
  } = props;
  return reactExports.useMemo(() => {
    const isInverseColor = color?.endsWith("-inverse");
    let nextVariant;
    if (variant) {
      nextVariant = variant;
    } else if (isInverseColor) {
      nextVariant = "solid";
    } else if (bordered === false) {
      nextVariant = "filled";
    } else {
      nextVariant = contextVariant || "filled";
    }
    let nextColor = isInverseColor ? color?.replace("-inverse", "") : color;
    if (nextColor === void 0 && nextVariant === "solid") {
      nextColor = "default";
    }
    const nextIsPreset = isPresetColor(nextColor);
    const nextIsStatus = isPresetStatusColor(nextColor);
    const tagStyle = {};
    if (!nextIsPreset && !nextIsStatus && nextColor) {
      if (nextVariant === "solid") {
        tagStyle.backgroundColor = color;
      } else {
        const hsl = new FastColor(nextColor).toHsl();
        hsl.l = 0.95;
        tagStyle.backgroundColor = new FastColor(hsl).toHexString();
        tagStyle.color = color;
        if (nextVariant === "outlined") {
          tagStyle.borderColor = color;
        }
      }
    }
    return [nextVariant, nextColor, nextIsPreset, nextIsStatus, tagStyle];
  }, [color, variant, bordered, contextVariant]);
}
const genPresetStyle = (token) => genPresetColor(token, (colorKey, {
  textColor,
  lightBorderColor,
  lightColor,
  darkColor
}) => ({
  [`${token.componentCls}${token.componentCls}-${colorKey}:not(${token.componentCls}-disabled)`]: {
    [`&${token.componentCls}-outlined`]: {
      backgroundColor: lightColor,
      borderColor: lightBorderColor,
      color: textColor
    },
    [`&${token.componentCls}-solid`]: {
      backgroundColor: darkColor,
      borderColor: darkColor,
      color: token.colorTextLightSolid
    },
    [`&${token.componentCls}-filled`]: {
      backgroundColor: lightColor,
      color: textColor
    }
  }
}));
const PresetCmp = genSubStyleComponent(["Tag", "preset"], (token) => {
  const tagToken = prepareToken(token);
  return genPresetStyle(tagToken);
}, prepareComponentToken);
function capitalize(str) {
  if (typeof str !== "string") {
    return str;
  }
  const ret = str.charAt(0).toUpperCase() + str.slice(1);
  return ret;
}
const genTagStatusStyle = (token, status, cssVariableType) => {
  const capitalizedCssVariableType = capitalize(cssVariableType);
  return {
    [`${token.componentCls}${token.componentCls}-${status}:not(${token.componentCls}-disabled)`]: {
      [`&${token.componentCls}-outlined`]: {
        backgroundColor: token[`color${capitalizedCssVariableType}Bg`],
        borderColor: token[`color${capitalizedCssVariableType}Border`],
        color: token[`color${cssVariableType}`]
      },
      [`&${token.componentCls}-solid`]: {
        backgroundColor: token[`color${cssVariableType}`],
        borderColor: token[`color${cssVariableType}`]
      },
      [`&${token.componentCls}-filled`]: {
        backgroundColor: token[`color${capitalizedCssVariableType}Bg`],
        color: token[`color${cssVariableType}`]
      }
    }
  };
};
const StatusCmp = genSubStyleComponent(["Tag", "status"], (token) => {
  const tagToken = prepareToken(token);
  return [genTagStatusStyle(tagToken, "success", "Success"), genTagStatusStyle(tagToken, "processing", "Info"), genTagStatusStyle(tagToken, "error", "Error"), genTagStatusStyle(tagToken, "warning", "Warning")];
}, prepareComponentToken);
const InternalTag = /* @__PURE__ */ reactExports.forwardRef((props, ref) => {
  const {
    prefixCls: customizePrefixCls,
    className,
    rootClassName,
    style,
    children,
    icon,
    color,
    variant: _variant,
    onClose,
    bordered,
    disabled: customDisabled,
    href,
    target,
    styles,
    classNames,
    ...restProps
  } = props;
  const {
    getPrefixCls,
    direction,
    className: contextClassName,
    variant: contextVariant,
    style: contextStyle,
    classNames: contextClassNames,
    styles: contextStyles
  } = useComponentConfig("tag");
  const [mergedVariant, mergedColor, isPreset, isStatus, customTagStyle] = useColor(props, contextVariant);
  const isInternalColor = isPreset || isStatus;
  const disabled = reactExports.useContext(DisabledContext);
  const mergedDisabled = customDisabled ?? disabled;
  const {
    tag: tagContext
  } = reactExports.useContext(ConfigContext);
  const [visible, setVisible] = reactExports.useState(true);
  const domProps = omit(restProps, ["closeIcon", "closable"]);
  const mergedProps = {
    ...props,
    color: mergedColor,
    variant: mergedVariant,
    disabled: mergedDisabled
  };
  const contextStyleRoot = useSemanticRootStyle(contextStyle);
  const styleRoot = useSemanticRootStyle(style);
  const [mergedClassNames, mergedStyles] = useMergeSemantic([contextClassNames, classNames], [contextStyles, contextStyleRoot, styles, styleRoot], {
    props: mergedProps
  });
  const tagStyle = reactExports.useMemo(() => {
    let nextTagStyle = mergedStyles.root;
    if (!mergedDisabled) {
      nextTagStyle = {
        ...customTagStyle,
        ...nextTagStyle
      };
    }
    return nextTagStyle;
  }, [mergedStyles.root, customTagStyle, mergedDisabled]);
  const prefixCls = getPrefixCls("tag", customizePrefixCls);
  const [hashId, cssVarCls] = useStyle(prefixCls);
  const tagClassName = clsx(prefixCls, contextClassName, mergedClassNames.root, `${prefixCls}-${mergedVariant}`, {
    [`${prefixCls}-${mergedColor}`]: isInternalColor,
    [`${prefixCls}-hidden`]: !visible,
    [`${prefixCls}-rtl`]: direction === "rtl",
    [`${prefixCls}-disabled`]: mergedDisabled
  }, className, rootClassName, hashId, cssVarCls);
  const triggerClose = (e) => {
    if (mergedDisabled) {
      return;
    }
    e.stopPropagation();
    onClose?.(e);
    if (e.defaultPrevented) {
      return;
    }
    if (href) {
      e.preventDefault();
    }
    setVisible(false);
  };
  const handleCloseKeyDown = (e) => {
    if (e.key === "Enter" || e.key === " ") {
      e.preventDefault();
      e.currentTarget.click();
    }
  };
  const [, mergedCloseIcon] = useClosable(pickClosable(props), pickClosable(tagContext), {
    closable: false,
    closeIconRender: (iconNode2) => {
      const replacement = /* @__PURE__ */ reactExports.createElement("span", {
        role: "button",
        tabIndex: mergedDisabled ? -1 : 0,
        "aria-disabled": mergedDisabled || void 0,
        className: clsx(`${prefixCls}-close-icon`, mergedClassNames.close),
        onClick: triggerClose,
        onKeyDown: handleCloseKeyDown,
        style: mergedStyles.close
      }, iconNode2);
      return replaceElement(iconNode2, replacement, (originProps) => ({
        onClick: (e) => {
          originProps?.onClick?.(e);
          triggerClose(e);
        },
        onKeyDown: (e) => {
          originProps?.onKeyDown?.(e);
          if (!e.defaultPrevented) {
            handleCloseKeyDown(e);
          }
        },
        role: "button",
        tabIndex: mergedDisabled ? -1 : 0,
        "aria-disabled": mergedDisabled || void 0,
        className: clsx(originProps?.className, `${prefixCls}-close-icon`, mergedClassNames.close),
        style: {
          ...mergedStyles.close,
          ...originProps?.style
        }
      }));
    }
  });
  const isNeedWave = isFunction(restProps.onClick) || children && children.type === "a";
  const iconNode = cloneElement(icon, {
    className: clsx(/* @__PURE__ */ reactExports.isValidElement(icon) ? icon.props?.className : void 0, mergedClassNames.icon),
    style: mergedStyles.icon
  });
  const child = iconNode ? /* @__PURE__ */ reactExports.createElement(reactExports.Fragment, null, iconNode, children && /* @__PURE__ */ reactExports.createElement("span", {
    className: mergedClassNames.content,
    style: mergedStyles.content
  }, children)) : children;
  const TagWrapper = href ? "a" : "span";
  const tagNode = /* @__PURE__ */ reactExports.createElement(TagWrapper, {
    ...domProps,
    ref,
    className: tagClassName,
    style: tagStyle,
    href: mergedDisabled ? void 0 : href,
    target,
    onClick: mergedDisabled ? void 0 : domProps.onClick,
    ...href && mergedDisabled ? {
      "aria-disabled": true
    } : {}
  }, child, mergedCloseIcon, isPreset && /* @__PURE__ */ reactExports.createElement(PresetCmp, {
    key: "preset",
    prefixCls
  }), isStatus && /* @__PURE__ */ reactExports.createElement(StatusCmp, {
    key: "status",
    prefixCls
  }));
  return isNeedWave ? /* @__PURE__ */ reactExports.createElement(Wave, {
    component: "Tag"
  }, tagNode) : tagNode;
});
const Tag = InternalTag;
Tag.CheckableTag = CheckableTag;
Tag.CheckableTagGroup = CheckableTagGroup;
export {
  Button as B,
  Tag as T,
  convertLegacyProps as c,
  mergeProps as m,
  pickClosable as p,
  useClosable as u
};
