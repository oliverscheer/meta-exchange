using MetaExchange.Shared.Models;

namespace MetaExchange.Shared.Services;

public interface IExchangeService
{
    Task<Result<CryptoExchange[]>> GetCryptoExchanges(CancellationToken cancellationToken);
}
