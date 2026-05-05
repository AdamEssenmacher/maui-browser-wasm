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

  function isPlainPrimaryClick(event) {
    return !!event
      && !event.defaultPrevented
      && event.button === 0
      && !event.metaKey
      && !event.ctrlKey
      && !event.shiftKey
      && !event.altKey;
  }

  function readSubmitterAttribute(submitter, submitterAttribute, form, formAttribute, fallback) {
    if (submitter && typeof submitter.getAttribute === "function") {
      const submitterValue = submitter.getAttribute(submitterAttribute);
      if (typeof submitterValue === "string" && submitterValue.length > 0) {
        return submitterValue;
      }
    }

    const formValue = form.getAttribute(formAttribute);
    return typeof formValue === "string" ? formValue : fallback;
  }

  function createFormData(form, submitter) {
    if (submitter) {
      try {
        return new FormData(form, submitter);
      } catch {
        const fallback = new FormData(form);
        const name = typeof submitter.name === "string" ? submitter.name : "";
        if (name.length > 0 && !submitter.disabled) {
          fallback.append(name, typeof submitter.value === "string" ? submitter.value : "");
        }

        return fallback;
      }
    }

    return new FormData(form);
  }

  function resolveFormUrl(form, submitter) {
    const rawTarget = readSubmitterAttribute(submitter, "formtarget", form, "target", "").trim().toLowerCase();
    if (rawTarget.length > 0 && rawTarget !== "_self") {
      return null;
    }

    const rawMethod = readSubmitterAttribute(submitter, "formmethod", form, "method", "get").trim().toLowerCase();
    if (rawMethod !== "get") {
      return null;
    }

    const url = resolveUrl(readSubmitterAttribute(submitter, "formaction", form, "action", window.location.href));
    if (!url) {
      return null;
    }

    try {
      const next = new URL(url, document.baseURI);
      const query = new URLSearchParams(createFormData(form, submitter));
      for (const [key, value] of query) {
        next.searchParams.append(key, value);
      }

      return next.href;
    } catch {
      return url;
    }
  }

  document.addEventListener("click", function (event) {
    if (!isPlainPrimaryClick(event)) {
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
    post("navigating", { url });
  });

  document.addEventListener("submit", function (event) {
    if (event.defaultPrevented) {
      return;
    }

    const form = event.target;
    if (!form || typeof form.getAttribute !== "function") {
      return;
    }

    const url = resolveFormUrl(form, event.submitter || null);
    if (!url) {
      return;
    }

    event.preventDefault();
    post("navigating", { url });
  });

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
