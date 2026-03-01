# M5 Accessibility Audit Checklist (Starter)

## Purpose
Start a repeatable accessibility audit checklist for currently implemented runtime controls, consistent with project constraints that require baseline semantics from day one.

## Scope
- Runtime renderer output in `samples/HelloWorld` (`/maui` routes).
- Control set currently in milestone scope:
  - `Label`
  - `Button`
  - `Entry`
  - `StackLayout`
  - `Grid`
  - `ContentPage`
  - `NavigationPage`
  - `CollectionView` prototype

## Audit Method
1. Automated checks:
   - Browser accessibility tree inspection.
   - Axe or equivalent page scan on `/maui` and representative nested routes.
2. Manual checks:
   - Keyboard-only navigation (Tab/Shift+Tab/Enter/Space).
   - Screen reader spot checks (VoiceOver/NVDA as available).
   - Disabled-state behavior and announcements.
   - Focus order and focus visibility.

## Checklist Matrix

### Label
- [ ] Exposes readable text content.
- [ ] `aria-label` set when semantic label is provided.
- [ ] No unintended interactive role.

### Button
- [ ] Native button semantics or equivalent role exposure.
- [ ] Accessible name from text/label.
- [ ] Disabled state maps to `disabled` and/or `aria-disabled`.
- [ ] Keyboard activation works (Enter/Space).

### Entry
- [ ] Exposes textbox role and editable semantics.
- [ ] Accessible name provided (label association or `aria-label`).
- [ ] Disabled/readonly states map correctly.
- [ ] Focus ring is visible.
- [ ] Typing and IME composition are operable.

### StackLayout
- [ ] Does not introduce conflicting landmark or widget roles.
- [ ] Child semantics are preserved through nested rendering.

### Grid
- [ ] Layout container semantics do not override child control semantics.
- [ ] Reading/focus order remains logical.

### ContentPage
- [ ] Page container has coherent semantic grouping.
- [ ] Child content remains discoverable by accessibility tree.

### NavigationPage
- [ ] Current page content is announced/discoverable after route change.
- [ ] Browser back/forward keeps focus and semantics coherent.

### CollectionView (Prototype)
- [ ] List/listitem semantics (or equivalent) are present and consistent.
- [ ] Item template interactive children keep semantics and disabled propagation.
- [ ] Truncation message (when cap is hit) is readable by assistive tech.

## Route Coverage
- [ ] `/maui`
- [ ] `/maui/details`
- [ ] `/maui/details/settings`
- [ ] `/maui/details/settings/profile`
- [ ] invalid route key example (NotFound rendering path)

## Logging Template
- Date:
- Reviewer:
- Browser + Version:
- OS:
- Assistive Tech:
- Findings:
- Follow-up Issues:

## Initial Gaps to Track
- [ ] Define minimum required semantics per control in architecture docs.
- [ ] Add automated accessibility checks to CI once baseline is stable.
- [ ] Add regression tests for disabled propagation and keyboard operation where practical.
