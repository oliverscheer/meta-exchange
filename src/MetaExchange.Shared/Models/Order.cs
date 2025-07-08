using System.Diagnostics;

namespace MetaExchange.Shared.Models;

[DebuggerDisplay("""
    Type: {Type},
    Amount: {Amount},
    Price: {Price}
    """)]
public class Order
{
    public required string Id { get; init; }
    public DateTime Time { get; init; }
    public OrderType Type { get; init; }
    public OrderKind Kind { get; init; }
    public decimal Amount { get; set; }
    public decimal Price { get; init; }
}
