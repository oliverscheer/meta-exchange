using System.Diagnostics;

namespace MetaExchange.Shared.Models;

[DebuggerDisplay("""
    TotalPrice: {TotalPrice},
    TotalAmount: {TotalAmount}
    """)]
public class OrderPlan
{
    public OrderPlan(OrderType orderType)
    {
        OrderType = orderType;
    }

    public decimal TotalPrice
    {
        get
        {
            return _orderPlanDetails.Sum(orderPlanDetail => orderPlanDetail.Order.Price * orderPlanDetail.Amount);
        }
    }

    public decimal TotalAmount
    {
        get
        {
            return _orderPlanDetails.Sum(orderPlanDetail => orderPlanDetail.Amount);
        }
    }

    private readonly List<OrderPlanDetail> _orderPlanDetails = [];
    public OrderPlanDetail[] OrderPlanDetails => [.. _orderPlanDetails];
    public OrderType OrderType{ get; }

    public void AddOrderPlanDetail(OrderPlanDetail orderPlanDetail)
    {
        _orderPlanDetails.Add(orderPlanDetail);
    }
}
