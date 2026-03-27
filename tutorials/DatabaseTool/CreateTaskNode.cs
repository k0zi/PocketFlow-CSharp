using PocketFlow;

namespace DatabaseTool;

/// <summary>
/// Inserts a new task row into the <c>tasks</c> table.
/// Port of <c>CreateTaskNode</c> in nodes.py.
/// </summary>
public class CreateTaskNode : Node
{
    protected override object? Prepare(object shared)
    {
        var store = (Dictionary<string, object>)shared;
        return (
            DbPath:      (string)store["db_path"],
            Title:       (string)store["task_title"],
            Description: (string)store["task_description"]
        );
    }

    protected override object? Execute(object? prepRes)
    {
        var (dbPath, title, description) = ((string DbPath, string Title, string Description))prepRes!;

        const string query = "INSERT INTO tasks (title, description) VALUES ($title, $description)";
        DatabaseUtils.ExecuteSql(dbPath, query,
        [
            ("$title",       title),
            ("$description", description),
        ]);

        return "Task created successfully";
    }

    protected override object? Post(object shared, object? prepRes, object? execRes)
    {
        var store = (Dictionary<string, object>)shared;
        store["task_status"] = (string)execRes!;
        return "default";
    }
}


