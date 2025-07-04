using MetaExchange.Console;
using MetaExchange.Shared.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((_, services) =>
    {
        services.AddSingleton<IExchangeService, FileBasedExchangeService>();
        services.AddSingleton<IOrderBookService, OrderBookService>();
        services.AddSingleton<MetaExchangeConsole>();
    })
    .ConfigureLogging(logging =>
    {
        logging.ClearProviders();
        logging.AddConsole();
        logging.SetMinimumLevel(LogLevel.Error); // oder LogLevel.Information
    })
    .Build();

MetaExchangeConsole app = host.Services.GetRequiredService<MetaExchangeConsole>();
app.Run();
