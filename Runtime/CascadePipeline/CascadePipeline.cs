using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace com.aqua.system
{
    /// <summary>
    /// Generic async pipeline runner with loop control, once-only steps and end hooks.
    /// </summary>
    public class CascadePipeline<TContext>
    {
        private readonly List<IPipelineStep<TContext>> _steps = new();
        private readonly List<IPipelineStep<TContext>> _onPipelineEndSteps = new();
        private readonly int _maxIterations;
        private bool _hasEndLoopStep;
        private int _iterationCount;

        public CascadePipeline(int maxIterations = 30)
        {
            if (maxIterations <= 0) throw new ArgumentOutOfRangeException(nameof(maxIterations));
            _maxIterations = maxIterations;
        }

        public CascadePipeline<TContext> AddOnceStep(OnceStepBase<TContext> step) => AddStep(step);
        public CascadePipeline<TContext> AddRerunStep(RerunStepBase<TContext> step) => AddStep(step);
        private CascadePipeline<TContext> AddStep(IPipelineStep<TContext> step)
        {
            EnsureMutable();
            step = step ?? throw new ArgumentNullException(nameof(step));
            _steps.Add(step);
            return this;
        }

        public CascadePipeline<TContext> AddEndLoopStep(EndLoopStep<TContext> step)
        {
            if (_hasEndLoopStep)
                throw new InvalidOperationException("Pipeline already has an EndStep");
            _steps.Add(step ?? throw new ArgumentNullException(nameof(step)));
            _hasEndLoopStep = true;
            return this;
        }

        public CascadePipeline<TContext> AddOnPipelineEndStep(OnceStepBase<TContext> step)
        {
            _onPipelineEndSteps.Add(step ?? throw new ArgumentNullException(nameof(step)));
            return this;
        }

        /// <summary>
        /// Executes the pipeline. Optional reset callback runs at the beginning of each iteration.
        /// </summary>
        /// <param name="deltaTime">Seconds since the last iteration.</param>
        public async UniTask RunAsync(TContext context, double deltaTime = 0f,
            Action<TContext> onIterationStart = null, CancellationToken cancellationToken = default)
        {
            if (!_hasEndLoopStep) throw new InvalidOperationException("Pipeline must end with AddEndStep");
            if (context == null) throw new ArgumentNullException(nameof(context));

            Reset();

            while (_iterationCount < _maxIterations)
            {
                cancellationToken.ThrowIfCancellationRequested();
                onIterationStart?.Invoke(context);
                var shouldContinue = false;

                foreach (var step in _steps)
                {
                    if (step.IsRunOnce && step.HasRun) continue;
                    shouldContinue = await step.ExecuteAsync(context, deltaTime);
                    if (!shouldContinue) break;
                }

                _iterationCount++;
                if (!shouldContinue) break;
            }

            foreach (var step in _onPipelineEndSteps)
            {
                await step.ExecuteAsync(context, deltaTime);
            }
        }

        private void Reset()
        {
            _iterationCount = 0;
            foreach (var step in _steps)
            {
                step.Reset();
            }
            foreach (var step in _onPipelineEndSteps)
            {
                step.Reset();
            }
        }

        private void EnsureMutable()
        {
            if (_hasEndLoopStep)
                throw new InvalidOperationException("Cannot add steps after AddEndLoopStep");
        }
    }
}
