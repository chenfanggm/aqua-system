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
        /// <summary>
        /// Generic method to publish events through MessagePipe
        /// </summary>
        UniTask PublishAsync<TEvent>(TEvent message);

        IDisposable Subscribe<TEvent>(IAsyncMessageHandler<TEvent> handler);
    }
}
