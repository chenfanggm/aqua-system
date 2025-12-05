using Cysharp.Threading.Tasks;

namespace com.aqua.system
{
    /// <summary>
    /// Generic asynchronous pipeline step. Returns TRUE to continue iterating.
    /// </summary>
    public interface IPipelineStep<TContext>
    {
        bool IsRunOnce { get; }
        bool HasRun { get; }

        UniTask<bool> ExecuteAsync(TContext context, double deltaTime);

        // Reset the step to its initial state.
        void Reset();
    }
}
