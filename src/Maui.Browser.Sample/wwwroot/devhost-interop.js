export function getLocationOrigin() {
  return globalThis.location.origin;
}

export function forceReload() {
  globalThis.location.reload();
}
