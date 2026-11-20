namespace ReceiptFunctions.Tests;

// Checks the summary row: newest first in key order, and the totals stored as JSON.
public class ReceiptSummaryTests
{
    [Fact]
    public void Row_keys_sort_the_newest_summary_first()
    {
        var older = new DateTimeOffset(2026, 11, 20, 10, 0, 0, TimeSpan.Zero);
        var newer = older.AddMilliseconds(1);

        var olderKey = ReceiptSummary.NewestFirstRowKey(older);
        var newerKey = ReceiptSummary.NewestFirstRowKey(newer);

        Assert.True(string.CompareOrdinal(newerKey, olderKey) < 0);
        Assert.Equal(olderKey.Length, newerKey.Length);
    }

    [Fact]
    public void Row_keys_keep_their_order_across_a_change_in_digit_count()
    {
        var keys = new[] { DateTimeOffset.MinValue, new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero), DateTimeOffset.MaxValue }
            .Select(ReceiptSummary.NewestFirstRowKey)
            .ToList();

        Assert.Equal(keys.OrderByDescending(key => key, StringComparer.Ordinal), keys);
    }

    [Fact]
    public void Summary_stores_the_file_and_its_totals()
    {
        var totals = new ReceiptTotals(3, 1, new Dictionary<string, decimal> { ["EUR"] = 19.75m, ["USD"] = 20.00m });
        var processedAt = new DateTimeOffset(2026, 11, 20, 10, 0, 0, TimeSpan.Zero);

        var summary = ReceiptSummary.Create("receipts-2026-11.csv", totals, processedAt);

        Assert.Equal("summaries", summary.PartitionKey);
        Assert.Equal(ReceiptSummary.NewestFirstRowKey(processedAt), summary.RowKey);
        Assert.Equal("receipts-2026-11.csv", summary.BlobName);
        Assert.Equal(3, summary.LineCount);
        Assert.Equal(1, summary.MalformedLineCount);
        Assert.Equal("""{"EUR":19.75,"USD":20.00}""", summary.TotalsJson);
        Assert.Equal(processedAt, summary.ProcessedAt);
    }

    [Fact]
    public void Response_returns_the_totals_as_an_object()
    {
        var totals = new ReceiptTotals(1, 0, new Dictionary<string, decimal> { ["EUR"] = 12.50m });
        var summary = ReceiptSummary.Create("receipts-2026-11.csv", totals, DateTimeOffset.UnixEpoch);

        var response = SummaryResponse.From(summary);

        Assert.Equal(12.50m, response.Totals["EUR"]);
        Assert.Equal("receipts-2026-11.csv", response.BlobName);
    }
}
