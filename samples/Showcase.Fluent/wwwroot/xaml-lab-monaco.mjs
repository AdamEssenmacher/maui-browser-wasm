const tagName = "mirage-monaco-editor";
const monacoModuleUrl = "https://cdn.jsdelivr.net/npm/monaco-editor@0.52.2/+esm";
let monacoModulePromise = null;
let upgradeObserver = null;

async function loadMonacoModule() {
  if (typeof window !== "undefined" && window.monaco?.editor) {
    return window.monaco;
  }

  if (!monacoModulePromise) {
    monacoModulePromise = import(monacoModuleUrl)
      .then((module) => {
        const monaco = module?.editor ? module : module?.default;
        if (monaco?.editor && typeof window !== "undefined") {
          window.monaco = monaco;
        }

        return monaco ?? null;
      })
      .catch(() => null);
  }

  return monacoModulePromise;
}

if (!customElements.get(tagName)) {
  const MonacoEditorElement = class MirageMonacoEditorElement extends HTMLElement {
      constructor() {
        super();
        this._value = "";
        this._language = "xml";
        this._theme = "vs-dark";
        this._readOnly = false;
        this._isConnected = false;
        this._container = document.createElement("div");
        this._container.className = "editor-shell";
        this.style.display = "block";
        this.style.width = "100%";
        this.style.maxWidth = "100%";
        this.style.minWidth = "0";
        this.style.minHeight = "280px";
        this.style.boxSizing = "border-box";
        this.style.overflow = "hidden";

        this._container.style.width = "100%";
        this._container.style.maxWidth = "100%";
        this._container.style.minWidth = "0";
        this._container.style.minHeight = "280px";
        this._container.style.height = "100%";
        this._container.style.boxSizing = "border-box";
        this._container.style.overflow = "hidden";

        this._textarea = document.createElement("textarea");
        this._textarea.spellcheck = false;
        this._textarea.style.width = "100%";
        this._textarea.style.maxWidth = "100%";
        this._textarea.style.minWidth = "0";
        this._textarea.style.minHeight = "280px";
        this._textarea.style.height = "100%";
        this._textarea.style.boxSizing = "border-box";
        this._textarea.style.resize = "vertical";
        this._textarea.style.overflowX = "auto";
        this._textarea.style.whiteSpace = "pre";
        this._textarea.style.border = "1px solid rgba(80, 80, 80, 0.4)";
        this._textarea.style.borderRadius = "6px";
        this._textarea.style.padding = "12px";
        this._textarea.style.background = "#1e1e1e";
        this._textarea.style.color = "#f8f8f8";
        this._textarea.style.fontFamily =
          'ui-monospace, Menlo, Monaco, "Cascadia Mono", "Segoe UI Mono", Consolas, monospace';
        this._textarea.style.fontSize = "13px";
        this._textarea.style.lineHeight = "1.5";

        this._textarea.value = this._value;
        this._textarea.addEventListener("input", () => {
          this._onUserInput(this._textarea.value);
        });
        this._container.appendChild(this._textarea);
        this.appendChild(this._container);

        this.setAttribute("data-editor-backend", "textarea");
      }

      connectedCallback() {
        this._isConnected = true;
        this._applyStateToTextarea();
        this._tryInitializeMonaco();
      }

      disconnectedCallback() {
        this._isConnected = false;
        this._disposeMonacoEditor();
      }

      get value() {
        return this._value;
      }

      set value(nextValue) {
        const normalized = typeof nextValue === "string" ? nextValue : "";
        if (this._value === normalized) {
          return;
        }

        this._value = normalized;
        this._applyValueToEditors();
      }

      get language() {
        return this._language;
      }

      set language(nextLanguage) {
        const normalized = typeof nextLanguage === "string" && nextLanguage.length > 0 ? nextLanguage : "xml";
        if (this._language === normalized) {
          return;
        }

        this._language = normalized;
        this._applyLanguageToMonaco();
      }

      get theme() {
        return this._theme;
      }

      set theme(nextTheme) {
        const normalized = typeof nextTheme === "string" && nextTheme.length > 0 ? nextTheme : "vs-dark";
        if (this._theme === normalized) {
          return;
        }

        this._theme = normalized;
        this._applyThemeToMonaco();
      }

      get readOnly() {
        return this._readOnly;
      }

      set readOnly(nextReadOnly) {
        const normalized = Boolean(nextReadOnly);
        if (this._readOnly === normalized) {
          return;
        }

        this._readOnly = normalized;
        this._applyReadOnlyState();
      }

      setTextForTest(nextText) {
        this.value = typeof nextText === "string" ? nextText : "";
        this.dispatchEvent(new Event("input", { bubbles: true, composed: true }));
      }

      _onUserInput(nextValue) {
        const normalized = typeof nextValue === "string" ? nextValue : "";
        if (this._value === normalized) {
          return;
        }

        this._value = normalized;
        this._applyValueToEditors();
        this.dispatchEvent(new Event("input", { bubbles: true, composed: true }));
      }

      _applyValueToEditors() {
        if (this._textarea && this._textarea.value !== this._value) {
          this._textarea.value = this._value;
        }

        if (this._monacoEditor) {
          const currentValue = this._monacoEditor.getValue();
          if (currentValue !== this._value) {
            this._monacoEditor.setValue(this._value);
          }
        }
      }

      _applyStateToTextarea() {
        if (!this._textarea) {
          return;
        }

        this._textarea.value = this._value;
        this._textarea.readOnly = this._readOnly;
      }

      _applyLanguageToMonaco() {
        if (!this._monacoEditor || !window.monaco?.editor) {
          return;
        }

        const model = this._monacoEditor.getModel?.();
        if (model) {
          window.monaco.editor.setModelLanguage(model, this._language);
        }
      }

      _applyThemeToMonaco() {
        if (!window.monaco?.editor) {
          return;
        }

        try {
          window.monaco.editor.setTheme(this._theme);
        } catch {
          // Keep default theme when the requested theme is unavailable.
        }
      }

      _applyReadOnlyState() {
        if (this._textarea) {
          this._textarea.readOnly = this._readOnly;
        }

        if (this._monacoEditor?.updateOptions) {
          this._monacoEditor.updateOptions({ readOnly: this._readOnly });
        }
      }

      async _tryInitializeMonaco() {
        if (!this._isConnected || this._monacoEditor || this._monacoFailed) {
          return;
        }

        const monaco = await loadMonacoModule();
        if (!this._isConnected || !monaco?.editor) {
          this._monacoFailed = true;
          return;
        }

        try {
          const host = document.createElement("div");
          host.className = "monaco-host";
          host.style.width = "100%";
          host.style.maxWidth = "100%";
          host.style.minWidth = "0";
          host.style.minHeight = "280px";
          host.style.height = "100%";
          host.style.border = "1px solid rgba(80, 80, 80, 0.4)";
          host.style.borderRadius = "6px";
          host.style.overflow = "hidden";
          this._container.appendChild(host);
          this._monacoHost = host;

          this._monacoEditor = monaco.editor.create(host, {
            value: this._value,
            language: this._language,
            theme: this._theme,
            readOnly: this._readOnly,
            automaticLayout: true,
            minimap: { enabled: false },
            scrollBeyondLastLine: false,
            wordWrap: "on",
            fontSize: 13,
            lineNumbers: "on"
          });

          this._monacoSubscription = this._monacoEditor.onDidChangeModelContent(() => {
            this._onUserInput(this._monacoEditor.getValue());
          });

          this._textarea.style.display = "none";
          this.setAttribute("data-editor-backend", "monaco");
        } catch {
          this._monacoFailed = true;
          this._disposeMonacoEditor();
        }
      }

      _disposeMonacoEditor() {
        if (this._monacoSubscription?.dispose) {
          this._monacoSubscription.dispose();
        }

        this._monacoSubscription = null;

        if (this._monacoEditor?.dispose) {
          this._monacoEditor.dispose();
        }

        this._monacoEditor = null;

        if (this._monacoHost?.parentElement === this._container) {
          this._container.removeChild(this._monacoHost);
        }

        this._monacoHost = null;
        if (this._textarea) {
          this._textarea.style.display = "";
        }
        this.setAttribute("data-editor-backend", "textarea");
      }
    };

  customElements.define(
    tagName,
    MonacoEditorElement
  );

  const ensureEditorUpgraded = (element) => {
    if (!(element instanceof HTMLElement)) {
      return;
    }

    if (element.localName !== tagName) {
      return;
    }

    if (!(element instanceof MonacoEditorElement)) {
      customElements.upgrade(element);
    }

    if (!element.hasAttribute("data-editor-backend")) {
      element.setAttribute("data-editor-backend", "textarea");
    }
  };

  const upgradeKnownEditors = () => {
    if (typeof document === "undefined") {
      return;
    }

    document.querySelectorAll(tagName).forEach((node) => ensureEditorUpgraded(node));
  };

  if (typeof document !== "undefined") {
    if (document.readyState === "loading") {
      document.addEventListener("DOMContentLoaded", upgradeKnownEditors, { once: true });
    } else {
      upgradeKnownEditors();
    }

    if (!upgradeObserver && typeof MutationObserver === "function") {
      upgradeObserver = new MutationObserver((mutationRecords) => {
        for (const record of mutationRecords) {
          for (const node of record.addedNodes) {
            if (!(node instanceof HTMLElement)) {
              continue;
            }

            ensureEditorUpgraded(node);
            node.querySelectorAll?.(tagName).forEach((child) => ensureEditorUpgraded(child));
          }
        }
      });

      upgradeObserver.observe(document.documentElement, { childList: true, subtree: true });
    }
  }
}
