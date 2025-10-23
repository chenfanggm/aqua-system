using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using MessagePipe;

namespace com.aqua.system
{
    public class InlineEventHandler<TEvent> : IAsyncMessageHandler<TEvent>
    {
        private readonly Func<TEvent, CancellationToken, UniTask> _handler;

        public InlineEventHandler(Func<TEvent, CancellationToken, UniTask> handler)
        {
            _handler = handler ?? throw new ArgumentNullException(nameof(handler));
        }

        public InlineEventHandler(Func<TEvent, UniTask> handler)
        {
            _handler = (message, _) => handler(message);
        }

        public InlineEventHandler(Action<TEvent> handler)
        {
            _handler = (message, _) =>
            {
                handler(message);
                return UniTask.CompletedTask;
            };
        }

        public async UniTask HandleAsync(TEvent message, CancellationToken cancellationToken)
        {
            await _handler(message, cancellationToken);
        }
    }
}
