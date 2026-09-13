using Microsoft.AspNetCore.Components;
using System.Text;
using Veto.Core;

namespace Veto.Components.BlinkText;

/// <summary>
/// Reveals text a character at a time, like a terminal printing it, with a blinking cursor and
/// longer pauses at punctuation.
/// </summary>
/// <remarks>
/// <para>
/// Feed it a finished string through <see cref="Text"/>, or a stream through
/// <see cref="Stream"/> - chunks are appended as they arrive and drained at the reveal rate, so
/// text from a slow API reads at a steady pace instead of arriving in lurches.
/// </para>
/// <para>
/// The reveal is decoration. The full text is always present for assistive technology, which
/// reads it in one go; only the animated copy is hidden from it. Under
/// <c>prefers-reduced-motion</c> the text appears complete immediately.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// &lt;VetoBlinkText Text="Wake up, Neo..." CharactersPerSecond="18" Scramble="true" /&gt;
/// </code>
/// </example>
public partial class VetoBlinkText : VetoComponentBase, IDisposable
{
    private readonly StringBuilder _target = new();
    private readonly Random _random = new();

    private CancellationTokenSource? _run;
    private string _revealed = string.Empty;
    private string _scrambled = string.Empty;
    private string? _lastText;
    private object? _lastStream;

    /// <inheritdoc />
    protected override string RootClass => "veto-blink-text";

    /// <summary>The text to reveal. Changing it restarts the reveal.</summary>
    [Parameter]
    public string? Text { get; set; }

    /// <summary>
    /// A stream of chunks appended as they arrive - the output of a model, a log tail, a slow
    /// endpoint. Drained at the reveal rate rather than dumped on arrival.
    /// </summary>
    [Parameter]
    public IAsyncEnumerable<string>? Stream { get; set; }

    /// <summary>How fast the text appears, in characters per second.</summary>
    [Parameter]
    public double CharactersPerSecond { get; set; } = 45;

    /// <summary>Whether to reveal whole words rather than single characters.</summary>
    [Parameter]
    public bool ByWord { get; set; }

    /// <summary>
    /// Extra pauses, in milliseconds, after particular characters. Defaults to a short beat at
    /// commas and a longer one at the end of a sentence, which is what makes the reveal read
    /// like speech rather than a ticker.
    /// </summary>
    [Parameter]
    public IReadOnlyDictionary<char, int> PauseOn { get; set; } = DefaultPauses;

    /// <summary>How long to wait before starting, in milliseconds.</summary>
    [Parameter]
    public int StartDelay { get; set; }

    /// <summary>Whether to show a blinking cursor.</summary>
    [Parameter]
    public bool Cursor { get; set; } = true;

    /// <summary>Whether the cursor stays once the text is complete.</summary>
    [Parameter]
    public bool CursorWhenComplete { get; set; }

    /// <summary>The cursor's character.</summary>
    [Parameter]
    public string CursorCharacter { get; set; } = "▋";

    /// <summary>
    /// Flickers a few random glyphs ahead of the cursor before they settle - the effect the
    /// falling-code screens are known for.
    /// </summary>
    [Parameter]
    public bool Scramble { get; set; }

    /// <summary>How many glyphs flicker ahead of the cursor when <see cref="Scramble"/> is on.</summary>
    [Parameter]
    public int ScrambleLength { get; set; } = 3;

    /// <summary>The glyphs drawn while scrambling.</summary>
    [Parameter]
    public string ScrambleCharacters { get; set; } = "アイウエオカキクケコサシスセソタチツテトナニヌネノ01";

    /// <summary>Starts again from the beginning once complete.</summary>
    [Parameter]
    public bool Loop { get; set; }

    /// <summary>How long to hold the finished text before looping, in milliseconds.</summary>
    [Parameter]
    public int LoopDelay { get; set; } = 1600;

    /// <summary>
    /// Reserves room for the finished text so the surrounding layout does not shift as the
    /// reveal grows. Only meaningful with <see cref="Text"/>, where the length is known.
    /// </summary>
    [Parameter]
    public bool PreserveSpace { get; set; } = true;

    /// <summary>Raised once the whole text has been revealed.</summary>
    [Parameter]
    public EventCallback OnCompleted { get; set; }

    /// <summary>
    /// Shows the text immediately, skipping the reveal. Also what
    /// <c>prefers-reduced-motion</c> produces, through CSS.
    /// </summary>
    [Parameter]
    public bool Instant { get; set; }

    /// <summary>The default pauses: a beat at a comma, longer at the end of a sentence.</summary>
    public static readonly IReadOnlyDictionary<char, int> DefaultPauses = new Dictionary<char, int>
    {
        [','] = 180,
        [';'] = 200,
        [':'] = 200,
        ['—'] = 260,
        ['.'] = 420,
        ['!'] = 420,
        ['?'] = 420,
        ['\n'] = 500
    };

    /// <summary>Everything that will eventually be shown - the source for assistive technology.</summary>
    private string FullText => _target.ToString();

    private string Revealed => Instant ? FullText : _revealed;

    private bool IsComplete => _revealed.Length >= _target.Length;

    private string? Sub ( string suffix ) => TestId is null ? null : $"{TestId}-{suffix}";

    private int DelayPerStep => CharactersPerSecond <= 0 ? 0 : (int)Math.Max(1, 1000 / CharactersPerSecond);

    /// <inheritdoc />
    protected override CssBuilder StartCssClass () =>
        base.StartCssClass()
            .AddClass(Modifier("preserve-space"), PreserveSpace && Text is not null)
            .AddClass(Modifier("complete"), IsComplete);

    /// <inheritdoc />
    protected override void OnParametersSet ()
    {
        base.OnParametersSet();

        // Restarts only when the source actually changes, so an unrelated re-render of the
        // parent does not send the text back to the beginning.
        if (!ReferenceEquals(Stream, _lastStream))
        {
            _lastStream = Stream;
            _lastText = null;
            Restart(consumeStream: true);
            return;
        }

        if (Stream is null && Text != _lastText)
        {
            _lastText = Text;
            Restart(consumeStream: false);
        }
    }

    private void Restart ( bool consumeStream )
    {
        _run?.Cancel();
        _run?.Dispose();
        _run = new CancellationTokenSource();

        _target.Clear();
        _revealed = string.Empty;
        _scrambled = string.Empty;

        if (!consumeStream && Text is not null)
        {
            _target.Append(Text);
        }

        var token = _run.Token;

        if (consumeStream && Stream is not null)
        {
            _ = ConsumeStreamAsync(Stream, token);
        }

        _ = RevealAsync(token);
    }

    /// <summary>
    /// Appends stream chunks to the target as they arrive. Runs alongside the reveal rather than
    /// ahead of it, so text shows at a steady pace however unevenly it arrives.
    /// </summary>
    private async Task ConsumeStreamAsync ( IAsyncEnumerable<string> stream, CancellationToken token )
    {
        try
        {
            await foreach (var chunk in stream.WithCancellation(token))
            {
                _target.Append(chunk);
                await InvokeAsync(StateHasChanged);
            }
        }
        catch (OperationCanceledException)
        {
            // Superseded by a newer stream, or the component has gone.
        }
    }

    private async Task RevealAsync ( CancellationToken token )
    {
        try
        {
            if (StartDelay > 0)
            {
                await Task.Delay(StartDelay, token);
            }

            while (!token.IsCancellationRequested)
            {
                // A stream may not have delivered anything yet; wait rather than finishing early.
                if (_revealed.Length >= _target.Length)
                {
                    if (Stream is not null)
                    {
                        await Task.Delay(DelayPerStep, token);
                        continue;
                    }

                    break;
                }

                var next = NextStep();
                _revealed = _target.ToString(0, next);
                _scrambled = BuildScramble(next);

                await InvokeAsync(StateHasChanged);

                var pause = DelayPerStep;

                if (next > 0 && PauseOn.TryGetValue(_target[next - 1], out var extra))
                {
                    pause += extra;
                }

                await Task.Delay(pause, token);
            }

            if (token.IsCancellationRequested)
            {
                return;
            }

            _scrambled = string.Empty;
            await InvokeAsync(StateHasChanged);
            await OnCompleted.InvokeAsync();

            if (Loop)
            {
                await Task.Delay(LoopDelay, token);

                _revealed = string.Empty;
                await InvokeAsync(StateHasChanged);
                await RevealAsync(token);
            }
        }
        catch (OperationCanceledException)
        {
            // Restarted or disposed; the newer run owns the text now.
        }
    }

    /// <summary>How far through the text the next step lands.</summary>
    private int NextStep ()
    {
        if (!ByWord)
        {
            return _revealed.Length + 1;
        }

        // Word mode advances to the end of the next run of non-space characters, so a word
        // appears whole rather than growing letter by letter.
        var index = _revealed.Length;

        while (index < _target.Length && char.IsWhiteSpace(_target[index]))
        {
            index++;
        }

        while (index < _target.Length && !char.IsWhiteSpace(_target[index]))
        {
            index++;
        }

        return Math.Min(index, _target.Length);
    }

    /// <summary>The flickering glyphs shown just ahead of the cursor.</summary>
    private string BuildScramble ( int revealedTo )
    {
        if (!Scramble || ScrambleCharacters.Length == 0)
        {
            return string.Empty;
        }

        // Never longer than what is actually still to come, so the scramble does not imply more
        // text than there is.
        var length = Math.Min(ScrambleLength, _target.Length - revealedTo);

        if (length <= 0)
        {
            return string.Empty;
        }

        return string.Create(length, ScrambleCharacters, ( span, glyphs ) =>
        {
            for (var i = 0; i < span.Length; i++)
            {
                span[i] = glyphs[_random.Next(glyphs.Length)];
            }
        });
    }

    /// <inheritdoc />
    public void Dispose ()
    {
        _run?.Cancel();
        _run?.Dispose();
    }
}