/// <summary>
/// Setup and validation utilities for PocketFlow tracing with Langfuse.
/// Consolidated from <c>utils/setup.py</c> in the pocketflow-tracing cookbook.
/// </summary>
public static class TracingSetupUtils
{
    // ── Validation ────────────────────────────────────────────────────────────

    /// <summary>
    /// Checks that all required Langfuse environment variables are present.
    /// Prints a descriptive warning for any missing value.
    /// </summary>
    /// <returns><see langword="true"/> when all required variables are set.</returns>
    public static bool ValidateTracingEnvironment()
    {
        var secretKey = Environment.GetEnvironmentVariable("LANGFUSE_SECRET_KEY");
        var publicKey = Environment.GetEnvironmentVariable("LANGFUSE_PUBLIC_KEY");
        var host      = Environment.GetEnvironmentVariable("LANGFUSE_HOST");

        bool valid = !string.IsNullOrWhiteSpace(secretKey)
                  && !string.IsNullOrWhiteSpace(publicKey)
                  && !string.IsNullOrWhiteSpace(host);

        if (!valid)
        {
            Console.WriteLine("⚠️  Tracing configuration is incomplete. Missing:");
            if (string.IsNullOrWhiteSpace(secretKey)) Console.WriteLine("   - LANGFUSE_SECRET_KEY");
            if (string.IsNullOrWhiteSpace(publicKey)) Console.WriteLine("   - LANGFUSE_PUBLIC_KEY");
            if (string.IsNullOrWhiteSpace(host))      Console.WriteLine("   - LANGFUSE_HOST");
        }

        return valid;
    }

    // ── Help ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Prints a human-readable guide for configuring PocketFlow tracing.
    /// Mirrors <c>print_configuration_help()</c> from <c>utils/setup.py</c>.
    /// </summary>
    public static void PrintConfigurationHelp() =>
        Console.WriteLine("""
🔧 PocketFlow Tracing Configuration Help
─────────────────────────────────────────

To use PocketFlow tracing you need Langfuse credentials set as environment
variables (or in a .env file loaded before the process starts):

  Required:
    LANGFUSE_SECRET_KEY=sk-lf-...
    LANGFUSE_PUBLIC_KEY=pk-lf-...
    LANGFUSE_HOST=https://your-langfuse-host

  Optional:
    POCKETFLOW_TRACING_DEBUG=true
    POCKETFLOW_TRACE_INPUTS=true
    POCKETFLOW_TRACE_OUTPUTS=true
    POCKETFLOW_TRACE_PREP=true
    POCKETFLOW_TRACE_EXEC=true
    POCKETFLOW_TRACE_POST=true
    POCKETFLOW_TRACE_ERRORS=true
    POCKETFLOW_SESSION_ID=<session-id>
    POCKETFLOW_USER_ID=<user-id>

Get your credentials from: https://langfuse.com
""");
}

