// Starts the isolated worker: HTTP through ASP.NET Core, telemetry through OpenTelemetry, data clients with the app's identity.
using Azure.Identity;
using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Azure.Functions.Worker.OpenTelemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ReceiptFunctions;

var builder = FunctionsApplication.CreateBuilder(args);
builder.ConfigureFunctionsWebApplication();

// One identity for data and telemetry: the app's user-assigned identity in Azure (D4), the developer's sign-in locally.
var credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions
{
    ManagedIdentityClientId = builder.Configuration[$"{DataStorage.ConnectionName}:clientId"],
});

builder.Services.AddSingleton(DataStorage.CreateBlobServiceClient(builder.Configuration, credential));
builder.Services.AddSingleton(DataStorage.CreateSummariesTableClient(builder.Configuration, credential));

// Application Insights accepts only Entra-authenticated telemetry here, so the exporter gets the credential;
// without a connection string (local runs) nothing is exported.
var openTelemetry = builder.Services.AddOpenTelemetry().UseFunctionsWorkerDefaults();
var isApplicationInsightsConfigured = !string.IsNullOrWhiteSpace(builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]);
if (isApplicationInsightsConfigured)
{
    openTelemetry.UseAzureMonitorExporter(options => options.Credential = credential);
}

builder.Build().Run();
