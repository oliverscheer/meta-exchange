using MetaExchange.Shared.Models;

namespace MetaExchange.Shared.Services;

public interface IExchangeService
{
    //CryptoExchange[] CryptoExchanges { get; set; }

    Task<Result<CryptoExchange[]>> GetCryptoExchanges();
}
