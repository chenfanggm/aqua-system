using Cysharp.Threading.Tasks;

namespace com.aqua.system
{
    /// <summary>
    /// Base class for pipeline steps that should only execute once per pipeline run.
    /// </summary>
    public abstract class OnceStepBase<TContext> : IPipelineStep<TContext>
    {
        public bool IsRunOnce => true;
        public bool HasRun { get; private set; }

        public async UniTask<bool> ExecuteAsync(TContext context, long deltaTime)
        {
            var result = await OnExecuteAsync(context, deltaTime);
            HasRun = true;
            return result;
        }

        public void Reset()
        {
            HasRun = false;
        }

        protected abstract UniTask<bool> OnExecuteAsync(TContext context, long deltaTime);
    }
}

