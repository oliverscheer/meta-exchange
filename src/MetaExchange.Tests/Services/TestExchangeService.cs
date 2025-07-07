using MetaExchange.Shared.Models;
using MetaExchange.Shared.Services;

namespace MetaExchange.Tests.Services;
public class TestExchangeService : IExchangeService
{
    public TestExchangeService(decimal initEuro, decimal initCrypto)
    {
        CryptoExchange sampleCryptoExchange = new()
        {
            AvailableFunds = new Availablefunds()
            {
                Euro = initEuro,
                Crypto = initCrypto
            },
            Id = "Test-Empty-Exchange",
            OrderBook = new Orderbook() { Asks = [], Bids = [] }
        };

        _cryptoExchanges.Add(sampleCryptoExchange);
    }

    public CryptoExchange[] CryptoExchanges { get => [.. _cryptoExchanges]; set => new NotImplementedException(); }
    private readonly List<CryptoExchange> _cryptoExchanges = [];

    public async Task<Result<CryptoExchange[]>> GetCryptoExchanges()
    {
        // Simulate asynchronous behavior to resolve CS1998
        return await Task.FromResult(new Result<CryptoExchange[]>([.. _cryptoExchanges]));
    }
}
