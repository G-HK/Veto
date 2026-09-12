# Veto component roadmap

Every component Veto might want, with its current state. This is a discussion
document - nothing here is committed to until we talk it through.

**Status key:** `done` shipped and tested · `stub` file exists, no real API ·
`planned` agreed, not started · `idea` worth discussing

Each `done` component satisfies the checklist in [Contributing.md](Contributing.md):
derives from a Veto base class, splats attributes, carries `data-testid`, scoped CSS
built only from `--veto-*` tokens, ARIA + keyboard, a demo page section, and
Playwright specs.

---

## 1. Foundation

| Component | Status | Notes |
|---|---|---|
| `IVetoComponent` / `VetoComponentBase` / `VetoInputBase<T>` | **done** | The shared contract. `VetoComponentCore` holds the one implementation both bases delegate to. |
| `CssBuilder` / `StyleBuilder` | **done** | Class and style composition. |
| `VetoSize` / `VetoColor` / `VetoVariant` | **done** | Modifier-class enums. |
| Design tokens (`VetoStyles.css`) | **done** | Light + dark palettes, spacing, radius, type, motion. |
| `Glow` / `Ping` effects | **done** | Parameters on the shared contract, so every component has them: a breathing neon glow and a ring that expands out of the border and fades. Off by default; colour follows the component's own role, then the theme, and can be set per instance. Drawn on a pseudo-element so they never argue with a component's own box-shadow. Both hold still, rather than disappearing, under reduced motion. |

## 2. Theming

| Component | Status | Notes |
|---|---|---|
| `VetoThemeProvider` | **done** | Wraps the app, cascades the active theme, writes `data-veto-theme` on `<html>`. |
| `IVetoThemeService` | **done** | Light / Dark / System, persisted to `localStorage`, reacts to `prefers-color-scheme` changes. |
| `VetoTheme` (palette model) | **done** | C# palette emitted as inline `--veto-*` properties, so apps rebrand without editing library CSS. |
| `VetoThemeToggle` | **done** | Switch, or a Light/System/Dark segmented control. Supersedes the `DarkModeSwitch` stub, which is still present and still a self-contained CSS demo with no API. |
| Anti-flash boot script | **done** | Applies the stored theme before first paint; without it every prerendered page flashes light. |

## 3. Layout

Three tiers. Primitives are the vocabulary; the shell pieces compose into an app
frame; the presets are ready-made page frames built from the other two.

### 3.1 Primitives

The vocabulary every other component and layout is written in. These live in
`Veto/Components/Layout/` - they are components, not page layouts; `Veto/Layouts/` is
reserved for page frames.

| Component | Status | Notes |
|---|---|---|
| `VetoStack` | **done** | Flex row/column, `Gap`, `Align`, `Justify`, `Wrap`. The workhorse - replaces most ad-hoc flex divs. Every library has this (MudStack, RadzenStack, Ant Space, MUI Stack). |
| `VetoGrid` / `VetoGridItem` | **done** | CSS grid, responsive `Columns`, item `Span`. The 12-column row/col system every library ships. |
| `VetoContainer` | **done** | Centered max-width page wrapper, size variants. |
| `VetoCard` | **done** | Padded elevated surface. Two APIs that coexist: the original header/body/footer slots, and composed sections - `VetoCardHeader`, `VetoCardTitle`, `VetoCardDescription`, `VetoCardContent`, `VetoCardFooter`. When sections are used the automatic body wrapper collapses to `display: contents`, which is what lets the sections own their padding and an `<img>` reach the card's edges; the card clips to its own corners so a media image needs no rounding of its own. A KPI tile is a header with the description written before the title. 14 Playwright specs. |
| `VetoDivider` | **done** | Horizontal / vertical rule, optional inline label. |
| `VetoSpacer` | **done** | Flexible gap that pushes siblings apart. |
| `VetoCenter` | idea | Centers a single child on both axes. |
| `VetoAspectRatio` | idea | Holds a ratio for media. |
| `VetoScrollArea` | idea | Fixed-height scroll region, styled scrollbars. |
| `VetoSplitter` | idea | Two panes, draggable divider, persisted size. Real JS interop work. |
| `VetoTilt3D` | **done** | Rewritten from the unusable `3dDiv` stub: pointer-tracking 3D tilt with optional glare. Pointer tracked in JS so nothing crosses into .NET per frame; dropped entirely under reduced motion and for coarse pointers. |

### 3.2 App shell pieces

Composable frame, not a monolith - the same decomposition MudBlazor, Radzen and Ant
all settled on, because a single `AppShell` component always needs escape hatches.

| Component | Status | Notes |
|---|---|---|
| `VetoLayout` | **done** | The root frame. Owns the CSS grid that positions header, sidebar, body and footer, and the responsive breakpoint at which the sidebar collapses. |
| `VetoAppBar` | **done** | Top bar. `Height`, `Dense`, `Bordered`, `Elevated`, `Translucent`. |
| `VetoDrawer` | **done** | Side navigation. `Position` Left/Right, collapsible to an icon rail, becomes an overlay drawer below the breakpoint, width persisted. |
| `VetoMainContent` | **done** | Main content region; handles its own scrolling so the frame stays put. |
| `VetoFooter` | **done** | Bottom bar, sticky or static. |


### 3.3 Page-shell presets

Assembled from the above. Opinionated on purpose - an app adopts one and fills slots.

| Component | Status | Notes |
|---|---|---|
| `VetoDockLayout` | **done** | **Your Apple/Windows frame.** Content fills the viewport, `VetoDock` floats over it. Wallpaper slot, optional window chrome. The layout that makes Veto look unlike everything else. |
| `VetoDashboardLayout` | **done** | App shell plus a responsive widget grid. The most-requested shape in any admin library. |
| `VetoDocsLayout` | **done** | Sidebar nav + content + right-hand table of contents. What the Veto demo site itself needs. |
| `VetoCenteredLayout` | planned | One centered card on an empty background - sign-in, 404, onboarding. |
| `VetoBlankLayout` | idea | Header only, no sidebar. Marketing and landing pages. |

### 3.4 Responsive helpers

| Component | Status | Notes |
|---|---|---|
| `VetoBreakpointProvider` | planned | Publishes the current breakpoint; the sidebar, dock and grid all read it. Needed before any layout can be genuinely responsive. |
| `VetoHidden` | idea | Renders children only above/below a breakpoint. |

## 4. Navigation

| Component | Status | Notes |
|---|---|---|
| `VetoDock` | **done** | **The dock / taskbar navbar.** Floats over content; `Position` = Left / Right / Top / Bottom; macOS-dock hover magnification or flat Windows-taskbar styling; auto-hide with reveal on edge hover; pinned and running items; tooltips on hover. Pairs with `VetoDockLayout`. |
| `VetoNavMenu` | **done** | Vertical nav with sections, icons, badges and active-route matching. What `VetoSidebar` is usually filled with. Takes the underline reveal as an option, applied to the label but answering the whole row. |
| `VetoLink` | **done** | A bare text link that draws a thin neon underline out from its middle on hover and on keyboard focus alike. `Underline` = None / Reveal / Always, own colour, `Inherit` for links inside prose, and active-route matching so it doubles as a nav link. |
| `VetoAppBarMenu` | idea | Horizontal top-level menu - the classic desktop File/Edit/View bar. |
| `VetoBreadcrumbs` / `VetoBreadcrumb` | **done** | Steps from an `Items` list or as self-registering children, so a step can carry markup of its own. A step is a link only when it has an `Href` - without one it is text, or a button when `OnItemClick` is wired - which is what lets the same component draw a file path or a wizard rather than only navigation. Separator as a character, an icon or a fragment; long trails collapse their middle to an expander. 11 Playwright specs. |
| `VetoTabs` | idea | Tab list + panels, arrow-key navigation, lazy panels. |
| `VetoMenu` | idea | Dropdown / context menu, submenus, roving focus. Shares positioning with `VetoToolTip`. |
| `VetoPagination` | idea | Page numbers, prev/next; pairs with `VetoTable`. |
| `VetoCommandPalette` | idea | Ctrl+K fuzzy command search. |

## 5. Form inputs

All derive from `VetoInputBase<T>`, so they get `@bind-Value`, `EditContext` and
validation for free.

| Component | Status | Notes |
|---|---|---|
| `VetoTextBox<T>` | **done** | Reference implementation. 16 Playwright specs. |
| `VetoButton` | **done** | Variants, colour roles, sizes, start/end icons, loading, full width, rounded. Renders an anchor when given `Href`; a disabled button never does, because a disabled anchor still navigates. |
| `VetoTextArea` | planned | Auto-grow, character counter. |
| `VetoSwitch<T>` | **done** | Bound to `bool` for on/off, or to `bool?` with `ThreeState` for off/on/indeterminate. Two-state is an ARIA `switch`; three-state is an ARIA `checkbox`, the only role whose `aria-checked` may be `mixed`. Cycle order, colour, size, variant, label placement, state text and thumb icons are all parameters. 15 Playwright specs. |
| `VetoCheckBox` | planned | The same states in a square skin; should share `VetoSwitch`'s state machine rather than restate it. |
| `VetoRadioGroup` | planned | Generic over the value type. |
| `VetoSelect<T>` | planned | Single select, searchable, keyboard navigation. |
| `VetoMultiSelect<T>` | idea | Chips, select-all. |
| `VetoDatePicker` / `VetoTimePicker` | planned | `Components/DateTime/` is an empty folder waiting for these. |
| `VetoNumberInput` | idea | Steppers, min/max/step, clamping. |
| `VetoSlider<T>` | **done** | Generic over the number type. `Min`/`Max`/`Step`, marks - generated or supplied, labelled with a number or a word - `SnapToMarks`, and `Range` for a second thumb bound through `@bind-UpperValue`. Built on native range inputs, so keyboard, touch and pointer capture come from the browser; only the rail, the fill and the marks are drawn. 16 Playwright specs. |
| `VetoRating<T>` | **done** | A score out of `Max` icons, six by default, from any icon. `AllowHalf` clips a filled copy over an empty one, so halves work for every icon rather than the few with a purpose-drawn half glyph. One tab stop reporting as an ARIA slider, named scores through `ItemLabels`. 17 Playwright specs. |
| `VetoFileUpload` | idea | Drag-and-drop, previews, progress. |
| `VetoColorPicker` | idea | Would pair well with the theming work. |
| `VetoMagicTextBox` | stub | From `docs/Concept.md`: *"text splitter splits the text into separate portions in input field"* - a tag / token input. Needs its behaviour pinned down. |
| `VetoCron` | **done** | Visual cron-expression builder. Seven shapes (every-n minutes through yearly, plus raw), the expression always visible and editable underneath, read back as a sentence, with the next runs worked out. Parser, describer and scheduler are plain classes usable without the component (`VetoCronExpression`, `VetoCronDescriber`, `VetoCronSchedule`), covered by 44 unit tests. Warns about cron's "either day" rule, which is almost never what the author meant. **The describer is built in and English only on purpose** - `Describer` is the seam for `CronExpressionDescriptor` and its ~25 languages, so the library keeps its zero dependencies and the docs site carries the package instead. `Bordered="false"` drops every border - the builder's box and each control's - keeping the focus ring. 23 Playwright specs, plus 44 unit tests. |

## 6. Data display

| Component | Status | Notes |
|---|---|---|
| `VetoTable<TItem>` | stub | Has `Items` plus header/row/footer templates today. Needs sorting, paging, selection, virtualization, empty state. |
| `VetoList<TItem>` | idea | Simpler than a table; rows with leading/trailing slots. |
| `VetoTree<TItem>` | idea | Expand/collapse, lazy children, keyboard navigation. |
| `VetoGantt` | **done** | Project/task tree, day/week/month zoom, text and category filters, today marker and navigation, milestones, per-row colours, CSV + JSON export with an `OnExport` hook, and dependency arrows between chained rows. |
| `VetoGantt` drag to reschedule | **done** | Drag a bar to move it and its edges to resize, or do both from the keyboard. `Editable` and `Resizable` are off by default. Each reader's edits are kept in their own browser through PGlite, mirrored to `localStorage` so a slow CDN cannot lose them, with a reset control and optional alphabetical ordering. |
| `VetoGauge` | **done** | A dial for one number: filled arc or swinging needle, `Bands` for a tachometer's red line, and `FollowBands` so the reading colours itself. Drawn as plain SVG - the arc is revealed with a dash offset rather than by swapping the path, because `d` cannot be transitioned. Ticks and their numbers are drawn *inside* the arc: outside put them at radius 106 in a viewBox whose radius is 100, so they were clipped. Needle length, width and colour, the hub, the tick-number size and the printed reading's size are all parameters - a long unit like `km/h` needs a smaller reading or it runs out over the scale. `Background` and `FaceContent` print a dial face - a gradient, an image, a maker's plate - under the ticks, so the needle sweeps over it. `FlickerFrom` makes the dial flicker like a neon tube past a threshold, held steady under reduced motion. `TransitionDuration` matters: the default ease is right for a polled dial and wrong for a driven one, where every new value restarts it and the needle trails its input. Live values come from `ValueProvider` (any function) or `PollUrl` + `PollJsonPath` (the app's `HttpClient`), neither polling until `PollInterval` is set. The docs site drives a pair of them from a seven-speed gearbox model. 40 Playwright specs. |
| `VetoChart` | idea | `Components/Charts/` is empty. Big surface - probably wrap an existing JS chart library rather than draw from scratch. |
| `VetoBadge` / `VetoChip` | planned | Small, high-use, cheap. |
| `VetoAvatar` | idea | Image, initials fallback, groups. |
| `VetoTerminalBox` | stub | Fully styled terminal UI with no API. Needs `Lines`, typing animation, copy button. |
| `VetoPara` | stub | `Components/Para/` is empty. A typography component - decide if it earns its place over plain `<p>` plus tokens. |
| `VetoKbd` + `VetoShortcut` / `VetoShortcutList` | **done** | Key caps for documentation, tooltips and menu hints. A combination is one string - `Keys="Ctrl+Shift+P"` - split into a `<kbd>` per key nested inside an outer one, which is what the HTML spec prescribes for a chord. `Symbols` swaps in ⌘/⇧/⌥ but stays off by default because a screen reader reads a glyph badly. The shortcut list is a real `<dl>`; descriptions left, keys aligned down the right, which is what makes a reference list scannable. `Bordered="false"` drops the cap and leaves the key as tinted text. 16 Playwright specs. |
| `VetoHandbookPage` / `VetoHandbookToc` | **done** | A documentation article and its contents rail, built from one section list so they cannot disagree. Anchor links are path-qualified, which is what makes them work under a `<base href>`. Optional landing highlight. |
| `VetoCode` | **done** | Monospaced, scrollable, copy button. Renders code as text so Razor snippets display rather than execute. |

## 7. Feedback and overlay

| Component | Status | Notes |
|---|---|---|
| `VetoToast` + `IVetoToastService` | **done** | **Notifications.** Inject the service anywhere and call it; one `VetoToastHost` in the layout renders everything. Six positions, eleven animations (a toast always leaves the way it came in), stacking with `MaxToasts`, `Sticky` toasts that never hide, and `Duplicates` Allow/Block/Replace - Replace folds a repeat into the toast on screen and counts it. The service owns the list, the host owns the clock: a caller's `Duration` may be null and only the host knows what null means. The countdown is a CSS animation, not a render per frame. 15 Playwright specs. |
| `VetoToolTip` | planned | `Components/ToolTip/` is empty, and `docs/Concept.md` opens with *"hover over an element to trigger something"* - this is that. Hover/focus triggered, collision-aware placement. |
| `VetoDialog` / `VetoModal` | planned | Focus trap, Escape to close, scroll lock, `async` result. |
| `VetoDrawer` | idea | Slide-in panel from any edge; shares mechanics with dialog and dock. |
| `VetoPopover` | idea | The positioning engine `VetoMenu`, `VetoToolTip` and `VetoSelect` would all share. Worth building once, deliberately. |
| `VetoAlert` | planned | Inline severity banner. |
| `VetoProgress` | **done** | Linear, determinate and indeterminate, with proper `progressbar` semantics. A circular variant is still outstanding. |
| `VetoSkeleton` | **done** | Text, rectangle and circle placeholders; multi-line blocks end short. Hidden from assistive technology. |
| `VetoBlinkText` | **done** | Teletype reveal with a blinking cursor, punctuation pauses, word mode, optional glyph scramble, and an `IAsyncEnumerable` stream input for API or model output. |
| `VetoSpinner` | **done** | Indeterminate wait; decorative unless given a label. |
| `VetoLoadingOverlay` | **done** | Veils content being refreshed and marks it inert. |
| `VetoConfirm` | idea | Convenience wrapper over dialog: `await Confirm.AskAsync(...)`. |

## 8. Utility

| Component | Status | Notes |
|---|---|---|
| `VetoIcon` + `VetoIcons` | **done** | SVG path constants, ~30 built in, arbitrary SVG accepted. No font, no dependency. |
| `VetoIconButton` | **done** | Icon-only button; renders an anchor when `Href` is set. |
| `VetoButtonGroup` | **done** | Related buttons as one segmented control: touching borders collapse and only the outer corners round. `Size` is set once on the group and normalises every button in it, so a group cannot come out uneven. `Separators` draws a line between every child; `VetoButtonGroupSeparator` places one between only some. `VetoButtonGroupText` embeds a muted, non-interactive label. `IsToolbar` reports the outermost container as an ARIA toolbar - the rich-text-editor pattern. 12 Playwright specs. |
| `VetoPortal` | idea | Renders children at the document root - the primitive under dialog, toast and popover. |
| `VetoClickOutside` / `VetoFocusTrap` | idea | Behaviour helpers, not visual components. |
| `VetoVirtualize` | idea | Blazor already ships `Virtualize`; only wrap it if we add something. |

---

## Open questions

1. ~~**Icons**~~ - settled: `VetoIcon` takes an SVG path string, with `VetoIcons` shipping ~30 constants and arbitrary SVG accepted. No font, no package dependency.
2. **Popover engine** - build one positioning primitive first, or let each overlay solve placement itself? Building it once is more work up front and much less later.
3. **Charts** - wrap a JS library, or leave charts out of scope entirely?
4. **The inherited stubs** - `TerminalBox` and `MagicTextBox` are interesting but unlike the rest of the set. Promote them to first-class components, or move them to a `Veto.Extras` package? (`3dDiv` became `VetoTilt3D`; `CronUI` became `VetoCron`; `DarkModeSwitch` was deleted for leaking global CSS.)
5. **`Para`** - does a typography component earn its place, or do tokens plus plain HTML cover it?
6. **PGlite's weight** - a full Postgres compiled to WebAssembly is several megabytes to store a handful of date overrides. It is opt-in and falls back to `localStorage`, but worth revisiting whether it earns its place for this particular job.

## Done

1. Theming engine - service, palette model, provider, toggle, anti-flash script
2. Foundation - the component contract, design tokens, `CssBuilder`/`StyleBuilder`
3. `VetoIcon` + `VetoIcons`, `VetoIconButton`, `VetoButton`, `VetoCode`
4. Layout components - `Stack`, `Grid`, `Container`, `Card`, `Divider`, `Spacer`
5. Shell - `VetoLayout`, `VetoAppBar`, `VetoDrawer` (all four variants), `VetoMainContent`, `VetoFooter`, `VetoNavMenu`
6. Layout presets - `VetoDocsLayout`, `VetoDockLayout`, `VetoDashboardLayout`, plus the gallery and live demos
7. `VetoDock` - both appearances, all four edges, auto-hide, keyboard traversal
8. `VetoTilt3D` - rewritten from the unusable `3dDiv`
9. `VetoHandbookPage` + `VetoHandbookToc`
10. `VetoGantt` - tree, zoom, filters, navigation, export, dependency arrows, drag to reschedule with per-user persistence
11. Loading - `VetoSpinner`, `VetoProgress`, `VetoSkeleton`, `VetoLoadingOverlay`
12. `VetoBlinkText` - teletype reveal with streaming input
13. `Glow` and `Ping` on every component, and `VetoLink` with the underline reveal

## In progress

Nothing. `VetoGantt` editing and persistence landed - see below.

## Recently done

**`VetoGantt` editing and persistence** - 11 specs in `VetoGanttEditingTests`, all green.

- [x] Drag a bar to move it, snapped to the current zoom unit
- [x] Drag its edges to resize
- [x] Keyboard equivalents, so it works without a mouse
- [x] `OnItemChanged`, so an app can store changes server-side instead
- [x] Per-user persistence in the browser through PGlite, falling back to `localStorage` when it cannot load
- [x] A reset control, so someone can clear what was cached about them
- [x] Optional alphabetical ordering, off by default so source order is respected

Two things worth remembering from building it:

- Dragging is one delegated pointer handler on the grid, not a handler per bar. Bars come and
  go as rows are filtered, collapsed and re-sorted, and per-bar handlers would have to be
  attached and detached in step with every render.
- PGlite is fetched over the network, so whether it is available varies from one visit to the
  next. `veto-gantt-store.js` therefore keeps a `localStorage` mirror alongside it and routes
  every read and write through one opening promise; without that, edits saved on a visit where
  PGlite loaded were read back on a visit where it did not, and looked lost.

## Next

2. Overlays: the popover engine, then `ToolTip`, `Menu`, `Dialog`
3. Remaining form inputs - `TextArea`, `CheckBox`, `RadioGroup`, `Select`, date and time pickers
4. `VetoTable` promoted to a real component - it is still the original sketch
5. Circular variant of `VetoProgress`
6. Retire the legacy fixture pages under `VetoWeb.Client/Pages/Components`, migrating their specs to the `/docs` routes
