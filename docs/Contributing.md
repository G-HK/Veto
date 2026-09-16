# Building a Veto component

Every component follows the same contract. `VetoTextBox` is the reference
implementation - read it alongside this page.

## The contract

Every component exposes these, whichever base class it uses:

| Parameter | Purpose |
|---|---|
| `Class` | extra CSS classes, appended *after* the component's own so the consumer always wins |
| `Style` | inline style, appended after the component's own |
| `Id` | root element id; auto-generated (`veto-textbox-3`) when not supplied, so `for` and `aria-*` never dangle |
| `TestId` | rendered as `data-testid` - the stable hook for Playwright |
| `Visible` | when false the component renders nothing |
| `AdditionalAttributes` | unmatched attributes, splatted onto the root element |

## Pick a base class

There are two, and which one you need is decided for you:

- **`VetoInputBase<TValue>`** - anything the user types or picks a value in. It inherits
  Blazor's `InputBase<TValue>`, which is what supplies `@bind-Value`, `ValueExpression`,
  the ambient `EditContext` and the `FieldIdentifier` used for validation. Never hand-roll
  two-way binding. It also adds `Label`, `HelperText`, `Placeholder`, `Disabled`,
  `ReadOnly`, `Required`, `Size` and `Variant`.
- **`VetoComponentBase`** - everything else: buttons, layout, overlays, data display.

They are separate roots because C# allows one base class and a form component must
inherit `InputBase<TValue>`. Both implement `IVetoComponent` and delegate to the same
`VetoComponentCore`, so the shared behaviour is written once - if you add something to
the contract, it goes in the core, not in both bases.

## Checklist

1. **Base class** - `VetoComponentBase` or `VetoInputBase<T>`; override `RootClass` with a
   lowercase hyphenated name such as `veto-dock`.
2. **Root element** - carries `@attributes="InnerAttributes"`, `data-testid="@TestId"`,
   `id="@ElementId"`, `class="@CssClass"` (or `RootCssClass` on inputs) and `style`.
3. **Modifier classes** - override `StartCssClass()` and add them with
   `Modifier("sm")` → `veto-dock--sm`. Never build class strings by concatenation; use
   `CssBuilder`.
4. **Scoped CSS** - a `<Name>.razor.css` next to the component, using **only**
   `var(--veto-*)` tokens. A hard-coded color is a bug: it will not follow the theme.
   Add a new token to `wwwroot/VetoStyles.css` if one is genuinely missing.
5. **Accessibility** - correct roles and `aria-*`, full keyboard operation, and a visible
   `:focus-visible` ring.
6. **Demo page** - a section per feature in `VetoWeb.Client/Pages/Components`, each with
   its own `TestId`. The page root must carry `data-testid="page"` and set
   `data-ready="true"` on first interactive render, which is what the tests wait on.
7. **Tests** - a spec class in `tests/Veto.Tests.E2E` deriving from `VetoPageTest`,
   covering rendering, interaction, disabled/read-only states, and the ARIA attributes.
8. **XML docs** - on the type and every `[Parameter]`. They are what consumers see in
   IntelliSense, and the build warns (CS1591) when they are missing.
9. **Roadmap** - move the component to `done` in [Roadmap.md](Roadmap.md).

## Running it

```powershell
dotnet build Veto.slnx
dotnet run --project VetoWeb/VetoWeb      # http://localhost:5038/components/textbox
dotnet test tests/Veto.Tests.E2E
```

See [`tests/Veto.Tests.E2E/README.md`](../../tests/Veto.Tests.E2E/README.md) for the
one-time Playwright browser install and the useful `dotnet test` switches.
