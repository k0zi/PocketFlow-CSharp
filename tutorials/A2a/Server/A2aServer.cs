using System.Text.Json;
using A2aProtocol;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace A2a;

/// <summary>
/// Minimal ASP.NET Core host that exposes the A2A JSON-RPC endpoints.
/// Ported from <c>common/server/server.py</c>.
/// </summary>
public sealed class A2aServer
{
    private readonly AgentCard            _agentCard;
    private readonly A2aTaskManagerBase   _taskManager;
    private readonly string               _host;
    private readonly int                  _port;
    private readonly string               _endpoint;

    public A2aServer(
        AgentCard          agentCard,
        A2aTaskManagerBase taskManager,
        string             host     = "0.0.0.0",
        int                port     = 10002,
        string             endpoint = "/")
    {
        _agentCard   = agentCard;
        _taskManager = taskManager;
        _host        = host;
        _port        = port;
        _endpoint    = endpoint.StartsWith('/') ? endpoint : "/" + endpoint;
    }

    // ── Startup ───────────────────────────────────────────────────────────────

    /// <summary>Builds and runs the web application (blocks until shutdown).</summary>
    public void Start(string[]? args = null)
    {
        var builder = WebApplication.CreateBuilder(args ?? []);
        builder.WebHost.UseUrls($"http://{_host}:{_port}");

        var app = builder.Build();

        RegisterRoutes(app);
        app.Run();
    }

    // ── Route Registration ────────────────────────────────────────────────────

    private void RegisterRoutes(WebApplication app)
    {
        // ── Agent discovery ────────────────────────────────────────────────────
        app.MapGet("/.well-known/agent.json", () =>
            Results.Json(_agentCard, A2aJsonOptions.Default));

        // ── JSON-RPC dispatcher ────────────────────────────────────────────────
        app.MapPost(_endpoint,
            (Delegate)(async (HttpContext ctx) => await DispatchAsync(ctx)));
    }

    // ── JSON-RPC Dispatch ─────────────────────────────────────────────────────

    private async Task<IResult> DispatchAsync(HttpContext ctx)
    {
        JsonElement body;
        try
        {
            body = await JsonSerializer.DeserializeAsync<JsonElement>(
                ctx.Request.Body, A2aJsonOptions.Default);
        }
        catch
        {
            return Results.BadRequest("Invalid JSON body.");
        }

        if (!body.TryGetProperty("method", out var methodEl))
            return Results.BadRequest("Missing 'method' field.");

        var method = methodEl.GetString() ?? string.Empty;
        var id     = body.TryGetProperty("id", out var idEl) ? idEl : (JsonElement?)null;

        return method switch
        {
            "tasks/send"                  => await HandleSendTaskAsync(ctx, body, id),
            "tasks/get"                   => await HandleGetTaskAsync(body, id),
            "tasks/cancel"                => await HandleCancelTaskAsync(body, id),
            "tasks/pushNotification/set"  => await HandleSetPushNotificationAsync(body, id),
            "tasks/pushNotification/get"  => await HandleGetPushNotificationAsync(body, id),
            "tasks/resubscribe"           => await HandleResubscribeAsync(ctx, body, id),
            _                             => ErrorResult(id, A2aErrors.UnsupportedOperation()),
        };
    }

    // ── Method Handlers ───────────────────────────────────────────────────────

    private async Task<IResult> HandleSendTaskAsync(
        HttpContext ctx, JsonElement body, JsonElement? id)
    {
        SendTaskRequest req;
        try { req = Deserialize<SendTaskRequest>(body); }
        catch (Exception ex)
        {
            return ErrorResult(id, A2aErrors.InternalError(ex.Message));
        }

        // SSE streaming if client requests it via Accept header
        bool wantsSse = ctx.Request.Headers.Accept.ToString()
                           .Contains("text/event-stream", StringComparison.OrdinalIgnoreCase)
                        || (req.Params.Configuration?.AcceptedOutputModes
                               ?.Contains("text/event-stream") ?? false);

        if (wantsSse)
            return await StreamSseResponseAsync(
                ctx, _taskManager.OnSendTaskSubscribeAsync(req));

        try
        {
            var response = await _taskManager.OnSendTaskAsync(req);
            return Results.Json(response, A2aJsonOptions.Default);
        }
        catch (Exception ex)
        {
            return ErrorResult(id, A2aErrors.InternalError(ex.Message));
        }
    }

    private async Task<IResult> HandleGetTaskAsync(JsonElement body, JsonElement? id)
    {
        try
        {
            var req      = Deserialize<GetTaskRequest>(body);
            var response = await _taskManager.OnGetTaskAsync(req);
            return Results.Json(response, A2aJsonOptions.Default);
        }
        catch (Exception ex)
        {
            return ErrorResult(id, A2aErrors.InternalError(ex.Message));
        }
    }

    private async Task<IResult> HandleCancelTaskAsync(JsonElement body, JsonElement? id)
    {
        try
        {
            var req      = Deserialize<CancelTaskRequest>(body);
            var response = await _taskManager.OnCancelTaskAsync(req);
            return Results.Json(response, A2aJsonOptions.Default);
        }
        catch (Exception ex)
        {
            return ErrorResult(id, A2aErrors.InternalError(ex.Message));
        }
    }

    private async Task<IResult> HandleSetPushNotificationAsync(JsonElement body, JsonElement? id)
    {
        try
        {
            var req      = Deserialize<SetTaskPushNotificationRequest>(body);
            var response = await _taskManager.OnSetTaskPushNotificationAsync(req);
            return Results.Json(response, A2aJsonOptions.Default);
        }
        catch (Exception ex)
        {
            return ErrorResult(id, A2aErrors.InternalError(ex.Message));
        }
    }

    private async Task<IResult> HandleGetPushNotificationAsync(JsonElement body, JsonElement? id)
    {
        try
        {
            var req      = Deserialize<GetTaskPushNotificationRequest>(body);
            var response = await _taskManager.OnGetTaskPushNotificationAsync(req);
            return Results.Json(response, A2aJsonOptions.Default);
        }
        catch (Exception ex)
        {
            return ErrorResult(id, A2aErrors.InternalError(ex.Message));
        }
    }

    private async Task<IResult> HandleResubscribeAsync(
        HttpContext ctx, JsonElement body, JsonElement? id)
    {
        TaskResubscriptionRequest req;
        try { req = Deserialize<TaskResubscriptionRequest>(body); }
        catch (Exception ex)
        {
            return ErrorResult(id, A2aErrors.InternalError(ex.Message));
        }

        return await StreamSseResponseAsync(
            ctx, _taskManager.OnResubscribeToTaskAsync(req));
    }

    // ── SSE Helper ────────────────────────────────────────────────────────────

    private static async Task<IResult> StreamSseResponseAsync(
        HttpContext ctx,
        IAsyncEnumerable<SendTaskStreamingResponse> events)
    {
        ctx.Response.ContentType = "text/event-stream";
        ctx.Response.Headers.CacheControl = "no-cache";
        ctx.Response.Headers.Connection   = "keep-alive";

        await foreach (var evt in events)
        {
            if (ctx.RequestAborted.IsCancellationRequested) break;

            var json = JsonSerializer.Serialize(evt, A2aJsonOptions.Default);
            await ctx.Response.WriteAsync($"data: {json}\n\n");
            await ctx.Response.Body.FlushAsync();
        }

        await ctx.Response.WriteAsync("data: [DONE]\n\n");
        await ctx.Response.Body.FlushAsync();

        return Results.Empty;
    }

    // ── Utilities ─────────────────────────────────────────────────────────────

    private static T Deserialize<T>(JsonElement element)
        => JsonSerializer.Deserialize<T>(element.GetRawText(), A2aJsonOptions.Default)
           ?? throw new InvalidOperationException($"Failed to deserialise {typeof(T).Name}.");

    private static IResult ErrorResult(JsonElement? id, JsonRpcError error)
    {
        var resp = new
        {
            jsonrpc = "2.0",
            id,
            error,
        };
        return Results.Json(resp, A2aJsonOptions.Default);
    }
}




