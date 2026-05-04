(function () {
  if (window.__mirageWebViewBridgeInstalled) {
    return;
  }

  window.__mirageWebViewBridgeInstalled = true;

  function post(type, payload) {
    window.parent.postMessage(Object.assign({
      __mirageWebView: true,
      type,
      url: window.location.href
    }, payload || {}), "*");
  }

  function serialize(value) {
    if (typeof value === "undefined") {
      return "";
    }
    if (value === null || typeof value === "string") {
      return value ?? "";
    }
    try {
      return JSON.stringify(value);
    } catch {
      return String(value);
    }
  }

  function resolveUrl(value) {
    if (typeof value !== "string" || value.length === 0) {
      return null;
    }
    try {
      return new URL(value, document.baseURI).href;
    } catch {
      return value;
    }
  }

  function resolveFormUrl(form) {
    const rawTarget = (form.getAttribute("target") || "").trim().toLowerCase();
    if (rawTarget.length > 0 && rawTarget !== "_self") {
      return null;
    }

    const rawMethod = (form.getAttribute("method") || "get").trim().toLowerCase();
    if (rawMethod !== "get") {
      return null;
    }

    const url = resolveUrl(form.getAttribute("action") || window.location.href);
    if (!url) {
      return null;
    }

    try {
      const next = new URL(url, document.baseURI);
      const query = new URLSearchParams(new FormData(form)).toString();
      if (query.length > 0) {
        next.search = query;
      }
      return next.href;
    } catch {
      return url;
    }
  }

  document.addEventListener("click", function (event) {
    if (event.defaultPrevented || event.button !== 0 || event.metaKey || event.ctrlKey || event.shiftKey || event.altKey) {
      return;
    }

    const anchor = event.target && event.target.closest ? event.target.closest("a[href]") : null;
    if (!anchor) {
      return;
    }

    const rawTarget = (anchor.getAttribute("target") || "").trim().toLowerCase();
    if (rawTarget.length > 0 && rawTarget !== "_self") {
      return;
    }

    const url = resolveUrl(anchor.getAttribute("href"));
    if (!url) {
      return;
    }

    event.preventDefault();
    event.stopPropagation();
    post("navigating", { url });
  }, true);

  document.addEventListener("submit", function (event) {
    if (event.defaultPrevented) {
      return;
    }

    const form = event.target;
    if (!form || typeof form.getAttribute !== "function") {
      return;
    }

    const url = resolveFormUrl(form);
    if (!url) {
      return;
    }

    event.preventDefault();
    event.stopPropagation();
    post("navigating", { url });
  }, true);

  window.addEventListener("message", function (event) {
    const message = event.data;
    if (!message || message.__mirageWebView !== true || message.type !== "evaluate") {
      return;
    }

    const requestId = Number.isFinite(message.requestId) ? Math.trunc(message.requestId) : 0;
    const captureResult = message.captureResult === true;
    try {
      const result = (0, eval)(message.script || "");
      if (captureResult) {
        post("evalresult", {
          requestId,
          success: true,
          result: serialize(result),
          error: null
        });
      }
    } catch (error) {
      post("evalresult", {
        requestId,
        success: false,
        result: null,
        error: error instanceof Error ? error.message : String(error)
      });
    }
  });

  function ready() {
    post("ready");
  }

  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", ready, { once: true });
  } else {
    ready();
  }
  window.addEventListener("load", ready, { once: true });
})();
