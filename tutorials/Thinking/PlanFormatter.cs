using System.Text;

namespace Thinking;

internal static class PlanFormatter
{
    /// <summary>
    /// Recursively formats a plan list for console output (full detail).
    /// Mirrors the Python <c>format_plan</c> helper.
    /// </summary>
    public static string FormatPlan(object? planItems, int indentLevel = 0)
    {
        var indent = new string(' ', indentLevel * 2);
        var sb = new StringBuilder();

        if (planItems is List<object> list)
        {
            foreach (var item in list)
            {
                if (item is Dictionary<object, object> dict)
                {
                    var status = dict.TryGetValue("status", out var s) ? s?.ToString() ?? "Unknown" : "Unknown";
                    var desc   = dict.TryGetValue("description", out var d) ? d?.ToString() ?? "No description" : "No description";
                    var result = dict.TryGetValue("result", out var r) ? r?.ToString() : null;
                    var mark   = dict.TryGetValue("mark",   out var m) ? m?.ToString() : null;

                    var line = $"{indent}- [{status}] {desc}";
                    if (!string.IsNullOrEmpty(result)) line += $": {result}";
                    if (!string.IsNullOrEmpty(mark))   line += $" ({mark})";
                    sb.AppendLine(line);

                    if (dict.TryGetValue("sub_steps", out var subSteps) && subSteps != null)
                        sb.AppendLine(FormatPlan(subSteps, indentLevel + 1));
                }
                else if (item is string str)
                    sb.AppendLine($"{indent}- {str}");
                else
                    sb.AppendLine($"{indent}- {item}");
            }
        }
        else if (planItems is string s2)
            sb.AppendLine($"{indent}{s2}");
        else
            sb.AppendLine($"{indent}# Invalid plan format: {planItems?.GetType().Name ?? "null"}");

        return sb.ToString().TrimEnd('\n', '\r');
    }

    /// <summary>
    /// Recursively formats a plan list for inclusion in the LLM prompt (simplified view).
    /// Mirrors the Python <c>format_plan_for_prompt</c> helper.
    /// </summary>
    public static string FormatPlanForPrompt(object? planItems, int indentLevel = 0)
    {
        var indent = new string(' ', indentLevel * 2);
        var sb = new StringBuilder();

        if (planItems is List<object> list)
        {
            foreach (var item in list)
            {
                if (item is Dictionary<object, object> dict)
                {
                    var status = dict.TryGetValue("status", out var s) ? s?.ToString() ?? "Unknown" : "Unknown";
                    var desc   = dict.TryGetValue("description", out var d) ? d?.ToString() ?? "No description" : "No description";
                    sb.AppendLine($"{indent}- [{status}] {desc}");

                    if (dict.TryGetValue("sub_steps", out var subSteps) && subSteps != null)
                        sb.AppendLine(FormatPlanForPrompt(subSteps, indentLevel + 1));
                }
                else
                    sb.AppendLine($"{indent}- {item}");
            }
        }
        else
            sb.AppendLine($"{indent}{planItems}");

        return sb.ToString().TrimEnd('\n', '\r');
    }
}