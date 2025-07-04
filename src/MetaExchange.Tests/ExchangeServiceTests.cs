using MetaExchange.Shared.Models.Results;
using MetaExchange.Shared.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace MetaExchange.Tests;

public class FileBasedExchangeServiceTests
{

    [Fact]
    public async Task Load_All_Embedded_Files()
    {
        // Arrange
        Mock<ILogger<FileBasedExchangeService>> mockLogger = new();
        IExchangeService exchangeService = new FileBasedExchangeService(mockLogger.Object);

        // Act
        // do nothing

        // Assert
        CryptoExchangesResult result = await exchangeService.GetCryptoExchanges();
        Assert.NotNull(result.CryptoExchanges);
        Assert.True(result.CryptoExchanges.Length > 0);
    }
    
}
