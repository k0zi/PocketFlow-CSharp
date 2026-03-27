using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Tracing;

/// <summary>
/// Core tracer that pushes observability events to the Langfuse REST API.
/// Uses the <c>/api/public/ingestion</c> batch endpoint (Langfuse SDK v2 format).
/// Ported from tracing/core.py in the pocketflow-tracing cookbook.
/// </summary>
public sealed class LangfuseTracer : IDisposable
{
    // ── State ─────────────────────────────────────────────────────────────────
    private readonly TracingConfig _config;
    private readonly HttpClient? _http;

    private string? _currentTraceId;
    private readonly Dictionary<string, SpanInfo> _spans = new();
    private readonly List<object> _pending = new();
    private bool _disposed;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy        = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition      = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented               = false,
    };

    private record SpanInfo(string SpanId, string TraceId, DateTime StartTime);

    // ── Construction ─────────────────────────────────────────────────────────
    public LangfuseTracer(TracingConfig config)
    {
        _config = config;

        if (!config.Validate())
        {
            if (config.Debug) Console.WriteLine("✗ Langfuse not available or configuration invalid");
            return;
        }

        try
        {
            _http = new HttpClient { BaseAddress = new Uri(config.LangfuseHost!) };

            // Basic auth: public_key:secret_key (base64)
            var credentials = Convert.ToBase64String(
                Encoding.UTF8.GetBytes($"{config.LangfusePublicKey}:{config.LangfuseSecretKey}"));
            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Basic", credentials);
            _http.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            if (config.Debug)
                Console.WriteLine($"✓ Langfuse client initialised with host: {config.LangfuseHost}");
        }
        catch (Exception e)
        {
            if (config.Debug) Console.WriteLine($"✗ Failed to initialise Langfuse client: {e.Message}");
            _http?.Dispose();
            _http = null;
        }
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>Starts a new Langfuse trace for a flow execution.</summary>
    /// <returns>The trace ID, or <see langword="null"/> when tracing is disabled.</returns>
    public string? StartTrace(string flowName, object inputData)
    {
        if (_http == null) return null;
        try
        {
            _currentTraceId = Guid.NewGuid().ToString();

            EnqueueEvent("trace-create", new
            {
                id        = _currentTraceId,
                name      = flowName,
                input     = SerializeData(inputData),
                metadata  = new
                {
                    framework  = "PocketFlow",
                    trace_type = "flow_execution",
                    timestamp  = DateTime.UtcNow.ToString("O"),
                },
                sessionId = _config.SessionId,
                userId    = _config.UserId,
            });

            if (_config.Debug)
                Console.WriteLine($"✓ Started trace: {_currentTraceId} for flow: {flowName}");

            return _currentTraceId;
        }
        catch (Exception e)
        {
            if (_config.Debug) Console.WriteLine($"✗ Failed to start trace: {e.Message}");
            return null;
        }
    }

    /// <summary>Finalises the current trace and records its output.</summary>
    public void EndTrace(object outputData, string status = "success")
    {
        if (_currentTraceId == null) return;
        try
        {
            EnqueueEvent("trace-update", new
            {
                id       = _currentTraceId,
                output   = SerializeData(outputData),
                metadata = new { status, end_timestamp = DateTime.UtcNow.ToString("O") },
            });

            if (_config.Debug) Console.WriteLine($"✓ Ended trace with status: {status}");
        }
        catch (Exception e)
        {
            if (_config.Debug) Console.WriteLine($"✗ Failed to end trace: {e.Message}");
        }
        finally
        {
            _currentTraceId = null;
            _spans.Clear();
        }
    }

    /// <summary>Begins an observation span for a single node phase.</summary>
    /// <returns>An opaque span key to pass to <see cref="EndNodeSpan"/>.</returns>
    public string? StartNodeSpan(string nodeName, string nodeId, string phase)
    {
        if (_currentTraceId == null) return null;
        try
        {
            var spanId  = Guid.NewGuid().ToString();
            var spanKey = $"{nodeId}_{phase}";
            var start   = DateTime.UtcNow;

            _spans[spanKey] = new SpanInfo(spanId, _currentTraceId, start);

            EnqueueEvent("observation-create", new
            {
                id        = spanId,
                traceId   = _currentTraceId,
                type      = "SPAN",
                name      = $"{nodeName}.{phase}",
                startTime = start.ToString("O"),
                metadata  = new { node_type = nodeName, node_id = nodeId, phase },
            });

            if (_config.Debug) Console.WriteLine($"✓ Started span: {spanKey}");
            return spanKey;
        }
        catch (Exception e)
        {
            if (_config.Debug) Console.WriteLine($"✗ Failed to start span: {e.Message}");
            return null;
        }
    }

    /// <summary>Closes a previously started node span.</summary>
    public void EndNodeSpan(
        string?    spanKey,
        object?    inputData  = null,
        object?    outputData = null,
        Exception? error      = null)
    {
        if (spanKey == null || !_spans.TryGetValue(spanKey, out var info)) return;
        try
        {
            var body = new Dictionary<string, object?>
            {
                ["id"]      = info.SpanId,
                ["traceId"] = info.TraceId,
                ["type"]    = "SPAN",
                ["endTime"] = DateTime.UtcNow.ToString("O"),
            };

            if (inputData  != null && _config.TraceInputs)  body["input"]  = SerializeData(inputData);
            if (outputData != null && _config.TraceOutputs) body["output"] = SerializeData(outputData);

            if (error != null && _config.TraceErrors)
            {
                body["level"]         = "ERROR";
                body["statusMessage"] = error.Message;
                body["metadata"]      = new
                {
                    error_type    = error.GetType().Name,
                    error_message = error.Message,
                    end_timestamp = DateTime.UtcNow.ToString("O"),
                };
            }
            else
            {
                body["level"]    = "DEFAULT";
                body["metadata"] = new { end_timestamp = DateTime.UtcNow.ToString("O") };
            }

            EnqueueEvent("observation-update", body);

            if (_config.Debug)
                Console.WriteLine($"✓ Ended span: {spanKey} [{(error == null ? "SUCCESS" : "ERROR")}]");
        }
        catch (Exception e)
        {
            if (_config.Debug) Console.WriteLine($"✗ Failed to end span: {e.Message}");
        }
        finally
        {
            _spans.Remove(spanKey);
        }
    }

    /// <summary>Sends all queued events to Langfuse synchronously.</summary>
    public void Flush()
    {
        if (_http == null || _pending.Count == 0) return;
        try
        {
            FlushAsync().GetAwaiter().GetResult();
        }
        catch (Exception e)
        {
            if (_config.Debug) Console.WriteLine($"✗ Flush failed: {e.Message}");
        }
    }

    /// <summary>Sends all queued events to Langfuse asynchronously.</summary>
    public async Task FlushAsync()
    {
        if (_http == null || _pending.Count == 0) return;

        var batch = _pending.ToList();
        _pending.Clear();

        try
        {
            var payload = new { batch };
            var json    = JsonSerializer.Serialize(payload, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _http.PostAsync("/api/public/ingestion", content);

            if (_config.Debug)
            {
                if (response.IsSuccessStatusCode)
                    Console.WriteLine($"✓ Flushed {batch.Count} event(s) to Langfuse");
                else
                {
                    var body = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"✗ Langfuse API error {(int)response.StatusCode}: {body}");
                }
            }
        }
        catch (Exception e)
        {
            if (_config.Debug) Console.WriteLine($"✗ FlushAsync failed: {e.Message}");
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void EnqueueEvent(string type, object body) =>
        _pending.Add(new
        {
            id        = Guid.NewGuid().ToString(),
            type,
            timestamp = DateTime.UtcNow.ToString("O"),
            body,
        });

    private static object? SerializeData(object? data)
    {
        if (data == null) return null;
        try
        {
            return data switch
            {
                string or int or long or double or float or bool or decimal => data,
                IDictionary<string, object> => data,
                IEnumerable<object> => data,
                _ => new { _type = data.GetType().Name, _data = data.ToString() },
            };
        }
        catch
        {
            return new { _type = "unknown", _data = "<serialization_failed>" };
        }
    }

    // ── IDisposable ───────────────────────────────────────────────────────────
    public void Dispose()
    {
        if (_disposed) return;
        Flush();
        _http?.Dispose();
        _disposed = true;
    }
}

