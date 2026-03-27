using PocketFlow;

namespace Visualization;

// ── Payment Nodes ─────────────────────────────────────────────────────────────

public class ValidatePayment : AsyncNode
{
    protected override async Task<object?> ExecAsync(object? prepRes)
    {
        Console.WriteLine("  1.1. Validating payment...");
        await Task.Delay(50);
        return "Payment validated successfully";
    }

    protected override Task<object?> PostAsync(object shared, object? prepRes, object? execRes)
    {
        if (shared is Dictionary<string, object> s) s["payment_status"] = execRes!;
        return Task.FromResult<object?>("default");
    }
}

public class ProcessPayment : AsyncNode
{
    protected override async Task<object?> ExecAsync(object? prepRes)
    {
        Console.WriteLine("  1.2. Processing payment...");
        await Task.Delay(50);
        return "Payment processed successfully";
    }

    protected override Task<object?> PostAsync(object shared, object? prepRes, object? execRes)
    {
        if (shared is Dictionary<string, object> s) s["payment_result"] = execRes!;
        return Task.FromResult<object?>("default");
    }
}

public class PaymentConfirmation : AsyncNode
{
    protected override async Task<object?> ExecAsync(object? prepRes)
    {
        Console.WriteLine("  1.3. Confirming payment...");
        await Task.Delay(50);
        return "Payment confirmed";
    }

    protected override Task<object?> PostAsync(object shared, object? prepRes, object? execRes)
    {
        if (shared is Dictionary<string, object> s) s["payment_confirmation"] = execRes!;
        return Task.FromResult<object?>("default");
    }
}

// ── Inventory Nodes ───────────────────────────────────────────────────────────

public class CheckStock : AsyncNode
{
    protected override async Task<object?> ExecAsync(object? prepRes)
    {
        Console.WriteLine("  2.1. Checking inventory stock...");
        await Task.Delay(50);
        return "Stock available";
    }

    protected override Task<object?> PostAsync(object shared, object? prepRes, object? execRes)
    {
        if (shared is Dictionary<string, object> s) s["stock_status"] = execRes!;
        return Task.FromResult<object?>("default");
    }
}

public class ReserveItems : AsyncNode
{
    protected override async Task<object?> ExecAsync(object? prepRes)
    {
        Console.WriteLine("  2.2. Reserving items...");
        await Task.Delay(50);
        return "Items reserved";
    }

    protected override Task<object?> PostAsync(object shared, object? prepRes, object? execRes)
    {
        if (shared is Dictionary<string, object> s) s["reservation_status"] = execRes!;
        return Task.FromResult<object?>("default");
    }
}

public class UpdateInventory : AsyncNode
{
    protected override async Task<object?> ExecAsync(object? prepRes)
    {
        Console.WriteLine("  2.3. Updating inventory...");
        await Task.Delay(50);
        return "Inventory updated";
    }

    protected override Task<object?> PostAsync(object shared, object? prepRes, object? execRes)
    {
        if (shared is Dictionary<string, object> s) s["inventory_update"] = execRes!;
        return Task.FromResult<object?>("default");
    }
}

// ── Shipping Nodes ────────────────────────────────────────────────────────────

public class CreateLabel : AsyncNode
{
    protected override async Task<object?> ExecAsync(object? prepRes)
    {
        Console.WriteLine("  3.1. Creating shipping label...");
        await Task.Delay(50);
        return "Shipping label created";
    }

    protected override Task<object?> PostAsync(object shared, object? prepRes, object? execRes)
    {
        if (shared is Dictionary<string, object> s) s["shipping_label"] = execRes!;
        return Task.FromResult<object?>("default");
    }
}

public class AssignCarrier : AsyncNode
{
    protected override async Task<object?> ExecAsync(object? prepRes)
    {
        Console.WriteLine("  3.2. Assigning carrier...");
        await Task.Delay(50);
        return "Carrier assigned";
    }

    protected override Task<object?> PostAsync(object shared, object? prepRes, object? execRes)
    {
        if (shared is Dictionary<string, object> s) s["carrier"] = execRes!;
        return Task.FromResult<object?>("default");
    }
}

public class SchedulePickup : AsyncNode
{
    protected override async Task<object?> ExecAsync(object? prepRes)
    {
        Console.WriteLine("  3.3. Scheduling pickup...");
        await Task.Delay(50);
        return "Pickup scheduled";
    }

    protected override Task<object?> PostAsync(object shared, object? prepRes, object? execRes)
    {
        if (shared is Dictionary<string, object> s) s["pickup_status"] = execRes!;
        return Task.FromResult<object?>("default");
    }
}

