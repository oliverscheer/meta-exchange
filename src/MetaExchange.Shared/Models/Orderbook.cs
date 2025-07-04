using System.Diagnostics;

namespace MetaExchange.Shared.Models;

[DebuggerDisplay("""
    Bids: {Bids.Length},
    Asks: {Asks.Length}
    """)]
public class Orderbook
{
    public required Bid[] Bids { get; init; }
    public required Ask[] Asks { get; init; }
}
