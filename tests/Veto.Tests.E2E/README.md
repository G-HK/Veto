# Veto end-to-end tests

Playwright specs that drive the `VetoWeb` demo app in a real browser.

## One-time setup

Build once, then install the browsers Playwright drives:

```powershell
dotnet build tests/Veto.Tests.E2E
pwsh tests/Veto.Tests.E2E/bin/Debug/net10.0/playwright.ps1 install
```

## Running

```powershell
dotnet test tests/Veto.Tests.E2E
```

`BlazorAppFixture` starts the demo app on <http://localhost:5177> for the duration of the
run and stops it afterwards. If something is already serving that URL, the fixture reuses
it instead of starting a second copy - handy while writing tests against a `dotnet run`
you keep open.

Useful switches:

```powershell
dotnet test tests/Veto.Tests.E2E -- Playwright.LaunchOptions.Headless=false   # watch it
dotnet test tests/Veto.Tests.E2E -- Playwright.LaunchOptions.SlowMo=500       # slow it down
dotnet test tests/Veto.Tests.E2E -- Playwright.BrowserName=firefox            # or webkit
```

## Adding specs for a new component

1. Add a demo page under `VetoWeb.Client/Pages/Components` whose root carries
   `data-testid="page"` and sets `data-ready="true"` on first interactive render.
2. Give every component instance on it a `TestId`.
3. Derive the spec class from `VetoPageTest` and use `Component(testId)` / `Input(testId)`.
