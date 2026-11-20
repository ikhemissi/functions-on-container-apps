namespace ReceiptFunctions.Tests;

// Checks how the Event Grid BlobCreated message of the queue is read: container, blob name, missing URL.
public class EventGridBlobEventTests
{
    private static string BlobCreatedMessage(string url) => $$"""
        {
          "subject": "/blobServices/default/containers/receipts/blobs/receipts-2026-11.csv",
          "eventType": "Microsoft.Storage.BlobCreated",
          "id": "6d79dbfb-0e37-4fc4-981f-442c9ca89e3e",
          "data": { "api": "PutBlob", "contentType": "text/csv", "blobType": "BlockBlob", "url": "{{url}}" },
          "dataVersion": "",
          "metadataVersion": "1",
          "eventTime": "2026-11-20T10:00:00.0000000Z"
        }
        """;

    [Fact]
    public void Reads_the_container_and_blob_name_of_a_receipts_upload()
    {
        var message = BlobCreatedMessage("https://stdata.blob.core.windows.net/receipts/receipts-2026-11.csv");

        var blobEvent = EventGridBlobEvent.Parse(message);

        Assert.Equal(new EventGridBlobEvent("receipts", "receipts-2026-11.csv"), blobEvent);
    }

    [Fact]
    public void Keeps_folders_and_decodes_escaped_characters_in_the_blob_name()
    {
        var message = BlobCreatedMessage("https://stdata.blob.core.windows.net/receipts/2026/november%20receipts.csv");

        var blobEvent = EventGridBlobEvent.Parse(message);

        Assert.Equal(new EventGridBlobEvent("receipts", "2026/november receipts.csv"), blobEvent);
    }

    [Fact]
    public void Reads_the_container_of_an_upload_elsewhere()
    {
        var message = BlobCreatedMessage("https://stdata.blob.core.windows.net/invoices/invoice-42.pdf");

        var blobEvent = EventGridBlobEvent.Parse(message);

        Assert.NotNull(blobEvent);
        Assert.Equal("invoices", blobEvent.ContainerName);
        Assert.NotEqual(SummarizeReceipts.ReceiptsContainer, blobEvent.ContainerName);
    }

    [Theory]
    [InlineData("""{ "eventType": "Microsoft.Storage.BlobCreated", "data": { "api": "PutBlob" } }""")]
    [InlineData("""{ "eventType": "Microsoft.Storage.BlobCreated" }""")]
    [InlineData("""{ "data": { "url": 42 } }""")]
    [InlineData("""{ "data": { "url": "not a url" } }""")]
    [InlineData("""{ "data": { "url": "https://stdata.blob.core.windows.net/receipts" } }""")]
    [InlineData("[]")]
    [InlineData("not json")]
    public void Returns_null_without_a_usable_blob_url(string message)
    {
        Assert.Null(EventGridBlobEvent.Parse(message));
    }
}
