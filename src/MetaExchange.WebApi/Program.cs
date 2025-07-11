using MetaExchange.Shared.Models;
using MetaExchange.Shared.Services;
using MetaExchange.WebApi;
using Scalar.AspNetCore;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

builder.Services.AddOpenApi();

builder.Services.AddSingleton<IExchangeService, FileBasedExchangeService>();
builder.Services.AddSingleton<IOrderBookService, OrderBookService>();

WebApplication app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        List<ScalarServer> servers = [];

        string? httpsPort = Environment.GetEnvironmentVariable("ASPNETCORE_HTTPS_PORT");
        if (httpsPort is not null)
        {
            servers.Add(new ScalarServer($"https://localhost:{httpsPort}"));
        }

        string? httpPort = Environment.GetEnvironmentVariable("ASPNETCORE_HTTP_PORT");
        if (httpPort is not null)
        {
            servers.Add(new ScalarServer($"http://localhost:{httpPort}"));
        }

        options.Servers = servers;
        options.Title = "Meta Exchange API";
        options.ShowSidebar = true;

        ILogger<Program> logger = app.Services.GetRequiredService<ILogger<Program>>();
        if (httpsPort is not null)
        {
            logger.LogInformation("Start scalar on https://localhost:{httpsPort}/scalar", httpsPort);
        }
        if (httpPort is not null)
        {
            logger.LogInformation("Start scalar on http://localhost:{httpPort}/scalar", httpPort);
        }
    });
}

app.UseHttpsRedirection();

//
// Crypto Exchange Endpoints
//

app.MapGet(ApiRoutes.CryptoExchanges.GetAll, async (
    ILoggerFactory loggerFactory,
    IOrderBookService orderBookService,
    CancellationToken cancellationToken) =>
{
    ILogger logger = loggerFactory.CreateLogger("CryptoExchange");
    logger.LogInformation("Get Crypto Exchanges called");

    Result<CryptoExchange[]> result = await orderBookService.GetCryptoExchanges(cancellationToken);
    if (!result.Successful ||
        result.Value is null)
    {
        logger.LogError("Error while getting crypto exchanges. Message: {message}", result.ErrorMessage);
        return Results.BadRequest();
    }

    CryptoExchange[] cryptoExchanges = result.Value;

    if (cryptoExchanges.Length == 0)
    {
        logger.LogWarning("No crypto exchanges found.");
        return Results.NotFound("No crypto exchanges found.");
    }

    IEnumerable<string> response = cryptoExchanges.Select(ce => ce.Id);
    return Results.Ok(response);
});

app.MapGet(ApiRoutes.CryptoExchanges.GetExchangeById, async (
    string id,
    ILoggerFactory loggerFactory,
    IOrderBookService orderBookService,
    CancellationToken cancellationToken) =>
{
    ILogger logger = loggerFactory.CreateLogger("CryptoExchange");
    logger.LogInformation("Get Crypto Exchange by id called");

    Result<CryptoExchange[]> result = await orderBookService.GetCryptoExchanges(cancellationToken);
    if (!result.Successful ||
        result.Value is null)
    {
        logger.LogError("Error while getting crypto exchanges. Message: {message}", result.ErrorMessage);
        return Results.BadRequest();
    }

    CryptoExchange[] cryptoExchanges = result.Value;

    CryptoExchange? exchange = cryptoExchanges.FirstOrDefault(e => e.Id == id);
    if (exchange is not null)
    {
        return Results.Ok(exchange);
    }

    logger.LogWarning("Crypto exchange with ID {id} not found.", id);
    return Results.NotFound($"Crypto exchange with ID {id} not found.");

});

//
// OrderBook Endpoints
//

app.MapGet(ApiRoutes.OrderBook.GetBuyOrderPlan, async (
    decimal amountOfBtc,
    bool execute,
    IOrderBookService orderBookService,
    ILoggerFactory loggerFactory,
    CancellationToken cancellationToken) =>
{
    ILogger logger = loggerFactory.CreateLogger("OrderBook");
    Result<OrderPlan> orderPlanResult = await orderBookService.CreateBuyPlan(amountOfBtc, cancellationToken);

    if (!orderPlanResult.Successful ||
        orderPlanResult.Value is null)
    {
        logger.LogError("Error while creating buy plan. Message: {message}", orderPlanResult.ErrorMessage);
        return Results.BadRequest();
    }

    OrderPlan orderPlan = orderPlanResult.Value;

    if (orderPlan.OrderPlanDetails.Length == 0)
    {
        logger.LogWarning("Order plan could not be created.");
        return Results.NotFound();
    }

    if (!execute)
    {
        return Results.Ok(orderPlan);
    }

    Result<OrderPlan> executionResult = await orderBookService.ExecuteOrderPlan(orderPlan, cancellationToken);
    if (executionResult.Successful)
    {
        return Results.Ok(orderPlan);
    }

    logger.LogError("Error while executing order plan. Message: {message}", executionResult.ErrorMessage);
    return Results.BadRequest();

});

app.MapGet(ApiRoutes.OrderBook.GetSellOrderPlan, async (
    decimal amountOfBtc,
    IOrderBookService orderBookService,
    ILoggerFactory loggerFactory,
    CancellationToken cancellationToken) =>
{
    ILogger logger = loggerFactory.CreateLogger("OrderBook");
    Result<OrderPlan> result = await orderBookService.CreateSellPlan(amountOfBtc, cancellationToken);
    if (!result.Successful ||
        result.Value is null)
    {
        logger.LogError("Error while getting crypto exchanges. Message: {message}", result.ErrorMessage);
        return Results.BadRequest("Failed to create sell order plan.");
    }

    OrderPlan orderPlan = result.Value;

    if (orderPlan.OrderPlanDetails.Length != 0)
    {
        return Results.Ok(orderPlan);
    }

    logger.LogWarning("Order plan could not be created.");
    return Results.NotFound("Order plan could not be created.");

});

app.MapPost(ApiRoutes.OrderBook.ExecuteOrderPlan, async (
    OrderPlan orderPlan,
    IOrderBookService orderBookService,
    ILoggerFactory loggerFactory,
    CancellationToken cancellationToken) =>
{
    ILogger logger = loggerFactory.CreateLogger("OrderBook");
    Result<OrderPlan> result = await orderBookService.ExecuteOrderPlan(orderPlan, cancellationToken);

    if (result.Successful)
    {
        return Results.Ok();
    }

    logger.LogWarning("Order plan execution failed.");
    return Results.BadRequest("Order plan execution failed: " + result.ErrorMessage);

});

app.Run();
