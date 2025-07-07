using MetaExchange.Shared.Models;
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
        // 
        // Arrange
        //

        Mock<ILogger<FileBasedExchangeService>> mockExchangeServiceLogger = new();
        FileBasedExchangeService exchangeService = new(mockExchangeServiceLogger.Object);

        Mock<ILogger<OrderBookService>> mockOrderBookServiceLogger = new();
        OrderBookService orderBookService = new(exchangeService, mockOrderBookServiceLogger.Object);

        // Act
        // nothing

        // Assert
        Result<CryptoExchange[]> cryptoExchanges = await orderBookService.GetCryptoExchanges();
        Assert.NotNull(cryptoExchanges);
        Assert.True(cryptoExchanges.Value!.Length > 0);
    }

    [Fact]
    public async Task Buy_Three_BTC_From_FilebasedExchanges()
    {
        // Arrange
        Mock<ILogger<FileBasedExchangeService>> mockExchangeServiceLogger = new();
        FileBasedExchangeService exchangeService = new(mockExchangeServiceLogger.Object);

        Mock<ILogger<OrderBookService>> mockOrderBookServiceLogger = new();
        OrderBookService orderBookService = new(exchangeService, mockOrderBookServiceLogger.Object);

        // Act
        decimal amountOfBtc = 3.00M;

        Result<OrderPlan> orderPlanresult = await orderBookService.CreateBuyPlan(amountOfBtc);
        OrderPlan orderPlan = orderPlanresult.Value!;
        
        Result<OrderPlan> executeOrderPlanResult = await orderBookService.ExecuteOrderPlan(orderPlan);

        // Assert
        Assert.True(executeOrderPlanResult.Successful, executeOrderPlanResult.ErrorMessage);
        Assert.Equal(amountOfBtc, orderPlan.TotalAmount);
        Assert.True(orderPlan.TotalPrice > 0);

        Result<CryptoExchange[]> cryptoExchangeResult = await orderBookService.GetCryptoExchanges();
        foreach (OrderPlanDetail opd in orderPlan.OrderPlanDetails)
        {
            CryptoExchange? cryptoExchange = cryptoExchangeResult
                .Value!
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
    public async Task I_Buy_Three_BTC()
    {
        // 
        // Arrange
        //

        // Create a mock exchange service with 3000 Euro and 10 BTC
        TestExchangeService exchangeService = new(
            3000.0M, // Euro 
            0.0M     // BTC
            );

        Mock<ILogger<OrderBookService>> mockOrderBookServiceLogger = new();
        OrderBookService orderBookService = new(exchangeService, mockOrderBookServiceLogger.Object);

        // 
        // Act
        //

        // Add ask to sell 3 BTC 
        Result<CryptoExchange[]> cryptoExchangeResult = await orderBookService.GetCryptoExchanges();
        orderBookService.AddAsk(
            cryptoExchangeResult.Value![0],
            new Order()
            {
                Id = "New Ask Order - 1",
                Amount = 3.00M,
                Price = 1000,
                Type = OrderType.Sell,
                Time = DateTime.Now,
                Kind = OrderKind.Limit
            });

        // Create an order plan to buy 3 BTC
        decimal amountOfBtc = 3.00M;
        Result<OrderPlan> orderPlanresult = await orderBookService.CreateBuyPlan(amountOfBtc);
        OrderPlan orderPlan = orderPlanresult.Value!;
        Assert.NotNull(orderPlan);

        Assert.True(orderPlan.OrderPlanDetails.Length > 0, "Order plan should have at least one order detail.");

        // Execute Order Plan
        Result<OrderPlan> executeOrderPlanResult = await orderBookService.ExecuteOrderPlan(orderPlan);
        Assert.True(executeOrderPlanResult.Successful, executeOrderPlanResult.ErrorMessage);

        // Assert
        Assert.Equal(amountOfBtc, orderPlan.TotalAmount);
        Assert.True(orderPlan.TotalPrice > 0);

        foreach (OrderPlanDetail opd in orderPlan.OrderPlanDetails)
        {
            CryptoExchange? cryptoExchange = cryptoExchangeResult
                .Value!
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
        //Mock<ILogger<FileBasedExchangeService>> mockExchangeServiceLogger = new();
        //FileBasedExchangeService exchangeService = new(mockExchangeServiceLogger.Object);

        // Create an exchange
        // - with 10 btc
        decimal initEuro = 0.00M;
        decimal initCrypto = 10.00M;
        IExchangeService exchangeService = new TestExchangeService(initEuro, initCrypto);

        Mock<ILogger<OrderBookService>> mockOrderBookServiceLogger = new();
        OrderBookService orderBookService = new(exchangeService, mockOrderBookServiceLogger.Object);

        // Add ask order for 10 btc
        Result<CryptoExchange[]> cryptoExchangeResult = await orderBookService.GetCryptoExchanges();
        orderBookService.AddBid(
            cryptoExchangeResult.Value![0],
            new Order()
            {
                Id = "New Bid Order 1",
                Amount = 10.00M,
                Price = 10000,
                Type = OrderType.Sell,
                Time = DateTime.Now,
                Kind = OrderKind.Limit
            });

        // Act
        decimal amountOfBtc = 10.00M;

        Result<OrderPlan> orderPlanResult = await orderBookService.CreateSellPlan(amountOfBtc);
        OrderPlan orderPlan = orderPlanResult.Value;

        Assert.True(orderPlan.OrderPlanDetails.Length > 0);
        Result<OrderPlan> executeOrderPlanResult = await orderBookService.ExecuteOrderPlan(orderPlan);

        // Assert
        Assert.True(executeOrderPlanResult.Successful, executeOrderPlanResult.ErrorMessage);
        Assert.Equal(amountOfBtc, orderPlan.TotalAmount);
        Assert.True(orderPlan.TotalPrice > 0);

        foreach (OrderPlanDetail opd in orderPlan.OrderPlanDetails)
        {
            CryptoExchange? cryptoExchange = cryptoExchangeResult
                .Value!
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
    public async Task Sell_One_BTC_Having_Enough_BTC()
    {
        // Arrange

        // Create an exchange 
        decimal initEuro = 0M;
        decimal initCrypto = 1M;
        IExchangeService exchangeService = new TestExchangeService(initEuro, initCrypto);

        // Create orderBookService
        Mock<ILogger<OrderBookService>> mockOrderBookServiceLogger = new();
        OrderBookService orderBookService = new(exchangeService, mockOrderBookServiceLogger.Object);

        Result<CryptoExchange[]> cryptoExchangeResult = await orderBookService.GetCryptoExchanges();

        // Create ask to buy btc
        orderBookService.AddBid(
            cryptoExchangeResult.Value![0],
            new Order()
            {
                Id = "New Bid Order 1",
                Amount = 1.50M,
                Price = 5000,
                Type = OrderType.Buy,
                Time = DateTime.Now,
                Kind = OrderKind.Limit
            });

        // Act
        Result<OrderPlan> orderPlanResult = await orderBookService.CreateSellPlan(1.00M);
        OrderPlan orderPlan = orderPlanResult.Value;

        Result<OrderPlan> executeOrderPlanResult = await orderBookService.ExecuteOrderPlan(orderPlan);

        // Assert
        Assert.NotNull(orderBookService.GetCryptoExchanges());
        Assert.True(cryptoExchangeResult.Value!.Length > 0);
        Assert.True(orderPlan.OrderPlanDetails.Length > 0, "Order plan should have at least one order detail.");
        Assert.True(executeOrderPlanResult.Successful, executeOrderPlanResult.ErrorMessage);
    }

    [Fact]
    public async Task I_Sell_One_BTC_To_An_Ask_Of_One()
    {
        // Arrange

        // Create an exchange 
        decimal initEuro = 0M;
        decimal initCrypto = 1M;
        IExchangeService exchangeService = new TestExchangeService(initEuro, initCrypto);

        // Create orderBookService
        Mock<ILogger<OrderBookService>> mockOrderBookServiceLogger = new();
        OrderBookService orderBookService = new(exchangeService, mockOrderBookServiceLogger.Object);

        Result<CryptoExchange[]> cryptoExchangeResult = await orderBookService.GetCryptoExchanges();

        // Create Asks in orderbook to Sell btc
        orderBookService.AddBid(
            cryptoExchangeResult.Value![0],
            new Order()
            {
                Id = "New Ask Order 1",
                Amount = 1.00M,
                Price = 15000,
                Type = OrderType.Sell,
                Time = DateTime.Now,
                Kind = OrderKind.Limit
            });

        // Act
        Result<OrderPlan> orderPlanResult = await orderBookService.CreateSellPlan(initCrypto);
        OrderPlan orderPlan = orderPlanResult.Value;

        Result<OrderPlan> executionResult = await orderBookService.ExecuteOrderPlan(orderPlan);

        // Assert
        Assert.NotNull(orderBookService.GetCryptoExchanges());
        Assert.True(cryptoExchangeResult.Value!.Length > 0);
        Assert.True(orderPlan.OrderPlanDetails.Length > 0, "Order plan should have at least one order detail.");
        Assert.True(executionResult.Successful);
        Assert.Empty(cryptoExchangeResult.Value![0].OrderBook.Asks);
    }

    [Fact]
    public async Task I_Sell_One_BTC_To_An_Ask_Of_Two()
    {
        // Arrange

        // Create an exchange 
        decimal initEuro = 0M;
        decimal initCrypto = 1M;
        IExchangeService exchangeService = new TestExchangeService(initEuro, initCrypto);

        // Create orderBookService
        Mock<ILogger<OrderBookService>> mockOrderBookServiceLogger = new();
        OrderBookService orderBookService = new(exchangeService, mockOrderBookServiceLogger.Object);

        Result<CryptoExchange[]> cryptoExchangeResult = await orderBookService.GetCryptoExchanges();

        // Create Asks in orderbook to Sell btc
        orderBookService.AddBid(
            cryptoExchangeResult.Value![0],
            new Order()
            {
                Id = "New Ask Order 1",
                Amount = initCrypto * 2,
                Price = 15000,
                Type = OrderType.Sell,
                Time = DateTime.Now,
                Kind = OrderKind.Limit
            });

        // Act
        Result<OrderPlan> orderPlanResult = await orderBookService.CreateSellPlan(initCrypto);
        Assert.True(orderPlanResult.Successful);
        OrderPlan orderPlan = orderPlanResult.Value;

        Result<OrderPlan> executionResult = await orderBookService.ExecuteOrderPlan(orderPlan);

        // Assert
        Assert.NotNull(orderBookService.GetCryptoExchanges());
        Assert.True(cryptoExchangeResult.Value!.Length > 0);
        Assert.True(orderPlan.OrderPlanDetails.Length > 0, "Order plan should have at least one order detail.");
        Assert.True(executionResult.Successful);

        Assert.True(cryptoExchangeResult.Value![0].OrderBook.Bids.Length == 1, "On bid should be still there but reduced");

    }

    [Fact]
    public async Task Buy_One_BTC_Having_Enough_Euro()
    {
        // 
        // Arrange
        //

        // Create an exchange 
        decimal initEuro = 10000.0M;
        decimal initCrypto = 0M;
        IExchangeService exchangeService = new TestExchangeService(initEuro, initCrypto);

        // Create orderBookService
        Mock<ILogger<OrderBookService>> mockOrderBookServiceLogger = new();
        OrderBookService orderBookService = new(exchangeService, mockOrderBookServiceLogger.Object);

        Result<CryptoExchange[]> cryptoExchangeResult = await orderBookService.GetCryptoExchanges();

        // 
        // Act
        // 

        // Create bid in orderbook to sell 1 btc
        decimal bidPrice = initEuro;
        orderBookService.AddAsk(
            cryptoExchangeResult.Value![0],
            new Order()
            {
                Id = "New Ask Order - 1",
                Amount = 1.00M,
                Price = bidPrice,
                Type = OrderType.Sell,
                Time = DateTime.Now,
                Kind = OrderKind.Limit
            });

        // Assert
        Assert.NotNull(orderBookService.GetCryptoExchanges());
        Assert.True(cryptoExchangeResult.Value!.Length > 0);
        Assert.True(cryptoExchangeResult.Value![0].OrderBook.Asks.Length == 1, 
            "Order book should have at least one bid order.");

        // Act

        // Sell 1 btc for 10000 Euro
        Result<OrderPlan> orderPlanResult = await orderBookService.CreateBuyPlan(1.00M);
        OrderPlan orderPlan = orderPlanResult.Value;

        Result <OrderPlan> executionResult = await orderBookService.ExecuteOrderPlan(orderPlan);

        // Assert
        Assert.NotNull(orderBookService.GetCryptoExchanges());
        Assert.True(cryptoExchangeResult.Value!.Length > 0);
        Assert.True(orderPlan.OrderPlanDetails.Length > 0, "Order plan should have at least one order detail.");
        Assert.True(executionResult.Successful, executionResult.ErrorMessage);
        Assert.True(cryptoExchangeResult.Value![0].OrderBook.Bids.Length == 0,
            "Order book should have no bid orders after execution.");
    }

    [Fact]
    public async Task Buy_One_BTC_Having_Not_Enough_Euro()
    {
        // 
        // Arrange
        //

        // Create an exchange 
        decimal initEuro = 10000.0M;
        decimal initCrypto = 0M;
        IExchangeService exchangeService = new TestExchangeService(initEuro, initCrypto);

        // Create orderBookService
        Mock<ILogger<OrderBookService>> mockOrderBookServiceLogger = new();
        OrderBookService orderBookService = new(exchangeService, mockOrderBookServiceLogger.Object);

        Result<CryptoExchange[]> cryptoExchangeResult = await orderBookService.GetCryptoExchanges();

        // 
        // Act
        // 

        // Create bid in orderbook to sell 1 btc
        decimal bidPrice = initEuro * 2;
        orderBookService.AddBid(
            cryptoExchangeResult.Value![0],
            new Order()
            {
                Id = "New Bid Order - 1",
                Amount = 1.00M,
                Price = bidPrice,
                Type = OrderType.Buy,
                Time = DateTime.Now,
                Kind = OrderKind.Limit
            });

        // Assert
        Assert.NotNull(orderBookService.GetCryptoExchanges());
        Assert.True(cryptoExchangeResult.Value!.Length > 0);
        Assert.True(cryptoExchangeResult.Value![0].OrderBook.Bids.Length == 1,
            "Order book should have at least one bid order.");
        Assert.Empty(cryptoExchangeResult.Value![0].OrderBook.Asks);

        // Act

        // Sell 1 btc for 10000 Euro
        Result<OrderPlan> orderPlanResult = await orderBookService.CreateBuyPlan(1.00M);
        OrderPlan orderPlan = orderPlanResult.Value;
        Assert.Empty(orderPlan.OrderPlanDetails);

        Result<OrderPlan> executionResult = await orderBookService.ExecuteOrderPlan(orderPlan);

        // Assert
        Assert.NotNull(orderBookService.GetCryptoExchanges());
        Assert.True(cryptoExchangeResult.Value!.Length > 0);
        Assert.False(executionResult.Successful, executionResult.ErrorMessage);
        Assert.True(cryptoExchangeResult.Value![0].OrderBook.Bids.Length == 1,
            "Order book should have no bid orders after execution.");
    }

    [Fact]
    public async Task Try_To_Buy_More_Than_I_Can_Afford()
    {
        // 
        // Arrange
        //

        // Create exchange 
        decimal initEuro = 10000.0M;
        decimal initCrypto = 5.0M;
        IExchangeService exchangeService = new TestExchangeService(initEuro, initCrypto);

        decimal bidPrice = initEuro;

        // Create orderBookService
        Mock<ILogger<OrderBookService>> mockOrderBookServiceLogger = new();
        OrderBookService orderBookService = new(exchangeService, mockOrderBookServiceLogger.Object);

        Result<CryptoExchange[]> cryptoExchangeResult = await orderBookService.GetCryptoExchanges();

        // 
        // Act
        //

        // Create bid order in orderbook to buy 10 btc
        Assert.NotNull(cryptoExchangeResult);
        orderBookService.AddAsk(
            cryptoExchangeResult.Value![0],
            new Order()
            {
                Id = "New Ask Order - 1",
                Amount = 3000.00M,
                Price = bidPrice,
                Type = OrderType.Buy,
                Time = DateTime.Now,
                Kind = OrderKind.Limit
            });

        Result<OrderPlan> orderPlanResult = await orderBookService.CreateBuyPlan(10.00M);
        OrderPlan orderPlan = orderPlanResult.Value;

        Result<OrderPlan> executionResult = await orderBookService.ExecuteOrderPlan(orderPlan);

        // 
        // Assert
        //

        Assert.False(executionResult.Successful, "Should fail because of not enough Euro.");
        Assert.NotNull(orderBookService.GetCryptoExchanges());
        Assert.True(cryptoExchangeResult.Value!.Length >= 0);
        Assert.True(cryptoExchangeResult.Value![0].AvailableFunds.Crypto >= 0);
    }
}

