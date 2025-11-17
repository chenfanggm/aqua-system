using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using MessagePipe;

namespace com.aqua.system
{
    /// <summary>
    /// MessagePipe-based puzzle event manager using BuiltinContainerBuilder
    /// No external DI framework required - uses GlobalMessagePipe
    /// Configured through PuzzleEventBusBuilder
    /// </summary>
    public class MessagePipeEventBus : IEventBus
    {
        // ========== Publish ==========
        public void Publish<TEvent>(TEvent message)
        {
            GlobalMessagePipe.GetPublisher<TEvent>().Publish(message);
        }

        public async UniTask PublishAsync<TEvent>(TEvent message)
        {
            await GlobalMessagePipe.GetAsyncPublisher<TEvent>().PublishAsync(message);
        }

        public void Publish<TTopic, TEvent>(TTopic topic, TEvent message)
        {
            GlobalMessagePipe.GetPublisher<TTopic, TEvent>().Publish(topic, message);
        }

        public async UniTask PublishAsync<TTopic, TEvent>(TTopic topic, TEvent message)
        {
            await GlobalMessagePipe
                .GetAsyncPublisher<TTopic, TEvent>()
                .PublishAsync(topic, message);
        }

        // ========== Subscribe (sync) ==========
        public IDisposable Subscribe<TEvent>(Action<TEvent> handler)
        {
            return Subscribe(new InlineSyncHandler<TEvent>(handler));
        }

        public IDisposable Subscribe<TTopic, TEvent>(TTopic topic, Action<TEvent> handler)
        {
            return Subscribe(topic, new InlineSyncHandler<TEvent>(handler));
        }

        // ========== Subscribe (async) ==========
        public IDisposable Subscribe<TEvent>(Func<TEvent, UniTask> handler)
        {
            return Subscribe(new InlineAsyncHandler<TEvent>(handler));
        }

        public IDisposable Subscribe<TTopic, TEvent>(TTopic topic, Func<TEvent, UniTask> handler)
        {
            return Subscribe(topic, new InlineAsyncHandler<TEvent>(handler));
        }

        // ========== Low-level MessagePipe route ==========
        public IDisposable Subscribe<TEvent>(IAsyncMessageHandler<TEvent> handler)
        {
            return GlobalMessagePipe.GetAsyncSubscriber<TEvent>().Subscribe(handler);
        }

        public IDisposable Subscribe<TTopic, TEvent>(
            TTopic topic,
            IAsyncMessageHandler<TEvent> handler
        )
        {
            return GlobalMessagePipe.GetAsyncSubscriber<TTopic, TEvent>().Subscribe(topic, handler);
        }

        public IDisposable Subscribe<TEvent>(IMessageHandler<TEvent> handler)
        {
            return GlobalMessagePipe.GetSubscriber<TEvent>().Subscribe(handler);
        }

        public IDisposable Subscribe<TTopic, TEvent>(TTopic topic, IMessageHandler<TEvent> handler)
        {
            return GlobalMessagePipe.GetSubscriber<TTopic, TEvent>().Subscribe(topic, handler);
        }
    }
}
