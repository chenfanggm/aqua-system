using System;
using Cysharp.Threading.Tasks;

namespace com.aqua.system
{
    /// <summary>
    /// Terminates the loop when the supplied predicate returns true.
    /// </summary>
    public class EndLoopStep<TContext> : RerunStepBase<TContext>
    {
        private readonly Func<TContext, bool> _shouldEndLoop;

        public EndLoopStep(Func<TContext, bool> shouldEndLoop)
        {
            _shouldEndLoop = shouldEndLoop ?? throw new ArgumentNullException(nameof(shouldEndLoop));
        }

        protected override UniTask<bool> OnExecuteAsync(TContext context, long deltaTime)
        {
            var shouldRerun = !_shouldEndLoop(context);
            return UniTask.FromResult(shouldRerun);
        }
    }
}
