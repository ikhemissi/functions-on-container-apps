using System.Text.Json;
using Azure;
using Azure.Data.Tables;

namespace ReceiptFunctions;

// One row of the ReceiptSummaries table: the totals of one uploaded receipts file (D7).
public sealed class ReceiptSummary : ITableEntity
{
    public const string TableName = "ReceiptSummaries";
    public const string SummariesPartition = "summaries";

    public string PartitionKey { get; set; } = SummariesPartition;
    public string RowKey { get; set; } = string.Empty;
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    public string BlobName { get; set; } = string.Empty;
    public int LineCount { get; set; }
    public int MalformedLineCount { get; set; }
    public string TotalsJson { get; set; } = "{}";
    public DateTimeOffset ProcessedAt { get; set; }

    public static ReceiptSummary Create(string blobName, ReceiptTotals totals, DateTimeOffset processedAt) => new()
    {
        RowKey = NewestFirstRowKey(processedAt),
        BlobName = blobName,
        LineCount = totals.LineCount,
        MalformedLineCount = totals.MalformedLineCount,
        TotalsJson = JsonSerializer.Serialize(totals.TotalsByCurrency),
        ProcessedAt = processedAt,
    };

    // Tables sort rows by key, ascending: inverted ticks, padded to a fixed width, put the newest first.
    public static string NewestFirstRowKey(DateTimeOffset processedAt) =>
        (DateTimeOffset.MaxValue.UtcTicks - processedAt.UtcTicks).ToString("D19", System.Globalization.CultureInfo.InvariantCulture);
}
