using PocketFlow;

namespace DatabaseTool;

/// <summary>
/// Initialises the SQLite database and creates the <c>tasks</c> table if needed.
/// Port of <c>InitDatabaseNode</c> in nodes.py.
/// </summary>
public class InitDatabaseNode : Node
{
    protected override object? Prepare(object shared)
    {
        var store = (Dictionary<string, object>)shared;
        return (string)store["db_path"];
    }

    protected override object? Execute(object? prepRes)
    {
        var dbPath = (string)prepRes!;
        DatabaseUtils.InitDb(dbPath);
        return "Database initialized";
    }

    protected override object? Post(object shared, object? prepRes, object? execRes)
    {
        var store = (Dictionary<string, object>)shared;
        store["db_status"] = (string)execRes!;
        return "default";
    }
}

