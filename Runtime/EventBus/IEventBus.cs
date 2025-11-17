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
        // Publish
        void Publish<TEvent>(TEvent message);
        UniTask PublishAsync<TEvent>(TEvent message);

        void Publish<TTopic, TEvent>(TTopic topic, TEvent message);
        UniTask PublishAsync<TTopic, TEvent>(TTopic topic, TEvent message);

        // Subscribe
        IDisposable Subscribe<TEvent>(IAsyncMessageHandler<TEvent> handler);
        IDisposable Subscribe<TTopic, TEvent>(TTopic topic, IAsyncMessageHandler<TEvent> handler);

        IDisposable Subscribe<TEvent>(Action<TEvent> handler);
        IDisposable Subscribe<TEvent>(Func<TEvent, UniTask> handler);

        IDisposable Subscribe<TTopic, TEvent>(TTopic topic, Action<TEvent> handler);
        IDisposable Subscribe<TTopic, TEvent>(TTopic topic, Func<TEvent, UniTask> handler);
    }
}
