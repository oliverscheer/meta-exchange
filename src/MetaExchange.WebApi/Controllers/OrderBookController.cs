using MetaExchange.Shared.Models;
using MetaExchange.Shared.Services;
using Microsoft.AspNetCore.Mvc;

namespace MetaExchange.WebApi.Controllers;

[ApiController]
public class OrderBookController : ControllerBase
{
    private readonly ILogger<OrderBookController> _logger;
    private readonly IOrderBookService _orderBookService;

    public OrderBookController(ILogger<OrderBookController> logger,
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
    [Route(ApiRoutes.OrderBook.GetBuyOrderPlan)]
    public async Task<IActionResult> GetBuyOrderPlan(decimal amountOfBtc, bool execute = false)
    {
        Result<OrderPlan> orderPlanResult = await _orderBookService.CreateBuyPlan(amountOfBtc);
        OrderPlan orderPlan = orderPlanResult.Value;

        if (orderPlan.OrderPlanDetails.Length == 0)
        {
            _logger.LogWarning("Order plan could not be created.");
            return NotFound();
        }

        // Remark: To make it easier for demoing
        if (execute)
        {
            Result<OrderPlan> executionResult = await _orderBookService.ExecuteOrderPlan(orderPlan);
            if (!executionResult.Successful)
            {
                _logger.LogError(executionResult.ErrorMessage);
                return BadRequest();
            }
        }

        return Ok(orderPlan);
    }

    [HttpGet]
    [Route(ApiRoutes.OrderBook.GetSellOrderPlan)]
    public async Task<IActionResult> GetSellOrderPlan(decimal amountOfBtc)
    {
        Result<OrderPlan> orderPlanResult = await _orderBookService.CreateSellPlan(amountOfBtc);
        OrderPlan orderPlan = orderPlanResult.Value;

        if (orderPlan.OrderPlanDetails.Length == 0)
        {
            _logger.LogWarning("Order plan could not be created.");
            return NotFound("Order plan could not be created.");
        }

        return Ok(orderPlan);
    }

    [HttpGet]
    [Route(ApiRoutes.OrderBook.ExecuteOrderPlan)]
    public async Task<IActionResult> ExecuteOrderPlan([FromBody] OrderPlan orderPlan)
    {
        Result<OrderPlan> executeOrderPlanResult = await _orderBookService.ExecuteOrderPlan(orderPlan);

        if (!executeOrderPlanResult.Successful)
        {
            _logger.LogWarning("Order plan execution failed.");
            return BadRequest("Order plan execution failed: " + executeOrderPlanResult.ErrorMessage);
        }

        return Ok();
    }
}
