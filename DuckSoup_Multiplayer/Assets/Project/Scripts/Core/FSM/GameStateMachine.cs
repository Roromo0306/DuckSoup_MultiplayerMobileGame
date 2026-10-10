using System;

namespace DuckSoup.Application.StateMachine
{
    public sealed class GameStateMachine
    {
        private IGameState currentState;
        private IGameState pendingState;

        public string CurrentStateName =>
            currentState?.GetType().Name ?? "None";


        public void RequestStateChange(IGameState nextState)
        {
            if (nextState == null)
            {
                throw new ArgumentNullException(nameof(nextState));
            }
                

            if (pendingState != null && !ReferenceEquals(pendingState, nextState))
            {
                throw new InvalidOperationException("Ya existe una transición pendiente.");
            }

            pendingState = nextState;
        }

        public void Tick(float deltaTime)
        {
            currentState?.Tick(deltaTime);
        }

        public void ApplyPendingTransition()
        {
            if (pendingState == null)
            {
                return;
            }
             

            IGameState nextState = pendingState;
            pendingState = null;

            if (ReferenceEquals(currentState, nextState))
            {
                return;
            }
           

            IGameState previousState = currentState;
            currentState = null;

            previousState?.Exit();

            currentState = nextState;
            currentState.Enter();
        }

        public void Stop()
        {
            pendingState = null;

            IGameState previousState = currentState;
            currentState = null;

            try
            {
                previousState?.Exit();
            }
            finally
            {
                pendingState = null;
            }
        }
    }
}
