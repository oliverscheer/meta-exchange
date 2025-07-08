using MetaExchange.Shared.Models;
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
        FileBasedExchangeService exchangeService = new(mockLogger.Object);
        CancellationToken cancellationToken = CancellationToken.None;

        // Act
        Result<CryptoExchange[]> result = await exchangeService.GetCryptoExchanges(cancellationToken);

        // Assert
        Assert.NotNull(result.Value);
        Assert.True(result.Value.Length > 0);
    }
}
