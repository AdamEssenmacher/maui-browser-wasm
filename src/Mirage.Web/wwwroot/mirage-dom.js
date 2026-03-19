const elementByHandle = new Map();
const handleByElement = new WeakMap();
let nextElementHandle = 1;
const hotReloadHints = [];
const HOT_RELOAD_HINT_LIMIT = 256;
const HOT_RELOAD_PATH_PATTERN = /\/_hotreload\/[A-Za-z0-9_.-]+\/[^"'\\\s]+/g;
let hotReloadHintBridgeInstalled = false;
const MIRAGE_FRAMEWORK_STYLE_ID = "mirage-framework-control-styles";

function ensureMirageFrameworkStyles() {
  if (typeof document === "undefined" || !document.head) {
    return;
  }

  if (document.getElementById(MIRAGE_FRAMEWORK_STYLE_ID)) {
    return;
  }

  const styleElement = document.createElement("style");
  styleElement.id = MIRAGE_FRAMEWORK_STYLE_ID;
  styleElement.textContent = `
@keyframes mirage-activity-indicator-spin {
  0% {
    transform: rotate(0deg);
  }

  100% {
    transform: rotate(360deg);
  }
}

[data-mirage-kind="ActivityIndicator"] {
  position: relative;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  box-sizing: border-box;
  color: CanvasText;
}

[data-mirage-kind="ActivityIndicator"]::before {
  content: "";
  box-sizing: border-box;
  display: block;
  block-size: 100%;
  inline-size: auto;
  max-inline-size: 100%;
  aspect-ratio: 1 / 1;
  border-radius: 50%;
  border: 2px solid rgba(148, 163, 184, 0.38);
  border-top-color: var(--mirage-activity-color, currentColor);
  animation: mirage-activity-indicator-spin 0.85s linear infinite;
}

[data-mirage-kind="ActivityIndicator"][data-maui-activity-running="false"]::before {
  opacity: 0;
  animation-play-state: paused;
}

input[data-mirage-kind="Slider"][type="range"] {
  -webkit-appearance: none;
  appearance: none;
  background: transparent;
  border: none;
  padding: 0;
  outline: none;
}

input[data-mirage-kind="Slider"][type="range"]::-webkit-slider-runnable-track {
  height: 4px;
  border-radius: 999px;
  background: var(--mirage-slider-track-gradient, linear-gradient(to right, rgba(59,130,246,1) 0%, rgba(59,130,246,1) 50%, rgba(209,213,219,1) 50%, rgba(209,213,219,1) 100%));
}

input[data-mirage-kind="Slider"][type="range"]::-webkit-slider-thumb {
  -webkit-appearance: none;
  appearance: none;
  width: 22px;
  height: 22px;
  border-radius: 50%;
  border: 1px solid rgba(193,194,202,1);
  margin-top: -9px;
  box-shadow: 0 1px 2px rgba(0,0,0,0.2);
  background-color: var(--mirage-slider-thumb-color, rgba(255,255,255,1));
  background-image: var(--mirage-slider-thumb-image, none);
  background-repeat: no-repeat;
  background-position: center;
  background-size: contain;
}

input[data-mirage-kind="Slider"][type="range"][data-maui-slider-thumb-image-state="resolved"]::-webkit-slider-thumb {
  width: 10px;
  height: 20px;
  border-radius: 2px;
  border: 0;
  margin-top: -8px;
  box-shadow: none;
  background-color: transparent;
  background-size: contain;
}

input[data-mirage-kind="Slider"][type="range"]::-moz-range-track {
  height: 4px;
  border-radius: 999px;
  background: var(--mirage-slider-track-gradient, linear-gradient(to right, rgba(59,130,246,1) 0%, rgba(59,130,246,1) 50%, rgba(209,213,219,1) 50%, rgba(209,213,219,1) 100%));
}

input[data-mirage-kind="Slider"][type="range"]::-moz-range-thumb {
  width: 22px;
  height: 22px;
  border-radius: 50%;
  border: 1px solid rgba(193,194,202,1);
  box-shadow: 0 1px 2px rgba(0,0,0,0.2);
  background-color: var(--mirage-slider-thumb-color, rgba(255,255,255,1));
  background-image: var(--mirage-slider-thumb-image, none);
  background-repeat: no-repeat;
  background-position: center;
  background-size: contain;
}

input[data-mirage-kind="Slider"][type="range"][data-maui-slider-thumb-image-state="resolved"]::-moz-range-thumb {
  width: 10px;
  height: 20px;
  border-radius: 2px;
  border: 0;
  box-shadow: none;
  background-color: transparent;
  background-size: contain;
}

input[data-mirage-kind="Slider"][type="range"]:focus-visible {
  outline: 2px solid rgba(59,130,246,0.95);
  outline-offset: 2px;
}

input[data-mirage-kind="Slider"][type="range"]:focus-visible::-webkit-slider-thumb {
  box-shadow: 0 0 0 3px rgba(59,130,246,0.35);
}

input[data-mirage-kind="Slider"][type="range"]:focus-visible::-moz-range-thumb {
  box-shadow: 0 0 0 3px rgba(59,130,246,0.35);
}

input[data-mirage-kind="Slider"][type="range"]:disabled {
  cursor: default;
}

input[data-mirage-kind="Slider"][type="range"]:disabled::-webkit-slider-runnable-track {
  background: rgba(221,223,230,1);
}

input[data-mirage-kind="Slider"][type="range"]:disabled::-webkit-slider-thumb {
  background-color: rgba(246,246,249,1);
  border-color: rgba(201,202,210,1);
  box-shadow: none;
}

input[data-mirage-kind="Slider"][type="range"]:disabled::-moz-range-track {
  background: rgba(221,223,230,1);
}

input[data-mirage-kind="Slider"][type="range"]:disabled::-moz-range-thumb {
  background-color: rgba(246,246,249,1);
  border-color: rgba(201,202,210,1);
  box-shadow: none;
}

input[data-mirage-kind="Switch"][type="checkbox"] {
  appearance: none;
  -webkit-appearance: none;
  margin: 0;
  padding: 0;
  border: 1px solid rgba(15, 23, 42, 0.22);
  border-radius: 999px;
  width: 52px;
  height: 32px;
  background-color: var(--mirage-switch-track-off, rgba(148,163,184,1));
  box-sizing: border-box;
  position: relative;
  cursor: pointer;
  transition: background-color 120ms ease, border-color 120ms ease;
}

input[data-mirage-kind="Switch"][type="checkbox"]::before {
  content: "";
  position: absolute;
  top: 2px;
  left: 2px;
  width: 26px;
  height: 26px;
  border-radius: 50%;
  background-color: var(--mirage-switch-thumb, rgba(255,255,255,1));
  box-shadow: 0 1px 2px rgba(15,23,42,0.35);
  transition: transform 120ms ease, background-color 120ms ease;
}

input[data-mirage-kind="Switch"][type="checkbox"]:checked {
  background-color: var(--mirage-switch-track-on, rgba(16,185,129,1));
}

input[data-mirage-kind="Switch"][type="checkbox"]:checked::before {
  transform: translateX(20px);
}

input[data-mirage-kind="Switch"][type="checkbox"]:focus-visible {
  outline: 2px solid rgba(59,130,246,0.95);
  outline-offset: 2px;
}

input[data-mirage-kind="Switch"][type="checkbox"]:disabled {
  cursor: default;
  filter: saturate(0.45);
}

[data-mirage-kind="Stepper"] {
  display: flex;
  align-items: center;
  justify-content: flex-start;
  gap: 8px;
  inline-size: 100%;
  min-height: 44px;
  padding: 4px;
}

[data-mirage-kind="Stepper"] > [data-mirage-value-element="true"],
[data-mirage-kind="Stepper"] > [data-mirage-secondary-element="true"] {
  appearance: none;
  -webkit-appearance: none;
  border: 0;
  border-radius: 4px;
  background: rgba(203,204,208,1);
  color: rgba(28,28,28,0.92);
  min-width: 100px;
  height: 40px;
  padding: 0;
  line-height: 1;
  font-size: 28px;
  font-family: system-ui, sans-serif;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  box-shadow: 0 1px 2px rgba(0,0,0,0.12);
  user-select: none;
  cursor: pointer;
}

[data-mirage-kind="Stepper"] > [data-mirage-value-element="true"]:focus-visible,
[data-mirage-kind="Stepper"] > [data-mirage-secondary-element="true"]:focus-visible {
  outline: 2px solid rgba(59,130,246,0.9);
  outline-offset: 2px;
}

[data-mirage-kind="Stepper"] > [data-mirage-value-element="true"]:disabled,
[data-mirage-kind="Stepper"] > [data-mirage-secondary-element="true"]:disabled {
  background: rgba(210,211,216,0.82);
  color: rgba(88,89,94,0.45);
  box-shadow: none;
  cursor: default;
}

[data-mirage-kind="Map"] {
  position: relative;
  overflow: hidden;
  min-height: 180px;
}

[data-mirage-kind="Map"][data-mirage-map-status="error"]::before {
  content: attr(data-mirage-map-error);
  position: absolute;
  inset: 0;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 12px;
  font-size: 13px;
  line-height: 1.35;
  text-align: center;
  color: rgba(71, 5, 22, 0.92);
  background: rgba(255, 240, 246, 0.96);
  z-index: 1000;
  pointer-events: none;
}
`;

  document.head.appendChild(styleElement);
}

function normalizeHotReloadHint(rawHint) {
  if (typeof rawHint !== "string") {
    return null;
  }

  let hint = rawHint.trim();
  if (hint.length === 0) {
    return null;
  }

  if (hint === "*") {
    return hint;
  }

  try {
    if (hint.startsWith("http://") || hint.startsWith("https://")) {
      hint = new URL(hint).pathname;
    }
  } catch {
    // Keep original hint when URL parsing fails.
  }

  if (!hint.startsWith("/")) {
    hint = `/${hint}`;
  }

  return hint;
}

function enqueueHotReloadHint(rawHint) {
  const normalized = normalizeHotReloadHint(rawHint);
  if (!normalized) {
    return;
  }

  if (hotReloadHints.length > 0 && hotReloadHints[hotReloadHints.length - 1] === normalized) {
    return;
  }

  hotReloadHints.push(normalized);
  if (hotReloadHints.length > HOT_RELOAD_HINT_LIMIT) {
    hotReloadHints.splice(0, hotReloadHints.length - HOT_RELOAD_HINT_LIMIT);
  }
}

function collectHotReloadHints(value, sink) {
  if (value == null) {
    return;
  }

  if (typeof value === "string") {
    const pathMatches = value.match(HOT_RELOAD_PATH_PATTERN);
    if (pathMatches) {
      for (const pathMatch of pathMatches) {
        sink.push(pathMatch);
      }
    }

    const lowered = value.toLowerCase();
    if (lowered.includes("updatestaticfile") || lowered.includes("hotreload")) {
      sink.push("*");
    }

    return;
  }

  if (Array.isArray(value)) {
    for (const item of value) {
      collectHotReloadHints(item, sink);
    }
    return;
  }

  if (typeof value !== "object") {
    return;
  }

  for (const [key, child] of Object.entries(value)) {
    if (typeof child === "string" && key.toLowerCase().includes("path")) {
      sink.push(child);
    }

    collectHotReloadHints(child, sink);
  }
}

function inspectHotReloadMessage(rawData) {
  if (typeof rawData !== "string" || rawData.length === 0) {
    return;
  }

  const hints = [];
  collectHotReloadHints(rawData, hints);

  try {
    const parsed = JSON.parse(rawData);
    collectHotReloadHints(parsed, hints);
  } catch {
    // Not a JSON payload; raw string hint extraction already ran.
  }

  for (const hint of hints) {
    enqueueHotReloadHint(hint);
  }
}

export function installHotReloadHintBridge() {
  if (hotReloadHintBridgeInstalled || typeof window === "undefined" || typeof window.WebSocket !== "function") {
    return;
  }

  hotReloadHintBridgeInstalled = true;

  const NativeWebSocket = window.WebSocket;
  function WrappedWebSocket(...args) {
    const socket = new NativeWebSocket(...args);
    socket.addEventListener("message", (event) => {
      inspectHotReloadMessage(event.data);
    });
    return socket;
  }

  WrappedWebSocket.prototype = NativeWebSocket.prototype;
  Object.setPrototypeOf(WrappedWebSocket, NativeWebSocket);
  window.WebSocket = WrappedWebSocket;
}

export function drainHotReloadHintsJson() {
  if (hotReloadHints.length === 0) {
    return "[]";
  }

  const drained = hotReloadHints.splice(0, hotReloadHints.length);
  return JSON.stringify(drained);
}

function parseBooleanFlag(value) {
  if (typeof value !== "string") {
    return false;
  }

  switch (value.trim().toLowerCase()) {
    case "1":
    case "true":
    case "yes":
    case "on":
      return true;
    default:
      return false;
  }
}

export function isDiagnosticsEnabled() {
  if (typeof window === "undefined" || !window.location) {
    return false;
  }

  try {
    const query = new URLSearchParams(window.location.search ?? "");
    return parseBooleanFlag(query.get("mirageDiagnostics"));
  } catch {
    return false;
  }
}

export async function fetchText(path) {
  const result = await fetchTextWithStatus(path);
  if (!result || typeof result !== "object") {
    return null;
  }

  return result.ok ? result.text : null;
}

export async function fetchTextWithStatus(path) {
  if (typeof path !== "string" || path.length === 0) {
    return {
      ok: false,
      status: 0,
      text: null
    };
  }

  try {
    const response = await fetch(path, { cache: "no-store" });
    if (!response.ok) {
      return {
        ok: false,
        status: response.status,
        text: null
      };
    }

    return {
      ok: true,
      status: response.status,
      text: await response.text()
    };
  } catch {
    return {
      ok: false,
      status: 0,
      text: null
    };
  }
}

export async function fetchTextWithStatusJson(path) {
  const result = await fetchTextWithStatus(path);
  return JSON.stringify(result);
}

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
  ensureMirageFrameworkStyles();
  const normalizedTag = typeof tag === "string" ? tag.toLowerCase() : "";
  const element = isSvgTag(normalizedTag)
    ? document.createElementNS("http://www.w3.org/2000/svg", normalizedTag)
    : document.createElement(tag);
  const customElementConstructor = globalThis.customElements?.get?.(tag);
  if (
    customElementConstructor &&
    typeof HTMLUnknownElement !== "undefined" &&
    element instanceof HTMLUnknownElement
  ) {
    try {
      return toHandle(new customElementConstructor());
    } catch {
      // Fall back to the document-created element when constructor creation fails.
    }
  }

  return toHandle(element);
}

function isSvgTag(tag) {
  switch (tag) {
    case "svg":
    case "defs":
    case "path":
    case "lineargradient":
    case "radialgradient":
    case "stop":
    case "rect":
    case "circle":
    case "ellipse":
    case "polygon":
    case "polyline":
    case "line":
    case "g":
    case "clippath":
      return true;
    default:
      return false;
  }
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
  if (typeof name !== "string" || name.length === 0) {
    return;
  }

  const hasExplicitCssName = name.startsWith("--") || name.includes("-");
  if (hasExplicitCssName) {
    if (value == null || value === "") {
      element.style.removeProperty(name);
      return;
    }

    element.style.setProperty(name, value);
    return;
  }

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

export function setInputChecked(elementHandle, value) {
  const element = fromHandle(elementHandle);
  element.checked = Boolean(value);
}

export function getInputChecked(elementHandle) {
  const element = fromHandle(elementHandle);
  return Boolean(element.checked);
}

export function setScrollPosition(elementHandle, left, top) {
  const element = fromHandle(elementHandle);
  element.scrollLeft = Number.isFinite(left) ? Math.max(0, left) : 0;
  element.scrollTop = Number.isFinite(top) ? Math.max(0, top) : 0;
}

export function getScrollPosition(elementHandle) {
  const element = fromHandle(elementHandle);
  const left = Number.isFinite(element.scrollLeft) ? element.scrollLeft : 0;
  const top = Number.isFinite(element.scrollTop) ? element.scrollTop : 0;
  return [left, top];
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

function normalizeHistoryPath(path) {
  if (typeof path !== "string" || path.trim().length === 0) {
    return "/";
  }

  let normalized = path.trim();
  try {
    if (normalized.startsWith("http://") || normalized.startsWith("https://")) {
      const url = new URL(normalized);
      normalized = `${url.pathname || "/"}${url.search || ""}${url.hash || ""}`;
    }
  } catch {
    // Keep user-provided path when URL parsing fails.
  }

  if (!normalized.startsWith("/")) {
    normalized = `/${normalized}`;
  }

  return normalized;
}

export function getLocationPathAndQueryAndHash() {
  if (typeof window === "undefined" || !window.location) {
    return "/";
  }

  return `${window.location.pathname || "/"}${window.location.search || ""}${window.location.hash || ""}`;
}

export function pushHistoryState(path) {
  if (typeof window === "undefined" || !window.history) {
    return;
  }

  window.history.pushState(null, "", normalizeHistoryPath(path));
}

export function replaceHistoryState(path) {
  if (typeof window === "undefined" || !window.history) {
    return;
  }

  window.history.replaceState(null, "", normalizeHistoryPath(path));
}

export function getDocumentTitle() {
  if (typeof document === "undefined") {
    return "";
  }

  return document.title || "";
}

export function setDocumentTitle(title) {
  if (typeof document === "undefined") {
    return;
  }

  document.title = title ?? "";
}

const popStateSubscriptions = new Map();
let nextPopStateSubscriptionId = 1;

export function subscribePopState(callback) {
  if (typeof window === "undefined" || typeof callback !== "function") {
    return 0;
  }

  const listener = () => callback();
  const subscriptionId = nextPopStateSubscriptionId++;
  popStateSubscriptions.set(subscriptionId, listener);
  window.addEventListener("popstate", listener);
  return subscriptionId;
}

export function unsubscribePopState(subscriptionId) {
  const listener = popStateSubscriptions.get(subscriptionId);
  if (!listener || typeof window === "undefined") {
    return;
  }

  window.removeEventListener("popstate", listener);
  popStateSubscriptions.delete(subscriptionId);
}

function resolvePreferredColorScheme() {
  if (typeof window === "undefined" || typeof window.matchMedia !== "function") {
    return "light";
  }

  try {
    return window.matchMedia("(prefers-color-scheme: dark)").matches ? "dark" : "light";
  } catch {
    return "light";
  }
}

export function getPreferredColorScheme() {
  return resolvePreferredColorScheme();
}

const preferredColorSchemeSubscriptions = new Map();
let nextPreferredColorSchemeSubscriptionId = 1;

export function subscribePreferredColorSchemeChanged(callback) {
  if (typeof window === "undefined" || typeof window.matchMedia !== "function" || typeof callback !== "function") {
    return 0;
  }

  let mediaQuery;
  try {
    mediaQuery = window.matchMedia("(prefers-color-scheme: dark)");
  } catch {
    return 0;
  }

  const listener = () => callback();
  const subscriptionId = nextPreferredColorSchemeSubscriptionId++;
  preferredColorSchemeSubscriptions.set(subscriptionId, { mediaQuery, listener });

  if (typeof mediaQuery.addEventListener === "function") {
    mediaQuery.addEventListener("change", listener);
  } else if (typeof mediaQuery.addListener === "function") {
    mediaQuery.addListener(listener);
  }

  return subscriptionId;
}

export function unsubscribePreferredColorSchemeChanged(subscriptionId) {
  const subscription = preferredColorSchemeSubscriptions.get(subscriptionId);
  if (!subscription) {
    return;
  }

  const { mediaQuery, listener } = subscription;
  if (typeof mediaQuery.removeEventListener === "function") {
    mediaQuery.removeEventListener("change", listener);
  } else if (typeof mediaQuery.removeListener === "function") {
    mediaQuery.removeListener(listener);
  }

  preferredColorSchemeSubscriptions.delete(subscriptionId);
}
