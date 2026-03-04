# Handlers (Browser)

Current first-slice handlers:
- `BrowserLabelHandler` (`ILabel` -> `<span>`)
- `BrowserButtonHandler` (`ITextButton` -> `<button>`)
- `BrowserEntryHandler` (`IEntry` -> `<input type="text">`)
- `BrowserLayoutHandler` (`ILayout` -> `<div>` + child command mapping)
- `BrowserContentViewHandler` (`IContentView` -> `<div>` + presented content)

Shared pieces:
- `BrowserViewHandler<TVirtualView>` for platform arrange defaults.
- `BrowserViewMapper` for visibility/opacity/padding/text style mapping.

Guardrails:
- Platform view type remains `DomElement`.
- JSImport calls stay in `DomApi`; handlers only use `DomElement`.
