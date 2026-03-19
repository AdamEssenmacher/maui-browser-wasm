const mapEntries = new Map();
let leafletReadinessPromise = null;
let leafletReadinessResult = null;

function toResult(ok, errorCode = null, errorMessage = null, warningCode = null, warningMessage = null) {
  return { ok, errorCode, errorMessage, warningCode, warningMessage };
}

function toResultJson(result) {
  return JSON.stringify(result ?? toResult(false, "invalid-result", "Invalid result payload."));
}

function parseJson(json, fallbackValue) {
  if (typeof json !== "string" || json.length === 0) {
    return fallbackValue;
  }

  try {
    return JSON.parse(json);
  } catch {
    return fallbackValue;
  }
}

function escapeAttributeValue(value) {
  if (typeof value !== "string" || value.length === 0) {
    return "";
  }

  return value.replace(/\\/g, "\\\\").replace(/"/g, "\\\"");
}

function findMapHost(nodeId) {
  if (typeof document === "undefined" || !document.querySelector) {
    return null;
  }

  if (typeof nodeId !== "string" || nodeId.length === 0) {
    return null;
  }

  const escapedNodeId = escapeAttributeValue(nodeId);
  const selector = `[data-mirage-node-id="${escapedNodeId}"][data-mirage-kind="Map"]`;
  return document.querySelector(selector);
}

function normalizeTimeoutMs(value) {
  if (!Number.isFinite(value)) {
    return 10000;
  }

  return Math.max(1000, Math.min(60000, Math.floor(value)));
}

function ensureStylesheet(url, timeoutMs) {
  return new Promise((resolve) => {
    if (typeof document === "undefined" || !document.head) {
      resolve(toResult(false, "css-host-unavailable", "Document head is unavailable for Leaflet CSS loading."));
      return;
    }

    if (typeof url !== "string" || url.length === 0) {
      resolve(toResult(false, "css-url-missing", "Leaflet CSS URL is required."));
      return;
    }

    const existing = document.querySelector(`link[data-mirage-leaflet-css="true"][href="${escapeAttributeValue(url)}"]`);
    if (existing) {
      const existingStatus = existing.getAttribute("data-mirage-leaflet-css-status");
      if (existingStatus === "loaded" || existing.sheet) {
        resolve(toResult(true));
        return;
      }

      if (existingStatus === "error") {
        resolve(toResult(false, "css-load-failed", `Failed to load Leaflet CSS from '${url}'.`));
        return;
      }

      let completed = false;
      const onComplete = (result) => {
        if (completed) {
          return;
        }

        completed = true;
        clearTimeout(timer);
        existing.removeEventListener("load", onLoad);
        existing.removeEventListener("error", onError);
        resolve(result);
      };

      const onLoad = () => {
        existing.setAttribute("data-mirage-leaflet-css-status", "loaded");
        onComplete(toResult(true));
      };

      const onError = () => {
        existing.setAttribute("data-mirage-leaflet-css-status", "error");
        onComplete(toResult(false, "css-load-failed", `Failed to load Leaflet CSS from '${url}'.`));
      };

      const timer = setTimeout(() => {
        if (existing.sheet) {
          onLoad();
          return;
        }

        onComplete(toResult(false, "css-timeout", `Timed out while waiting for Leaflet CSS from '${url}'.`));
      }, timeoutMs);

      existing.addEventListener("load", onLoad);
      existing.addEventListener("error", onError);

      if (existing.sheet) {
        queueMicrotask(onLoad);
      }

      return;
    }

    const link = document.createElement("link");
    link.rel = "stylesheet";
    link.href = url;
    link.setAttribute("data-mirage-leaflet-css", "true");
    link.setAttribute("data-mirage-leaflet-css-status", "loading");

    let completed = false;
    const timer = setTimeout(() => {
      if (completed) {
        return;
      }

      completed = true;
      resolve(toResult(false, "css-timeout", `Timed out while loading Leaflet CSS from '${url}'.`));
    }, timeoutMs);

    link.onload = () => {
      link.setAttribute("data-mirage-leaflet-css-status", "loaded");
      if (completed) {
        return;
      }

      completed = true;
      clearTimeout(timer);
      resolve(toResult(true));
    };

    link.onerror = () => {
      link.setAttribute("data-mirage-leaflet-css-status", "error");
      if (completed) {
        return;
      }

      completed = true;
      clearTimeout(timer);
      resolve(toResult(false, "css-load-failed", `Failed to load Leaflet CSS from '${url}'.`));
    };

    document.head.appendChild(link);
  });
}

function ensureScript(url, timeoutMs) {
  return new Promise((resolve) => {
    if (typeof document === "undefined" || !document.head) {
      resolve(toResult(false, "js-host-unavailable", "Document head is unavailable for Leaflet script loading."));
      return;
    }

    if (typeof url !== "string" || url.length === 0) {
      resolve(toResult(false, "js-url-missing", "Leaflet JavaScript URL is required."));
      return;
    }

    const existing = document.querySelector(`script[data-mirage-leaflet-js="true"][src="${escapeAttributeValue(url)}"]`);
    if (existing) {
      if (typeof window !== "undefined" && window.L && typeof window.L.map === "function") {
        resolve(toResult(true));
        return;
      }

      let completed = false;
      const onComplete = (result) => {
        if (completed) {
          return;
        }

        completed = true;
        clearTimeout(timer);
        existing.removeEventListener("load", onLoad);
        existing.removeEventListener("error", onError);
        resolve(result);
      };

      const onLoad = () => {
        if (typeof window !== "undefined" && window.L && typeof window.L.map === "function") {
          onComplete(toResult(true));
          return;
        }

        onComplete(toResult(false, "leaflet-global-missing", "Leaflet script loaded but global 'L' is unavailable."));
      };

      const onError = () => {
        onComplete(toResult(false, "js-load-failed", `Failed to load Leaflet JavaScript from '${url}'.`));
      };

      const timer = setTimeout(() => {
        if (typeof window !== "undefined" && window.L && typeof window.L.map === "function") {
          onComplete(toResult(true));
          return;
        }

        onComplete(toResult(false, "js-timeout", `Timed out while waiting for Leaflet JavaScript from '${url}'.`));
      }, timeoutMs);

      existing.addEventListener("load", onLoad);
      existing.addEventListener("error", onError);

      if (existing.readyState === "complete" || existing.readyState === "loaded") {
        queueMicrotask(onLoad);
      }

      return;
    }

    const script = document.createElement("script");
    script.src = url;
    script.async = true;
    script.defer = true;
    script.setAttribute("data-mirage-leaflet-js", "true");

    let completed = false;
    const timer = setTimeout(() => {
      if (completed) {
        return;
      }

      completed = true;
      resolve(toResult(false, "js-timeout", `Timed out while loading Leaflet JavaScript from '${url}'.`));
    }, timeoutMs);

    script.onload = () => {
      if (completed) {
        return;
      }

      completed = true;
      clearTimeout(timer);
      resolve(toResult(true));
    };

    script.onerror = () => {
      if (completed) {
        return;
      }

      completed = true;
      clearTimeout(timer);
      resolve(toResult(false, "js-load-failed", `Failed to load Leaflet JavaScript from '${url}'.`));
    };

    document.head.appendChild(script);
  });
}

async function ensureLeafletReadyCore(options) {
  if (typeof window !== "undefined" && window.L && typeof window.L.map === "function") {
    return toResult(true);
  }

  const timeoutMs = normalizeTimeoutMs(options?.readinessTimeoutMs);
  const cssResult = await ensureStylesheet(options?.stylesheetUrl ?? "", timeoutMs);
  if (!cssResult.ok) {
    return cssResult;
  }

  const jsResult = await ensureScript(options?.javaScriptUrl ?? "", timeoutMs);
  if (!jsResult.ok) {
    return jsResult;
  }

  if (typeof window === "undefined" || !window.L || typeof window.L.map !== "function") {
    return toResult(false, "leaflet-global-missing", "Leaflet global 'L' is unavailable after script load.");
  }

  return toResult(true);
}

export async function ensureLeafletReadyJson(optionsJson) {
  if (leafletReadinessResult && leafletReadinessResult.ok) {
    return toResultJson(leafletReadinessResult);
  }

  if (!leafletReadinessPromise) {
    const options = parseJson(optionsJson, {});
    leafletReadinessPromise = ensureLeafletReadyCore(options)
      .then((result) => {
        leafletReadinessResult = result;
        if (!result.ok) {
          leafletReadinessPromise = null;
        }

        return result;
      })
      .catch((error) => {
        const message = error && typeof error.message === "string" && error.message.length > 0
          ? error.message
          : "Leaflet readiness failed with an unexpected exception.";
        const failedResult = toResult(false, "readiness-exception", message);
        leafletReadinessResult = failedResult;
        leafletReadinessPromise = null;
        return failedResult;
      });
  }

  const result = await leafletReadinessPromise;
  return toResultJson(result);
}

function resolveRuntimeOptions(optionsJson) {
  const options = parseJson(optionsJson, {});
  return {
    tileLayerUrlTemplate: typeof options.tileLayerUrlTemplate === "string" && options.tileLayerUrlTemplate.length > 0
      ? options.tileLayerUrlTemplate
      : "https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png",
    tileLayerAttribution: typeof options.tileLayerAttribution === "string"
      ? options.tileLayerAttribution
      : "&copy; OpenStreetMap contributors",
    tileLayerMinZoom: Number.isFinite(options.tileLayerMinZoom) ? Math.max(0, Math.floor(options.tileLayerMinZoom)) : 1,
    tileLayerMaxZoom: Number.isFinite(options.tileLayerMaxZoom) ? Math.max(1, Math.floor(options.tileLayerMaxZoom)) : 19
  };
}

function clearMapError(host) {
  if (!host) {
    return;
  }

  host.setAttribute("data-mirage-map-status", "ready");
  host.removeAttribute("data-mirage-map-error-code");
  host.removeAttribute("data-mirage-map-error");
}

function emitMirageEvent(host, eventName, payload) {
  if (!host || typeof eventName !== "string" || eventName.length === 0) {
    return;
  }

  try {
    host.value = JSON.stringify(payload ?? {});
  } catch {
    host.value = "{}";
  }

  host.dispatchEvent(new CustomEvent(eventName));
}

function emitDiagnostic(host, code, message) {
  if (!host) {
    return;
  }

  host.setAttribute("data-mirage-map-last-diagnostic-code", typeof code === "string" ? code : "provider-diagnostic");
  host.setAttribute("data-mirage-map-last-diagnostic-message", typeof message === "string" ? message : "Map provider diagnostic.");
  emitMirageEvent(host, "mirage-map-diagnostic", {
    provider: "leaflet",
    code: typeof code === "string" ? code : "provider-diagnostic",
    message: typeof message === "string" ? message : "Map provider diagnostic."
  });
}

function toFiniteOrNull(value) {
  if (!Number.isFinite(value)) {
    return null;
  }

  return Number(value);
}

function normalizeLongitude(value) {
  let normalized = value;
  while (normalized < -180) {
    normalized += 360;
  }

  while (normalized > 180) {
    normalized -= 360;
  }

  return normalized;
}

function normalizeRegion(region) {
  const centerLatitude = toFiniteOrNull(region?.centerLatitude);
  const centerLongitude = toFiniteOrNull(region?.centerLongitude);
  const latitudeSpan = toFiniteOrNull(region?.latitudeSpan);
  const longitudeSpan = toFiniteOrNull(region?.longitudeSpan);
  if (centerLatitude == null || centerLongitude == null || latitudeSpan == null || longitudeSpan == null) {
    return null;
  }

  return {
    centerLatitude: Math.max(-90, Math.min(90, centerLatitude)),
    centerLongitude: normalizeLongitude(centerLongitude),
    latitudeSpan: Math.max(0.0001, Math.abs(latitudeSpan)),
    longitudeSpan: Math.max(0.0001, Math.abs(longitudeSpan))
  };
}

function getLongitudeSpanFromBounds(southWestLongitude, northEastLongitude) {
  const southWest = toFiniteOrNull(southWestLongitude);
  const northEast = toFiniteOrNull(northEastLongitude);
  if (southWest == null || northEast == null) {
    return 0.0001;
  }

  let span = northEast - southWest;
  while (span < 0) {
    span += 360;
  }

  return Math.max(0.0001, Math.min(360, span));
}

function approximatelyEqual(left, right, absoluteTolerance, relativeTolerance = 0.002) {
  const diff = Math.abs(left - right);
  if (diff <= absoluteTolerance) {
    return true;
  }

  const scale = Math.max(Math.abs(left), Math.abs(right), 1);
  return diff <= (scale * relativeTolerance);
}

function areRegionsEquivalent(left, right) {
  if (!left || !right) {
    return false;
  }

  return approximatelyEqual(left.centerLatitude, right.centerLatitude, 0.0001, 0.001)
    && approximatelyEqual(left.centerLongitude, right.centerLongitude, 0.0001, 0.001)
    && approximatelyEqual(left.latitudeSpan, right.latitudeSpan, 0.0005, 0.01)
    && approximatelyEqual(left.longitudeSpan, right.longitudeSpan, 0.0005, 0.01);
}

function toBoolean(value, defaultValue) {
  if (typeof value === "boolean") {
    return value;
  }

  if (typeof value === "string") {
    const normalized = value.trim().toLowerCase();
    if (normalized === "true" || normalized === "1" || normalized === "yes" || normalized === "on") {
      return true;
    }

    if (normalized === "false" || normalized === "0" || normalized === "no" || normalized === "off") {
      return false;
    }
  }

  return !!defaultValue;
}

function setRegionTelemetry(host, region) {
  if (!host) {
    return;
  }

  if (!region) {
    host.removeAttribute("data-mirage-map-center-lat");
    host.removeAttribute("data-mirage-map-center-lng");
    host.removeAttribute("data-mirage-map-lat-span");
    host.removeAttribute("data-mirage-map-lng-span");
    return;
  }

  host.setAttribute("data-mirage-map-center-lat", String(region.centerLatitude));
  host.setAttribute("data-mirage-map-center-lng", String(region.centerLongitude));
  host.setAttribute("data-mirage-map-lat-span", String(region.latitudeSpan));
  host.setAttribute("data-mirage-map-lng-span", String(region.longitudeSpan));
}

function readRegionFromMap(map) {
  if (!map || typeof map.getCenter !== "function" || typeof map.getBounds !== "function") {
    return null;
  }

  try {
    const center = map.getCenter();
    const bounds = map.getBounds();
    if (!center || !bounds || typeof bounds.getSouthWest !== "function" || typeof bounds.getNorthEast !== "function") {
      return null;
    }

    const southWest = bounds.getSouthWest();
    const northEast = bounds.getNorthEast();
    if (!southWest || !northEast) {
      return null;
    }

    const latitudeSpan = Math.max(0.0001, Math.abs(northEast.lat - southWest.lat));
    const longitudeSpan = getLongitudeSpanFromBounds(southWest.lng, northEast.lng);
    return {
      centerLatitude: center.lat,
      centerLongitude: center.lng,
      latitudeSpan,
      longitudeSpan
    };
  } catch {
    return null;
  }
}

function clearScheduledInvalidateSize(entry) {
  if (!entry) {
    return;
  }

  if (entry.pendingInvalidateFrame != null && typeof cancelAnimationFrame === "function") {
    cancelAnimationFrame(entry.pendingInvalidateFrame);
  }

  if (entry.pendingInvalidateTimeout != null) {
    clearTimeout(entry.pendingInvalidateTimeout);
  }

  entry.pendingInvalidateFrame = null;
  entry.pendingInvalidateTimeout = null;
}

function scheduleInvalidateSize(entry) {
  if (!entry || !entry.map) {
    return;
  }

  if (entry.pendingInvalidateFrame != null || entry.pendingInvalidateTimeout != null) {
    return;
  }

  const runInvalidateSize = () => {
    entry.pendingInvalidateFrame = null;
    entry.pendingInvalidateTimeout = null;
    if (!entry.map) {
      return;
    }

    try {
      entry.map.invalidateSize(false);
    } catch {
      // best effort
    }
  };

  if (typeof requestAnimationFrame === "function") {
    entry.pendingInvalidateFrame = requestAnimationFrame(runInvalidateSize);
    return;
  }

  entry.pendingInvalidateTimeout = setTimeout(runInvalidateSize, 0);
}

function subscribeHostResize(entry) {
  if (!entry || !entry.host) {
    return;
  }

  if (typeof ResizeObserver === "function") {
    entry.resizeObserver = new ResizeObserver(() => {
      scheduleInvalidateSize(entry);
    });
    entry.resizeObserver.observe(entry.host);
    return;
  }

  if (typeof window !== "undefined" && typeof window.addEventListener === "function") {
    entry.windowResizeHandler = () => {
      scheduleInvalidateSize(entry);
    };
    window.addEventListener("resize", entry.windowResizeHandler);
  }
}

function unsubscribeHostResize(entry) {
  if (!entry) {
    return;
  }

  if (entry.resizeObserver && typeof entry.resizeObserver.disconnect === "function") {
    entry.resizeObserver.disconnect();
  }
  entry.resizeObserver = null;

  if (entry.windowResizeHandler
    && typeof window !== "undefined"
    && typeof window.removeEventListener === "function") {
    window.removeEventListener("resize", entry.windowResizeHandler);
  }
  entry.windowResizeHandler = null;

  clearScheduledInvalidateSize(entry);
}

function raiseRegionChanged(entry) {
  if (!entry || !entry.map || !entry.host) {
    return;
  }

  const region = readRegionFromMap(entry.map);
  if (!region) {
    return;
  }

  entry.lastAppliedRegion = region;
  setRegionTelemetry(entry.host, region);
  emitMirageEvent(entry.host, "mirage-map-region-changed", region);
}

export function initializeMapJson(nodeId, optionsJson) {
  const host = findMapHost(nodeId);
  if (!host) {
    return toResultJson(toResult(false, "host-not-found", `Map host '${nodeId}' was not found.`));
  }

  if (mapEntries.has(nodeId)) {
    clearMapError(host);
    return toResultJson(toResult(true));
  }

  if (typeof window === "undefined" || !window.L || typeof window.L.map !== "function") {
    return toResultJson(toResult(false, "leaflet-global-missing", "Leaflet global 'L' is unavailable."));
  }

  const options = resolveRuntimeOptions(optionsJson);
  try {
    host.setAttribute("data-mirage-map-provider", "leaflet");
    host.setAttribute("data-mirage-map-status", "ready");

    const map = window.L.map(host, {
      zoomControl: true,
      attributionControl: true
    });

    const tileLayer = window.L.tileLayer(options.tileLayerUrlTemplate, {
      attribution: options.tileLayerAttribution,
      minZoom: options.tileLayerMinZoom,
      maxZoom: options.tileLayerMaxZoom
    });

    tileLayer.addTo(map);
    const entry = {
      host,
      map,
      tileLayer,
      markers: new Map(),
      elements: new Map(),
      userLocationMarker: null,
      userLocationWatchId: null,
      resizeObserver: null,
      windowResizeHandler: null,
      pendingInvalidateFrame: null,
      pendingInvalidateTimeout: null,
      lastProjectedRegion: null,
      lastAppliedRegion: null,
      lastRegionRequestToken: 0,
      lastDismissPopupRequestToken: 0
    };
    mapEntries.set(nodeId, entry);
    subscribeHostResize(entry);

    map.on("moveend", () => {
      raiseRegionChanged(entry);
    });

    map.on("click", (event) => {
      if (!event || !event.latlng) {
        return;
      }

      emitMirageEvent(host, "mirage-map-clicked", {
        latitude: event.latlng.lat,
        longitude: event.latlng.lng
      });
    });

    map.invalidateSize(false);
    raiseRegionChanged(entry);
    return toResultJson(toResult(true));
  } catch (error) {
    const message = error instanceof Error ? error.message : String(error);
    return toResultJson(toResult(false, "map-initialize-failed", message));
  }
}

function normalizeMapType(value) {
  if (typeof value !== "string" || value.length === 0) {
    return "street";
  }

  const normalized = value.trim().toLowerCase();
  if (normalized === "street" || normalized === "satellite" || normalized === "hybrid") {
    return normalized;
  }

  return "unknown";
}

function applyRegion(entry, region, animate) {
  if (!entry) {
    return null;
  }

  const normalizedRegion = normalizeRegion(region);
  if (!normalizedRegion) {
    return null;
  }

  const halfLat = Math.max(0.00005, normalizedRegion.latitudeSpan / 2);
  const halfLng = Math.max(0.00005, normalizedRegion.longitudeSpan / 2);
  const south = Math.max(-90, normalizedRegion.centerLatitude - halfLat);
  const north = Math.min(90, normalizedRegion.centerLatitude + halfLat);
  const west = normalizedRegion.centerLongitude - halfLng;
  const east = normalizedRegion.centerLongitude + halfLng;

  const bounds = [
    [south, west],
    [north, east]
  ];

  entry.map.fitBounds(bounds, {
    animate: !!animate,
    duration: animate ? 0.25 : undefined
  });

  return readRegionFromMap(entry.map) ?? normalizedRegion;
}

function setLeafletHandlerState(handler, enabled) {
  if (!handler) {
    return;
  }

  if (enabled) {
    if (typeof handler.enable === "function") {
      handler.enable();
    }

    return;
  }

  if (typeof handler.disable === "function") {
    handler.disable();
  }
}

function applyInteractionState(entry, state) {
  const isScrollEnabled = toBoolean(state?.isScrollEnabled, true);
  const isZoomEnabled = toBoolean(state?.isZoomEnabled, true);

  setLeafletHandlerState(entry.map.dragging, isScrollEnabled);
  setLeafletHandlerState(entry.map.scrollWheelZoom, isZoomEnabled);
  setLeafletHandlerState(entry.map.touchZoom, isZoomEnabled);
  setLeafletHandlerState(entry.map.doubleClickZoom, isZoomEnabled);
  setLeafletHandlerState(entry.map.boxZoom, isZoomEnabled);
  setLeafletHandlerState(entry.map.keyboard, isZoomEnabled);
}

function mapGeolocationErrorCode(error) {
  switch (error?.code) {
    case 1:
      return "geolocation-permission-denied";
    case 2:
      return "geolocation-position-unavailable";
    case 3:
      return "geolocation-timeout";
    default:
      return "geolocation-error";
  }
}

function clearUserLocation(entry) {
  if (!entry) {
    return;
  }

  if (entry.userLocationWatchId != null && typeof navigator !== "undefined" && navigator.geolocation) {
    navigator.geolocation.clearWatch(entry.userLocationWatchId);
    entry.userLocationWatchId = null;
  }

  if (entry.userLocationMarker) {
    try {
      entry.map.removeLayer(entry.userLocationMarker);
    } catch {
      // best effort
    }

    entry.userLocationMarker = null;
  }

  entry.host.setAttribute("data-mirage-map-geolocation-status", "disabled");
}

function applyUserLocationState(entry, state) {
  const isShowingUser = toBoolean(state?.isShowingUser, false);
  if (!isShowingUser) {
    clearUserLocation(entry);
    return null;
  }

  if (typeof navigator === "undefined" || !navigator.geolocation) {
    const message = "Browser geolocation API is unavailable.";
    entry.host.setAttribute("data-mirage-map-geolocation-status", "unavailable");
    emitDiagnostic(entry.host, "geolocation-unavailable", message);
    return toResult(false, "geolocation-unavailable", message);
  }

  if (entry.userLocationWatchId != null) {
    return null;
  }

  entry.host.setAttribute("data-mirage-map-geolocation-status", "pending");
  try {
    let watchId = null;
    watchId = navigator.geolocation.watchPosition(
      (position) => {
        const latitude = toFiniteOrNull(position?.coords?.latitude);
        const longitude = toFiniteOrNull(position?.coords?.longitude);
        if (latitude == null || longitude == null) {
          return;
        }

        if (!entry.userLocationMarker) {
          entry.userLocationMarker = window.L.circleMarker([latitude, longitude], {
            radius: 8,
            color: "#2563eb",
            weight: 2,
            fillColor: "#93c5fd",
            fillOpacity: 0.72
          }).addTo(entry.map);
        } else {
          entry.userLocationMarker.setLatLng([latitude, longitude]);
        }

        entry.host.setAttribute("data-mirage-map-geolocation-status", "ready");
      },
      (error) => {
        if (entry.userLocationWatchId === watchId) {
          if (typeof navigator !== "undefined" && navigator.geolocation && typeof navigator.geolocation.clearWatch === "function") {
            try {
              navigator.geolocation.clearWatch(watchId);
            } catch {
              // best effort
            }
          }

          entry.userLocationWatchId = null;
        }

        const code = mapGeolocationErrorCode(error);
        const message = error?.message || "Unable to read current browser location.";
        entry.host.setAttribute("data-mirage-map-geolocation-status", "error");
        emitDiagnostic(entry.host, code, message);
      },
      {
        enableHighAccuracy: true,
        maximumAge: 10000,
        timeout: 15000
      }
    );
    entry.userLocationWatchId = watchId;
  } catch (error) {
    entry.userLocationWatchId = null;
    const message = error instanceof Error ? error.message : String(error);
    entry.host.setAttribute("data-mirage-map-geolocation-status", "error");
    emitDiagnostic(entry.host, "geolocation-watch-failed", message);
    return toResult(false, "geolocation-watch-failed", message);
  }

  return null;
}

function buildPinPopupContent(pin, key) {
  if (typeof document === "undefined" || !document.createElement) {
    return null;
  }

  const labelText = typeof pin?.label === "string" ? pin.label : "";
  const addressText = typeof pin?.address === "string" ? pin.address : "";
  if (labelText.length === 0 && addressText.length === 0) {
    return null;
  }

  const root = document.createElement("div");
  root.setAttribute("data-mirage-map-pin-popup-key", key);

  if (labelText.length > 0) {
    const label = document.createElement("span");
    label.textContent = labelText;
    root.appendChild(label);
  }

  if (addressText.length > 0) {
    if (root.childNodes.length > 0) {
      const separator = document.createElement("span");
      separator.textContent = " — ";
      root.appendChild(separator);
    }

    const address = document.createElement("span");
    address.textContent = addressText;
    root.appendChild(address);
  }

  return root;
}

function wireMarkerInteraction(entry, marker, key) {
  const existingPinClickHandler = marker.__miragePinClickHandler;
  if (typeof existingPinClickHandler === "function") {
    marker.off("click", existingPinClickHandler);
  }

  const onPinClick = () => {
    emitMirageEvent(entry.host, "mirage-map-pin-clicked", { key });
  };
  marker.__miragePinClickHandler = onPinClick;
  marker.on("click", onPinClick);

  const existingPopupOpenHandler = marker.__miragePinPopupOpenHandler;
  if (typeof existingPopupOpenHandler === "function") {
    marker.off("popupopen", existingPopupOpenHandler);
  }

  const onPopupOpen = (event) => {
    const popupElement = event?.popup?.getElement?.();
    if (!popupElement) {
      return;
    }

    const existingHandler = popupElement.__miragePinInfoWindowClickHandler;
    if (typeof existingHandler === "function") {
      popupElement.removeEventListener("click", existingHandler);
    }

    const onPopupClick = () => {
      emitMirageEvent(entry.host, "mirage-map-pin-info-window-clicked", { key });
    };

    popupElement.__miragePinInfoWindowClickHandler = onPopupClick;
    popupElement.addEventListener("click", onPopupClick);
  };
  marker.__miragePinPopupOpenHandler = onPopupOpen;
  marker.on("popupopen", onPopupOpen);
}

function syncPins(entry, pins) {
  const nextPins = Array.isArray(pins) ? pins : [];
  const seenKeys = new Set();

  for (const rawPin of nextPins) {
    if (!rawPin || typeof rawPin !== "object") {
      continue;
    }

    const key = typeof rawPin.key === "string" && rawPin.key.length > 0 ? rawPin.key : null;
    const latitude = toFiniteOrNull(rawPin.latitude);
    const longitude = toFiniteOrNull(rawPin.longitude);
    if (!key || latitude == null || longitude == null) {
      continue;
    }

    seenKeys.add(key);
    let marker = entry.markers.get(key);
    if (!marker) {
      marker = window.L.marker([latitude, longitude], {
        title: typeof rawPin.label === "string" ? rawPin.label : ""
      });
      marker.addTo(entry.map);
      entry.markers.set(key, marker);
    } else {
      marker.setLatLng([latitude, longitude]);
      marker.options.title = typeof rawPin.label === "string" ? rawPin.label : "";
    }
    wireMarkerInteraction(entry, marker, key);

    const popupContent = buildPinPopupContent(rawPin, key);
    if (popupContent) {
      marker.bindPopup(popupContent);
    } else if (marker.getPopup()) {
      marker.unbindPopup();
    }
  }

  for (const [key, marker] of entry.markers.entries()) {
    if (seenKeys.has(key)) {
      continue;
    }

    entry.map.removeLayer(marker);
    entry.markers.delete(key);
  }

  entry.host.setAttribute("data-mirage-map-pin-count", String(seenKeys.size));
}

function normalizeCoordinates(rawCoordinates) {
  if (!Array.isArray(rawCoordinates)) {
    return [];
  }

  const coordinates = [];
  for (const rawCoordinate of rawCoordinates) {
    if (!rawCoordinate || typeof rawCoordinate !== "object") {
      continue;
    }

    const latitude = toFiniteOrNull(rawCoordinate.latitude);
    const longitude = toFiniteOrNull(rawCoordinate.longitude);
    if (latitude == null || longitude == null) {
      continue;
    }

    coordinates.push([latitude, longitude]);
  }

  return coordinates;
}

function normalizeElementKind(value) {
  if (typeof value !== "string") {
    return "unknown";
  }

  const normalized = value.trim().toLowerCase();
  if (normalized === "circle" || normalized === "polyline" || normalized === "polygon") {
    return normalized;
  }

  return "unknown";
}

function createElementStyle(rawElement, kind) {
  const strokeColor = typeof rawElement?.strokeColor === "string" && rawElement.strokeColor.length > 0
    ? rawElement.strokeColor
    : "#2563eb";
  const fillColor = typeof rawElement?.fillColor === "string" && rawElement.fillColor.length > 0
    ? rawElement.fillColor
    : strokeColor;
  const strokeThickness = toFiniteOrNull(rawElement?.strokeThickness);

  if (kind === "polyline") {
    return {
      color: strokeColor,
      weight: strokeThickness != null ? Math.max(0.5, strokeThickness) : 2
    };
  }

  return {
    color: strokeColor,
    weight: strokeThickness != null ? Math.max(0.5, strokeThickness) : 2,
    fillColor,
    fillOpacity: 0.3
  };
}

function syncElements(entry, elements) {
  const nextElements = Array.isArray(elements) ? elements : [];
  const seenKeys = new Set();

  for (const rawElement of nextElements) {
    if (!rawElement || typeof rawElement !== "object") {
      continue;
    }

    const key = typeof rawElement.key === "string" && rawElement.key.length > 0 ? rawElement.key : null;
    const kind = normalizeElementKind(rawElement.kind);
    const coordinates = normalizeCoordinates(rawElement.coordinates);
    if (!key || kind === "unknown" || coordinates.length === 0) {
      continue;
    }

    seenKeys.add(key);
    const style = createElementStyle(rawElement, kind);
    const existing = entry.elements.get(key);
    let layer = existing?.layer || null;
    const existingKind = existing?.kind || null;

    if (existingKind !== kind && layer) {
      try {
        entry.map.removeLayer(layer);
      } catch {
        // best effort
      }

      layer = null;
    }

    if (!layer) {
      if (kind === "circle") {
        const radiusMeters = toFiniteOrNull(rawElement.radiusMeters);
        if (radiusMeters == null) {
          continue;
        }

        layer = window.L.circle(coordinates[0], {
          radius: Math.max(1, radiusMeters),
          ...style
        });
      } else if (kind === "polygon") {
        layer = window.L.polygon(coordinates, style);
      } else if (kind === "polyline") {
        layer = window.L.polyline(coordinates, style);
      }

      if (!layer) {
        continue;
      }

      layer.addTo(entry.map);
      entry.elements.set(key, { kind, layer });
    } else {
      if (kind === "circle") {
        const radiusMeters = toFiniteOrNull(rawElement.radiusMeters);
        if (typeof layer.setLatLng === "function") {
          layer.setLatLng(coordinates[0]);
        }

        if (radiusMeters != null && typeof layer.setRadius === "function") {
          layer.setRadius(Math.max(1, radiusMeters));
        }
      } else if (typeof layer.setLatLngs === "function") {
        layer.setLatLngs(coordinates);
      }

      if (typeof layer.setStyle === "function") {
        layer.setStyle(style);
      }
    }
  }

  for (const [key, value] of entry.elements.entries()) {
    if (seenKeys.has(key)) {
      continue;
    }

    try {
      entry.map.removeLayer(value.layer);
    } catch {
      // best effort
    }

    entry.elements.delete(key);
  }

  entry.host.setAttribute("data-mirage-map-element-count", String(seenKeys.size));
}

function applyDismissPopupRequest(entry, state) {
  const dismissToken = Number.isFinite(state?.dismissPopupRequestToken) ? Number(state.dismissPopupRequestToken) : 0;
  if (dismissToken <= 0 || dismissToken === entry.lastDismissPopupRequestToken) {
    return;
  }

  entry.lastDismissPopupRequestToken = dismissToken;
  const dismissKey = typeof state?.dismissPopupKey === "string" ? state.dismissPopupKey : "";
  if (dismissKey.length === 0) {
    return;
  }

  const marker = entry.markers.get(dismissKey);
  if (marker && typeof marker.closePopup === "function") {
    marker.closePopup();
  }
}

export function applyMapStateJson(nodeId, stateJson) {
  const entry = mapEntries.get(nodeId);
  if (!entry) {
    return toResultJson(toResult(false, "map-not-initialized", `Map '${nodeId}' has not been initialized.`));
  }

  clearMapError(entry.host);

  const state = parseJson(stateJson, {});
  let warningCode = null;
  let warningMessage = null;

  const mapType = normalizeMapType(state?.mapType);
  if (mapType !== "street") {
    warningCode = "map-type-unsupported";
    warningMessage = `Leaflet provider currently falls back to street tiles for map type '${mapType}'.`;
  }

  applyInteractionState(entry, state);
  const geolocationResult = applyUserLocationState(entry, state);
  if (geolocationResult && !geolocationResult.ok) {
    warningCode = warningCode || geolocationResult.errorCode || "geolocation-failed";
    warningMessage = warningMessage || geolocationResult.errorMessage || "Geolocation failed.";
  }

  if (toBoolean(state?.isTrafficEnabled, false) && !warningCode) {
    warningCode = "traffic-unsupported";
    warningMessage = "Leaflet provider does not provide built-in traffic overlays.";
    emitDiagnostic(entry.host, warningCode, warningMessage);
  }

  const regionRequestToken = Number.isFinite(state?.regionRequestToken) ? state.regionRequestToken : 0;
  const shouldAnimate = regionRequestToken > 0 && regionRequestToken !== entry.lastRegionRequestToken;
  entry.lastRegionRequestToken = regionRequestToken;

  if (state?.region && typeof state.region === "object") {
    const normalizedRequestedRegion = normalizeRegion(state.region);
    if (normalizedRequestedRegion) {
      const projectedRegionChanged = !areRegionsEquivalent(entry.lastProjectedRegion, normalizedRequestedRegion);
      const regionMatchesViewport = areRegionsEquivalent(entry.lastAppliedRegion, normalizedRequestedRegion);
      const shouldApplyRegion = shouldAnimate || (projectedRegionChanged && !regionMatchesViewport);
      entry.lastProjectedRegion = normalizedRequestedRegion;
      if (shouldApplyRegion) {
        const appliedRegion = applyRegion(entry, normalizedRequestedRegion, shouldAnimate);
        if (appliedRegion) {
          entry.lastAppliedRegion = appliedRegion;
        }
      }

      const telemetryRegion = shouldApplyRegion ? normalizedRequestedRegion : readRegionFromMap(entry.map);
      setRegionTelemetry(entry.host, telemetryRegion ?? normalizedRequestedRegion);
    } else {
      entry.lastProjectedRegion = null;
      setRegionTelemetry(entry.host, null);
    }
  } else {
    entry.lastProjectedRegion = null;
    entry.lastAppliedRegion = null;
    setRegionTelemetry(entry.host, null);
  }

  syncPins(entry, state?.pins);
  syncElements(entry, state?.elements);
  applyDismissPopupRequest(entry, state);
  scheduleInvalidateSize(entry);
  return toResultJson(toResult(true, null, null, warningCode, warningMessage));
}

export function disposeMap(nodeId) {
  const entry = mapEntries.get(nodeId);
  if (!entry) {
    return;
  }

  try {
    for (const marker of entry.markers.values()) {
      entry.map.removeLayer(marker);
    }

    for (const element of entry.elements.values()) {
      entry.map.removeLayer(element.layer);
    }

    entry.markers.clear();
    entry.elements.clear();
    clearUserLocation(entry);
    unsubscribeHostResize(entry);
    entry.map.remove();
  } catch {
    // best effort
  } finally {
    mapEntries.delete(nodeId);
  }
}

export function showMapError(nodeId, errorCode, message) {
  const host = findMapHost(nodeId);
  if (!host) {
    return;
  }

  host.setAttribute("data-mirage-map-status", "error");
  host.setAttribute("data-mirage-map-error-code", typeof errorCode === "string" ? errorCode : "unknown");
  host.setAttribute("data-mirage-map-error", typeof message === "string" ? message : "Unknown map error.");
}
