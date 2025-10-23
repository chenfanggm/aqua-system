using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using MessagePipe;

namespace com.aqua.grid
{
    /// <summary>
    /// MessagePipe-based puzzle event manager using BuiltinContainerBuilder
    /// No external DI framework required - uses GlobalMessagePipe
    /// Configured through PuzzleEventBusBuilder
    /// </summary>
    public class MessagePipeEventBus : IEventBus
    {
        /// <summary>
        /// Generic helper method to publish events through MessagePipe
        /// </summary>
        public async UniTask PublishAsync<TEvent>(TEvent message)
        {
            var publisher = GlobalMessagePipe.GetAsyncPublisher<TEvent>();
            await publisher.PublishAsync(message);
        }

        public IDisposable Subscribe<TEvent>(IAsyncMessageHandler<TEvent> handler)
        {
            var subscriber = GlobalMessagePipe.GetAsyncSubscriber<TEvent>();
            return subscriber.Subscribe(handler);
        }
    }
}
