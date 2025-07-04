using MetaExchange.Shared.Models;
using MetaExchange.Shared.Models.Results;

namespace MetaExchange.Shared.Services;
public interface IOrderBookService
{
    Task<CryptoExchangesResult> GetCryptoExchanges();
    Task<OrderPlan> CreateBuyPlan(decimal amountOfBtcToBuy);
    Task<OrderPlan> CreateSellPlan(decimal amountOfBtcToSell);
    Task<ExecuteOrderPlanResult> ExecuteOrderPlan(OrderPlan orderPlan);
    void AddAsk(CryptoExchange cryptoExchange, Order order);
}
