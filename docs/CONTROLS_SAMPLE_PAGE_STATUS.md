# Controls.Sample Page Status Tracker

Tracks page-level status for vendored upstream Controls.Sample sources under `vendor/upstream/controls-sample/Pages`.

This table is ordered by a suggested implementation progression: foundation and layouts first, then simple controls, then complex scenarios.

Visual legend:
- `Mirage Support`: `[x]` Runnable, `[!]` HiddenUnsupported, `[R]` HiddenRazor, `[P]` HiddenPlatformSpecific, `[ ]` NotImported.
- `E2E Covered`: `[x]` covered, `[ ]` not covered.
- `Manual Parity (User)`: `[x]` validated, `[ ]` pending validation.

| Upstream Page | Source File | Mirage Support | E2E Covered | Manual Parity (User) |
|---|---|---|---|---|
| `AppShell.xaml` | `Pages/AppShell.xaml` | `[ ]` | `[ ]` | `[ ]` |
| `BasePage.cs` | `Pages/Base/BasePage.cs` | `[ ]` | `[ ]` | `[ ]` |
| `ControlsPage.xaml` | `Pages/ControlsPage.xaml` | `[ ]` | `[ ]` | `[ ]` |
| `CustomFlyoutPage.xaml` | `Pages/CustomFlyoutPage.xaml` | `[ ]` | `[ ]` | `[ ]` |
| `CustomNavigationPage.xaml` | `Pages/CustomNavigationPage.xaml` | `[ ]` | `[ ]` | `[ ]` |
| `MainPage.xaml` | `Pages/MainPage.xaml` | `[ ]` | `[ ]` | `[ ]` |
| `OthersPage.xaml` | `Pages/OthersPage.xaml` | `[ ]` | `[ ]` | `[ ]` |
| `SettingsPage.xaml` | `Pages/SettingsPage.xaml` | `[ ]` | `[ ]` | `[ ]` |
| `AbsoluteLayoutPage.xaml` | `Pages/Layouts/AbsoluteLayoutPage.xaml` | `[x]` | `[x]` | `[x]` |
| `ClippingPage.xaml` | `Pages/Layouts/ClippingPage.xaml` | `[x]` | `[ ]` | `[ ]` |
| `ContentViewPage.xaml` | `Pages/Layouts/ContentViewPage.xaml` | `[x]` | `[x]` | `[ ]` |
| `CustomLayoutPage.xaml` | `Pages/Layouts/CustomLayoutPage.xaml` | `[x]` | `[x]` | `[ ]` |
| `FlexLayoutPage.xaml` | `Pages/Layouts/FlexLayoutPage.xaml` | `[x]` | `[x]` | `[ ]` |
| `GridPage.xaml` | `Pages/Layouts/GridPage.xaml` | `[x]` | `[x]` | `[ ]` |
| `HorizontalStackLayoutPage.xaml` | `Pages/Layouts/HorizontalStackLayoutPage.xaml` | `[x]` | `[x]` | `[ ]` |
| `LayoutIsEnabledPage.xaml` | `Pages/Layouts/LayoutIsEnabledPage.xaml` | `[x]` | `[x]` | `[ ]` |
| `LayoutUpdatesPage.cs` | `Pages/Layouts/LayoutUpdatesPage.cs` | `[x]` | `[x]` | `[ ]` |
| `RelativeLayoutPage.xaml` | `Pages/Layouts/RelativeLayoutPage.xaml` | `[x]` | `[x]` | `[ ]` |
| `ScrollViewPage.xaml` | `Pages/Layouts/ScrollViewPage.xaml` | `[x]` | `[x]` | `[ ]` |
| `ScrollToEndPage.xaml` | `Pages/Layouts/ScrollViewPages/ScrollToEndPage.xaml` | `[x]` | `[ ]` | `[ ]` |
| `ScrollToFromConstructorPage.xaml` | `Pages/Layouts/ScrollViewPages/ScrollToFromConstructorPage.xaml` | `[x]` | `[x]` | `[ ]` |
| `ScrollViewOrientationPage.xaml` | `Pages/Layouts/ScrollViewPages/ScrollViewOrientationPage.xaml` | `[x]` | `[x]` | `[ ]` |
| `ScrollViewTemplatePage.xaml` | `Pages/Layouts/ScrollViewPages/ScrollViewTemplatePage.xaml` | `[x]` | `[ ]` | `[ ]` |
| `StackLayoutPage.xaml` | `Pages/Layouts/StackLayoutPage.xaml` | `[x]` | `[x]` | `[ ]` |
| `TemplatedViewPage.xaml` | `Pages/Layouts/TemplatedViewPage.xaml` | `[x]` | `[x]` | `[ ]` |
| `VerticalStackLayoutPage.xaml` | `Pages/Layouts/VerticalStackLayoutPage.xaml` | `[x]` | `[x]` | `[ ]` |
| `ZIndexPage.xaml` | `Pages/Layouts/ZIndexPage.xaml` | `[x]` | `[x]` | `[ ]` |
| `LayoutsPage.xaml` | `Pages/LayoutsPage.xaml` | `[x]` | `[x]` | `[ ]` |
| `AlertsPage.xaml` | `Pages/Core/AlertsPage.xaml` | `[ ]` | `[ ]` | `[ ]` |
| `AppThemeBindingPage.xaml` | `Pages/Core/AppThemeBindingPage.xaml` | `[ ]` | `[ ]` | `[ ]` |
| `ApplicationControlPage.xaml` | `Pages/Core/ApplicationControlPage.xaml` | `[ ]` | `[ ]` | `[ ]` |
| `BorderAlignment.xaml` | `Pages/Core/BorderGalleries/BorderAlignment.xaml` | `[x]` | `[x]` | `[ ]` |
| `BorderClipPlayground.xaml` | `Pages/Core/BorderGalleries/BorderClipPlayground.xaml` | `[x]` | `[x]` | `[ ]` |
| `BorderLayout.xaml` | `Pages/Core/BorderGalleries/BorderLayout.xaml` | `[x]` | `[x]` | `[ ]` |
| `BorderPage.cs` | `Pages/Core/BorderGalleries/BorderPage.cs` | `[x]` | `[x]` | `[ ]` |
| `BorderPlayground.xaml` | `Pages/Core/BorderGalleries/BorderPlayground.xaml` | `[x]` | `[x]` | `[ ]` |
| `BorderResizeContent.xaml` | `Pages/Core/BorderGalleries/BorderResizeContent.xaml` | `[x]` | `[x]` | `[ ]` |
| `BorderStroke.xaml` | `Pages/Core/BorderGalleries/BorderStroke.xaml` | `[x]` | `[x]` | `[ ]` |
| `BorderStyles.xaml` | `Pages/Core/BorderGalleries/BorderStyles.xaml` | `[x]` | `[x]` | `[ ]` |
| `Borderless.xaml` | `Pages/Core/BorderGalleries/Borderless.xaml` | `[x]` | `[x]` | `[ ]` |
| `BrushesPage.xaml` | `Pages/Core/BrushesPage.xaml` | `[x]` | `[x]` | `[ ]` |
| `ClipPage.xaml` | `Pages/Core/ClipPage.xaml` | `[x]` | `[x]` | `[ ]` |
| `ContentPageBackgroundGallery.xaml` | `Pages/Core/ContentPageGalleries/ContentPageBackgroundGallery.xaml` | `[ ]` | `[ ]` | `[ ]` |
| `ContentPageBackgroundImageGallery.xaml` | `Pages/Core/ContentPageGalleries/ContentPageBackgroundImageGallery.xaml` | `[ ]` | `[ ]` | `[ ]` |
| `ContentPageGallery.cs` | `Pages/Core/ContentPageGalleries/ContentPageGallery.cs` | `[ ]` | `[ ]` | `[ ]` |
| `ContextFlyoutPage.xaml` | `Pages/Core/ContextFlyoutPage.xaml` | `[ ]` | `[ ]` | `[ ]` |
| `DevicePage.xaml` | `Pages/Core/DevicePage.xaml` | `[ ]` | `[ ]` | `[ ]` |
| `DispatcherPage.xaml` | `Pages/Core/DispatcherPage.xaml` | `[ ]` | `[ ]` | `[ ]` |
| `DragAndDropBetweenLayouts.xaml` | `Pages/Core/DragAndDropBetweenLayouts.xaml` | `[ ]` | `[ ]` | `[ ]` |
| `DropFileToMauiApp.xaml` | `Pages/Core/DropFileToMauiApp.xaml` | `[ ]` | `[ ]` | `[ ]` |
| `EffectsPage.xaml` | `Pages/Core/EffectsPage.xaml` | `[ ]` | `[ ]` | `[ ]` |
| `FlyoutPageGallery.xaml` | `Pages/Core/FlyoutPageGallery.xaml` | `[ ]` | `[ ]` | `[ ]` |
| `FocusPage.xaml` | `Pages/Core/FocusPage.xaml` | `[ ]` | `[ ]` | `[ ]` |
| `GesturesPage.xaml` | `Pages/Core/GesturesPage.xaml` | `[ ]` | `[ ]` | `[ ]` |
| `InputTransparentPage.xaml` | `Pages/Core/InputTransparentPage.xaml` | `[!]` | `[ ]` | `[ ]` |
| `MenuBarPage.xaml` | `Pages/Core/MenuBarPage.xaml` | `[ ]` | `[ ]` | `[ ]` |
| `ModalPage.xaml` | `Pages/Core/ModalPage.xaml` | `[ ]` | `[ ]` | `[ ]` |
| `MultiWindowPage.xaml` | `Pages/Core/MultiWindowPage.xaml` | `[ ]` | `[ ]` | `[ ]` |
| `NavigationGallery.xaml` | `Pages/Core/NavigationGallery.xaml` | `[ ]` | `[ ]` | `[ ]` |
| `PanGestureEventsGallery.xaml` | `Pages/Core/PanGestureGalleries/PanGestureEventsGallery.xaml` | `[ ]` | `[ ]` | `[ ]` |
| `PanGestureGallery.cs` | `Pages/Core/PanGestureGalleries/PanGestureGallery.cs` | `[ ]` | `[ ]` | `[ ]` |
| `PanGesturePlaygroundGallery.cs` | `Pages/Core/PanGestureGalleries/PanGesturePlaygroundGallery.cs` | `[ ]` | `[ ]` | `[ ]` |
| `PinchGestureTestPage.cs` | `Pages/Core/PinchGestureTestPage.cs` | `[ ]` | `[ ]` | `[ ]` |
| `PointerGestureGalleryPage.xaml` | `Pages/Core/PointerGestureGalleryPage.xaml` | `[ ]` | `[ ]` | `[ ]` |
| `SemanticsPage.xaml` | `Pages/Core/SemanticsPage.xaml` | `[ ]` | `[ ]` | `[ ]` |
| `InvalidateShadowHostPage.xaml` | `Pages/Core/ShadowGalleries/InvalidateShadowHostPage.xaml` | `[ ]` | `[ ]` | `[ ]` |
| `ShadowPage.cs` | `Pages/Core/ShadowGalleries/ShadowPage.cs` | `[ ]` | `[ ]` | `[ ]` |
| `ShadowPlaygroundPage.xaml` | `Pages/Core/ShadowGalleries/ShadowPlaygroundPage.xaml` | `[ ]` | `[ ]` | `[ ]` |
| `ShellChromeGallery.xaml` | `Pages/Core/ShellGalleries/ShellChromeGallery.xaml` | `[ ]` | `[ ]` | `[ ]` |
| `SwipeGestureGalleryPage.cs` | `Pages/Core/SwipeGestureGalleryPage.cs` | `[ ]` | `[ ]` | `[ ]` |
| `TapGestureGalleryPage.cs` | `Pages/Core/TapGestureGalleryPage.cs` | `[ ]` | `[ ]` | `[ ]` |
| `ToolbarPage.xaml` | `Pages/Core/ToolbarPage.xaml` | `[ ]` | `[ ]` | `[ ]` |
| `TransformationsPage.xaml` | `Pages/Core/TransformationsPage.xaml` | `[x]` | `[x]` | `[ ]` |
| `WindowTitleBar.xaml` | `Pages/Core/WindowTitleBar.xaml` | `[ ]` | `[ ]` | `[ ]` |
| `CorePage.xaml` | `Pages/CorePage.xaml` | `[ ]` | `[ ]` | `[ ]` |
| `HitTestingPage.xaml` | `Pages/HitTestingPage.xaml` | `[ ]` | `[ ]` | `[ ]` |
| `ActivityIndicatorPage.xaml` | `Pages/Controls/ActivityIndicatorPage.xaml` | `[x]` | `[x]` | `[ ]` |
| `BoxViewPage.xaml` | `Pages/Controls/BoxViewPage.xaml` | `[x]` | `[x]` | `[ ]` |
| `ButtonPage.xaml` | `Pages/Controls/ButtonPage.xaml` | `[x]` | `[x]` | `[ ]` |
| `CheckBoxPage.xaml` | `Pages/Controls/CheckBoxPage.xaml` | `[x]` | `[x]` | `[ ]` |
| `DatePickerPage.xaml` | `Pages/Controls/DatePickerPage.xaml` | `[ ]` | `[ ]` | `[ ]` |
| `EditorPage.xaml` | `Pages/Controls/EditorPage.xaml` | `[ ]` | `[ ]` | `[ ]` |
| `EntryPage.xaml` | `Pages/Controls/EntryPage.xaml` | `[ ]` | `[ ]` | `[ ]` |
| `HybridWebViewPage.xaml` | `Pages/Controls/HybridWebViewPage.xaml` | `[ ]` | `[ ]` | `[ ]` |
| `ImageButtonPage.xaml` | `Pages/Controls/ImageButtonPage.xaml` | `[ ]` | `[ ]` | `[ ]` |
| `ImagePage.xaml` | `Pages/Controls/ImagePage.xaml` | `[x]` | `[x]` | `[ ]` |
| `IndicatorPage.xaml` | `Pages/Controls/IndicatorPage.xaml` | `[ ]` | `[ ]` | `[ ]` |
| `LabelPage.xaml` | `Pages/Controls/LabelPage.xaml` | `[ ]` | `[ ]` | `[ ]` |
| `PickerPage.xaml` | `Pages/Controls/PickerPage.xaml` | `[ ]` | `[ ]` | `[ ]` |
| `ProgressBarPage.xaml` | `Pages/Controls/ProgressBarPage.xaml` | `[ ]` | `[ ]` | `[ ]` |
| `RefreshViewPage.xaml` | `Pages/Controls/RefreshViewPage.xaml` | `[ ]` | `[ ]` | `[ ]` |
| `SearchBarPage.xaml` | `Pages/Controls/SearchBarPage.xaml` | `[ ]` | `[ ]` | `[ ]` |
| `ShapesPage.xaml` | `Pages/Controls/ShapesPage.xaml` | `[ ]` | `[ ]` | `[ ]` |
| `SliderPage.xaml` | `Pages/Controls/SliderPage.xaml` | `[x]` | `[x]` | `[ ]` |
| `StepperPage.xaml` | `Pages/Controls/StepperPage.xaml` | `[x]` | `[x]` | `[ ]` |
| `SwitchPage.xaml` | `Pages/Controls/SwitchPage.xaml` | `[ ]` | `[ ]` | `[ ]` |
| `TimePickerPage.xaml` | `Pages/Controls/TimePickerPage.xaml` | `[ ]` | `[ ]` | `[ ]` |
| `TitleBarPage.xaml` | `Pages/Controls/TitleBarPage.xaml` | `[ ]` | `[ ]` | `[ ]` |
| `CarouselCodeGallery.cs` | `Pages/Controls/CarouselViewGalleries/CarouselCodeGallery.cs` | `[ ]` | `[ ]` | `[ ]` |
| `CarouselItemsGallery.cs` | `Pages/Controls/CarouselViewGalleries/CarouselItemsGallery.cs` | `[ ]` | `[ ]` | `[ ]` |
| `CarouselSnapGallery.cs` | `Pages/Controls/CarouselViewGalleries/CarouselSnapGallery.cs` | `[ ]` | `[ ]` | `[ ]` |
| `CarouselViewGallery.cs` | `Pages/Controls/CarouselViewGalleries/CarouselViewGallery.cs` | `[ ]` | `[ ]` | `[ ]` |
| `CarouselXamlGallery.xaml` | `Pages/Controls/CarouselViewGalleries/CarouselXamlGallery.xaml` | `[ ]` | `[ ]` | `[ ]` |
| `CollectionCarouselViewGallery.cs` | `Pages/Controls/CarouselViewGalleries/CollectionCarouselViewGallery.cs` | `[ ]` | `[ ]` | `[ ]` |
| `EmptyCarouselGallery.xaml` | `Pages/Controls/CarouselViewGalleries/EmptyCarouselGallery.xaml` | `[ ]` | `[ ]` | `[ ]` |
| `ExampleTemplateCarousel.xaml` | `Pages/Controls/CarouselViewGalleries/ExampleTemplateCarousel.xaml` | `[ ]` | `[ ]` | `[ ]` |
| `IndicatorCodeGallery.cs` | `Pages/Controls/CarouselViewGalleries/IndicatorCodeGallery.cs` | `[ ]` | `[ ]` | `[ ]` |
| `AdaptiveCollectionView.xaml` | `Pages/Controls/CollectionViewGalleries/AdaptiveCollectionView.xaml` | `[x]` | `[x]` | `[ ]` |
| `AlternateLayoutGallery.cs` | `Pages/Controls/CollectionViewGalleries/AlternateLayoutGalleries/AlternateLayoutGallery.cs` | `[x]` | `[x]` | `[ ]` |
| `StaggeredLayout.xaml` | `Pages/Controls/CollectionViewGalleries/AlternateLayoutGalleries/StaggeredLayout.xaml` | `[ ]` | `[ ]` | `[ ]` |
| `CarouselViewPage.xaml` | `Pages/Controls/CollectionViewGalleries/CarouselViewGalleries/CarouselViewPage.xaml` | `[ ]` | `[ ]` | `[ ]` |
| `CollectionViewGallery.cs` | `Pages/Controls/CollectionViewGalleries/CollectionViewGallery.cs` | `[x]` | `[x]` | `[ ]` |
| `DataTemplateGallery.cs` | `Pages/Controls/CollectionViewGalleries/DataTemplateGallery.cs` | `[x]` | `[x]` | `[ ]` |
| `VariedSizeDataTemplateSelectorGallery.xaml` | `Pages/Controls/CollectionViewGalleries/DataTemplateSelectorGalleries/VariedSizeDataTemplateSelectorGallery.xaml` | `[x]` | `[x]` | `[ ]` |
| `DataTemplateSelectorGallery.xaml` | `Pages/Controls/CollectionViewGalleries/DataTemplateSelectorGallery.xaml` | `[ ]` | `[ ]` | `[ ]` |
| `DefaultTextGallery.cs` | `Pages/Controls/CollectionViewGalleries/DefaultTextGallery.cs` | `[x]` | `[x]` | `[ ]` |
| `EmptyViewGallery.cs` | `Pages/Controls/CollectionViewGalleries/EmptyViewGalleries/EmptyViewGallery.cs` | `[x]` | `[x]` | `[ ]` |
| `EmptyViewLoadSimulateGallery.xaml` | `Pages/Controls/CollectionViewGalleries/EmptyViewGalleries/EmptyViewLoadSimulateGallery.xaml` | `[x]` | `[x]` | `[ ]` |
| `EmptyViewNullGallery.xaml` | `Pages/Controls/CollectionViewGalleries/EmptyViewGalleries/EmptyViewNullGallery.xaml` | `[x]` | `[x]` | `[ ]` |
| `EmptyViewRTLGallery.xaml` | `Pages/Controls/CollectionViewGalleries/EmptyViewGalleries/EmptyViewRTLGallery.xaml` | `[ ]` | `[ ]` | `[ ]` |
| `EmptyViewStringGallery.xaml` | `Pages/Controls/CollectionViewGalleries/EmptyViewGalleries/EmptyViewStringGallery.xaml` | `[ ]` | `[ ]` | `[ ]` |
| `EmptyViewSwapGallery.xaml` | `Pages/Controls/CollectionViewGalleries/EmptyViewGalleries/EmptyViewSwapGallery.xaml` | `[ ]` | `[ ]` | `[ ]` |
| `EmptyViewTemplateGallery.xaml` | `Pages/Controls/CollectionViewGalleries/EmptyViewGalleries/EmptyViewTemplateGallery.xaml` | `[ ]` | `[ ]` | `[ ]` |
| `EmptyViewViewGallery.xaml` | `Pages/Controls/CollectionViewGalleries/EmptyViewGalleries/EmptyViewViewGallery.xaml` | `[ ]` | `[ ]` | `[ ]` |
| `EmptyViewWithDataTemplateSelector.xaml` | `Pages/Controls/CollectionViewGalleries/EmptyViewGalleries/EmptyViewWithDataTemplateSelector.xaml` | `[ ]` | `[ ]` | `[ ]` |
| `FilterCollectionView.xaml` | `Pages/Controls/CollectionViewGalleries/FilterCollectionView.xaml` | `[ ]` | `[ ]` | `[ ]` |
| `BasicGrouping.xaml` | `Pages/Controls/CollectionViewGalleries/GroupingGalleries/BasicGrouping.xaml` | `[x]` | `[x]` | `[ ]` |
| `GridGrouping.xaml` | `Pages/Controls/CollectionViewGalleries/GroupingGalleries/GridGrouping.xaml` | `[x]` | `[x]` | `[ ]` |
| `GroupingGallery.cs` | `Pages/Controls/CollectionViewGalleries/GroupingGalleries/GroupingGallery.cs` | `[x]` | `[x]` | `[ ]` |
| `GroupingNoTemplates.xaml` | `Pages/Controls/CollectionViewGalleries/GroupingGalleries/GroupingNoTemplates.xaml` | `[x]` | `[x]` | `[ ]` |
| `GroupingPlusSelection.xaml` | `Pages/Controls/CollectionViewGalleries/GroupingGalleries/GroupingPlusSelection.xaml` | `[x]` | `[x]` | `[ ]` |
| `MeasureFirstStrategy.xaml` | `Pages/Controls/CollectionViewGalleries/GroupingGalleries/MeasureFirstStrategy.xaml` | `[ ]` | `[ ]` | `[ ]` |
| `ObservableGrouping.cs` | `Pages/Controls/CollectionViewGalleries/GroupingGalleries/ObservableGrouping.cs` | `[x]` | `[x]` | `[ ]` |
| `SomeEmptyGroups.xaml` | `Pages/Controls/CollectionViewGalleries/GroupingGalleries/SomeEmptyGroups.xaml` | `[x]` | `[x]` | `[ ]` |
| `SwitchGrouping.xaml` | `Pages/Controls/CollectionViewGalleries/GroupingGalleries/SwitchGrouping.xaml` | `[x]` | `[x]` | `[ ]` |
| `FooterOnlyString.xaml` | `Pages/Controls/CollectionViewGalleries/HeaderFooterGalleries/FooterOnlyString.xaml` | `[x]` | `[x]` | `[ ]` |
| `HeaderFooterGallery.cs` | `Pages/Controls/CollectionViewGalleries/HeaderFooterGalleries/HeaderFooterGallery.cs` | `[x]` | `[x]` | `[ ]` |
| `HeaderFooterGrid.xaml` | `Pages/Controls/CollectionViewGalleries/HeaderFooterGalleries/HeaderFooterGrid.xaml` | `[x]` | `[x]` | `[ ]` |
| `HeaderFooterGridHorizontal.xaml` | `Pages/Controls/CollectionViewGalleries/HeaderFooterGalleries/HeaderFooterGridHorizontal.xaml` | `[x]` | `[x]` | `[ ]` |
| `HeaderFooterString.xaml` | `Pages/Controls/CollectionViewGalleries/HeaderFooterGalleries/HeaderFooterString.xaml` | `[x]` | `[x]` | `[ ]` |
| `HeaderFooterTemplate.xaml` | `Pages/Controls/CollectionViewGalleries/HeaderFooterGalleries/HeaderFooterTemplate.xaml` | `[x]` | `[x]` | `[ ]` |
| `HeaderFooterView.xaml` | `Pages/Controls/CollectionViewGalleries/HeaderFooterGalleries/HeaderFooterView.xaml` | `[x]` | `[x]` | `[ ]` |
| `ChatExample.xaml` | `Pages/Controls/CollectionViewGalleries/ItemSizeGalleries/ChatExample.xaml` | `[x]` | `[x]` | `[ ]` |
| `DynamicItemSizeGallery.cs` | `Pages/Controls/CollectionViewGalleries/ItemSizeGalleries/DynamicItemSizeGallery.cs` | `[x]` | `[x]` | `[ ]` |
| `ItemsSizeGallery.cs` | `Pages/Controls/CollectionViewGalleries/ItemSizeGalleries/ItemsSizeGallery.cs` | `[x]` | `[x]` | `[ ]` |
| `VariableSizeTemplateGridGallery.cs` | `Pages/Controls/CollectionViewGalleries/ItemSizeGalleries/VariableSizeTemplateGridGallery.cs` | `[x]` | `[x]` | `[ ]` |
| `NestedCollectionViewGallery.xaml` | `Pages/Controls/CollectionViewGalleries/NestedGalleries/NestedCollectionViewGallery.xaml` | `[x]` | `[x]` | `[ ]` |
| `ObservableCodeCollectionViewGallery.cs` | `Pages/Controls/CollectionViewGalleries/ObservableCodeCollectionViewGallery.cs` | `[x]` | `[x]` | `[ ]` |
| `ObservableCollectionGallery.cs` | `Pages/Controls/CollectionViewGalleries/ObservableCollectionGallery.cs` | `[x]` | `[x]` | `[ ]` |
| `ObservableCollectionResetGallery.cs` | `Pages/Controls/CollectionViewGalleries/ObservableCollectionResetGallery.cs` | `[x]` | `[x]` | `[ ]` |
| `ObservableMultiItemCollectionViewGallery.cs` | `Pages/Controls/CollectionViewGalleries/ObservableMultiItemCollectionViewGallery.cs` | `[x]` | `[x]` | `[ ]` |
| `OnlineImages.xaml` | `Pages/Controls/CollectionViewGalleries/OnlineImages.xaml` | `[x]` | `[x]` | `[ ]` |
| `PropagateCodeGallery.cs` | `Pages/Controls/CollectionViewGalleries/PropagateCodeGallery.cs` | `[x]` | `[x]` | `[ ]` |
| `PropagationGallery.cs` | `Pages/Controls/CollectionViewGalleries/PropagationGallery.cs` | `[x]` | `[x]` | `[ ]` |
| `GroupedReorderingGallery.cs` | `Pages/Controls/CollectionViewGalleries/ReorderingGalleries/GroupedReorderingGallery.cs` | `[x]` | `[x]` | `[ ]` |
| `ReorderingGallery.cs` | `Pages/Controls/CollectionViewGalleries/ReorderingGalleries/ReorderingGallery.cs` | `[x]` | `[x]` | `[ ]` |
| `UngroupedReorderingGallery.cs` | `Pages/Controls/CollectionViewGalleries/ReorderingGalleries/UngroupedReorderingGallery.cs` | `[x]` | `[x]` | `[ ]` |
| `ItemsUpdatingScrollModeGallery.xaml` | `Pages/Controls/CollectionViewGalleries/ScrollModeGalleries/ItemsUpdatingScrollModeGallery.xaml` | `[x]` | `[x]` | `[ ]` |
| `ScrollModeGallery.cs` | `Pages/Controls/CollectionViewGalleries/ScrollModeGalleries/ScrollModeGallery.cs` | `[x]` | `[x]` | `[ ]` |
| `ScrollModeTestGallery.xaml` | `Pages/Controls/CollectionViewGalleries/ScrollModeGalleries/ScrollModeTestGallery.xaml` | `[x]` | `[x]` | `[ ]` |
| `ScrollToCodeGallery.cs` | `Pages/Controls/CollectionViewGalleries/ScrollToCodeGallery.cs` | `[x]` | `[x]` | `[ ]` |
| `ScrollToGroup.xaml` | `Pages/Controls/CollectionViewGalleries/ScrollToGalleries/ScrollToGroup.xaml` | `[x]` | `[x]` | `[ ]` |
| `ScrollToGallery.cs` | `Pages/Controls/CollectionViewGalleries/ScrollToGallery.cs` | `[x]` | `[x]` | `[ ]` |
| `FilterSelection.xaml` | `Pages/Controls/CollectionViewGalleries/SelectionGalleries/FilterSelection.xaml` | `[ ]` | `[ ]` | `[ ]` |
| `MultipleBoundSelection.xaml` | `Pages/Controls/CollectionViewGalleries/SelectionGalleries/MultipleBoundSelection.xaml` | `[x]` | `[x]` | `[ ]` |
| `PreselectedItemGallery.xaml` | `Pages/Controls/CollectionViewGalleries/SelectionGalleries/PreselectedItemGallery.xaml` | `[x]` | `[x]` | `[ ]` |
| `PreselectedItemsGallery.xaml` | `Pages/Controls/CollectionViewGalleries/SelectionGalleries/PreselectedItemsGallery.xaml` | `[x]` | `[x]` | `[ ]` |
| `SelectionChangedCommandParameter.xaml` | `Pages/Controls/CollectionViewGalleries/SelectionGalleries/SelectionChangedCommandParameter.xaml` | `[x]` | `[x]` | `[ ]` |
| `SelectionGallery.cs` | `Pages/Controls/CollectionViewGalleries/SelectionGalleries/SelectionGallery.cs` | `[x]` | `[x]` | `[ ]` |
| `SelectionModeGallery.xaml` | `Pages/Controls/CollectionViewGalleries/SelectionGalleries/SelectionModeGallery.xaml` | `[x]` | `[x]` | `[ ]` |
| `SelectionSynchronization.xaml` | `Pages/Controls/CollectionViewGalleries/SelectionGalleries/SelectionSynchronization.xaml` | `[x]` | `[x]` | `[ ]` |
| `SingleBoundSelection.xaml` | `Pages/Controls/CollectionViewGalleries/SelectionGalleries/SingleBoundSelection.xaml` | `[x]` | `[x]` | `[ ]` |
| `VisualStatesGallery.xaml` | `Pages/Controls/CollectionViewGalleries/SelectionGalleries/VisualStatesGallery.xaml` | `[x]` | `[x]` | `[ ]` |
| `SnapPointsCodeGallery.cs` | `Pages/Controls/CollectionViewGalleries/SnapPointsCodeGallery.cs` | `[x]` | `[x]` | `[ ]` |
| `SnapPointsGallery.cs` | `Pages/Controls/CollectionViewGalleries/SnapPointsGallery.cs` | `[x]` | `[x]` | `[ ]` |
| `ItemsSpacingGallery.cs` | `Pages/Controls/CollectionViewGalleries/SpacingGalleries/ItemsSpacingGallery.cs` | `[x]` | `[x]` | `[ ]` |
| `SpacingGallery.cs` | `Pages/Controls/CollectionViewGalleries/SpacingGalleries/SpacingGallery.cs` | `[x]` | `[x]` | `[ ]` |
| `TemplateCodeCollectionViewGallery.cs` | `Pages/Controls/CollectionViewGalleries/TemplateCodeCollectionViewGallery.cs` | `[x]` | `[x]` | `[ ]` |
| `TemplateCodeCollectionViewGridGallery.cs` | `Pages/Controls/CollectionViewGalleries/TemplateCodeCollectionViewGridGallery.cs` | `[x]` | `[x]` | `[ ]` |
| `TextCodeCollectionViewGallery.cs` | `Pages/Controls/CollectionViewGalleries/TextCodeCollectionViewGallery.cs` | `[x]` | `[x]` | `[ ]` |
| `TextCodeCollectionViewGridGallery.cs` | `Pages/Controls/CollectionViewGalleries/TextCodeCollectionViewGridGallery.cs` | `[x]` | `[x]` | `[ ]` |
| `ContentProperties.xaml` | `Pages/Controls/RadioButtonGalleries/ContentProperties.xaml` | `[ ]` | `[ ]` | `[ ]` |
| `RadioButtonBorder.xaml` | `Pages/Controls/RadioButtonGalleries/RadioButtonBorder.xaml` | `[ ]` | `[ ]` | `[ ]` |
| `RadioButtonContentGallery.xaml` | `Pages/Controls/RadioButtonGalleries/RadioButtonContentGallery.xaml` | `[ ]` | `[ ]` | `[ ]` |
| `RadioButtonGalleries.cs` | `Pages/Controls/RadioButtonGalleries/RadioButtonGalleries.cs` | `[ ]` | `[ ]` | `[ ]` |
| `RadioButtonGroupBindingGallery.xaml` | `Pages/Controls/RadioButtonGalleries/RadioButtonGroupBindingGallery.xaml` | `[ ]` | `[ ]` | `[ ]` |
| `RadioButtonGroupGallery.xaml` | `Pages/Controls/RadioButtonGalleries/RadioButtonGroupGallery.xaml` | `[ ]` | `[ ]` | `[ ]` |
| `RadioButtonGroupGalleryPage.xaml` | `Pages/Controls/RadioButtonGalleries/RadioButtonGroupGalleryPage.xaml` | `[ ]` | `[ ]` | `[ ]` |
| `ScatteredRadioButtonGallery.xaml` | `Pages/Controls/RadioButtonGalleries/ScatteredRadioButtonGallery.xaml` | `[ ]` | `[ ]` | `[ ]` |
| `TemplateFromStyle.xaml` | `Pages/Controls/RadioButtonGalleries/TemplateFromStyle.xaml` | `[ ]` | `[ ]` | `[ ]` |
| `AddRemoveClipGallery.cs` | `Pages/Controls/ShapesGalleries/AddRemoveClipGallery.cs` | `[ ]` | `[ ]` | `[ ]` |
| `AnimateShapeGallery.cs` | `Pages/Controls/ShapesGalleries/AnimateShapeGallery.cs` | `[ ]` | `[ ]` | `[ ]` |
| `AutoSizeShapesGallery.xaml` | `Pages/Controls/ShapesGalleries/AutoSizeShapesGallery.xaml` | `[ ]` | `[ ]` | `[ ]` |
| `ClipCornerRadiusGallery.xaml` | `Pages/Controls/ShapesGalleries/ClipCornerRadiusGallery.xaml` | `[ ]` | `[ ]` | `[ ]` |
| `ClipGallery.xaml` | `Pages/Controls/ShapesGalleries/ClipGallery.xaml` | `[ ]` | `[ ]` | `[ ]` |
| `ClipPerformanceGallery.cs` | `Pages/Controls/ShapesGalleries/ClipPerformanceGallery.cs` | `[ ]` | `[ ]` | `[ ]` |
| `ClipViewsGallery.xaml` | `Pages/Controls/ShapesGalleries/ClipViewsGallery.xaml` | `[ ]` | `[ ]` | `[ ]` |
| `CompositionGallery.xaml` | `Pages/Controls/ShapesGalleries/CompositionGallery.xaml` | `[ ]` | `[ ]` | `[ ]` |
| `EllipseGallery.xaml` | `Pages/Controls/ShapesGalleries/EllipseGallery.xaml` | `[ ]` | `[ ]` | `[ ]` |
| `InvalidateBrushGallery.xaml` | `Pages/Controls/ShapesGalleries/InvalidateBrushGallery.xaml` | `[ ]` | `[ ]` | `[ ]` |
| `InvalidateClipGallery.xaml` | `Pages/Controls/ShapesGalleries/InvalidateClipGallery.xaml` | `[ ]` | `[ ]` | `[ ]` |
| `LineCapGallery.xaml` | `Pages/Controls/ShapesGalleries/LineCapGallery.xaml` | `[ ]` | `[ ]` | `[ ]` |
| `LineGallery.xaml` | `Pages/Controls/ShapesGalleries/LineGallery.xaml` | `[ ]` | `[ ]` | `[ ]` |
| `LineJoinGallery.xaml` | `Pages/Controls/ShapesGalleries/LineJoinGallery.xaml` | `[ ]` | `[ ]` | `[ ]` |
| `PathAspectGallery.xaml` | `Pages/Controls/ShapesGalleries/PathAspectGallery.xaml` | `[ ]` | `[ ]` | `[ ]` |
| `PathGallery.xaml` | `Pages/Controls/ShapesGalleries/PathGallery.xaml` | `[ ]` | `[ ]` | `[ ]` |
| `PathLayoutOptionsGallery.xaml` | `Pages/Controls/ShapesGalleries/PathLayoutOptionsGallery.xaml` | `[ ]` | `[ ]` | `[ ]` |
| `PathTransformStringGallery.xaml` | `Pages/Controls/ShapesGalleries/PathTransformStringGallery.xaml` | `[ ]` | `[ ]` | `[ ]` |
| `PolygonGallery.xaml` | `Pages/Controls/ShapesGalleries/PolygonGallery.xaml` | `[ ]` | `[ ]` | `[ ]` |
| `PolylineGallery.xaml` | `Pages/Controls/ShapesGalleries/PolylineGallery.xaml` | `[ ]` | `[ ]` | `[ ]` |
| `RectangleGallery.xaml` | `Pages/Controls/ShapesGalleries/RectangleGallery.xaml` | `[ ]` | `[ ]` | `[ ]` |
| `ShapeAppThemeGallery.xaml` | `Pages/Controls/ShapesGalleries/ShapeAppThemeGallery.xaml` | `[ ]` | `[ ]` | `[ ]` |
| `ShapeBackgroundGallery.xaml` | `Pages/Controls/ShapesGalleries/ShapeBackgroundGallery.xaml` | `[ ]` | `[ ]` | `[ ]` |
| `ShapesGallery.cs` | `Pages/Controls/ShapesGalleries/ShapesGallery.cs` | `[ ]` | `[ ]` | `[ ]` |
| `TransformPlaygroundGallery.xaml` | `Pages/Controls/ShapesGalleries/TransformPlaygroundGallery.xaml` | `[ ]` | `[ ]` | `[ ]` |
| `UpdatePathDataGallery.xaml` | `Pages/Controls/ShapesGalleries/UpdatePathDataGallery.xaml` | `[ ]` | `[ ]` | `[ ]` |
| `AddRemoveSwipeItemsGallery.cs` | `Pages/Controls/SwipeViewGalleries/AddRemoveSwipeItemsGallery.cs` | `[ ]` | `[ ]` | `[ ]` |
| `BasicSwipeGallery.xaml` | `Pages/Controls/SwipeViewGalleries/BasicSwipeGallery.xaml` | `[ ]` | `[ ]` | `[ ]` |
| `CustomSizeSwipeViewGallery.xaml` | `Pages/Controls/SwipeViewGalleries/CustomSizeSwipeViewGallery.xaml` | `[ ]` | `[ ]` | `[ ]` |
| `CustomSwipeControl.xaml` | `Pages/Controls/SwipeViewGalleries/CustomSwipeControl.xaml` | `[ ]` | `[ ]` | `[ ]` |
| `CustomSwipeControlGallery.xaml` | `Pages/Controls/SwipeViewGalleries/CustomSwipeControlGallery.xaml` | `[ ]` | `[ ]` | `[ ]` |
| `CustomSwipeItemGallery.cs` | `Pages/Controls/SwipeViewGalleries/CustomSwipeItemGallery.cs` | `[ ]` | `[ ]` | `[ ]` |
| `CustomSwipeItemViewGallery.xaml` | `Pages/Controls/SwipeViewGalleries/CustomSwipeItemViewGallery.xaml` | `[ ]` | `[ ]` | `[ ]` |
| `CustomizeSwipeItemGallery.cs` | `Pages/Controls/SwipeViewGalleries/CustomizeSwipeItemGallery.cs` | `[ ]` | `[ ]` | `[ ]` |
| `HorizontalSwipeThresholdGallery.xaml` | `Pages/Controls/SwipeViewGalleries/HorizontalSwipeThresholdGallery.xaml` | `[ ]` | `[ ]` | `[ ]` |
| `NoIconTextSwipeItemGallery.cs` | `Pages/Controls/SwipeViewGalleries/NoIconTextSwipeItemGallery.cs` | `[ ]` | `[ ]` | `[ ]` |
| `OpenCloseSwipeGallery.cs` | `Pages/Controls/SwipeViewGalleries/OpenCloseSwipeGallery.cs` | `[ ]` | `[ ]` | `[ ]` |
| `PositionGallery.cs` | `Pages/Controls/SwipeViewGalleries/PositionGallery.cs` | `[ ]` | `[ ]` | `[ ]` |
| `ResourceSwipeItemsGallery.xaml` | `Pages/Controls/SwipeViewGalleries/ResourceSwipeItemsGallery.xaml` | `[ ]` | `[ ]` | `[ ]` |
| `SwipeBehaviorOnInvokedGallery.cs` | `Pages/Controls/SwipeViewGalleries/SwipeBehaviorOnInvokedGallery.cs` | `[ ]` | `[ ]` | `[ ]` |
| `SwipeBindableLayoutGallery.xaml` | `Pages/Controls/SwipeViewGalleries/SwipeBindableLayoutGallery.xaml` | `[ ]` | `[ ]` | `[ ]` |
| `SwipeCarouselViewGallery.xaml` | `Pages/Controls/SwipeViewGalleries/SwipeCarouselViewGallery.xaml` | `[ ]` | `[ ]` | `[ ]` |
| `SwipeCollectionViewGallery.cs` | `Pages/Controls/SwipeViewGalleries/SwipeCollectionViewGallery.cs` | `[ ]` | `[ ]` | `[ ]` |
| `SwipeHorizontalCollectionViewGallery.xaml` | `Pages/Controls/SwipeViewGalleries/SwipeHorizontalCollectionViewGallery.xaml` | `[ ]` | `[ ]` | `[ ]` |
| `SwipeItemIconGallery.cs` | `Pages/Controls/SwipeViewGalleries/SwipeItemIconGallery.cs` | `[ ]` | `[ ]` | `[ ]` |
| `SwipeItemIsEnabledGallery.cs` | `Pages/Controls/SwipeViewGalleries/SwipeItemIsEnabledGallery.cs` | `[ ]` | `[ ]` | `[ ]` |
| `SwipeItemIsVisibleGallery.cs` | `Pages/Controls/SwipeViewGalleries/SwipeItemIsVisibleGallery.cs` | `[ ]` | `[ ]` | `[ ]` |
| `SwipeItemPositionGallery.xaml` | `Pages/Controls/SwipeViewGalleries/SwipeItemPositionGallery.xaml` | `[ ]` | `[ ]` | `[ ]` |
| `SwipeItemSizeGallery.xaml` | `Pages/Controls/SwipeViewGalleries/SwipeItemSizeGallery.xaml` | `[ ]` | `[ ]` | `[ ]` |
| `SwipeItemViewIsEnabledGallery.cs` | `Pages/Controls/SwipeViewGalleries/SwipeItemViewIsEnabledGallery.cs` | `[ ]` | `[ ]` | `[ ]` |
| `SwipeItemViewPositionGallery.xaml` | `Pages/Controls/SwipeViewGalleries/SwipeItemViewPositionGallery.xaml` | `[ ]` | `[ ]` | `[ ]` |
| `SwipeItemsDisposeGallery.xaml` | `Pages/Controls/SwipeViewGalleries/SwipeItemsDisposeGallery.xaml` | `[ ]` | `[ ]` | `[ ]` |
| `SwipeListViewGallery.xaml` | `Pages/Controls/SwipeViewGalleries/SwipeListViewGallery.xaml` | `[ ]` | `[ ]` | `[ ]` |
| `SwipeThresholdCustomSwipeItemGallery.xaml` | `Pages/Controls/SwipeViewGalleries/SwipeThresholdCustomSwipeItemGallery.xaml` | `[ ]` | `[ ]` | `[ ]` |
| `SwipeThresholdGallery.cs` | `Pages/Controls/SwipeViewGalleries/SwipeThresholdGallery.cs` | `[ ]` | `[ ]` | `[ ]` |
| `SwipeTransitionModeGallery.cs` | `Pages/Controls/SwipeViewGalleries/SwipeTransitionModeGallery.cs` | `[ ]` | `[ ]` | `[ ]` |
| `SwipeVerticalCollectionViewGallery.xaml` | `Pages/Controls/SwipeViewGalleries/SwipeVerticalCollectionViewGallery.xaml` | `[ ]` | `[ ]` | `[ ]` |
| `SwipeViewBindingContextGallery.xaml` | `Pages/Controls/SwipeViewGalleries/SwipeViewBindingContextGallery.xaml` | `[ ]` | `[ ]` | `[ ]` |
| `SwipeViewEventsGallery.cs` | `Pages/Controls/SwipeViewGalleries/SwipeViewEventsGallery.cs` | `[ ]` | `[ ]` | `[ ]` |
| `SwipeViewGallery.cs` | `Pages/Controls/SwipeViewGalleries/SwipeViewGallery.cs` | `[ ]` | `[ ]` | `[ ]` |
| `SwipeViewGestureRecognizerGallery.xaml` | `Pages/Controls/SwipeViewGalleries/SwipeViewGestureRecognizerGallery.xaml` | `[ ]` | `[ ]` | `[ ]` |
| `SwipeViewMarginGallery.xaml` | `Pages/Controls/SwipeViewGalleries/SwipeViewMarginGallery.xaml` | `[ ]` | `[ ]` | `[ ]` |
| `SwipeViewNoLayoutGallery.xaml` | `Pages/Controls/SwipeViewGalleries/SwipeViewNoLayoutGallery.xaml` | `[ ]` | `[ ]` | `[ ]` |
| `SwipeViewShadowGallery.xaml` | `Pages/Controls/SwipeViewGalleries/SwipeViewShadowGallery.xaml` | `[ ]` | `[ ]` | `[ ]` |
| `SwipeViewVisualStatesCollectionGallery.xaml` | `Pages/Controls/SwipeViewGalleries/SwipeViewVisualStatesCollectionGallery.xaml` | `[ ]` | `[ ]` | `[ ]` |
| `VerticalSwipeThresholdGallery.xaml` | `Pages/Controls/SwipeViewGalleries/VerticalSwipeThresholdGallery.xaml` | `[ ]` | `[ ]` | `[ ]` |
| `WebViewFullScreenVideoGallery.xaml` | `Pages/Controls/WebViewGalleries/WebViewFullScreenVideoGallery.xaml` | `[ ]` | `[ ]` | `[ ]` |
| `WebViewGalleries.cs` | `Pages/Controls/WebViewGalleries/WebViewGalleries.cs` | `[ ]` | `[ ]` | `[ ]` |
| `WebViewGallery.xaml` | `Pages/Controls/WebViewGalleries/WebViewGallery.xaml` | `[ ]` | `[ ]` | `[ ]` |
| `AnimationPage.xaml` | `Pages/UserInterface/AnimationPage.xaml` | `[ ]` | `[ ]` | `[ ]` |
| `BehaviorsPage.xaml` | `Pages/UserInterface/BehaviorsPage.xaml` | `[ ]` | `[ ]` | `[ ]` |
| `FontsPage.xaml` | `Pages/UserInterface/FontsPage.xaml` | `[ ]` | `[ ]` | `[ ]` |
| `StateTriggersPage.xaml` | `Pages/UserInterface/StateTriggersPage.xaml` | `[ ]` | `[ ]` | `[ ]` |
| `StylesPage.xaml` | `Pages/UserInterface/StylesPage.xaml` | `[ ]` | `[ ]` | `[ ]` |
| `TriggersPage.xaml` | `Pages/UserInterface/TriggersPage.xaml` | `[ ]` | `[ ]` | `[ ]` |
| `VisualStatesPage.xaml` | `Pages/UserInterface/VisualStatesPage.xaml` | `[ ]` | `[ ]` | `[ ]` |
| `UserInterfacePage.xaml` | `Pages/UserInterfacePage.xaml` | `[ ]` | `[ ]` | `[ ]` |
| `BasicMapGallery.xaml` | `Pages/Controls/MapsGalleries/BasicMapGallery.xaml` | `[x]` | `[x]` | `[ ]` |
| `CircleGallery.xaml` | `Pages/Controls/MapsGalleries/CircleGallery.xaml` | `[x]` | `[x]` | `[ ]` |
| `MapPinsGallery.xaml` | `Pages/Controls/MapsGalleries/MapPinsGallery.xaml` | `[x]` | `[x]` | `[ ]` |
| `MapTypeGallery.xaml` | `Pages/Controls/MapsGalleries/MapTypeGallery.xaml` | `[x]` | `[x]` | `[ ]` |
| `MapsGallery.cs` | `Pages/Controls/MapsGalleries/MapsGallery.cs` | `[x]` | `[x]` | `[ ]` |
| `PinItemsSourceGallery.xaml` | `Pages/Controls/MapsGalleries/PinItemsSourceGallery.xaml` | `[x]` | `[x]` | `[ ]` |
| `PolygonsGallery.xaml` | `Pages/Controls/MapsGalleries/PolygonsGallery.xaml` | `[x]` | `[x]` | `[ ]` |
| `PlatformSpecificsPage.xaml` | `Pages/PlatformSpecificsPage.xaml` | `[ ]` | `[ ]` | `[ ]` |
| `BlazorPage.xaml` | `Pages/Blazor/BlazorPage.xaml` | `[R]` | `[ ]` | `[ ]` |
| `Index.razor` | `Pages/Blazor/Index.razor` | `[R]` | `[ ]` | `[ ]` |
| `MyDynamicComponent.razor` | `Pages/Blazor/MyDynamicComponent.razor` | `[R]` | `[ ]` | `[ ]` |
| `OtherPage.razor` | `Pages/Blazor/OtherPage.razor` | `[R]` | `[ ]` | `[ ]` |
| `GraphicsViewPage.xaml` | `Pages/Others/GraphicsViewPage.xaml` | `[ ]` | `[ ]` | `[ ]` |
| `LargeTitlesPageiOS.cs` | `Pages/Others/LargeTitlesPageiOS.cs` | `[ ]` | `[ ]` | `[ ]` |
| `RenderViewPage.xaml` | `Pages/Others/RenderViewPage.xaml` | `[ ]` | `[ ]` | `[ ]` |
| `StyleSheetsPage.xaml` | `Pages/Others/StyleSheetsPage.xaml` | `[ ]` | `[ ]` | `[ ]` |
| `TemplatePage.xaml` | `Pages/Others/TemplatePage.xaml` | `[ ]` | `[ ]` | `[ ]` |
| `TwoPaneViewPage.xaml` | `Pages/Others/TwoPaneViewPage.xaml` | `[ ]` | `[ ]` | `[ ]` |
