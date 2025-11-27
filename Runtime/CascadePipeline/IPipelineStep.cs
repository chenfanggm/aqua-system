using Cysharp.Threading.Tasks;

namespace com.aqua.system
{
    /// <summary>
    /// Generic asynchronous pipeline step. Returns TRUE to continue iterating.
    /// </summary>
    internal interface IPipelineStep<TContext>
    {
        UniTask<bool> ExecuteAsync(TContext context);
    }
}
