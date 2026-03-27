using PocketFlow;

namespace Visualization;

/// <summary>
/// Loop variant of the order pipeline (mirrors async_loop_flow.py).
///
/// Key differences from the standard <see cref="OrderFlow"/>:
/// <list type="bullet">
///   <item>ValidatePayment loops back to itself on <c>"out_of_stock"</c></item>
///   <item>ProcessPayment retries ValidatePayment on <c>"something_fail"</c></item>
///   <item>ProcessPayment continues to PaymentConfirmation only on <c>"pass"</c></item>
/// </list>
/// </summary>
public class LoopOrderFlow : AsyncFlow
{
    public LoopOrderFlow(AsyncFlow paymentFlow,
                         AsyncFlow inventoryFlow,
                         AsyncFlow shippingFlow)
    {
        paymentFlow.Next(inventoryFlow);
        inventoryFlow.Next(shippingFlow);
        StartNode = paymentFlow;
    }
}

public static class LoopFlowFactory
{
    public static LoopOrderFlow BuildLoopOrderPipeline()
    {
        // Payment sub-flow with retry/loop edges
        var validatePayment     = new ValidatePayment();
        var processPayment      = new ProcessPayment();
        var paymentConfirmation = new PaymentConfirmation();

        // default edge: validate → process
        validatePayment.Next(processPayment);
        // out_of_stock self-loop on validatePayment
        validatePayment.On("out_of_stock").Then(validatePayment);
        // failure retry on processPayment → validatePayment
        processPayment.On("something_fail").Then(validatePayment);
        // success edge: process → confirmation
        processPayment.On("pass").Then(paymentConfirmation);

        var paymentFlow = new AsyncFlow(start: validatePayment);

        // Inventory sub-flow (unchanged)
        var checkStock      = new CheckStock();
        var reserveItems    = new ReserveItems();
        var updateInventory = new UpdateInventory();

        checkStock.Next(reserveItems);
        reserveItems.Next(updateInventory);
        var inventoryFlow = new AsyncFlow(start: checkStock);

        // Shipping sub-flow (unchanged)
        var createLabel    = new CreateLabel();
        var assignCarrier  = new AssignCarrier();
        var schedulePickup = new SchedulePickup();

        createLabel.Next(assignCarrier);
        assignCarrier.Next(schedulePickup);
        var shippingFlow = new AsyncFlow(start: createLabel);

        return new LoopOrderFlow(paymentFlow, inventoryFlow, shippingFlow);
    }
}

