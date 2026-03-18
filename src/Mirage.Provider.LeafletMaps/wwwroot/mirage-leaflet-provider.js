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
      resolve(toResult(true));
      return;
    }

    const link = document.createElement("link");
    link.rel = "stylesheet";
    link.href = url;
    link.setAttribute("data-mirage-leaflet-css", "true");

    let completed = false;
    const timer = setTimeout(() => {
      if (completed) {
        return;
      }

      completed = true;
      resolve(toResult(false, "css-timeout", `Timed out while loading Leaflet CSS from '${url}'.`));
    }, timeoutMs);

    link.onload = () => {
      if (completed) {
        return;
      }

      completed = true;
      clearTimeout(timer);
      resolve(toResult(true));
    };

    link.onerror = () => {
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

      resolve(toResult(false, "leaflet-global-missing", "Leaflet script tag exists but global 'L' is unavailable."));
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
    leafletReadinessPromise = ensureLeafletReadyCore(options).then((result) => {
      leafletReadinessResult = result;
      return result;
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

function toFiniteOrNull(value) {
  if (!Number.isFinite(value)) {
    return null;
  }

  return Number(value);
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
    host.style.width = "100%";
    host.style.height = "100%";

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
    mapEntries.set(nodeId, {
      host,
      map,
      tileLayer,
      markers: new Map(),
      lastRegionRequestToken: 0
    });

    map.invalidateSize(false);
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
  if (!entry || !region) {
    return;
  }

  const centerLatitude = toFiniteOrNull(region.centerLatitude);
  const centerLongitude = toFiniteOrNull(region.centerLongitude);
  const latitudeSpan = toFiniteOrNull(region.latitudeSpan);
  const longitudeSpan = toFiniteOrNull(region.longitudeSpan);
  if (centerLatitude == null || centerLongitude == null || latitudeSpan == null || longitudeSpan == null) {
    return;
  }

  const halfLat = Math.max(0.00005, Math.abs(latitudeSpan) / 2);
  const halfLng = Math.max(0.00005, Math.abs(longitudeSpan) / 2);
  const south = Math.max(-90, centerLatitude - halfLat);
  const north = Math.min(90, centerLatitude + halfLat);
  const west = centerLongitude - halfLng;
  const east = centerLongitude + halfLng;

  const bounds = [
    [south, west],
    [north, east]
  ];

  entry.map.fitBounds(bounds, {
    animate: !!animate,
    duration: animate ? 0.25 : undefined
  });
}

function buildPinPopup(pin) {
  const parts = [];
  if (typeof pin.label === "string" && pin.label.length > 0) {
    parts.push(pin.label);
  }

  if (typeof pin.address === "string" && pin.address.length > 0) {
    parts.push(pin.address);
  }

  return parts.join(" — ");
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

    const popup = buildPinPopup(rawPin);
    if (popup.length > 0) {
      marker.bindPopup(popup);
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

  const regionRequestToken = Number.isFinite(state?.regionRequestToken) ? state.regionRequestToken : 0;
  const shouldAnimate = regionRequestToken > 0 && regionRequestToken !== entry.lastRegionRequestToken;
  entry.lastRegionRequestToken = regionRequestToken;

  if (state?.region && typeof state.region === "object") {
    applyRegion(entry, state.region, shouldAnimate);
    setRegionTelemetry(entry.host, state.region);
  } else {
    setRegionTelemetry(entry.host, null);
  }

  syncPins(entry, state?.pins);
  entry.map.invalidateSize(false);
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

    entry.markers.clear();
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
