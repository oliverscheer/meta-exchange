using MetaExchange.Shared.Models;
using MetaExchange.Shared.Services;

namespace MetaExchange.Tests.Services;
public class TestExchangeService : IExchangeService
{
    public TestExchangeService(decimal initEuro, decimal initCrypto)
    {
        CryptoExchange sampleCryptoExchange = new()
        {
            AvailableFunds = new AvailableFunds
            {
                Euro = initEuro,
                Crypto = initCrypto
            },
            Id = "Test-Empty-Exchange",
            OrderBook = new Orderbook { Asks = [], Bids = [] }
        };

        _cryptoExchanges.Add(sampleCryptoExchange);
    }

    private readonly List<CryptoExchange> _cryptoExchanges = [];

    public async Task<Result<CryptoExchange[]>> GetCryptoExchanges(CancellationToken cancellationToken)
    {
        return await Task.FromResult(new Result<CryptoExchange[]>([.. _cryptoExchanges]));
    }


}
