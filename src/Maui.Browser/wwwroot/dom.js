const elementByHandle = new Map();
const handleByElement = new WeakMap();
let nextElementHandle = 1;

function toHandle(element) {
  const existingHandle = handleByElement.get(element);
  if (existingHandle !== undefined) {
    return existingHandle;
  }

  const handle = nextElementHandle++;
  elementByHandle.set(handle, element);
  handleByElement.set(element, handle);
  return handle;
}

function fromHandle(handle) {
  if (!Number.isInteger(handle)) {
    throw new Error(`Invalid element handle '${handle}'`);
  }

  const element = elementByHandle.get(handle);
  if (!element) {
    throw new Error(`Unknown element handle '${handle}'`);
  }

  return element;
}

export function getRootById(id) {
  const el = document.getElementById(id);
  if (!el) {
    throw new Error(`No element found with id '${id}'`);
  }

  return toHandle(el);
}

export function createElement(tag) {
  return toHandle(document.createElement(tag));
}

export function appendChild(parentHandle, childHandle) {
  const parent = fromHandle(parentHandle);
  const child = fromHandle(childHandle);
  parent.appendChild(child);
}

export function insertChildAt(parentHandle, childHandle, index) {
  const parent = fromHandle(parentHandle);
  const child = fromHandle(childHandle);
  const normalizedIndex = Number.isInteger(index) ? Math.max(0, index) : parent.children.length;
  const referenceNode = parent.children.item(normalizedIndex);
  if (referenceNode) {
    parent.insertBefore(child, referenceNode);
    return;
  }

  parent.appendChild(child);
}

export function removeChild(parentHandle, childHandle) {
  const parent = fromHandle(parentHandle);
  const child = fromHandle(childHandle);
  parent.removeChild(child);
}

export function clearChildren(parentHandle) {
  const parent = fromHandle(parentHandle);
  while (parent.firstChild) {
    parent.removeChild(parent.firstChild);
  }
}

export function setText(elementHandle, text) {
  const element = fromHandle(elementHandle);
  element.textContent = text ?? "";
}

export function setAttr(elementHandle, name, value) {
  const element = fromHandle(elementHandle);
  if (value == null) {
    element.removeAttribute(name);
    return;
  }

  element.setAttribute(name, value);
}

export function setStyle(elementHandle, name, value) {
  const element = fromHandle(elementHandle);
  // name is a CSSOM style property, e.g. "backgroundColor", "fontSize"
  element.style[name] = value ?? "";
}

export function getClientSize(elementHandle) {
  const element = fromHandle(elementHandle);
  const r = element.getBoundingClientRect();
  return [r.width, r.height];
}

export function setScrollPosition(elementHandle, left, top) {
  const element = fromHandle(elementHandle);
  const normalizedLeft = Number.isFinite(left) ? Math.max(0, left) : 0;
  const normalizedTop = Number.isFinite(top) ? Math.max(0, top) : 0;
  element.scrollLeft = normalizedLeft;
  element.scrollTop = normalizedTop;
}

export function getScrollPosition(elementHandle) {
  const element = fromHandle(elementHandle);
  const left = Number.isFinite(element.scrollLeft) ? element.scrollLeft : 0;
  const top = Number.isFinite(element.scrollTop) ? element.scrollTop : 0;
  return [left, top];
}

export function scrollChildIntoView(containerHandle, childHandle, scrollToPosition) {
  const container = fromHandle(containerHandle);
  const child = fromHandle(childHandle);

  const containerRect = container.getBoundingClientRect();
  const childRect = child.getBoundingClientRect();
  const currentTop = Number.isFinite(container.scrollTop) ? container.scrollTop : 0;
  const relativeTop = childRect.top - containerRect.top + currentTop;
  const relativeBottom = childRect.bottom - containerRect.top + currentTop;
  const childHeight = Number.isFinite(childRect.height) ? childRect.height : 0;
  const viewportHeight = Number.isFinite(container.clientHeight) ? container.clientHeight : 0;

  let targetTop = currentTop;
  switch (scrollToPosition) {
    // ScrollToPosition.Start
    case 1:
      targetTop = relativeTop;
      break;
    // ScrollToPosition.Center
    case 2:
      targetTop = relativeTop - ((viewportHeight - childHeight) / 2);
      break;
    // ScrollToPosition.End
    case 3:
      targetTop = relativeBottom - viewportHeight;
      break;
    // ScrollToPosition.MakeVisible
    case 0:
    default:
      if (relativeTop < currentTop) {
        targetTop = relativeTop;
      } else if (relativeBottom > currentTop + viewportHeight) {
        targetTop = relativeBottom - viewportHeight;
      }
      break;
  }

  const maxTop = Math.max(0, (Number.isFinite(container.scrollHeight) ? container.scrollHeight : 0) - viewportHeight);
  const normalizedTop = Number.isFinite(targetTop)
    ? Math.min(Math.max(0, targetTop), maxTop)
    : 0;

  container.scrollTop = normalizedTop;
  return Number.isFinite(container.scrollTop) ? container.scrollTop : normalizedTop;
}

export function focusElement(elementHandle) {
  const element = fromHandle(elementHandle);
  if (typeof element.focus === "function") {
    element.focus();
  }

  return document.activeElement === element;
}

export function blurElement(elementHandle) {
  const element = fromHandle(elementHandle);
  if (typeof element.blur === "function") {
    element.blur();
  }
}

const eventSubscriptions = new Map();
let nextEventSubscriptionId = 1;
let placeholderStyleInjected = false;

function ensurePlaceholderStyle() {
  if (placeholderStyleInjected) {
    return;
  }

  const style = document.createElement("style");
  style.id = "maui-placeholder-style";
  style.textContent = ".maui-placeholder-color::placeholder { color: var(--maui-placeholder-color); opacity: 1; }";
  document.head.appendChild(style);
  placeholderStyleInjected = true;
}

function removeElementSubscriptions(element) {
  for (const [subscriptionId, subscription] of eventSubscriptions) {
    if (subscription.element !== element) {
      continue;
    }

    if (typeof subscription.dispose === "function") {
      subscription.dispose();
      eventSubscriptions.delete(subscriptionId);
      continue;
    }

    subscription.element.removeEventListener(subscription.eventName, subscription.listener);
    eventSubscriptions.delete(subscriptionId);
  }
}

function releaseElementRecursive(element) {
  for (const child of Array.from(element.children)) {
    releaseElementRecursive(child);
  }

  removeElementSubscriptions(element);

  const handle = handleByElement.get(element);
  if (handle === undefined) {
    return;
  }

  handleByElement.delete(element);
  elementByHandle.delete(handle);
}

export function subscribeEvent(elementHandle, eventName, callback) {
  const element = fromHandle(elementHandle);
  const subscriptionId = nextEventSubscriptionId++;
  const listener = () => callback();
  eventSubscriptions.set(subscriptionId, { element, eventName, listener });
  element.addEventListener(eventName, listener);
  return subscriptionId;
}

function findScrollableAncestor(root, startNode) {
  const owner = root;
  let node = startNode instanceof Element ? startNode : null;
  while (node && node !== owner && owner.contains(node)) {
    if (node instanceof HTMLElement) {
      if (node.hasAttribute("data-maui-cv-scroll-host")) {
        return node;
      }

      const style = getComputedStyle(node);
      const overflowY = style?.overflowY ?? "";
      if (overflowY === "auto" || overflowY === "scroll") {
        return node;
      }
    }

    node = node.parentElement;
  }

  if (owner instanceof HTMLElement) {
    const ownerStyle = getComputedStyle(owner);
    const overflowY = ownerStyle?.overflowY ?? "";
    if (overflowY === "auto" || overflowY === "scroll") {
      return owner;
    }
  }

  return null;
}

function resetPullState(root) {
  root.setAttribute("data-maui-rv-pull-active", "false");
  root.setAttribute("data-maui-rv-pull-armed", "false");
  root.setAttribute("data-maui-rv-pull-distance", "0");
}

function isPullGestureAllowed(root) {
  const isRefreshEnabled = root.getAttribute("data-maui-rv-enabled") === "true";
  const isRefreshing = root.getAttribute("data-maui-rv-refreshing") === "true";
  return isRefreshEnabled && !isRefreshing;
}

export function subscribePullToRefresh(elementHandle, threshold, callback) {
  const root = fromHandle(elementHandle);
  const normalizedThreshold = Number.isFinite(threshold) && threshold > 8 ? threshold : 72;
  const maxDistance = normalizedThreshold * 1.8;
  const state = {
    tracking: false,
    pointerId: -1,
    startY: 0,
    initialScrollTop: 0,
    armed: false,
    scroller: null
  };

  resetPullState(root);
  const subscriptionId = nextEventSubscriptionId++;

  const completeTracking = (triggerRefresh) => {
    if (!state.tracking) {
      return;
    }

    const shouldTrigger = Boolean(triggerRefresh && state.armed);
    state.tracking = false;
    state.pointerId = -1;
    state.startY = 0;
    state.initialScrollTop = 0;
    state.armed = false;
    state.scroller = null;
    resetPullState(root);

    if (shouldTrigger) {
      callback();
    }
  };

  const pointerDownListener = (event) => {
    if (!(event instanceof PointerEvent)) {
      return;
    }

    if (state.tracking) {
      return;
    }

    if (event.pointerType !== "touch" && event.button !== 0) {
      return;
    }

    if (!isPullGestureAllowed(root)) {
      return;
    }

    const scroller = findScrollableAncestor(root, event.target);
    if (!scroller) {
      return;
    }

    state.tracking = true;
    state.pointerId = event.pointerId;
    state.startY = event.clientY;
    state.initialScrollTop = Number.isFinite(scroller.scrollTop) ? Math.max(0, scroller.scrollTop) : 0;
    state.armed = false;
    state.scroller = scroller;
    resetPullState(root);
  };

  const pointerMoveListener = (event) => {
    if (!state.tracking || !(event instanceof PointerEvent)) {
      return;
    }

    if (!isPullGestureAllowed(root)) {
      completeTracking(false);
      return;
    }

    if (event.pointerId !== state.pointerId) {
      return;
    }

    if (!state.scroller) {
      completeTracking(false);
      return;
    }

    const rawDeltaY = event.clientY - state.startY;
    const reachedTopByScroll = state.scroller.scrollTop <= 0.5;
    const reachedTopByDrag = state.initialScrollTop > 0 && rawDeltaY >= state.initialScrollTop;
    if (!reachedTopByScroll && !reachedTopByDrag) {
      // Keep tracking the same gesture so downward drag can arm once
      // the scroller reaches top without requiring a new pointerdown.
      state.armed = false;
      root.setAttribute("data-maui-rv-pull-distance", "0");
      root.setAttribute("data-maui-rv-pull-armed", "false");
      root.setAttribute("data-maui-rv-pull-active", "false");
      return;
    }

    const deltaY = rawDeltaY - state.initialScrollTop;
    if (deltaY <= 0) {
      root.setAttribute("data-maui-rv-pull-distance", "0");
      root.setAttribute("data-maui-rv-pull-armed", "false");
      root.setAttribute("data-maui-rv-pull-active", "false");
      state.armed = false;
      return;
    }

    const clampedDistance = Math.min(maxDistance, deltaY);
    state.armed = clampedDistance >= normalizedThreshold;
    root.setAttribute("data-maui-rv-pull-active", "true");
    root.setAttribute("data-maui-rv-pull-armed", state.armed ? "true" : "false");
    root.setAttribute("data-maui-rv-pull-distance", String(Math.round(clampedDistance)));
    event.preventDefault();
  };

  const pointerUpListener = (event) => {
    if (!(event instanceof PointerEvent) || event.pointerId !== state.pointerId) {
      return;
    }

    completeTracking(true);
  };

  const pointerCancelListener = (event) => {
    if (event instanceof PointerEvent && state.pointerId >= 0 && event.pointerId !== state.pointerId) {
      return;
    }

    completeTracking(false);
  };

  const scrollListener = () => {
    if (!state.tracking || !state.scroller) {
      return;
    }

    if (!isPullGestureAllowed(root)) {
      completeTracking(false);
      return;
    }

    const currentTop = Number.isFinite(state.scroller.scrollTop) ? state.scroller.scrollTop : 0;
    // Do not abort when a downward drag is consuming pre-existing scroll offset.
    // Abort only when scroll position moves deeper than the initial offset.
    if (currentTop > state.initialScrollTop + 0.5) {
      completeTracking(false);
    }
  };

  root.addEventListener("pointerdown", pointerDownListener, { passive: true });
  root.addEventListener("pointermove", pointerMoveListener, { passive: false });
  root.addEventListener("scroll", scrollListener, true);
  window.addEventListener("pointerup", pointerUpListener, true);
  window.addEventListener("pointercancel", pointerCancelListener, true);
  window.addEventListener("blur", pointerCancelListener);

  eventSubscriptions.set(subscriptionId, {
    element: root,
    dispose: () => {
      root.removeEventListener("pointerdown", pointerDownListener, { passive: true });
      root.removeEventListener("pointermove", pointerMoveListener, { passive: false });
      root.removeEventListener("scroll", scrollListener, true);
      window.removeEventListener("pointerup", pointerUpListener, true);
      window.removeEventListener("pointercancel", pointerCancelListener, true);
      window.removeEventListener("blur", pointerCancelListener);
      completeTracking(false);
    }
  });

  return subscriptionId;
}

export function subscribeClickActivation(elementHandle, pointerCallback, keyboardCallback) {
  const element = fromHandle(elementHandle);
  const subscriptionId = nextEventSubscriptionId++;
  const listener = (event) => {
    if (event.detail === 0) {
      keyboardCallback();
      return;
    }

    pointerCallback();
  };

  eventSubscriptions.set(subscriptionId, { element, eventName: "click", listener });
  element.addEventListener("click", listener);
  return subscriptionId;
}

export function subscribeEnterKey(elementHandle, callback) {
  const element = fromHandle(elementHandle);
  const subscriptionId = nextEventSubscriptionId++;
  const listener = (event) => {
    if (event.key === "Enter") {
      callback();
    }
  };

  eventSubscriptions.set(subscriptionId, { element, eventName: "keydown", listener });
  element.addEventListener("keydown", listener);
  return subscriptionId;
}

export function unsubscribeEvent(subscriptionId) {
  const subscription = eventSubscriptions.get(subscriptionId);
  if (!subscription) {
    return;
  }

  if (typeof subscription.dispose === "function") {
    subscription.dispose();
    eventSubscriptions.delete(subscriptionId);
    return;
  }

  subscription.element.removeEventListener(subscription.eventName, subscription.listener);
  eventSubscriptions.delete(subscriptionId);
}

export function releaseElement(elementHandle) {
  const element = elementByHandle.get(elementHandle);
  if (!element) {
    return;
  }

  releaseElementRecursive(element);
}

export function setInputValue(elementHandle, value) {
  const element = fromHandle(elementHandle);
  element.value = value ?? "";
}

export function getInputValue(elementHandle) {
  const element = fromHandle(elementHandle);
  return element.value ?? "";
}

export function setInputChecked(elementHandle, value) {
  const element = fromHandle(elementHandle);
  element.checked = Boolean(value);
}

export function getInputChecked(elementHandle) {
  const element = fromHandle(elementHandle);
  return Boolean(element.checked);
}

export function setPlaceholderColor(elementHandle, value) {
  const element = fromHandle(elementHandle);
  if (!value) {
    element.classList.remove("maui-placeholder-color");
    element.style.removeProperty("--maui-placeholder-color");
    return;
  }

  ensurePlaceholderStyle();
  element.classList.add("maui-placeholder-color");
  element.style.setProperty("--maui-placeholder-color", value);
}

export function setSelectSelectedIndex(elementHandle, value) {
  const element = fromHandle(elementHandle);
  const selectedIndex = Number.isInteger(value) ? value : -1;
  element.selectedIndex = selectedIndex;
}

export function getSelectSelectedIndex(elementHandle) {
  const element = fromHandle(elementHandle);
  const selectedIndex = element.selectedIndex;
  return Number.isInteger(selectedIndex) ? selectedIndex : -1;
}

const resizeSubscriptions = new Map();
let nextResizeSubscriptionId = 1;
const pointerReleaseSubscriptions = new Map();
let nextPointerReleaseSubscriptionId = 1;

export function subscribeWindowResize(callback) {
  const subscriptionId = nextResizeSubscriptionId++;
  const listener = () => callback();
  resizeSubscriptions.set(subscriptionId, listener);
  window.addEventListener("resize", listener);
  return subscriptionId;
}

export function unsubscribeWindowResize(subscriptionId) {
  const listener = resizeSubscriptions.get(subscriptionId);
  if (!listener) {
    return;
  }

  window.removeEventListener("resize", listener);
  resizeSubscriptions.delete(subscriptionId);
}

export function subscribeWindowPointerRelease(callback) {
  const subscriptionId = nextPointerReleaseSubscriptionId++;
  const listener = () => callback();
  pointerReleaseSubscriptions.set(subscriptionId, listener);
  window.addEventListener("pointerup", listener, true);
  window.addEventListener("pointercancel", listener, true);
  window.addEventListener("blur", listener);
  return subscriptionId;
}

export function unsubscribeWindowPointerRelease(subscriptionId) {
  const listener = pointerReleaseSubscriptions.get(subscriptionId);
  if (!listener) {
    return;
  }

  window.removeEventListener("pointerup", listener, true);
  window.removeEventListener("pointercancel", listener, true);
  window.removeEventListener("blur", listener);
  pointerReleaseSubscriptions.delete(subscriptionId);
}

export function getDiagnostics() {
  return [
    elementByHandle.size,
    eventSubscriptions.size,
    resizeSubscriptions.size
  ];
}

let measurementCanvas;

function getMeasurementContext() {
  if (!measurementCanvas) {
    measurementCanvas = document.createElement("canvas");
  }

  return measurementCanvas.getContext("2d");
}

function parseFontSize(font) {
  if (!font) {
    return 14;
  }

  const match = /([0-9]+(?:\.[0-9]+)?)px/.exec(font);
  if (!match) {
    return 14;
  }

  return Number.parseFloat(match[1]);
}

export function measureText(text, font) {
  const context = getMeasurementContext();
  if (!context) {
    const fallbackWidth = (text ?? "").length * 8;
    return [fallbackWidth, 16];
  }

  const normalizedFont = font && font.trim().length > 0
    ? font
    : "normal 400 14px system-ui, sans-serif";
  context.font = normalizedFont;

  const metrics = context.measureText(text ?? "");
  const width = Number.isFinite(metrics.width) ? metrics.width : 0;
  const ascent = Number.isFinite(metrics.actualBoundingBoxAscent) ? metrics.actualBoundingBoxAscent : 0;
  const descent = Number.isFinite(metrics.actualBoundingBoxDescent) ? metrics.actualBoundingBoxDescent : 0;
  const measuredHeight = ascent + descent;
  const lineHeight = parseFontSize(normalizedFont) * 1.2;
  const height = Math.max(measuredHeight, lineHeight);

  return [width, height];
}

export function openPicker(elementHandle) {
  const element = fromHandle(elementHandle);
  if (typeof element.showPicker === "function") {
    try {
      element.showPicker();
      return true;
    } catch {
      // Some browser contexts can reject showPicker; fallback to focus.
    }
  }

  if (typeof element.focus === "function") {
    element.focus();
    return document.activeElement === element;
  }

  return false;
}
