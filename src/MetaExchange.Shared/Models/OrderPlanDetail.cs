using System.Diagnostics;

namespace MetaExchange.Shared.Models;

[DebuggerDisplay("""
    CryptoExchange: {CryptoExchangeId}
    Order: {Order}
    Amount: {Amount}
    """)]
    
public record OrderPlanDetail(
    string CryptoExchangeId,
    Order Order,
    decimal Amount);
