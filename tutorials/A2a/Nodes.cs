using System.Text.RegularExpressions;
using PocketFlow;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace A2a;

// ── Helpers ───────────────────────────────────────────────────────────────────

internal static class YamlHelper
{
    private static readonly IDeserializer Deserializer =
        new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

    public static Dictionary<string, object> ParseYaml(string response)
    {
        try
        {
            var block = ExtractYamlBlock(response);
            return Deserializer.Deserialize<Dictionary<string, object>>(block)
                   ?? new Dictionary<string, object>();
        }
        catch
        {
            return new Dictionary<string, object>();
        }
    }

    private static string ExtractYamlBlock(string text)
    {
        var match = Regex.Match(text, @"```yaml\s*(.*?)\s*```",
            RegexOptions.IgnoreCase | RegexOptions.Singleline);
        return match.Success ? match.Groups[1].Value.Trim() : text.Trim();
    }
}

// ── Node 1 – ExtractInfoNode ──────────────────────────────────────────────────

/// <summary>
/// Extracts structured expense information (type, amount, description, date)
/// from the user's free-text message using the LLM.
/// </summary>
public class ExtractInfoNode : Node
{
    protected override object? Prepare(object shared)
    {
        var store = (Dictionary<string, object>)shared;
        return store.TryGetValue("user_message", out var msg) ? (string)msg : string.Empty;
    }

    protected override object? Execute(object? prepRes)
    {
        var userMessage = (string)prepRes!;

        var prompt = $"""
            You are an AI assistant that helps process expense reimbursement requests.
            Extract the following information from the expense request:
            - expense_type: The type of expense (travel, meal, equipment, other)
            - amount: The amount in USD (as a number)
            - description: A brief description
            - date: The date if mentioned

            Expense request: {userMessage}

            Return as YAML:
            ```yaml
            expense_type: ...
            amount: ...
            description: ...
            date: ...
            ```
            """;

        var response = OllamaConnector.CallLlm(prompt);
        return YamlHelper.ParseYaml(response);
    }

    protected override object? Post(object shared, object? prepRes, object? execRes)
    {
        var store = (Dictionary<string, object>)shared;
        var info  = (Dictionary<string, object>)execRes!;

        store["expense_info"] = info;

        var expenseType = info.TryGetValue("expense_type", out var t)
            ? t?.ToString()?.ToLower() ?? "other"
            : "other";

        return expenseType is "travel" or "meal" or "equipment"
            ? "classify"
            : "respond";
    }
}

// ── Node 2 – ClassifyExpenseNode ──────────────────────────────────────────────

/// <summary>
/// Determines whether the expense is a valid business expense and whether it
/// requires manager approval based on per-category thresholds.
/// </summary>
public class ClassifyExpenseNode : Node
{
    protected override object? Prepare(object shared)
    {
        var store = (Dictionary<string, object>)shared;
        return store.TryGetValue("expense_info", out var info)
            ? (Dictionary<string, object>)info
            : new Dictionary<string, object>();
    }

    protected override object? Execute(object? prepRes)
    {
        var info        = (Dictionary<string, object>)prepRes!;
        var expenseType = info.TryGetValue("expense_type", out var t) ? t?.ToString() : "other";
        var amount      = info.TryGetValue("amount", out var a)
            ? Convert.ToDecimal(a?.ToString() ?? "0")
            : 0m;

        var prompt = $"""
            Classify this expense request:
            Type: {expenseType}
            Amount: ${amount}

            Determine:
            1. Is this a valid business expense? (yes/no)
            2. Does it require manager approval?
               (yes if amount > $100 for meals, > $500 for travel, > $200 for equipment)

            Return as YAML:
            ```yaml
            valid: true/false
            requires_approval: true/false
            reason: ...
            ```
            """;

        var response = OllamaConnector.CallLlm(prompt);
        return YamlHelper.ParseYaml(response);
    }

    protected override object? Post(object shared, object? prepRes, object? execRes)
    {
        var store          = (Dictionary<string, object>)shared;
        var classification = (Dictionary<string, object>)execRes!;

        store["classification"] = classification;

        var valid = classification.TryGetValue("valid", out var v) && v is true or "true";
        return valid ? "check_policy" : "respond";
    }
}

// ── Node 3 – CheckPolicyNode ──────────────────────────────────────────────────

/// <summary>
/// Applies company policy to determine whether the expense should be
/// auto-approved, requires further review, or is rejected.
/// </summary>
public class CheckPolicyNode : Node
{
    protected override object? Prepare(object shared)
    {
        var store = (Dictionary<string, object>)shared;
        var info  = store.TryGetValue("expense_info", out var i)
            ? (Dictionary<string, object>)i : new();
        var cls   = store.TryGetValue("classification", out var c)
            ? (Dictionary<string, object>)c : new();
        return (info, cls);
    }

    protected override object? Execute(object? prepRes)
    {
        var (expenseInfo, classification) = ((Dictionary<string, object>, Dictionary<string, object>))prepRes!;

        var requiresApproval = classification.TryGetValue("requires_approval", out var ra)
                               && ra is true or "true";

        if (!requiresApproval)
        {
            return new Dictionary<string, object>
            {
                ["status"]          = "approved",
                ["policy_notes"]    = "Auto-approved: within limits",
                ["action_required"] = "none",
            };
        }

        var expenseType = expenseInfo.TryGetValue("expense_type", out var t) ? t?.ToString() : "other";
        var amount      = expenseInfo.TryGetValue("amount", out var a)
            ? Convert.ToDecimal(a?.ToString() ?? "0") : 0m;

        var prompt = $"""
            This expense requires manager approval:
            Type: {expenseType}
            Amount: ${amount}

            Provide a policy check response:
            Return as YAML:
            ```yaml
            status: approved/rejected/more_info
            policy_notes: ...
            action_required: ...
            ```
            """;

        var response = OllamaConnector.CallLlm(prompt);
        return YamlHelper.ParseYaml(response);
    }

    protected override object? Post(object shared, object? prepRes, object? execRes)
    {
        var store       = (Dictionary<string, object>)shared;
        var policyCheck = (Dictionary<string, object>)execRes!;

        store["policy_check"] = policyCheck;

        return policyCheck.TryGetValue("status", out var s)
            ? s?.ToString() ?? "more_info"
            : "more_info";
    }
}

// ── Node 4 – PrepareResponseNode ──────────────────────────────────────────────

/// <summary>
/// Produces a clear, friendly natural-language response summarising the
/// outcome of the expense request.
/// </summary>
public class PrepareResponseNode : Node
{
    protected override object? Prepare(object shared)
    {
        var store = (Dictionary<string, object>)shared;
        return new Dictionary<string, object?>
        {
            ["expense_info"]   = store.TryGetValue("expense_info",   out var ei) ? ei : new Dictionary<string, object>(),
            ["classification"] = store.TryGetValue("classification", out var cl) ? cl : new Dictionary<string, object>(),
            ["policy_check"]   = store.TryGetValue("policy_check",   out var pc) ? pc : new Dictionary<string, object>(),
        };
    }

    protected override object? Execute(object? prepRes)
    {
        var context = System.Text.Json.JsonSerializer.Serialize(prepRes, A2aProtocol.A2aJsonOptions.Default);

        var prompt = $"""
            Prepare a clear, friendly response for an expense reimbursement request.

            Context: {context}

            Write a concise, helpful response in plain English explaining:
            1. Whether the expense is approved / rejected / needs more info
            2. The reasoning
            3. Any next steps required

            Response:
            """;

        return OllamaConnector.CallLlm(prompt);
    }

    protected override object? Post(object shared, object? prepRes, object? execRes)
    {
        var store = (Dictionary<string, object>)shared;
        store["response"] = execRes?.ToString() ?? string.Empty;
        return "respond";
    }
}

