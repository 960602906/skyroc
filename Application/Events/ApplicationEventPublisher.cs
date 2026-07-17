using Microsoft.Extensions.DependencyInjection;

namespace Application.Events;

/// <summary>
///     基于 DI 的同步应用事件发布器。
/// </summary>
public sealed class ApplicationEventPublisher(IServiceProvider serviceProvider) : IApplicationEventPublisher
{
    /// <inheritdoc />
    public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : IApplicationEvent
    {
        ArgumentNullException.ThrowIfNull(@event);
        var handlers = serviceProvider.GetServices<IApplicationEventHandler<TEvent>>().ToList();
        if (handlers.Count == 0)
        {
            throw new InvalidOperationException(
                $"未注册应用事件处理器: {typeof(TEvent).FullName}");
        }

        foreach (var handler in handlers)
        {
            await handler.HandleAsync(@event, cancellationToken);
        }
    }
}
