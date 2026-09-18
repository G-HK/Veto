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

Releases are started by hand and run from the `Production` branch:

1. Bump `<Version>` in `src/Directory.Build.props` and commit it to `main`.
2. Move `Production` to that commit: `git push origin main:Production`.
3. In GitHub Actions, run the **release** workflow on `Production`. Tick "Run the E2E tests"
   if the release should prove itself first.

The workflow packs every package at that version, verifies each contains its static web assets,
pushes them to nuget.org via Trusted Publishing, then tags the commit and creates the GitHub
release. It refuses to run if the version was already released.
