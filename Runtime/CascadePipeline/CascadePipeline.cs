using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace com.aqua.system
{
    /// <summary>
    /// Generic async pipeline runner with loop control, once-only steps and end hooks.
    /// </summary>
    internal class CascadePipeline<TContext>
    {
        private readonly List<IPipelineStep<TContext>> _steps = new();
        private readonly List<OnLoopEndStep<TContext>> _onLoopEndSteps = new();
        private readonly List<IPipelineStep<TContext>> _runOnceSteps = new();
        private readonly HashSet<IPipelineStep<TContext>> _hasRunOnceSteps = new();
        private readonly int _maxIterations;
        private bool _hasEndLoopStep;
        private int _iterationCount;

        public CascadePipeline(int maxIterations = 30)
        {
            if (maxIterations <= 0) throw new ArgumentOutOfRangeException(nameof(maxIterations));
            _maxIterations = maxIterations;
        }

        public CascadePipeline<TContext> AddStep(IPipelineStep<TContext> step)
        {
            EnsureMutable();
            _steps.Add(step ?? throw new ArgumentNullException(nameof(step)));
            return this;
        }

        public CascadePipeline<TContext> AddOnceStep(IPipelineStep<TContext> step)
        {
            EnsureMutable();
            step = step ?? throw new ArgumentNullException(nameof(step));
            _steps.Add(step);
            _runOnceSteps.Add(step);
            return this;
        }

        public CascadePipeline<TContext> AddRerunStep(IPipelineStep<TContext> step) => AddStep(step);

        public CascadePipeline<TContext> AddEndLoopStep(EndLoopStep<TContext> step)
        {
            if (_hasEndLoopStep)
                throw new InvalidOperationException("Pipeline already has an EndStep");
            _steps.Add(step ?? throw new ArgumentNullException(nameof(step)));
            _hasEndLoopStep = true;
            return this;
        }

        public CascadePipeline<TContext> AddOnLoopEndStep(OnLoopEndStep<TContext> step)
        {
            _onLoopEndSteps.Add(step ?? throw new ArgumentNullException(nameof(step)));
            return this;
        }

        /// <summary>
        /// Executes the pipeline. Optional reset callback runs at the beginning of each iteration.
        /// </summary>
        public async UniTask RunAsync(TContext context, Action<TContext> resetContext = null)
        {
            if (!_hasEndLoopStep)
                throw new InvalidOperationException("Pipeline must end with AddEndStep");
            if (context == null) throw new ArgumentNullException(nameof(context));

            ResetExecutionState();

            while (_iterationCount < _maxIterations)
            {
                resetContext?.Invoke(context);
                var shouldContinue = false;

                foreach (var step in _steps)
                {
                    if (ShouldSkipRunOnceStep(step))
                        continue;

                    shouldContinue = await step.ExecuteAsync(context);
                    if (!shouldContinue)
                        break;

                    MarkRunOnceSteps(step);
                }

                _iterationCount++;
                if (!shouldContinue)
                    break;
            }

            foreach (var step in _onLoopEndSteps)
            {
                await step.ExecuteAsync(context);
            }
        }

        private void ResetExecutionState()
        {
            _iterationCount = 0;
            _hasRunOnceSteps.Clear();
        }

        private void EnsureMutable()
        {
            if (_hasEndLoopStep)
                throw new InvalidOperationException("Cannot add steps after AddEndStep");
        }

        private bool ShouldSkipRunOnceStep(IPipelineStep<TContext> step)
        {
            return _runOnceSteps.Contains(step) && _hasRunOnceSteps.Contains(step);
        }

        private void MarkRunOnceSteps(IPipelineStep<TContext> step)
        {
            if (!_hasRunOnceSteps.Contains(step))
                _hasRunOnceSteps.Add(step);
        }
    }
}
