namespace MetaExchange.WebApi;

public static class ApiRoutes
{
    private const string Root = "api/";

    public static class CryptoExchanges
    {
        private const string Root = ApiRoutes.Root + "cryptoexchange";
        public const string GetAll = Root;
        public const string GetExchangeById = Root + "/id";
    }

    public static class OrderBook
    {
        private const string Root = ApiRoutes.Root + "orderbook";
        public const string GetBuyOrderPlan = Root + "/buyplan";
        public const string GetSellOrderPlan = Root + "/sellplan";
        //public const string GetOrderBook = Root + "/{exchangeId}";
        public const string ExecuteOrderPlan = Root + "/execute";
    }
}
