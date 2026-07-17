namespace Application.Events;

/// <summary>
///     应用事件处理器。
/// </summary>
/// <typeparam name="TEvent">事件类型。</typeparam>
public interface IApplicationEventHandler<in TEvent> where TEvent : IApplicationEvent
{
    /// <summary>
    ///     处理事件；异常将向上抛出并回滚当前事务（若在事务内发布）。
    /// </summary>
    Task HandleAsync(TEvent @event, CancellationToken cancellationToken = default);
}
