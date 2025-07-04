using System.Diagnostics;

namespace MetaExchange.Shared.Models;

[DebuggerDisplay("""
    Crypto: {Crypto},
    Euro: {Euro} €
    """)]
public class Availablefunds
{
    public decimal Crypto { get; set; }
    public decimal Euro { get; set; }
}
