using System.Diagnostics;

namespace MetaExchange.Shared.Models;

[DebuggerDisplay("""
    Id: {Id},
    AvailableFunds: {AvailableFunds}
    """)]
public class CryptoExchange
{
    public required string Id { get; init; }
    public required Availablefunds AvailableFunds { get; set; }
    public required Orderbook OrderBook { get; set; }

    
}
