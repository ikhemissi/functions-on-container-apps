using System.Text.Json;

namespace ReceiptFunctions;

// The blob named by an Event Grid BlobCreated event (Event Grid schema), as the queue delivers it (D3).
public sealed record EventGridBlobEvent(string ContainerName, string BlobName)
{
    // Returns null when the message is not JSON or has no usable blob URL in data.url.
    public static EventGridBlobEvent? Parse(string message)
    {
        var blobUrl = ReadBlobUrl(message);
        if (!Uri.TryCreate(blobUrl, UriKind.Absolute, out var blobUri))
        {
            return null;
        }

        // The path of a blob URL is /<container>/<blob name>; the blob name may contain slashes.
        var path = Uri.UnescapeDataString(blobUri.AbsolutePath).TrimStart('/');
        var separatorIndex = path.IndexOf('/');
        if (separatorIndex <= 0 || separatorIndex == path.Length - 1)
        {
            return null;
        }

        return new EventGridBlobEvent(path[..separatorIndex], path[(separatorIndex + 1)..]);
    }

    private static string? ReadBlobUrl(string message)
    {
        try
        {
            using var document = JsonDocument.Parse(message);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object
                || !root.TryGetProperty("data", out var data)
                || data.ValueKind != JsonValueKind.Object
                || !data.TryGetProperty("url", out var url)
                || url.ValueKind != JsonValueKind.String)
            {
                return null;
            }

            return url.GetString();
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
