using System.Globalization;
using Veto.Components.Gantt;
using Xunit;

namespace Veto.Tests.E2E;

/// <summary>Unit tests for <see cref="VetoGanttExporter"/>, the CSV and JSON the export menu writes.</summary>
public class VetoGanttExporterTests
{
    private static readonly VetoGanttItem Plain =
        new("a", "Design", new DateTime(2026, 3, 2, 9, 0, 0), new DateTime(2026, 3, 6, 17, 0, 0));

    [Fact]
    public void Dates_are_gregorian_whatever_the_current_culture ()
    {
        var previous = CultureInfo.CurrentCulture;

        try
        {
            // Thai uses the Buddhist calendar: 2026 is 2569 there.
            CultureInfo.CurrentCulture = new CultureInfo("th-TH");

            var csv = VetoGanttExporter.ToCsv([Plain]);
            var json = VetoGanttExporter.ToJson([Plain]);

            Assert.Contains("2026-03-02", csv);
            Assert.DoesNotContain("2569", csv);
            Assert.Contains("\"2026-03-02\"", json);
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Theory]
    [InlineData("=HYPERLINK(\"http://x\")")]
    [InlineData("+1+2")]
    [InlineData("-2+3")]
    [InlineData("@SUM(A1)")]
    public void A_cell_that_would_run_as_a_formula_is_defused ( string name )
    {
        var item = new VetoGanttItem("b", name, new DateTime(2026, 3, 2), new DateTime(2026, 3, 3));

        var row = VetoGanttExporter.ToCsv([item]).Split(Environment.NewLine)[1];
        var cell = row.Split(',')[1].TrimStart('"');

        Assert.StartsWith("'", cell);
    }

    [Fact]
    public void Commas_quotes_and_newlines_are_quoted ()
    {
        var item = new VetoGanttItem("c", "Plan, \"review\"\nship", new DateTime(2026, 3, 2), new DateTime(2026, 3, 3));

        Assert.Contains("\"Plan, \"\"review\"\"\nship\"", VetoGanttExporter.ToCsv([item]));
    }
}
