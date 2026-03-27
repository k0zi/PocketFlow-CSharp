# A2A – Agent-to-Agent Protocol Example

This project demonstrates an **Agent-to-Agent (A2A) expense-reimbursement agent** built with PocketFlow and ASP.NET Core, implementing the Google A2A JSON-RPC protocol.

## What is A2A?

The **Agent-to-Agent (A2A) protocol** is an open standard that lets AI agents discover one another and exchange tasks over HTTP/JSON-RPC. Key concepts:

| Concept | Description |
|---------|-------------|
| **AgentCard** | JSON descriptor served at `GET /.well-known/agent.json` – advertises the agent's name, capabilities, and skills |
| **Task** | A unit of work with a unique ID, status lifecycle (`submitted → working → completed/failed`), and result artifacts |
| **JSON-RPC** | All requests are `POST /` with a JSON-RPC 2.0 envelope |
| **SSE Streaming** | Long-running tasks can stream `TaskStatusUpdateEvent` / `TaskArtifactUpdateEvent` via Server-Sent Events |

## Architecture

```
Program.cs
    ├── Server mode → A2aServer (ASP.NET Core minimal API)
    │       └── AgentTaskManager  (implements InMemoryTaskManagerBase)
    │               └── ExpenseFlow (PocketFlow pipeline)
    │                       ├── ExtractInfoNode
    │                       ├── ClassifyExpenseNode
    │                       ├── CheckPolicyNode
    │                       └── PrepareResponseNode
    └── Client mode → A2aClient (polls server via HTTP)
```

### Shared Infrastructure (SharedUtils/A2a/)

| File | Description |
|------|-------------|
| `A2aTypes.cs` | All A2A protocol types (`AgentCard`, `A2aTask`, `Part`, etc.) |
| `A2aJsonOptions.cs` | Shared `JsonSerializerOptions` (camelCase, null-ignore, enum strings) |
| `InMemoryCache.cs` | Generic thread-safe in-memory cache |
| `PushNotificationAuth.cs` | RSA/JWT push-notification signing & verification |
| `TaskManagerBase.cs` | Abstract `A2aTaskManagerBase` interface |
| `InMemoryTaskManagerBase.cs` | Default in-memory `get / cancel / push-notification` implementation |
| `Client/A2aClient.cs` | HTTP client (send, get, cancel, SSE streaming) |
| `Client/CardResolver.cs` | Fetches `AgentCard` from `/.well-known/agent.json` |

### Application Layer (A2a/)

| File | Description |
|------|-------------|
| `Program.cs` | Entry point – `--server` or `--client` mode |
| `Server/A2aServer.cs` | ASP.NET Core minimal API that routes JSON-RPC to the task manager |
| `AgentTaskManager.cs` | Runs the PocketFlow flow per task; streams SSE updates |
| `Flow.cs` | Wires the expense-reimbursement PocketFlow nodes |
| `Nodes.cs` | `ExtractInfoNode`, `ClassifyExpenseNode`, `CheckPolicyNode`, `PrepareResponseNode` |

## Flow Description

```
ExtractInfo
    ├─ classify  →  ClassifyExpense
    │                  ├─ check_policy  →  CheckPolicy
    │                  │                      ├─ approved   →  PrepareResponse
    │                  │                      ├─ rejected   →  PrepareResponse
    │                  │                      └─ more_info  →  PrepareResponse
    │                  └─ respond       →  PrepareResponse
    └─ respond   →  PrepareResponse
```

1. **ExtractInfoNode** – LLM extracts `expense_type`, `amount`, `description`, `date` from the user message.
2. **ClassifyExpenseNode** – LLM decides whether the expense is valid and whether manager approval is needed (thresholds: $100 meals, $500 travel, $200 equipment).
3. **CheckPolicyNode** – If approval is required, LLM performs a policy check; otherwise auto-approves within-limit expenses.
4. **PrepareResponseNode** – LLM generates a friendly plain-English summary of the outcome.

## Running

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Ollama](https://ollama.com) running locally with `gemma3:latest` (or set `OLLAMA_MODEL`)

### Start the server

```bash
dotnet run --project A2a -- --server --host 0.0.0.0 --port 10002
```

The agent card will be served at:  
`http://localhost:10002/.well-known/agent.json`

### Run the interactive client

In a separate terminal:

```bash
dotnet run --project A2a -- --client --url http://localhost:10002
```

Then type expense requests, e.g.:

```
Describe your expense: I need reimbursement for a $45 team lunch yesterday
```

### Environment Variables

| Variable | Default | Description |
|----------|---------|-------------|
| `OLLAMA_HOST` | `http://localhost:11434` | Ollama API endpoint |
| `OLLAMA_MODEL` | `gemma3:latest` | LLM model to use |

## A2A Protocol Quick Reference

### Discover the agent

```bash
curl http://localhost:10002/.well-known/agent.json
```

### Send a task

```bash
curl -X POST http://localhost:10002/ \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "id": 1,
    "method": "tasks/send",
    "params": {
      "id": "task-001",
      "message": {
        "role": "user",
        "parts": [{"type": "text", "text": "Reimburse $350 flight to NYC for client meeting"}]
      }
    }
  }'
```

### Poll for result

```bash
curl -X POST http://localhost:10002/ \
  -H "Content-Type: application/json" \
  -d '{"jsonrpc":"2.0","id":2,"method":"tasks/get","params":{"id":"task-001"}}'
```

### Stream updates (SSE)

```bash
curl -X POST http://localhost:10002/ \
  -H "Content-Type: application/json" \
  -H "Accept: text/event-stream" \
  -d '{"jsonrpc":"2.0","id":3,"method":"tasks/send","params":{...}}'
```

## Python → C# Conversion Map

| Python file | C# equivalent |
|------------|---------------|
| `common/types.py` | `SharedUtils/A2a/A2aTypes.cs` |
| `common/utils/in_memory_cache.py` | `SharedUtils/A2a/InMemoryCache.cs` |
| `common/utils/push_notification_auth.py` | `SharedUtils/A2a/PushNotificationAuth.cs` |
| `common/server/task_manager.py` | `SharedUtils/A2a/TaskManagerBase.cs` |
| `common/server/server.py` | `A2a/Server/A2aServer.cs` |
| `common/client/card_resolver.py` | `SharedUtils/A2a/Client/CardResolver.cs` |
| `common/client/client.py` | `SharedUtils/A2a/Client/A2aClient.cs` |
| `task_manager.py` | `SharedUtils/A2a/InMemoryTaskManagerBase.cs` |
| `nodes.py` | `A2a/Nodes.cs` |
| `flow.py` | `A2a/Flow.cs` |
| `a2a_server.py` | `A2a/AgentTaskManager.cs` |
| `a2a_client.py` | `A2a/Program.cs` (client branch) |
| `main.py` | `A2a/Program.cs` (entry point) |
| `utils.py` | `SharedUtils/OllamaConnector.cs` (existing) |

