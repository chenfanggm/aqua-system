using Cysharp.Threading.Tasks;

namespace com.aqua.system
{
    /// <summary>
    /// Base class for pipeline steps that execute on every iteration.
    /// </summary>
    public abstract class RerunStepBase<TContext> : IPipelineStep<TContext>
    {
        public bool IsRunOnce => false;
        public bool HasRun { get; private set; }

        public UniTask<bool> ExecuteAsync(TContext context, double deltaTime)
        {
            HasRun = true;
            return OnExecuteAsync(context, deltaTime);
        }

        public void Reset()
        {
            HasRun = false;
        }

        protected abstract UniTask<bool> OnExecuteAsync(TContext context, double deltaTime);
    }
}

