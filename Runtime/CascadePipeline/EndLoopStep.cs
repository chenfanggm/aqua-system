using System;
using Cysharp.Threading.Tasks;

namespace com.aqua.system
{
    /// <summary>
    /// Terminates the loop when the supplied predicate returns true.
    /// </summary>
    internal class EndLoopStep<TContext> : IPipelineStep<TContext>
    {
        private readonly Func<TContext, bool> _shouldEndLoop;

        public EndLoopStep(Func<TContext, bool> shouldEndLoop)
        {
            _shouldEndLoop = shouldEndLoop ?? throw new ArgumentNullException(nameof(shouldEndLoop));
        }

        public UniTask<bool> ExecuteAsync(TContext context)
        {
            var shouldRerun = !_shouldEndLoop(context);
            return UniTask.FromResult(shouldRerun);
        }
    }
}
