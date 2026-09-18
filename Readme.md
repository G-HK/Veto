# Veto

Veto is an open source component library for Blazor projects, with no third-party package
dependencies. Developed by G-HK.

Targets .NET 10, and works in any Blazor project - WebAssembly, Server, or a Blazor Web App.

## Install

Take the whole library:

```powershell
dotnet add package Veto
```

`Veto` is a meta package - it has no components of its own, it just depends on every component
package below. Take individual packages instead if you only want a few; each pulls in
`Veto.Core` and whatever else it genuinely uses, and nothing more:

```powershell
dotnet add package Veto.Buttons
dotnet add package Veto.Layout.Card
```

Then register the services and link the stylesheet:

```csharp
// Program.cs - from the meta package
builder.Services.AddVeto();

// or, with individual packages, the two that register anything:
builder.Services.AddVetoTheming();
builder.Services.AddVetoToast();
```

```html
<!-- App.razor or index.html, inside <head> -->
<script src="_content/Veto.Theming/veto-theme-init.js"></script>
<link rel="stylesheet" href="_content/Veto.Core/VetoStyles.css" />
```

## Packages

| Package | Contains |
| --- | --- |
| `Veto` | Meta package: depends on everything below |
| `Veto.Core` | Base classes, builders, shared enums, design tokens |
| `Veto.Theming` | Theme provider, theme service, theme toggle |
| `Veto.BlinkText` | `VetoBlinkText` |
| `Veto.Buttons` | `VetoButton`, `VetoIconButton`, `VetoButtonGroup`, `VetoButtonReveal` |
| `Veto.Code` | `VetoCode` |
| `Veto.Cron` | `VetoCron` and its parser, describer and scheduler |
| `Veto.Feedback` | `VetoSpinner`, `VetoProgress`, `VetoSkeleton`, `VetoLoadingOverlay` |
| `Veto.Gantt` | `VetoGantt` |
| `Veto.Gauge` | `VetoGauge` |
| `Veto.Handbook` | `VetoHandbookPage`, `VetoHandbookToc` |
| `Veto.Icon` | `VetoIcon` and the built-in icon set |
| `Veto.Kbd` | `VetoKbd`, `VetoShortcut`, `VetoShortcutList` |
| `Veto.MagicTextBox` | `VetoMagicTextBox` - collapses when the text outruns its space |
| `Veto.Menu` | `VetoMenu`, `VetoMenuItem`, `VetoMenuLabel`, `VetoMenuDivider` |
| `Veto.Navigation` | `VetoLink`, `VetoNavMenu`, `VetoBreadcrumbs` |
| `Veto.Rating` | `VetoRating` |
| `Veto.Slider` | `VetoSlider` |
| `Veto.Switch` | `VetoSwitch` |
| `Veto.Table` | `VetoTable` |
| `Veto.TerminalBox` | `VetoTerminalBox`, `VetoTerminalLine` |
| `Veto.TextBox` | `VetoTextBox` |
| `Veto.Tilt` | `VetoTilt3D` |
| `Veto.Toast` | `VetoToastHost`, `IVetoToastService` |
| `Veto.Layout.Card` | `VetoCard` and its composed sections |
| `Veto.Layout.Container` | `VetoContainer` |
| `Veto.Layout.Divider` | `VetoDivider` |
| `Veto.Layout.Grid` | `VetoGrid`, `VetoGridItem` |
| `Veto.Layout.Spacer` | `VetoSpacer` |
| `Veto.Layout.Stack` | `VetoStack` |
| `Veto.Layouts.Shell` | `VetoLayout`, `VetoAppBar`, `VetoDrawer`, `VetoMainContent`, `VetoFooter` |
| `Veto.Layouts.Dashboard` | `VetoDashboard`, `VetoDashboardTile` |
| `Veto.Layouts.DashboardLayout` | Ready-made dashboard page layout |
| `Veto.Layouts.Dock` | `VetoDock`, `VetoDockItem` |
| `Veto.Layouts.DockLayout` | Ready-made dock page layout |
| `Veto.Layouts.DocsLayout` | Ready-made documentation page layout |

Every package ships at the same version, and the meta package pins its dependencies to that
exact version, so a mixed set is not something you can end up with by accident.

## Repository layout

```
src/
  Veto/                     meta package
  Veto.Core/
  Veto.Theming/
  Components/
    Veto.Buttons/  Veto.Icon/  Veto.Gantt/  ...
    Layout/
      Veto.Layout.Card/  Veto.Layout.Grid/  ...
  Layouts/
    Veto.Layouts.Shell/  Veto.Layouts.DocsLayout/  ...
VetoWeb/                    the docs and demo site
tests/Veto.Tests.E2E/       Playwright specs
docs/                       concept, contributing, roadmap
```

One project per package - that is the only way NuGet packs, so the folder name and the package
id are deliberately kept identical.

## Releasing

Every package carries its own `<Version>` in its `.csproj` and is released on its own:

1. Bump `<Version>` in the `.csproj` of each package that changed, and commit it to `main`.
2. Move `Production` to that commit: `git push origin main:Production`.
3. In GitHub Actions, run the **release** workflow on `Production` and type the packages to
   release - `Veto.Gantt`, several separated by commas, or `all`. Tick "Run the E2E tests" if
   the release should prove itself first.

Only the packages named are built and packed. The workflow stops before building if a name is
wrong or a version is already on nuget.org, and before publishing if a package depends on a Veto
package version that is neither on nuget.org nor in the same release. It then pushes to
nuget.org via Trusted Publishing and tags each package, e.g. `Veto.Gantt-v1.0.2`.

A package's dependencies on other Veto packages are "this version or newer", so one package can
move ahead of the rest. Release `Veto` as well when everyone installing the whole library
should get the new version by default.
