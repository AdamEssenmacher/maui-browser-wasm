import { dotnet } from "./_framework/dotnet.js";

function setupHotReloadDiagnostics() {
  const reloadCounterKey = "maui-browser-hot-reload-page-count";
  const priorCount = Number.parseInt(sessionStorage.getItem(reloadCounterKey) ?? "0", 10);
  const pageReloadCount = Number.isFinite(priorCount) ? priorCount + 1 : 1;
  sessionStorage.setItem(reloadCounterKey, String(pageReloadCount));

  const diagnostics = {
    pageReloadCount,
    deltaApplyCount: 0,
    dotnetModifiableAssemblies: "<unknown>",
    aspnetcoreBrowserTools: "<unknown>",
    sampleVersion: null
  };

  globalThis.__mauiBrowserHotReload = diagnostics;
  console.info(`[hot-reload] page boot #${pageReloadCount}`);

  const installDeltaHook = () => {
    const blazorInternal = globalThis.Blazor?._internal;
    if (!blazorInternal || typeof blazorInternal.applyHotReloadDeltas !== "function") {
      return false;
    }

    if (blazorInternal.applyHotReloadDeltas.__mauiHooked === true) {
      return true;
    }

    const original = blazorInternal.applyHotReloadDeltas;
    const wrapped = (...args) => {
      diagnostics.deltaApplyCount += 1;
      console.info(`[hot-reload] applyHotReloadDeltas invoked (${diagnostics.deltaApplyCount})`);
      return original(...args);
    };

    wrapped.__mauiHooked = true;
    blazorInternal.applyHotReloadDeltas = wrapped;
    console.info("[hot-reload] delta callback hook installed");
    return true;
  };

  const pollId = setInterval(() => {
    if (installDeltaHook()) {
      clearInterval(pollId);
    }
  }, 500);

  setTimeout(() => clearInterval(pollId), 60000);
  return diagnostics;
}

function startXamlRefreshFallback(diagnostics) {
  const sampleVersionUrl = "/_dev/sample-version";
  let currentVersion = null;

  const poll = async () => {
    try {
      const response = await fetch(sampleVersionUrl, { cache: "no-store" });
      if (!response.ok) {
        return;
      }

      const payload = await response.json();
      const version = payload?.version;
      if (!Number.isFinite(version)) {
        return;
      }

      diagnostics.sampleVersion = version;
      if (currentVersion === null) {
        currentVersion = version;
        return;
      }

      if (version !== currentVersion) {
        console.info(`[hot-reload] dev sample version changed ${currentVersion} -> ${version}; forcing page reload for XAML refresh`);
        currentVersion = version;
        globalThis.location.reload();
      }
    } catch {
      // Dev endpoint is optional outside the watch host.
    }
  };

  poll();
  setInterval(poll, 1000);
}

async function start() {
  const appRoot = document.getElementById("app");
  const diagnostics = setupHotReloadDiagnostics();
  startXamlRefreshFallback(diagnostics);

  try {
    let hotReloadModule = null;
    try {
      hotReloadModule = await import("./_framework/hotreload.module.js");
      console.info("[hot-reload] JS initializer module loaded");
    } catch (moduleError) {
      console.info("[hot-reload] JS initializer module unavailable; refresh-only diagnostics remain active", moduleError);
    }

    const runtime = await dotnet
      .withDiagnosticTracing(false)
      .withOnConfigLoaded((config) => {
        if (hotReloadModule?.onRuntimeConfigLoaded) {
          Promise.resolve(hotReloadModule.onRuntimeConfigLoaded(config))
            .catch((hookError) => {
              console.warn("[hot-reload] onRuntimeConfigLoaded hook failed", hookError);
            });
        }

        const env = config?.environmentVariables ?? {};
        diagnostics.dotnetModifiableAssemblies = env.DOTNET_MODIFIABLE_ASSEMBLIES ?? "<unset>";
        diagnostics.aspnetcoreBrowserTools = env.__ASPNETCORE_BROWSER_TOOLS ?? "<unset>";
        console.info("[hot-reload] runtime config",
          `DOTNET_MODIFIABLE_ASSEMBLIES=${diagnostics.dotnetModifiableAssemblies}`,
          `__ASPNETCORE_BROWSER_TOOLS=${diagnostics.aspnetcoreBrowserTools}`);
      })
      .create();

    if (hotReloadModule?.onRuntimeReady && typeof runtime.getAssemblyExports === "function") {
      Promise.resolve(hotReloadModule.onRuntimeReady({ getAssemblyExports: runtime.getAssemblyExports.bind(runtime) }))
        .then(() => {
          console.info("[hot-reload] runtime-ready initializer invoked");
        })
        .catch((hookError) => {
          console.warn("[hot-reload] onRuntimeReady hook failed", hookError);
        });
    }

    await runtime.runMain();
  } catch (error) {
    console.error("Failed to start .NET runtime:", error);

    if (appRoot) {
      appRoot.textContent = "Startup failed. Check browser console for details.";
    }
  }
}

await start();
