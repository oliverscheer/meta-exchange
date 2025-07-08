using System.Globalization;
using System.Text;
using MetaExchange.Shared.Models;
using MetaExchange.Shared.Services;
using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace MetaExchange.Console;

public class MetaExchangeConsole
{
    private readonly ILogger<MetaExchangeConsole> _logger;
    private readonly IOrderBookService _orderBookService;

    static readonly CultureInfo s_culture = CultureInfo.GetCultureInfo("de-DE");

    public MetaExchangeConsole(ILogger<MetaExchangeConsole> logger,
        IOrderBookService orderBookService)
    {
        _logger = logger;
        _orderBookService = orderBookService;
    }

    public async Task Run()
    {
        System.Console.OutputEncoding = Encoding.UTF8;

        _logger.LogInformation("Console App started.");

        CancellationTokenSource cts = new();
        CancellationToken token = cts.Token;

        bool exitRequested = false;

        Result<CryptoExchange[]> cryptoExchangeResult = await _orderBookService.GetCryptoExchanges(token);

        while (!exitRequested)
        {
            PrintHeader();

            if (cryptoExchangeResult.Value is null ||
                cryptoExchangeResult.Value.Length == 0 ||
                !cryptoExchangeResult.Successful
                )
            {
                AnsiConsole.MarkupLine("[red]Error: No Data available[/]");
                PressAnyKeyToContinue();
                continue;
            }
            CryptoExchange[] cryptoExchanges = cryptoExchangeResult.Value;

            PrintCryptoExchangesTable(cryptoExchanges);

            string choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Choose an option:")
                .PageSize(10)
                .AddChoices("Buy", "Sell", "Exit"));

            switch (choice)
            {
                case "Buy":
                    await ShowBuyOrSellMenu(OrderType.Buy, token);
                    break;

                case "Sell":
                    await ShowBuyOrSellMenu(OrderType.Sell, token);
                    break;

                case "Exit":
                    AnsiConsole.MarkupLine("Exiting...");
                    exitRequested = true;
                    break;
            }
        }
    }

    private static void PressAnyKeyToContinue()
    {
        AnsiConsole.MarkupLine("[grey]Press any key to continue...[/]");
        System.Console.ReadKey(true);
    }

    private static void PrintHeader()
    {
        AnsiConsole.Clear();
        AnsiConsole.Write(
            new FigletText("Meta Exchange")
                .Color(Color.Green));
        AnsiConsole.MarkupLine("[blue]By oliver.scheer@adesso.de[/]", new Style(Color.Red));
    }

    private async Task ShowBuyOrSellMenu(OrderType orderType, CancellationToken cancellationToken)
    {
        string question = orderType == OrderType.Buy
            ? "How many BTC do you want to buy?"
            : "How many BTC do you want to sell?";

        decimal amount = AnsiConsole.Prompt(
            new TextPrompt<decimal>($"[green]{question}[/]")
                .PromptStyle("green")
                .ValidationErrorMessage("[red]Please enter valid number[/]")
                .Validate(x => x > 0));

        string feedback = orderType == OrderType.Buy
            ? $"You want to buy {amount} BTC."
            : $"You want to sell {amount} BTC.";
        AnsiConsole.MarkupLine($"[green]{feedback}[/]");

        Result<OrderPlan> orderPlanResult;
        if (orderType == OrderType.Buy)
        {
            orderPlanResult = await _orderBookService.CreateBuyPlan(amount, cancellationToken);
        }
        else
        {
            orderPlanResult = await _orderBookService.CreateSellPlan(amount, cancellationToken);
        }

        if (!orderPlanResult.Successful ||
            orderPlanResult.Value is null)
        {
            AnsiConsole.MarkupLine("[red]Failed to create order plan.[/]");
            PressAnyKeyToContinue();
            return;
        }

        OrderPlan orderPlan = orderPlanResult.Value;

        ShowOrderPlanTable(orderType, orderPlanResult);

        bool execute = AnsiConsole.Prompt(
            new ConfirmationPrompt("Do you want to execute order plan?"));
        AnsiConsole.MarkupLine(execute ? "[green]Confirmed[/]" : "[red]Canceled[/]");

        if (execute)
        {
            Result<OrderPlan> executeOrderPlanResult = await _orderBookService.ExecuteOrderPlan(orderPlan, cancellationToken);
            if (executeOrderPlanResult.Successful)
            {
                AnsiConsole.MarkupLine("[green]Order plan executed successfully![/]");
            }
            else
            {
                AnsiConsole.MarkupLine("[red]Failed to execute order plan.[/]");
                PressAnyKeyToContinue();
            }
        }
    }

    private static void ShowOrderPlanTable(OrderType orderType, Result<OrderPlan> orderPlanResult)
    {
        AnsiConsole.MarkupLine(
            orderType == OrderType.Buy
                ? "[green]Order Plan for Buying BTC[/]"
                : "[red]Order Plan for Selling BTC[/]");

        if (orderPlanResult.Value is null ||
            orderPlanResult.Value.OrderPlanDetails.Length == 0)
        {
            AnsiConsole.MarkupLine("[red]No orders found for this plan.[/]");
            return;
        }

        OrderPlan orderPlan = orderPlanResult.Value;

        Table table = new();

        table.Border(TableBorder.Rounded);
        table.AddColumn(new TableColumn("No").RightAligned());
        table.AddColumn("Exchange");
        table.AddColumn("Order Kind");
        table.AddColumn(("Time"));
        table.AddColumn(new TableColumn("Amount").RightAligned());
        table.AddColumn(new TableColumn("Price").RightAligned());
        table.AddColumn(new TableColumn("Sum").RightAligned());

        int orderNo = 0;

        if (!orderPlanResult.Successful)
        {
            AnsiConsole.MarkupLine($"[red]Error: {orderPlanResult.ErrorMessage}[/]");
            PressAnyKeyToContinue();
            return;
        }

        foreach (string waring in orderPlanResult.Warnings)
        {
            AnsiConsole.MarkupLine($"[yellow]Warning: {waring}[/]");
        }

        foreach (OrderPlanDetail orderPlanDetail in orderPlan.OrderPlanDetails)
        {
            decimal cost = orderPlanDetail.Order.Price * orderPlanDetail.Amount;
            table.AddRow(
                (orderNo++).ToString(),
                orderPlanDetail.CryptoExchangeId,
                orderPlanDetail.Order.Kind.ToString(),
                $"{orderPlanDetail.Order.Time.ToShortTimeString()} {orderPlanDetail.Order.Time.ToShortDateString()}",
                $"{orderPlanDetail.Amount}",
                $"{orderPlanDetail.Order.Price.ToString("C", s_culture)}",
                $"{cost:C}"
                );
        }
        table.AddRow("-", "-", "-", "-", "-");
        table.AddRow(
            "Sum",
            "",
            "",
            "",
            $"{orderPlan.TotalAmount}",
            "",
            $"{orderPlan.TotalPrice.ToString("C", s_culture)}");

        AnsiConsole.Write(table);
    }

    private static void PrintCryptoExchangesTable(CryptoExchange[] cryptoExchanges)
    {
        Table table = new();

        table.Border(TableBorder.Rounded);
        table.AddColumn("Exchange Id");
        table.AddColumn(new TableColumn("Euro").RightAligned());
        table.AddColumn(new TableColumn("BTC").RightAligned());
        table.AddColumn(new TableColumn("Asks").RightAligned());
        table.AddColumn(new TableColumn("Bids").RightAligned());

        decimal totalEuro = cryptoExchanges.Sum(exchange => exchange.AvailableFunds.Euro);
        decimal totalCrypto = cryptoExchanges.Sum(exchange => exchange.AvailableFunds.Crypto);
        int totalAsks = cryptoExchanges.Sum(exchange => exchange.OrderBook.Asks.Length);
        int totalBids = cryptoExchanges.Sum(exchange => exchange.OrderBook.Bids.Length);

        foreach (CryptoExchange cryptoExchange in cryptoExchanges)
        {
            table.AddRow(
                cryptoExchange.Id,
                $"{cryptoExchange.AvailableFunds.Euro.ToString("C", s_culture)}",
                $"{cryptoExchange.AvailableFunds.Crypto}",
                $"{cryptoExchange.OrderBook.Asks.Length}",
                $"{cryptoExchange.OrderBook.Bids.Length}"
                );
        }
        table.AddRow("--", "--", "--", "--", "--");
        table.AddRow(
            "Total",
            $"{totalEuro.ToString("C", s_culture)}",
            $"{totalCrypto}",
            $"{totalAsks}",
            $"{totalBids}");

        AnsiConsole.Write(table);
    }
}
