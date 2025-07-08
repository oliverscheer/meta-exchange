using MetaExchange.Shared.Models;

namespace MetaExchange.Shared.Services;
public interface IOrderBookService
{
    Task<Result<CryptoExchange[]>> GetCryptoExchanges(CancellationToken cancellationToken);
    Task<Result<OrderPlan>> CreateBuyPlan(decimal amountOfBtcToBuy, CancellationToken cancellationToken);
    Task<Result<OrderPlan>> CreateSellPlan(decimal amountOfBtcToSell, CancellationToken cancellationToken);
    Task<Result<OrderPlan>> ExecuteOrderPlan(OrderPlan orderPlan, CancellationToken cancellationToken);
    void AddAsk(CryptoExchange cryptoExchange, Order order);
    void AddBid(CryptoExchange cryptoExchange, Order order);
}
