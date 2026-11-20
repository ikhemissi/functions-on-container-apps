# The receipt functions' container image: built with the .NET 10 SDK, run on the Functions isolated base image (D5).
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ReceiptFunctions.csproj ./
RUN dotnet restore ReceiptFunctions.csproj
COPY . ./
RUN dotnet publish ReceiptFunctions.csproj --configuration Release --no-restore --output /home/site/wwwroot

FROM mcr.microsoft.com/azure-functions/dotnet-isolated:4-dotnet-isolated10.0
# The base image listens on 80; 8080 lets the postdeploy step re-provision the app only once (D5).
ENV AzureWebJobsScriptRoot=/home/site/wwwroot \
    AzureFunctionsJobHost__Logging__Console__IsEnabled=true \
    ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
COPY --from=build /home/site/wwwroot /home/site/wwwroot
