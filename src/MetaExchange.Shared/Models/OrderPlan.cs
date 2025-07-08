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
            decimal totalPrice = 0;
            foreach (OrderPlanDetail orderPlanDetail in _orderPlanDetails)
            {
                totalPrice += orderPlanDetail.Order.Price * orderPlanDetail.Amount;
            }
            return totalPrice;
        }
    }

    public decimal TotalAmount
    {
        get
        {
            decimal totalAmount = 0;
            foreach (OrderPlanDetail orderPlanDetail in _orderPlanDetails)
            {
                totalAmount += orderPlanDetail.Amount;
            }
            return totalAmount;
        }
    }

    private readonly List<OrderPlanDetail> _orderPlanDetails = [];
    public OrderPlanDetail[] OrderPlanDetails => [.. _orderPlanDetails];
    public OrderType OrderType{ get; internal set; }

    public void AddOrderPlanDetail(OrderPlanDetail orderPlanDetail)
    {
        _orderPlanDetails.Add(orderPlanDetail);
    }
}
