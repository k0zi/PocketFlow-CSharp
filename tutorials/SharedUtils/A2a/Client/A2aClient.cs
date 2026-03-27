using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace A2aProtocol;

/// <summary>
/// HTTP client for the A2A JSON-RPC protocol.
/// Ported from <c>common/client/client.py</c>.
/// </summary>
public sealed class A2aClient
{
    private readonly HttpClient  _http;
    private readonly string      _url;
    private          int         _idCounter;

    public AgentCard AgentCard { get; }

    public A2aClient(AgentCard agentCard, HttpClient? http = null)
    {
        AgentCard = agentCard;
        _url      = agentCard.Url.TrimEnd('/') + "/";
        _http     = http ?? new HttpClient();
    }

    // ── Core request builder ──────────────────────────────────────────────────

    private object BuildRequest(string method, object @params) => new
    {
        jsonrpc = "2.0",
        id      = System.Threading.Interlocked.Increment(ref _idCounter),
        method,
        @params,
    };

    // ── tasks/send ────────────────────────────────────────────────────────────

    /// <summary>Submits a task (non-streaming) and returns the initial task object.</summary>
    public async Task<SendTaskResponse> SendTaskAsync(TaskSendParams taskParams)
    {
        var body = BuildRequest("tasks/send", taskParams);
        var resp = await _http.PostAsJsonAsync(_url, body, A2aJsonOptions.Default);
        resp.EnsureSuccessStatusCode();

        var json = await resp.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<SendTaskResponse>(json, A2aJsonOptions.Default)
               ?? throw new InvalidOperationException("Null response from tasks/send");
    }

    // ── tasks/get ─────────────────────────────────────────────────────────────

    /// <summary>Retrieves the current state of a task.</summary>
    public async Task<GetTaskResponse> GetTaskAsync(TaskQueryParams queryParams)
    {
        var body = BuildRequest("tasks/get", queryParams);
        var resp = await _http.PostAsJsonAsync(_url, body, A2aJsonOptions.Default);
        resp.EnsureSuccessStatusCode();

        var json = await resp.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<GetTaskResponse>(json, A2aJsonOptions.Default)
               ?? throw new InvalidOperationException("Null response from tasks/get");
    }

    // ── tasks/cancel ──────────────────────────────────────────────────────────

    /// <summary>Requests cancellation of a running task.</summary>
    public async Task<CancelTaskResponse> CancelTaskAsync(TaskIdParams idParams)
    {
        var body = BuildRequest("tasks/cancel", idParams);
        var resp = await _http.PostAsJsonAsync(_url, body, A2aJsonOptions.Default);
        resp.EnsureSuccessStatusCode();

        var json = await resp.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<CancelTaskResponse>(json, A2aJsonOptions.Default)
               ?? throw new InvalidOperationException("Null response from tasks/cancel");
    }

    // ── tasks/send (streaming / SSE) ──────────────────────────────────────────

    /// <summary>
    /// Submits a task and receives SSE streaming events.
    /// Yields each <see cref="SendTaskStreamingResponse"/> as it arrives.
    /// </summary>
    public async IAsyncEnumerable<SendTaskStreamingResponse> SendTaskStreamingAsync(
        TaskSendParams taskParams,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var body = BuildRequest("tasks/send", taskParams);

        using var request = new HttpRequestMessage(HttpMethod.Post, _url)
        {
            Content = JsonContent.Create(body, options: A2aJsonOptions.Default),
        };
        request.Headers.Accept.ParseAdd("text/event-stream");

        using var resp   = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        resp.EnsureSuccessStatusCode();

        using var stream = await resp.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        string? line;
        while (!cancellationToken.IsCancellationRequested
               && (line = await reader.ReadLineAsync(cancellationToken)) is not null)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            if (line.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            {
                var data = line["data:".Length..].Trim();
                if (data == "[DONE]") yield break;

                SendTaskStreamingResponse? evt = null;
                try
                {
                    evt = JsonSerializer.Deserialize<SendTaskStreamingResponse>(
                        data, A2aJsonOptions.Default);
                }
                catch { /* skip malformed events */ }

                if (evt is not null) yield return evt;
            }
        }
    }

    // ── tasks/pushNotification/set ────────────────────────────────────────────

    public async Task<SetTaskPushNotificationResponse> SetTaskPushNotificationAsync(
        TaskPushNotificationConfig config)
    {
        var body = BuildRequest("tasks/pushNotification/set", config);
        var resp = await _http.PostAsJsonAsync(_url, body, A2aJsonOptions.Default);
        resp.EnsureSuccessStatusCode();

        var json = await resp.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<SetTaskPushNotificationResponse>(json, A2aJsonOptions.Default)
               ?? throw new InvalidOperationException("Null response from tasks/pushNotification/set");
    }

    // ── tasks/pushNotification/get ────────────────────────────────────────────

    public async Task<GetTaskPushNotificationResponse> GetTaskPushNotificationAsync(TaskIdParams idParams)
    {
        var body = BuildRequest("tasks/pushNotification/get", idParams);
        var resp = await _http.PostAsJsonAsync(_url, body, A2aJsonOptions.Default);
        resp.EnsureSuccessStatusCode();

        var json = await resp.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<GetTaskPushNotificationResponse>(json, A2aJsonOptions.Default)
               ?? throw new InvalidOperationException("Null response from tasks/pushNotification/get");
    }
}


