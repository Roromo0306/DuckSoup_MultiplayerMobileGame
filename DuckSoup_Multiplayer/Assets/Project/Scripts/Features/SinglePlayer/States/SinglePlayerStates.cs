
using System;
using DuckSoup.Application.StateMachine;
using DuckSoup.Features.SinglePlayer.Contracts;
using DuckSoup.Features.SinglePlayer.Services;

namespace DuckSoup.Features.SinglePlayer.States
{
    /// <summary>
    /// Cuenta atrás inicial:
    /// 3, 2, 1, DUCK SOUP!
    /// </summary>
    public sealed class SinglePlayerCountdownState : IGameState
    {
        private readonly ISinglePlayerFlowView view;
        private readonly Action onFinished;

        private float elapsed;
        private bool finished;
        private string lastMessage;

        public SinglePlayerCountdownState(
            ISinglePlayerFlowView view,
            Action onFinished)
        {
            this.view = view ??
                throw new ArgumentNullException(nameof(view));

            this.onFinished = onFinished ??
                throw new ArgumentNullException(
                    nameof(onFinished));
        }

        public void Enter()
        {
            elapsed = 0f;
            finished = false;
            lastMessage = "3";

            view.SetGameplayInputEnabled(false);
            view.ShowOverlay("3");
        }

        public void Tick(float deltaTime)
        {
            if (finished)
                return;

            elapsed += deltaTime;

            string message;

            if (elapsed < 1f)
                message = "3";
            else if (elapsed < 2f)
                message = "2";
            else if (elapsed < 3f)
                message = "1";
            else
                message = "DUCK SOUP!";

            if (message != lastMessage)
            {
                view.ShowOverlay(message);
                lastMessage = message;
            }

            if (elapsed >= 3.7f)
            {
                finished = true;
                onFinished.Invoke();
            }
        }

        public void Exit()
        {
            view.HideOverlay();
        }
    }

    /// <summary>
    /// Gameplay individual mientras
    /// queden segundos disponibles.
    /// </summary>
    public sealed class SinglePlayerPlayingState : IGameState
    {
        private readonly SinglePlayerSession session;
        private readonly ISinglePlayerFlowView view;
        private readonly Action onTimeUp;

        private bool endRequested;

        public SinglePlayerPlayingState(
            SinglePlayerSession session,
            ISinglePlayerFlowView view,
            Action onTimeUp)
        {
            this.session = session ??
                throw new ArgumentNullException(
                    nameof(session));

            this.view = view ??
                throw new ArgumentNullException(
                    nameof(view));

            this.onTimeUp = onTimeUp ??
                throw new ArgumentNullException(
                    nameof(onTimeUp));
        }

        public void Enter()
        {
            endRequested = false;

            view.HideOverlay();
            view.SetGameplayInputEnabled(true);
            view.RefreshBoard();

            view.RefreshHud(
                session.RemainingSeconds,
                session.CompletedRecipes);
        }

        public void Tick(float deltaTime)
        {
            if (endRequested)
                return;

            bool boardChanged =
                session.Advance(deltaTime);

            if (boardChanged)
                view.RefreshBoard();

            view.RefreshHud(
                session.RemainingSeconds,
                session.CompletedRecipes);

            if (session.IsTimeUp)
            {
                endRequested = true;
                onTimeUp.Invoke();
            }
        }

        public void Exit()
        {
            view.SetGameplayInputEnabled(false);
        }
    }

    /// <summary>
    /// Muestra TIME UP y solicita
    /// volver al menú principal.
    /// </summary>
    public sealed class SinglePlayerTimeUpState : IGameState
    {
        private readonly SinglePlayerSession session;
        private readonly ISinglePlayerFlowView view;

        private readonly Action onEntered;
        private readonly Action onReturnToMenu;

        private readonly float displayDuration;

        private float elapsed;
        private bool returnRequested;

        public SinglePlayerTimeUpState(
            SinglePlayerSession session,
            ISinglePlayerFlowView view,
            Action onEntered,
            Action onReturnToMenu,
            float displayDuration)
        {
            this.session = session ??
                throw new ArgumentNullException(
                    nameof(session));

            this.view = view ??
                throw new ArgumentNullException(
                    nameof(view));

            this.onEntered = onEntered ??
                throw new ArgumentNullException(
                    nameof(onEntered));

            this.onReturnToMenu = onReturnToMenu ??
                throw new ArgumentNullException(
                    nameof(onReturnToMenu));

            if (displayDuration <= 0f)
                throw new ArgumentOutOfRangeException(
                    nameof(displayDuration));

            this.displayDuration = displayDuration;
        }

        public void Enter()
        {
            elapsed = 0f;
            returnRequested = false;

            view.SetGameplayInputEnabled(false);

            view.RefreshHud(
                0f,
                session.CompletedRecipes);

            view.ShowOverlay(
                $"TIME UP!\n" +
                $"{session.CompletedRecipes} RECIPES");

            // Guardaremos el resultado al entrar
            // en este estado una sola vez.
            onEntered.Invoke();
        }

        public void Tick(float deltaTime)
        {
            if (returnRequested)
                return;

            elapsed += deltaTime;

            if (elapsed >= displayDuration)
            {
                returnRequested = true;
                onReturnToMenu.Invoke();
            }
        }

        public void Exit()
        {
            view.HideOverlay();
        }
    }
}
