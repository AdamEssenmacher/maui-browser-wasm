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

input.mirage-searchbar-input::placeholder {
  color: var(--mirage-searchbar-placeholder-color);
}

input.mirage-searchbar-input::-webkit-input-placeholder {
  color: var(--mirage-searchbar-placeholder-color);
}

[data-mirage-kind="Entry"] {
  display: flex;
  align-items: center;
  gap: 8px;
  box-sizing: border-box;
}

input.mirage-entry-input {
  flex: 1 1 auto;
  min-width: 0;
  box-sizing: border-box;
}

input.mirage-entry-input::placeholder {
  color: var(--mirage-entry-placeholder-color);
}

input.mirage-entry-input::-webkit-input-placeholder {
  color: var(--mirage-entry-placeholder-color);
}

[data-mirage-kind="Editor"] {
  display: flex;
  align-items: stretch;
  box-sizing: border-box;
}

 [data-mirage-kind="RadioButton"] {
  display: inline-flex;
  align-items: center;
  justify-content: flex-start;
  gap: 8px;
  box-sizing: border-box;
}

input.mirage-radiobutton-input {
  margin: 0;
  flex: 0 0 auto;
}

.mirage-radiobutton-content {
  display: inline-flex;
  align-items: center;
  min-width: 0;
  flex: 1 1 auto;
}

.mirage-radiobutton-text {
  line-height: 1.3;
  white-space: normal;
}

textarea.mirage-editor-input {
  flex: 1 1 auto;
  width: 100%;
  min-width: 0;
  box-sizing: border-box;
  resize: none;
}

textarea.mirage-editor-input::placeholder {
  color: var(--mirage-editor-placeholder-color);
}

textarea.mirage-editor-input::-webkit-input-placeholder {
  color: var(--mirage-editor-placeholder-color);
}

button.mirage-entry-clear {
  white-space: nowrap;
  border: 0;
  background: transparent;
}

button.mirage-entry-clear:focus-visible {
  outline: 2px solid rgba(59,130,246,0.9);
  outline-offset: 2px;
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

[data-mirage-kind="IndicatorView"] {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  box-sizing: border-box;
}

[data-mirage-kind="IndicatorView"] .mirage-indicator-dot {
  appearance: none;
  -webkit-appearance: none;
  box-sizing: border-box;
  border: 0;
  border-radius: 999px;
  padding: 0;
  margin: 0;
  background: rgba(148,163,184,0.55);
  cursor: pointer;
  user-select: none;
}

[data-mirage-kind="IndicatorView"] .mirage-indicator-dot:focus-visible {
  outline: 2px solid rgba(59,130,246,0.78);
  outline-offset: 2px;
}

[data-mirage-kind="IndicatorView"] .mirage-indicator-dot[disabled] {
  cursor: default;
}

[data-mirage-kind="IndicatorView"] .mirage-indicator-dot[data-mirage-indicator-selected="true"] {
  opacity: 1;
}

[data-mirage-kind="ShapeView"] {
  position: relative;
  overflow: hidden;
  min-width: 0;
  min-height: 0;
}

[data-mirage-kind="ShapeView"] .mirage-shape-svg {
  position: absolute;
  inset: 0;
  width: 100%;
  height: 100%;
  overflow: visible;
  pointer-events: none;
}

[data-mirage-kind="ShapeView"] .mirage-shape-path {
  vector-effect: none;
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

function decodeBase64ToUint8Array(base64) {
  if (typeof base64 !== "string" || base64.length === 0) {
    return null;
  }

  try {
    const binary = atob(base64);
    const bytes = new Uint8Array(binary.length);
    for (let index = 0; index < binary.length; index += 1) {
      bytes[index] = binary.charCodeAt(index);
    }

    return bytes;
  } catch {
    return null;
  }
}

export function createObjectUrlFromBase64(base64, contentType) {
  if (typeof Blob === "undefined" || typeof URL === "undefined" || typeof URL.createObjectURL !== "function") {
    return null;
  }

  const bytes = decodeBase64ToUint8Array(base64);
  if (!bytes || bytes.length === 0) {
    return null;
  }

  const normalizedContentType = typeof contentType === "string" && contentType.trim().length > 0
    ? contentType.trim()
    : "application/octet-stream";

  const blob = new Blob([bytes], { type: normalizedContentType });
  return URL.createObjectURL(blob);
}

export function revokeObjectUrl(objectUrl) {
  if (
    typeof objectUrl !== "string"
    || objectUrl.length === 0
    || typeof URL === "undefined"
    || typeof URL.revokeObjectURL !== "function"
  ) {
    return;
  }

  try {
    URL.revokeObjectURL(objectUrl);
  } catch {
    // best effort cleanup
  }
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
const trackedEventPayloads = new Map();
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

  const trackedPayloadPrefix = `${handle}:`;
  for (const key of Array.from(trackedEventPayloads.keys())) {
    if (key.startsWith(trackedPayloadPrefix)) {
      trackedEventPayloads.delete(key);
    }
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

export function getAttr(elementHandle, name) {
  const element = fromHandle(elementHandle);
  if (typeof name !== "string" || name.length === 0) {
    return null;
  }

  return element.getAttribute(name);
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

function removePropertyFromElement(element, name) {
  if (!name) {
    return;
  }

  let prototype = Object.getPrototypeOf(element);
  let isPrototypeBackedProperty = false;
  while (prototype) {
    if (Object.prototype.hasOwnProperty.call(prototype, name)) {
      isPrototypeBackedProperty = true;
      break;
    }

    prototype = Object.getPrototypeOf(prototype);
  }

  if (!isPrototypeBackedProperty) {
    delete element[name];
    return;
  }

  const current = element[name];
  try {
    if (typeof current === "string") {
      element[name] = "";
      return;
    }

    if (typeof current === "boolean") {
      element[name] = false;
      return;
    }

    if (typeof current === "number") {
      element[name] = 0;
      return;
    }

    element[name] = null;
  } catch {
    delete element[name];
  }
}

export function removeProperty(elementHandle, name) {
  const element = fromHandle(elementHandle);
  removePropertyFromElement(element, name);
}

function applyPropertyMutation(element, mutation) {
  if (!element || !mutation || typeof mutation.name !== "string" || mutation.name.length === 0) {
    return;
  }

  switch (mutation.kind) {
    case "string":
      element[mutation.name] = mutation.stringValue ?? "";
      return;
    case "bool":
      element[mutation.name] = !!mutation.boolValue;
      return;
    case "number":
      element[mutation.name] = Number.isFinite(mutation.numberValue) ? mutation.numberValue : 0;
      return;
    case "json":
      if (mutation.stringValue == null || mutation.stringValue.length === 0) {
        element[mutation.name] = null;
        return;
      }

      try {
        element[mutation.name] = JSON.parse(mutation.stringValue);
      } catch {
        element[mutation.name] = null;
      }
      return;
    case "null":
      element[mutation.name] = null;
      return;
    case "remove":
      removePropertyFromElement(element, mutation.name);
      return;
    case "input-value":
      element.value = mutation.stringValue ?? "";
      return;
    case "input-checked":
      element.checked = !!mutation.boolValue;
      return;
    default:
      return;
  }
}

function applyMutationOperation(element, operation) {
  if (!element || !operation || typeof operation.kind !== "string" || operation.kind.length === 0) {
    return;
  }

  switch (operation.kind) {
    case "text":
      element.textContent = operation.stringValue ?? "";
      return;
    case "attribute":
      if (typeof operation.name !== "string" || operation.name.length === 0) {
        return;
      }

      if (operation.stringValue == null) {
        element.removeAttribute(operation.name);
      } else {
        element.setAttribute(operation.name, operation.stringValue);
      }
      return;
    case "style":
      if (typeof operation.name !== "string" || operation.name.length === 0) {
        return;
      }

      {
        const hasExplicitCssName = operation.name.startsWith("--") || operation.name.includes("-");
        if (hasExplicitCssName) {
          if (operation.stringValue == null || operation.stringValue === "") {
            element.style.removeProperty(operation.name);
          } else {
            element.style.setProperty(operation.name, operation.stringValue);
          }
          return;
        }

        element.style[operation.name] = operation.stringValue ?? "";
        return;
      }
    case "class-add":
      if (operation.name) {
        element.classList.add(operation.name);
      }
      return;
    case "class-remove":
      if (operation.name) {
        element.classList.remove(operation.name);
      }
      return;
    case "property-string":
      applyPropertyMutation(element, { name: operation.name, kind: "string", stringValue: operation.stringValue });
      return;
    case "property-bool":
      applyPropertyMutation(element, { name: operation.name, kind: "bool", boolValue: operation.boolValue });
      return;
    case "property-number":
      applyPropertyMutation(element, { name: operation.name, kind: "number", numberValue: operation.numberValue });
      return;
    case "property-json":
      applyPropertyMutation(element, { name: operation.name, kind: "json", stringValue: operation.stringValue });
      return;
    case "property-null":
      applyPropertyMutation(element, { name: operation.name, kind: "null" });
      return;
    case "property-remove":
      applyPropertyMutation(element, { name: operation.name, kind: "remove" });
      return;
    case "property-input-value":
      applyPropertyMutation(element, { name: operation.name, kind: "input-value", stringValue: operation.stringValue });
      return;
    case "property-input-checked":
      applyPropertyMutation(element, { name: operation.name, kind: "input-checked", boolValue: operation.boolValue });
      return;
    default:
      return;
  }
}

export function applyMutationBatch(batchJson) {
  if (typeof batchJson !== "string" || batchJson.length === 0) {
    return;
  }

  let payload;
  try {
    payload = JSON.parse(batchJson);
  } catch {
    return;
  }

  const elements = Array.isArray(payload?.elements) ? payload.elements : [];
  for (const mutation of elements) {
    if (!mutation || !Number.isInteger(mutation.handle)) {
      continue;
    }

    const element = elementByHandle.get(mutation.handle);
    if (!element) {
      continue;
    }

    if (Array.isArray(mutation.operations)) {
      for (const operation of mutation.operations) {
        applyMutationOperation(element, operation);
      }
    }
  }
}

export function subscribeEvent(elementHandle, eventName, callback) {
  const element = fromHandle(elementHandle);
  const listener = () => callback();
  const subscriptionId = nextEventSubscriptionId++;
  eventSubscriptions.set(subscriptionId, { element, eventName, listener });
  element.addEventListener(eventName, listener);
  return subscriptionId;
}

function getEventPayload(event, payloadKind, fallbackElement) {
  if (payloadKind === "key") {
    if (!event || typeof event !== "object") {
      return null;
    }

    return typeof event.key === "string" && event.key.length > 0
      ? event.key
      : null;
  }

  if (payloadKind !== "source-attribute") {
    return null;
  }

  const eventTarget = (event && typeof event === "object" ? event.currentTarget : null) ?? fallbackElement;
  if (!eventTarget) {
    return null;
  }

  if (typeof eventTarget.getAttribute === "function") {
    const attributeValue = eventTarget.getAttribute("src");
    if (typeof attributeValue === "string" && attributeValue.length > 0) {
      return attributeValue;
    }
  }

  if (typeof eventTarget.currentSrc === "string" && eventTarget.currentSrc.length > 0) {
    return eventTarget.currentSrc;
  }

  return null;
}

function setTrackedEventPayload(elementHandle, payloadKind, payload) {
  if (!Number.isInteger(elementHandle) || typeof payloadKind !== "string" || payloadKind.length === 0) {
    return;
  }

  const key = `${elementHandle}:${payloadKind}`;
  trackedEventPayloads.set(key, typeof payload === "string" && payload.length > 0 ? payload : null);
}

export function consumeTrackedEventPayload(elementHandle, payloadKind) {
  if (!Number.isInteger(elementHandle) || typeof payloadKind !== "string" || payloadKind.length === 0) {
    return null;
  }

  const key = `${elementHandle}:${payloadKind}`;
  if (!trackedEventPayloads.has(key)) {
    return null;
  }

  const payload = trackedEventPayloads.get(key);
  trackedEventPayloads.delete(key);
  return typeof payload === "string" ? payload : null;
}

export function subscribeEventWithPayload(elementHandle, eventName, payloadKind, callback) {
  const element = fromHandle(elementHandle);
  const listener = (event) => {
    setTrackedEventPayload(elementHandle, payloadKind, getEventPayload(event, payloadKind, element));
    callback();
  };
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

export function getInputSelection(elementHandle) {
  const element = fromHandle(elementHandle);
  try {
    const start = element.selectionStart;
    const end = element.selectionEnd;
    if (!Number.isInteger(start) || !Number.isInteger(end)) {
      return [];
    }

    return [Math.max(0, start), Math.max(0, end)];
  } catch {
    return [];
  }
}

export function setInputSelection(elementHandle, selectionStart, selectionEnd) {
  const element = fromHandle(elementHandle);
  if (!element || typeof element.setSelectionRange !== "function") {
    return false;
  }

  const normalizedStart = Number.isFinite(selectionStart) ? Math.max(0, Math.trunc(selectionStart)) : 0;
  const normalizedEnd = Number.isFinite(selectionEnd) ? Math.max(normalizedStart, Math.trunc(selectionEnd)) : normalizedStart;
  try {
    element.setSelectionRange(normalizedStart, normalizedEnd);
    return true;
  } catch {
    return false;
  }
}

export function focusElement(elementHandle) {
  const element = fromHandle(elementHandle);
  if (!element || typeof element.focus !== "function") {
    return false;
  }

  try {
    element.focus();
    return document.activeElement === element;
  } catch {
    return false;
  }
}

export function blurElement(elementHandle) {
  const element = fromHandle(elementHandle);
  if (!element || typeof element.blur !== "function") {
    return;
  }

  try {
    element.blur();
  } catch {
    // no-op: deterministic failure behavior lives at handler/runtime layer
  }
}

export function setScrollPosition(elementHandle, left, top) {
  const element = fromHandle(elementHandle);
  const viewportWidth = Number.isFinite(element.clientWidth) ? element.clientWidth : 0;
  const maxLeft = Math.max(0, (Number.isFinite(element.scrollWidth) ? element.scrollWidth : 0) - viewportWidth);
  const normalizedLeft = Number.isFinite(left)
    ? Math.min(Math.max(0, left), maxLeft)
    : 0;
  element.scrollLeft = toRawScrollLeft(element, normalizedLeft, maxLeft);
  element.scrollTop = Number.isFinite(top) ? Math.max(0, top) : 0;
}

export function getScrollPosition(elementHandle) {
  const element = fromHandle(elementHandle);
  const viewportWidth = Number.isFinite(element.clientWidth) ? element.clientWidth : 0;
  const maxLeft = Math.max(0, (Number.isFinite(element.scrollWidth) ? element.scrollWidth : 0) - viewportWidth);
  const left = readLogicalScrollLeft(element, maxLeft);
  const top = Number.isFinite(element.scrollTop) ? element.scrollTop : 0;
  return [left, top];
}

export function scrollChildIntoView(containerHandle, childHandle, scrollToPosition, isHorizontalOrientation, isAnimated) {
  const container = fromHandle(containerHandle);
  const child = fromHandle(childHandle);

  const containerRect = container.getBoundingClientRect();
  const childRect = child.getBoundingClientRect();
  const viewportWidth = Number.isFinite(container.clientWidth) ? container.clientWidth : 0;
  const maxLeft = Math.max(0, (Number.isFinite(container.scrollWidth) ? container.scrollWidth : 0) - viewportWidth);
  const currentLeft = readLogicalScrollLeft(container, maxLeft);
  const currentTop = Number.isFinite(container.scrollTop) ? container.scrollTop : 0;

  const relativeLeft = childRect.left - containerRect.left + currentLeft;
  const relativeRight = childRect.right - containerRect.left + currentLeft;
  const relativeTop = childRect.top - containerRect.top + currentTop;
  const relativeBottom = childRect.bottom - containerRect.top + currentTop;
  const childWidth = Number.isFinite(childRect.width) ? childRect.width : 0;
  const childHeight = Number.isFinite(childRect.height) ? childRect.height : 0;
  const viewportHeight = Number.isFinite(container.clientHeight) ? container.clientHeight : 0;

  let targetLeft = currentLeft;
  let targetTop = currentTop;

  const horizontal = Boolean(isHorizontalOrientation);

  switch (scrollToPosition) {
    // ScrollToPosition.Start
    case 1:
      if (horizontal) {
        targetLeft = relativeLeft;
      } else {
        targetTop = relativeTop;
      }
      break;
    // ScrollToPosition.Center
    case 2:
      if (horizontal) {
        targetLeft = relativeLeft - ((viewportWidth - childWidth) / 2);
      } else {
        targetTop = relativeTop - ((viewportHeight - childHeight) / 2);
      }
      break;
    // ScrollToPosition.End
    case 3:
      if (horizontal) {
        targetLeft = relativeRight - viewportWidth;
      } else {
        targetTop = relativeBottom - viewportHeight;
      }
      break;
    // ScrollToPosition.MakeVisible
    case 0:
    default:
      if (horizontal) {
        if (relativeLeft < currentLeft) {
          targetLeft = relativeLeft;
        } else if (relativeRight > currentLeft + viewportWidth) {
          targetLeft = relativeRight - viewportWidth;
        }
      } else if (relativeTop < currentTop) {
        targetTop = relativeTop;
      } else if (relativeBottom > currentTop + viewportHeight) {
        targetTop = relativeBottom - viewportHeight;
      }

      break;
  }

  const maxTop = Math.max(0, (Number.isFinite(container.scrollHeight) ? container.scrollHeight : 0) - viewportHeight);
  const normalizedLeft = Number.isFinite(targetLeft)
    ? Math.min(Math.max(0, targetLeft), maxLeft)
    : 0;
  const normalizedTop = Number.isFinite(targetTop)
    ? Math.min(Math.max(0, targetTop), maxTop)
    : 0;
  const rawLeft = toRawScrollLeft(container, normalizedLeft, maxLeft);

  if (Boolean(isAnimated) && typeof container.scrollTo === "function") {
    container.scrollTo({ left: rawLeft, top: normalizedTop, behavior: "smooth" });
  } else {
    container.scrollLeft = rawLeft;
    container.scrollTop = normalizedTop;
  }
  const actualLeft = readLogicalScrollLeft(container, maxLeft);
  const actualTop = Number.isFinite(container.scrollTop) ? container.scrollTop : normalizedTop;
  return [actualLeft, actualTop];
}

let cachedRtlScrollType;

function resolveRtlScrollType() {
  if (cachedRtlScrollType) {
    return cachedRtlScrollType;
  }

  if (typeof document === "undefined" || !document.body) {
    cachedRtlScrollType = "default";
    return cachedRtlScrollType;
  }

  const probe = document.createElement("div");
  probe.dir = "rtl";
  probe.style.width = "4px";
  probe.style.height = "1px";
  probe.style.overflow = "scroll";
  probe.style.position = "absolute";
  probe.style.top = "-9999px";

  const content = document.createElement("div");
  content.style.width = "8px";
  content.style.height = "1px";
  probe.appendChild(content);
  document.body.appendChild(probe);

  if (probe.scrollLeft > 0) {
    cachedRtlScrollType = "default";
  } else {
    probe.scrollLeft = 1;
    cachedRtlScrollType = probe.scrollLeft === 0 ? "negative" : "reverse";
  }

  probe.remove();
  return cachedRtlScrollType;
}

function isRtlDirection(element) {
  if (!element || typeof window === "undefined" || typeof window.getComputedStyle !== "function") {
    return false;
  }

  return window.getComputedStyle(element).direction === "rtl";
}

function readLogicalScrollLeft(element, maxLeft) {
  const rawLeft = Number.isFinite(element.scrollLeft) ? element.scrollLeft : 0;
  const cappedMaxLeft = Number.isFinite(maxLeft) ? Math.max(0, maxLeft) : 0;
  if (!isRtlDirection(element)) {
    return Math.min(Math.max(0, rawLeft), cappedMaxLeft);
  }

  switch (resolveRtlScrollType()) {
    case "negative":
      return Math.min(Math.max(0, cappedMaxLeft + rawLeft), cappedMaxLeft);
    case "reverse":
      return Math.min(Math.max(0, cappedMaxLeft - rawLeft), cappedMaxLeft);
    case "default":
    default:
      return Math.min(Math.max(0, rawLeft), cappedMaxLeft);
  }
}

function toRawScrollLeft(element, logicalLeft, maxLeft) {
  const cappedMaxLeft = Number.isFinite(maxLeft) ? Math.max(0, maxLeft) : 0;
  const normalizedLeft = Number.isFinite(logicalLeft)
    ? Math.min(Math.max(0, logicalLeft), cappedMaxLeft)
    : 0;
  if (!isRtlDirection(element)) {
    return normalizedLeft;
  }

  switch (resolveRtlScrollType()) {
    case "negative":
      return normalizedLeft - cappedMaxLeft;
    case "reverse":
      return cappedMaxLeft - normalizedLeft;
    case "default":
    default:
      return normalizedLeft;
  }
}

const resizeSubscriptions = new Map();
let nextResizeSubscriptionId = 1;

export function subscribeWindowResize(callback) {
  const id = nextResizeSubscriptionId++;
  let frameId = 0;
  let scheduled = false;
  const listener = () => {
    if (scheduled) {
      return;
    }

    scheduled = true;
    frameId = window.requestAnimationFrame(() => {
      scheduled = false;
      frameId = 0;
      callback();
    });
  };
  resizeSubscriptions.set(id, {
    listener,
    cancel() {
      scheduled = false;
      if (frameId !== 0) {
        window.cancelAnimationFrame(frameId);
        frameId = 0;
      }
    }
  });
  window.addEventListener("resize", listener);
  return id;
}

export function unsubscribeWindowResize(subscriptionId) {
  const entry = resizeSubscriptions.get(subscriptionId);
  if (!entry) {
    return;
  }

  entry.cancel();
  window.removeEventListener("resize", entry.listener);
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

function tryGetLocalStorage() {
  if (typeof window === "undefined") {
    return null;
  }

  try {
    return window.localStorage ?? null;
  } catch {
    return null;
  }
}

export function getLocalStorageItem(key) {
  const storage = tryGetLocalStorage();
  if (!storage || typeof key !== "string" || key.length === 0) {
    return null;
  }

  try {
    return storage.getItem(key);
  } catch {
    return null;
  }
}

export function setLocalStorageItem(key, value) {
  const storage = tryGetLocalStorage();
  if (!storage || typeof key !== "string" || key.length === 0) {
    return;
  }

  try {
    storage.setItem(key, value ?? "");
  } catch {
    // Ignore storage write failures so callers can fall back deterministically.
  }
}

export function removeLocalStorageItem(key) {
  const storage = tryGetLocalStorage();
  if (!storage || typeof key !== "string" || key.length === 0) {
    return;
  }

  try {
    storage.removeItem(key);
  } catch {
    // Ignore storage removal failures.
  }
}

export function containsLocalStorageKey(key) {
  const storage = tryGetLocalStorage();
  if (!storage || typeof key !== "string" || key.length === 0) {
    return false;
  }

  try {
    return storage.getItem(key) !== null;
  } catch {
    return false;
  }
}

export function clearLocalStorageByPrefix(prefix) {
  const storage = tryGetLocalStorage();
  if (!storage || typeof prefix !== "string" || prefix.length === 0) {
    return;
  }

  try {
    const keys = [];
    for (let index = 0; index < storage.length; index++) {
      const key = storage.key(index);
      if (typeof key === "string" && key.startsWith(prefix)) {
        keys.push(key);
      }
    }

    for (const key of keys) {
      storage.removeItem(key);
    }
  } catch {
    // Ignore scoped clear failures.
  }
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
