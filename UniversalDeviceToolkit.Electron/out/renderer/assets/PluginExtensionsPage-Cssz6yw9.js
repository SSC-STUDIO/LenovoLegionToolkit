import { r as reactExports, f as clsx, $ as React, a4 as useId, ah as isNumber, bs as presetPrimaryColors, J as useComponentConfig, Z as isPlainObject, h as omit, m as Tooltip, v as genStyleHooks, w as merge, A as resetComponent, bt as Keyframe, B as FastColor, L as useSemanticRootStyle, N as useMergeSemantic, ap as RefIcon$1, ab as RefIcon$2, aq as RefIcon$3, bu as RefIcon$4, aA as getDefaultExportFromCjs, aB as Icon, o as on, i as invoke, c as create, u as useTranslation, j as jsxRuntimeExports, F as Flex, T as Typography, C as Card, s as staticMethods } from "./index-3RTipSd5.js";
import { S as Space } from "./index-Dro2pb1j.js";
import { B as Button, T as Tag } from "./index-uyL__3sF.js";
import { I as Input } from "./index-DBj245TA.js";
import { f as RefIcon$5, S as Select, E as Empty, C as Collapse } from "./index-BxBscas6.js";
import { A as Alert } from "./index-DWceQSKB.js";
import { L as List } from "./index-DaSpOuam.js";
import { P as Popconfirm } from "./index-DdhF4o9H.js";
import "./Addon-CECo-qGW.js";
import "./Input-mSSMIOSE.js";
import "./index-Hdt_DTHG.js";
const defaultProps = {
  percent: 0,
  prefixCls: "rc-progress",
  strokeColor: "#2db7f5",
  strokeLinecap: "round",
  strokeWidth: 1,
  railColor: "#D9D9D9",
  railWidth: 1,
  gapPosition: "bottom",
  loading: false
};
const useTransitionDuration = () => {
  const pathsRef = reactExports.useRef([]);
  const prevTimeStamp = reactExports.useRef(null);
  reactExports.useEffect(() => {
    const now = Date.now();
    let updated = false;
    pathsRef.current.forEach((path) => {
      if (!path) {
        return;
      }
      updated = true;
      const pathStyle = path.style;
      pathStyle.transitionDuration = ".3s, .3s, .3s, .06s";
      if (prevTimeStamp.current && now - prevTimeStamp.current < 100) {
        pathStyle.transitionDuration = "0s, 0s";
      }
    });
    if (updated) {
      prevTimeStamp.current = Date.now();
    }
  });
  return pathsRef.current;
};
const Block = ({
  bg,
  children
}) => /* @__PURE__ */ reactExports.createElement("div", {
  style: {
    width: "100%",
    height: "100%",
    background: bg
  }
}, children);
function getPtgColors(color, scale) {
  return Object.keys(color).map((key) => {
    const parsedKey = parseFloat(key);
    const ptgKey = `${Math.floor(parsedKey * scale)}%`;
    return `${color[key]} ${ptgKey}`;
  });
}
const PtgCircle = /* @__PURE__ */ reactExports.forwardRef((props, ref) => {
  const {
    prefixCls,
    color,
    gradientId,
    radius,
    className,
    style: circleStyleForStack,
    ptg,
    strokeLinecap,
    strokeWidth,
    size,
    gapDegree
  } = props;
  const isGradient = color && typeof color === "object";
  const stroke = isGradient ? `#FFF` : void 0;
  const halfSize = size / 2;
  const circleNode = /* @__PURE__ */ reactExports.createElement("circle", {
    className: clsx(`${prefixCls}-circle-path`, className),
    r: radius,
    cx: halfSize,
    cy: halfSize,
    stroke,
    strokeLinecap,
    strokeWidth,
    opacity: ptg === 0 ? 0 : 1,
    style: circleStyleForStack,
    ref
  });
  if (!isGradient) {
    return circleNode;
  }
  const maskId = `${gradientId}-conic`;
  const fromDeg = gapDegree ? `${180 + gapDegree / 2}deg` : "0deg";
  const conicColors = getPtgColors(color, (360 - gapDegree) / 360);
  const linearColors = getPtgColors(color, 1);
  const conicColorBg = `conic-gradient(from ${fromDeg}, ${conicColors.join(", ")})`;
  const linearColorBg = `linear-gradient(to ${gapDegree ? "bottom" : "top"}, ${linearColors.join(", ")})`;
  return /* @__PURE__ */ reactExports.createElement(reactExports.Fragment, null, /* @__PURE__ */ reactExports.createElement("mask", {
    id: maskId
  }, circleNode), /* @__PURE__ */ reactExports.createElement("foreignObject", {
    x: 0,
    y: 0,
    width: size,
    height: size,
    mask: `url(#${maskId})`
  }, /* @__PURE__ */ reactExports.createElement(Block, {
    bg: linearColorBg
  }, /* @__PURE__ */ reactExports.createElement(Block, {
    bg: conicColorBg
  }))));
});
const VIEW_BOX_SIZE = 100;
const getCircleStyle = (perimeter, perimeterWithoutGap, offset, percent, rotateDeg, gapDegree, gapPosition, strokeColor, strokeLinecap, strokeWidth, stepSpace = 0) => {
  const offsetDeg = offset / 100 * 360 * ((360 - gapDegree) / 360);
  const positionDeg = gapDegree === 0 ? 0 : {
    bottom: 0,
    top: 180,
    left: 90,
    right: -90
  }[gapPosition];
  let strokeDashoffset = (100 - percent) / 100 * perimeterWithoutGap;
  if (strokeLinecap === "round" && percent !== 100) {
    strokeDashoffset += strokeWidth / 2;
    if (strokeDashoffset >= perimeterWithoutGap) {
      strokeDashoffset = perimeterWithoutGap - 0.01;
    }
  }
  const halfSize = VIEW_BOX_SIZE / 2;
  return {
    stroke: typeof strokeColor === "string" ? strokeColor : void 0,
    strokeDasharray: `${perimeterWithoutGap}px ${perimeter}`,
    strokeDashoffset: strokeDashoffset + stepSpace,
    transform: `rotate(${rotateDeg + offsetDeg + positionDeg}deg)`,
    transformOrigin: `${halfSize}px ${halfSize}px`,
    transition: "stroke-dashoffset .3s ease 0s, stroke-dasharray .3s ease 0s, stroke .3s, stroke-width .06s ease .3s, opacity .3s ease 0s",
    fillOpacity: 0
  };
};
const getIndeterminateCircle = (({
  id,
  loading
}) => {
  if (!loading) {
    return {
      indeterminateStyleProps: {},
      indeterminateStyleAnimation: null
    };
  }
  const animationName = `${id}-indeterminate-animate`;
  return {
    indeterminateStyleProps: {
      transform: "rotate(0deg)",
      animation: `${animationName} 1s linear infinite`
    },
    indeterminateStyleAnimation: /* @__PURE__ */ React.createElement("style", null, `@keyframes ${animationName} {
            0% { transform: rotate(0deg); }
            100% { transform: rotate(360deg); }
          }`)
  };
});
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
function toArray(value) {
  const mergedValue = value ?? [];
  return Array.isArray(mergedValue) ? mergedValue : [mergedValue];
}
const Circle$1 = (props) => {
  const {
    id,
    prefixCls,
    classNames = {},
    styles = {},
    steps,
    strokeWidth,
    railWidth,
    gapDegree = 0,
    gapPosition,
    railColor,
    strokeLinecap,
    style,
    className,
    strokeColor,
    percent,
    loading,
    ...restProps
  } = {
    ...defaultProps,
    ...props
  };
  const halfSize = VIEW_BOX_SIZE / 2;
  const mergedId = useId(id);
  const gradientId = `${mergedId}-gradient`;
  const radius = halfSize - strokeWidth / 2;
  const perimeter = Math.PI * 2 * radius;
  const rotateDeg = gapDegree > 0 ? 90 + gapDegree / 2 : -90;
  const perimeterWithoutGap = perimeter * ((360 - gapDegree) / 360);
  const {
    count: stepCount,
    gap: stepGap
  } = typeof steps === "object" ? steps : {
    count: steps,
    gap: 2
  };
  const percentList = toArray(percent);
  const strokeColorList = toArray(strokeColor);
  const gradient = strokeColorList.find((color) => color && typeof color === "object");
  const isConicGradient = gradient && typeof gradient === "object";
  const mergedStrokeLinecap = isConicGradient ? "butt" : strokeLinecap;
  const {
    indeterminateStyleProps,
    indeterminateStyleAnimation
  } = getIndeterminateCircle({
    id: mergedId,
    loading
  });
  const circleStyle = getCircleStyle(perimeter, perimeterWithoutGap, 0, 100, rotateDeg, gapDegree, gapPosition, railColor, mergedStrokeLinecap, strokeWidth);
  const paths = useTransitionDuration();
  const getStokeList = () => {
    let stackPtg = 0;
    return percentList.map((ptg, index) => {
      const color = strokeColorList[index] || strokeColorList[strokeColorList.length - 1];
      const circleStyleForStack = getCircleStyle(perimeter, perimeterWithoutGap, stackPtg, ptg, rotateDeg, gapDegree, gapPosition, color, mergedStrokeLinecap, strokeWidth);
      stackPtg += ptg;
      return /* @__PURE__ */ reactExports.createElement(PtgCircle, {
        key: index,
        color,
        ptg,
        radius,
        prefixCls,
        gradientId,
        className: classNames.track,
        style: {
          ...circleStyleForStack,
          ...indeterminateStyleProps,
          ...styles.track
        },
        strokeLinecap: mergedStrokeLinecap,
        strokeWidth,
        gapDegree,
        ref: (elem) => {
          paths[index] = elem;
        },
        size: VIEW_BOX_SIZE
      });
    }).reverse();
  };
  const getStepStokeList = () => {
    const current = Math.round(stepCount * (percentList[0] / 100));
    const stepPtg = 100 / stepCount;
    let stackPtg = 0;
    return new Array(stepCount).fill(null).map((_, index) => {
      const color = index <= current - 1 ? strokeColorList[0] : railColor;
      const stroke = color && typeof color === "object" ? `url(#${gradientId})` : void 0;
      const circleStyleForStack = getCircleStyle(perimeter, perimeterWithoutGap, stackPtg, stepPtg, rotateDeg, gapDegree, gapPosition, color, "butt", strokeWidth, stepGap);
      stackPtg += (perimeterWithoutGap - circleStyleForStack.strokeDashoffset + stepGap) * 100 / perimeterWithoutGap;
      return /* @__PURE__ */ reactExports.createElement("circle", {
        key: index,
        className: clsx(`${prefixCls}-circle-path`, classNames.track),
        r: radius,
        cx: halfSize,
        cy: halfSize,
        stroke,
        strokeWidth,
        opacity: 1,
        style: {
          ...circleStyleForStack,
          ...styles.track
        },
        ref: (elem) => {
          paths[index] = elem;
        }
      });
    });
  };
  return /* @__PURE__ */ reactExports.createElement("svg", _extends$1({
    className: clsx(`${prefixCls}-circle`, classNames.root, className),
    viewBox: `0 0 ${VIEW_BOX_SIZE} ${VIEW_BOX_SIZE}`,
    style: {
      ...styles.root,
      ...style
    },
    id,
    role: "presentation"
  }, restProps), !stepCount && /* @__PURE__ */ reactExports.createElement("circle", {
    className: clsx(`${prefixCls}-circle-rail`, classNames.rail),
    r: radius,
    cx: halfSize,
    cy: halfSize,
    stroke: railColor,
    strokeLinecap: mergedStrokeLinecap,
    strokeWidth: railWidth || strokeWidth,
    style: {
      ...circleStyle,
      ...styles.rail
    }
  }), stepCount ? getStepStokeList() : getStokeList(), indeterminateStyleAnimation);
};
function validProgress(progress) {
  if (!progress || progress < 0) {
    return 0;
  }
  if (progress > 100) {
    return 100;
  }
  return progress;
}
function getSuccessPercent({
  success
}) {
  let percent;
  if (success && "percent" in success) {
    percent = success.percent;
  }
  return percent;
}
const getPercentage = ({
  percent,
  success
}) => {
  const realSuccessPercent = validProgress(getSuccessPercent({
    success
  }));
  return [realSuccessPercent, validProgress(validProgress(percent) - realSuccessPercent)];
};
const getStrokeColor = ({
  success = {},
  strokeColor
}) => {
  const {
    strokeColor: successColor
  } = success;
  return [successColor || presetPrimaryColors.green, strokeColor || null];
};
const getSize = (size, type, extra) => {
  let width = -1;
  let height = -1;
  if (type === "step") {
    const steps = extra.steps;
    const strokeWidth = extra.strokeWidth;
    if (typeof size === "string" || typeof size === "undefined") {
      width = size === "small" ? 2 : 14;
      height = strokeWidth ?? 8;
    } else if (isNumber(size)) {
      [width, height] = [size, size];
    } else {
      [width = 14, height = 8] = Array.isArray(size) ? size : [size.width, size.height];
    }
    width *= steps;
  } else if (type === "line") {
    const strokeWidth = extra?.strokeWidth;
    if (typeof size === "string" || typeof size === "undefined") {
      height = strokeWidth || (size === "small" ? 6 : 8);
    } else if (isNumber(size)) {
      [width, height] = [size, size];
    } else {
      [width = -1, height = 8] = Array.isArray(size) ? size : [size.width, size.height];
    }
  } else if (type === "circle" || type === "dashboard") {
    if (typeof size === "string" || typeof size === "undefined") {
      [width, height] = size === "small" ? [60, 60] : [120, 120];
    } else if (isNumber(size)) {
      [width, height] = [size, size];
    } else if (Array.isArray(size)) {
      width = size[0] ?? size[1] ?? 120;
      height = size[0] ?? size[1] ?? 120;
    }
  }
  return [width, height];
};
const CIRCLE_MIN_STROKE_WIDTH = 3;
const getMinPercent = (width) => CIRCLE_MIN_STROKE_WIDTH / width * 100;
const OMIT_SEMANTIC_NAMES = ["root", "body", "indicator"];
const Circle = (props) => {
  const {
    prefixCls,
    classNames,
    styles,
    railColor,
    trailColor,
    strokeLinecap = "round",
    gapPosition,
    gapPlacement,
    gapDegree,
    width: originWidth = 120,
    type,
    children,
    success,
    size = originWidth,
    steps
  } = props;
  const {
    direction
  } = useComponentConfig("progress");
  const mergedRailColor = railColor ?? trailColor;
  const [width, height] = getSize(size, "circle");
  let {
    strokeWidth
  } = props;
  if (strokeWidth === void 0) {
    strokeWidth = Math.max(getMinPercent(width), 6);
  }
  const circleStyle = {
    width,
    height,
    fontSize: width * 0.15 + 6
  };
  const realGapDegree = reactExports.useMemo(() => {
    if (gapDegree || gapDegree === 0) {
      return gapDegree;
    }
    if (type === "dashboard") {
      return 75;
    }
    return void 0;
  }, [gapDegree, type]);
  const percentArray = getPercentage(props);
  const gapPos = reactExports.useMemo(() => {
    const mergedPlacement = (gapPlacement ?? gapPosition) || type === "dashboard" && "bottom" || void 0;
    const isRTL = direction === "rtl";
    switch (mergedPlacement) {
      case "start":
        return isRTL ? "right" : "left";
      case "end":
        return isRTL ? "left" : "right";
      default:
        return mergedPlacement;
    }
  }, [direction, gapPlacement, gapPosition, type]);
  const isGradient = isPlainObject(props.strokeColor);
  const strokeColor = getStrokeColor({
    success,
    strokeColor: props.strokeColor
  });
  const wrapperClassName = clsx(`${prefixCls}-body`, {
    [`${prefixCls}-circle-gradient`]: isGradient
  }, classNames.body);
  const circleContent = /* @__PURE__ */ reactExports.createElement(Circle$1, {
    steps,
    percent: steps ? percentArray[1] : percentArray,
    strokeWidth,
    railWidth: strokeWidth,
    strokeColor: steps ? strokeColor[1] : strokeColor,
    strokeLinecap,
    railColor: mergedRailColor,
    prefixCls,
    gapDegree: realGapDegree,
    gapPosition: gapPos,
    classNames: omit(classNames, OMIT_SEMANTIC_NAMES),
    styles: omit(styles, OMIT_SEMANTIC_NAMES)
  });
  const smallCircle = width <= 20;
  const node = /* @__PURE__ */ reactExports.createElement("div", {
    className: wrapperClassName,
    style: {
      ...circleStyle,
      ...styles.body
    }
  }, circleContent, !smallCircle && children);
  if (smallCircle) {
    return /* @__PURE__ */ reactExports.createElement(Tooltip, {
      title: children
    }, node);
  }
  return node;
};
const LineStrokeColorVar = "--progress-line-stroke-color";
const genAntProgressActive = (isRtl) => {
  const direction = "-100%";
  return new Keyframe(`antProgress${"LTR"}Active`, {
    "0%": {
      transform: `translateX(${direction}) scaleX(0)`,
      opacity: 0.1
    },
    "20%": {
      transform: `translateX(${direction}) scaleX(0)`,
      opacity: 0.5
    },
    to: {
      transform: "translateX(0) scaleX(1)",
      opacity: 0
    }
  });
};
const genBaseStyle = (token) => {
  const {
    componentCls: progressCls,
    iconCls: iconPrefixCls
  } = token;
  return {
    [progressCls]: {
      ...resetComponent(token),
      display: "inline-flex",
      "&-rtl": {
        direction: "rtl"
      },
      [`${progressCls}-indicator`]: {
        color: token.colorText,
        lineHeight: 1,
        whiteSpace: "nowrap",
        verticalAlign: "middle",
        wordBreak: "normal",
        [iconPrefixCls]: {
          fontSize: token.fontSize
        }
      },
      [`&${progressCls}-status-exception`]: {
        [`${progressCls}-indicator`]: {
          color: token.colorError
        }
      },
      [`&${progressCls}-status-success`]: {
        [`${progressCls}-indicator`]: {
          color: token.colorSuccess
        }
      }
    }
  };
};
const genLineStyle = (token) => {
  const {
    componentCls
  } = token;
  return {
    [`${componentCls}-line`]: {
      position: "relative",
      width: "100%",
      fontSize: token.fontSize,
      [`${componentCls}-body`]: {
        display: "inline-flex",
        alignItems: "center",
        width: "100%",
        gap: token.marginXS
      },
      [`${componentCls}-rail`]: {
        flex: "auto",
        background: token.remainingColor,
        borderRadius: token.lineBorderRadius,
        position: "relative",
        width: "100%",
        overflow: "hidden"
      },
      [`&${componentCls}-status-active`]: {
        [`${componentCls}-track:after`]: {
          content: '""',
          position: "absolute",
          inset: 0,
          backgroundColor: token.colorBgContainer,
          borderRadius: "inherit",
          opacity: 0,
          animationName: genAntProgressActive(),
          animationDuration: token.progressActiveMotionDuration,
          animationTimingFunction: token.motionEaseOutQuint,
          animationIterationCount: "infinite"
        }
      },
      [`${componentCls}-track`]: {
        position: "absolute",
        insetInlineStart: 0,
        insetBlock: 0,
        borderRadius: "inherit",
        background: token.defaultColor,
        transition: `all ${token.motionDurationSlow} ${token.motionEaseInOutCirc}`,
        minWidth: "max-content",
        display: "flex",
        alignItems: "center",
        "&-success": {
          background: token.colorSuccess
        }
      },
      [`&${componentCls}-status-exception`]: {
        [`${componentCls}-track`]: {
          background: token.colorError
        }
      },
      [`&${componentCls}-status-success`]: {
        [`${componentCls}-track`]: {
          background: token.colorSuccess
        }
      },
      // >>>>> indicator
      // >>> Outer
      [`${componentCls}-indicator-outer`]: {
        [`&${componentCls}-indicator-start`]: {
          order: -1
        }
      },
      [`${componentCls}-body-layout-bottom`]: {
        flexDirection: "column",
        alignItems: "center",
        gap: token.marginXXS
      },
      // >>> Inner
      [`${componentCls}-indicator${componentCls}-indicator-inner`]: {
        color: token.colorWhite,
        paddingInline: token.paddingXXS,
        width: "100%",
        display: "flex",
        justifyContent: "center",
        [`&${componentCls}-indicator-end`]: {
          justifyContent: "end"
        },
        [`&${componentCls}-indicator-start`]: {
          justifyContent: "start"
        },
        [`&${componentCls}-indicator-bright`]: {
          color: "rgba(0, 0, 0, 0.45)"
        }
      }
    }
  };
};
const genCircleStyle = (token) => {
  const {
    componentCls: progressCls,
    iconCls: iconPrefixCls
  } = token;
  return {
    [`${progressCls}-circle`]: {
      [`${progressCls}-circle-rail`]: {
        stroke: token.remainingColor
      },
      [`${progressCls}-body:not(${progressCls}-circle-gradient)`]: {
        [`${progressCls}-circle-path`]: {
          stroke: token.defaultColor
        }
      },
      [`${progressCls}-body`]: {
        position: "relative",
        lineHeight: 1,
        backgroundColor: "transparent"
      },
      [`${progressCls}-indicator`]: {
        position: "absolute",
        insetBlockStart: "50%",
        insetInlineStart: 0,
        width: "100%",
        margin: 0,
        padding: 0,
        color: token.circleTextColor,
        fontSize: token.circleTextFontSize,
        lineHeight: 1,
        whiteSpace: "normal",
        textAlign: "center",
        transform: "translateY(-50%)",
        [iconPrefixCls]: {
          fontSize: token.circleIconFontSize
        }
      },
      [`&${progressCls}-status-exception`]: {
        [`${progressCls}-body:not(${progressCls}-circle-gradient)`]: {
          [`${progressCls}-circle-path`]: {
            stroke: token.colorError
          }
        }
      },
      [`&${progressCls}-status-success`]: {
        [`${progressCls}-body:not(${progressCls}-circle-gradient)`]: {
          [`${progressCls}-circle-path`]: {
            stroke: token.colorSuccess
          }
        }
      }
    },
    [`${progressCls}-inline-circle`]: {
      lineHeight: 1,
      [`${progressCls}-inner`]: {
        verticalAlign: "bottom"
      }
    }
  };
};
const genStepStyle = (token) => {
  const {
    componentCls: progressCls
  } = token;
  return {
    [progressCls]: {
      [`${progressCls}-steps`]: {
        display: "inline-block",
        "&-body": {
          display: "flex",
          flexDirection: "row",
          alignItems: "center",
          gap: token.progressStepMarginInlineEnd,
          [`${progressCls}-indicator`]: {
            marginInlineStart: token.marginXS
          }
        },
        "&-item": {
          flexShrink: 0,
          minWidth: token.progressStepMinWidth,
          backgroundColor: token.remainingColor,
          transition: `all ${token.motionDurationSlow}`,
          "&-active": {
            backgroundColor: token.defaultColor
          }
        }
      }
    }
  };
};
const genSmallLine = (token) => {
  const {
    componentCls: progressCls,
    iconCls: iconPrefixCls
  } = token;
  return {
    [progressCls]: {
      [`${progressCls}-small&-line, ${progressCls}-small&-line ${progressCls}-indicator ${iconPrefixCls}`]: {
        fontSize: token.fontSizeSM
      }
    }
  };
};
const prepareComponentToken = (token) => ({
  circleTextColor: token.colorText,
  defaultColor: token.colorInfo,
  remainingColor: token.colorFillSecondary,
  lineBorderRadius: 100,
  // magic for capsule shape, should be a very large number
  circleTextFontSize: "1em",
  circleIconFontSize: `${token.fontSize / token.fontSizeSM}em`
});
const useStyle = genStyleHooks("Progress", (token) => {
  const progressStepMarginInlineEnd = token.calc(token.marginXXS).div(2).equal();
  const progressToken = merge(token, {
    progressStepMarginInlineEnd,
    progressStepMinWidth: progressStepMarginInlineEnd,
    progressActiveMotionDuration: "2.4s"
  });
  return [genBaseStyle(progressToken), genLineStyle(progressToken), genCircleStyle(progressToken), genStepStyle(progressToken), genSmallLine(progressToken)];
}, prepareComponentToken);
const sortGradient = (gradients) => {
  let tempArr = [];
  Object.keys(gradients).forEach((key) => {
    const formattedKey = Number.parseFloat(key.replace(/%/g, ""));
    if (!Number.isNaN(formattedKey)) {
      tempArr.push({
        key: formattedKey,
        value: gradients[key]
      });
    }
  });
  tempArr = tempArr.sort((a, b) => a.key - b.key);
  return tempArr.map(({
    key,
    value
  }) => `${value} ${key}%`).join(", ");
};
const handleGradient = (strokeColor, directionConfig) => {
  const {
    from = presetPrimaryColors.blue,
    to = presetPrimaryColors.blue,
    direction = directionConfig === "rtl" ? "to left" : "to right",
    ...rest
  } = strokeColor;
  if (Object.keys(rest).length !== 0) {
    const sortedGradients = sortGradient(rest);
    const background2 = `linear-gradient(${direction}, ${sortedGradients})`;
    return {
      background: background2,
      [LineStrokeColorVar]: background2
    };
  }
  const background = `linear-gradient(${direction}, ${from}, ${to})`;
  return {
    background,
    [LineStrokeColorVar]: background
  };
};
const Line = (props) => {
  const {
    prefixCls,
    classNames,
    styles,
    direction: directionConfig,
    percent,
    size,
    strokeWidth,
    strokeColor,
    strokeLinecap = "round",
    children,
    railColor,
    trailColor,
    percentPosition,
    success
  } = props;
  const {
    align: infoAlign,
    type: infoPosition
  } = percentPosition;
  const mergedRailColor = railColor ?? trailColor;
  const borderRadius = strokeLinecap === "square" || strokeLinecap === "butt" ? 0 : void 0;
  const mergedSize = size ?? [-1, strokeWidth || (size === "small" ? 6 : 8)];
  const [width, height] = getSize(mergedSize, "line", {
    strokeWidth
  });
  const railStyle = {
    backgroundColor: mergedRailColor || void 0,
    borderRadius,
    height
  };
  const trackCls = `${prefixCls}-track`;
  const backgroundProps = strokeColor && typeof strokeColor !== "string" ? handleGradient(strokeColor, directionConfig) : {
    [LineStrokeColorVar]: strokeColor,
    background: strokeColor
  };
  const percentTrackStyle = {
    width: `${validProgress(percent)}%`,
    height,
    borderRadius,
    ...backgroundProps
  };
  const successPercent = getSuccessPercent(props);
  const successTrackStyle = {
    width: `${validProgress(successPercent)}%`,
    height,
    borderRadius,
    backgroundColor: success?.strokeColor
  };
  return /* @__PURE__ */ reactExports.createElement("div", {
    className: clsx(`${prefixCls}-body`, classNames.body, {
      [`${prefixCls}-body-layout-bottom`]: infoAlign === "center" && infoPosition === "outer"
    }),
    style: {
      width: width > 0 ? width : "100%",
      ...styles.body
    }
  }, /* @__PURE__ */ reactExports.createElement("div", {
    className: clsx(`${prefixCls}-rail`, classNames.rail),
    style: {
      ...railStyle,
      ...styles.rail
    }
  }, /* @__PURE__ */ reactExports.createElement("div", {
    className: clsx(trackCls, classNames.track),
    style: {
      ...percentTrackStyle,
      ...styles.track
    }
  }, infoPosition === "inner" && children), successPercent !== void 0 && /* @__PURE__ */ reactExports.createElement("div", {
    className: clsx(trackCls, `${trackCls}-success`, classNames.track),
    style: {
      ...successTrackStyle,
      ...styles.track
    }
  })), infoPosition === "outer" && children);
};
const Steps = (props) => {
  const {
    classNames,
    styles,
    size,
    steps,
    rounding: customRounding = Math.round,
    percent = 0,
    strokeWidth = 8,
    strokeColor,
    railColor,
    trailColor,
    prefixCls,
    children
  } = props;
  const current = customRounding(steps * (percent / 100));
  const stepWidth = size === "small" ? 2 : 14;
  const mergedSize = size ?? [stepWidth, strokeWidth];
  const [width, height] = getSize(mergedSize, "step", {
    steps,
    strokeWidth
  });
  const unitWidth = width / steps;
  const styledSteps = Array.from({
    length: steps
  });
  const mergedRailColor = railColor ?? trailColor;
  for (let i = 0; i < steps; i++) {
    const color = Array.isArray(strokeColor) ? strokeColor[i] : strokeColor;
    styledSteps[i] = /* @__PURE__ */ reactExports.createElement("div", {
      key: i,
      className: clsx(`${prefixCls}-steps-item`, {
        [`${prefixCls}-steps-item-active`]: i <= current - 1
      }, classNames.track),
      style: {
        backgroundColor: i <= current - 1 ? color : mergedRailColor,
        width: unitWidth,
        height,
        ...styles.track
      }
    });
  }
  return /* @__PURE__ */ reactExports.createElement("div", {
    className: clsx(`${prefixCls}-steps-body`, classNames.body),
    style: styles.body
  }, styledSteps, children);
};
const ProgressStatuses = ["normal", "exception", "active", "success"];
const Progress = /* @__PURE__ */ reactExports.forwardRef((props, ref) => {
  const {
    prefixCls: customizePrefixCls,
    className,
    rootClassName,
    classNames,
    styles,
    steps,
    strokeColor,
    percent = 0,
    size = "medium",
    showInfo = true,
    type = "line",
    status,
    format,
    style,
    percentPosition = {},
    ...restProps
  } = props;
  const {
    align: infoAlign = "end",
    type: infoPosition = "outer"
  } = percentPosition;
  const strokeColorNotArray = Array.isArray(strokeColor) ? strokeColor[0] : strokeColor;
  const strokeColorNotGradient = typeof strokeColor === "string" || Array.isArray(strokeColor) ? strokeColor : void 0;
  const strokeColorIsBright = reactExports.useMemo(() => {
    if (strokeColorNotArray) {
      const color = typeof strokeColorNotArray === "string" ? strokeColorNotArray : Object.values(strokeColorNotArray)[0];
      return new FastColor(color).isLight();
    }
    return false;
  }, [strokeColor]);
  const percentNumber = reactExports.useMemo(() => {
    const successPercent = getSuccessPercent(props);
    return Number.parseInt(successPercent !== void 0 ? (successPercent ?? 0)?.toString() : (percent ?? 0)?.toString(), 10);
  }, [percent, props.success]);
  const progressStatus = reactExports.useMemo(() => {
    if (!ProgressStatuses.includes(status) && percentNumber >= 100) {
      return "success";
    }
    return status || "normal";
  }, [status, percentNumber]);
  const {
    getPrefixCls,
    direction,
    className: contextClassName,
    style: contextStyle,
    classNames: contextClassNames,
    styles: contextStyles
  } = useComponentConfig("progress");
  const prefixCls = getPrefixCls("progress", customizePrefixCls);
  const [hashId, cssVarCls] = useStyle(prefixCls);
  const mergedProps = {
    ...props,
    percent,
    type,
    size,
    showInfo,
    percentPosition
  };
  const contextStyleRoot = useSemanticRootStyle(contextStyle);
  const styleRoot = useSemanticRootStyle(style);
  const [mergedClassNames, mergedStyles] = useMergeSemantic([contextClassNames, classNames], [contextStyles, contextStyleRoot, styles, styleRoot], {
    props: mergedProps
  });
  const isLineType = type === "line";
  const isPureLineType = isLineType && !steps;
  const progressInfo = reactExports.useMemo(() => {
    if (!showInfo) {
      return null;
    }
    const successPercent = getSuccessPercent(props);
    let text;
    const textFormatter = format || ((number) => `${number}%`);
    const isBrightInnerColor = isLineType && strokeColorIsBright && infoPosition === "inner";
    if (infoPosition === "inner" || format || progressStatus !== "exception" && progressStatus !== "success") {
      text = textFormatter(validProgress(percent), validProgress(successPercent));
    } else if (progressStatus === "exception") {
      text = isLineType ? /* @__PURE__ */ reactExports.createElement(RefIcon$1, null) : /* @__PURE__ */ reactExports.createElement(RefIcon$2, null);
    } else if (progressStatus === "success") {
      text = isLineType ? /* @__PURE__ */ reactExports.createElement(RefIcon$3, null) : /* @__PURE__ */ reactExports.createElement(RefIcon$4, null);
    }
    return /* @__PURE__ */ reactExports.createElement("span", {
      className: clsx(`${prefixCls}-indicator`, {
        [`${prefixCls}-indicator-bright`]: isBrightInnerColor,
        [`${prefixCls}-indicator-${infoAlign}`]: isPureLineType,
        [`${prefixCls}-indicator-${infoPosition}`]: isPureLineType
      }, mergedClassNames.indicator),
      style: mergedStyles.indicator,
      title: typeof text === "string" ? text : void 0
    }, text);
  }, [showInfo, percent, percentNumber, progressStatus, type, prefixCls, format, isLineType, strokeColorIsBright, infoPosition, infoAlign, isPureLineType, mergedClassNames.indicator, mergedStyles.indicator]);
  const sharedProps = {
    ...props,
    classNames: mergedClassNames,
    styles: mergedStyles
  };
  let progress;
  if (type === "line") {
    progress = steps ? /* @__PURE__ */ reactExports.createElement(Steps, {
      ...sharedProps,
      strokeColor: strokeColorNotGradient,
      prefixCls,
      steps: isPlainObject(steps) ? steps.count : steps
    }, progressInfo) : /* @__PURE__ */ reactExports.createElement(Line, {
      ...sharedProps,
      strokeColor: strokeColorNotArray,
      prefixCls,
      direction,
      percentPosition: {
        align: infoAlign,
        type: infoPosition
      }
    }, progressInfo);
  } else if (type === "circle" || type === "dashboard") {
    progress = /* @__PURE__ */ reactExports.createElement(Circle, {
      ...sharedProps,
      strokeColor: strokeColorNotArray,
      prefixCls,
      progressStatus
    }, progressInfo);
  }
  const classString = clsx(prefixCls, `${prefixCls}-status-${progressStatus}`, {
    [`${prefixCls}-${type === "dashboard" && "circle" || type}`]: type !== "line",
    [`${prefixCls}-inline-circle`]: type === "circle" && getSize(size, "circle")[0] <= 20,
    [`${prefixCls}-line`]: isPureLineType,
    [`${prefixCls}-line-align-${infoAlign}`]: isPureLineType,
    [`${prefixCls}-line-position-${infoPosition}`]: isPureLineType,
    [`${prefixCls}-steps`]: steps,
    [`${prefixCls}-show-info`]: showInfo,
    [`${prefixCls}-small`]: size === "small",
    [`${prefixCls}-rtl`]: direction === "rtl"
  }, contextClassName, className, rootClassName, mergedClassNames.root, hashId, cssVarCls);
  return /* @__PURE__ */ reactExports.createElement("div", {
    ref,
    style: mergedStyles.root,
    className: classString,
    role: "progressbar",
    "aria-valuenow": percentNumber,
    "aria-valuemin": 0,
    "aria-valuemax": 100,
    ...omit(restProps, ["railColor", "trailColor", "strokeWidth", "width", "gapDegree", "gapPosition", "gapPlacement", "strokeLinecap", "success"])
  }, progress);
});
var ReloadOutlined$1 = {};
var hasRequiredReloadOutlined;
function requireReloadOutlined() {
  if (hasRequiredReloadOutlined) return ReloadOutlined$1;
  hasRequiredReloadOutlined = 1;
  Object.defineProperty(ReloadOutlined$1, "__esModule", { value: true });
  var ReloadOutlined2 = { "icon": { "tag": "svg", "attrs": { "viewBox": "64 64 896 896", "focusable": "false" }, "children": [{ "tag": "path", "attrs": { "d": "M909.1 209.3l-56.4 44.1C775.8 155.1 656.2 92 521.9 92 290 92 102.3 279.5 102 511.5 101.7 743.7 289.8 932 521.9 932c181.3 0 335.8-115 394.6-276.1 1.5-4.2-.7-8.9-4.9-10.3l-56.7-19.5a8 8 0 00-10.1 4.8c-1.8 5-3.8 10-5.9 14.9-17.3 41-42.1 77.8-73.7 109.4A344.77 344.77 0 01655.9 829c-42.3 17.9-87.4 27-133.8 27-46.5 0-91.5-9.1-133.8-27A341.5 341.5 0 01279 755.2a342.16 342.16 0 01-73.7-109.4c-17.9-42.4-27-87.4-27-133.9s9.1-91.5 27-133.9c17.3-41 42.1-77.8 73.7-109.4 31.6-31.6 68.4-56.4 109.3-73.8 42.3-17.9 87.4-27 133.8-27 46.5 0 91.5 9.1 133.8 27a341.5 341.5 0 01109.3 73.8c9.9 9.9 19.2 20.4 27.8 31.4l-60.2 47a8 8 0 003 14.1l175.6 43c5 1.2 9.9-2.6 9.9-7.7l.8-180.9c-.1-6.6-7.8-10.3-13-6.2z" } }] }, "name": "reload", "theme": "outlined" };
  ReloadOutlined$1.default = ReloadOutlined2;
  return ReloadOutlined$1;
}
var ReloadOutlinedExports = /* @__PURE__ */ requireReloadOutlined();
const ReloadOutlinedSvg = /* @__PURE__ */ getDefaultExportFromCjs(ReloadOutlinedExports);
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
const ReloadOutlined = (props, ref) => /* @__PURE__ */ reactExports.createElement(Icon, _extends({}, props, {
  ref,
  icon: ReloadOutlinedSvg
}));
const RefIcon = /* @__PURE__ */ reactExports.forwardRef(ReloadOutlined);
const pluginsApi = {
  async list(forceRefresh) {
    return invoke(
      "plugins.list",
      forceRefresh ? { forceRefresh } : {}
    );
  },
  async checkUpdates() {
    return invoke("plugins.checkUpdates", {});
  },
  async install(pluginId) {
    return invoke("plugins.install", { pluginId });
  },
  async uninstall(pluginId) {
    return invoke("plugins.uninstall", { pluginId });
  },
  async importFile(filePath) {
    return invoke("plugins.import", { filePath });
  },
  async refresh() {
    return invoke("plugins.refresh", {});
  },
  onInstallProgress(cb) {
    return on("plugins.installProgress", cb);
  },
  onInstalled(cb) {
    return on("plugins.installed", cb);
  },
  onUninstalled(cb) {
    return on("plugins.uninstalled", cb);
  }
};
const usePluginsStore = create()((set, get) => ({
  plugins: [],
  updates: {},
  installingIds: {},
  loading: false,
  offline: false,
  error: null,
  async load(force = false) {
    if (get().loading) return;
    set({ loading: true, error: null });
    try {
      const [listResult, updatesResult] = await Promise.all([
        pluginsApi.list(force),
        pluginsApi.checkUpdates()
      ]);
      const updates = {};
      for (const update of updatesResult.updates) updates[update.id] = update.availableVersion;
      set({
        plugins: listResult.plugins,
        updates,
        offline: !listResult.online,
        loading: false
      });
    } catch (error) {
      set({ error: error.message, loading: false });
    }
  },
  async install(pluginId) {
    try {
      set({ installingIds: { ...get().installingIds, [pluginId]: 0 } });
      const result = await pluginsApi.install(pluginId);
      if (result.ok) await get().load();
      return result.ok;
    } catch (error) {
      set({ error: error.message });
      return false;
    } finally {
      const { [pluginId]: _removed, ...rest } = get().installingIds;
      set({ installingIds: rest });
    }
  },
  async uninstall(pluginId) {
    try {
      const result = await pluginsApi.uninstall(pluginId);
      if (result.ok) await get().load();
      return { ok: result.ok, dependencyBlocked: result.dependencyBlocked ?? false };
    } catch (error) {
      set({ error: error.message });
      return { ok: false, dependencyBlocked: false };
    }
  },
  async refresh() {
    try {
      await pluginsApi.refresh();
      await get().load(true);
    } catch (error) {
      set({ error: error.message });
    }
  },
  async importFile(path) {
    try {
      const result = await pluginsApi.importFile(path);
      if (result.ok) await get().load();
      return result.ok;
    } catch (error) {
      set({ error: error.message });
      return false;
    }
  }
}));
pluginsApi.onInstallProgress((progress) => {
  usePluginsStore.setState((state) => {
    if (!(progress.pluginId in state.installingIds)) return state;
    if (progress.phase === "completed" || progress.phase === "failed") {
      const { [progress.pluginId]: _removed, ...rest } = state.installingIds;
      return { installingIds: rest };
    }
    return { installingIds: { ...state.installingIds, [progress.pluginId]: progress.progressPercentage } };
  });
});
pluginsApi.onInstalled(() => {
  void usePluginsStore.getState().load();
});
pluginsApi.onUninstalled(() => {
  void usePluginsStore.getState().load();
});
function PluginStatusTag({ plugin }) {
  const { t } = useTranslation();
  if (plugin.installedVersion) {
    return plugin.updateAvailable ? /* @__PURE__ */ jsxRuntimeExports.jsx(Tag, { color: "warning", children: t("plugins.updateAvailable") }) : /* @__PURE__ */ jsxRuntimeExports.jsx(Tag, { color: "success", children: t("plugins.installed") });
  }
  return /* @__PURE__ */ jsxRuntimeExports.jsx(Tag, { color: "blue", children: t("plugins.online") });
}
function PluginCard({ plugin }) {
  const { t } = useTranslation();
  const install = usePluginsStore((state) => state.install);
  const uninstall = usePluginsStore((state) => state.uninstall);
  const installingIds = usePluginsStore((state) => state.installingIds);
  const installing = plugin.id in installingIds;
  const progress = installingIds[plugin.id] ?? 0;
  const collapseItems = reactExports.useMemo(() => {
    const items = [];
    if (plugin.details) {
      items.push({
        key: "details",
        label: t("plugins.details"),
        children: /* @__PURE__ */ jsxRuntimeExports.jsx(Typography.Paragraph, { style: { marginBottom: 0 }, children: plugin.details })
      });
    }
    if (plugin.usageGuide) {
      items.push({
        key: "usageGuide",
        label: t("plugins.usageGuide"),
        children: /* @__PURE__ */ jsxRuntimeExports.jsx(Typography.Paragraph, { style: { marginBottom: 0 }, children: plugin.usageGuide })
      });
    }
    if (plugin.changelog) {
      items.push({
        key: "changelog",
        label: t("plugins.changelog"),
        children: /* @__PURE__ */ jsxRuntimeExports.jsx(Typography.Paragraph, { style: { marginBottom: 0 }, children: plugin.changelog })
      });
    }
    return items;
  }, [plugin, t]);
  const handleUninstall = async () => {
    const result = await uninstall(plugin.id);
    if (result.dependencyBlocked) {
      staticMethods.warning(t("plugins.dependenciesBlocked"));
    } else if (!result.ok) {
      staticMethods.error(t("plugins.uninstallFailed"));
    }
  };
  const actions = /* @__PURE__ */ jsxRuntimeExports.jsx(Space, { children: installing ? /* @__PURE__ */ jsxRuntimeExports.jsx(Typography.Text, { type: "secondary", children: t("plugins.installing") }) : /* @__PURE__ */ jsxRuntimeExports.jsxs(jsxRuntimeExports.Fragment, { children: [
    plugin.installedVersion ? plugin.updateAvailable && /* @__PURE__ */ jsxRuntimeExports.jsx(Button, { type: "primary", size: "small", onClick: () => void install(plugin.id), children: t("plugins.update") }) : /* @__PURE__ */ jsxRuntimeExports.jsx(Button, { type: "primary", size: "small", onClick: () => void install(plugin.id), children: t("plugins.install") }),
    plugin.installedVersion && /* @__PURE__ */ jsxRuntimeExports.jsx(
      Popconfirm,
      {
        title: t("plugins.uninstallConfirm"),
        onConfirm: () => void handleUninstall(),
        children: /* @__PURE__ */ jsxRuntimeExports.jsx(Button, { size: "small", danger: true, children: t("plugins.uninstall") })
      }
    )
  ] }) });
  return /* @__PURE__ */ jsxRuntimeExports.jsxs(
    Card,
    {
      size: "small",
      title: /* @__PURE__ */ jsxRuntimeExports.jsxs(Space, { size: 8, wrap: true, children: [
        /* @__PURE__ */ jsxRuntimeExports.jsx(Typography.Text, { strong: true, children: plugin.name }),
        /* @__PURE__ */ jsxRuntimeExports.jsxs(Typography.Text, { type: "secondary", children: [
          "v",
          plugin.version
        ] }),
        /* @__PURE__ */ jsxRuntimeExports.jsx(PluginStatusTag, { plugin }),
        plugin.isSystemPlugin && /* @__PURE__ */ jsxRuntimeExports.jsx(Tag, { color: "gold", children: "System" })
      ] }),
      extra: actions,
      children: [
        /* @__PURE__ */ jsxRuntimeExports.jsx(Typography.Paragraph, { type: "secondary", style: { marginBottom: 8 }, children: plugin.description }),
        (plugin.tags.length > 0 || plugin.dependencies.length > 0) && /* @__PURE__ */ jsxRuntimeExports.jsxs(Space, { size: [4, 4], wrap: true, style: { marginBottom: 8 }, children: [
          plugin.tags.map((tag) => /* @__PURE__ */ jsxRuntimeExports.jsx(Tag, { children: tag }, tag)),
          plugin.dependencies.length > 0 && /* @__PURE__ */ jsxRuntimeExports.jsxs(Tag, { color: "geekblue", children: [
            t("plugins.dependencies"),
            ": ",
            plugin.dependencies.join(", ")
          ] })
        ] }),
        installing && /* @__PURE__ */ jsxRuntimeExports.jsx(Progress, { percent: progress, size: "small" }),
        collapseItems.length > 0 && /* @__PURE__ */ jsxRuntimeExports.jsx(Collapse, { ghost: true, size: "small", items: collapseItems })
      ]
    }
  );
}
function PluginExtensionsPage() {
  const { t } = useTranslation();
  const { plugins, loading, offline, error, load, refresh } = usePluginsStore();
  const [search, setSearch] = reactExports.useState("");
  const [filter, setFilter] = reactExports.useState("all");
  reactExports.useEffect(() => {
    void load();
  }, [load]);
  const filtered = reactExports.useMemo(() => {
    const query = search.trim().toLowerCase();
    return plugins.filter((plugin) => {
      if (filter === "installed" && !plugin.installedVersion) return false;
      if (filter === "notInstalled" && plugin.installedVersion) return false;
      if (!query) return true;
      return plugin.name.toLowerCase().includes(query) || plugin.description.toLowerCase().includes(query) || plugin.tags.some((tag) => tag.toLowerCase().includes(query));
    });
  }, [plugins, search, filter]);
  const installedCount = plugins.filter((plugin) => plugin.installedVersion).length;
  const updateCount = plugins.filter((plugin) => plugin.updateAvailable).length;
  return /* @__PURE__ */ jsxRuntimeExports.jsxs(Flex, { vertical: true, gap: 16, children: [
    /* @__PURE__ */ jsxRuntimeExports.jsxs(Flex, { align: "center", justify: "space-between", wrap: true, gap: 8, children: [
      /* @__PURE__ */ jsxRuntimeExports.jsx(Typography.Title, { level: 3, style: { margin: 0 }, children: t("plugins.title") }),
      /* @__PURE__ */ jsxRuntimeExports.jsxs(Space, { wrap: true, children: [
        /* @__PURE__ */ jsxRuntimeExports.jsxs(Typography.Text, { type: "secondary", children: [
          t("plugins.total", { count: plugins.length }),
          " ·",
          " ",
          t("plugins.summary", { count: installedCount }),
          " ·",
          " ",
          t("plugins.updatable", { count: updateCount })
        ] }),
        /* @__PURE__ */ jsxRuntimeExports.jsx(
          Button,
          {
            icon: /* @__PURE__ */ jsxRuntimeExports.jsx(RefIcon, {}),
            loading,
            onClick: () => void refresh(),
            children: t("plugins.refresh")
          }
        )
      ] })
    ] }),
    /* @__PURE__ */ jsxRuntimeExports.jsxs(Flex, { gap: 8, wrap: true, children: [
      /* @__PURE__ */ jsxRuntimeExports.jsx(
        Input,
        {
          allowClear: true,
          prefix: /* @__PURE__ */ jsxRuntimeExports.jsx(RefIcon$5, {}),
          placeholder: t("plugins.search"),
          value: search,
          onChange: (event) => setSearch(event.target.value),
          style: { maxWidth: 320 }
        }
      ),
      /* @__PURE__ */ jsxRuntimeExports.jsx(
        Select,
        {
          value: filter,
          onChange: setFilter,
          style: { width: 160 },
          options: [
            { value: "all", label: t("plugins.filterAll") },
            { value: "installed", label: t("plugins.filterInstalled") },
            { value: "notInstalled", label: t("plugins.filterNotInstalled") }
          ]
        }
      )
    ] }),
    offline && /* @__PURE__ */ jsxRuntimeExports.jsx(Alert, { type: "warning", showIcon: true, message: t("plugins.offline") }),
    error && /* @__PURE__ */ jsxRuntimeExports.jsx(Alert, { type: "error", showIcon: true, message: error }),
    filtered.length === 0 ? /* @__PURE__ */ jsxRuntimeExports.jsx(Empty, { description: t("plugins.empty") }) : /* @__PURE__ */ jsxRuntimeExports.jsx(
      List,
      {
        loading,
        dataSource: filtered,
        renderItem: (plugin) => /* @__PURE__ */ jsxRuntimeExports.jsx(PluginCard, { plugin }, plugin.id)
      }
    )
  ] });
}
export {
  PluginExtensionsPage as default
};
