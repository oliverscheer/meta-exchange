namespace MetaExchange.WebApi;

public static class ApiRoutes
{
    private const string Root = "api/";

    public static class CryptoExchanges
    {
        private const string CryptoExchangesRoot = ApiRoutes.Root + "cryptoexchange";
        public const string GetAll = CryptoExchangesRoot;
        public const string GetExchangeById = CryptoExchangesRoot + "/id";
    }

    public static class OrderBook
    {
        private const string OrderBookRoot = Root + "orderbook";
        public const string GetBuyOrderPlan = OrderBookRoot + "/buyplan";
        public const string GetSellOrderPlan = OrderBookRoot + "/sellplan";
        public const string ExecuteOrderPlan = OrderBookRoot + "/execute";
    }
}
