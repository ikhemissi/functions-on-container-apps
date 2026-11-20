using Azure.Core;
using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;

namespace ReceiptFunctions;

// Builds the SDK clients of the data storage account from the same DataStorage settings as the queue trigger (D4).
public static class DataStorage
{
    public const string ConnectionName = "DataStorage";

    public static BlobServiceClient CreateBlobServiceClient(IConfiguration configuration, TokenCredential credential)
    {
        var settings = configuration.GetSection(ConnectionName);
        if (IsConnectionString(settings))
        {
            return new BlobServiceClient(settings.Value);
        }

        return new BlobServiceClient(RequiredUri(settings, "blobServiceUri"), credential);
    }

    public static TableClient CreateSummariesTableClient(IConfiguration configuration, TokenCredential credential)
    {
        var settings = configuration.GetSection(ConnectionName);
        if (IsConnectionString(settings))
        {
            return new TableClient(settings.Value, ReceiptSummary.TableName);
        }

        return new TableClient(RequiredUri(settings, "tableServiceUri"), ReceiptSummary.TableName, credential);
    }

    // A plain value is a connection string: only local runs against the storage emulator use one.
    private static bool IsConnectionString(IConfigurationSection settings) =>
        !string.IsNullOrWhiteSpace(settings.Value);

    private static Uri RequiredUri(IConfigurationSection settings, string name)
    {
        var value = settings[name];
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"Setting {ConnectionName}__{name} is missing.");
        }

        return new Uri(value);
    }
}
