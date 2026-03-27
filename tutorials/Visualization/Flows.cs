using PocketFlow;

namespace Visualization;

/// <summary>
/// Top-level order-processing flow (mirrors async_flow.py).
/// Payment → Inventory → Shipping as three nested <see cref="AsyncFlow"/> sub-flows.
/// </summary>
public class OrderFlow : AsyncFlow
{
    public OrderFlow(AsyncFlow paymentFlow,
                     AsyncFlow inventoryFlow,
                     AsyncFlow shippingFlow)
    {
        // Wire the three sub-flows together
        paymentFlow.Next(inventoryFlow);
        inventoryFlow.Next(shippingFlow);

        StartNode = paymentFlow;
    }
}

/// <summary>
/// Factory helpers that build the standard order pipeline.
/// </summary>
public static class FlowFactory
{
    /// <summary>
    /// Build an <see cref="OrderFlow"/> with sequential payment → inventory → shipping.
    /// </summary>
    public static OrderFlow BuildOrderPipeline()
    {
        // Payment sub-flow: ValidatePayment → ProcessPayment → PaymentConfirmation
        var validatePayment   = new ValidatePayment();
        var processPayment    = new ProcessPayment();
        var paymentConfirmation = new PaymentConfirmation();

        validatePayment.Next(processPayment);
        processPayment.Next(paymentConfirmation);
        var paymentFlow = new AsyncFlow(start: validatePayment);

        // Inventory sub-flow: CheckStock → ReserveItems → UpdateInventory
        var checkStock      = new CheckStock();
        var reserveItems    = new ReserveItems();
        var updateInventory = new UpdateInventory();

        checkStock.Next(reserveItems);
        reserveItems.Next(updateInventory);
        var inventoryFlow = new AsyncFlow(start: checkStock);

        // Shipping sub-flow: CreateLabel → AssignCarrier → SchedulePickup
        var createLabel    = new CreateLabel();
        var assignCarrier  = new AssignCarrier();
        var schedulePickup = new SchedulePickup();

        createLabel.Next(assignCarrier);
        assignCarrier.Next(schedulePickup);
        var shippingFlow = new AsyncFlow(start: createLabel);

        return new OrderFlow(paymentFlow, inventoryFlow, shippingFlow);
    }

    /// <summary>
    /// Sample shared-data dictionary representing an incoming order.
    /// </summary>
    public static Dictionary<string, object> BuildSharedData() => new()
    {
        ["order_id"]  = "ORD-12345",
        ["customer"]  = "John Doe",
        ["items"]     = new[]
        {
            new Dictionary<string, object> { ["id"] = "ITEM-001", ["name"] = "Smartphone",  ["price"] = 999.99, ["quantity"] = 1 },
            new Dictionary<string, object> { ["id"] = "ITEM-002", ["name"] = "Phone case",  ["price"] =  29.99, ["quantity"] = 1 },
        },
        ["shipping_address"] = new Dictionary<string, object>
        {
            ["street"] = "123 Main St",
            ["city"]   = "Anytown",
            ["state"]  = "CA",
            ["zip"]    = "12345",
        },
    };
}

