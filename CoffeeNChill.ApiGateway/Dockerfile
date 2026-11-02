
# Stage 1: Build the API Gateway
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src

COPY CoffeeNChill.ApiGateway.csproj ./

RUN dotnet restore CoffeeNChill.ApiGateway.csproj

COPY . ./

RUN dotnet publish CoffeeNChill.ApiGateway.csproj \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

# Stage 2: Run the API Gateway
FROM mcr.microsoft.com/dotnet/aspnet:8.0

WORKDIR /app

COPY --from=build /app/publish ./

ENV ASPNETCORE_HTTP_PORTS=8080

EXPOSE 8080

ENTRYPOINT ["dotnet", "CoffeeNChill.ApiGateway.dll"]
