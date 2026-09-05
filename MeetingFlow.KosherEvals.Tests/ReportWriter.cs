using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;
using MeetingFlow.KosherEvals.Tests.Models;

namespace MeetingFlow.KosherEvals.Tests;

public static class ReportWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
        Converters = { new JsonStringEnumConverter() }
    };

    public static async Task<ReportFiles> SaveJsonAndHtmlAsync(EvalReport report, string directory)
    {
        Directory.CreateDirectory(directory);
        var name = $"eval-{report.StartedAtUtc:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}";
        var jsonPath = Path.Combine(directory, name + ".json");
        var htmlPath = Path.Combine(directory, name + ".html");

        var json = JsonSerializer.Serialize(report, JsonOptions);
        await File.WriteAllTextAsync(jsonPath, json);

        // Generate HTML from the saved JSON, not from the original object.
        await ConvertJsonToHtmlAsync(jsonPath, htmlPath);

        return new ReportFiles
        {
            JsonPath = jsonPath,
            HtmlPath = htmlPath
        };
    }

    public static async Task ConvertJsonToHtmlAsync(string jsonPath, string htmlPath)
    {
        // Protect the source JSON from being accidentally overwritten with HTML.
        if (string.Equals(Path.GetFullPath(jsonPath), Path.GetFullPath(htmlPath), StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("JSON and HTML must have different file paths.", nameof(htmlPath));
        }

        var json = await File.ReadAllTextAsync(jsonPath);
        var report = JsonSerializer.Deserialize<EvalReport>(json, JsonOptions)
            ?? throw new InvalidOperationException("The JSON report is empty.");

        var rows = new StringBuilder();
        foreach (var item in report.Cases)
        {
            rows.AppendLine($"""
                <tr>
                  <th scope="row">{Encode(item.Case.Id)}</th>
                  <td>{Encode(item.Case.ExpectedStatus.ToString())}</td>
                  <td>{Encode(item.Actual.Status.ToString())}</td>
                  <td>{(item.CodePassed ? "Pass" : "Fail")}</td>
                  <td>{item.Judgment.Score} / 2</td>
                  <td>{(item.Judgment.HasInventedFacts ? "Yes" : "No")}</td>
                  <td class="{(item.Passed ? "pass" : "fail")}">{(item.Passed ? "Passed" : "Failed")}</td>
                </tr>
                <tr><td colspan="7"><details>
                  <summary>Description, expectations and response</summary>
                  <dl>
                    <dt>Dish</dt><dd>{Encode(item.Case.Dish)}</dd>
                    <dt>Expected clarification</dt><dd>{Encode(item.Case.ExpectedClarification ?? "No clarification needed")}</dd>
                    <dt>Expected reasoning</dt><dd>{Encode(item.Case.ExpectedReasoning)}</dd>
                    <dt>Service response</dt><dd>{Encode(item.Actual.Explanation)}</dd>
                    <dt>Judge reasoning</dt><dd>{Encode(item.Judgment.Reason)}</dd>
                  </dl>
                </details></td></tr>
                """);
        }

        var conclusion = report.Conclusion switch
        {
            "Passed" => "All cases passed",
            "Failed" => "Some cases failed",
            _ => "Run incomplete"
        };
        var average = report.AverageScore?.ToString("0.00", CultureInfo.InvariantCulture) ?? "No scores";
        var html = $$"""
            <!doctype html>
            <html lang="en">
            <head>
              <meta charset="utf-8">
              <meta name="viewport" content="width=device-width, initial-scale=1">
              <meta http-equiv="Content-Security-Policy" content="default-src 'none'; style-src 'unsafe-inline'; base-uri 'none'; form-action 'none'">
              <title>Kosher evaluation report</title>
              <style>
                body { font: 16px/1.5 system-ui, sans-serif; color: #18334b; background: #f5f8fa; margin: 0; }
                main { max-width: 1200px; margin: 40px auto; padding: 24px; background: white; border-top: 6px solid #138b99; }
                h1 { margin-top: 0; } .table-wrap { overflow-x: auto; }
                table { width: 100%; border-collapse: collapse; }
                th, td { text-align: left; padding: 12px; border-bottom: 1px solid #dce5eb; vertical-align: top; }
                thead { background: #edf4f6; } .pass { color: #126338; } .fail { color: #a02828; }
                dt { font-weight: 600; margin-top: 12px; } dd { margin-left: 0; white-space: pre-wrap; overflow-wrap: anywhere; }
                summary { cursor: pointer; } .notice { background: #fff3d6; padding: 12px; }
                @media print { main { margin: 0; } }
              </style>
            </head>
            <body><main>
              <h1>Kosher evaluation</h1>
              <p>{{Encode(conclusion)}}</p>
              <p>Started (UTC): {{Encode(report.StartedAtUtc.UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss"))}}<br>
                 Model: {{Encode(report.Model)}} · Judge: {{Encode(report.JudgeModel)}}</p>
              <p>Completed: {{report.CompletedCases}} / {{report.TotalCases}} · Passed: {{report.PassedCases}} / {{report.TotalCases}} ·
                 Average score of completed cases: {{Encode(average)}} (maximum 2)</p>
              {{(report.Error is null ? "" : $"<p class=\"notice\">{Encode(report.Error)}</p>")}}
              <div class="table-wrap"><table>
                <thead><tr><th>Case</th><th>Expected</th><th>Actual</th><th>Code check</th><th>Judge score</th><th>Invented facts</th><th>Result</th></tr></thead>
                <tbody>{{rows}}</tbody>
              </table></div>
              <p>Pass: matching status, score of 2, and no invented facts.</p>
            </main></body></html>
            """;

        await File.WriteAllTextAsync(htmlPath, html);
    }

    // Display even HTML-containing answers as text, not as page code.
    private static string Encode(string value) => WebUtility.HtmlEncode(value);
}
