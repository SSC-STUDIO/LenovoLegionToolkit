import { $ as React, r as reactExports, as as isReactRenderable, f as clsx, v as genStyleHooks, w as merge, J as useComponentConfig, g as toArray, aM as useOrientation, L as useSemanticRootStyle, N as useMergeSemantic, bZ as isPresetSize, b6 as Compact, b_ as isValidGapNumber } from "./index-3RTipSd5.js";
import { S as SpaceAddon } from "./Addon-CECo-qGW.js";
const SpaceContext = /* @__PURE__ */ React.createContext({
  latestIndex: 0
});
const SpaceContextProvider = SpaceContext.Provider;
const Item = (props) => {
  const {
    className,
    prefix,
    index,
    children,
    separator,
    style,
    classNames,
    styles
  } = props;
  const {
    latestIndex
  } = reactExports.useContext(SpaceContext);
  if (!isReactRenderable(children)) {
    return null;
  }
  return /* @__PURE__ */ reactExports.createElement(reactExports.Fragment, null, /* @__PURE__ */ reactExports.createElement("div", {
    className,
    style
  }, children), index < latestIndex && separator && /* @__PURE__ */ reactExports.createElement("span", {
    className: clsx(`${prefix}-item-separator`, classNames?.separator),
    style: styles?.separator
  }, separator));
};
const genSpaceStyle = (token) => {
  const {
    componentCls,
    antCls
  } = token;
  return {
    [componentCls]: {
      display: "inline-flex",
      "&-rtl": {
        direction: "rtl"
      },
      "&-vertical": {
        flexDirection: "column"
      },
      "&-align": {
        flexDirection: "column",
        "&-center": {
          alignItems: "center"
        },
        "&-start": {
          alignItems: "flex-start"
        },
        "&-end": {
          alignItems: "flex-end"
        },
        "&-baseline": {
          alignItems: "baseline"
        }
      },
      [`${componentCls}-item:empty`]: {
        display: "none"
      },
      // https://github.com/ant-design/ant-design/issues/47875
      [`${componentCls}-item > ${antCls}-badge-not-a-wrapper:only-child`]: {
        display: "block"
      }
    }
  };
};
const genSpaceGapStyle = (token) => {
  const {
    componentCls
  } = token;
  return {
    [componentCls]: {
      "&-gap-row-small": {
        rowGap: token.spaceGapSmallSize
      },
      "&-gap-row-medium, &-gap-row-middle": {
        rowGap: token.spaceGapMiddleSize
      },
      "&-gap-row-large": {
        rowGap: token.spaceGapLargeSize
      },
      "&-gap-col-small": {
        columnGap: token.spaceGapSmallSize
      },
      "&-gap-col-medium, &-gap-col-middle": {
        columnGap: token.spaceGapMiddleSize
      },
      "&-gap-col-large": {
        columnGap: token.spaceGapLargeSize
      }
    }
  };
};
const useStyle = genStyleHooks("Space", (token) => {
  const spaceToken = merge(token, {
    spaceGapSmallSize: token.paddingXS,
    spaceGapMiddleSize: token.padding,
    spaceGapLargeSize: token.paddingLG
  });
  return [genSpaceStyle(spaceToken), genSpaceGapStyle(spaceToken)];
}, () => ({}), {
  // Space component don't apply extra font style
  // https://github.com/ant-design/ant-design/issues/40315
  resetStyle: false
});
const InternalSpace = /* @__PURE__ */ reactExports.forwardRef((props, ref) => {
  const {
    getPrefixCls,
    direction: directionConfig,
    size: contextSize,
    className: contextClassName,
    style: contextStyle,
    classNames: contextClassNames,
    styles: contextStyles
  } = useComponentConfig("space");
  const {
    size = contextSize ?? "small",
    align,
    className,
    rootClassName,
    children,
    direction,
    orientation,
    prefixCls: customizePrefixCls,
    split,
    separator,
    style,
    vertical,
    wrap = false,
    classNames,
    styles,
    ...restProps
  } = props;
  const [horizontalSize, verticalSize] = Array.isArray(size) ? size : [size, size];
  const isPresetVerticalSize = isPresetSize(verticalSize);
  const isPresetHorizontalSize = isPresetSize(horizontalSize);
  const isValidVerticalSize = isValidGapNumber(verticalSize);
  const isValidHorizontalSize = isValidGapNumber(horizontalSize);
  const childNodes = toArray(children, {
    keepEmpty: true
  });
  const [mergedOrientation, mergedVertical] = useOrientation(orientation, vertical, direction);
  const mergedAlign = align === void 0 && !mergedVertical ? "center" : align;
  const mergedSeparator = separator ?? split;
  const prefixCls = getPrefixCls("space", customizePrefixCls);
  const [hashId, cssVarCls] = useStyle(prefixCls);
  const mergedProps = {
    ...props,
    size,
    orientation: mergedOrientation,
    align: mergedAlign
  };
  const contextStyleRoot = useSemanticRootStyle(contextStyle);
  const styleRoot = useSemanticRootStyle(style);
  const [mergedClassNames, mergedStyles] = useMergeSemantic([contextClassNames, classNames], [contextStyles, contextStyleRoot, styles, styleRoot], {
    props: mergedProps
  });
  const rootClassNames = clsx(prefixCls, contextClassName, hashId, `${prefixCls}-${mergedOrientation}`, {
    [`${prefixCls}-rtl`]: directionConfig === "rtl",
    [`${prefixCls}-align-${mergedAlign}`]: mergedAlign,
    [`${prefixCls}-gap-row-${verticalSize}`]: isPresetVerticalSize,
    [`${prefixCls}-gap-col-${horizontalSize}`]: isPresetHorizontalSize
  }, className, rootClassName, cssVarCls, mergedClassNames.root);
  const itemClassName = clsx(`${prefixCls}-item`, mergedClassNames.item);
  const renderedItems = childNodes.map((child, i) => {
    const key = child?.key || `${itemClassName}-${i}`;
    return /* @__PURE__ */ reactExports.createElement(Item, {
      prefix: prefixCls,
      classNames: mergedClassNames,
      styles: mergedStyles,
      className: itemClassName,
      key,
      index: i,
      separator: mergedSeparator,
      style: mergedStyles.item
    }, child);
  });
  const memoizedSpaceContext = reactExports.useMemo(() => {
    const calcLatestIndex = childNodes.reduce((latest, child, i) => isReactRenderable(child) ? i : latest, 0);
    return {
      latestIndex: calcLatestIndex
    };
  }, [childNodes]);
  if (childNodes.length === 0) {
    return null;
  }
  const gapStyle = {};
  if (wrap) {
    gapStyle.flexWrap = "wrap";
  }
  if (!isPresetHorizontalSize && isValidHorizontalSize) {
    gapStyle.columnGap = horizontalSize;
  }
  if (!isPresetVerticalSize && isValidVerticalSize) {
    gapStyle.rowGap = verticalSize;
  }
  return /* @__PURE__ */ reactExports.createElement("div", {
    ref,
    className: rootClassNames,
    style: {
      ...gapStyle,
      ...mergedStyles.root
    },
    ...restProps
  }, /* @__PURE__ */ reactExports.createElement(SpaceContextProvider, {
    value: memoizedSpaceContext
  }, renderedItems));
});
const Space = InternalSpace;
Space.Compact = Compact;
Space.Addon = SpaceAddon;
export {
  Space as S
};
