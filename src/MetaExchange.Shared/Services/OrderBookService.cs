using MetaExchange.Shared.Models;
using MetaExchange.Shared.Models.Results;
using Microsoft.Extensions.Logging;

namespace MetaExchange.Shared.Services;
public class OrderBookService : IOrderBookService
{
    private readonly IExchangeService _exchangeService;

    private readonly ILogger<OrderBookService> _logger;

    public OrderBookService(IExchangeService exchangeService, ILogger<OrderBookService> logger)
    {
        _exchangeService = exchangeService;
        _logger = logger;
    }

    public async Task<CryptExchangesResult> GetCryptoExchanges()
    {
        return await _exchangeService.GetCryptoExchanges();
    }

    public async Task<OrderPlan> CreateSellPlan(decimal amountOfBtcToBuy)
    {
        _logger.LogInformation($"Buying {amountOfBtcToBuy} BTC from crypto exchanges.");
        OrderPlan orderPlan = new();
        decimal remainingAmountOfBtcToBuy = amountOfBtcToBuy;

        CryptExchangesResult cryptoExchangesResult = await _exchangeService.GetCryptoExchanges();

        List<Ask> asks = [.. cryptoExchangesResult.CryptoExchanges
            .SelectMany(exchange => exchange.OrderBook.Asks)
            .OrderBy(ask => ask.Order.Price)];

        foreach (Ask ask in asks)
        {
            if (remainingAmountOfBtcToBuy <= 0)
            {
                break;
            }

            decimal tradeAmountOfBtc = Math.Min(remainingAmountOfBtcToBuy, ask.Order.Amount);

            CryptoExchange cryptoExchangeWithAsk = cryptoExchangesResult.CryptoExchanges
                    .FirstOrDefault(exchange => exchange.OrderBook.Asks.Contains(ask))
                    ?? throw new InvalidOperationException("Crypto exchange with ask not found.");

            OrderPlanDetail orderPlanDetail = new(
                    cryptoExchangeWithAsk.Id,
                    ask.Order,
                    tradeAmountOfBtc);

            if (!CheckOrder(cryptoExchangeWithAsk, orderPlan, orderPlanDetail))
            {
                continue;
            }

            orderPlan.AddOrderPlanDetail(orderPlanDetail);
            remainingAmountOfBtcToBuy -= tradeAmountOfBtc;
        }
        return orderPlan;
    }

    public async Task<OrderPlan> CreateBuyPlan(decimal amountOfBtcToSell)
    {
        _logger.LogInformation($"Selling {amountOfBtcToSell} BTC from crypto exchanges.");
        OrderPlan orderPlan = new();
        decimal remainingAmountOfBtcToSell = amountOfBtcToSell;

        CryptExchangesResult cryptoExchangesResult = await _exchangeService.GetCryptoExchanges();

        List<Bid> bids = [.. cryptoExchangesResult.CryptoExchanges
            .SelectMany(exchange => exchange.OrderBook.Bids)
            .OrderByDescending(bid => bid.Order.Price)];

        foreach (Bid bid in bids)
        {
            if (remainingAmountOfBtcToSell <= 0)
            {
                break;
            }

            decimal tradeAmountOfBtc = Math.Min(remainingAmountOfBtcToSell, bid.Order.Amount);

            CryptoExchange cryptoExchangeWithBid = cryptoExchangesResult.CryptoExchanges
                    .FirstOrDefault(exchange => exchange.OrderBook.Bids.Contains(bid))
                    ?? throw new InvalidOperationException("Crypto exchange with bid not found.");

            OrderPlanDetail orderPlanDetail = new(
                    cryptoExchangeWithBid.Id,
                    bid.Order,
                    tradeAmountOfBtc);

            if (!CheckOrder(cryptoExchangeWithBid, orderPlan, orderPlanDetail))
            {
                continue;
            }

            orderPlan.AddOrderPlanDetail(orderPlanDetail);
            remainingAmountOfBtcToSell -= tradeAmountOfBtc;
        }
        return orderPlan;
    }

    private bool CheckOrder(CryptoExchange cryptoExchange,
        OrderPlan orderPlan,
        OrderPlanDetail orderPlanDetail)
    {
        decimal estimatedEuroForCryptoExchange = cryptoExchange.AvailableFunds.Euro;
        decimal estimatedCryptoForCryptoExchange = cryptoExchange.AvailableFunds.Crypto;

        foreach (OrderPlanDetail opd in orderPlan.OrderPlanDetails)
        {
            if (opd.CryptoExchangeId == cryptoExchange.Id)
            {
                if (opd.Order.Type == OrderType.Sell)
                {
                    estimatedEuroForCryptoExchange -= opd.Order.Price * opd.Amount;
                    estimatedCryptoForCryptoExchange += opd.Amount;
                }
                else
                {
                    estimatedEuroForCryptoExchange += opd.Order.Price * opd.Amount;
                    estimatedCryptoForCryptoExchange -= opd.Amount;
                }
            }
        }

        if (orderPlanDetail.Order.Type == OrderType.Sell)
        {
            // sell checks
            if (estimatedCryptoForCryptoExchange < orderPlanDetail.Amount)
            {
                _logger.LogWarning($"Crypto exchange {cryptoExchange.Id} does not have enough BTC to fulfill the order.");
                return false;
            }

            decimal price = orderPlanDetail.Order.Price * orderPlanDetail.Amount;
            if (estimatedEuroForCryptoExchange < price)
            {
                _logger.LogWarning($"Crypto exchange {cryptoExchange.Id} does not have enough Euro to fulfill the order.");
                return false;
            }
        }
        else if (orderPlanDetail.Order.Type == OrderType.Buy)
        {
            // buy checks
            if (estimatedCryptoForCryptoExchange < orderPlanDetail.Amount)
            {
                _logger.LogWarning($"Crypto exchange {cryptoExchange.Id} does not have enough BTC to fulfill the order.");
                return false;
            }

            //decimal price = orderPlanDetail.Order.Price * orderPlanDetail.Amount;
            //if (estimatedEuroForCryptoExchange < price)
            //{
            //    _logger.LogWarning($"Crypto exchange {cryptoExchange.Id} does not have enough Euro to fulfill the order.");
            //    return false;
            //}
        }
        else
        {
            throw new Exception($"Unknown order type: {orderPlanDetail.Order.Type}");
        }

        return true;
    } 

    public async Task<ExecuteOrderPlanResult> ExecuteOrderPlan(OrderPlan orderPlan)
    {
        ExecuteOrderPlanResult result = new();

        CryptExchangesResult cryptoExchangesResult = await _exchangeService.GetCryptoExchanges();

        foreach (OrderPlanDetail orderPlanDetail in orderPlan.OrderPlanDetails)
        {
            CryptoExchange cryptoExchange = cryptoExchangesResult.CryptoExchanges
                .FirstOrDefault(exchange => exchange.Id == orderPlanDetail.CryptoExchangeId)
                ?? throw new InvalidOperationException($"Crypto exchange with ID {orderPlanDetail.CryptoExchangeId} not found.");

            Order order = orderPlanDetail.Order;
            if (order.Type == OrderType.Buy)
            {
                cryptoExchange.AvailableFunds.Crypto -= orderPlanDetail.Amount;
                decimal price = order.Price * orderPlanDetail.Amount;
                cryptoExchange.AvailableFunds.Euro += price;

                if (order.Amount > orderPlanDetail.Amount)
                {
                    // Reduce order
                    order.Amount -= orderPlanDetail.Amount;
                }
                else
                {
                    // remove bid order
                    Bid? bidOrderToRemove = cryptoExchange.OrderBook.Bids
                        .FirstOrDefault(b => b.Order.Id == order.Id);

                    if (bidOrderToRemove is null)
                    {
                        result.AddError("Bid order to remove not found in the order book.");
                        return result;
                    }

                    List<Bid> bids = [.. cryptoExchange.OrderBook.Bids];
                    bids.Remove(bidOrderToRemove);
                    cryptoExchange.OrderBook = new Orderbook
                    {
                        Bids = [.. bids],
                        Asks = cryptoExchange.OrderBook.Asks
                    };

                    //// remove ask order
                    //Ask? askOrderToRemove = cryptoExchange.OrderBook.Asks
                    //    .FirstOrDefault(b => b.Order.Id == order.Id);

                    //if (askOrderToRemove is null)
                    //{
                    //    result.AddError("Ask order to remove not found in the order book.");
                    //    return result;
                    //}

                    //List<Ask> asks = [.. cryptoExchange.OrderBook.Asks];
                    //asks.Remove(askOrderToRemove);
                    //cryptoExchange.OrderBook = new Orderbook
                    //{
                    //    Bids = cryptoExchange.OrderBook.Bids,
                    //    Asks = [.. asks]
                    //};
                }
            }
            else if (order.Type == OrderType.Sell)
            {
                cryptoExchange.AvailableFunds.Crypto += orderPlanDetail.Amount;
                decimal price = order.Price * orderPlanDetail.Amount;
                cryptoExchange.AvailableFunds.Euro -= price;

                if (order.Amount > orderPlanDetail.Amount)
                {
                    // Reduce order
                    order.Amount -= orderPlanDetail.Amount;
                }
                else
                {
                    // remove ask order
                    Ask askOrderToRemove = cryptoExchange.OrderBook.Asks
                        .FirstOrDefault(b => b.Order.Id == order.Id)
                        ?? throw new InvalidOperationException("Bid order to remove not found in the order book.");

                    List<Ask> asks = [.. cryptoExchange.OrderBook.Asks];
                    asks.Remove(askOrderToRemove);
                    cryptoExchange.OrderBook = new Orderbook
                    {
                        Bids = cryptoExchange.OrderBook.Bids,
                        Asks = [.. asks]
                    };

                    //// remove bid order
                    //Bid? bidOrderToRemove = cryptoExchange.OrderBook.Bids
                    //    .FirstOrDefault(b => b.Order.Id == order.Id);

                    //if (bidOrderToRemove is null)
                    //{
                    //    result.AddError("Bid order to remove not found in the order book.");
                    //    return result;
                    //}

                    //List<Bid> bids = [.. cryptoExchange.OrderBook.Bids];
                    //bids.Remove(bidOrderToRemove);
                    //cryptoExchange.OrderBook = new Orderbook
                    //{
                    //    Bids = [.. bids],
                    //    Asks = cryptoExchange.OrderBook.Asks
                    //};
                }
            }
            else
            {
                string errorMessage = $"Unknown order type: {order.Type}";
                result.AddError(errorMessage);
                return result;
            }
        }
        return result;
    }

    public void AddAsk(CryptoExchange cryptoExchange, Order order)
    {
        Ask ask = new() { Order = order };
        List<Ask> asks = [.. cryptoExchange.OrderBook.Asks];
        asks.Add(ask);
        cryptoExchange.OrderBook = new Orderbook()
        {
            Bids = cryptoExchange.OrderBook.Bids,
            Asks = [.. asks]
        };
    }

    public void AddBid(CryptoExchange cryptoExchange, Order order)
    {
        Bid bid = new() { Order = order };
        List<Bid> bids = [.. cryptoExchange.OrderBook.Bids];
        bids.Add(bid);
        cryptoExchange.OrderBook = new Orderbook()
        {
            Asks = cryptoExchange.OrderBook.Asks,
            Bids = [.. bids]
        };
    }
}
