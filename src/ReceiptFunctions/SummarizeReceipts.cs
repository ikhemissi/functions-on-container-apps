using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace ReceiptFunctions;

// Sums each receipts CSV uploaded to the receipts container and stores its summary in the ReceiptSummaries table (D3, D7).
public sealed class SummarizeReceipts(BlobServiceClient blobServiceClient, TableClient summariesTable, ILogger<SummarizeReceipts> logger)
{
    public const string ReceiptsContainer = "receipts";

    [Function(nameof(SummarizeReceipts))]
    public async Task Run(
        [QueueTrigger("receipts-uploaded", Connection = DataStorage.ConnectionName)] string message,
        CancellationToken cancellationToken)
    {
        var blobEvent = EventGridBlobEvent.Parse(message);
        if (blobEvent is null)
        {
            logger.LogWarning("Ignored a message without a blob URL in data.url.");
            return;
        }

        if (blobEvent.ContainerName != ReceiptsContainer)
        {
            logger.LogInformation("Ignored blob {BlobName} of container {ContainerName}.", blobEvent.BlobName, blobEvent.ContainerName);
            return;
        }

        var content = await ReadBlobAsync(blobEvent, cancellationToken);
        var totals = ReceiptCsv.Summarize(content);
        var summary = ReceiptSummary.Create(blobEvent.BlobName, totals, DateTimeOffset.UtcNow);
        await summariesTable.AddEntityAsync(summary, cancellationToken);

        logger.LogInformation(
            "Summarized {BlobName}: {LineCount} lines, {MalformedLineCount} malformed, totals {TotalsJson}.",
            summary.BlobName, summary.LineCount, summary.MalformedLineCount, summary.TotalsJson);
    }

    private async Task<string> ReadBlobAsync(EventGridBlobEvent blobEvent, CancellationToken cancellationToken)
    {
        var blob = blobServiceClient.GetBlobContainerClient(blobEvent.ContainerName).GetBlobClient(blobEvent.BlobName);
        var download = await blob.DownloadContentAsync(cancellationToken);
        return download.Value.Content.ToString();
    }
}
