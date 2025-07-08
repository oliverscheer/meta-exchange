using MetaExchange.Shared.Helper;
using MetaExchange.Shared.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Reflection;

namespace MetaExchange.Shared.Services;

public class FileBasedExchangeService : IExchangeService
{
    private readonly ILogger<FileBasedExchangeService> _logger;

    private CryptoExchange[] _cryptoExchanges = [];

    public FileBasedExchangeService(ILogger<FileBasedExchangeService> logger)
    {
        _logger = logger;
    }

    public async Task<Result<CryptoExchange[]>> GetCryptoExchanges(CancellationToken cancellationToken)
    {
        if (_cryptoExchanges.Length == 0)
        {
            _logger.LogInformation("CryptoExchanges is null. Loading data from embedded files.");
            _cryptoExchanges = await Task.Run(LoadDataFromEmbbedFiles, cancellationToken);
        }
        _logger.LogInformation("Get Crypto Exchanges called");

        Result<CryptoExchange[]> result = new(_cryptoExchanges);
        return result;
    }

    private List<string> GetEmbeddedFiles()
    {
        _logger.LogInformation("Get embedded sample files");

        // Get all files in the current assembly that in the Exchanges Folder
        Assembly assembly = typeof(FileBasedExchangeService).Assembly;
        List<string> resourceNames = [
            .. assembly
                .GetManifestResourceNames()
                .Where(name => name.StartsWith("MetaExchange.Shared.Data.Exchanges."))
                ];
        return resourceNames;
    }

    private CryptoExchange[] LoadDataFromEmbbedFiles()
    {
        _logger.LogInformation("Load data from embedded sample files");

        try
        {
            List<string> filenames = GetEmbeddedFiles();
            List<CryptoExchange> cryptoExchanges = [];

            foreach (string filename in filenames)
            {
                _logger.LogInformation($"Loading exchange data from file: {filename}");
                string jsonData = EmbeddedResourceHelper.GetEmbeddedResource(filename);
                CryptoExchange? crypto = LoadDataFromString(jsonData);

                cryptoExchanges.Add(crypto ?? throw new InvalidOperationException($"Failed to load exchange data from {filename}"));
            }
            _logger.LogInformation("Data loaded successfully.");

            return [.. cryptoExchanges];
        }
        catch (Exception exc)
        {
            _logger.LogError(exc, "Error loading exchange data from embedded files.");
            return [];
        }
    }

    private CryptoExchange? LoadDataFromString(string jsonData)
    {
        try
        {
            _logger.LogInformation("LoadDataFromString");

            CryptoExchange? exchangeData = JsonConvert.DeserializeObject<CryptoExchange>(jsonData);
            if (exchangeData == null)
            {
                _logger.LogError("Deserialized exchange data is null.");
                return null;
            }
            _logger.LogInformation("Data loaded successfully.");
            return exchangeData;
        }
        catch (Exception exc)
        {
            _logger.LogError(exc, $"Error loading exchange data from file: {jsonData}");
            throw;
        }
    }
}
