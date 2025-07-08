using System.Diagnostics;

namespace MetaExchange.Shared.Models;

[DebuggerDisplay("""
    Crypto: {Crypto},
    Euro: {Euro} €
    """)]
public class AvailableFunds
{
    public decimal Crypto { get; set; }
    public decimal Euro { get; set; }
}
