# SQLite Database Tool — PocketFlow C# Port

This example demonstrates integrating SQLite database operations with PocketFlow in C#.
It is a direct port of the Python `pocketflow-tool-database` cookbook example.

## What it does

1. **Initialises** a local SQLite database (`example.db`) with a `tasks` table.
2. **Creates** an example task row via a parameterised `INSERT`.
3. **Lists** every task stored in the table and prints them to the console.

## Project structure

```
DatabaseTool/
├── InitDatabaseNode.cs   # Creates the tasks table (port of InitDatabaseNode in nodes.py)
├── CreateTaskNode.cs     # Inserts a new task        (port of CreateTaskNode  in nodes.py)
├── ListTasksNode.cs      # Selects all tasks         (port of ListTasksNode   in nodes.py)
├── Program.cs            # Flow wiring + entry point (port of flow.py + main.py)
└── README.md
```

### Shared utilities

Database helpers live in the shared library so other projects can reuse them:

```
SharedUtils/
└── DatabaseUtils.cs      # ExecuteSql() + InitDb()   (port of tools/database.py)
```

## Key concepts

| Python concept | C# equivalent |
|---|---|
| `execute_sql(query, params)` | `DatabaseUtils.ExecuteSql(dbPath, query, parameters)` |
| `init_db()` | `DatabaseUtils.InitDb(dbPath)` |
| `Node.prep / exec / post` | `Node.Prepare / Execute / Post` |
| `Flow(start=…).run(shared)` | `new Flow(start: …).Run(shared)` |
| `shared` dict | `Dictionary<string, object>` |

## Running the example

```bash
dotnet run --project src/DatabaseTool
```

### Example output

```
Database Status: Database initialized
Task Status: Task created successfully

All Tasks:
- ID: 1
  Title: Example Task
  Description: This is an example task created using PocketFlow
  Status: pending
  Created: 2026-03-27 12:00:00
```

## Best practices demonstrated

- **SQL injection prevention** — all user-supplied values are passed as named parameters
  (`$title`, `$description`) via `SqliteCommand.Parameters`.
- **Connection hygiene** — every `SqliteConnection` is opened inside a `using` block.
- **Separation of concerns** — database I/O lives in `SharedUtils.DatabaseUtils`;
  PocketFlow orchestration lives in the node classes.
- **Shared-state pattern** — nodes communicate exclusively through the `shared` dictionary,
  matching the PocketFlow design contract.

