using System;
using Cysharp.Threading.Tasks;
using MessagePipe;

namespace com.aqua.system
{
    /// <summary>
    /// Manages all puzzle-related events
    /// Separates event concerns from business logic
    /// </summary>
    public interface IEventBus
    {
        UniTask PublishAsync<TEvent>(TEvent message);
        IDisposable Subscribe<TEvent>(IAsyncMessageHandler<TEvent> handler);

        UniTask PublishAsync<TTopic, TEvent>(TTopic topic, TEvent message);
        IDisposable Subscribe<TTopic, TEvent>(TTopic topic, IAsyncMessageHandler<TEvent> handler);
    }
}
