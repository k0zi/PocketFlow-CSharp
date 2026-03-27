using System.Text.Json;
using System.Text.Json.Serialization;

namespace A2aProtocol;

// ── Task State Enum ───────────────────────────────────────────────────────────

[JsonConverter(typeof(JsonStringEnumConverter<TaskState>))]
public enum TaskState
{
    [JsonStringEnumMemberName("submitted")]   Submitted,
    [JsonStringEnumMemberName("working")]     Working,
    [JsonStringEnumMemberName("input-required")] InputRequired,
    [JsonStringEnumMemberName("completed")]   Completed,
    [JsonStringEnumMemberName("canceled")]    Canceled,
    [JsonStringEnumMemberName("failed")]      Failed,
    [JsonStringEnumMemberName("unknown")]     Unknown,
}

// ── Content Parts (polymorphic, discriminated by "type") ──────────────────────

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(TextPart), "text")]
[JsonDerivedType(typeof(ImagePart), "image")]
[JsonDerivedType(typeof(FilePart), "file")]
public abstract class Part
{
    [JsonPropertyName("metadata")]
    public Dictionary<string, JsonElement>? Metadata { get; set; }
}

public class TextPart : Part
{
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;
}

public class ImageData
{
    [JsonPropertyName("mimeType")]
    public string MimeType { get; set; } = string.Empty;

    /// <summary>Base-64 encoded image bytes.</summary>
    [JsonPropertyName("data")]
    public string Data { get; set; } = string.Empty;
}

public class ImagePart : Part
{
    [JsonPropertyName("image")]
    public ImageData Image { get; set; } = new();
}

public class FileData
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("mimeType")]
    public string? MimeType { get; set; }

    [JsonPropertyName("uri")]
    public string? Uri { get; set; }

    /// <summary>Base-64 encoded file bytes (optional, alternative to Uri).</summary>
    [JsonPropertyName("data")]
    public string? Data { get; set; }
}

public class FilePart : Part
{
    [JsonPropertyName("file")]
    public FileData File { get; set; } = new();
}

// ── Message ───────────────────────────────────────────────────────────────────

public class A2aMessage
{
    /// <summary>"user" or "agent"</summary>
    [JsonPropertyName("role")]
    public string Role { get; set; } = "user";

    [JsonPropertyName("parts")]
    public List<Part> Parts { get; set; } = new();

    [JsonPropertyName("metadata")]
    public Dictionary<string, JsonElement>? Metadata { get; set; }
}

// ── Task Status ───────────────────────────────────────────────────────────────

public class A2aTaskStatus
{
    [JsonPropertyName("state")]
    public TaskState State { get; set; }

    [JsonPropertyName("message")]
    public A2aMessage? Message { get; set; }

    [JsonPropertyName("timestamp")]
    public string Timestamp { get; set; } = DateTime.UtcNow.ToString("o");
}

// ── Artifact ──────────────────────────────────────────────────────────────────

public class Artifact
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("parts")]
    public List<Part> Parts { get; set; } = new();

    [JsonPropertyName("index")]
    public int Index { get; set; }

    [JsonPropertyName("append")]
    public bool? Append { get; set; }

    [JsonPropertyName("lastChunk")]
    public bool? LastChunk { get; set; }

    [JsonPropertyName("metadata")]
    public Dictionary<string, JsonElement>? Metadata { get; set; }
}

// ── Task ──────────────────────────────────────────────────────────────────────

/// <summary>
/// A2A protocol Task object.  Named <c>A2aTask</c> to avoid collision with
/// <see cref="System.Threading.Tasks.Task"/>.
/// </summary>
public class A2aTask
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("sessionId")]
    public string? SessionId { get; set; }

    [JsonPropertyName("status")]
    public A2aTaskStatus Status { get; set; } = new();

    [JsonPropertyName("artifacts")]
    public List<Artifact>? Artifacts { get; set; }

    [JsonPropertyName("history")]
    public List<A2aMessage>? History { get; set; }

    [JsonPropertyName("metadata")]
    public Dictionary<string, JsonElement>? Metadata { get; set; }
}

// ── Request Parameters ────────────────────────────────────────────────────────

public class TaskIdParams
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("metadata")]
    public Dictionary<string, JsonElement>? Metadata { get; set; }
}

public class TaskQueryParams : TaskIdParams
{
    [JsonPropertyName("historyLength")]
    public int? HistoryLength { get; set; }
}

public class AuthenticationInfo
{
    [JsonPropertyName("schemes")]
    public List<string> Schemes { get; set; } = new();

    [JsonPropertyName("credentials")]
    public string? Credentials { get; set; }
}

public class PushNotificationConfig
{
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("token")]
    public string? Token { get; set; }

    [JsonPropertyName("authentication")]
    public AuthenticationInfo? Authentication { get; set; }
}

public class TaskSendConfiguration
{
    [JsonPropertyName("acceptedOutputModes")]
    public List<string>? AcceptedOutputModes { get; set; }

    [JsonPropertyName("historyLength")]
    public int? HistoryLength { get; set; }

    [JsonPropertyName("pushNotificationConfig")]
    public PushNotificationConfig? PushNotificationConfig { get; set; }
}

public class TaskSendParams : TaskIdParams
{
    [JsonPropertyName("sessionId")]
    public string? SessionId { get; set; }

    [JsonPropertyName("message")]
    public A2aMessage Message { get; set; } = new();

    [JsonPropertyName("configuration")]
    public TaskSendConfiguration? Configuration { get; set; }

    [JsonPropertyName("pushNotification")]
    public PushNotificationConfig? PushNotification { get; set; }
}

// ── JSON-RPC Base Types ───────────────────────────────────────────────────────

public class JsonRpcError
{
    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    public JsonElement? Data { get; set; }
}

// ── Strongly-typed Request / Response pairs ───────────────────────────────────

public class SendTaskRequest
{
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; set; } = "2.0";

    [JsonPropertyName("id")]
    public JsonElement? Id { get; set; }

    [JsonPropertyName("method")]
    public string Method { get; set; } = "tasks/send";

    [JsonPropertyName("params")]
    public TaskSendParams Params { get; set; } = new();
}

public class SendTaskResponse
{
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; set; } = "2.0";

    [JsonPropertyName("id")]
    public JsonElement? Id { get; set; }

    [JsonPropertyName("result")]
    public A2aTask? Result { get; set; }

    [JsonPropertyName("error")]
    public JsonRpcError? Error { get; set; }
}

public class GetTaskRequest
{
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; set; } = "2.0";

    [JsonPropertyName("id")]
    public JsonElement? Id { get; set; }

    [JsonPropertyName("method")]
    public string Method { get; set; } = "tasks/get";

    [JsonPropertyName("params")]
    public TaskQueryParams Params { get; set; } = new();
}

public class GetTaskResponse
{
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; set; } = "2.0";

    [JsonPropertyName("id")]
    public JsonElement? Id { get; set; }

    [JsonPropertyName("result")]
    public A2aTask? Result { get; set; }

    [JsonPropertyName("error")]
    public JsonRpcError? Error { get; set; }
}

public class CancelTaskRequest
{
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; set; } = "2.0";

    [JsonPropertyName("id")]
    public JsonElement? Id { get; set; }

    [JsonPropertyName("method")]
    public string Method { get; set; } = "tasks/cancel";

    [JsonPropertyName("params")]
    public TaskIdParams Params { get; set; } = new();
}

public class CancelTaskResponse
{
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; set; } = "2.0";

    [JsonPropertyName("id")]
    public JsonElement? Id { get; set; }

    [JsonPropertyName("result")]
    public A2aTask? Result { get; set; }

    [JsonPropertyName("error")]
    public JsonRpcError? Error { get; set; }
}

// ── Streaming / SSE Events ────────────────────────────────────────────────────

public class TaskStatusUpdateEvent
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public A2aTaskStatus Status { get; set; } = new();

    [JsonPropertyName("final")]
    public bool Final { get; set; }

    [JsonPropertyName("metadata")]
    public Dictionary<string, JsonElement>? Metadata { get; set; }
}

public class TaskArtifactUpdateEvent
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("artifact")]
    public Artifact Artifact { get; set; } = new();

    [JsonPropertyName("metadata")]
    public Dictionary<string, JsonElement>? Metadata { get; set; }
}

public class SendTaskStreamingResponse
{
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; set; } = "2.0";

    [JsonPropertyName("id")]
    public JsonElement? Id { get; set; }

    /// <summary>
    /// Either a <see cref="TaskStatusUpdateEvent"/> or a <see cref="TaskArtifactUpdateEvent"/>.
    /// </summary>
    [JsonPropertyName("result")]
    public object? Result { get; set; }

    [JsonPropertyName("error")]
    public JsonRpcError? Error { get; set; }
}

// ── Push-notification Types ───────────────────────────────────────────────────

public class TaskPushNotificationConfig
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("pushNotificationConfig")]
    public PushNotificationConfig PushNotificationConfig { get; set; } = new();
}

public class SetTaskPushNotificationRequest
{
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; set; } = "2.0";

    [JsonPropertyName("id")]
    public JsonElement? Id { get; set; }

    [JsonPropertyName("params")]
    public TaskPushNotificationConfig Params { get; set; } = new();
}

public class SetTaskPushNotificationResponse
{
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; set; } = "2.0";

    [JsonPropertyName("id")]
    public JsonElement? Id { get; set; }

    [JsonPropertyName("result")]
    public TaskPushNotificationConfig? Result { get; set; }

    [JsonPropertyName("error")]
    public JsonRpcError? Error { get; set; }
}

public class GetTaskPushNotificationRequest
{
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; set; } = "2.0";

    [JsonPropertyName("id")]
    public JsonElement? Id { get; set; }

    [JsonPropertyName("params")]
    public TaskIdParams Params { get; set; } = new();
}

public class GetTaskPushNotificationResponse
{
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; set; } = "2.0";

    [JsonPropertyName("id")]
    public JsonElement? Id { get; set; }

    [JsonPropertyName("result")]
    public TaskPushNotificationConfig? Result { get; set; }

    [JsonPropertyName("error")]
    public JsonRpcError? Error { get; set; }
}

public class TaskResubscriptionRequest
{
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; set; } = "2.0";

    [JsonPropertyName("id")]
    public JsonElement? Id { get; set; }

    [JsonPropertyName("params")]
    public TaskIdParams Params { get; set; } = new();
}

// ── Well-known Error Factories ────────────────────────────────────────────────

public static class A2aErrors
{
    public static JsonRpcError TaskNotFound()
        => new() { Code = -32001, Message = "Task not found" };

    public static JsonRpcError TaskNotCancelable()
        => new() { Code = -32002, Message = "Task cannot be canceled" };

    public static JsonRpcError PushNotificationNotSupported()
        => new() { Code = -32003, Message = "Push Notification is not supported" };

    public static JsonRpcError UnsupportedOperation()
        => new() { Code = -32004, Message = "This operation is not supported" };

    public static JsonRpcError ContentTypeNotSupported()
        => new() { Code = -32005, Message = "Unsupported content type" };

    public static JsonRpcError InternalError(string? detail = null)
        => new() { Code = -32603, Message = detail is null ? "Internal error" : $"Internal error: {detail}" };
}

// ── Agent Card (capability advertisement) ─────────────────────────────────────

public class AgentCapabilities
{
    [JsonPropertyName("streaming")]
    public bool Streaming { get; set; }

    [JsonPropertyName("pushNotifications")]
    public bool PushNotifications { get; set; }

    [JsonPropertyName("stateTransitionHistory")]
    public bool StateTransitionHistory { get; set; }
}

public class AgentAuthentication
{
    [JsonPropertyName("schemes")]
    public List<string> Schemes { get; set; } = new();

    [JsonPropertyName("credentials")]
    public string? Credentials { get; set; }
}

public class AgentProvider
{
    [JsonPropertyName("organization")]
    public string Organization { get; set; } = string.Empty;

    [JsonPropertyName("url")]
    public string? Url { get; set; }
}

public class AgentSkillInfo
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("tags")]
    public List<string>? Tags { get; set; }

    [JsonPropertyName("examples")]
    public List<string>? Examples { get; set; }

    [JsonPropertyName("inputModes")]
    public List<string>? InputModes { get; set; }

    [JsonPropertyName("outputModes")]
    public List<string>? OutputModes { get; set; }
}

public class AgentCard
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("provider")]
    public AgentProvider? Provider { get; set; }

    [JsonPropertyName("version")]
    public string Version { get; set; } = "1.0";

    [JsonPropertyName("documentationUrl")]
    public string? DocumentationUrl { get; set; }

    [JsonPropertyName("capabilities")]
    public AgentCapabilities Capabilities { get; set; } = new();

    [JsonPropertyName("authentication")]
    public AgentAuthentication? Authentication { get; set; }

    [JsonPropertyName("defaultInputModes")]
    public List<string> DefaultInputModes { get; set; } = ["text"];

    [JsonPropertyName("defaultOutputModes")]
    public List<string> DefaultOutputModes { get; set; } = ["text"];

    [JsonPropertyName("skills")]
    public List<AgentSkillInfo> Skills { get; set; } = new();
}

