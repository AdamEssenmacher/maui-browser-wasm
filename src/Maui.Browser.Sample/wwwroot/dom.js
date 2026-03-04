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

const eventSubscriptions = new Map();
let nextEventSubscriptionId = 1;

export function subscribeEvent(elementHandle, eventName, callback) {
  const element = fromHandle(elementHandle);
  const subscriptionId = nextEventSubscriptionId++;
  const listener = () => callback();
  eventSubscriptions.set(subscriptionId, { element, eventName, listener });
  element.addEventListener(eventName, listener);
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

export function setInputValue(elementHandle, value) {
  const element = fromHandle(elementHandle);
  element.value = value ?? "";
}

export function getInputValue(elementHandle) {
  const element = fromHandle(elementHandle);
  return element.value ?? "";
}

const resizeSubscriptions = new Map();
let nextResizeSubscriptionId = 1;

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
