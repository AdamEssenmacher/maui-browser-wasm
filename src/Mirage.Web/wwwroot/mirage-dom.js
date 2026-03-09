const elementByHandle = new Map();
const handleByElement = new WeakMap();
let nextElementHandle = 1;

function toHandle(element) {
  const existing = handleByElement.get(element);
  if (existing !== undefined) {
    return existing;
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
  const element = document.getElementById(id);
  if (!element) {
    throw new Error(`No root element found with id '${id}'`);
  }

  return toHandle(element);
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
  let normalizedIndex = Number.isInteger(index) ? Math.max(0, index) : parent.children.length;

  if (child.parentElement === parent) {
    const currentIndex = Array.prototype.indexOf.call(parent.children, child);
    if (currentIndex >= 0 && currentIndex < normalizedIndex) {
      normalizedIndex = Math.min(parent.children.length, normalizedIndex + 1);
    }
  }

  const reference = parent.children.item(normalizedIndex);
  if (reference) {
    parent.insertBefore(child, reference);
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

const eventSubscriptions = new Map();
let nextEventSubscriptionId = 1;

function releaseElementRecursive(element) {
  for (const child of Array.from(element.children)) {
    releaseElementRecursive(child);
  }

  for (const [subscriptionId, subscription] of eventSubscriptions) {
    if (subscription.element !== element) {
      continue;
    }

    subscription.element.removeEventListener(subscription.eventName, subscription.listener);
    eventSubscriptions.delete(subscriptionId);
  }

  const handle = handleByElement.get(element);
  if (handle === undefined) {
    return;
  }

  handleByElement.delete(element);
  elementByHandle.delete(handle);
}

export function releaseElement(elementHandle) {
  const element = elementByHandle.get(elementHandle);
  if (!element) {
    return;
  }

  releaseElementRecursive(element);
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
  element.style[name] = value ?? "";
}

export function addClass(elementHandle, className) {
  const element = fromHandle(elementHandle);
  if (!className) {
    return;
  }

  element.classList.add(className);
}

export function removeClass(elementHandle, className) {
  const element = fromHandle(elementHandle);
  if (!className) {
    return;
  }

  element.classList.remove(className);
}

export function setPropertyString(elementHandle, name, value) {
  const element = fromHandle(elementHandle);
  element[name] = value ?? "";
}

export function setPropertyBool(elementHandle, name, value) {
  const element = fromHandle(elementHandle);
  element[name] = !!value;
}

export function setPropertyNumber(elementHandle, name, value) {
  const element = fromHandle(elementHandle);
  element[name] = Number.isFinite(value) ? value : 0;
}

export function setPropertyNull(elementHandle, name) {
  const element = fromHandle(elementHandle);
  element[name] = null;
}

export function setPropertyJson(elementHandle, name, json) {
  const element = fromHandle(elementHandle);
  if (json == null || json.length === 0) {
    element[name] = null;
    return;
  }

  try {
    element[name] = JSON.parse(json);
  } catch {
    element[name] = null;
  }
}

export function removeProperty(elementHandle, name) {
  const element = fromHandle(elementHandle);
  delete element[name];
}

export function subscribeEvent(elementHandle, eventName, callback) {
  const element = fromHandle(elementHandle);
  const listener = () => callback();
  const subscriptionId = nextEventSubscriptionId++;
  eventSubscriptions.set(subscriptionId, { element, eventName, listener });
  element.addEventListener(eventName, listener);
  return subscriptionId;
}

export function subscribeInputEvent(elementHandle, eventName, callback) {
  const element = fromHandle(elementHandle);
  const listener = () => callback(element.value ?? "");
  const subscriptionId = nextEventSubscriptionId++;
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
  const id = nextResizeSubscriptionId++;
  const listener = () => callback();
  resizeSubscriptions.set(id, listener);
  window.addEventListener("resize", listener);
  return id;
}

export function unsubscribeWindowResize(subscriptionId) {
  const listener = resizeSubscriptions.get(subscriptionId);
  if (!listener) {
    return;
  }

  window.removeEventListener("resize", listener);
  resizeSubscriptions.delete(subscriptionId);
}

export function getClientSize(elementHandle) {
  const element = fromHandle(elementHandle);
  const rect = element.getBoundingClientRect();
  return [rect.width, rect.height];
}

let measurementCanvas;
let measurementContext;

function ensureMeasurementContext() {
  if (measurementContext) {
    return measurementContext;
  }

  measurementCanvas = document.createElement("canvas");
  measurementContext = measurementCanvas.getContext("2d");
  return measurementContext;
}

export function measureText(text, font) {
  const ctx = ensureMeasurementContext();
  if (!ctx) {
    return [0, 0];
  }

  ctx.font = font || "14px sans-serif";
  const metrics = ctx.measureText(text ?? "");
  const height = Math.max(1, (metrics.actualBoundingBoxAscent || 0) + (metrics.actualBoundingBoxDescent || 0));
  return [metrics.width || 0, height || 0];
}

function findElementByAutomationId(root, automationId) {
  if (!root || !automationId) {
    return null;
  }

  if (root instanceof Element && root.getAttribute("data-automation-id") === automationId) {
    return root;
  }

  const candidates = root.querySelectorAll("[data-automation-id]");
  for (const candidate of candidates) {
    if (candidate.getAttribute("data-automation-id") === automationId) {
      return candidate;
    }
  }

  return null;
}

export function scrollToAutomationId(rootHandle, automationId, smooth) {
  const root = fromHandle(rootHandle);
  const target = findElementByAutomationId(root, automationId);
  if (!target) {
    return false;
  }

  target.scrollIntoView({
    behavior: smooth ? "smooth" : "auto",
    block: "start",
    inline: "nearest"
  });

  return true;
}

export async function fetchText(path) {
  if (!path) {
    return null;
  }

  const response = await fetch(path, { cache: "no-store" });
  if (!response.ok) {
    return null;
  }

  return await response.text();
}
