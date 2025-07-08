using MetaExchange.Shared.Models;
using Microsoft.Extensions.Logging;

namespace MetaExchange.Shared.Services;
public class OrderBookService(IExchangeService exchangeService, ILogger<OrderBookService> logger)
    : IOrderBookService
{
    public async Task<Result<CryptoExchange[]>> GetCryptoExchanges(CancellationToken cancellationToken)
    {
        return await exchangeService.GetCryptoExchanges(cancellationToken);
    }

    public async Task<Result<OrderPlan>> CreateBuyPlan(decimal amountOfBtcToBuy, CancellationToken cancellationToken)
    {
        logger.LogInformation($"Create buying plan for {amountOfBtcToBuy}");
        OrderPlan orderPlan = new(OrderType.Buy);
        Result<OrderPlan> result = new(orderPlan);

        decimal remainingAmountOfBtcToBuy = amountOfBtcToBuy;

        Result<CryptoExchange[]> cryptoExchangesResult = await exchangeService.GetCryptoExchanges(cancellationToken);
        if (!cryptoExchangesResult.Successful)
        {
            logger.LogError("Failed to get crypto exchanges.");
            throw new InvalidOperationException("Failed to get crypto exchanges.");
        }

        List<Ask> asks = [.. cryptoExchangesResult.Value!
            .SelectMany(exchange => exchange.OrderBook.Asks)
            .OrderBy(ask => ask.Order.Price)];

        foreach (Ask ask in asks)
        {
            if (remainingAmountOfBtcToBuy <= 0)
            {
                break;
            }

            decimal tradeAmountOfBtc = Math.Min(remainingAmountOfBtcToBuy, ask.Order.Amount);

            CryptoExchange cryptoExchangeWithAsk = cryptoExchangesResult.Value!
                    .FirstOrDefault(exchange => exchange.OrderBook.Asks.Contains(ask))
                    ?? throw new InvalidOperationException("Crypto exchange with ask not found.");

            OrderPlanDetail orderPlanDetail = new(
                    cryptoExchangeWithAsk.Id,
                    ask.Order,
                    tradeAmountOfBtc);

            Result<OrderPlan> checkResult = CheckOrderPlanWithNewOrderPlanDetail(cryptoExchangeWithAsk, orderPlan, orderPlanDetail);
            if (!checkResult.Successful)
            {
                result.AddWarning(checkResult.ErrorMessage);
                logger.LogWarning($"Order plan check failed: {checkResult.ErrorMessage}");
                continue;
            }

            orderPlan.AddOrderPlanDetail(orderPlanDetail);
            remainingAmountOfBtcToBuy -= tradeAmountOfBtc;
        }
        return result;
    }

    public async Task<Result<OrderPlan>> CreateSellPlan(decimal amountOfBtcToSell, CancellationToken cancellationToken)
    {

        logger.LogInformation($"Create selling plan for {amountOfBtcToSell} BTC.");

        OrderPlan orderPlan = new(OrderType.Sell);
        Result<OrderPlan> result = new(orderPlan);

        decimal remainingAmountOfBtcToSell = amountOfBtcToSell;

        Result<CryptoExchange[]> cryptoExchangesResult = await exchangeService.GetCryptoExchanges(cancellationToken);

        List<Bid> bids = [.. cryptoExchangesResult.Value!
            .SelectMany(exchange => exchange.OrderBook.Bids)
            .OrderByDescending(bid => bid.Order.Price)];

        foreach (Bid bid in bids)
        {
            if (remainingAmountOfBtcToSell <= 0)
            {
                break;
            }

            decimal tradeAmountOfBtc = Math.Min(remainingAmountOfBtcToSell, bid.Order.Amount);

            CryptoExchange cryptoExchangeWithBid = cryptoExchangesResult.Value!
                    .FirstOrDefault(exchange => exchange.OrderBook.Bids.Contains(bid))
                    ?? throw new InvalidOperationException("Crypto exchange with bid not found.");

            OrderPlanDetail newOrderPlanDetail = new(
                    cryptoExchangeWithBid.Id,
                    bid.Order,
                    tradeAmountOfBtc);

            Result<OrderPlan> checkResult = CheckOrderPlanWithNewOrderPlanDetail(cryptoExchangeWithBid, orderPlan, newOrderPlanDetail);
            if (!checkResult.Successful)
            {
                result.AddWarning(checkResult.ErrorMessage);
                continue;
            }

            orderPlan.AddOrderPlanDetail(newOrderPlanDetail);
            remainingAmountOfBtcToSell -= tradeAmountOfBtc;
        }

        return result;
    }

    private static Result<OrderPlan> CheckOrderPlanWithNewOrderPlanDetail(CryptoExchange cryptoExchange,
        OrderPlan orderPlan,
        OrderPlanDetail orderPlanDetail)
    {
        Result<OrderPlan> result = new(orderPlan);

        decimal estimatedEuroForCryptoExchange = cryptoExchange.AvailableFunds.Euro;
        decimal estimatedCryptoForCryptoExchange = cryptoExchange.AvailableFunds.Crypto;

        // Calculate the estimated funds for the crypto exchange
        // before new order plan detail is added
        foreach (OrderPlanDetail opd in orderPlan.OrderPlanDetails)
        {
            if (opd.CryptoExchangeId != cryptoExchange.Id)
            {
                continue;
            }

            if (orderPlan.OrderType == OrderType.Buy)
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

        switch (orderPlan.OrderType)
        {
            // Check if the estimated values allow a buy or sell
            case OrderType.Buy:
            {
                // Can only buy with existing money
                decimal price = orderPlanDetail.Amount * orderPlanDetail.Order.Price;
                if (estimatedEuroForCryptoExchange >= price)
                {
                    return result;
                }

                result.AddError($"You don't have {price} Euro to buy the crypto in Exchange: '{cryptoExchange.Id}");
                return result;
            }
            // Can only sell with existing crypto
            case OrderType.Sell when estimatedCryptoForCryptoExchange >= orderPlanDetail.Amount:
                return result;
            case OrderType.Sell:
                result.AddError($"You don't have enough Crypto to sell on exchange {cryptoExchange.Id}.");
                return result;
            default:
                throw new Exception($"Unknown order type: {orderPlanDetail.Order.Type}");
        }
    }

    public async Task<Result<OrderPlan>> ExecuteOrderPlan(OrderPlan orderPlan, CancellationToken cancellationToken)
    {
        Result<OrderPlan> result = new();
        if (orderPlan.OrderPlanDetails.Length == 0)
        {
            result.AddError("Order plan has no details to execute.");
            return result;
        }

        Result<CryptoExchange[]> cryptoExchangesResult = await exchangeService.GetCryptoExchanges(cancellationToken);

        if (!cryptoExchangesResult.Successful ||
            cryptoExchangesResult.Value is null)
        {
            result.AddError("Failed to get crypto exchanges.");
            return result;
        }

        foreach ((string cryptoExchangeId, Order order, decimal amount) in orderPlan.OrderPlanDetails)
        {
            CryptoExchange cryptoExchange = cryptoExchangesResult.Value
                .FirstOrDefault(exchange => exchange.Id == cryptoExchangeId)
                ?? throw new InvalidOperationException($"Crypto exchange with ID {cryptoExchangeId} not found.");

            switch (orderPlan.OrderType)
            {
                case OrderType.Buy:
                {
                    // I'm buying from a bid
                    // so we need to reduce crypto exchange funds
                    // and add increase euro funds
                    cryptoExchange.AvailableFunds.Crypto += amount;
                    cryptoExchange.AvailableFunds.Euro -= order.Price * amount;

                    if (order.Amount > amount)
                    {
                        // Reduce amount in bid order
                        order.Amount -= amount;
                    }
                    else
                    {
                        // remove ask order
                        Ask askOrderToRemove = cryptoExchange.OrderBook.Asks
                                                   .FirstOrDefault(b => b.Order.Id == order.Id)
                                               ?? throw new InvalidOperationException("Ask order to remove not found in the order book.");

                        // Update Orderbook
                        List<Ask> asks = [.. cryptoExchange.OrderBook.Asks];
                        asks.Remove(askOrderToRemove);
                        cryptoExchange.OrderBook = new Orderbook
                        {
                            Bids = cryptoExchange.OrderBook.Bids,
                            Asks = [.. asks]
                        };
                    }

                    break;
                }
                case OrderType.Sell:
                {
                    // I'm selling to an ask,
                    // so we need to increase crypto exchange funds
                    // and reduce euro funds
                    cryptoExchange.AvailableFunds.Crypto -= amount;
                    cryptoExchange.AvailableFunds.Euro += order.Price * amount;

                    if (order.Amount > amount)
                    {
                        // Reduce order
                        order.Amount -= amount;
                    }
                    else
                    {
                        // Remove bid order completely
                        Bid? bidOrderToRemove = cryptoExchange.OrderBook.Bids
                            .FirstOrDefault(b => b.Order.Id == order.Id);

                        if (bidOrderToRemove is null)
                        {
                            result.AddError("Bid order to remove not found in the order book.");
                            return result;
                        }

                        // Update Order book
                        List<Bid> bids = [.. cryptoExchange.OrderBook.Bids];
                        bids.Remove(bidOrderToRemove);
                        cryptoExchange.OrderBook = new Orderbook
                        {
                            Bids = [.. bids],
                            Asks = cryptoExchange.OrderBook.Asks
                        };
                    }

                    break;
                }
                default:
                {
                    string errorMessage = $"Unknown order type: {order.Type}";
                    result.AddError(errorMessage);
                    return result;
                }
            }
        }
        return result;
    }

    public void AddAsk(CryptoExchange cryptoExchange, Order order)
    {
        Ask ask = new() { Order = order };
        List<Ask> asks =
        [
            .. cryptoExchange.OrderBook.Asks,
            ask
        ];
        cryptoExchange.OrderBook = new Orderbook
        {
            Bids = cryptoExchange.OrderBook.Bids,
            Asks = [.. asks]
        };
    }

    public void AddBid(CryptoExchange cryptoExchange, Order order)
    {
        Bid bid = new() { Order = order };
        List<Bid> bids =
        [
            .. cryptoExchange.OrderBook.Bids,
            bid
        ];
        cryptoExchange.OrderBook = new Orderbook
        {
            Asks = cryptoExchange.OrderBook.Asks,
            Bids = [.. bids]
        };
    }
}
