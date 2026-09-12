using Veto.Tests.E2E.Infrastructure;
using Xunit;
using Xunit.Abstractions;

namespace Veto.Tests.E2E;

/// <summary>Temporary probe: did the drag handlers and store actually initialise?</summary>
public class ScratchGanttProbe ( BlazorAppFixture app, ITestOutputHelper output ) : VetoPageTest(app)
{
    [Fact]
    public async Task Probe ()
    {
        Page.PageError += (_, e) => output.WriteLine($"pageerror: {e}");
        Page.Console += (_, m) => { if (m.Type == "error") output.WriteLine($"console: {m.Text}"); };

        await Page.SetViewportSizeAsync(1500, 1200);
        await GotoComponentAsync("docs/components/gantt");
        await Page.WaitForTimeoutAsync(1500);

        output.WriteLine("store=" + await Component("editable").GetAttributeAsync("data-store"));
        output.WriteLine("draggable=" + await Component("editable").GetAttributeAsync("data-draggable"));
        output.WriteLine("diagnostic=" + await Component("editable").GetAttributeAsync("data-diagnostic"));

        var bar = Component("editable-bar-wiring");
        var box = await bar.BoundingBoxAsync()!;
        await Page.Mouse.MoveAsync((float) (box!.X + box.Width / 2), (float) (box.Y + box.Height / 2));
        await Page.Mouse.DownAsync();
        output.WriteLine("dragging=" + await bar.GetAttributeAsync("data-dragging"));
        await Page.Mouse.MoveAsync((float) (box.X + box.Width / 2 + 120), (float) (box.Y + box.Height / 2), new() { Steps = 6 });
        await Page.Mouse.UpAsync();
        await Page.WaitForTimeoutAsync(400);
        output.WriteLine("note=" + await Page.GetByTestId("change-note").TextContentAsync());
    }
}
