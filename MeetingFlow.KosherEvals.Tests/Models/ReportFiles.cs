namespace MeetingFlow.KosherEvals.Tests.Models;

public class ReportFiles
{
    // Paths to both saved files, without deriving one from the other.
    public required string JsonPath { get; init; }
    public required string HtmlPath { get; init; }
}
