using Cysharp.Threading.Tasks;

namespace com.aqua.system
{
    /// <summary>
    /// Base type for steps executed after the main loop finishes.
    /// </summary>
    public abstract class OnLoopEndStep<TContext> : IPipelineStep<TContext>
    {
        public abstract UniTask<bool> ExecuteAsync(TContext context);
    }
}
