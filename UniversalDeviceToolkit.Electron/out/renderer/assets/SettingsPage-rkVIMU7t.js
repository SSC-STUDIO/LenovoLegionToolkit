import { r as reactExports, e as ConfigContext, f as clsx, D as Divider, g as toArray, M as MenuItem$1, h as omit, k as cloneElement, l as isFunction, m as Tooltip, n as unit, p as genFocusOutline, q as textEllipsis, v as genStyleHooks, w as merge, x as initSlideMotion, y as initZoomMotion, z as clearFix, A as resetComponent, B as FastColor, E as resetIcon, G as useFullPath, H as useZIndex, I as SubMenu$1, J as useComponentConfig, K as useEvent, L as useSemanticRootStyle, N as useMergeSemantic, O as ExportMenu, P as RefIcon, Q as useCSSVarCls, U as initCollapseMotion, V as MenuItemGroup, c as create, W as settingsApi, u as useTranslation, X as useTheme, j as jsxRuntimeExports, T as Typography, Y as changeLanguage, s as staticMethods, C as Card } from "./index-3RTipSd5.js";
import { g as genCollapseMotion, S as Select } from "./index-BxBscas6.js";
import { R as Radio, C as ColorPicker } from "./ColorPicker-CDJwnKVz.js";
import { S as Switch } from "./index-BbS3n2P6.js";
import "./index-QUbxwEY1.js";
import "./index-Hdt_DTHG.js";
import "./Addon-CECo-qGW.js";
import "./Input-mSSMIOSE.js";
const SiderContext = /* @__PURE__ */ reactExports.createContext({});
const MenuContext = /* @__PURE__ */ reactExports.createContext({
  prefixCls: "",
  firstLevel: true,
  inlineCollapsed: false,
  styles: null,
  classNames: null
});
const MenuDivider = (props) => {
  const {
    prefixCls: customizePrefixCls,
    className,
    dashed,
    ...restProps
  } = props;
  const {
    getPrefixCls
  } = reactExports.useContext(ConfigContext);
  const prefixCls = getPrefixCls("menu", customizePrefixCls);
  const classString = clsx({
    [`${prefixCls}-item-divider-dashed`]: !!dashed
  }, className);
  return /* @__PURE__ */ reactExports.createElement(Divider, {
    className: classString,
    ...restProps
  });
};
const MenuItem = (props) => {
  const {
    className,
    children,
    icon,
    title,
    danger,
    extra
  } = props;
  const {
    prefixCls,
    firstLevel,
    direction,
    disableMenuItemTitleTooltip,
    tooltip,
    inlineCollapsed: isInlineCollapsed,
    styles,
    classNames
  } = reactExports.useContext(MenuContext);
  const renderItemChildren = (inlineCollapsed) => {
    const label = children?.[0];
    const wrapNode = /* @__PURE__ */ reactExports.createElement("span", {
      className: clsx(`${prefixCls}-title-content`, firstLevel ? classNames?.itemContent : classNames?.subMenu?.itemContent, {
        [`${prefixCls}-title-content-with-extra`]: !!extra || extra === 0
      }),
      style: firstLevel ? styles?.itemContent : styles?.subMenu?.itemContent
    }, children);
    if (!icon || /* @__PURE__ */ reactExports.isValidElement(children) && children.type === "span") {
      if (children && inlineCollapsed && firstLevel && typeof label === "string") {
        return /* @__PURE__ */ reactExports.createElement("div", {
          className: `${prefixCls}-inline-collapsed-noicon`
        }, label.charAt(0));
      }
    }
    return wrapNode;
  };
  const {
    siderCollapsed
  } = reactExports.useContext(SiderContext);
  let tooltipTitle = title;
  if (typeof title === "undefined") {
    tooltipTitle = firstLevel ? children : "";
  } else if (title === false) {
    tooltipTitle = "";
  }
  const tooltipConfig = tooltip === false ? void 0 : tooltip;
  const mergedTooltipTitle = tooltipConfig && tooltipConfig.title !== void 0 ? tooltipConfig.title : tooltipTitle;
  const tooltipProps = {
    ...tooltipConfig ?? null,
    title: mergedTooltipTitle
  };
  if (!siderCollapsed && !isInlineCollapsed) {
    tooltipProps.title = null;
    tooltipProps.open = false;
  }
  const childrenLength = toArray(children).length;
  let returnNode = /* @__PURE__ */ reactExports.createElement(MenuItem$1, {
    ...omit(props, ["title", "icon", "danger"]),
    className: clsx(firstLevel ? classNames?.item : classNames?.subMenu?.item, {
      [`${prefixCls}-item-danger`]: danger,
      [`${prefixCls}-item-only-child`]: (icon ? childrenLength + 1 : childrenLength) === 1
    }, className),
    style: {
      ...firstLevel ? styles?.item : styles?.subMenu?.item,
      ...props.style
    },
    title: typeof title === "string" ? title : void 0,
    itemData: props?.itemData ?? {
      ...props,
      key: props.eventKey
    }
  }, cloneElement(icon, (oriProps) => ({
    className: clsx(`${prefixCls}-item-icon`, firstLevel ? classNames?.itemIcon : classNames?.subMenu?.itemIcon, oriProps.className),
    style: {
      ...firstLevel ? styles?.itemIcon : styles?.subMenu?.itemIcon,
      ...oriProps.style
    }
  })), renderItemChildren(isInlineCollapsed));
  if (!disableMenuItemTitleTooltip && tooltip !== false) {
    const mergedTooltipPlacement = tooltipConfig && tooltipConfig.placement ? tooltipConfig.placement : direction === "rtl" ? "left" : "right";
    const baseTooltipClassName = `${prefixCls}-inline-collapsed-tooltip`;
    const mergeTooltipRootClassName = (classNames2) => ({
      ...classNames2,
      root: clsx(baseTooltipClassName, classNames2?.root)
    });
    const mergedTooltipClassNames = isFunction(tooltipConfig?.classNames) ? (info) => {
      const resolvedClassNames = tooltipConfig.classNames(info);
      return mergeTooltipRootClassName(resolvedClassNames);
    } : mergeTooltipRootClassName(tooltipConfig?.classNames);
    returnNode = /* @__PURE__ */ reactExports.createElement(Tooltip, {
      ...tooltipProps,
      placement: mergedTooltipPlacement,
      classNames: mergedTooltipClassNames
    }, returnNode);
  }
  return returnNode;
};
const OverrideContext = /* @__PURE__ */ reactExports.createContext(null);
const getHorizontalStyle = (token) => {
  const {
    componentCls,
    motionDurationSlow,
    horizontalLineHeight,
    colorSplit,
    lineWidth,
    lineType,
    itemPaddingInline
  } = token;
  return {
    [`${componentCls}-horizontal`]: {
      lineHeight: horizontalLineHeight,
      border: 0,
      borderBottom: `${unit(lineWidth)} ${lineType} ${colorSplit}`,
      boxShadow: "none",
      "&::after": {
        display: "block",
        clear: "both",
        height: 0,
        content: '"\\20"'
      },
      // ======================= Item =======================
      [`${componentCls}-item, ${componentCls}-submenu`]: {
        position: "relative",
        display: "inline-block",
        verticalAlign: "bottom",
        paddingInline: itemPaddingInline
      },
      [`> ${componentCls}-item:hover,
        > ${componentCls}-item-active,
        > ${componentCls}-submenu ${componentCls}-submenu-title:hover`]: {
        backgroundColor: "transparent"
      },
      [`${componentCls}-item, ${componentCls}-submenu-title`]: {
        transition: [`border-color`, `background-color`].map((prop) => `${prop} ${motionDurationSlow}`).join(",")
      },
      // ===================== Sub Menu =====================
      [`${componentCls}-submenu-arrow`]: {
        display: "none"
      }
    }
  };
};
const getRTLStyle = ({
  componentCls,
  menuArrowOffset,
  calc
}) => ({
  [`${componentCls}-rtl`]: {
    direction: "rtl"
  },
  [`${componentCls}-submenu-rtl`]: {
    transformOrigin: "100% 0"
  },
  // Vertical Arrow
  [`${componentCls}-rtl${componentCls}-vertical,
    ${componentCls}-submenu-rtl ${componentCls}-vertical`]: {
    [`${componentCls}-submenu-arrow`]: {
      "&::before": {
        transform: `rotate(-45deg) translateY(${unit(calc(menuArrowOffset).mul(-1).equal())})`
      },
      "&::after": {
        transform: `rotate(45deg) translateY(${unit(menuArrowOffset)})`
      }
    }
  }
});
const accessibilityFocus = (token) => genFocusOutline(token);
const getThemeStyle = (token, themeSuffix) => {
  const {
    componentCls,
    itemColor,
    itemSelectedColor,
    subMenuItemSelectedColor,
    groupTitleColor,
    itemBg,
    subMenuItemBg,
    itemSelectedBg,
    activeBarHeight,
    activeBarWidth,
    activeBarBorderWidth,
    motionDurationSlow,
    motionEaseInOut,
    motionEaseOut,
    itemPaddingInline,
    motionDurationMid,
    itemHoverColor,
    lineType,
    colorSplit,
    // Disabled
    itemDisabledColor,
    // Danger
    dangerItemColor,
    dangerItemHoverColor,
    dangerItemSelectedColor,
    dangerItemActiveBg,
    dangerItemSelectedBg,
    // Bg
    popupBg,
    itemHoverBg,
    itemActiveBg,
    menuSubMenuBg,
    // Horizontal
    horizontalItemSelectedColor,
    horizontalItemSelectedBg,
    horizontalItemBorderRadius,
    horizontalItemHoverBg
  } = token;
  return {
    [`${componentCls}-${themeSuffix}, ${componentCls}-${themeSuffix} > ${componentCls}`]: {
      color: itemColor,
      background: itemBg,
      [`&${componentCls}-root:focus-visible`]: {
        ...accessibilityFocus(token)
      },
      // ======================== Item ========================
      [`${componentCls}-item`]: {
        "&-group-title, &-extra": {
          color: groupTitleColor
        }
      },
      [`${componentCls}-submenu-selected > ${componentCls}-submenu-title`]: {
        color: subMenuItemSelectedColor
      },
      [`${componentCls}-item, ${componentCls}-submenu-title`]: {
        color: itemColor,
        [`&:not(${componentCls}-item-disabled):focus-visible`]: {
          ...accessibilityFocus(token)
        }
      },
      // Disabled
      [`${componentCls}-item-disabled, ${componentCls}-submenu-disabled`]: {
        color: `${itemDisabledColor} !important`
      },
      // Hover
      [`${componentCls}-item:not(${componentCls}-item-selected):not(${componentCls}-submenu-selected)`]: {
        [`&:hover, > ${componentCls}-submenu-title:hover`]: {
          color: itemHoverColor
        }
      },
      // SubMenu active (when hover on parent menu item)
      [`${componentCls}-submenu:not(${componentCls}-submenu-selected)`]: {
        [`> ${componentCls}-submenu-title:hover`]: {
          color: itemHoverColor
        }
      },
      [`&:not(${componentCls}-horizontal)`]: {
        [`${componentCls}-item:not(${componentCls}-item-selected)`]: {
          "&:hover": {
            backgroundColor: itemHoverBg
          },
          "&:active": {
            backgroundColor: itemActiveBg
          }
        },
        [`${componentCls}-submenu-title`]: {
          "&:hover": {
            backgroundColor: itemHoverBg
          },
          "&:active": {
            backgroundColor: itemActiveBg
          }
        }
      },
      // Danger - only Item has
      [`${componentCls}-item-danger`]: {
        color: dangerItemColor,
        [`&${componentCls}-item:hover`]: {
          [`&:not(${componentCls}-item-selected):not(${componentCls}-submenu-selected)`]: {
            color: dangerItemHoverColor
          }
        },
        [`&${componentCls}-item:active`]: {
          background: dangerItemActiveBg
        }
      },
      [`${componentCls}-item a`]: {
        "&, &:hover": {
          color: "inherit"
        }
      },
      [`${componentCls}-item-selected`]: {
        color: itemSelectedColor,
        // Danger
        [`&${componentCls}-item-danger`]: {
          color: dangerItemSelectedColor
        },
        "a, a:hover": {
          color: "inherit"
        }
      },
      [`& ${componentCls}-item-selected`]: {
        backgroundColor: itemSelectedBg,
        // Danger
        [`&${componentCls}-item-danger`]: {
          backgroundColor: dangerItemSelectedBg
        }
      },
      [`&${componentCls}-submenu > ${componentCls}`]: {
        backgroundColor: menuSubMenuBg
      },
      // ===== 设置浮层的颜色 =======
      // ！dark 模式会被popupBg 会被rest 为 darkPopupBg
      [`&${componentCls}-popup > ${componentCls}`]: {
        backgroundColor: popupBg
      },
      [`&${componentCls}-submenu-popup > ${componentCls}`]: {
        backgroundColor: popupBg
      },
      // ===== 设置浮层的颜色 end =======
      // ====================== Horizontal ======================
      [`&${componentCls}-horizontal`]: {
        ...themeSuffix === "dark" ? {
          borderBottom: 0
        } : {},
        [`> ${componentCls}-item, > ${componentCls}-submenu`]: {
          top: activeBarBorderWidth,
          marginTop: token.calc(activeBarBorderWidth).mul(-1).equal(),
          marginBottom: 0,
          borderRadius: horizontalItemBorderRadius,
          "&::after": {
            position: "absolute",
            insetInline: itemPaddingInline,
            bottom: 0,
            borderBottom: `${unit(activeBarHeight)} solid transparent`,
            transition: `border-color ${motionDurationSlow} ${motionEaseInOut}`,
            content: '""'
          },
          "&:hover, &-active, &-open": {
            background: horizontalItemHoverBg,
            "&::after": {
              borderBottomWidth: activeBarHeight,
              borderBottomColor: horizontalItemSelectedColor
            }
          },
          "&-selected": {
            color: horizontalItemSelectedColor,
            backgroundColor: horizontalItemSelectedBg,
            "&:hover": {
              backgroundColor: horizontalItemSelectedBg
            },
            "&::after": {
              borderBottomWidth: activeBarHeight,
              borderBottomColor: horizontalItemSelectedColor
            }
          }
        }
      },
      // ================== Inline & Vertical ===================
      //
      [`&${componentCls}-root`]: {
        [`&${componentCls}-inline, &${componentCls}-vertical`]: {
          borderInlineEnd: `${unit(activeBarBorderWidth)} ${lineType} ${colorSplit}`
        }
      },
      // ======================== Inline ========================
      [`&${componentCls}-inline`]: {
        // Sub
        [`${componentCls}-sub${componentCls}-inline`]: {
          background: subMenuItemBg
        },
        [`${componentCls}-item`]: {
          position: "relative",
          "&::after": {
            position: "absolute",
            insetBlock: 0,
            insetInlineEnd: 0,
            borderInlineEnd: `${unit(activeBarWidth)} solid ${itemSelectedColor}`,
            transform: "scaleY(0.0001)",
            opacity: 0,
            transition: [`transform`, `opacity`].map((prop) => `${prop} ${motionDurationMid} ${motionEaseOut}`).join(","),
            content: '""'
          },
          // Danger
          [`&${componentCls}-item-danger`]: {
            "&::after": {
              borderInlineEndColor: dangerItemSelectedColor
            }
          }
        },
        [`${componentCls}-selected, ${componentCls}-item-selected`]: {
          "&::after": {
            transform: "scaleY(1)",
            opacity: 1,
            transition: [`transform`, `opacity`].map((prop) => `${prop} ${motionDurationMid} ${motionEaseInOut}`).join(",")
          }
        }
      }
    }
  };
};
const getVerticalInlineStyle = (token) => {
  const {
    componentCls,
    itemHeight,
    itemMarginInline,
    padding,
    menuArrowSize,
    marginXS,
    itemMarginBlock,
    itemWidth,
    itemPaddingInline
  } = token;
  const paddingWithArrow = token.calc(menuArrowSize).add(padding).add(marginXS).equal();
  return {
    [`${componentCls}-item`]: {
      position: "relative",
      overflow: "hidden"
    },
    [`${componentCls}-item, ${componentCls}-submenu-title`]: {
      height: itemHeight,
      lineHeight: unit(itemHeight),
      paddingInline: itemPaddingInline,
      overflow: "hidden",
      textOverflow: "ellipsis",
      marginInline: itemMarginInline,
      marginBlock: itemMarginBlock,
      width: itemWidth
    },
    [`> ${componentCls}-item,
            > ${componentCls}-submenu > ${componentCls}-submenu-title`]: {
      height: itemHeight,
      lineHeight: unit(itemHeight)
    },
    [`${componentCls}-item-group-list ${componentCls}-submenu-title,
            ${componentCls}-submenu-title`]: {
      paddingInlineEnd: paddingWithArrow
    }
  };
};
const getVerticalStyle = (token) => {
  const {
    componentCls,
    iconCls,
    itemHeight,
    colorTextLightSolid,
    dropdownWidth,
    controlHeightLG,
    motionEaseOut,
    padding,
    paddingXL,
    itemMarginInline,
    fontSizeLG,
    motionDurationFast,
    motionDurationSlow,
    paddingXS,
    boxShadowSecondary,
    collapsedWidth,
    collapsedIconSize
  } = token;
  const inlineItemStyle = {
    height: itemHeight,
    lineHeight: unit(itemHeight),
    listStylePosition: "inside",
    listStyleType: "disc"
  };
  return [
    {
      [componentCls]: {
        "&-inline, &-vertical": {
          [`&${componentCls}-root`]: {
            boxShadow: "none"
          },
          ...getVerticalInlineStyle(token)
        }
      },
      [`${componentCls}-submenu-popup`]: {
        [`${componentCls}-vertical`]: {
          ...getVerticalInlineStyle(token),
          boxShadow: boxShadowSecondary
        }
      }
    },
    // Vertical only
    {
      [`${componentCls}-submenu-popup ${componentCls}-vertical${componentCls}-sub`]: {
        minWidth: dropdownWidth,
        maxHeight: `calc(100vh - ${unit(token.calc(controlHeightLG).mul(2.5).equal())})`,
        padding: "0",
        overflow: "hidden",
        borderInlineEnd: 0,
        // https://github.com/ant-design/ant-design/issues/22244
        // https://github.com/ant-design/ant-design/issues/26812
        "&:not([class*='-active'])": {
          overflowX: "hidden",
          overflowY: "auto"
        }
      }
    },
    // Inline Only
    {
      [`${componentCls}-inline`]: {
        width: "100%",
        // Motion enhance for first level
        [`&${componentCls}-root`]: {
          [`${componentCls}-item, ${componentCls}-submenu-title`]: {
            display: "flex",
            alignItems: "center",
            transition: [`border-color ${motionDurationSlow}`, `background-color ${motionDurationSlow}`, `padding ${motionDurationFast} ${motionEaseOut}`].join(","),
            [`> ${componentCls}-title-content`]: {
              flex: "auto",
              minWidth: 0,
              overflow: "hidden",
              textOverflow: "ellipsis"
            },
            "> *": {
              flex: "none"
            }
          }
        },
        // >>>>> Sub
        [`${componentCls}-sub${componentCls}-inline`]: {
          padding: 0,
          border: 0,
          borderRadius: 0,
          boxShadow: "none",
          [`& > ${componentCls}-submenu > ${componentCls}-submenu-title`]: inlineItemStyle,
          [`& ${componentCls}-item-group-title`]: {
            paddingInlineStart: paddingXL
          }
        },
        // >>>>> Item
        [`${componentCls}-item`]: inlineItemStyle
      }
    },
    // Inline Collapse Only
    {
      [`${componentCls}-inline-collapsed`]: {
        width: collapsedWidth,
        [`&${componentCls}-root`]: {
          [`${componentCls}-item, ${componentCls}-submenu ${componentCls}-submenu-title`]: {
            [`> ${componentCls}-inline-collapsed-noicon`]: {
              fontSize: fontSizeLG,
              textAlign: "center",
              width: "100%"
            }
          }
        },
        [`> ${componentCls}-item,
          > ${componentCls}-item-group > ${componentCls}-item-group-list > ${componentCls}-item,
          > ${componentCls}-item-group > ${componentCls}-item-group-list > ${componentCls}-submenu > ${componentCls}-submenu-title,
          > ${componentCls}-submenu > ${componentCls}-submenu-title`]: {
          display: "flex",
          alignItems: "center",
          justifyContent: "flex-start",
          insetInlineStart: 0,
          paddingInline: `calc(50% - ${unit(token.calc(collapsedIconSize).div(2).equal())} - ${unit(itemMarginInline)})`,
          textOverflow: "clip",
          [`
            ${componentCls}-submenu-arrow,
            ${componentCls}-submenu-expand-icon
          `]: {
            opacity: 0
          },
          [`> ${componentCls}-title-content`]: {
            width: 0,
            opacity: 0,
            overflow: "hidden"
          },
          [`${componentCls}-item-icon, ${iconCls}`]: {
            margin: 0,
            fontSize: collapsedIconSize,
            lineHeight: unit(itemHeight),
            "+ span": {
              display: "inline-block",
              width: 0,
              opacity: 0,
              overflow: "hidden",
              marginInlineStart: 0
            }
          }
        },
        [`${componentCls}-item-icon, ${iconCls}`]: {
          display: "inline-block"
        },
        "&-tooltip": {
          pointerEvents: "none",
          [`${componentCls}-item-icon, ${iconCls}`]: {
            display: "none"
          },
          [`${componentCls}-item-extra`]: {
            paddingInlineStart: padding
          },
          "a, a:hover": {
            color: colorTextLightSolid
          }
        },
        [`${componentCls}-item-group-title`]: {
          ...textEllipsis,
          paddingInline: paddingXS
        }
      }
    }
  ];
};
const genMenuItemStyle = (token) => {
  const {
    componentCls,
    motionDurationSlow,
    motionDurationMid,
    motionEaseInOut,
    motionEaseOut,
    iconCls,
    iconSize,
    iconMarginInlineEnd
  } = token;
  return {
    // >>>>> Item
    [`${componentCls}-item, ${componentCls}-submenu-title`]: {
      position: "relative",
      display: "block",
      margin: 0,
      whiteSpace: "nowrap",
      cursor: "pointer",
      transition: [`border-color ${motionDurationSlow}`, `background-color ${motionDurationSlow}`, `padding calc(${motionDurationSlow} + 0.1s) ${motionEaseInOut}`].join(","),
      [`${componentCls}-item-icon, ${iconCls}`]: {
        minWidth: iconSize,
        fontSize: iconSize,
        transition: [`font-size ${motionDurationMid} ${motionEaseOut}`, `margin ${motionDurationSlow} ${motionEaseInOut}`, `color ${motionDurationSlow}`].join(","),
        "+ span": {
          marginInlineStart: iconMarginInlineEnd,
          opacity: 1,
          transition: [`opacity ${motionDurationSlow} ${motionEaseInOut}`, `margin ${motionDurationSlow}`, `color ${motionDurationSlow}`].join(",")
        }
      },
      [`${componentCls}-item-icon`]: {
        ...resetIcon()
      },
      [`&${componentCls}-item-only-child`]: {
        [`> ${iconCls}, > ${componentCls}-item-icon`]: {
          marginInlineEnd: 0
        }
      }
    },
    // Disabled state sets text to gray and nukes hover/tab effects
    [`${componentCls}-item-disabled, ${componentCls}-submenu-disabled`]: {
      background: "none !important",
      cursor: "not-allowed",
      "&::after": {
        borderColor: "transparent !important"
      },
      a: {
        color: "inherit !important",
        cursor: "not-allowed",
        pointerEvents: "none"
      },
      [`> ${componentCls}-submenu-title`]: {
        color: "inherit !important",
        cursor: "not-allowed"
      }
    }
  };
};
const genSubMenuArrowStyle = (token) => {
  const {
    componentCls,
    motionDurationSlow,
    motionEaseInOut,
    borderRadius,
    menuArrowSize,
    menuArrowOffset
  } = token;
  return {
    [`${componentCls}-submenu`]: {
      "&-expand-icon, &-arrow": {
        position: "absolute",
        top: "50%",
        insetInlineEnd: token.margin,
        width: menuArrowSize,
        color: "currentcolor",
        transform: "translateY(-50%)",
        transition: ["transform", "opacity"].map((prop) => `${prop} ${motionDurationSlow}`).join(",")
      },
      "&-arrow": {
        // →
        "&::before, &::after": {
          position: "absolute",
          width: token.calc(menuArrowSize).mul(0.6).equal(),
          height: token.calc(menuArrowSize).mul(0.15).equal(),
          backgroundColor: "currentcolor",
          borderRadius,
          transition: [`background-color`, `transform`, `top`, `color`].map((prop) => `${prop} ${motionDurationSlow} ${motionEaseInOut}`).join(","),
          content: '""'
        },
        "&::before": {
          transform: `rotate(45deg) translateY(${unit(token.calc(menuArrowOffset).mul(-1).equal())})`
        },
        "&::after": {
          transform: `rotate(-45deg) translateY(${unit(menuArrowOffset)})`
        }
      }
    }
  };
};
const getBaseStyle = (token) => {
  const {
    antCls,
    componentCls,
    fontSize,
    motionDurationSlow,
    motionDurationMid,
    motionEaseInOut,
    paddingXS,
    padding,
    colorSplit,
    lineWidth,
    zIndexPopup,
    borderRadiusLG,
    subMenuItemBorderRadius,
    menuArrowSize,
    menuArrowOffset,
    lineType,
    groupTitleLineHeight,
    groupTitleFontSize,
    iconSize,
    iconMarginInlineEnd
  } = token;
  const titleContentTypographyEllipsisSelector = [`> ${antCls}-typography-ellipsis-single-line`, `> ${componentCls}-item-label > ${antCls}-typography-ellipsis-single-line`].join(",");
  return [
    // Misc
    {
      "": {
        [componentCls]: {
          ...clearFix(),
          // Hidden
          "&-hidden": {
            display: "none"
          }
        }
      },
      [`${componentCls}-submenu-hidden`]: {
        display: "none"
      }
    },
    {
      [componentCls]: {
        ...resetComponent(token),
        ...clearFix(),
        marginBottom: 0,
        paddingInlineStart: 0,
        // Override default ul/ol
        fontSize,
        lineHeight: 0,
        // Fix display inline-block gap
        listStyle: "none",
        outline: "none",
        // Magic cubic here but smooth transition
        transition: `width ${motionDurationSlow} cubic-bezier(0.2, 0, 0, 1) 0s`,
        "ul, ol": {
          margin: 0,
          padding: 0,
          listStyle: "none"
        },
        // Overflow ellipsis
        "&-overflow": {
          display: "flex",
          [`${componentCls}-item`]: {
            flex: "none"
          }
        },
        [`${componentCls}-item, ${componentCls}-submenu, ${componentCls}-submenu-title`]: {
          borderRadius: token.itemBorderRadius
        },
        [`${componentCls}-item-group-title`]: {
          padding: `${unit(paddingXS)} ${unit(padding)}`,
          fontSize: groupTitleFontSize,
          lineHeight: groupTitleLineHeight,
          transition: `all ${motionDurationSlow}`
        },
        [`&-horizontal ${componentCls}-submenu`]: {
          transition: [`border-color`, `background-color`].map((prop) => `${prop} ${motionDurationSlow} ${motionEaseInOut}`).join(",")
        },
        [`${componentCls}-submenu, ${componentCls}-submenu-inline`]: {
          transition: [`border-color ${motionDurationSlow}`, `background-color ${motionDurationSlow}`, `padding ${motionDurationMid}`].map((prop) => `${prop} ${motionEaseInOut}`).join(",")
        },
        [`${componentCls}-submenu ${componentCls}-sub`]: {
          cursor: "initial",
          transition: [`background-color`, `padding`].map((prop) => `${prop} ${motionDurationSlow} ${motionEaseInOut}`).join(",")
        },
        [`${componentCls}-title-content`]: {
          transition: `color ${motionDurationSlow}`,
          "&-with-extra": {
            display: "inline-flex",
            alignItems: "center",
            width: "100%",
            minWidth: 0
          },
          [`${componentCls}-item-label`]: {
            flex: "auto",
            minWidth: 0,
            ...textEllipsis
          },
          // https://github.com/ant-design/ant-design/issues/41143
          [titleContentTypographyEllipsisSelector]: {
            display: "inline",
            verticalAlign: "unset"
          },
          [`${componentCls}-item-extra`]: {
            flex: "none",
            marginInlineStart: "auto",
            paddingInlineStart: token.padding
          }
        },
        [`${componentCls}-item-icon + ${componentCls}-title-content-with-extra`]: {
          width: `calc(100% - ${unit(token.calc(iconSize).add(iconMarginInlineEnd ?? 0).equal())})`
        },
        [`${componentCls}-item a`]: {
          "&::before": {
            position: "absolute",
            inset: 0,
            backgroundColor: "transparent",
            content: '""'
          }
        },
        // Removed a Badge related style seems it's safe
        // https://github.com/ant-design/ant-design/issues/19809
        // >>>>> Divider
        [`${componentCls}-item-divider`]: {
          overflow: "hidden",
          lineHeight: 0,
          borderColor: colorSplit,
          borderStyle: lineType,
          borderWidth: 0,
          borderTopWidth: lineWidth,
          marginBlock: lineWidth,
          padding: 0,
          "&-dashed": {
            borderStyle: "dashed"
          }
        },
        // Item
        ...genMenuItemStyle(token),
        [`${componentCls}-item-group`]: {
          [`${componentCls}-item-group-list`]: {
            margin: 0,
            padding: 0,
            [`${componentCls}-item, ${componentCls}-submenu-title`]: {
              paddingInline: `${unit(token.calc(fontSize).mul(2).equal())} ${unit(padding)}`
            }
          }
        },
        // ======================= Sub Menu =======================
        "&-submenu": {
          "&-popup": {
            position: "absolute",
            zIndex: zIndexPopup,
            borderRadius: borderRadiusLG,
            boxShadow: "none",
            transformOrigin: "0 0",
            [`&${componentCls}-submenu`]: {
              background: "transparent"
            },
            // https://github.com/ant-design/ant-design/issues/13955
            "&::before": {
              position: "absolute",
              inset: 0,
              zIndex: -1,
              width: "100%",
              height: "100%",
              opacity: 0,
              content: '""'
            },
            [`> ${componentCls}`]: {
              borderRadius: borderRadiusLG,
              ...genMenuItemStyle(token),
              ...genSubMenuArrowStyle(token),
              [`${componentCls}-item, ${componentCls}-submenu > ${componentCls}-submenu-title`]: {
                borderRadius: subMenuItemBorderRadius
              },
              [`${componentCls}-submenu-title::after`]: {
                transition: `transform ${motionDurationSlow} ${motionEaseInOut}`
              }
            }
          },
          "&-placement-leftTop, &-placement-bottomRight": {
            transformOrigin: "100% 0"
          },
          "&-placement-leftBottom, &-placement-topRight": {
            transformOrigin: "100% 100%"
          },
          "&-placement-rightBottom, &-placement-topLeft": {
            transformOrigin: "0 100%"
          },
          "&-placement-bottomLeft, &-placement-rightTop": {
            transformOrigin: "0 0"
          },
          "&-placement-leftTop, &-placement-leftBottom": {
            paddingInlineEnd: token.paddingXS
          },
          "&-placement-rightTop, &-placement-rightBottom": {
            paddingInlineStart: token.paddingXS
          },
          "&-placement-topRight, &-placement-topLeft": {
            paddingBottom: token.paddingXS
          },
          "&-placement-bottomRight, &-placement-bottomLeft": {
            paddingTop: token.paddingXS
          }
        },
        ...genSubMenuArrowStyle(token),
        [`&-inline-collapsed ${componentCls}-submenu-arrow,
        &-inline ${componentCls}-submenu-arrow`]: {
          // ↓
          "&::before": {
            transform: `rotate(-45deg) translateX(${unit(menuArrowOffset)})`
          },
          "&::after": {
            transform: `rotate(45deg) translateX(${unit(token.calc(menuArrowOffset).mul(-1).equal())})`
          }
        },
        [`${componentCls}-submenu-open${componentCls}-submenu-inline > ${componentCls}-submenu-title > ${componentCls}-submenu-arrow`]: {
          // ↑
          transform: `translateY(${unit(token.calc(menuArrowSize).mul(0.2).mul(-1).equal())})`,
          "&::after": {
            transform: `rotate(-45deg) translateX(${unit(token.calc(menuArrowOffset).mul(-1).equal())})`
          },
          "&::before": {
            transform: `rotate(45deg) translateX(${unit(menuArrowOffset)})`
          }
        }
      }
    },
    // Integration with header element so menu items have the same height
    {
      [`${antCls}-layout-header`]: {
        [componentCls]: {
          lineHeight: "inherit"
        }
      }
    }
  ];
};
const prepareComponentToken = (token) => {
  const {
    colorPrimary,
    colorError,
    colorTextDisabled,
    colorErrorBg,
    colorText,
    colorTextDescription,
    colorBgContainer,
    colorFillAlter,
    colorFillContent,
    lineWidth,
    lineWidthBold,
    controlItemBgActive,
    colorBgTextHover,
    controlHeightLG,
    lineHeight,
    colorBgElevated,
    marginXXS,
    padding,
    fontSize,
    controlHeightSM,
    fontSizeLG,
    colorTextLightSolid,
    colorErrorHover
  } = token;
  const activeBarWidth = token.activeBarWidth ?? 0;
  const activeBarBorderWidth = token.activeBarBorderWidth ?? lineWidth;
  const itemMarginInline = token.itemMarginInline ?? token.marginXXS;
  const colorTextDark = new FastColor(colorTextLightSolid).setA(0.65).toRgbString();
  return {
    dropdownWidth: 160,
    zIndexPopup: token.zIndexPopupBase + 50,
    radiusItem: token.borderRadiusLG,
    itemBorderRadius: token.borderRadiusLG,
    radiusSubMenuItem: token.borderRadiusSM,
    subMenuItemBorderRadius: token.borderRadiusSM,
    colorItemText: colorText,
    itemColor: colorText,
    colorItemTextHover: colorText,
    itemHoverColor: colorText,
    colorItemTextHoverHorizontal: colorPrimary,
    horizontalItemHoverColor: colorPrimary,
    colorGroupTitle: colorTextDescription,
    groupTitleColor: colorTextDescription,
    colorItemTextSelected: colorPrimary,
    itemSelectedColor: colorPrimary,
    subMenuItemSelectedColor: colorPrimary,
    colorItemTextSelectedHorizontal: colorPrimary,
    horizontalItemSelectedColor: colorPrimary,
    colorItemBg: colorBgContainer,
    itemBg: colorBgContainer,
    colorItemBgHover: colorBgTextHover,
    itemHoverBg: colorBgTextHover,
    colorItemBgActive: colorFillContent,
    itemActiveBg: controlItemBgActive,
    colorSubItemBg: colorFillAlter,
    subMenuItemBg: colorFillAlter,
    colorItemBgSelected: controlItemBgActive,
    itemSelectedBg: controlItemBgActive,
    colorItemBgSelectedHorizontal: "transparent",
    horizontalItemSelectedBg: "transparent",
    colorActiveBarWidth: 0,
    activeBarWidth,
    colorActiveBarHeight: lineWidthBold,
    activeBarHeight: lineWidthBold,
    colorActiveBarBorderSize: lineWidth,
    activeBarBorderWidth,
    // Disabled
    colorItemTextDisabled: colorTextDisabled,
    itemDisabledColor: colorTextDisabled,
    // Danger
    colorDangerItemText: colorError,
    dangerItemColor: colorError,
    colorDangerItemTextHover: colorError,
    dangerItemHoverColor: colorError,
    colorDangerItemTextSelected: colorError,
    dangerItemSelectedColor: colorError,
    colorDangerItemBgActive: colorErrorBg,
    dangerItemActiveBg: colorErrorBg,
    colorDangerItemBgSelected: colorErrorBg,
    dangerItemSelectedBg: colorErrorBg,
    itemMarginInline,
    horizontalItemBorderRadius: 0,
    horizontalItemHoverBg: "transparent",
    itemHeight: controlHeightLG,
    groupTitleLineHeight: lineHeight,
    collapsedWidth: controlHeightLG * 2,
    popupBg: colorBgElevated,
    itemMarginBlock: marginXXS,
    itemPaddingInline: padding,
    horizontalLineHeight: `${controlHeightLG * 1.15}px`,
    iconSize: fontSize,
    iconMarginInlineEnd: controlHeightSM - fontSize,
    collapsedIconSize: fontSizeLG,
    groupTitleFontSize: fontSize,
    // Disabled
    darkItemDisabledColor: new FastColor(colorTextLightSolid).setA(0.25).toRgbString(),
    // Dark
    darkItemColor: colorTextDark,
    darkDangerItemColor: colorError,
    darkItemBg: "#001529",
    darkPopupBg: "#001529",
    darkSubMenuItemBg: "#000c17",
    darkItemSelectedColor: colorTextLightSolid,
    darkItemSelectedBg: colorPrimary,
    darkDangerItemSelectedBg: colorError,
    darkItemHoverBg: "transparent",
    darkGroupTitleColor: colorTextDark,
    darkItemHoverColor: colorTextLightSolid,
    darkDangerItemHoverColor: colorErrorHover,
    darkDangerItemSelectedColor: colorTextLightSolid,
    darkDangerItemActiveBg: colorError,
    // internal
    itemWidth: activeBarWidth ? `calc(100% + ${activeBarBorderWidth}px)` : `calc(100% - ${itemMarginInline * 2}px)`
  };
};
const useStyle = (prefixCls, rootCls = prefixCls, injectStyle = true) => {
  const useStyle2 = genStyleHooks("Menu", (token) => {
    const {
      colorBgElevated,
      controlHeightLG,
      fontSize,
      darkItemColor,
      darkDangerItemColor,
      darkItemBg,
      darkSubMenuItemBg,
      darkItemSelectedColor,
      darkItemSelectedBg,
      darkDangerItemSelectedBg,
      darkItemHoverBg,
      darkGroupTitleColor,
      darkItemHoverColor,
      darkItemDisabledColor,
      darkDangerItemHoverColor,
      darkDangerItemSelectedColor,
      darkDangerItemActiveBg,
      popupBg,
      darkPopupBg
    } = token;
    const menuArrowSize = token.calc(fontSize).div(7).mul(5).equal();
    const menuToken = merge(token, {
      menuArrowSize,
      menuHorizontalHeight: token.calc(controlHeightLG).mul(1.15).equal(),
      menuArrowOffset: token.calc(menuArrowSize).mul(0.25).equal(),
      menuSubMenuBg: colorBgElevated,
      calc: token.calc,
      popupBg
    });
    const menuDarkToken = merge(menuToken, {
      itemColor: darkItemColor,
      itemHoverColor: darkItemHoverColor,
      groupTitleColor: darkGroupTitleColor,
      itemSelectedColor: darkItemSelectedColor,
      subMenuItemSelectedColor: darkItemSelectedColor,
      itemBg: darkItemBg,
      popupBg: darkPopupBg,
      subMenuItemBg: darkSubMenuItemBg,
      itemActiveBg: "transparent",
      itemSelectedBg: darkItemSelectedBg,
      activeBarHeight: 0,
      activeBarBorderWidth: 0,
      itemHoverBg: darkItemHoverBg,
      // Disabled
      itemDisabledColor: darkItemDisabledColor,
      // Danger
      dangerItemColor: darkDangerItemColor,
      dangerItemHoverColor: darkDangerItemHoverColor,
      dangerItemSelectedColor: darkDangerItemSelectedColor,
      dangerItemActiveBg: darkDangerItemActiveBg,
      dangerItemSelectedBg: darkDangerItemSelectedBg,
      menuSubMenuBg: darkSubMenuItemBg,
      // Horizontal
      horizontalItemSelectedColor: darkItemSelectedColor,
      horizontalItemSelectedBg: darkItemSelectedBg
    });
    return [
      // Basic
      getBaseStyle(menuToken),
      // Horizontal
      getHorizontalStyle(menuToken),
      // Hard code for some light style
      // Vertical
      getVerticalStyle(menuToken),
      // Hard code for some light style
      // Theme
      getThemeStyle(menuToken, "light"),
      getThemeStyle(menuDarkToken, "dark"),
      // RTL
      getRTLStyle(menuToken),
      // Motion
      genCollapseMotion(menuToken),
      initSlideMotion(menuToken, "slide-up"),
      initSlideMotion(menuToken, "slide-down"),
      initZoomMotion(menuToken, "zoom-big")
    ];
  }, prepareComponentToken, {
    deprecatedTokens: [["colorGroupTitle", "groupTitleColor"], ["radiusItem", "itemBorderRadius"], ["radiusSubMenuItem", "subMenuItemBorderRadius"], ["colorItemText", "itemColor"], ["colorItemTextHover", "itemHoverColor"], ["colorItemTextHoverHorizontal", "horizontalItemHoverColor"], ["colorItemTextSelected", "itemSelectedColor"], ["colorItemTextSelectedHorizontal", "horizontalItemSelectedColor"], ["colorItemTextDisabled", "itemDisabledColor"], ["colorDangerItemText", "dangerItemColor"], ["colorDangerItemTextHover", "dangerItemHoverColor"], ["colorDangerItemTextSelected", "dangerItemSelectedColor"], ["colorDangerItemBgActive", "dangerItemActiveBg"], ["colorDangerItemBgSelected", "dangerItemSelectedBg"], ["colorItemBg", "itemBg"], ["colorItemBgHover", "itemHoverBg"], ["colorSubItemBg", "subMenuItemBg"], ["colorItemBgActive", "itemActiveBg"], ["colorItemBgSelectedHorizontal", "horizontalItemSelectedBg"], ["colorActiveBarWidth", "activeBarWidth"], ["colorActiveBarHeight", "activeBarHeight"], ["colorActiveBarBorderSize", "activeBarBorderWidth"], ["colorItemBgSelected", "itemSelectedBg"]],
    // Dropdown will handle menu style self. We do not need to handle this.
    injectStyle,
    unitless: {
      groupTitleLineHeight: true
    }
  });
  return useStyle2(prefixCls, rootCls);
};
const SubMenu = (props) => {
  const {
    popupClassName,
    icon,
    title,
    theme: customTheme
  } = props;
  const context = reactExports.useContext(MenuContext);
  const {
    prefixCls,
    inlineCollapsed,
    theme: contextTheme,
    classNames,
    styles
  } = context;
  const parentPath = useFullPath();
  let titleNode;
  if (!icon) {
    titleNode = inlineCollapsed && !parentPath.length && title && typeof title === "string" ? /* @__PURE__ */ reactExports.createElement("div", {
      className: `${prefixCls}-inline-collapsed-noicon`
    }, title.charAt(0)) : /* @__PURE__ */ reactExports.createElement("span", {
      className: `${prefixCls}-title-content`
    }, title);
  } else {
    const titleIsSpan = /* @__PURE__ */ reactExports.isValidElement(title) && title.type === "span";
    titleNode = /* @__PURE__ */ reactExports.createElement(reactExports.Fragment, null, cloneElement(icon, (oriProps) => ({
      className: clsx(oriProps.className, `${prefixCls}-item-icon`, classNames?.itemIcon),
      style: {
        ...oriProps.style,
        ...styles?.itemIcon
      }
    })), titleIsSpan ? title : /* @__PURE__ */ reactExports.createElement("span", {
      className: `${prefixCls}-title-content`
    }, title));
  }
  const contextValue = reactExports.useMemo(() => ({
    ...context,
    firstLevel: false
  }), [context]);
  const [zIndex] = useZIndex("Menu");
  return /* @__PURE__ */ reactExports.createElement(MenuContext.Provider, {
    value: contextValue
  }, /* @__PURE__ */ reactExports.createElement(SubMenu$1, {
    ...omit(props, ["icon"]),
    title: titleNode,
    classNames: {
      list: classNames?.subMenu?.list,
      listTitle: classNames?.subMenu?.itemTitle
    },
    styles: {
      list: styles?.subMenu?.list,
      listTitle: styles?.subMenu?.itemTitle
    },
    popupClassName: clsx(prefixCls, popupClassName, classNames?.popup?.root, `${prefixCls}-${customTheme || contextTheme}`),
    popupStyle: {
      zIndex,
      // fix: https://github.com/ant-design/ant-design/issues/47826#issuecomment-2360737237
      ...props.popupStyle,
      ...styles?.popup?.root
    }
  }));
};
function isEmptyIcon(icon) {
  return icon === null || icon === false;
}
const MENU_COMPONENTS = {
  item: MenuItem,
  submenu: SubMenu,
  divider: MenuDivider
};
const InternalMenu = /* @__PURE__ */ reactExports.forwardRef((props, ref) => {
  const override = reactExports.useContext(OverrideContext);
  const overrideObj = override || {};
  const {
    prefixCls: customizePrefixCls,
    className,
    style,
    theme = "light",
    expandIcon,
    _internalDisableMenuItemTitleTooltip,
    tooltip,
    inlineCollapsed,
    siderCollapsed,
    rootClassName,
    mode,
    selectable,
    onClick,
    overflowedIndicatorPopupClassName,
    classNames,
    styles,
    ...restProps
  } = props;
  const {
    menu
  } = reactExports.useContext(ConfigContext);
  const {
    getPrefixCls,
    getPopupContainer,
    direction,
    className: contextClassName,
    style: contextStyle,
    classNames: contextClassNames,
    styles: contextStyles
  } = useComponentConfig("menu");
  const rootPrefixCls = getPrefixCls();
  const passedProps = omit(restProps, ["collapsedWidth"]);
  overrideObj.validator?.({
    mode
  });
  const onItemClick = useEvent((...args) => {
    onClick?.(...args);
    overrideObj.onClick?.();
  });
  const mergedMode = overrideObj.mode || mode;
  const mergedSelectable = selectable ?? overrideObj.selectable;
  const mergedInlineCollapsed = inlineCollapsed ?? siderCollapsed;
  const mergedProps = {
    ...props,
    mode: mergedMode,
    inlineCollapsed: mergedInlineCollapsed,
    selectable: mergedSelectable,
    theme
  };
  const contextStyleRoot = useSemanticRootStyle(contextStyle);
  const styleRoot = useSemanticRootStyle(style);
  const [mergedClassNames, mergedStyles] = useMergeSemantic([contextClassNames, classNames], [contextStyles, contextStyleRoot, styles, styleRoot], {
    props: mergedProps
  }, {
    popup: {
      _default: "root"
    },
    subMenu: {
      _default: "item"
    }
  });
  const defaultMotions = {
    horizontal: {
      motionName: `${rootPrefixCls}-slide-up`
    },
    inline: initCollapseMotion(rootPrefixCls),
    other: {
      motionName: `${rootPrefixCls}-zoom-big`
    }
  };
  const prefixCls = getPrefixCls("menu", customizePrefixCls || overrideObj.prefixCls);
  const rootCls = useCSSVarCls(prefixCls);
  const [hashId, cssVarCls] = useStyle(prefixCls, rootCls, !override);
  const menuClassName = clsx(`${prefixCls}-${theme}`, contextClassName, className);
  const mergedExpandIcon = reactExports.useMemo(() => {
    if (isFunction(expandIcon) || isEmptyIcon(expandIcon)) {
      return expandIcon || null;
    }
    if (isFunction(overrideObj.expandIcon) || isEmptyIcon(overrideObj.expandIcon)) {
      return overrideObj.expandIcon || null;
    }
    if (isFunction(menu?.expandIcon) || isEmptyIcon(menu?.expandIcon)) {
      return menu?.expandIcon || null;
    }
    const mergedIcon = expandIcon ?? overrideObj?.expandIcon ?? menu?.expandIcon;
    return cloneElement(mergedIcon, {
      className: clsx(`${prefixCls}-submenu-expand-icon`, /* @__PURE__ */ reactExports.isValidElement(mergedIcon) ? mergedIcon.props?.className : void 0)
    });
  }, [expandIcon, overrideObj?.expandIcon, menu?.expandIcon, prefixCls]);
  const contextValue = reactExports.useMemo(() => ({
    prefixCls,
    inlineCollapsed: mergedInlineCollapsed || false,
    direction,
    firstLevel: true,
    theme,
    mode: mergedMode,
    disableMenuItemTitleTooltip: _internalDisableMenuItemTitleTooltip,
    tooltip,
    classNames: mergedClassNames,
    styles: mergedStyles
  }), [prefixCls, mergedInlineCollapsed, direction, _internalDisableMenuItemTitleTooltip, theme, mergedMode, mergedClassNames, mergedStyles, tooltip]);
  return /* @__PURE__ */ reactExports.createElement(OverrideContext.Provider, {
    value: null
  }, /* @__PURE__ */ reactExports.createElement(MenuContext.Provider, {
    value: contextValue
  }, /* @__PURE__ */ reactExports.createElement(ExportMenu, {
    getPopupContainer,
    overflowedIndicator: /* @__PURE__ */ reactExports.createElement(RefIcon, null),
    overflowedIndicatorPopupClassName: clsx(prefixCls, `${prefixCls}-${theme}`, overflowedIndicatorPopupClassName),
    classNames: {
      list: mergedClassNames.list,
      listTitle: mergedClassNames.itemTitle
    },
    styles: {
      list: mergedStyles.list,
      listTitle: mergedStyles.itemTitle
    },
    mode: mergedMode,
    selectable: mergedSelectable,
    onClick: onItemClick,
    ...passedProps,
    inlineCollapsed: mergedInlineCollapsed,
    style: mergedStyles.root,
    className: menuClassName,
    prefixCls,
    direction,
    defaultMotions,
    expandIcon: mergedExpandIcon,
    ref,
    rootClassName: clsx(rootClassName, hashId, overrideObj.rootClassName, cssVarCls, rootCls, mergedClassNames.root),
    _internalComponents: MENU_COMPONENTS
  })));
});
const Menu = /* @__PURE__ */ reactExports.forwardRef((props, ref) => {
  const menuRef = reactExports.useRef(null);
  const context = reactExports.useContext(SiderContext);
  reactExports.useImperativeHandle(ref, () => ({
    menu: menuRef.current,
    focus: (options) => {
      menuRef.current?.focus(options);
    }
  }));
  return /* @__PURE__ */ reactExports.createElement(InternalMenu, {
    ref: menuRef,
    ...props,
    ...context
  });
});
Menu.Item = MenuItem;
Menu.SubMenu = SubMenu;
Menu.Divider = MenuDivider;
Menu.ItemGroup = MenuItemGroup;
const useSettingsStore = create((set, get) => ({
  scopes: {},
  loading: false,
  load: async (scopes) => {
    set({ loading: true });
    try {
      const result = await settingsApi.getAll(scopes);
      set({ scopes: result.scopes });
    } finally {
      set({ loading: false });
    }
  },
  setScope: (scope, value) => {
    set((state) => ({
      scopes: { ...state.scopes, [scope]: value }
    }));
  },
  save: async (scopes) => {
    const target = scopes ?? Object.keys(get().scopes);
    await settingsApi.save(target);
    await get().load(target);
  }
}));
const LANGUAGE_OPTIONS = [
  { value: "zh-CN", label: "简体中文" },
  { value: "en-US", label: "English" }
];
const THEME_OPTIONS = [
  { value: "System", labelKey: "settings.appearance.themeOptions.system" },
  { value: "Light", labelKey: "settings.appearance.themeOptions.light" },
  { value: "Dark", labelKey: "settings.appearance.themeOptions.dark" }
];
const TEMPERATURE_UNIT_OPTIONS = [
  { value: "C", label: "°C" },
  { value: "F", label: "°F" }
];
const APP_SCALE_OPTIONS = [80, 90, 100, 110, 125];
function readString(app, key) {
  const value = app[key];
  return typeof value === "string" ? value : void 0;
}
function readNumber(app, key) {
  const value = app[key];
  return typeof value === "number" && Number.isFinite(value) ? value : void 0;
}
function readThemePreference(app) {
  const value = readString(app, "Theme");
  return value === "Light" || value === "Dark" ? value : "System";
}
function readTemperatureUnit(app) {
  return readString(app, "TemperatureUnit") === "F" ? "F" : "C";
}
function readAccentColor(app) {
  const value = app["AccentColor"];
  if (value != null && typeof value === "object" && typeof value.R === "number" && typeof value.G === "number" && typeof value.B === "number") {
    return value;
  }
  return void 0;
}
function accentColorToHex(color) {
  const toHex = (value) => value.toString(16).padStart(2, "0");
  return `#${toHex(color.R)}${toHex(color.G)}${toHex(color.B)}`;
}
function SettingRow({
  label,
  control
}) {
  return /* @__PURE__ */ jsxRuntimeExports.jsxs(
    "div",
    {
      style: {
        display: "flex",
        alignItems: "center",
        justifyContent: "space-between",
        padding: "14px 0",
        borderBottom: "1px solid rgba(128, 128, 128, 0.15)"
      },
      children: [
        /* @__PURE__ */ jsxRuntimeExports.jsx(Typography.Text, { children: label }),
        control
      ]
    }
  );
}
function AppearanceSection() {
  const { t, i18n } = useTranslation();
  const { setThemeMode, setAccent } = useTheme();
  const scopes = useSettingsStore((s) => s.scopes);
  const load = useSettingsStore((s) => s.load);
  const setScope = useSettingsStore((s) => s.setScope);
  const rawApp = scopes.application;
  const app = typeof rawApp === "object" && rawApp !== null ? rawApp : {};
  reactExports.useEffect(() => {
    void load();
  }, [load]);
  const accentColor = readAccentColor(app);
  const accentHex = accentColor ? accentColorToHex(accentColor) : void 0;
  const [accentValue, setAccentValue] = reactExports.useState(accentHex);
  reactExports.useEffect(() => {
    setAccentValue(accentHex);
  }, [accentHex]);
  const storedScale = readNumber(app, "AppScale");
  const appScale = storedScale != null && APP_SCALE_OPTIONS.includes(storedScale) ? storedScale : 100;
  const handleLanguageChange = (value) => {
    localStorage.setItem("udt.lang", value);
    void changeLanguage(value);
  };
  const handleTemperatureUnitChange = (value) => {
    const next = { ...app, TemperatureUnit: value };
    setScope("application", next);
    settingsApi.set("application", next).then(() => settingsApi.save(["application"])).catch(() => staticMethods.error(t("settings.saveFailed")));
  };
  const handleThemeChange = (value) => {
    localStorage.removeItem("udt.theme");
    if (value === "System") {
      setThemeMode(window.matchMedia("(prefers-color-scheme: dark)").matches ? "dark" : "light");
    } else {
      setThemeMode(value === "Dark" ? "dark" : "light");
    }
    const next = { ...app, Theme: value };
    setScope("application", next);
    settingsApi.set("application", next).then(() => settingsApi.save(["application"])).catch(() => staticMethods.error(t("settings.saveFailed")));
  };
  const handleAccentChange = (value, css) => {
    setAccentValue(css);
    setAccent(css);
    const rgb = value.toRgb();
    const next = { ...app, AccentColor: { R: rgb.r, G: rgb.g, B: rgb.b } };
    setScope("application", next);
    settingsApi.set("application", next).catch(() => staticMethods.error(t("settings.saveFailed")));
  };
  const handleAppScaleChange = (value) => {
    const next = { ...app, AppScale: value };
    setScope("application", next);
    settingsApi.set("application", next).then(() => settingsApi.save(["application"])).catch(() => staticMethods.error(t("settings.saveFailed")));
  };
  return /* @__PURE__ */ jsxRuntimeExports.jsxs("div", { children: [
    /* @__PURE__ */ jsxRuntimeExports.jsx(Typography.Title, { level: 4, children: t("settings.nav.appearance") }),
    /* @__PURE__ */ jsxRuntimeExports.jsx(
      SettingRow,
      {
        label: t("settings.appearance.language"),
        control: /* @__PURE__ */ jsxRuntimeExports.jsx(
          Select,
          {
            value: i18n.language.startsWith("zh") ? "zh-CN" : "en-US",
            options: LANGUAGE_OPTIONS,
            onChange: handleLanguageChange,
            style: { width: 160 }
          }
        )
      }
    ),
    /* @__PURE__ */ jsxRuntimeExports.jsx(
      SettingRow,
      {
        label: t("settings.appearance.temperatureUnit"),
        control: /* @__PURE__ */ jsxRuntimeExports.jsx(
          Select,
          {
            value: readTemperatureUnit(app),
            options: TEMPERATURE_UNIT_OPTIONS,
            onChange: handleTemperatureUnitChange,
            style: { width: 160 }
          }
        )
      }
    ),
    /* @__PURE__ */ jsxRuntimeExports.jsx(
      SettingRow,
      {
        label: t("settings.appearance.theme"),
        control: /* @__PURE__ */ jsxRuntimeExports.jsx(
          Radio.Group,
          {
            value: readThemePreference(app),
            options: THEME_OPTIONS.map((option) => ({
              value: option.value,
              label: t(option.labelKey)
            })),
            onChange: (e) => handleThemeChange(e.target.value)
          }
        )
      }
    ),
    /* @__PURE__ */ jsxRuntimeExports.jsx(
      SettingRow,
      {
        label: t("settings.appearance.accentColor"),
        control: /* @__PURE__ */ jsxRuntimeExports.jsx(ColorPicker, { value: accentValue, onChange: handleAccentChange })
      }
    ),
    /* @__PURE__ */ jsxRuntimeExports.jsx(
      SettingRow,
      {
        label: t("settings.appearance.appScale"),
        control: /* @__PURE__ */ jsxRuntimeExports.jsx(
          Select,
          {
            value: appScale,
            options: APP_SCALE_OPTIONS.map((value) => ({ value, label: `${value}%` })),
            onChange: handleAppScaleChange,
            style: { width: 160 }
          }
        )
      }
    )
  ] });
}
const TOGGLE_ITEMS = [
  {
    field: "MinimizeToTray",
    labelKey: "settings.application.minimizeToTray",
    descKey: "settings.application.minimizeToTrayDesc"
  },
  {
    field: "MinimizeOnClose",
    labelKey: "settings.application.minimizeOnClose",
    descKey: "settings.application.minimizeOnCloseDesc"
  },
  {
    field: "DisableUnsupportedHardwareWarning",
    labelKey: "settings.application.disableUnsupportedWarning",
    descKey: "settings.application.disableUnsupportedWarningDesc"
  },
  {
    field: "EnableHardwareSensors",
    labelKey: "settings.application.enableHardwareSensors",
    descKey: "settings.application.enableHardwareSensorsDesc"
  },
  {
    field: "DontShowNotifications",
    labelKey: "settings.application.dontShowNotifications",
    descKey: "settings.application.dontShowNotificationsDesc"
  },
  {
    field: "ExtensionsEnabled",
    labelKey: "settings.application.extensionsEnabled",
    descKey: "settings.application.extensionsEnabledDesc"
  }
];
function readBoolean(app, key) {
  return app[key] === true;
}
function ApplicationSection() {
  const { t } = useTranslation();
  const scopes = useSettingsStore((s) => s.scopes);
  const load = useSettingsStore((s) => s.load);
  const setScope = useSettingsStore((s) => s.setScope);
  const rawApp = scopes.application;
  const app = typeof rawApp === "object" && rawApp !== null ? rawApp : {};
  reactExports.useEffect(() => {
    void load();
  }, [load]);
  const handleToggle = (field, checked) => {
    const next = { ...app, [field]: checked };
    setScope("application", next);
    settingsApi.set("application", next).then(() => settingsApi.save(["application"])).catch(() => staticMethods.error(t("settings.saveFailed")));
  };
  return /* @__PURE__ */ jsxRuntimeExports.jsxs("div", { children: [
    /* @__PURE__ */ jsxRuntimeExports.jsx(Typography.Title, { level: 4, children: t("settings.nav.application") }),
    TOGGLE_ITEMS.map((item) => {
      const checked = readBoolean(app, item.field);
      return /* @__PURE__ */ jsxRuntimeExports.jsxs(
        "div",
        {
          style: {
            display: "flex",
            alignItems: "center",
            justifyContent: "space-between",
            padding: "14px 0",
            borderBottom: "1px solid rgba(128, 128, 128, 0.15)"
          },
          children: [
            /* @__PURE__ */ jsxRuntimeExports.jsxs("div", { children: [
              /* @__PURE__ */ jsxRuntimeExports.jsx(Typography.Text, { children: t(item.labelKey) }),
              /* @__PURE__ */ jsxRuntimeExports.jsx("div", { children: /* @__PURE__ */ jsxRuntimeExports.jsx(Typography.Text, { type: "secondary", style: { fontSize: 12 }, children: t(item.descKey) }) })
            ] }),
            /* @__PURE__ */ jsxRuntimeExports.jsxs("div", { style: { display: "flex", alignItems: "center", gap: 8 }, children: [
              /* @__PURE__ */ jsxRuntimeExports.jsx(Typography.Text, { type: "secondary", children: checked ? t("settings.application.valueOn") : t("settings.application.valueOff") }),
              /* @__PURE__ */ jsxRuntimeExports.jsx(Switch, { checked, onChange: (value) => handleToggle(item.field, value) })
            ] })
          ]
        },
        item.field
      );
    })
  ] });
}
const SECTION_KEYS = [
  { key: "appearance", labelKey: "settings.nav.appearance" },
  { key: "application", labelKey: "settings.nav.application" },
  { key: "power", labelKey: "settings.nav.power" },
  { key: "display", labelKey: "settings.nav.display" },
  { key: "smartKeys", labelKey: "settings.nav.smartKeys" },
  { key: "update", labelKey: "settings.nav.update" },
  { key: "integrations", labelKey: "settings.nav.integrations" }
];
function PlaceholderSection() {
  const { t } = useTranslation();
  return /* @__PURE__ */ jsxRuntimeExports.jsx(Typography.Text, { type: "secondary", children: t("pages.placeholder") });
}
function renderSection(key) {
  switch (key) {
    case "appearance":
      return /* @__PURE__ */ jsxRuntimeExports.jsx(AppearanceSection, {});
    case "application":
      return /* @__PURE__ */ jsxRuntimeExports.jsx(ApplicationSection, {});
    // TODO: 固定文件就绪后改为渲染真实子页组件
    case "power":
    case "display":
    case "smartKeys":
    case "update":
    case "integrations":
      return /* @__PURE__ */ jsxRuntimeExports.jsx(PlaceholderSection, {});
  }
}
function SettingsPage() {
  const { t } = useTranslation();
  const [active, setActive] = reactExports.useState("appearance");
  return /* @__PURE__ */ jsxRuntimeExports.jsx(Card, { title: t("settings.title"), children: /* @__PURE__ */ jsxRuntimeExports.jsxs("div", { style: { display: "flex" }, children: [
    /* @__PURE__ */ jsxRuntimeExports.jsx(
      Menu,
      {
        mode: "inline",
        selectedKeys: [active],
        items: SECTION_KEYS.map((item) => ({ key: item.key, label: t(item.labelKey) })),
        onClick: ({ key }) => setActive(key),
        style: {
          width: 200,
          borderInlineEnd: "1px solid rgba(128, 128, 128, 0.15)",
          background: "transparent"
        }
      }
    ),
    /* @__PURE__ */ jsxRuntimeExports.jsx("div", { style: { flex: 1, paddingLeft: 24, minWidth: 0 }, children: renderSection(active) })
  ] }) });
}
export {
  SettingsPage as default
};
