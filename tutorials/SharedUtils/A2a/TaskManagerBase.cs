using System.Runtime.CompilerServices;

namespace A2aProtocol;

/// <summary>
/// Abstract base class for A2A task managers.
/// Ported from <c>common/server/task_manager.py</c>.
/// </summary>
public abstract class A2aTaskManagerBase
{
    /// <summary>Retrieve the current state of a task.</summary>
    public abstract Task<GetTaskResponse> OnGetTaskAsync(GetTaskRequest request);

    /// <summary>Cancel a running task.</summary>
    public abstract Task<CancelTaskResponse> OnCancelTaskAsync(CancelTaskRequest request);

    /// <summary>Submit a new task (non-streaming).</summary>
    public abstract Task<SendTaskResponse> OnSendTaskAsync(SendTaskRequest request);

    /// <summary>Submit a new task and receive SSE streaming updates.</summary>
    public abstract IAsyncEnumerable<SendTaskStreamingResponse> OnSendTaskSubscribeAsync(
        SendTaskRequest    request,
        CancellationToken  cancellationToken = default);

    /// <summary>Register a push-notification callback URL for a task.</summary>
    public abstract Task<SetTaskPushNotificationResponse> OnSetTaskPushNotificationAsync(SetTaskPushNotificationRequest request);

    /// <summary>Retrieve the current push-notification config for a task.</summary>
    public abstract Task<GetTaskPushNotificationResponse> OnGetTaskPushNotificationAsync(GetTaskPushNotificationRequest request);

    /// <summary>Re-subscribe to SSE events for an existing task.</summary>
    public abstract IAsyncEnumerable<SendTaskStreamingResponse> OnResubscribeToTaskAsync(
        TaskResubscriptionRequest request,
        CancellationToken         cancellationToken = default);
}


