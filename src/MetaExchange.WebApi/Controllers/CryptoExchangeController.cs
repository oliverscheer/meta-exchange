using MetaExchange.Shared.Models;
using MetaExchange.Shared.Models.Results;
using MetaExchange.Shared.Services;
using Microsoft.AspNetCore.Mvc;

namespace MetaExchange.WebApi.Controllers;

[ApiController]
public class CryptoExchangeController : ControllerBase
{
    private readonly ILogger<CryptoExchangeController> _logger;
    private readonly IOrderBookService _orderBookService;

    public CryptoExchangeController(ILogger<CryptoExchangeController> logger,
        IOrderBookService orderBookService)
    {
        _logger = logger;
        _orderBookService = orderBookService;
    }

    // Remarks:
    // - This could be simplified with OneOf-Library, but that is not used in this project.
    // - CancellationToken should be used everywhere
    // - DTOs should be used instead of the full CryptoExchange model.

    [HttpGet]
    [Route(ApiRoutes.CryptoExchanges.GetAll)]
    public async Task<IActionResult> GetCryptoExchanges()
    {
        _logger.LogInformation("Get Crypto Exchanges called");
        CryptExchangesResult result = await _orderBookService.GetCryptoExchanges();

        // Remarks: This can be simplified with OneOf-Library, but that is not used in this project.

        if (result.CryptoExchanges.Length == 0)
        {
            _logger.LogWarning("No crypto exchanges found.");
            return NotFound("No crypto exchanges found.");
        }

        IEnumerable<string> reponse = result.CryptoExchanges.Select(ce => ce.Id);
        return Ok(reponse);
    }

    [HttpGet]
    [Route(ApiRoutes.CryptoExchanges.GetExchangeById)]
    public async Task<IActionResult> GetCryptoExchangeById(string id)
    {
        _logger.LogInformation("Get Crypto Exchange by id called");
        CryptExchangesResult result = await _orderBookService.GetCryptoExchanges();
        CryptoExchange? exchange = result.CryptoExchanges.FirstOrDefault(e => e.Id == id);
        if (exchange == null)
        {
            _logger.LogWarning($"Crypto exchange with ID {id} not found.");
            return NotFound($"Crypto exchange with ID {id} not found.");
        }

        return Ok(exchange);
    }
}
