# ============================================================
# Stage 1: Build the Azure Functions application
# ============================================================

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src

# Copy the project file first
COPY ["CoffeeNChill.Functions.csproj", "./"]

# Restore NuGet packages
RUN dotnet restore "CoffeeNChill.Functions.csproj"

# Copy the remaining application files
COPY . .

# Build and publish the application
RUN dotnet publish "CoffeeNChill.Functions.csproj" \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false


# ============================================================
# Stage 2: Azure Functions runtime
# ============================================================

FROM mcr.microsoft.com/azure-functions/dotnet-isolated:4-dotnet-isolated8.0

WORKDIR /home/site/wwwroot

# Copy the published application from the build stage
COPY --from=build /app/publish .

# Azure Functions configuration
ENV AzureWebJobsScriptRoot=/home/site/wwwroot
ENV AzureFunctionsJobHost__Logging__Console__IsEnabled=true

# Azure Functions listens on port 80 inside the container
EXPOSE 80
