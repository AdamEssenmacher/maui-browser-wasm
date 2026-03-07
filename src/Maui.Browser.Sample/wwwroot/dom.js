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
