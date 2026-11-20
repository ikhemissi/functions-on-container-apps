using System.Text.Json;
using Azure.Data.Tables;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace ReceiptFunctions;

// GET /api/summaries: the newest receipt summaries, anonymous and read-only (D7).
public sealed class ListSummaries(TableClient summariesTable)
{
    public const int MaxSummaries = 20;

    [Function(nameof(ListSummaries))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "summaries")] HttpRequest request,
        CancellationToken cancellationToken)
    {
        var summaries = new List<SummaryResponse>(MaxSummaries);
        var rows = summariesTable.QueryAsync<ReceiptSummary>(
            row => row.PartitionKey == ReceiptSummary.SummariesPartition,
            maxPerPage: MaxSummaries,
            cancellationToken: cancellationToken);

        // Rows come newest first (see ReceiptSummary.NewestFirstRowKey), so the first ones are the newest.
        await foreach (var row in rows)
        {
            summaries.Add(SummaryResponse.From(row));
            if (summaries.Count == MaxSummaries)
            {
                break;
            }
        }

        return new OkObjectResult(summaries);
    }
}

// The public shape of a summary: the totals as a JSON object, without the table's keys and ETag.
public sealed record SummaryResponse(
    string BlobName,
    int LineCount,
    int MalformedLineCount,
    IReadOnlyDictionary<string, decimal> Totals,
    DateTimeOffset ProcessedAt)
{
    public static SummaryResponse From(ReceiptSummary row) => new(
        row.BlobName,
        row.LineCount,
        row.MalformedLineCount,
        JsonSerializer.Deserialize<Dictionary<string, decimal>>(row.TotalsJson) ?? [],
        row.ProcessedAt);
}
