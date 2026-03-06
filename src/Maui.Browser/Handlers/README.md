# Handlers (Browser)

Current first-slice handlers:
- `BrowserWindowHandler` (`Window` -> `<div>` root host)
- `BrowserNavigationPageHandler` (`NavigationPage` -> navigation bar + active page host)
- `BrowserContentPageHandler` (`ContentPage` -> `<div>` + page content host)
- `BrowserScrollViewHandler` (`IScrollView` -> `<div>` overflow host + presented content)
- `BrowserLabelHandler` (`ILabel` -> `<span>`)
- `BrowserButtonHandler` (`ITextButton` -> `<button>`)
- `BrowserEntryHandler` (`IEntry` -> `<input type="text">`)
- `BrowserSwitchHandler` (`ISwitch` -> `<button role="switch">` + track/thumb children)
- `BrowserCheckBoxHandler` (`ICheckBox` -> `<input type="checkbox">`)
- `BrowserSliderHandler` (`ISlider` -> `<input type="range">`)
- `BrowserPickerHandler` (`IPicker` -> `<select>`)
- `BrowserCollectionViewHandler` (`CollectionView` -> scroll host + realized item/empty-view DOM projection)
- `BrowserLayoutHandler` (`ILayout` -> `<div>` + child command mapping, stack semantic flex and grid semantic CSS-grid projection with absolute fallback)
- `BrowserContentViewHandler` (`IContentView` -> `<div>` + presented content)

Shared pieces:
- `BrowserViewHandler<TVirtualView>` for platform arrange defaults.
- `BrowserViewMapper` for shared view mapping (visibility/opacity/semantics/transform/focus/etc.).

Guardrails:
- Platform view type remains `DomElement`.
- JSImport calls stay in `DomApi`; handlers only use `DomElement`.
