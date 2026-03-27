namespace Tracing;

/// <summary>
/// Configuration class for PocketFlow tracing with Langfuse.
/// Ported from tracing/config.py in the pocketflow-tracing cookbook.
/// </summary>
public class TracingConfig
{
    // ── Langfuse credentials ─────────────────────────────────────────────────
    public string? LangfuseSecretKey { get; set; }
    public string? LangfusePublicKey { get; set; }
    public string? LangfuseHost { get; set; }

    // ── Tracing options ──────────────────────────────────────────────────────
    public bool Debug { get; set; } = false;
    public bool TraceInputs { get; set; } = true;
    public bool TraceOutputs { get; set; } = true;
    public bool TracePrep { get; set; } = true;
    public bool TraceExec { get; set; } = true;
    public bool TracePost { get; set; } = true;
    public bool TraceErrors { get; set; } = true;

    // ── Session / user ───────────────────────────────────────────────────────
    public string? SessionId { get; set; }
    public string? UserId { get; set; }

    /// <summary>
    /// Creates a <see cref="TracingConfig"/> populated from environment variables.
    /// Reads the same variables as the Python <c>TracingConfig.from_env()</c>.
    /// </summary>
    public static TracingConfig FromEnv() => new()
    {
        LangfuseSecretKey = Environment.GetEnvironmentVariable("LANGFUSE_SECRET_KEY"),
        LangfusePublicKey = Environment.GetEnvironmentVariable("LANGFUSE_PUBLIC_KEY"),
        LangfuseHost      = Environment.GetEnvironmentVariable("LANGFUSE_HOST"),
        Debug          = GetBool("POCKETFLOW_TRACING_DEBUG",  false),
        TraceInputs    = GetBool("POCKETFLOW_TRACE_INPUTS",   true),
        TraceOutputs   = GetBool("POCKETFLOW_TRACE_OUTPUTS",  true),
        TracePrep      = GetBool("POCKETFLOW_TRACE_PREP",     true),
        TraceExec      = GetBool("POCKETFLOW_TRACE_EXEC",     true),
        TracePost      = GetBool("POCKETFLOW_TRACE_POST",     true),
        TraceErrors    = GetBool("POCKETFLOW_TRACE_ERRORS",   true),
        SessionId      = Environment.GetEnvironmentVariable("POCKETFLOW_SESSION_ID"),
        UserId         = Environment.GetEnvironmentVariable("POCKETFLOW_USER_ID"),
    };

    /// <summary>
    /// Returns <see langword="true"/> when all required Langfuse credentials are present.
    /// </summary>
    public bool Validate()
    {
        if (string.IsNullOrWhiteSpace(LangfuseSecretKey))
        {
            if (Debug) Console.WriteLine("Warning: LANGFUSE_SECRET_KEY not set");
            return false;
        }
        if (string.IsNullOrWhiteSpace(LangfusePublicKey))
        {
            if (Debug) Console.WriteLine("Warning: LANGFUSE_PUBLIC_KEY not set");
            return false;
        }
        if (string.IsNullOrWhiteSpace(LangfuseHost))
        {
            if (Debug) Console.WriteLine("Warning: LANGFUSE_HOST not set");
            return false;
        }
        return true;
    }

    /// <summary>
    /// Returns a dictionary of kwargs suitable for initialising a Langfuse client.
    /// </summary>
    public Dictionary<string, string> ToLangfuseArgs()
    {
        var args = new Dictionary<string, string>();
        if (!string.IsNullOrWhiteSpace(LangfuseSecretKey)) args["secret_key"] = LangfuseSecretKey!;
        if (!string.IsNullOrWhiteSpace(LangfusePublicKey)) args["public_key"] = LangfusePublicKey!;
        if (!string.IsNullOrWhiteSpace(LangfuseHost))      args["host"]       = LangfuseHost!;
        if (Debug)                                          args["debug"]      = "true";
        return args;
    }

    // ── helpers ──────────────────────────────────────────────────────────────
    private static bool GetBool(string key, bool defaultValue)
    {
        var val = Environment.GetEnvironmentVariable(key);
        if (string.IsNullOrWhiteSpace(val)) return defaultValue;
        return val.Trim().ToLowerInvariant() == "true";
    }
}

