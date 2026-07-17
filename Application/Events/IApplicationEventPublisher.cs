namespace Application.Events;

/// <summary>
///     应用事件发布器：同步调度已注册的 handler。
/// </summary>
public interface IApplicationEventPublisher
{
    /// <summary>
    ///     发布事件并顺序执行全部 handler；无 handler 时抛出异常，避免关键副作用被静默跳过。
    /// </summary>
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : IApplicationEvent;
}
