using System.Globalization;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Veto.Core;

namespace Veto.Components.Gauge;

/// <summary>
/// A dial that reads one number against a range - a speedometer, a tachometer, a CPU meter.
/// </summary>
/// <remarks>
/// <para>
/// Drawn as an SVG arc with no charting library behind it. <see cref="Needle"/> switches
/// between the two shapes the pattern comes in: a filled arc, which is what a disk-usage or
/// CPU meter wants, and a swinging needle, which is what the dashboard of a car looks like.
/// </para>
/// <para>
/// Bands colour stretches of the dial - the red line on a tachometer. With
/// <see cref="FollowBands"/> the reading itself takes the colour of whichever band it currently
/// falls in, so the gauge turns red on its own rather than being told to.
/// </para>
/// <para>
/// <b>Live values.</b> Two ways in, and they layer. <see cref="ValueProvider"/> is a function
/// the gauge calls on a timer - use it when the value comes from a hub, a SignalR stream, a
/// local counter, or an endpoint that needs your own headers. <see cref="PollUrl"/> is the
/// shortcut for the common case: the gauge GETs that URL with the app's
/// <see cref="HttpClient"/> and reads a number out of the response, optionally at
/// <see cref="PollJsonPath"/>. Neither polls at all until <see cref="PollInterval"/> is
/// positive.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// &lt;VetoGauge Value="cpu" Label="CPU" Unit="%" /&gt;
///
/// &lt;VetoGauge Needle="true" Min="0" Max="8000" Label="RPM"
///            Bands="[new(6500, 8000, &quot;var(--veto-color-danger)&quot;, &quot;Red line&quot;)]"
///            FollowBands="true" /&gt;
///
/// &lt;VetoGauge PollUrl="/api/metrics" PollJsonPath="cpu.percent" PollInterval="2000" /&gt;
/// </code>
/// </example>
public partial class VetoGauge : VetoComponentBase, IAsyncDisposable
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    private CancellationTokenSource? _poll;

    private double _polled;

    private bool _hasPolled;

    /// <summary>The service provider, used to find an <see cref="HttpClient"/> only if <see cref="PollUrl"/> is set.</summary>
    [Inject]
    private IServiceProvider Services { get; set; } = default!;

    /// <inheritdoc />
    protected override string RootClass => "veto-gauge";

    /// <summary>The number the dial is showing.</summary>
    [Parameter]
    public double Value { get; set; }

    /// <summary>The low end of the scale.</summary>
    [Parameter]
    public double Min { get; set; }

    /// <summary>The high end of the scale.</summary>
    [Parameter]
    public double Max { get; set; } = 100;

    /// <summary>
    /// Where the dial starts, in degrees clockwise from the top. The default pair leaves the
    /// bottom of the dial open, the way a car's instruments do.
    /// </summary>
    [Parameter]
    public double StartAngle { get; set; } = -135;

    /// <summary>Where the dial ends, in degrees clockwise from the top.</summary>
    [Parameter]
    public double EndAngle { get; set; } = 135;

    /// <summary>How thick the dial is drawn, in the SVG's own 200-unit box.</summary>
    [Parameter]
    public double Thickness { get; set; } = 14;

    /// <summary>When true the reading is a swinging needle rather than a filled arc.</summary>
    [Parameter]
    public bool Needle { get; set; }

    /// <summary>When true the arc's ends are rounded. Ignored for a needle.</summary>
    [Parameter]
    public bool RoundCaps { get; set; } = true;

    /// <summary>Coloured stretches of the dial, such as a tachometer's red line.</summary>
    [Parameter]
    public IReadOnlyList<VetoGaugeBand>? Bands { get; set; }

    /// <summary>
    /// When true the reading takes the colour of the band it currently falls in, so the gauge
    /// turns red by itself. Falls back to <see cref="Color"/> outside every band.
    /// </summary>
    [Parameter]
    public bool FollowBands { get; set; }

    /// <summary>
    /// The reading at or above which the dial flickers like a neon tube - the needle, the hub
    /// and the printed figure all pulsing on an irregular beat. Null never flickers.
    /// </summary>
    /// <remarks>
    /// For the top of a scale that means something: a tachometer on its limiter, a temperature
    /// past the line. It is deliberately loud, so it is worth reserving for the reading you
    /// actually want someone to look up at. Held steady under <c>prefers-reduced-motion</c>,
    /// where a flashing instrument is at best unpleasant.
    /// </remarks>
    [Parameter]
    public double? FlickerFrom { get; set; }

    /// <summary>The colour of the reading when no band applies.</summary>
    [Parameter]
    public VetoColor Color { get; set; } = VetoColor.Primary;

    /// <summary>The gauge's size.</summary>
    [Parameter]
    public VetoSize Size { get; set; } = VetoSize.Medium;

    /// <summary>The name under the reading, and the dial's accessible name.</summary>
    [Parameter]
    public string? Label { get; set; }

    /// <summary>
    /// The dial's face, as any CSS <c>background</c> value - a URL, a data URI, a gradient. It
    /// is clipped to the dial's circle and sits under everything the gauge draws, so the ticks
    /// and the needle sweep over it the way they do on a real instrument.
    /// </summary>
    [Parameter]
    public string? Background { get; set; }

    /// <summary>
    /// Content placed in the middle of the dial, under the needle - a maker's badge, a model
    /// name, a warning symbol. Purely decorative as far as assistive technology is concerned:
    /// it sits in an <c>aria-hidden</c> layer, because the reading is what a gauge is for.
    /// </summary>
    [Parameter]
    public RenderFragment? FaceContent { get; set; }

    /// <summary>What the number is measured in - <c>%</c>, <c>rpm</c>, <c>km/h</c>. Printed straight after it.</summary>
    [Parameter]
    public string? Unit { get; set; }

    /// <summary>Numeric format for every number the gauge prints, e.g. <c>N0</c> or <c>0.0</c>.</summary>
    [Parameter]
    public string? Format { get; set; }

    /// <summary>Whether the reading is printed in the middle of the dial.</summary>
    [Parameter]
    public bool ShowValue { get; set; } = true;

    /// <summary>Whether tick marks are drawn around the dial.</summary>
    [Parameter]
    public bool ShowTicks { get; set; }

    /// <summary>The spacing of minor ticks, on the gauge's own scale.</summary>
    [Parameter]
    public double TickEvery { get; set; } = 10;

    /// <summary>How many minor ticks there are between major ones.</summary>
    [Parameter]
    public int MajorTickEvery { get; set; } = 5;

    /// <summary>Whether major ticks carry their number.</summary>
    [Parameter]
    public bool ShowTickLabels { get; set; }

    /// <summary>The size of a tick's number, in the SVG's own 200-unit box.</summary>
    [Parameter]
    public double TickLabelSize { get; set; } = 11;

    /// <summary>
    /// How far a tick's number sits in from the dial, in the SVG's own 200-unit box. Raise it
    /// on a crowded scale to move the numbers clear of the ticks.
    /// </summary>
    [Parameter]
    public double TickLabelOffset { get; set; } = 15;

    /// <summary>
    /// How long the needle is, in the SVG's own 200-unit box. Null works one out that stops
    /// short of the numbers.
    /// </summary>
    [Parameter]
    public double? NeedleLength { get; set; }

    /// <summary>How thick the needle is drawn.</summary>
    [Parameter]
    public double NeedleWidth { get; set; } = 3;

    /// <summary>
    /// The needle's colour. Null follows the reading, so a needle in a red band turns red with
    /// everything else.
    /// </summary>
    [Parameter]
    public string? NeedleColor { get; set; }

    /// <summary>The radius of the hub the needle turns on. Zero draws no hub at all.</summary>
    [Parameter]
    public double HubRadius { get; set; } = 7;

    /// <summary>The hub's colour. Null follows the reading, like the needle.</summary>
    [Parameter]
    public string? HubColor { get; set; }

    /// <summary>
    /// The size of the printed reading, as any CSS length. Null uses the size modifier's own.
    /// Worth setting when a long unit - <c>km/h</c> - makes the default too wide for the dial.
    /// </summary>
    [Parameter]
    public string? ValueFontSize { get; set; }

    /// <summary>The size of the name under the reading, as any CSS length.</summary>
    [Parameter]
    public string? LabelFontSize { get; set; }

    /// <summary>
    /// How long the needle or arc takes to reach a new reading, in milliseconds. Null uses the
    /// theme's own motion.
    /// <para>
    /// The default is right for a gauge that updates now and then - a CPU meter should glide
    /// rather than snap. It is wrong for one driven continuously: every new value restarts the
    /// ease from wherever the needle currently is, so a dragged or animated dial trails its
    /// input by the whole duration and only settles when the input stops. Set it to roughly the
    /// interval between updates - or to zero - and the needle tracks exactly.
    /// </para>
    /// </summary>
    [Parameter]
    public int? TransitionDuration { get; set; }

    /// <summary>
    /// A function the gauge calls every <see cref="PollInterval"/> for the current value. The
    /// general way in: call a hub, a counter, or an endpoint needing your own headers.
    /// </summary>
    [Parameter]
    public Func<Task<double>>? ValueProvider { get; set; }

    /// <summary>
    /// A URL the gauge GETs every <see cref="PollInterval"/>. The shortcut for the common case;
    /// needs an <see cref="HttpClient"/> registered in the app. Ignored when
    /// <see cref="ValueProvider"/> is given, which is the more capable of the two.
    /// </summary>
    [Parameter]
    public string? PollUrl { get; set; }

    /// <summary>
    /// Dotted path to the number inside the polled JSON, e.g. <c>cpu.percent</c>. Leave it null
    /// when the response is a bare number.
    /// </summary>
    [Parameter]
    public string? PollJsonPath { get; set; }

    /// <summary>
    /// How often to poll, in milliseconds. Zero - the default - means the gauge never polls and
    /// simply shows <see cref="Value"/>.
    /// </summary>
    [Parameter]
    public int PollInterval { get; set; }

    /// <summary>Raised after a poll changes the reading.</summary>
    [Parameter]
    public EventCallback<double> OnValueChanged { get; set; }

    /// <summary>Raised when a poll fails, with the exception. The gauge keeps polling.</summary>
    [Parameter]
    public EventCallback<Exception> OnPollError { get; set; }

    /// <summary>The most recent poll failure, shown under the dial. Null once a poll succeeds.</summary>
    public string? PollError { get; private set; }

    /// <summary>True while the gauge is polling for its own value.</summary>
    private bool IsPolling => PollInterval > 0 && (ValueProvider is not null || !string.IsNullOrWhiteSpace(PollUrl));

    /// <summary>What the dial is actually showing: the polled value once one has arrived, otherwise <see cref="Value"/>.</summary>
    private double Reading => Math.Clamp(_hasPolled ? _polled : Value, Min, Max);

    private double Span => Max - Min is var s && s > 0 ? s : 1;

    private double Sweep => EndAngle - StartAngle;

    /// <summary>
    /// The radius the dial is drawn at. The ticks and their numbers sit <em>inside</em> the
    /// arc, which is both what a car's instrument looks like and the only way they are
    /// guaranteed to stay inside the viewBox - drawing them outside put the numbers at radius
    /// 106 in a box whose radius is 100, so they were clipped and collided with the dial.
    /// </summary>
    private double Radius => 100 - Thickness / 2 - 2;

    /// <summary>The radius the tick marks start at, just inside the arc.</summary>
    private double TickStart => Radius - Thickness / 2 - 2;

    private string Cap => RoundCaps ? "round" : "butt";

    private IReadOnlyList<VetoGaugeBand> BandList => Bands ?? [];

    private VetoGaugeBand? CurrentBand
        => BandList.FirstOrDefault(b => Reading >= Math.Min(b.From, b.To) && Reading <= Math.Max(b.From, b.To));

    private string ReadingColor
        => FollowBands && CurrentBand is { } band ? band.Color : $"var(--veto-gauge-color)";

    /// <summary>True when the reading has climbed into the range that flickers.</summary>
    private bool IsFlickering => FlickerFrom is { } from && Reading >= from;

    /// <summary>The face's own inline style, when it was given a background.</summary>
    private string? FaceStyle => Background is null ? null : $"background:{Background}";

    /// <summary>True when there is anything to draw on the dial's face at all.</summary>
    private bool HasFace => Background is not null || FaceContent is not null;

    private string ValueText => $"{FormatNumber(Reading)}{Unit}{(CurrentBand?.Label is { } l ? $", {l}" : string.Empty)}";

    private double NeedleAngle => AngleFor(Reading);

    /// <summary>
    /// How far the needle reaches. Stops short of the numbers when there are any, so the
    /// pointer never crosses the scale it is pointing at.
    /// </summary>
    private double NeedleReach => NeedleLength
        ?? (ShowTicks && ShowTickLabels ? TickStart - TickLabelOffset - TickLabelSize / 2 : TickStart - 4);

    /// <summary>How far along the dial the reading sits, from 0 at Min to 1 at Max.</summary>
    private double Fraction => (Reading - Min) / Span;

    /// <summary>The drawn length of the whole dial, which is what the dash offset is measured in.</summary>
    private double ArcLength => 2 * Math.PI * Radius * (Math.Abs(Sweep) / 360);

    /// <inheritdoc />
    protected override void OnParametersSet ()
    {
        base.OnParametersSet();
        _ticks = ShowTicks ? [.. BuildTicks()] : [];
        RestartPolling();
    }

    /// <inheritdoc />
    protected override StyleBuilder StartStyle ()
        => base.StartStyle()
            .AddStyle("--veto-gauge-value-size", ValueFontSize, ValueFontSize is not null)
            .AddStyle("--veto-gauge-label-size", LabelFontSize, LabelFontSize is not null)
            // Linear rather than eased: a continuously driven dial is interpolating between
            // frames, and an ease-out on every frame reads as floating rather than as motion.
            .AddStyle("--veto-gauge-transition", $"{TransitionDuration}ms linear", TransitionDuration is not null);

    /// <inheritdoc />
    protected override CssBuilder StartCssClass ()
        => base.StartCssClass()
            .AddClass(Modifier(Size.ToModifier()))
            .AddClass(Modifier(Color.ToModifier()))
            .AddClass(Modifier("needle"), Needle)
            .AddClass(Modifier("polling"), IsPolling)
            .AddClass(Modifier("flicker"), IsFlickering);

    /// <summary>Stops polling.</summary>
    public ValueTask DisposeAsync ()
    {
        _poll?.Cancel();
        _poll?.Dispose();
        _poll = null;
        return ValueTask.CompletedTask;
    }

    /// <summary>Fetches the value once, right now, whether or not the gauge is polling.</summary>
    public async Task RefreshAsync ()
    {
        try
        {
            var next = ValueProvider is not null ? await ValueProvider() : await FetchFromUrlAsync();

            PollError = null;

            if (!_hasPolled || Math.Abs(next - _polled) > double.Epsilon)
            {
                _polled = next;
                _hasPolled = true;

                if (OnValueChanged.HasDelegate)
                {
                    await OnValueChanged.InvokeAsync(next);
                }
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // A gauge that blanks itself the moment a network blip happens is worse than one
            // holding a slightly stale figure, so the last good reading stays on the dial.
            PollError = ex.Message;

            if (OnPollError.HasDelegate)
            {
                await OnPollError.InvokeAsync(ex);
            }
        }

        StateHasChanged();
    }

    private void RestartPolling ()
    {
        _poll?.Cancel();
        _poll?.Dispose();
        _poll = null;

        if (!IsPolling)
        {
            return;
        }

        var cts = new CancellationTokenSource();
        _poll = cts;
        _ = PollLoopAsync(cts.Token);
    }

    private async Task PollLoopAsync ( CancellationToken token )
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                await RefreshAsync();
                await Task.Delay(PollInterval, token);
            }
        }
        catch (OperationCanceledException)
        {
            // The gauge was disposed or its parameters changed.
        }
    }

    private async Task<double> FetchFromUrlAsync ()
    {
        if (Services.GetService(typeof(HttpClient)) is not HttpClient http)
        {
            throw new InvalidOperationException(
                $"{nameof(PollUrl)} needs an HttpClient registered in the app. Register one, or use {nameof(ValueProvider)} instead.");
        }

        using var document = await http.GetFromJsonAsync<JsonDocument>(PollUrl!)
            ?? throw new InvalidOperationException($"{PollUrl} returned no content.");

        var element = document.RootElement;

        if (!string.IsNullOrWhiteSpace(PollJsonPath))
        {
            foreach (var segment in PollJsonPath.Split('.', StringSplitOptions.RemoveEmptyEntries))
            {
                if (!element.TryGetProperty(segment, out element))
                {
                    throw new InvalidOperationException($"'{PollJsonPath}' is not present in the response from {PollUrl}.");
                }
            }
        }

        return element.ValueKind switch
        {
            JsonValueKind.Number => element.GetDouble(),
            JsonValueKind.String when double.TryParse(element.GetString(), NumberStyles.Float, Inv, out var parsed) => parsed,
            _ => throw new InvalidOperationException($"The value at '{PollJsonPath ?? "the response root"}' is not a number.")
        };
    }

    /// <summary>The angle, in degrees clockwise from the top, that a value sits at on the dial.</summary>
    private double AngleFor ( double value )
        => StartAngle + (Math.Clamp(value, Min, Max) - Min) / Span * Sweep;

    /// <summary>A point on the dial, in the SVG's 200-unit box centred on (100, 100).</summary>
    private static (double X, double Y) Point ( double angleDegrees, double radius )
    {
        var radians = (angleDegrees - 90) * Math.PI / 180;
        return (100 + radius * Math.Cos(radians), 100 + radius * Math.Sin(radians));
    }

    private string Arc ( double fromAngle, double toAngle, double radius )
    {
        var (x1, y1) = Point(fromAngle, radius);
        var (x2, y2) = Point(toAngle, radius);
        var large = Math.Abs(toAngle - fromAngle) > 180 ? 1 : 0;
        var sweepFlag = toAngle >= fromAngle ? 1 : 0;

        return $"M {Css(x1)} {Css(y1)} A {Css(radius)} {Css(radius)} 0 {large} {sweepFlag} {Css(x2)} {Css(y2)}";
    }

    private string TrackPath => Arc(StartAngle, EndAngle, Radius);

    private string BandPath ( VetoGaugeBand band )
        => Arc(AngleFor(Math.Min(band.From, band.To)), AngleFor(Math.Max(band.From, band.To)), Radius);

    private record Tick ( double Value, double X1, double Y1, double X2, double Y2, double LabelX, double LabelY, bool Major );

    private IReadOnlyList<Tick> _ticks = [];

    /// <summary>
    /// The tick marks, worked out once per parameter change rather than once per render. They
    /// only move when the scale does, and a dial being animated re-renders many times a second.
    /// </summary>
    private IReadOnlyList<Tick> Ticks => _ticks;

    private IEnumerable<Tick> BuildTicks ()
    {
        {
            if (TickEvery <= 0)
            {
                yield break;
            }

            var index = 0;

            for (var value = Min; value <= Max + TickEvery * 1e-9; value += TickEvery, index++)
            {
                var major = MajorTickEvery > 0 && index % MajorTickEvery == 0;
                var angle = AngleFor(value);
                var (x1, y1) = Point(angle, TickStart);
                var (x2, y2) = Point(angle, TickStart - (major ? 7 : 4));
                var (lx, ly) = Point(angle, TickStart - TickLabelOffset);

                yield return new Tick(Math.Min(value, Max), x1, y1, x2, y2, lx, ly, major);

                // A misconfigured Min/Max/TickEvery must not hang the render loop.
                if (index > 400)
                {
                    yield break;
                }
            }
        }
    }

    /// <summary>
    /// One tick's number as an SVG <c>text</c> element. Built as markup because Razor reserves
    /// the <c>&lt;text&gt;</c> tag name for its own block syntax and will not emit it verbatim.
    /// The only interpolated value is a number the component formatted itself.
    /// </summary>
    private MarkupString TickLabel ( Tick tick )
        => new($"<text class=\"veto-gauge__tick-label\" x=\"{Css(tick.LabelX)}\" y=\"{Css(tick.LabelY)}\" " +
               $"text-anchor=\"middle\" dominant-baseline=\"middle\" font-size=\"{Css(TickLabelSize)}\">" +
               $"{FormatNumber(tick.Value)}</text>");

    private string FormatNumber ( double value )
        => Format is null ? value.ToString("0.##", Inv) : value.ToString(Format, CultureInfo.CurrentCulture);

    /// <summary>
    /// A number for an SVG attribute or a CSS declaration. Always invariant: a culture that
    /// writes decimals with a comma would otherwise emit coordinates SVG silently drops.
    /// </summary>
    private static string Css ( double value ) => value.ToString("0.####", Inv);
}
