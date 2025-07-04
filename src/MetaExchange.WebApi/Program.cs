using MetaExchange.Shared.Services;
using Scalar.AspNetCore;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddSingleton<IExchangeService, FileBasedExchangeService>();
builder.Services.AddSingleton<IOrderBookService, OrderBookService>();

WebApplication app = builder.Build();

// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
//{
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
            logger.LogInformation($"Start scalar on https://localhost:{httpsPort}/scalar");
        }
        if (httpPort is not null)
        {
            logger.LogInformation($"Start scalar on http://localhost:{httpPort}/scalar");
        }
    });
//}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
