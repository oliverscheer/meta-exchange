using System.Diagnostics;

namespace MetaExchange.Shared.Models;

[DebuggerDisplay("""
    Bid: {Order.Type},
    Amount: {Order.Amount},
    Price: {Order.Price}
    """)]
public class Bid
{
    public required Order Order { get; init; }
}
