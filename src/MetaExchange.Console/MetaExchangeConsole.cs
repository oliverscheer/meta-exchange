using System.Globalization;
using System.Text;
using MetaExchange.Shared.Models;
using MetaExchange.Shared.Models.Results;
using MetaExchange.Shared.Services;
using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace MetaExchange.Console;

public class MetaExchangeConsole
{
    private readonly ILogger<MetaExchangeConsole> _logger;
    private readonly IOrderBookService _orderBookService;

    readonly static CultureInfo s_culture = CultureInfo.GetCultureInfo("de-DE");

    public MetaExchangeConsole(ILogger<MetaExchangeConsole> logger,
        IExchangeService exchangeService,
        IOrderBookService orderBookService)
    {
        _logger = logger;
        _orderBookService = orderBookService;
    }

    public async Task Run()
    {
        System.Console.OutputEncoding = Encoding.UTF8;

        _logger.LogInformation("Console App started.");

        bool exitRequested = false;
        while (!exitRequested)
        {
            PrintHeader();
            CryptoExchangesResult cryptoExchangeResult = await _orderBookService.GetCryptoExchanges();
            PrintCryptoExchangesTable(cryptoExchangeResult.CryptoExchanges);

            string choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Choose an option:")
                    .PageSize(10)
                    .AddChoices(["Buy", "Sell", "Exit"]));

            switch (choice)
            {
                case "Buy":
                    await ShowBuyOrSellMenu(OrderType.Buy);
                    break;

                case "Sell":
                    await ShowBuyOrSellMenu(OrderType.Sell);
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

    private async Task ShowBuyOrSellMenu(OrderType orderType)
    {
        string question = orderType == OrderType.Buy
            ? "Do you want to buy more BTC?"
            : "Do you want to sell more BTC?";

        decimal amount = AnsiConsole.Prompt(
            new TextPrompt<decimal>($"[green]{question}[/]")
                .PromptStyle("green")
                .ValidationErrorMessage("[red]Please enter valid number[/]")
                .Validate(x => x > 0));

        string feedback = orderType == OrderType.Buy
            ? $"You want to buy {amount} BTC."
            : $"You want to sell {amount} BTC.";
        AnsiConsole.MarkupLine($"[green]{feedback}[/]");

        OrderPlan orderPlan;
        if (orderType == OrderType.Buy)
        {
            orderPlan = await _orderBookService.CreateBuyPlan(amount);
        }
        else
        {
            orderPlan = await _orderBookService.CreateSellPlan(amount);
        }

        ShowOrderPlanTable(orderType, orderPlan);

        bool execute = AnsiConsole.Prompt(
            new ConfirmationPrompt("Do you want to execute order plan?"));
        AnsiConsole.MarkupLine(execute ? "[green]Confirmed[/]" : "[red]Canceled[/]");

        if (execute)
        {
            ExecuteOrderPlanResult executeOrderPlanResult = await _orderBookService.ExecuteOrderPlan(orderPlan);
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

    private static void ShowOrderPlanTable(OrderType orderType, OrderPlan orderPlan)
    {
        AnsiConsole.MarkupLine(
            orderType == OrderType.Buy
                ? "[green]Order Plan for Buying BTC[/]"
                : "[red]Order Plan for Selling BTC[/]");

        Table table = new();
        
        table.Border(TableBorder.Rounded);
        table.AddColumn(new TableColumn("No").RightAligned());
        table.AddColumn("Exchange");
        table.AddColumn(new TableColumn("Amount").RightAligned());
        table.AddColumn(new TableColumn("Price").RightAligned());
        table.AddColumn(new TableColumn("Sum").RightAligned());

        int orderNo = 0;

        if (orderPlan.OrderPlanDetails.Length == 0)
        {
            AnsiConsole.MarkupLine("[red]No orders found for this plan.[/]");
            return;
        }

        foreach (OrderPlanDetail orderPlanDetail in orderPlan.OrderPlanDetails)
        {
            decimal cost = orderPlanDetail.Order.Price * orderPlanDetail.Amount;
            table.AddRow(
                (orderNo++).ToString(),
                orderPlanDetail.CryptoExchangeId,
                $"{orderPlanDetail.Amount}",
                $"{orderPlanDetail.Order.Price.ToString("C", s_culture)}",
                $"{cost:C}"
                );
        }
        table.AddRow("-", "-", "-", "-", "-");
        table.AddRow(
            "Sum",
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
