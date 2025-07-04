using MetaExchange.Shared.Models;
using MetaExchange.Shared.Models.Results;
using MetaExchange.Shared.Services;
using MetaExchange.Tests.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace MetaExchange.Tests;
public class OrderBookServiceTests
{
    [Fact]
    public async Task Load_Existing_Exchanges()
    {
        // Arrange
        Mock<ILogger<FileBasedExchangeService>> mockExchangeServiceLogger = new();
        FileBasedExchangeService exchangeService = new(mockExchangeServiceLogger.Object);

        Mock<ILogger<OrderBookService>> mockOrderBookServiceLogger = new();
        OrderBookService orderBookService = new(exchangeService, mockOrderBookServiceLogger.Object);

        // Act
        // nothing

        // Assert
        CryptoExchangesResult cryptoExchanges = await orderBookService.GetCryptoExchanges();
        Assert.NotNull(cryptoExchanges);
        Assert.True(cryptoExchanges.CryptoExchanges.Length > 0);
    }

    [Fact]
    public async Task Buy_Three_BTC()
    {
        // Arrange
        Mock<ILogger<FileBasedExchangeService>> mockExchangeServiceLogger = new();
        FileBasedExchangeService exchangeService = new(mockExchangeServiceLogger.Object);

        Mock<ILogger<OrderBookService>> mockOrderBookServiceLogger = new();
        OrderBookService orderBookService = new(exchangeService, mockOrderBookServiceLogger.Object);

        // Act
        decimal amountOfBtc = 3.00M; 
        OrderPlan orderPlan = await orderBookService.CreateBuyPlan(amountOfBtc);

        ExecuteOrderPlanResult executeOrderPlanResult = await orderBookService.ExecuteOrderPlan(orderPlan);

        // Assert
        Assert.True(executeOrderPlanResult.Successful, executeOrderPlanResult.ErrorMessage);
        Assert.Equal(amountOfBtc, orderPlan.TotalAmount);
        Assert.True(orderPlan.TotalPrice > 0);

        CryptoExchangesResult cryptoExchangeResult = await orderBookService.GetCryptoExchanges();
        foreach (OrderPlanDetail opd in orderPlan.OrderPlanDetails)
        {
            CryptoExchange? cryptoExchange = cryptoExchangeResult
                .CryptoExchanges
                .FirstOrDefault(exchange => exchange.Id == opd.CryptoExchangeId);

            Assert.NotNull(cryptoExchange);
            Assert.True(cryptoExchange.AvailableFunds.Crypto >= 0,
                $"Crypto exchange {cryptoExchange.Id} should have 0 or more BTC.");
            Assert.True(cryptoExchange.AvailableFunds.Euro >= 0,
                $"Crypto exchange {cryptoExchange.Id} should have 0 or more Euro.");
            Assert.True(opd.Amount > 0, "Order amount should be greater than zero.");
            Assert.True(opd.Order.Price > 0, "Order price should be greater than zero.");
        }
    }

    [Fact]
    public async Task Sell_Ten_BTC()
    {
        // Arrange
        Mock<ILogger<FileBasedExchangeService>> mockExchangeServiceLogger = new();
        FileBasedExchangeService exchangeService = new(mockExchangeServiceLogger.Object);

        Mock<ILogger<OrderBookService>> mockOrderBookServiceLogger = new();
        OrderBookService orderBookService = new(exchangeService, mockOrderBookServiceLogger.Object);

        // Act
        decimal amountOfBtc = 10.00M;
        OrderPlan orderPlan = await orderBookService.CreateSellPlan(amountOfBtc);

        ExecuteOrderPlanResult executeOrderPlanResult = await orderBookService.ExecuteOrderPlan(orderPlan);

        // Assert
        Assert.True(executeOrderPlanResult.Successful, executeOrderPlanResult.ErrorMessage);
        Assert.Equal(amountOfBtc, orderPlan.TotalAmount);
        Assert.True(orderPlan.TotalPrice > 0);

        CryptoExchangesResult cryptoExchangeResult = await orderBookService.GetCryptoExchanges();

        foreach (OrderPlanDetail opd in orderPlan.OrderPlanDetails)
        {
            CryptoExchange? cryptoExchange = cryptoExchangeResult
                .CryptoExchanges
                .FirstOrDefault(exchange => exchange.Id == opd.CryptoExchangeId);

            Assert.NotNull(cryptoExchange);
            Assert.True(cryptoExchange.AvailableFunds.Crypto >= 0,
                $"Crypto exchange {cryptoExchange.Id} should have 0 or more BTC.");
            Assert.True(cryptoExchange.AvailableFunds.Euro >= 0,
                $"Crypto exchange {cryptoExchange.Id} should have 0 or more Euro.");
            Assert.True(opd.Amount > 0, "Order amount should be greater than zero.");
            Assert.True(opd.Order.Price > 0, "Order price should be greater than zero.");
        }
    }

    [Fact]
    public async Task Sell_One_BTC_On_Sample_Exchange()
    {
        // Arrange

        // Create an exchange 
        decimal initEuro = 1000M;
        decimal initCrypto = 10M;
        IExchangeService exchangeService = new TestExchangeService(initEuro, initCrypto);

        // Create orderBookService
        Mock<ILogger<OrderBookService>> mockOrderBookServiceLogger = new();
        OrderBookService orderBookService = new(exchangeService, mockOrderBookServiceLogger.Object);

        CryptoExchangesResult cryptoExchangeResult = await orderBookService.GetCryptoExchanges();

        // Create ask to buy btc
        orderBookService.AddAsk(
            cryptoExchangeResult.CryptoExchanges[0],
            new Order()
            {
                Id = "New Ask Order 1",
                Amount = 1.50M,
                Price = 5000,
                Type = OrderType.Buy,
                Time = DateTime.Now,
                Kind = OrderKind.Limit
            });

        // Act
        OrderPlan orderPlan = await orderBookService.CreateSellPlan(1.00M);

        ExecuteOrderPlanResult executeOrderPlanResult = await orderBookService.ExecuteOrderPlan(orderPlan);

        // Assert
        Assert.NotNull(orderBookService.GetCryptoExchanges());
        Assert.True(cryptoExchangeResult.CryptoExchanges.Length > 0);
        Assert.True(orderPlan.OrderPlanDetails.Length > 0, "Order plan should have at least one order detail.");
        Assert.True(executeOrderPlanResult.Successful, executeOrderPlanResult.ErrorMessage);
    }

    [Fact]
    public async Task Add_Ask_and_Sell_To_Remove_Ask_Complete_Test()
    {
        // Arrange

        // Create an exchange 
        decimal initEuro = 300000M;
        decimal initCrypto = 10M;
        IExchangeService exchangeService = new TestExchangeService(initEuro, initCrypto);

        // Create orderBookService
        Mock<ILogger<OrderBookService>> mockOrderBookServiceLogger = new();
        OrderBookService orderBookService = new(exchangeService, mockOrderBookServiceLogger.Object);

        CryptoExchangesResult cryptoExchangeResult = await orderBookService.GetCryptoExchanges();

        // Create Asks in orderbook to Sell btc
        orderBookService.AddAsk(
            cryptoExchangeResult.CryptoExchanges[0],
            new Order()
            {
                Id = "New Ask Order 1",
                Amount = 1.50M,
                Price = 15000,
                Type = OrderType.Sell,
                Time = DateTime.Now,
                Kind = OrderKind.Limit
            });

        // Act
        OrderPlan orderPlan = await orderBookService.CreateSellPlan(1.50M);
        ExecuteOrderPlanResult executionResult = await orderBookService.ExecuteOrderPlan(orderPlan);

        // Assert
        Assert.NotNull(orderBookService.GetCryptoExchanges());
        Assert.True(cryptoExchangeResult.CryptoExchanges.Length > 0);
        Assert.True(orderPlan.OrderPlanDetails.Length > 0, "Order plan should have at least one order detail.");
        Assert.True(executionResult.Successful);
        Assert.True(cryptoExchangeResult.CryptoExchanges[0].OrderBook.Asks.Length == 0);
    }
}
