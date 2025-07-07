using MetaExchange.Shared.Models;

namespace MetaExchange.Shared.Services;
public interface IOrderBookService
{
    Task<Result<CryptoExchange[]>> GetCryptoExchanges();
    Task<Result<OrderPlan>> CreateBuyPlan(decimal amountOfBtcToBuy);
    Task<Result<OrderPlan>> CreateSellPlan(decimal amountOfBtcToSell);
    Task<Result<OrderPlan>> ExecuteOrderPlan(OrderPlan orderPlan);
    void AddAsk(CryptoExchange cryptoExchange, Order order);
    void AddBid(CryptoExchange cryptoExchange, Order order);
}
