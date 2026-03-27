using PocketFlow;

namespace AgentSkills;

/// <summary>
/// Injects the selected skill instructions into the LLM prompt and runs the task.
/// Mirrors ApplySkill in nodes.py.
/// </summary>
public class ApplySkillNode : Node
{
    protected override object? Prepare(object shared)
    {
        var store = (Dictionary<string, object>)shared;
        return (
            task:         (string)store["task"],
            skillName:    (string)store["selected_skill"],
            skillContent: (string)store["selected_skill_content"]
        );
    }

    protected override object? Execute(object? prepRes)
    {
        var (task, skillName, skillContent) = ((string, string, string))prepRes!;

        var prompt = $"""
                      You are running an Agent Skill.

                      Skill name: {skillName}

                      Skill instructions:
                      ---
                      {skillContent}
                      ---

                      User task:
                      {task}

                      Follow the skill instructions exactly and return the final result only.
                      """.Trim();

        return OllamaConnector.CallLlm(prompt);
    }

    protected override object? Post(object shared, object? prepRes, object? execRes)
    {
        var store = (Dictionary<string, object>)shared;
        store["result"] = (string)execRes!;
        return "default";
    }
}