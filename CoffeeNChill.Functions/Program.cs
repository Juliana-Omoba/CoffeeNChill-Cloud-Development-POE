using CoffeeNChill.Functions.Interfaces;
using CoffeeNChill.Functions.Services;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication.CreateBuilder(args);

// Configure Azure Functions
// Configure Azure Functions with ASP.NET Core HTTP integration 
builder.ConfigureFunctionsWebApplication();

// Register services for Dependency Injection
builder.Services.AddSingleton<ITableStorageService, TableStorageService>();
builder.Services.AddSingleton<IFileStorageService, FileStorageService>();


// Part 2
builder.Services.AddSingleton<IOrderQueueService, OrderQueueService>();
builder.Services.AddSingleton<IOrderTableService, OrderTableService>();


builder.Build().Run();