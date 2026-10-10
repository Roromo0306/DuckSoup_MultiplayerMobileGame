using System;
using DuckSoup.Application.StateMachine;
using DuckSoup.Features.InputTest.Contracts;

namespace DuckSoup.Features.InputTest.States
{
    public sealed class InputTestState : IGameState
    {
        private readonly IInputTestScreen screen;

        public InputTestState(IInputTestScreen screen)
        {
            this.screen = screen ?? throw new ArgumentNullException(nameof(screen));
        }

        public void Enter()
        {
            screen.SetInteractionEnabled(true);
        }

        public void Tick(float deltaTime)
        {
            // Todavía no necesitamos lógica por frame.
        }

        public void Exit()
        {
            screen.SetInteractionEnabled(false);
        }
    }
}
