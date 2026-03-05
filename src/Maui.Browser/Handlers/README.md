# Handlers (Browser)

Current first-slice handlers:
- `BrowserWindowHandler` (`Window` -> `<div>` root host)
- `BrowserContentPageHandler` (`ContentPage` -> `<div>` + page content host)
- `BrowserLabelHandler` (`ILabel` -> `<span>`)
- `BrowserButtonHandler` (`ITextButton` -> `<button>`)
- `BrowserEntryHandler` (`IEntry` -> `<input type="text">`)
- `BrowserSwitchHandler` (`ISwitch` -> `<button role="switch">` + track/thumb children)
- `BrowserCheckBoxHandler` (`ICheckBox` -> `<input type="checkbox">`)
- `BrowserSliderHandler` (`ISlider` -> `<input type="range">`)
- `BrowserPickerHandler` (`IPicker` -> `<select>`)
- `BrowserLayoutHandler` (`ILayout` -> `<div>` + child command mapping)
- `BrowserContentViewHandler` (`IContentView` -> `<div>` + presented content)

Shared pieces:
- `BrowserViewHandler<TVirtualView>` for platform arrange defaults.
- `BrowserViewMapper` for visibility/opacity/padding/text style mapping.

Guardrails:
- Platform view type remains `DomElement`.
- JSImport calls stay in `DomApi`; handlers only use `DomElement`.
