#!/usr/bin/env bash
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
SAMPLE_PROJECT="$REPO_ROOT/src/Maui.Browser.Sample/Maui.Browser.Sample.csproj"
BUILD_OUTPUT="$REPO_ROOT/src/Maui.Browser.Sample/bin/Debug/net10.0-browser/browser-wasm"
RUNTIME_CONFIG="$BUILD_OUTPUT/Maui.Browser.Sample.runtimeconfig.json"
APP_BUNDLE_CONFIG="$BUILD_OUTPUT/AppBundle/Maui.Browser.Sample.runtimeconfig.json"

if [[ ! -f "$RUNTIME_CONFIG" ]]; then
  echo "[fail] Runtime config not found: $RUNTIME_CONFIG"
  echo "       Run: dotnet build \"$SAMPLE_PROJECT\" -c Debug"
  exit 1
fi

if ! rg -q '"System.Reflection.Metadata.MetadataUpdater.IsSupported": true' "$RUNTIME_CONFIG"; then
  echo "[fail] MetadataUpdater support flag is not true in $RUNTIME_CONFIG"
  exit 1
fi

if [[ ! -f "$APP_BUNDLE_CONFIG" ]]; then
  echo "[fail] AppBundle runtime config not found: $APP_BUNDLE_CONFIG"
  exit 1
fi

if ! rg -q '"System.Reflection.Metadata.MetadataUpdater.IsSupported": true' "$APP_BUNDLE_CONFIG"; then
  echo "[fail] MetadataUpdater support flag is not true in $APP_BUNDLE_CONFIG"
  exit 1
fi

if [[ ! -f "$BUILD_OUTPUT/AppBundle/_framework/hotreload.module.js" ]]; then
  echo "[fail] Hot reload browser JS initializer was not deployed to _framework/hotreload.module.js."
  exit 1
fi

echo "[pass] Hot reload prerequisites are present."
