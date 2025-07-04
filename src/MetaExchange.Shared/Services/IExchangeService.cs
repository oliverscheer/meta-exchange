using MetaExchange.Shared.Models;
using MetaExchange.Shared.Models.Results;

namespace MetaExchange.Shared.Services;
public interface IExchangeService
{
    //CryptoExchange[] CryptoExchanges { get; set; }

    Task<CryptExchangesResult> GetCryptoExchanges();
}
