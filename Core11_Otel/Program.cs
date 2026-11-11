using System.Diagnostics;
using System.Diagnostics.Metrics;
using Core11_Otel.Data;
using Core11_Otel.Data.Entities;
using Core11_Otel.Exceptions;
using Core11_Otel.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenTelemetry;

var builder = WebApplication.CreateBuilder(args);

var appInsightsConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"]
    ?? throw new InvalidOperationException("ApplicationInsights:ConnectionString is missing.");
var sqlConnectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is missing.");

// 1. Configure Microsoft.OpenTelemetry Distro (Traces, Metrics & Logs)
builder.UseMicrosoftOpenTelemetry(options =>
{
    options.Exporters = ExportTarget.AzureMonitor;
    options.AzureMonitor.ConnectionString = appInsightsConnectionString;
});
// 2. Register Custom Application ActivitySource & Meter with OpenTelemetry
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing.AddSource("OrdersApiCore11.Telemetry"))
    .WithMetrics(metrics => metrics.AddMeter("OrdersApiCore11.Telemetry"));

builder.Services.AddSingleton(new ActivitySource("OrdersApiCore11.Telemetry"));
builder.Services.AddSingleton<OrderMetrics>();
builder.Services.AddDbContext<OrdersDbContext>(options =>
    options.UseSqlServer(sqlConnectionString));

var app = builder.Build();

app.MapGet("/orders", async (
    OrdersDbContext dbContext,
    ActivitySource activitySource,
    ILogger<Program> logger,
    CancellationToken ct) =>
{
    using var span = activitySource.StartActivity("FetchAllOrdersFromSql");

    try
    {
        var orders = await dbContext.Orders
            .AsNoTracking()
            .OrderBy(order => order.OrderId)
            .ToListAsync(ct);

        logger.LogInformation("Retrieved {OrderCount} orders from Azure SQL", orders.Count);
        return Results.Ok(orders.Select(OrderDto.FromEntity).ToArray());
    }
    catch (Exception exception) when (exception is not OperationCanceledException || !ct.IsCancellationRequested)
    {
        logger.LogError(exception, "Failed to retrieve orders");
        var activity = span ?? Activity.Current;
        activity?.SetStatus(ActivityStatusCode.Error, exception.Message);
        activity?.AddException(exception);
        return Results.Problem(statusCode: StatusCodes.Status500InternalServerError,
            title: "An unexpected error occurred");
    }
});

// 3. GET Endpoint: Fetch an order from Azure SQL
app.MapGet("/orders/{id:int}", async (
    int id,
    OrdersDbContext dbContext,
    ActivitySource activitySource,
    ILogger<Program> logger,
    CancellationToken ct) =>
{
    using var span = activitySource.StartActivity("FetchOrderFromSql");
    span?.SetTag("order.id", id);

    try
    {
        var order = await dbContext.Orders
            .AsNoTracking()
            .SingleOrDefaultAsync(order => order.OrderId == id, ct);

        if (order is null)
        {
            throw new OrderNotFoundException(id);
        }

        logger.LogInformation("Retrieved order {OrderId} for customer {CustomerId}", order.OrderId, order.CustomerId);
        return Results.Ok(OrderDto.FromEntity(order));
    }
    catch (Exception exception) when (exception is not OperationCanceledException || !ct.IsCancellationRequested)
    {
        logger.LogError(exception, "Failed to retrieve order {OrderId}", id);
        var activity = span ?? Activity.Current;
        activity?.SetTag("order.id", id);
        activity?.SetStatus(ActivityStatusCode.Error, exception.Message);
        activity?.AddException(exception);

        return exception is OrderNotFoundException
            ? Results.Problem(statusCode: StatusCodes.Status404NotFound,
                title: "Order not found", detail: exception.Message)
            : Results.Problem(statusCode: StatusCodes.Status500InternalServerError,
                title: "An unexpected error occurred");
    }
});

// 4. POST Endpoint: Insert an order into Azure SQL and record a metric
app.MapPost("/orders", async (
    CreateOrderRequest request,
    OrdersDbContext dbContext,
    ActivitySource activitySource,
    OrderMetrics orderMetrics,
    ILogger<Program> logger,
    CancellationToken ct) =>
{
    using var span = activitySource.StartActivity("CreateOrderInSql");

    try
    {
        var order = request.ToEntity();
        dbContext.Orders.Add(order);
        await dbContext.SaveChangesAsync(ct);

        span?.SetTag("order.id", order.OrderId);
        orderMetrics.RecordOrderCreated(order.Freight);
        logger.LogInformation("Created order {OrderId} with freight {Freight}", order.OrderId, order.Freight);

        return Results.Created($"/orders/{order.OrderId}", OrderDto.FromEntity(order));
    }
    catch (Exception exception) when (exception is not OperationCanceledException || !ct.IsCancellationRequested)
    {
        logger.LogError(exception, "Failed to create order");
        var activity = span ?? Activity.Current;
        activity?.SetStatus(ActivityStatusCode.Error, exception.Message);
        activity?.AddException(exception);
        return Results.Problem(statusCode: StatusCodes.Status500InternalServerError,
            title: "An unexpected error occurred");
    }
});

app.Run();

public sealed class OrderMetrics
{
    private readonly Counter<long> _ordersCreated;
    private readonly Histogram<double> _orderAmount;

    public OrderMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create("OrdersApi.Telemetry");
        _ordersCreated = meter.CreateCounter<long>("orders.created", unit: "{order}");
        _orderAmount = meter.CreateHistogram<double>("orders.amount", unit: "INR");
    }

    public void RecordOrderCreated(decimal? amount)
    {
        _ordersCreated.Add(1);
        if (amount.HasValue)
        {
            _orderAmount.Record((double)amount.Value);
        }
    }
}