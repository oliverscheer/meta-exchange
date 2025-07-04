using MetaExchange.Shared.Models;
using MetaExchange.Shared.Models.Results;

namespace MetaExchange.Shared.Services;
public interface IOrderBookService
{
    Task<CryptExchangesResult> GetCryptoExchanges();
    Task<OrderPlan> CreateBuyPlan(decimal amountOfBtcToBuy);
    Task<OrderPlan> CreateSellPlan(decimal amountOfBtcToSell);
    Task<ExecuteOrderPlanResult> ExecuteOrderPlan(OrderPlan orderPlan);
}
