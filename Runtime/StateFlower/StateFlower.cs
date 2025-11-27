using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace com.aqua.system
{
    public sealed class StateFlower<TState>
    {
        public TState CurrentState => _currentState;

        private readonly IStateTransitionValidator<TState> _transitionValidator;
        private readonly EqualityComparer<TState> _comparer = EqualityComparer<TState>.Default;
        private readonly List<Func<UniTask>> _flowSteps = new();
        private TState _currentState;

        public StateFlower(
            TState initialState,
            IStateTransitionValidator<TState> transitionValidator
        )
        {
            _currentState = initialState;
            _transitionValidator = transitionValidator ?? throw new ArgumentNullException(nameof(transitionValidator));
        }

        public StateFlower<TState> Transition(TState to, Func<UniTask> action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            _flowSteps.Add(async () =>
            {
                TransitionTo(to);
                await action();
            });
            return this;
        }

        public async UniTask ExecuteAsync()
        {
            if (_flowSteps.Count == 0)
                throw new InvalidOperationException("Game flow must contain at least one step.");

            try
            {
                foreach (var step in _flowSteps)
                    await step();
            }
            finally
            {
                _flowSteps.Clear();
            }
        }

        private void TransitionTo(TState newState)
        {
            if (_comparer.Equals(newState, _currentState))
                throw new InvalidOperationException(
                    $"Invalid transition to the same state: {newState}"
                );

            if (!_transitionValidator.IsTransitionAllowed(_currentState, newState))
                throw new InvalidOperationException(
                    $"Invalid transition from {_currentState} to {newState}"
                );

            _currentState = newState;
        }
    }
}
