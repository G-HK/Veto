using System.Diagnostics;
using System.Net.Http;
using Xunit;

namespace Veto.Tests.E2E.Infrastructure;

/// <summary>
/// Starts the VetoWeb demo app once for the whole test run and shuts it down afterwards.
/// The app is launched as a real out-of-process server because Blazor WebAssembly needs
/// the framework files served over HTTP - <c>WebApplicationFactory</c> cannot do that.
/// </summary>
public sealed class BlazorAppFixture : IAsyncLifetime
{
    // A port of its own, so a `dotnet run` already listening on the 5038 dev profile
    // does not collide with a test run.
    private const string Url = "http://localhost:5177";

    private Process? _process;

    /// <summary>Root URL the demo app is served from.</summary>
    public string BaseUrl => Url;

    /// <inheritdoc />
    public async Task InitializeAsync ()
    {
        if (await IsRespondingAsync(TimeSpan.FromSeconds(2)))
        {
            // Something is already serving the app (a developer's own run); reuse it.
            return;
        }

        var repoRoot = FindRepoRoot();
        var publishDir = Path.Combine(Path.GetTempPath(), "veto-e2e-publish");

        // The app is published and then run from the published output rather than started
        // with `dotnet run`. Only the published layout puts wwwroot/_framework next to the
        // entry assembly, which is what MapStaticAssets needs in order to serve the
        // WebAssembly runtime - from a source tree those requests fail and the page never
        // becomes interactive.
        // Cleared first: `dotnet publish -o` does not prune, so fingerprinted asset bundles
        // from earlier runs pile up and a run can end up serving a stale one.
        if (Directory.Exists(publishDir))
        {
            Directory.Delete(publishDir, recursive: true);
        }

        Publish(repoRoot, publishDir);

        _process = Process.Start(new ProcessStartInfo
        {
            FileName = "dotnet",
            WorkingDirectory = publishDir,
            UseShellExecute = false,
            ArgumentList = { Path.Combine(publishDir, "VetoWeb.dll"), "--urls", Url },
            Environment = { ["ASPNETCORE_ENVIRONMENT"] = "Production" }
        }) ?? throw new InvalidOperationException("Could not start the published VetoWeb demo app.");

        if (!await IsRespondingAsync(TimeSpan.FromMinutes(2)))
        {
            throw new TimeoutException($"The VetoWeb demo app did not start listening on {Url}.");
        }
    }

    private static void Publish ( string repoRoot, string publishDir )
    {
        using var publish = Process.Start(new ProcessStartInfo
        {
            FileName = "dotnet",
            WorkingDirectory = repoRoot,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            ArgumentList =
            {
                "publish",
                Path.Combine(repoRoot, "VetoWeb", "VetoWeb", "VetoWeb.csproj"),
                "--configuration", "Release",
                "--output", publishDir,
                "--verbosity", "quiet",
                "--nologo",
                // MSBuild's reusable worker nodes and the compiler server outlive the publish and
                // inherit its output pipes, so ReadToEnd below never sees end-of-stream and the
                // whole test run hangs in fixture setup. Neither may outlive this process.
                "-nodeReuse:false",
                "-p:UseSharedCompilation=false"
            }
        }) ?? throw new InvalidOperationException("Could not start `dotnet publish`.");

        // Drained rather than inherited so a full output buffer cannot block the publish.
        var output = publish.StandardOutput.ReadToEnd() + publish.StandardError.ReadToEnd();
        publish.WaitForExit();

        if (publish.ExitCode != 0)
        {
            throw new InvalidOperationException($"Publishing the VetoWeb demo app failed:{Environment.NewLine}{output}");
        }
    }

    /// <inheritdoc />
    public Task DisposeAsync ()
    {
        if (_process is { HasExited: false })
        {
            // entireProcessTree: `dotnet run` spawns the actual web host as a child.
            _process.Kill(entireProcessTree: true);
            _process.WaitForExit(10_000);
        }

        _process?.Dispose();
        return Task.CompletedTask;
    }

    private static async Task<bool> IsRespondingAsync ( TimeSpan timeout )
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        var deadline = DateTime.UtcNow + timeout;

        while (DateTime.UtcNow < deadline)
        {
            try
            {
                using var response = await client.GetAsync(Url);
                if (response.IsSuccessStatusCode)
                {
                    return true;
                }
            }
            catch (HttpRequestException)
            {
                // Not listening yet.
            }
            catch (TaskCanceledException)
            {
                // Request timed out; keep waiting.
            }

            await Task.Delay(500);
        }

        return false;
    }

    private static string FindRepoRoot ()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);

        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Veto.slnx")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new DirectoryNotFoundException("Could not locate Veto.slnx above the test binaries.");
    }
}

/// <summary>Groups every test that needs the demo app, so it is started only once.</summary>
[CollectionDefinition( DemoAppCollection.Name )]
public sealed class DemoAppCollection : ICollectionFixture<BlazorAppFixture>
{
    /// <summary>The collection name tests reference with <c>[Collection]</c>.</summary>
    public const string Name = "Veto demo app";
}
