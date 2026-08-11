import { $ as React, r as reactExports, ah as isNumber, cm as matchScreen, g as toArray, N as useMergeSemantic, f as clsx, as as isReactRenderable, v as genStyleHooks, w as merge, q as textEllipsis, n as unit, A as resetComponent, J as useComponentConfig, bF as useBreakpoint, aL as useSize, L as useSemanticRootStyle, u as useTranslation, i as invoke, W as settingsApi, j as jsxRuntimeExports, F as Flex, T as Typography, C as Card } from "./index-3RTipSd5.js";
const DEFAULT_COLUMN_MAP = {
  xxxl: 4,
  xxl: 3,
  xl: 3,
  lg: 3,
  md: 3,
  sm: 2,
  xs: 1
};
const DescriptionsContext = /* @__PURE__ */ React.createContext(null);
const transChildren2Items = (childNodes) => toArray(childNodes).map((node) => ({
  ...node?.props,
  key: node.key
}));
function useItems(screens, items, children) {
  const mergedItems = reactExports.useMemo(() => (
    // Take `items` first or convert `children` into items
    items || transChildren2Items(children)
  ), [items, children]);
  const responsiveItems = reactExports.useMemo(() => mergedItems.map(({
    span,
    ...restItem
  }) => {
    if (span === "filled") {
      return {
        ...restItem,
        filled: true
      };
    }
    return {
      ...restItem,
      span: isNumber(span) ? span : matchScreen(screens, span)
    };
  }), [mergedItems, screens]);
  return responsiveItems;
}
function getCalcRows(rowItems, mergedColumn) {
  let rows = [];
  let tmpRow = [];
  let exceed = false;
  let count = 0;
  rowItems.filter((n) => n).forEach((rowItem) => {
    const {
      filled,
      ...restItem
    } = rowItem;
    if (filled) {
      tmpRow.push(restItem);
      rows.push(tmpRow);
      tmpRow = [];
      count = 0;
      return;
    }
    const restSpan = mergedColumn - count;
    count += rowItem.span || 1;
    if (count >= mergedColumn) {
      if (count > mergedColumn) {
        exceed = true;
        tmpRow.push({
          ...restItem,
          span: restSpan
        });
      } else {
        tmpRow.push(restItem);
      }
      rows.push(tmpRow);
      tmpRow = [];
      count = 0;
    } else {
      tmpRow.push(restItem);
    }
  });
  if (tmpRow.length > 0) {
    rows.push(tmpRow);
  }
  rows = rows.map((rows2) => {
    const count2 = rows2.reduce((acc, item) => acc + (item.span || 1), 0);
    if (count2 < mergedColumn) {
      const last = rows2[rows2.length - 1];
      last.span = mergedColumn - (count2 - (last.span || 1));
      return rows2;
    }
    return rows2;
  });
  return [rows, exceed];
}
const useRow = (mergedColumn, items) => {
  const [rows, exceed] = reactExports.useMemo(() => getCalcRows(items, mergedColumn), [items, mergedColumn]);
  return rows;
};
const DescriptionsItem = (props) => {
  return props.children;
};
const Cell = (props) => {
  const {
    itemPrefixCls,
    component,
    span,
    className,
    style,
    labelStyle,
    contentStyle,
    bordered,
    label,
    content,
    colon,
    type,
    styles,
    classNames
  } = props;
  const Component = component;
  const {
    classNames: contextClassNames,
    styles: contextStyles
  } = React.useContext(DescriptionsContext);
  const [mergedClassNames, mergedStyles] = useMergeSemantic([contextClassNames, classNames], [contextStyles, styles], {
    props
  });
  const mergedLabelStyle = {
    ...labelStyle,
    ...mergedStyles.label
  };
  const mergedContentStyle = {
    ...contentStyle,
    ...mergedStyles.content
  };
  if (bordered) {
    let typeStyle;
    if (type === "label") {
      typeStyle = mergedLabelStyle;
    }
    if (type === "content") {
      typeStyle = mergedContentStyle;
    }
    const mergedCellStyle = typeStyle ? {
      ...style,
      ...typeStyle
    } : style;
    return /* @__PURE__ */ React.createElement(Component, {
      colSpan: span,
      style: mergedCellStyle,
      className: clsx(className, {
        [`${itemPrefixCls}-item-${type}`]: type === "label" || type === "content",
        [mergedClassNames.label]: mergedClassNames.label && type === "label",
        [mergedClassNames.content]: mergedClassNames.content && type === "content"
      })
    }, isReactRenderable(label) && /* @__PURE__ */ React.createElement("span", null, label), isReactRenderable(content) && /* @__PURE__ */ React.createElement("span", null, content));
  }
  return /* @__PURE__ */ React.createElement(Component, {
    className: clsx(`${itemPrefixCls}-item`, className),
    style,
    colSpan: span
  }, /* @__PURE__ */ React.createElement("div", {
    className: `${itemPrefixCls}-item-container`
  }, isReactRenderable(label) && /* @__PURE__ */ React.createElement("span", {
    style: mergedLabelStyle,
    className: clsx(`${itemPrefixCls}-item-label`, mergedClassNames.label, {
      [`${itemPrefixCls}-item-no-colon`]: !colon
    })
  }, label), isReactRenderable(content) && /* @__PURE__ */ React.createElement("span", {
    style: mergedContentStyle,
    className: clsx(`${itemPrefixCls}-item-content`, mergedClassNames.content)
  }, content)));
};
function renderCells(items, {
  colon,
  prefixCls,
  bordered
}, {
  component,
  type,
  showLabel,
  showContent,
  labelStyle: rootLabelStyle,
  contentStyle: rootContentStyle,
  styles: rootStyles
}) {
  return items.map(({
    label,
    children,
    prefixCls: itemPrefixCls = prefixCls,
    className,
    style,
    labelStyle,
    contentStyle,
    span = 1,
    key,
    styles,
    classNames
  }, index) => {
    if (typeof component === "string") {
      return /* @__PURE__ */ reactExports.createElement(Cell, {
        key: `${type}-${key || index}`,
        className,
        style,
        classNames,
        styles: {
          label: {
            ...rootLabelStyle,
            ...rootStyles?.label,
            ...labelStyle,
            ...styles?.label
          },
          content: {
            ...rootContentStyle,
            ...rootStyles?.content,
            ...contentStyle,
            ...styles?.content
          }
        },
        span,
        colon,
        component,
        itemPrefixCls,
        bordered,
        label: showLabel ? label : null,
        content: showContent ? children : null,
        type
      });
    }
    const mergedStyles = {
      label: {
        ...rootLabelStyle,
        ...rootStyles?.label,
        ...labelStyle,
        ...styles?.label
      },
      content: {
        ...rootContentStyle,
        ...rootStyles?.content,
        ...contentStyle,
        ...styles?.content
      }
    };
    return [/* @__PURE__ */ reactExports.createElement(Cell, {
      key: `label-${key || index}`,
      className,
      style,
      classNames,
      styles: mergedStyles,
      span: 1,
      colon,
      component: component[0],
      itemPrefixCls,
      bordered,
      label,
      type: "label"
    }), /* @__PURE__ */ reactExports.createElement(Cell, {
      key: `content-${key || index}`,
      className,
      style,
      classNames,
      styles: mergedStyles,
      span: span * 2 - 1,
      component: component[1],
      itemPrefixCls,
      bordered,
      content: children,
      type: "content"
    })];
  });
}
const Row = (props) => {
  const descContext = reactExports.useContext(DescriptionsContext);
  const {
    prefixCls,
    vertical,
    row,
    index,
    bordered
  } = props;
  if (vertical) {
    return /* @__PURE__ */ reactExports.createElement(reactExports.Fragment, null, /* @__PURE__ */ reactExports.createElement("tr", {
      key: `label-${index}`,
      className: `${prefixCls}-row`
    }, renderCells(row, props, {
      component: "th",
      type: "label",
      showLabel: true,
      ...descContext
    })), /* @__PURE__ */ reactExports.createElement("tr", {
      key: `content-${index}`,
      className: `${prefixCls}-row`
    }, renderCells(row, props, {
      component: "td",
      type: "content",
      showContent: true,
      ...descContext
    })));
  }
  return /* @__PURE__ */ reactExports.createElement("tr", {
    key: index,
    className: `${prefixCls}-row`
  }, renderCells(row, props, {
    component: bordered ? ["th", "td"] : "td",
    type: "item",
    showLabel: true,
    showContent: true,
    ...descContext
  }));
};
const genBorderedStyle = (token) => {
  const {
    componentCls,
    labelBg
  } = token;
  return {
    [`&${componentCls}-bordered`]: {
      [`> ${componentCls}-view`]: {
        border: `${unit(token.lineWidth)} ${token.lineType} ${token.colorSplit}`,
        "> table": {
          tableLayout: "auto"
        },
        [`${componentCls}-row`]: {
          borderBottom: `${unit(token.lineWidth)} ${token.lineType} ${token.colorSplit}`,
          "&:first-child": {
            "> th:first-child, > td:first-child": {
              borderStartStartRadius: token.borderRadiusLG
            }
          },
          "&:last-child": {
            borderBottom: "none",
            "> th:first-child, > td:first-child": {
              borderEndStartRadius: token.borderRadiusLG
            }
          },
          [`> ${componentCls}-item-label, > ${componentCls}-item-content`]: {
            padding: `${unit(token.padding)} ${unit(token.paddingLG)}`,
            borderInlineEnd: `${unit(token.lineWidth)} ${token.lineType} ${token.colorSplit}`,
            "&:last-child": {
              borderInlineEnd: "none"
            }
          },
          [`> ${componentCls}-item-label`]: {
            color: token.colorTextSecondary,
            backgroundColor: labelBg,
            "&::after": {
              display: "none"
            }
          }
        }
      },
      [`&${componentCls}-medium`]: {
        [`${componentCls}-row`]: {
          [`> ${componentCls}-item-label, > ${componentCls}-item-content`]: {
            padding: `${unit(token.paddingSM)} ${unit(token.paddingLG)}`
          }
        }
      },
      [`&${componentCls}-small`]: {
        [`${componentCls}-row`]: {
          [`> ${componentCls}-item-label, > ${componentCls}-item-content`]: {
            padding: `${unit(token.paddingXS)} ${unit(token.padding)}`
          }
        }
      }
    }
  };
};
const genDescriptionStyles = (token) => {
  const {
    componentCls,
    extraColor,
    itemPaddingBottom,
    itemPaddingEnd,
    colonMarginRight,
    colonMarginLeft,
    titleMarginBottom
  } = token;
  return {
    [componentCls]: {
      ...resetComponent(token),
      ...genBorderedStyle(token),
      "&-rtl": {
        direction: "rtl"
      },
      [`${componentCls}-header`]: {
        display: "flex",
        alignItems: "center",
        marginBottom: titleMarginBottom
      },
      [`${componentCls}-title`]: {
        ...textEllipsis,
        flex: "auto",
        color: token.titleColor,
        fontWeight: token.fontWeightStrong,
        fontSize: token.fontSizeLG,
        lineHeight: token.lineHeightLG
      },
      [`${componentCls}-extra`]: {
        marginInlineStart: "auto",
        color: extraColor,
        fontSize: token.fontSize
      },
      [`${componentCls}-view`]: {
        // #54268 used `width: 0` with `min-width: 100%` to avoid oversized
        // intrinsic widths in max-content ancestors. Keep the wrapper at
        // `width: 100%` so it remains measurable in shrink-to-fit containers
        // like Popover (#58574), while the inner table preserves the minimum.
        width: "100%",
        borderRadius: token.borderRadiusLG,
        table: {
          minWidth: "100%",
          tableLayout: "fixed",
          borderCollapse: "collapse"
        }
      },
      [`${componentCls}-row`]: {
        "> th, > td": {
          paddingBottom: itemPaddingBottom,
          paddingInlineEnd: itemPaddingEnd
        },
        "> th:last-child, > td:last-child": {
          paddingInlineEnd: 0
        },
        "&:last-child": {
          borderBottom: "none",
          "> th, > td": {
            paddingBottom: 0
          }
        }
      },
      [`${componentCls}-item-label`]: {
        color: token.labelColor,
        fontWeight: "normal",
        fontSize: token.fontSize,
        lineHeight: token.lineHeight,
        textAlign: "start",
        "&::after": {
          content: '":"',
          position: "relative",
          top: -0.5,
          // magic for position
          marginInline: `${unit(colonMarginLeft)} ${unit(colonMarginRight)}`
        },
        [`&${componentCls}-item-no-colon::after`]: {
          content: '""'
        }
      },
      [`${componentCls}-item-no-label`]: {
        "&::after": {
          margin: 0,
          content: '""'
        }
      },
      [`${componentCls}-item-content`]: {
        display: "table-cell",
        flex: 1,
        color: token.contentColor,
        fontSize: token.fontSize,
        lineHeight: token.lineHeight,
        wordBreak: "break-word",
        overflowWrap: "break-word"
      },
      [`${componentCls}-item`]: {
        paddingBottom: 0,
        verticalAlign: "top",
        "&-container": {
          display: "flex",
          [`${componentCls}-item-label`]: {
            display: "inline-flex",
            alignItems: "baseline"
          },
          [`${componentCls}-item-content`]: {
            display: "inline-flex",
            alignItems: "baseline",
            minWidth: "1em"
          }
        }
      },
      "&-medium": {
        [`${componentCls}-row`]: {
          "> th, > td": {
            paddingBottom: token.paddingSM
          }
        }
      },
      "&-small": {
        [`${componentCls}-row`]: {
          "> th, > td": {
            paddingBottom: token.paddingXS
          }
        }
      }
    }
  };
};
const prepareComponentToken = (token) => ({
  labelBg: token.colorFillAlter,
  labelColor: token.colorTextTertiary,
  titleColor: token.colorText,
  titleMarginBottom: token.fontSizeSM * token.lineHeightSM,
  itemPaddingBottom: token.padding,
  itemPaddingEnd: token.padding,
  colonMarginRight: token.marginXS,
  colonMarginLeft: token.marginXXS / 2,
  contentColor: token.colorText,
  extraColor: token.colorText
});
const useStyle = genStyleHooks("Descriptions", (token) => {
  const descriptionToken = merge(token, {});
  return genDescriptionStyles(descriptionToken);
}, prepareComponentToken);
const Descriptions = /* @__PURE__ */ reactExports.forwardRef((props, ref) => {
  const {
    prefixCls: customizePrefixCls,
    title,
    extra,
    column,
    colon = true,
    bordered,
    layout,
    children,
    className,
    rootClassName,
    style,
    size: customizeSize,
    labelStyle,
    contentStyle,
    styles,
    items,
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
  } = useComponentConfig("descriptions");
  const prefixCls = getPrefixCls("descriptions", customizePrefixCls);
  const screens = useBreakpoint();
  const mergedColumn = reactExports.useMemo(() => {
    if (isNumber(column)) {
      return column;
    }
    return matchScreen(screens, column) ?? matchScreen(screens, DEFAULT_COLUMN_MAP) ?? 3;
  }, [screens, column]);
  const mergedItems = useItems(screens, items, children);
  const mergedSize = useSize(customizeSize);
  const rows = useRow(mergedColumn, mergedItems);
  const [hashId, cssVarCls] = useStyle(prefixCls);
  const mergedProps = {
    ...props,
    column: mergedColumn,
    items: mergedItems,
    size: mergedSize
  };
  const contextStyleRoot = useSemanticRootStyle(contextStyle);
  const styleRoot = useSemanticRootStyle(style);
  const [mergedClassNames, mergedStyles] = useMergeSemantic([contextClassNames, classNames], [contextStyles, contextStyleRoot, styles, styleRoot], {
    props: mergedProps
  });
  const memoizedValue = reactExports.useMemo(() => ({
    labelStyle,
    contentStyle,
    styles: {
      label: mergedStyles.label,
      content: mergedStyles.content
    },
    classNames: {
      label: mergedClassNames.label,
      content: mergedClassNames.content
    }
  }), [labelStyle, contentStyle, mergedStyles.label, mergedStyles.content, mergedClassNames.label, mergedClassNames.content]);
  const nativeElementRef = reactExports.useRef(null);
  reactExports.useImperativeHandle(ref, () => ({
    nativeElement: nativeElementRef.current
  }));
  return /* @__PURE__ */ reactExports.createElement(DescriptionsContext.Provider, {
    value: memoizedValue
  }, /* @__PURE__ */ reactExports.createElement("div", {
    ref: nativeElementRef,
    className: clsx(prefixCls, contextClassName, mergedClassNames.root, {
      [`${prefixCls}-medium`]: mergedSize === "medium" || mergedSize === "middle",
      [`${prefixCls}-small`]: mergedSize === "small",
      [`${prefixCls}-bordered`]: !!bordered,
      [`${prefixCls}-rtl`]: direction === "rtl"
    }, className, rootClassName, hashId, cssVarCls),
    style: mergedStyles.root,
    ...restProps
  }, (title || extra) && /* @__PURE__ */ reactExports.createElement("div", {
    className: clsx(`${prefixCls}-header`, mergedClassNames.header),
    style: mergedStyles.header
  }, title && /* @__PURE__ */ reactExports.createElement("div", {
    className: clsx(`${prefixCls}-title`, mergedClassNames.title),
    style: mergedStyles.title
  }, title), extra && /* @__PURE__ */ reactExports.createElement("div", {
    className: clsx(`${prefixCls}-extra`, mergedClassNames.extra),
    style: mergedStyles.extra
  }, extra)), /* @__PURE__ */ reactExports.createElement("div", {
    className: `${prefixCls}-view`
  }, /* @__PURE__ */ reactExports.createElement("table", null, /* @__PURE__ */ reactExports.createElement("tbody", null, rows.map((row, index) => /* @__PURE__ */ reactExports.createElement(Row, {
    key: index,
    index,
    colon,
    prefixCls,
    vertical: layout === "vertical",
    bordered,
    row
  })))))));
});
Descriptions.Item = DescriptionsItem;
const THIRD_PARTY_LIBS = [
  "Autofac",
  "Serilog",
  "LibreHardwareMonitorLib",
  "ManagedNativeWifi",
  "NAudio.Wasapi",
  "WindowsDisplayAPI",
  "System.Management",
  "Octokit",
  "Markdig",
  "Humanizer"
];
function AboutPage() {
  const { t } = useTranslation();
  const [appStatus, setAppStatus] = reactExports.useState(null);
  const [systemInfo, setSystemInfo] = reactExports.useState(null);
  const [dataFolder, setDataFolder] = reactExports.useState("");
  reactExports.useEffect(() => {
    void invoke("app.getStatus").then(setAppStatus).catch(() => void 0);
    void invoke("system.info").then(setSystemInfo).catch(() => void 0);
    void settingsApi.get("application").catch(() => void 0);
  }, []);
  reactExports.useEffect(() => {
    const maybeFolder = appStatus?.logPath;
    if (maybeFolder) {
      setDataFolder(maybeFolder.replace(/[\\/][^\\/]*$/, ""));
    }
  }, [appStatus]);
  return /* @__PURE__ */ jsxRuntimeExports.jsxs(Flex, { vertical: true, gap: 16, style: { maxWidth: 720 }, children: [
    /* @__PURE__ */ jsxRuntimeExports.jsx(Typography.Title, { level: 3, children: t("about.title") }),
    /* @__PURE__ */ jsxRuntimeExports.jsx(Card, { children: /* @__PURE__ */ jsxRuntimeExports.jsxs(Descriptions, { column: 1, size: "small", children: [
      /* @__PURE__ */ jsxRuntimeExports.jsx(Descriptions.Item, { label: t("about.appName"), children: "Universal Device Toolkit" }),
      /* @__PURE__ */ jsxRuntimeExports.jsx(Descriptions.Item, { label: t("about.version"), children: appStatus?.version ?? "..." }),
      /* @__PURE__ */ jsxRuntimeExports.jsx(Descriptions.Item, { label: t("about.pid"), children: appStatus?.pid ?? "..." }),
      /* @__PURE__ */ jsxRuntimeExports.jsx(Descriptions.Item, { label: t("about.machine"), children: systemInfo ? `${systemInfo.vendor ?? ""} ${systemInfo.model ?? ""} (${systemInfo.machineType ?? ""})` : "..." }),
      /* @__PURE__ */ jsxRuntimeExports.jsx(Descriptions.Item, { label: t("about.bios"), children: systemInfo?.biosVersion ?? "..." }),
      /* @__PURE__ */ jsxRuntimeExports.jsx(Descriptions.Item, { label: t("about.compatible"), children: systemInfo ? systemInfo.isCompatible ? t("about.yes") : t("about.no") : "..." }),
      /* @__PURE__ */ jsxRuntimeExports.jsx(Descriptions.Item, { label: t("about.dataFolder"), children: dataFolder || "..." })
    ] }) }),
    /* @__PURE__ */ jsxRuntimeExports.jsx(Card, { title: t("about.thirdParty"), children: /* @__PURE__ */ jsxRuntimeExports.jsx(Flex, { gap: 8, wrap: true, children: THIRD_PARTY_LIBS.map((lib) => /* @__PURE__ */ jsxRuntimeExports.jsx(Typography.Text, { code: true, children: lib }, lib)) }) }),
    /* @__PURE__ */ jsxRuntimeExports.jsxs(Typography.Text, { type: "secondary", children: [
      t("about.copyright"),
      " © SSC-STUDIO"
    ] })
  ] });
}
export {
  AboutPage as default
};
