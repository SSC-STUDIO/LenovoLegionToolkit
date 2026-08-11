import { aA as getDefaultExportFromCjs, r as reactExports, aB as Icon, $ as React, bg as KeyCode, f as clsx, aE as useControlledState, a2 as pickAttrs, v as genStyleHooks, w as merge, aZ as initInputToken, a$ as genCssVar, n as unit, A as resetComponent, p as genFocusOutline, ac as genFocusStyle, aY as initComponentToken, bB as genInputLargeStyle, bC as genInputSmallStyle, bD as genDisabledStyle, bE as genBaseOutlinedStyle, b5 as genBasicInputStyle, am as genSubStyleComponent, Z as isPlainObject, bF as useBreakpoint, au as useToken, J as useComponentConfig, aL as useSize, b8 as useVariant, L as useSemanticRootStyle, N as useMergeSemantic, a8 as useLocale, bG as locale$1, bH as RefIcon$2, bI as RefIcon$3, P as RefIcon$4, e as ConfigContext, k as cloneElement, d as Col, g as toArray, bp as isString, _ as _toConsumableArray, bJ as responsiveArray, b as Row, S as Spin, l as isFunction } from "./index-3RTipSd5.js";
import { m as mergeProps } from "./index-uyL__3sF.js";
import { S as Select, D as DefaultRenderEmpty } from "./index-BxBscas6.js";
var DoubleLeftOutlined$1 = {};
var hasRequiredDoubleLeftOutlined;
function requireDoubleLeftOutlined() {
  if (hasRequiredDoubleLeftOutlined) return DoubleLeftOutlined$1;
  hasRequiredDoubleLeftOutlined = 1;
  Object.defineProperty(DoubleLeftOutlined$1, "__esModule", { value: true });
  var DoubleLeftOutlined2 = { "icon": { "tag": "svg", "attrs": { "viewBox": "64 64 896 896", "focusable": "false" }, "children": [{ "tag": "path", "attrs": { "d": "M272.9 512l265.4-339.1c4.1-5.2.4-12.9-6.3-12.9h-77.3c-4.9 0-9.6 2.3-12.6 6.1L186.8 492.3a31.99 31.99 0 000 39.5l255.3 326.1c3 3.9 7.7 6.1 12.6 6.1H532c6.7 0 10.4-7.7 6.3-12.9L272.9 512zm304 0l265.4-339.1c4.1-5.2.4-12.9-6.3-12.9h-77.3c-4.9 0-9.6 2.3-12.6 6.1L490.8 492.3a31.99 31.99 0 000 39.5l255.3 326.1c3 3.9 7.7 6.1 12.6 6.1H836c6.7 0 10.4-7.7 6.3-12.9L576.9 512z" } }] }, "name": "double-left", "theme": "outlined" };
  DoubleLeftOutlined$1.default = DoubleLeftOutlined2;
  return DoubleLeftOutlined$1;
}
var DoubleLeftOutlinedExports = /* @__PURE__ */ requireDoubleLeftOutlined();
const DoubleLeftOutlinedSvg = /* @__PURE__ */ getDefaultExportFromCjs(DoubleLeftOutlinedExports);
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
const DoubleLeftOutlined = (props, ref) => /* @__PURE__ */ reactExports.createElement(Icon, _extends$2({}, props, {
  ref,
  icon: DoubleLeftOutlinedSvg
}));
const RefIcon$1 = /* @__PURE__ */ reactExports.forwardRef(DoubleLeftOutlined);
var DoubleRightOutlined$1 = {};
var hasRequiredDoubleRightOutlined;
function requireDoubleRightOutlined() {
  if (hasRequiredDoubleRightOutlined) return DoubleRightOutlined$1;
  hasRequiredDoubleRightOutlined = 1;
  Object.defineProperty(DoubleRightOutlined$1, "__esModule", { value: true });
  var DoubleRightOutlined2 = { "icon": { "tag": "svg", "attrs": { "viewBox": "64 64 896 896", "focusable": "false" }, "children": [{ "tag": "path", "attrs": { "d": "M533.2 492.3L277.9 166.1c-3-3.9-7.7-6.1-12.6-6.1H188c-6.7 0-10.4 7.7-6.3 12.9L447.1 512 181.7 851.1A7.98 7.98 0 00188 864h77.3c4.9 0 9.6-2.3 12.6-6.1l255.3-326.1c9.1-11.7 9.1-27.9 0-39.5zm304 0L581.9 166.1c-3-3.9-7.7-6.1-12.6-6.1H492c-6.7 0-10.4 7.7-6.3 12.9L751.1 512 485.7 851.1A7.98 7.98 0 00492 864h77.3c4.9 0 9.6-2.3 12.6-6.1l255.3-326.1c9.1-11.7 9.1-27.9 0-39.5z" } }] }, "name": "double-right", "theme": "outlined" };
  DoubleRightOutlined$1.default = DoubleRightOutlined2;
  return DoubleRightOutlined$1;
}
var DoubleRightOutlinedExports = /* @__PURE__ */ requireDoubleRightOutlined();
const DoubleRightOutlinedSvg = /* @__PURE__ */ getDefaultExportFromCjs(DoubleRightOutlinedExports);
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
const DoubleRightOutlined = (props, ref) => /* @__PURE__ */ reactExports.createElement(Icon, _extends$1({}, props, {
  ref,
  icon: DoubleRightOutlinedSvg
}));
const RefIcon = /* @__PURE__ */ reactExports.forwardRef(DoubleRightOutlined);
const locale = {
  // Options
  items_per_page: "条/页",
  jump_to: "跳至",
  jump_to_confirm: "确定",
  page: "页",
  // Pagination
  prev_page: "上一页",
  next_page: "下一页",
  prev_5: "向前 5 页",
  next_5: "向后 5 页",
  prev_3: "向前 3 页",
  next_3: "向后 3 页",
  page_size: "页码"
};
const defaultPageSizeOptions = [10, 20, 50, 100];
const Options = (props) => {
  const {
    pageSizeOptions = defaultPageSizeOptions,
    locale: locale2,
    changeSize,
    pageSize,
    goButton,
    quickGo,
    rootPrefixCls,
    disabled,
    buildOptionText,
    showSizeChanger,
    sizeChangerRender
  } = props;
  const [goInputText, setGoInputText] = React.useState("");
  const getValidValue = React.useMemo(() => {
    return !goInputText || Number.isNaN(goInputText) ? void 0 : Number(goInputText);
  }, [goInputText]);
  const mergeBuildOptionText = typeof buildOptionText === "function" ? buildOptionText : (value) => `${value} ${locale2.items_per_page}`;
  const handleChange = (e) => {
    const value = e.target.value;
    if (/^\d*$/.test(value)) {
      setGoInputText(value);
    }
  };
  const handleBlur = (e) => {
    if (goButton || goInputText === "") {
      return;
    }
    setGoInputText("");
    if (e.relatedTarget && (e.relatedTarget.className.includes(`${rootPrefixCls}-item-link`) || e.relatedTarget.className.includes(`${rootPrefixCls}-item`))) {
      return;
    }
    quickGo?.(getValidValue);
  };
  const go = (e) => {
    if (goInputText === "") {
      return;
    }
    if (e.keyCode === KeyCode.ENTER || e.type === "click") {
      setGoInputText("");
      quickGo?.(getValidValue);
    }
  };
  const getPageSizeOptions = () => {
    if (pageSizeOptions.some((option) => option.toString() === pageSize.toString())) {
      return pageSizeOptions;
    }
    return pageSizeOptions.concat([pageSize]).sort((a, b) => {
      const numberA = Number.isNaN(Number(a)) ? 0 : Number(a);
      const numberB = Number.isNaN(Number(b)) ? 0 : Number(b);
      return numberA - numberB;
    });
  };
  const prefixCls = `${rootPrefixCls}-options`;
  if (!showSizeChanger && !quickGo) {
    return null;
  }
  let changeSelect = null;
  let goInput = null;
  let gotoButton = null;
  if (showSizeChanger && sizeChangerRender) {
    changeSelect = sizeChangerRender({
      disabled,
      size: pageSize,
      onSizeChange: (nextValue) => {
        changeSize?.(Number(nextValue));
      },
      "aria-label": locale2.page_size,
      className: `${prefixCls}-size-changer`,
      options: getPageSizeOptions().map((opt) => ({
        label: mergeBuildOptionText(opt),
        value: opt
      }))
    });
  }
  if (quickGo) {
    if (goButton) {
      gotoButton = typeof goButton === "boolean" ? /* @__PURE__ */ React.createElement("button", {
        type: "button",
        onClick: go,
        onKeyUp: go,
        disabled,
        className: `${prefixCls}-quick-jumper-button`
      }, locale2.jump_to_confirm) : /* @__PURE__ */ React.createElement("span", {
        onClick: go,
        onKeyUp: go
      }, goButton);
    }
    goInput = /* @__PURE__ */ React.createElement("div", {
      className: `${prefixCls}-quick-jumper`
    }, locale2.jump_to, /* @__PURE__ */ React.createElement("input", {
      disabled,
      type: "text",
      value: goInputText,
      onChange: handleChange,
      onKeyUp: go,
      onBlur: handleBlur,
      "aria-label": locale2.page
    }), locale2.page, gotoButton);
  }
  return /* @__PURE__ */ React.createElement("li", {
    className: prefixCls
  }, changeSelect, goInput);
};
const Pager = (props) => {
  const {
    rootPrefixCls,
    page,
    active,
    className,
    style,
    showTitle,
    onClick,
    onKeyPress,
    itemRender
  } = props;
  const prefixCls = `${rootPrefixCls}-item`;
  const cls = clsx(prefixCls, `${prefixCls}-${page}`, {
    [`${prefixCls}-active`]: active,
    [`${prefixCls}-disabled`]: !page
  }, className);
  const handleClick = () => {
    onClick(page);
  };
  const handleKeyPress = (e) => {
    onKeyPress(e, onClick, page);
  };
  const pager = itemRender(page, "page", /* @__PURE__ */ React.createElement("a", {
    rel: "nofollow"
  }, page));
  return pager ? /* @__PURE__ */ React.createElement("li", {
    title: showTitle ? String(page) : null,
    className: cls,
    style,
    onClick: handleClick,
    onKeyDown: handleKeyPress,
    tabIndex: 0
  }, pager) : null;
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
const defaultItemRender = (_, __, element) => element;
function noop() {
}
function isInteger(v) {
  const value = Number(v);
  return typeof value === "number" && !Number.isNaN(value) && isFinite(value) && Math.floor(value) === value;
}
function calculatePage(p, pageSize, total) {
  const _pageSize = typeof p === "undefined" ? pageSize : p;
  return Math.floor((total - 1) / _pageSize) + 1;
}
const Pagination$1 = (props) => {
  const {
    // cls
    prefixCls = "rc-pagination",
    selectPrefixCls = "rc-select",
    className,
    classNames: paginationClassNames,
    styles,
    // control
    current: currentProp,
    defaultCurrent = 1,
    total = 0,
    pageSize: pageSizeProp,
    defaultPageSize = 10,
    onChange = noop,
    // config
    hideOnSinglePage,
    align,
    showPrevNextJumpers = true,
    showQuickJumper,
    showLessItems,
    showTitle = true,
    onShowSizeChange = noop,
    locale: locale$12 = locale,
    style,
    totalBoundaryShowSizeChanger = 50,
    disabled,
    simple,
    showTotal,
    showSizeChanger = total > totalBoundaryShowSizeChanger,
    sizeChangerRender,
    pageSizeOptions,
    // render
    itemRender = defaultItemRender,
    jumpPrevIcon,
    jumpNextIcon,
    prevIcon,
    nextIcon
  } = props;
  const paginationRef = React.useRef(null);
  const [pageSize, setPageSize] = useControlledState(defaultPageSize, pageSizeProp);
  const [internalCurrent, setCurrent] = useControlledState(defaultCurrent, currentProp);
  const current = Math.max(1, Math.min(internalCurrent, calculatePage(void 0, pageSize, total)));
  const [internalInputVal, setInternalInputVal] = React.useState(current);
  reactExports.useEffect(() => {
    setInternalInputVal(current);
  }, [current]);
  const jumpPrevPage = Math.max(1, current - (showLessItems ? 3 : 5));
  const jumpNextPage = Math.min(calculatePage(void 0, pageSize, total), current + (showLessItems ? 3 : 5));
  function getItemIcon(icon, label) {
    let iconNode = icon || /* @__PURE__ */ React.createElement("button", {
      type: "button",
      "aria-label": label,
      className: `${prefixCls}-item-link`
    });
    if (typeof icon === "function") {
      iconNode = /* @__PURE__ */ React.createElement(icon, props);
    }
    return iconNode;
  }
  function getValidValue(e) {
    const inputValue = e.target.value;
    const allPages2 = calculatePage(void 0, pageSize, total);
    let value;
    if (inputValue === "") {
      value = inputValue;
    } else if (Number.isNaN(Number(inputValue))) {
      value = internalInputVal;
    } else if (inputValue >= allPages2) {
      value = allPages2;
    } else {
      value = Number(inputValue);
    }
    return value;
  }
  function isValid(page) {
    return isInteger(page) && page !== current && isInteger(total) && total > 0;
  }
  const shouldDisplayQuickJumper = total > pageSize ? showQuickJumper : false;
  function handleKeyDown(event) {
    if (event.keyCode === KeyCode.UP || event.keyCode === KeyCode.DOWN) {
      event.preventDefault();
    }
  }
  function handleKeyUp(event) {
    const value = getValidValue(event);
    if (value !== internalInputVal) {
      setInternalInputVal(value);
    }
    switch (event.keyCode) {
      case KeyCode.ENTER:
        handleChange(value);
        break;
      case KeyCode.UP:
        handleChange(value - 1);
        break;
      case KeyCode.DOWN:
        handleChange(value + 1);
        break;
    }
  }
  function handleBlur(event) {
    handleChange(getValidValue(event));
  }
  function changePageSize(size) {
    const newCurrent = calculatePage(size, pageSize, total);
    const nextCurrent = current > newCurrent && newCurrent !== 0 ? newCurrent : current;
    setPageSize(size);
    setInternalInputVal(nextCurrent);
    onShowSizeChange?.(current, size);
    setCurrent(nextCurrent);
    onChange?.(nextCurrent, size);
  }
  function handleChange(page) {
    if (isValid(page) && !disabled) {
      const currentPage = calculatePage(void 0, pageSize, total);
      let newPage = page;
      if (page > currentPage) {
        newPage = currentPage;
      } else if (page < 1) {
        newPage = 1;
      }
      if (newPage !== internalInputVal) {
        setInternalInputVal(newPage);
      }
      setCurrent(newPage);
      onChange?.(newPage, pageSize);
      return newPage;
    }
    return current;
  }
  const hasPrev = current > 1;
  const hasNext = current < calculatePage(void 0, pageSize, total);
  function prevHandle() {
    if (hasPrev) handleChange(current - 1);
  }
  function nextHandle() {
    if (hasNext) handleChange(current + 1);
  }
  function jumpPrevHandle() {
    handleChange(jumpPrevPage);
  }
  function jumpNextHandle() {
    handleChange(jumpNextPage);
  }
  function runIfEnter(event, callback, ...restParams) {
    if (event.key === "Enter" || event.charCode === KeyCode.ENTER || event.keyCode === KeyCode.ENTER) {
      callback(...restParams);
    }
  }
  function runIfEnterPrev(event) {
    runIfEnter(event, prevHandle);
  }
  function runIfEnterNext(event) {
    runIfEnter(event, nextHandle);
  }
  function runIfEnterJumpPrev(event) {
    runIfEnter(event, jumpPrevHandle);
  }
  function runIfEnterJumpNext(event) {
    runIfEnter(event, jumpNextHandle);
  }
  function renderPrev(prevPage2) {
    const prevButton = itemRender(prevPage2, "prev", getItemIcon(prevIcon, "prev page"));
    return /* @__PURE__ */ React.isValidElement(prevButton) ? /* @__PURE__ */ React.cloneElement(prevButton, {
      disabled: !hasPrev
    }) : prevButton;
  }
  function renderNext(nextPage2) {
    const nextButton = itemRender(nextPage2, "next", getItemIcon(nextIcon, "next page"));
    return /* @__PURE__ */ React.isValidElement(nextButton) ? /* @__PURE__ */ React.cloneElement(nextButton, {
      disabled: !hasNext
    }) : nextButton;
  }
  function handleGoTO(event) {
    if (event.type === "click" || event.keyCode === KeyCode.ENTER) {
      handleChange(internalInputVal);
    }
  }
  let jumpPrev = null;
  const dataOrAriaAttributeProps = pickAttrs(props, {
    aria: true,
    data: true
  });
  const totalText = showTotal && /* @__PURE__ */ React.createElement("li", {
    className: `${prefixCls}-total-text`
  }, showTotal(total, [total === 0 ? 0 : (current - 1) * pageSize + 1, current * pageSize > total ? total : current * pageSize]));
  let jumpNext = null;
  const allPages = calculatePage(void 0, pageSize, total);
  if (hideOnSinglePage && total <= pageSize) {
    return null;
  }
  const pagerList = [];
  const pagerProps = {
    rootPrefixCls: prefixCls,
    onClick: handleChange,
    onKeyPress: runIfEnter,
    showTitle,
    itemRender,
    page: -1,
    className: paginationClassNames?.item,
    style: styles?.item
  };
  const prevPage = current - 1 > 0 ? current - 1 : 0;
  const nextPage = current + 1 < allPages ? current + 1 : allPages;
  const goButton = showQuickJumper && showQuickJumper.goButton;
  const isReadOnly = typeof simple === "object" ? simple.readOnly : !simple;
  let gotoButton = goButton;
  let simplePager = null;
  if (simple) {
    if (goButton) {
      if (typeof goButton === "boolean") {
        gotoButton = /* @__PURE__ */ React.createElement("button", {
          type: "button",
          onClick: handleGoTO,
          onKeyUp: handleGoTO
        }, locale$12.jump_to_confirm);
      } else {
        gotoButton = /* @__PURE__ */ React.createElement("span", {
          onClick: handleGoTO,
          onKeyUp: handleGoTO
        }, goButton);
      }
      gotoButton = /* @__PURE__ */ React.createElement("li", {
        title: showTitle ? `${locale$12.jump_to}${current}/${allPages}` : null,
        className: `${prefixCls}-simple-pager`
      }, gotoButton);
    }
    simplePager = /* @__PURE__ */ React.createElement("li", {
      title: showTitle ? `${current}/${allPages}` : null,
      className: clsx(`${prefixCls}-simple-pager`, paginationClassNames?.item),
      style: styles?.item
    }, isReadOnly ? internalInputVal : /* @__PURE__ */ React.createElement("input", {
      type: "text",
      "aria-label": locale$12.jump_to,
      value: internalInputVal,
      disabled,
      onKeyDown: handleKeyDown,
      onKeyUp: handleKeyUp,
      onChange: handleKeyUp,
      onBlur: handleBlur,
      size: 3
    }), /* @__PURE__ */ React.createElement("span", {
      className: `${prefixCls}-slash`
    }, "/"), allPages);
  }
  const pageBufferSize = showLessItems ? 1 : 2;
  if (allPages <= 3 + pageBufferSize * 2) {
    if (!allPages) {
      pagerList.push(/* @__PURE__ */ React.createElement(Pager, _extends({}, pagerProps, {
        key: "noPager",
        page: 1,
        className: `${prefixCls}-item-disabled`
      })));
    }
    for (let i = 1; i <= allPages; i += 1) {
      pagerList.push(/* @__PURE__ */ React.createElement(Pager, _extends({}, pagerProps, {
        key: i,
        page: i,
        active: current === i
      })));
    }
  } else {
    const prevItemTitle = showLessItems ? locale$12.prev_3 : locale$12.prev_5;
    const nextItemTitle = showLessItems ? locale$12.next_3 : locale$12.next_5;
    const jumpPrevContent = itemRender(jumpPrevPage, "jump-prev", getItemIcon(jumpPrevIcon, "prev page"));
    const jumpNextContent = itemRender(jumpNextPage, "jump-next", getItemIcon(jumpNextIcon, "next page"));
    if (showPrevNextJumpers) {
      jumpPrev = jumpPrevContent ? /* @__PURE__ */ React.createElement("li", {
        title: showTitle ? prevItemTitle : null,
        key: "prev",
        onClick: jumpPrevHandle,
        tabIndex: 0,
        onKeyDown: runIfEnterJumpPrev,
        className: clsx(`${prefixCls}-jump-prev`, {
          [`${prefixCls}-jump-prev-custom-icon`]: !!jumpPrevIcon
        })
      }, jumpPrevContent) : null;
      jumpNext = jumpNextContent ? /* @__PURE__ */ React.createElement("li", {
        title: showTitle ? nextItemTitle : null,
        key: "next",
        onClick: jumpNextHandle,
        tabIndex: 0,
        onKeyDown: runIfEnterJumpNext,
        className: clsx(`${prefixCls}-jump-next`, {
          [`${prefixCls}-jump-next-custom-icon`]: !!jumpNextIcon
        })
      }, jumpNextContent) : null;
    }
    let left = Math.max(1, current - pageBufferSize);
    let right = Math.min(current + pageBufferSize, allPages);
    if (current - 1 <= pageBufferSize) {
      right = 1 + pageBufferSize * 2;
    }
    if (allPages - current <= pageBufferSize) {
      left = allPages - pageBufferSize * 2;
    }
    const hasJumpPrev = !!jumpPrev && current - 1 >= pageBufferSize * 2 && current !== 1 + 2;
    const hasJumpNext = !!jumpNext && allPages - current >= pageBufferSize * 2 && current !== allPages - 2;
    if (!showLessItems && hasJumpPrev && right !== allPages) {
      left += 1;
    }
    if (!showLessItems && hasJumpNext && left !== 1) {
      right -= 1;
    }
    for (let i = left; i <= right; i += 1) {
      pagerList.push(/* @__PURE__ */ React.createElement(Pager, _extends({}, pagerProps, {
        key: i,
        page: i,
        active: current === i
      })));
    }
    if (hasJumpPrev) {
      pagerList[0] = /* @__PURE__ */ React.cloneElement(pagerList[0], {
        className: clsx(`${prefixCls}-item-after-jump-prev`, pagerList[0].props.className)
      });
      pagerList.unshift(jumpPrev);
    }
    if (hasJumpNext) {
      const lastOne = pagerList[pagerList.length - 1];
      pagerList[pagerList.length - 1] = /* @__PURE__ */ React.cloneElement(lastOne, {
        className: clsx(`${prefixCls}-item-before-jump-next`, lastOne.props.className)
      });
      pagerList.push(jumpNext);
    }
    if (left !== 1) {
      pagerList.unshift(/* @__PURE__ */ React.createElement(Pager, _extends({}, pagerProps, {
        key: 1,
        page: 1
      })));
    }
    if (right !== allPages) {
      pagerList.push(/* @__PURE__ */ React.createElement(Pager, _extends({}, pagerProps, {
        key: allPages,
        page: allPages
      })));
    }
  }
  let prev = renderPrev(prevPage);
  if (prev) {
    const prevDisabled = !hasPrev || !allPages;
    prev = /* @__PURE__ */ React.createElement("li", {
      title: showTitle ? locale$12.prev_page : null,
      onClick: prevHandle,
      tabIndex: prevDisabled ? null : 0,
      onKeyDown: runIfEnterPrev,
      className: clsx(`${prefixCls}-prev`, paginationClassNames?.item, {
        [`${prefixCls}-disabled`]: prevDisabled
      }),
      style: styles?.item,
      "aria-disabled": prevDisabled
    }, prev);
  }
  let next = renderNext(nextPage);
  if (next) {
    let nextDisabled, nextTabIndex;
    if (simple) {
      nextDisabled = !hasNext;
      nextTabIndex = hasPrev ? 0 : null;
    } else {
      nextDisabled = !hasNext || !allPages;
      nextTabIndex = nextDisabled ? null : 0;
    }
    next = /* @__PURE__ */ React.createElement("li", {
      title: showTitle ? locale$12.next_page : null,
      onClick: nextHandle,
      tabIndex: nextTabIndex,
      onKeyDown: runIfEnterNext,
      className: clsx(`${prefixCls}-next`, paginationClassNames?.item, {
        [`${prefixCls}-disabled`]: nextDisabled
      }),
      style: styles?.item,
      "aria-disabled": nextDisabled
    }, next);
  }
  const cls = clsx(prefixCls, className, {
    [`${prefixCls}-start`]: align === "start",
    [`${prefixCls}-center`]: align === "center",
    [`${prefixCls}-end`]: align === "end",
    [`${prefixCls}-simple`]: simple,
    [`${prefixCls}-disabled`]: disabled
  });
  return /* @__PURE__ */ React.createElement("ul", _extends({
    className: cls,
    style,
    ref: paginationRef
  }, dataOrAriaAttributeProps), totalText, prev, simple ? simplePager : pagerList, next, /* @__PURE__ */ React.createElement(Options, {
    locale: locale$12,
    rootPrefixCls: prefixCls,
    disabled,
    selectPrefixCls,
    changeSize: changePageSize,
    pageSize,
    pageSizeOptions,
    quickGo: shouldDisplayQuickJumper ? handleChange : null,
    goButton: gotoButton,
    showSizeChanger,
    sizeChangerRender
  }));
};
const genPaginationDisabledStyle = (token) => {
  const {
    componentCls
  } = token;
  return {
    [`${componentCls}-disabled`]: {
      "&, &:hover": {
        cursor: "not-allowed",
        [`${componentCls}-item-link`]: {
          color: token.colorTextDisabled,
          cursor: "not-allowed"
        }
      },
      "&:focus-visible": {
        cursor: "not-allowed",
        [`${componentCls}-item-link`]: {
          color: token.colorTextDisabled,
          cursor: "not-allowed"
        }
      }
    },
    [`&${componentCls}-disabled`]: {
      cursor: "not-allowed",
      [`${componentCls}-item`]: {
        cursor: "not-allowed",
        backgroundColor: "transparent",
        "&:hover, &:active": {
          backgroundColor: "transparent"
        },
        a: {
          color: token.colorTextDisabled,
          backgroundColor: "transparent",
          border: "none",
          cursor: "not-allowed"
        },
        "&-active": {
          borderColor: token.colorBorder,
          backgroundColor: token.itemActiveBgDisabled,
          "&:hover, &:active": {
            backgroundColor: token.itemActiveBgDisabled
          },
          a: {
            color: token.itemActiveColorDisabled
          }
        }
      },
      [`${componentCls}-item-link`]: {
        color: token.colorTextDisabled,
        cursor: "not-allowed",
        "&:hover, &:active": {
          backgroundColor: "transparent"
        },
        [`${componentCls}-simple&`]: {
          backgroundColor: "transparent",
          "&:hover, &:active": {
            backgroundColor: "transparent"
          }
        }
      },
      [`${componentCls}-simple-pager`]: {
        color: token.colorTextDisabled
      },
      [`${componentCls}-jump-prev, ${componentCls}-jump-next`]: {
        [`${componentCls}-item-link-icon`]: {
          opacity: 0
        },
        [`${componentCls}-item-ellipsis`]: {
          opacity: 1
        }
      }
    }
  };
};
const genPaginationSmallStyle = (token) => {
  const {
    componentCls
  } = token;
  return {
    [`&${componentCls}-small ${componentCls}-options`]: {
      marginInlineStart: token.paginationMiniOptionsMarginInlineStart,
      "&-quick-jumper": {
        input: {
          ...genInputSmallStyle(token),
          width: token.paginationMiniQuickJumperInputWidth
        }
      }
    }
  };
};
const genPaginationLargeStyle = (token) => {
  const {
    componentCls
  } = token;
  return {
    [`&${componentCls}-large ${componentCls}-options`]: {
      "&-quick-jumper": {
        input: {
          ...genInputLargeStyle(token)
        }
      }
    }
  };
};
const genPaginationSimpleStyle = (token) => {
  const {
    componentCls,
    antCls
  } = token;
  const [, varRef] = genCssVar(antCls, "pagination");
  return {
    [`&${componentCls}-simple`]: {
      [`${componentCls}-prev, ${componentCls}-next`]: {
        height: varRef(`item-size-actual`),
        lineHeight: varRef(`item-size-actual`),
        verticalAlign: "top",
        [`${componentCls}-item-link`]: {
          height: varRef(`item-size-actual`),
          backgroundColor: "transparent",
          border: 0,
          "&:hover": {
            backgroundColor: token.colorBgTextHover
          },
          "&:active": {
            backgroundColor: token.colorBgTextActive
          },
          "&::after": {
            height: varRef(`item-size-actual`),
            lineHeight: varRef(`item-size-actual`)
          }
        }
      },
      [`${componentCls}-simple-pager`]: {
        display: "inline-flex",
        alignItems: "center",
        height: varRef(`item-size-actual`),
        marginInlineEnd: varRef(`item-spacing-actual`),
        input: {
          boxSizing: "border-box",
          height: "100%",
          width: token.quickJumperInputWidth,
          padding: `0 ${unit(token.paginationItemPaddingInline)}`,
          textAlign: "center",
          backgroundColor: token.itemInputBg,
          border: `${unit(token.lineWidth)} ${token.lineType} ${token.colorBorder}`,
          borderRadius: token.borderRadius,
          outline: "none",
          transition: `border-color ${token.motionDurationMid}`,
          color: "inherit",
          "&:hover": {
            borderColor: token.colorPrimary
          },
          "&:focus": {
            borderColor: token.colorPrimaryHover,
            boxShadow: `${unit(token.inputOutlineOffset)} 0 ${unit(token.controlOutlineWidth)} ${token.controlOutline}`
          },
          "&[disabled]": {
            color: token.colorTextDisabled,
            backgroundColor: token.colorBgContainerDisabled,
            borderColor: token.colorBorder,
            cursor: "not-allowed"
          }
        }
      },
      [`&${componentCls}-disabled`]: {
        [`${componentCls}-prev, ${componentCls}-next`]: {
          [`${componentCls}-item-link`]: {
            "&:hover, &:active": {
              backgroundColor: "transparent"
            }
          }
        }
      },
      [`&${componentCls}-small`]: {
        [`${componentCls}-simple-pager`]: {
          input: {
            width: token.paginationMiniQuickJumperInputWidth
          }
        }
      }
    }
  };
};
const genPaginationInputVariantStyle = (token) => {
  const {
    componentCls
  } = token;
  const inputSelector = `${componentCls}-options-quick-jumper input, ${componentCls}-simple-pager input`;
  return {
    [`&${componentCls}-filled`]: {
      [inputSelector]: {
        background: token.colorFillTertiary,
        borderColor: "transparent",
        "&:hover": {
          background: token.colorFillSecondary
        },
        "&:focus": {
          borderColor: token.activeBorderColor,
          outline: 0,
          backgroundColor: token.activeBg
        },
        "&[disabled]": {
          ...genDisabledStyle(token)
        }
      }
    },
    [`&${componentCls}-borderless`]: {
      [inputSelector]: {
        background: "transparent",
        border: "none",
        "&:focus": {
          outline: "none",
          boxShadow: "none"
        },
        "&[disabled]": {
          color: token.colorTextDisabled,
          cursor: "not-allowed"
        }
      }
    },
    [`&${componentCls}-underlined`]: {
      [inputSelector]: {
        background: token.colorBgContainer,
        borderWidth: `${unit(token.lineWidth)} 0`,
        borderStyle: `${token.lineType} none`,
        borderColor: `transparent transparent ${token.colorBorder} transparent`,
        borderRadius: 0,
        "&:hover": {
          borderColor: `transparent transparent ${token.hoverBorderColor} transparent`,
          backgroundColor: token.hoverBg
        },
        "&:focus": {
          borderColor: `transparent transparent ${token.activeBorderColor} transparent`,
          outline: 0,
          backgroundColor: token.activeBg
        },
        "&[disabled]": {
          color: token.colorTextDisabled,
          boxShadow: "none",
          cursor: "not-allowed"
        }
      }
    }
  };
};
const genPaginationJumpStyle = (token) => {
  const {
    componentCls,
    iconCls,
    sizeLG,
    antCls
  } = token;
  const [, varRef] = genCssVar(antCls, "pagination");
  return {
    [`${componentCls}-jump-prev, ${componentCls}-jump-next`]: {
      outline: 0,
      [`${componentCls}-item-container`]: {
        position: "relative",
        [`${componentCls}-item-link-icon`]: {
          color: token.colorPrimary,
          fontSize: token.fontSizeSM,
          opacity: 0,
          transition: `all ${token.motionDurationMid}`,
          "&-svg": {
            top: 0,
            insetInlineEnd: 0,
            bottom: 0,
            insetInlineStart: 0,
            margin: "auto"
          }
        },
        [`${componentCls}-item-ellipsis`]: {
          position: "absolute",
          inset: 0,
          display: "inline-flex",
          justifyContent: "center",
          alignItems: "center",
          margin: "auto",
          color: token.colorTextDisabled,
          textAlign: "center",
          opacity: 1,
          transition: `all ${token.motionDurationMid}`,
          [`${iconCls}-ellipsis > svg`]: {
            width: sizeLG,
            height: sizeLG
          }
        }
      },
      "&:hover": {
        [`${componentCls}-item-link-icon`]: {
          opacity: 1
        },
        [`${componentCls}-item-ellipsis`]: {
          opacity: 0
        }
      }
    },
    [`
    ${componentCls}-prev,
    ${componentCls}-jump-prev,
    ${componentCls}-jump-next
    `]: {
      marginInlineEnd: varRef(`item-spacing-actual`)
    },
    [`
    ${componentCls}-prev,
    ${componentCls}-next,
    ${componentCls}-jump-prev,
    ${componentCls}-jump-next
    `]: {
      display: "inline-block",
      minWidth: varRef(`item-size-actual`),
      height: varRef(`item-size-actual`),
      color: token.colorText,
      fontFamily: token.fontFamily,
      lineHeight: varRef(`item-size-actual`),
      textAlign: "center",
      verticalAlign: "middle",
      listStyle: "none",
      borderRadius: token.borderRadius,
      cursor: "pointer",
      transition: `all ${token.motionDurationMid}`
    },
    [`${componentCls}-prev, ${componentCls}-next`]: {
      outline: 0,
      button: {
        color: token.colorText,
        cursor: "pointer",
        userSelect: "none"
      },
      [`${componentCls}-item-link`]: {
        display: "block",
        width: "100%",
        height: "100%",
        padding: 0,
        fontSize: token.fontSizeSM,
        textAlign: "center",
        backgroundColor: "transparent",
        border: `${unit(token.lineWidth)} ${token.lineType} transparent`,
        borderRadius: token.borderRadius,
        outline: "none",
        transition: `all ${token.motionDurationMid}`
      },
      [`&:hover ${componentCls}-item-link`]: {
        backgroundColor: token.colorBgTextHover
      },
      [`&:active ${componentCls}-item-link`]: {
        backgroundColor: token.colorBgTextActive
      },
      [`&${componentCls}-disabled:hover`]: {
        [`${componentCls}-item-link`]: {
          backgroundColor: "transparent"
        }
      }
    },
    [`${componentCls}-slash`]: {
      marginInlineEnd: token.paginationSlashMarginInlineEnd,
      marginInlineStart: token.paginationSlashMarginInlineStart
    },
    [`${componentCls}-options`]: {
      display: "inline-block",
      marginInlineStart: token.margin,
      verticalAlign: "middle",
      [`&-size-changer, &-size-changer${componentCls}-options-size-changer-select`]: {
        width: "auto"
      },
      "&-quick-jumper": {
        display: "inline-block",
        height: varRef(`item-size-actual`),
        marginInlineStart: token.marginXS,
        lineHeight: varRef(`item-size-actual`),
        verticalAlign: "baseline",
        input: {
          ...genBasicInputStyle(token),
          ...genBaseOutlinedStyle(token, {
            borderColor: token.colorBorder,
            hoverBorderColor: token.colorPrimaryHover,
            activeBorderColor: token.colorPrimary,
            activeShadow: token.activeShadow
          }),
          "&[disabled]": {
            ...genDisabledStyle(token)
          },
          width: token.quickJumperInputWidth,
          height: varRef(`item-size-actual`),
          boxSizing: "border-box",
          margin: 0,
          marginInlineStart: varRef(`item-spacing-actual`),
          marginInlineEnd: varRef(`item-spacing-actual`)
        }
      }
    }
  };
};
const genPaginationItemStyle = (token) => {
  const {
    componentCls,
    antCls
  } = token;
  const [, varRef] = genCssVar(antCls, "pagination");
  return {
    [`${componentCls}-item`]: {
      display: "inline-block",
      minWidth: varRef(`item-size-actual`),
      height: varRef(`item-size-actual`),
      marginInlineEnd: varRef(`item-spacing-actual`),
      fontFamily: token.fontFamily,
      lineHeight: unit(token.calc(varRef("item-size-actual")).sub(2).equal()),
      textAlign: "center",
      verticalAlign: "middle",
      listStyle: "none",
      backgroundColor: token.itemBg,
      border: `${unit(token.lineWidth)} ${token.lineType} transparent`,
      borderRadius: token.borderRadius,
      outline: 0,
      cursor: "pointer",
      userSelect: "none",
      a: {
        display: "block",
        padding: `0 ${unit(token.paginationItemPaddingInline)}`,
        color: token.colorText,
        "&:hover": {
          textDecoration: "none"
        }
      },
      [`&:not(${componentCls}-item-active)`]: {
        "&:hover": {
          transition: `all ${token.motionDurationMid}`,
          backgroundColor: token.colorBgTextHover
        },
        "&:active": {
          backgroundColor: token.colorBgTextActive
        }
      },
      "&-active": {
        fontWeight: token.fontWeightStrong,
        backgroundColor: token.itemActiveBg,
        borderColor: token.colorPrimary,
        a: {
          color: token.itemActiveColor
        },
        "&:hover": {
          borderColor: token.colorPrimaryHover
        },
        "&:hover a": {
          color: token.itemActiveColorHover
        }
      }
    }
  };
};
const genPaginationStyle = (token) => {
  const {
    componentCls,
    antCls
  } = token;
  const [varName, varRef] = genCssVar(antCls, "pagination");
  return {
    [componentCls]: {
      [varName(`item-size-actual`)]: unit(token.itemSize),
      [varName(`item-spacing-actual`)]: unit(token.marginXS),
      "&-small": {
        [varName(`item-size-actual`)]: unit(token.itemSizeSM),
        [varName(`item-spacing-actual`)]: unit(token.marginXXS)
      },
      "&-large": {
        [varName(`item-size-actual`)]: unit(token.itemSizeLG),
        [varName(`item-spacing-actual`)]: unit(token.marginSM)
      },
      ...resetComponent(token),
      display: "flex",
      alignItems: "center",
      "&-start": {
        justifyContent: "start"
      },
      "&-center": {
        justifyContent: "center"
      },
      "&-end": {
        justifyContent: "end"
      },
      "ul, ol": {
        margin: 0,
        padding: 0,
        listStyle: "none"
      },
      "&::after": {
        display: "block",
        clear: "both",
        height: 0,
        overflow: "hidden",
        visibility: "hidden",
        content: '""'
      },
      [`${componentCls}-total-text`]: {
        display: "inline-block",
        height: varRef(`item-size-actual`),
        marginInlineEnd: varRef(`item-spacing-actual`),
        lineHeight: unit(token.calc(varRef(`item-size-actual`)).sub(2).equal()),
        verticalAlign: "middle"
      },
      // item style
      ...genPaginationItemStyle(token),
      // jump btn style
      ...genPaginationJumpStyle(token),
      // simple style
      ...genPaginationSimpleStyle(token),
      // input variant style
      ...genPaginationInputVariantStyle(token),
      // size style
      ...genPaginationSmallStyle(token),
      ...genPaginationLargeStyle(token),
      // disabled style
      ...genPaginationDisabledStyle(token),
      // media query style
      [`@media only screen and (max-width: ${token.screenLG}px)`]: {
        [`${componentCls}-item`]: {
          "&-after-jump-prev, &-before-jump-next": {
            display: "none"
          }
        }
      },
      [`@media only screen and (max-width: ${token.screenSM}px)`]: {
        [`${componentCls}-options`]: {
          display: "none"
        }
      }
    },
    // rtl style
    [`&${token.componentCls}-rtl`]: {
      direction: "rtl"
    }
  };
};
const genPaginationFocusStyle = (token) => {
  const {
    componentCls
  } = token;
  return {
    [`${componentCls}:not(${componentCls}-disabled)`]: {
      [`${componentCls}-item`]: {
        ...genFocusStyle(token)
      },
      [`${componentCls}-jump-prev, ${componentCls}-jump-next`]: {
        "&:focus-visible": {
          [`${componentCls}-item-link-icon`]: {
            opacity: 1
          },
          [`${componentCls}-item-ellipsis`]: {
            opacity: 0
          },
          ...genFocusOutline(token)
        }
      },
      [`${componentCls}-prev, ${componentCls}-next`]: {
        [`&:focus-visible ${componentCls}-item-link`]: genFocusOutline(token)
      }
    }
  };
};
const prepareComponentToken$1 = (token) => ({
  itemBg: token.colorBgContainer,
  itemSize: token.controlHeight,
  itemSizeSM: token.controlHeightSM,
  itemSizeLG: token.controlHeightLG,
  itemActiveBg: token.colorBgContainer,
  itemActiveColor: token.colorPrimary,
  itemActiveColorHover: token.colorPrimaryHover,
  itemLinkBg: token.colorBgContainer,
  itemActiveColorDisabled: token.colorTextDisabled,
  itemActiveBgDisabled: token.controlItemBgActiveDisabled,
  itemInputBg: token.colorBgContainer,
  miniOptionsSizeChangerTop: 0,
  ...initComponentToken(token)
});
const prepareToken = (token) => merge(token, {
  inputOutlineOffset: 0,
  quickJumperInputWidth: token.calc(token.controlHeightLG).mul(1.25).equal(),
  paginationMiniOptionsMarginInlineStart: token.calc(token.marginXXS).div(2).equal(),
  paginationMiniQuickJumperInputWidth: token.calc(token.controlHeightLG).mul(1.1).equal(),
  paginationItemPaddingInline: token.calc(token.marginXXS).mul(1.5).equal(),
  paginationEllipsisLetterSpacing: token.calc(token.marginXXS).div(2).equal(),
  paginationSlashMarginInlineStart: token.marginSM,
  paginationSlashMarginInlineEnd: token.marginSM,
  paginationEllipsisTextIndent: "0.13em"
  // magic for ui experience
}, initInputToken(token));
const useStyle$1 = genStyleHooks("Pagination", (token) => {
  const paginationToken = prepareToken(token);
  return [genPaginationStyle(paginationToken), genPaginationFocusStyle(paginationToken)];
}, prepareComponentToken$1);
const genBorderedStyle$1 = (token) => {
  const {
    componentCls
  } = token;
  return {
    [`${componentCls}${componentCls}-bordered${componentCls}-disabled`]: {
      "&, &:hover": {
        [`${componentCls}-item-link`]: {
          borderColor: token.colorBorder
        }
      },
      "&:focus-visible": {
        [`${componentCls}-item-link`]: {
          borderColor: token.colorBorder
        }
      },
      [`${componentCls}-item, ${componentCls}-item-link`]: {
        backgroundColor: token.colorBgContainerDisabled,
        borderColor: token.colorBorder,
        [`&:hover:not(${componentCls}-item-active)`]: {
          backgroundColor: token.colorBgContainerDisabled,
          borderColor: token.colorBorder,
          a: {
            color: token.colorTextDisabled
          }
        },
        [`&${componentCls}-item-active`]: {
          backgroundColor: token.itemActiveBgDisabled
        }
      },
      [`${componentCls}-prev, ${componentCls}-next`]: {
        "&:hover button": {
          backgroundColor: token.colorBgContainerDisabled,
          borderColor: token.colorBorder,
          color: token.colorTextDisabled
        },
        [`${componentCls}-item-link`]: {
          backgroundColor: token.colorBgContainerDisabled,
          borderColor: token.colorBorder
        }
      }
    },
    [`${componentCls}${componentCls}-bordered`]: {
      [`${componentCls}-prev, ${componentCls}-next`]: {
        "&:hover button": {
          borderColor: token.colorPrimaryHover,
          backgroundColor: token.itemBg
        },
        [`${componentCls}-item-link`]: {
          backgroundColor: token.itemLinkBg,
          borderColor: token.colorBorder
        },
        [`&:hover ${componentCls}-item-link`]: {
          borderColor: token.colorPrimary,
          backgroundColor: token.itemBg,
          color: token.colorPrimary
        },
        [`&${componentCls}-disabled`]: {
          [`${componentCls}-item-link`]: {
            borderColor: token.colorBorder,
            color: token.colorTextDisabled
          }
        }
      },
      [`${componentCls}-item`]: {
        backgroundColor: token.itemBg,
        border: `${unit(token.lineWidth)} ${token.lineType} ${token.colorBorder}`,
        [`&:hover:not(${componentCls}-item-active)`]: {
          borderColor: token.colorPrimary,
          backgroundColor: token.itemBg,
          a: {
            color: token.colorPrimary
          }
        },
        "&-active": {
          borderColor: token.colorPrimary
        }
      }
    }
  };
};
const BorderedStyle = genSubStyleComponent(["Pagination", "bordered"], (token) => {
  const paginationToken = prepareToken(token);
  return genBorderedStyle$1(paginationToken);
}, prepareComponentToken$1);
function useShowSizeChanger(showSizeChanger) {
  return reactExports.useMemo(() => {
    if (typeof showSizeChanger === "boolean") {
      return [showSizeChanger, {}];
    }
    if (isPlainObject(showSizeChanger)) {
      return [true, showSizeChanger];
    }
    return [void 0, void 0];
  }, [showSizeChanger]);
}
const Pagination = (props) => {
  const {
    align,
    prefixCls: customizePrefixCls,
    selectPrefixCls: customizeSelectPrefixCls,
    className,
    rootClassName,
    style,
    size: customizeSize,
    locale: customLocale,
    responsive,
    showSizeChanger,
    components,
    selectComponentClass,
    pageSizeOptions,
    styles,
    classNames,
    ...restProps
  } = props;
  const {
    xs
  } = useBreakpoint(responsive);
  const [, token] = useToken();
  const {
    getPrefixCls,
    direction,
    showSizeChanger: contextShowSizeChangerConfig,
    className: contextClassName,
    style: contextStyle,
    classNames: contextClassNames,
    styles: contextStyles,
    totalBoundaryShowSizeChanger: contextTotalBoundaryShowSizeChanger
  } = useComponentConfig("pagination");
  const prefixCls = getPrefixCls("pagination", customizePrefixCls);
  const [hashId, cssVarCls] = useStyle$1(prefixCls);
  const mergedSize = useSize(customizeSize);
  const isSmall = mergedSize === "small" || !!(xs && !mergedSize && responsive);
  const [inputVariant, enableInputVariantCls] = useVariant("input");
  const mergedProps = {
    ...props,
    size: mergedSize
  };
  const contextStyleRoot = useSemanticRootStyle(contextStyle);
  const styleRoot = useSemanticRootStyle(style);
  const [mergedClassNames, mergedStyles] = useMergeSemantic([contextClassNames, classNames], [contextStyles, contextStyleRoot, styles, styleRoot], {
    props: mergedProps
  });
  const [contextLocale] = useLocale("Pagination", locale$1);
  const locale2 = {
    ...contextLocale,
    ...customLocale
  };
  const [propShowSizeChanger, propSizeChangerSelectProps] = useShowSizeChanger(showSizeChanger);
  const [contextShowSizeChanger, contextSizeChangerSelectProps] = useShowSizeChanger(contextShowSizeChangerConfig);
  const mergedShowSizeChanger = propShowSizeChanger ?? contextShowSizeChanger;
  const mergedShowSizeChangerSelectProps = propSizeChangerSelectProps ?? contextSizeChangerSelectProps;
  const SizeChanger = selectComponentClass || Select;
  const mergedPageSizeOptions = reactExports.useMemo(() => {
    return pageSizeOptions ? pageSizeOptions.map(Number) : void 0;
  }, [pageSizeOptions]);
  const sizeChangerRender = (info) => {
    const {
      disabled,
      size: pageSize,
      onSizeChange,
      "aria-label": ariaLabel,
      className: sizeChangerClassName,
      options
    } = info;
    const SizeChangerComponent = components?.sizeChanger;
    if (SizeChangerComponent) {
      return /* @__PURE__ */ reactExports.createElement(SizeChangerComponent, {
        value: pageSize,
        onChange: onSizeChange,
        disabled: !!disabled,
        className: sizeChangerClassName
      });
    }
    const {
      className: propSizeChangerClassName,
      onChange: propSizeChangerOnChange
    } = mergedShowSizeChangerSelectProps || {};
    const selectedValue = options.find((option) => String(option.value) === String(pageSize))?.value;
    return /* @__PURE__ */ reactExports.createElement(SizeChanger, {
      disabled,
      showSearch: true,
      popupMatchSelectWidth: false,
      getPopupContainer: (triggerNode) => triggerNode.parentNode,
      "aria-label": ariaLabel,
      options,
      ...mergedShowSizeChangerSelectProps,
      value: selectedValue,
      onChange: (nextSize, option) => {
        onSizeChange?.(nextSize);
        propSizeChangerOnChange?.(nextSize, option);
      },
      size: mergedSize,
      className: clsx(`${prefixCls}-options-size-changer-select`, sizeChangerClassName, propSizeChangerClassName)
    });
  };
  const iconsProps = reactExports.useMemo(() => {
    const ellipsis = /* @__PURE__ */ reactExports.createElement("span", {
      className: `${prefixCls}-item-ellipsis`
    }, /* @__PURE__ */ reactExports.createElement(RefIcon$4, null));
    const prevIcon = /* @__PURE__ */ reactExports.createElement("button", {
      className: `${prefixCls}-item-link`,
      type: "button",
      tabIndex: -1
    }, direction === "rtl" ? /* @__PURE__ */ reactExports.createElement(RefIcon$3, null) : /* @__PURE__ */ reactExports.createElement(RefIcon$2, null));
    const nextIcon = /* @__PURE__ */ reactExports.createElement("button", {
      className: `${prefixCls}-item-link`,
      type: "button",
      tabIndex: -1
    }, direction === "rtl" ? /* @__PURE__ */ reactExports.createElement(RefIcon$2, null) : /* @__PURE__ */ reactExports.createElement(RefIcon$3, null));
    const jumpPrevIcon = /* @__PURE__ */ reactExports.createElement("a", {
      className: `${prefixCls}-item-link`
    }, /* @__PURE__ */ reactExports.createElement("div", {
      className: `${prefixCls}-item-container`
    }, direction === "rtl" ? /* @__PURE__ */ reactExports.createElement(RefIcon, {
      className: `${prefixCls}-item-link-icon`
    }) : /* @__PURE__ */ reactExports.createElement(RefIcon$1, {
      className: `${prefixCls}-item-link-icon`
    }), ellipsis));
    const jumpNextIcon = /* @__PURE__ */ reactExports.createElement("a", {
      className: `${prefixCls}-item-link`
    }, /* @__PURE__ */ reactExports.createElement("div", {
      className: `${prefixCls}-item-container`
    }, direction === "rtl" ? /* @__PURE__ */ reactExports.createElement(RefIcon$1, {
      className: `${prefixCls}-item-link-icon`
    }) : /* @__PURE__ */ reactExports.createElement(RefIcon, {
      className: `${prefixCls}-item-link-icon`
    }), ellipsis));
    return {
      prevIcon,
      nextIcon,
      jumpPrevIcon,
      jumpNextIcon
    };
  }, [direction, prefixCls]);
  const selectPrefixCls = getPrefixCls("select", customizeSelectPrefixCls);
  const extendedClassName = clsx({
    [`${prefixCls}-${align}`]: !!align,
    [`${prefixCls}-${mergedSize}`]: mergedSize,
    [`${prefixCls}-${inputVariant}`]: enableInputVariantCls && inputVariant !== "outlined",
    /** @deprecated Should be removed in v7 */
    [`${prefixCls}-mini`]: isSmall,
    [`${prefixCls}-rtl`]: direction === "rtl",
    [`${prefixCls}-bordered`]: token.wireframe
  }, contextClassName, className, rootClassName, mergedClassNames.root, hashId, cssVarCls);
  const mergedStyle = {
    ...mergedStyles.root
  };
  return /* @__PURE__ */ reactExports.createElement(reactExports.Fragment, null, token.wireframe && /* @__PURE__ */ reactExports.createElement(BorderedStyle, {
    prefixCls
  }), /* @__PURE__ */ reactExports.createElement(Pagination$1, {
    ...iconsProps,
    ...restProps,
    styles: mergedStyles,
    classNames: mergedClassNames,
    style: mergedStyle,
    prefixCls,
    selectPrefixCls,
    className: extendedClassName,
    locale: locale2,
    pageSizeOptions: mergedPageSizeOptions,
    showSizeChanger: mergedShowSizeChanger,
    totalBoundaryShowSizeChanger: restProps.totalBoundaryShowSizeChanger ?? contextTotalBoundaryShowSizeChanger,
    sizeChangerRender
  }));
};
const ListContext = /* @__PURE__ */ React.createContext({});
ListContext.Consumer;
const Meta = /* @__PURE__ */ React.forwardRef((props, ref) => {
  const {
    prefixCls: customizePrefixCls,
    className,
    avatar,
    title,
    description,
    ...others
  } = props;
  const {
    getPrefixCls
  } = reactExports.useContext(ConfigContext);
  const prefixCls = getPrefixCls("list", customizePrefixCls);
  const classString = clsx(`${prefixCls}-item-meta`, className);
  const nativeElementRef = React.useRef(null);
  React.useImperativeHandle(ref, () => ({
    nativeElement: nativeElementRef.current
  }));
  const content = /* @__PURE__ */ React.createElement("div", {
    className: `${prefixCls}-item-meta-content`
  }, title && /* @__PURE__ */ React.createElement("h4", {
    className: `${prefixCls}-item-meta-title`
  }, title), description && /* @__PURE__ */ React.createElement("div", {
    className: `${prefixCls}-item-meta-description`
  }, description));
  return /* @__PURE__ */ React.createElement("div", {
    ref: nativeElementRef,
    ...others,
    className: classString
  }, avatar && /* @__PURE__ */ React.createElement("div", {
    className: `${prefixCls}-item-meta-avatar`
  }, avatar), (title || description) && content);
});
const InternalItem = /* @__PURE__ */ React.forwardRef((props, ref) => {
  const {
    prefixCls: customizePrefixCls,
    children,
    actions,
    extra,
    styles,
    className,
    classNames: customizeClassNames,
    colStyle,
    ...others
  } = props;
  const {
    grid,
    itemLayout
  } = reactExports.useContext(ListContext);
  const {
    getPrefixCls,
    list
  } = reactExports.useContext(ConfigContext);
  const moduleClass = (moduleName) => clsx(list?.item?.classNames?.[moduleName], customizeClassNames?.[moduleName]);
  const moduleStyle = (moduleName) => ({
    ...list?.item?.styles?.[moduleName],
    ...styles?.[moduleName]
  });
  const isItemContainsTextNodeAndNotSingular = () => {
    const childNodes = toArray(children);
    const hasTextNode = childNodes.some(isString);
    return hasTextNode && childNodes.length > 1;
  };
  const isFlexMode = () => {
    if (itemLayout === "vertical") {
      return !!extra;
    }
    return !isItemContainsTextNodeAndNotSingular();
  };
  const prefixCls = getPrefixCls("list", customizePrefixCls);
  const actionsContent = actions && actions.length > 0 && /* @__PURE__ */ React.createElement("ul", {
    className: clsx(`${prefixCls}-item-action`, moduleClass("actions")),
    key: "actions",
    style: moduleStyle("actions")
  }, actions.map((action, i) => /* @__PURE__ */ React.createElement("li", {
    key: `${prefixCls}-item-action-${i}`
  }, action, i !== actions.length - 1 && /* @__PURE__ */ React.createElement("em", {
    className: `${prefixCls}-item-action-split`
  }))));
  const Element = grid ? "div" : "li";
  const itemChildren = /* @__PURE__ */ React.createElement(Element, {
    ...others,
    ...!grid ? {
      ref
    } : {},
    className: clsx(`${prefixCls}-item`, {
      [`${prefixCls}-item-no-flex`]: !isFlexMode()
    }, className)
  }, itemLayout === "vertical" && extra ? [/* @__PURE__ */ React.createElement("div", {
    className: `${prefixCls}-item-main`,
    key: "content"
  }, children, actionsContent), /* @__PURE__ */ React.createElement("div", {
    className: clsx(`${prefixCls}-item-extra`, moduleClass("extra")),
    key: "extra",
    style: moduleStyle("extra")
  }, extra)] : [children, actionsContent, cloneElement(extra, {
    key: "extra"
  })]);
  return grid ? /* @__PURE__ */ React.createElement(Col, {
    ref,
    flex: 1,
    style: colStyle
  }, itemChildren) : itemChildren;
});
const Item = InternalItem;
Item.Meta = Meta;
const genBorderedStyle = (token) => {
  const {
    listBorderedCls,
    componentCls,
    paddingLG,
    margin,
    itemPaddingSM,
    itemPaddingLG,
    marginLG,
    borderRadiusLG
  } = token;
  const innerCornerBorderRadius = unit(token.calc(borderRadiusLG).sub(token.lineWidth).equal());
  return {
    [listBorderedCls]: {
      border: `${unit(token.lineWidth)} ${token.lineType} ${token.colorBorder}`,
      borderRadius: borderRadiusLG,
      [`${componentCls}-header`]: {
        borderRadius: `${innerCornerBorderRadius} ${innerCornerBorderRadius} 0 0`
      },
      [`${componentCls}-footer`]: {
        borderRadius: `0 0 ${innerCornerBorderRadius} ${innerCornerBorderRadius}`
      },
      [`${componentCls}-header,${componentCls}-footer,${componentCls}-item`]: {
        paddingInline: paddingLG
      },
      [`${componentCls}-pagination`]: {
        margin: `${unit(margin)} ${unit(marginLG)}`
      }
    },
    [`${listBorderedCls}${componentCls}-sm`]: {
      [`${componentCls}-item,${componentCls}-header,${componentCls}-footer`]: {
        padding: itemPaddingSM
      }
    },
    [`${listBorderedCls}${componentCls}-lg`]: {
      [`${componentCls}-item,${componentCls}-header,${componentCls}-footer`]: {
        padding: itemPaddingLG
      }
    }
  };
};
const genResponsiveStyle = (token) => {
  const {
    componentCls,
    screenSM,
    screenMD,
    marginLG,
    marginSM,
    margin
  } = token;
  return {
    [`@media screen and (max-width:${screenMD}px)`]: {
      [componentCls]: {
        [`${componentCls}-item`]: {
          [`${componentCls}-item-action`]: {
            marginInlineStart: marginLG
          }
        }
      },
      [`${componentCls}-vertical`]: {
        [`${componentCls}-item`]: {
          [`${componentCls}-item-extra`]: {
            marginInlineStart: marginLG
          }
        }
      }
    },
    [`@media screen and (max-width: ${screenSM}px)`]: {
      [componentCls]: {
        [`${componentCls}-item`]: {
          flexWrap: "wrap",
          [`${componentCls}-action`]: {
            marginInlineStart: marginSM
          }
        }
      },
      [`${componentCls}-vertical`]: {
        [`${componentCls}-item`]: {
          flexWrap: "wrap-reverse",
          [`${componentCls}-item-main`]: {
            minWidth: token.contentWidth
          },
          [`${componentCls}-item-extra`]: {
            margin: `auto auto ${unit(margin)}`
          }
        }
      }
    }
  };
};
const genBaseStyle = (token) => {
  const {
    componentCls,
    antCls,
    controlHeight,
    minHeight,
    paddingSM,
    marginLG,
    padding,
    itemPadding,
    colorPrimary,
    itemPaddingSM,
    itemPaddingLG,
    paddingXS,
    margin,
    colorText,
    colorTextDescription,
    motionDurationSlow,
    lineWidth,
    headerBg,
    footerBg,
    emptyTextPadding,
    metaMarginBottom,
    avatarMarginRight,
    titleMarginBottom,
    descriptionFontSize
  } = token;
  return {
    [componentCls]: {
      ...resetComponent(token),
      position: "relative",
      // fix https://github.com/ant-design/ant-design/issues/46177
      ["--rc-virtual-list-scrollbar-bg"]: token.colorSplit,
      "*": {
        outline: "none"
      },
      [`${componentCls}-header`]: {
        background: headerBg
      },
      [`${componentCls}-footer`]: {
        background: footerBg
      },
      [`${componentCls}-header, ${componentCls}-footer`]: {
        paddingBlock: paddingSM
      },
      [`${componentCls}-pagination`]: {
        marginBlockStart: marginLG,
        // https://github.com/ant-design/ant-design/issues/20037
        [`${antCls}-pagination-options`]: {
          textAlign: "start"
        }
      },
      [`${componentCls}-spin`]: {
        minHeight,
        textAlign: "center"
      },
      [`${componentCls}-items`]: {
        margin: 0,
        padding: 0,
        listStyle: "none"
      },
      [`${componentCls}-item`]: {
        display: "flex",
        alignItems: "center",
        justifyContent: "space-between",
        padding: itemPadding,
        color: colorText,
        [`${componentCls}-item-meta`]: {
          display: "flex",
          flex: 1,
          alignItems: "flex-start",
          maxWidth: "100%",
          [`${componentCls}-item-meta-avatar`]: {
            marginInlineEnd: avatarMarginRight
          },
          [`${componentCls}-item-meta-content`]: {
            flex: "1 0",
            width: 0,
            color: colorText
          },
          [`${componentCls}-item-meta-title`]: {
            margin: `0 0 ${unit(token.marginXXS)} 0`,
            color: colorText,
            fontSize: token.fontSize,
            lineHeight: token.lineHeight,
            "> a": {
              color: colorText,
              transition: `all ${motionDurationSlow}`,
              "&:hover": {
                color: colorPrimary
              }
            }
          },
          [`${componentCls}-item-meta-description`]: {
            color: colorTextDescription,
            fontSize: descriptionFontSize,
            lineHeight: token.lineHeight
          }
        },
        [`${componentCls}-item-action`]: {
          flex: "0 0 auto",
          marginInlineStart: token.marginXXL,
          padding: 0,
          fontSize: 0,
          listStyle: "none",
          "& > li": {
            position: "relative",
            display: "inline-block",
            padding: `0 ${unit(paddingXS)}`,
            color: colorTextDescription,
            fontSize: token.fontSize,
            lineHeight: token.lineHeight,
            textAlign: "center",
            "&:first-child": {
              paddingInlineStart: 0
            }
          },
          [`${componentCls}-item-action-split`]: {
            position: "absolute",
            insetBlockStart: "50%",
            insetInlineEnd: 0,
            width: lineWidth,
            height: token.calc(token.fontHeight).sub(token.calc(token.marginXXS).mul(2)).equal(),
            transform: "translateY(-50%)",
            backgroundColor: token.colorSplit
          }
        }
      },
      [`${componentCls}-empty`]: {
        padding: `${unit(padding)} 0`,
        color: colorTextDescription,
        fontSize: token.fontSizeSM,
        textAlign: "center"
      },
      [`${componentCls}-empty-text`]: {
        padding: emptyTextPadding,
        color: token.colorTextDisabled,
        fontSize: token.fontSize,
        textAlign: "center"
      },
      // ============================ without flex ============================
      [`${componentCls}-item-no-flex`]: {
        display: "block"
      }
    },
    [`${componentCls}-grid ${antCls}-col > ${componentCls}-item`]: {
      display: "block",
      maxWidth: "100%",
      marginBlockEnd: margin,
      paddingBlock: 0,
      borderBlockEnd: "none"
    },
    [`${componentCls}-vertical ${componentCls}-item`]: {
      alignItems: "initial",
      [`${componentCls}-item-main`]: {
        display: "block",
        flex: 1
      },
      [`${componentCls}-item-extra`]: {
        marginInlineStart: marginLG
      },
      [`${componentCls}-item-meta`]: {
        marginBlockEnd: metaMarginBottom,
        [`${componentCls}-item-meta-title`]: {
          marginBlockStart: 0,
          marginBlockEnd: titleMarginBottom,
          color: colorText,
          fontSize: token.fontSizeLG,
          lineHeight: token.lineHeightLG
        }
      },
      [`${componentCls}-item-action`]: {
        marginBlockStart: padding,
        marginInlineStart: "auto",
        "> li": {
          padding: `0 ${unit(padding)}`,
          "&:first-child": {
            paddingInlineStart: 0
          }
        }
      }
    },
    [`${componentCls}-split ${componentCls}-item`]: {
      borderBlockEnd: `${unit(token.lineWidth)} ${token.lineType} ${token.colorSplit}`,
      "&:last-child": {
        borderBlockEnd: "none"
      }
    },
    [`${componentCls}-split ${componentCls}-header`]: {
      borderBlockEnd: `${unit(token.lineWidth)} ${token.lineType} ${token.colorSplit}`
    },
    [`${componentCls}-split${componentCls}-empty ${componentCls}-footer`]: {
      borderTop: `${unit(token.lineWidth)} ${token.lineType} ${token.colorSplit}`
    },
    [`${componentCls}-loading ${componentCls}-spin-nested-loading`]: {
      minHeight: controlHeight
    },
    [`${componentCls}-split${componentCls}-something-after-last-item ${antCls}-spin-container > ${componentCls}-items > ${componentCls}-item:last-child`]: {
      borderBlockEnd: `${unit(token.lineWidth)} ${token.lineType} ${token.colorSplit}`
    },
    [`${componentCls}-lg ${componentCls}-item`]: {
      padding: itemPaddingLG
    },
    [`${componentCls}-sm ${componentCls}-item`]: {
      padding: itemPaddingSM
    },
    // Horizontal
    [`${componentCls}:not(${componentCls}-vertical)`]: {
      [`${componentCls}-item-no-flex`]: {
        [`${componentCls}-item-action`]: {
          float: "right"
        }
      }
    }
  };
};
const prepareComponentToken = (token) => ({
  contentWidth: 220,
  itemPadding: `${unit(token.paddingContentVertical)} 0`,
  itemPaddingSM: `${unit(token.paddingContentVerticalSM)} ${unit(token.paddingContentHorizontal)}`,
  itemPaddingLG: `${unit(token.paddingContentVerticalLG)} ${unit(token.paddingContentHorizontalLG)}`,
  headerBg: "transparent",
  footerBg: "transparent",
  emptyTextPadding: token.padding,
  metaMarginBottom: token.padding,
  avatarMarginRight: token.padding,
  titleMarginBottom: token.paddingSM,
  descriptionFontSize: token.fontSize
});
const useStyle = genStyleHooks("List", (token) => {
  const listToken = merge(token, {
    listBorderedCls: `${token.componentCls}-bordered`,
    minHeight: token.controlHeightLG
  });
  return [genBaseStyle(listToken), genBorderedStyle(listToken), genResponsiveStyle(listToken)];
}, prepareComponentToken, {
  extraCssVarPrefixCls: ({
    prefixCls
  }) => [`${prefixCls}-container`]
});
const InternalList = (props, ref) => {
  const {
    pagination = false,
    prefixCls: customizePrefixCls,
    bordered = false,
    split = true,
    className,
    rootClassName,
    style,
    children,
    itemLayout,
    loadMore,
    grid,
    dataSource = [],
    size: customizeSize,
    header,
    footer,
    loading = false,
    rowKey,
    renderItem,
    locale: locale2,
    ...rest
  } = props;
  const paginationObj = isPlainObject(pagination) ? pagination : {};
  const [paginationCurrent, setPaginationCurrent] = reactExports.useState(paginationObj.defaultCurrent || 1);
  const [paginationSize, setPaginationSize] = reactExports.useState(paginationObj.defaultPageSize || 10);
  const {
    getPrefixCls,
    direction,
    className: contextClassName,
    style: contextStyle
  } = useComponentConfig("list");
  const {
    renderEmpty
  } = reactExports.useContext(ConfigContext);
  const defaultPaginationProps = {
    current: 1,
    total: 0,
    position: "bottom"
  };
  const triggerPaginationEvent = (eventName) => (page, pageSize) => {
    setPaginationCurrent(page);
    setPaginationSize(pageSize);
    if (pagination) {
      pagination?.[eventName]?.(page, pageSize);
    }
  };
  const onPaginationChange = triggerPaginationEvent("onChange");
  const onPaginationShowSizeChange = triggerPaginationEvent("onShowSizeChange");
  const renderInternalItem = (item, index) => {
    if (!renderItem) {
      return null;
    }
    let key;
    if (isFunction(rowKey)) {
      key = rowKey(item);
    } else if (rowKey) {
      key = item[rowKey];
    } else {
      key = item.key;
    }
    if (!key) {
      key = `list-item-${index}`;
    }
    return /* @__PURE__ */ reactExports.createElement(reactExports.Fragment, {
      key
    }, renderItem(item, index));
  };
  const isSomethingAfterLastItem = !!(loadMore || pagination || footer);
  const prefixCls = getPrefixCls("list", customizePrefixCls);
  const [hashId, cssVarCls] = useStyle(prefixCls);
  let loadingProp = loading;
  if (typeof loadingProp === "boolean") {
    loadingProp = {
      spinning: loadingProp
    };
  }
  const isLoading = !!loadingProp?.spinning;
  const mergedSize = useSize(customizeSize);
  let sizeCls = "";
  switch (mergedSize) {
    case "large":
      sizeCls = "lg";
      break;
    case "small":
      sizeCls = "sm";
      break;
  }
  const classString = clsx(prefixCls, {
    [`${prefixCls}-vertical`]: itemLayout === "vertical",
    [`${prefixCls}-${sizeCls}`]: sizeCls,
    [`${prefixCls}-split`]: split,
    [`${prefixCls}-bordered`]: bordered,
    [`${prefixCls}-loading`]: isLoading,
    [`${prefixCls}-grid`]: !!grid,
    [`${prefixCls}-something-after-last-item`]: isSomethingAfterLastItem,
    [`${prefixCls}-rtl`]: direction === "rtl"
  }, contextClassName, className, rootClassName, hashId, cssVarCls);
  const containerCls = `${prefixCls}-container`;
  const paginationProps = mergeProps(defaultPaginationProps, {
    total: dataSource.length,
    current: paginationCurrent,
    pageSize: paginationSize
  }, pagination || {});
  const largestPage = Math.ceil(paginationProps.total / paginationProps.pageSize);
  paginationProps.current = Math.min(paginationProps.current, largestPage);
  const paginationContent = pagination && /* @__PURE__ */ reactExports.createElement("div", {
    className: clsx(`${prefixCls}-pagination`)
  }, /* @__PURE__ */ reactExports.createElement(Pagination, {
    align: "end",
    ...paginationProps,
    onChange: onPaginationChange,
    onShowSizeChange: onPaginationShowSizeChange
  }));
  let splitDataSource = _toConsumableArray(dataSource);
  if (pagination) {
    if (dataSource.length > (paginationProps.current - 1) * paginationProps.pageSize) {
      splitDataSource = _toConsumableArray(dataSource).splice((paginationProps.current - 1) * paginationProps.pageSize, paginationProps.pageSize);
    }
  }
  const needResponsive = Object.keys(grid || {}).some((key) => responsiveArray.includes(key));
  const screens = useBreakpoint(needResponsive);
  const currentBreakpoint = reactExports.useMemo(() => {
    for (let i = 0; i < responsiveArray.length; i += 1) {
      const breakpoint = responsiveArray[i];
      if (screens[breakpoint]) {
        return breakpoint;
      }
    }
    return void 0;
  }, [screens]);
  const colStyle = reactExports.useMemo(() => {
    if (!grid) {
      return void 0;
    }
    const columnCount = currentBreakpoint && grid[currentBreakpoint] ? grid[currentBreakpoint] : grid.column;
    if (columnCount) {
      return {
        width: `${100 / columnCount}%`,
        maxWidth: `${100 / columnCount}%`
      };
    }
  }, [JSON.stringify(grid), currentBreakpoint]);
  let childrenContent = isLoading && /* @__PURE__ */ reactExports.createElement("div", {
    style: {
      minHeight: 53
    }
  });
  if (splitDataSource.length > 0) {
    const items = splitDataSource.map(renderInternalItem);
    childrenContent = grid ? /* @__PURE__ */ reactExports.createElement(Row, {
      className: clsx(containerCls, cssVarCls),
      gutter: grid.gutter
    }, reactExports.Children.map(items, (child) => /* @__PURE__ */ reactExports.createElement("div", {
      key: child?.key,
      style: colStyle
    }, child))) : /* @__PURE__ */ reactExports.createElement("ul", {
      className: clsx(`${prefixCls}-items`, containerCls, cssVarCls)
    }, items);
  } else if (!children && !isLoading) {
    childrenContent = /* @__PURE__ */ reactExports.createElement("div", {
      className: `${prefixCls}-empty-text`
    }, locale2?.emptyText || renderEmpty?.("List") || /* @__PURE__ */ reactExports.createElement(DefaultRenderEmpty, {
      componentName: "List"
    }));
  }
  const paginationPosition = paginationProps.position;
  const contextValue = reactExports.useMemo(() => ({
    grid,
    itemLayout
  }), [JSON.stringify(grid), itemLayout]);
  return /* @__PURE__ */ reactExports.createElement(ListContext.Provider, {
    value: contextValue
  }, /* @__PURE__ */ reactExports.createElement("div", {
    ref,
    style: {
      ...contextStyle,
      ...style
    },
    className: classString,
    ...rest
  }, (paginationPosition === "top" || paginationPosition === "both") && paginationContent, header && /* @__PURE__ */ reactExports.createElement("div", {
    className: `${prefixCls}-header`
  }, header), /* @__PURE__ */ reactExports.createElement(Spin, {
    ...loadingProp
  }, childrenContent, children), footer && /* @__PURE__ */ reactExports.createElement("div", {
    className: `${prefixCls}-footer`
  }, footer), loadMore || (paginationPosition === "bottom" || paginationPosition === "both") && paginationContent));
};
const ListWithForwardRef = /* @__PURE__ */ reactExports.forwardRef(InternalList);
const List = ListWithForwardRef;
List.Item = Item;
export {
  List as L
};
