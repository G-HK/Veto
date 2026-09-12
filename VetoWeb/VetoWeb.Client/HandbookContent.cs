using Veto.Components.Handbook;

namespace VetoWeb.Client;

/// <summary>
/// Content for the Handbook demo. The body text is lorem ipsum on purpose: it is filler that
/// reads as filler, and each section is long enough that following an anchor visibly moves the
/// page - which is the thing the contents rail is there to demonstrate.
/// </summary>
public static class HandbookContent
{
    private const string Lorem =
        "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor " +
        "incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud " +
        "exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure " +
        "dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. " +
        "Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt " +
        "mollit anim id est laborum.";

    private const string LoremMore =
        "Sed ut perspiciatis unde omnis iste natus error sit voluptatem accusantium doloremque " +
        "laudantium, totam rem aperiam, eaque ipsa quae ab illo inventore veritatis et quasi " +
        "architecto beatae vitae dicta sunt explicabo. Nemo enim ipsam voluptatem quia voluptas " +
        "sit aspernatur aut odit aut fugit, sed quia consequuntur magni dolores eos qui ratione " +
        "voluptatem sequi nesciunt. Neque porro quisquam est, qui dolorem ipsum quia dolor sit " +
        "amet, consectetur, adipisci velit.";

    private const string LoremEnd =
        "At vero eos et accusamus et iusto odio dignissimos ducimus qui blanditiis praesentium " +
        "voluptatum deleniti atque corrupti quos dolores et quas molestias excepturi sint " +
        "occaecati cupiditate non provident, similique sunt in culpa qui officia deserunt " +
        "mollitia animi, id est laborum et dolorum fuga. Et harum quidem rerum facilis est et " +
        "expedita distinctio. Nam libero tempore, cum soluta nobis est eligendi optio cumque " +
        "nihil impedit quo minus id quod maxime placeat facere possimus.";

    /// <summary>One page of the handbook.</summary>
    public sealed record Page ( string Eyebrow, string Title, string Lead, IReadOnlyList<VetoHandbookSection> Sections );

    /// <summary>Every page, keyed by its route slug.</summary>
    public static readonly Dictionary<string, Page> Pages = new()
    {
        ["index"] = new("Introduction", "Handbook",
            "A worked example of VetoDocsLayout and VetoHandbookPage: navigation on the left, a reading column in the middle, and a contents rail that follows the page.",
            Build(
                ("reading", "The reading column"),
                ("rail", "The contents rail"),
                ("drawer", "The navigation drawer"),
                ("anchors", "Anchors"))),

        ["start"] = new("Guides", "Getting started",
            "Install the package, register the services, and render your first component.",
            Build(
                ("install", "Install"),
                ("register", "Register the services"),
                ("stylesheet", "Link the stylesheet"),
                ("first", "Your first component"))),

        ["config"] = new("Guides", "Configuration",
            "Where settings live, how they are layered, and which of them can change at runtime.",
            Build(
                ("sources", "Configuration sources"),
                ("theme", "Theming"),
                ("tokens", "Overriding tokens"))),

        ["deploy"] = new("Guides", "Deployment",
            "Publishing the app, and the static-asset detail that catches people out.",
            Build(
                ("publish", "Publishing"),
                ("assets", "Static assets"),
                ("hosting", "Hosting"))),

        ["cli"] = new("Reference", "CLI",
            "Commands for building, testing and publishing.",
            Build(
                ("build", "build"),
                ("test", "test"),
                ("publish", "publish"))),

        ["api"] = new("Reference", "API",
            "The shared surface every component exposes, and where to find the rest.",
            Build(
                ("contract", "The component contract"),
                ("inputs", "Form components"),
                ("layouts", "Layouts")))
    };

    /// <summary>Returns the page for a route slug, falling back to the introduction.</summary>
    public static Page Resolve ( string? slug )
        => Pages.TryGetValue(string.IsNullOrWhiteSpace(slug) ? "index" : slug.ToLowerInvariant(), out var page)
            ? page
            : Pages["index"];

    private static IReadOnlyList<VetoHandbookSection> Build ( params (string Id, string Title)[] sections )
        => sections
            .Select(( section, index ) => new VetoHandbookSection
            {
                Id = section.Id,
                Title = section.Title,
                // Long on purpose, and of alternating lengths: the sections have to be tall
                // enough that following an anchor visibly moves the page, which is the whole
                // point of the contents rail.
                Body = index % 2 == 0
                    ? $"{Lorem} {LoremMore} {LoremEnd}"
                    : $"{LoremMore} {Lorem}"
            })
            .ToList();
}
