using System.Diagnostics;

namespace MetaExchange.Shared.Models;

[DebuggerDisplay("""
    Ask: {Order.Type},
    Amount: {Order.Amount},
    Price: {Order.Price}
    """)]
public class Ask
{
    public required Order Order { get; init; }
}
