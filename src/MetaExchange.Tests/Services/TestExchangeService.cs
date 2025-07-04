using MetaExchange.Shared.Models;
using MetaExchange.Shared.Models.Results;
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

    public CryptoExchange[] CryptoExchanges { get => _cryptoExchanges.ToArray(); set => new NotImplementedException(); }
    private readonly List<CryptoExchange> _cryptoExchanges = [];

    public Task<CryptExchangesResult> GetCryptoExchanges()
    {
        CryptExchangesResult result = new()
        {
            CryptoExchanges = [.. _cryptoExchanges]
        };
        return Task.FromResult(result);
    }
}
