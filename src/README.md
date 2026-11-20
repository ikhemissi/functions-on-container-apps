# Code

The receipt functions of the sample (D7 in [the decisions](../docs/DECISIONS.md)): .NET 10, Azure Functions isolated worker 2.x, built into a container image that runs as a Container App with `kind: functionapp` (D1).

| Path | Role |
|---|---|
| `ReceiptFunctions.slnx` | The solution: the functions and their tests |
| `ReceiptFunctions/SummarizeReceipts.cs` | Queue trigger on `receipts-uploaded`: reads the blob of an Event Grid `BlobCreated` event (D3), sums it, writes one row to the `ReceiptSummaries` table; ignores blobs of other containers |
| `ReceiptFunctions/ListSummaries.cs` | `GET /api/summaries`, anonymous: the 20 newest summaries as JSON |
| `ReceiptFunctions/ReceiptCsv.cs` | Parses a `date,merchant,amount,currency` file and sums it per currency; malformed lines are counted and skipped |
| `ReceiptFunctions/EventGridBlobEvent.cs` | Reads the container and blob name from the event's `data.url` |
| `ReceiptFunctions/ReceiptSummary.cs` | The table row: partition `summaries`, row key = inverted timestamp (newest first), blob name, line counts, totals as JSON |
| `ReceiptFunctions/DataStorage.cs` | Blob and table clients from the `DataStorage__*` settings, with the app's identity (D4) |
| `ReceiptFunctions/Program.cs` | Starts the worker; OpenTelemetry to Application Insights with `DefaultAzureCredential` when `APPLICATIONINSIGHTS_CONNECTION_STRING` is set |
| `ReceiptFunctions/Dockerfile` | Multi-stage image, listening on 8080 (D5) |
| `ReceiptFunctions.Tests/` | xUnit tests of the CSV totals, the event parsing and the row keys; no Azure needed |

## Settings

The app reads the data account through the `DataStorage` connection, without keys (D4): `DataStorage__blobServiceUri`, `DataStorage__queueServiceUri`, `DataStorage__tableServiceUri`, `DataStorage__credential=managedidentity` and `DataStorage__clientId`. The same client ID selects the identity for telemetry. The infrastructure sets them, with the host storage (`AzureWebJobsStorage__*`) and the Application Insights connection string, and creates the `receipts-uploaded` queue and the `ReceiptSummaries` table.

## Build and test

From the repository root, as CI does:

```bash
dotnet build src -warnaserror
dotnet test src                       # Microsoft.Testing.Platform, opted in by global.json
docker build src/ReceiptFunctions
```

## Run locally

Copy `ReceiptFunctions/local.settings.json.example` to `local.settings.json` (git-ignored): it points both connections at the [Azurite](https://learn.microsoft.com/azure/storage/common/storage-use-azurite) storage emulator. Create the `receipts` container, the `receipts-uploaded` queue and the `ReceiptSummaries` table in Azurite, then run `func start` in `ReceiptFunctions/` ([Core Tools](https://learn.microsoft.com/azure/azure-functions/functions-run-local)). A message on the queue is an Event Grid event (base64 encoded) whose `data.url` is `https://<account>.blob.core.windows.net/receipts/<blob name>`; [`samples/receipts-2026-11.csv`](../samples/receipts-2026-11.csv) is a file to upload.
