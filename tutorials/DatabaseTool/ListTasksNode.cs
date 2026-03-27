using PocketFlow;

namespace DatabaseTool;

/// <summary>
/// Queries all rows from the <c>tasks</c> table and stores them in shared state.
/// Port of <c>ListTasksNode</c> in nodes.py.
/// </summary>
public class ListTasksNode : Node
{
    protected override object? Prepare(object shared)
    {
        var store = (Dictionary<string, object>)shared;
        return (string)store["db_path"];
    }

    protected override object? Execute(object? prepRes)
    {
        var dbPath = (string)prepRes!;
        return DatabaseUtils.ExecuteSql(dbPath, "SELECT * FROM tasks");
    }

    protected override object? Post(object shared, object? prepRes, object? execRes)
    {
        var store = (Dictionary<string, object>)shared;
        store["tasks"] = (List<object?[]>)execRes!;
        return "default";
    }
}

