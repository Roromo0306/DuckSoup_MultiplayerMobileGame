using System;
using DuckSoup.Application.StateMachine;
using DuckSoup.Features.TwoPlayer.Contracts;
using DuckSoup.Features.TwoPlayer.Services;
using DuckSoup.Features.TwoPlayer.Views;

namespace DuckSoup.Features.TwoPlayer.States
{
    public sealed class TwoPlayerCountdownState : IGameState
    {
        private readonly ITwoPlayerFlowView view;
        private readonly Action finished;
        private float elapsed;
        private bool requested;
        private string lastMessage;

        public TwoPlayerCountdownState(ITwoPlayerFlowView view, Action finished)
        {
            this.view = view ?? throw new ArgumentNullException(nameof(view));
            this.finished = finished ?? throw new ArgumentNullException(nameof(finished));
        }
        public void Enter()
        {
            elapsed = 0f;
            requested = false;
            lastMessage = "3";
            view.SetInputEnabled(false);
            view.ShowOverlay("3");
        }
        public void Tick(float deltaTime)
        {
            if (requested) return;
            elapsed += deltaTime;
            string text = elapsed < 1f ? "3" : elapsed < 2f ? "2" :
                elapsed < 3f ? "1" : "DUCK SOUP!";
            if (text != lastMessage) { view.ShowOverlay(text); lastMessage = text; }
            if (elapsed >= 3.7f) { requested = true; finished.Invoke(); }
        }
        public void Exit() => view.HideOverlay();
    }

    public sealed class TwoPlayerPlayingState : IGameState
    {
        private readonly TwoPlayerSession session;
        private readonly ITwoPlayerFlowView view;
        private readonly Action onTimeUp;
        private bool requested;

        public TwoPlayerPlayingState(
            TwoPlayerSession session, ITwoPlayerFlowView view, Action onTimeUp)
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session));
            this.view = view ?? throw new ArgumentNullException(nameof(view));
            this.onTimeUp = onTimeUp ?? throw new ArgumentNullException(nameof(onTimeUp));
        }
        public void Enter()
        {
            requested = false;
            view.HideOverlay();
            view.SetInputEnabled(true);
            view.RefreshBoard();
            view.RefreshHud(session.RemainingSeconds, session.Player1Score, session.Player2Score);
        }
        public void Tick(float deltaTime)
        {
            if (requested) return;
            if (session.Advance(deltaTime)) view.RefreshBoard();
            view.RefreshHud(session.RemainingSeconds, session.Player1Score, session.Player2Score);
            if (session.IsTimeUp) { requested = true; onTimeUp.Invoke(); }
        }
        public void Exit() => view.SetInputEnabled(false);
    }

    public sealed class TwoPlayerTimeUpState : IGameState
    {
        private readonly ITwoPlayerFlowView view;
        private readonly Action finished;
        private readonly float seconds;
        private float elapsed;
        private bool requested;

        public TwoPlayerTimeUpState(
            ITwoPlayerFlowView view, Action finished, float seconds)
        {
            this.view = view ?? throw new ArgumentNullException(nameof(view));
            this.finished = finished ?? throw new ArgumentNullException(nameof(finished));
            if (seconds <= 0f) throw new ArgumentOutOfRangeException(nameof(seconds));
            this.seconds = seconds;
        }
        public void Enter()
        {
            elapsed = 0f;
            requested = false;
            view.SetInputEnabled(false);
            view.ShowOverlay("TIME UP!");
        }
        public void Tick(float deltaTime)
        {
            if (requested) return;
            elapsed += deltaTime;
            if (elapsed >= seconds) { requested = true; finished.Invoke(); }
        }
        public void Exit() => view.HideOverlay();
    }

    public sealed class TwoPlayerResultsState : IGameState
    {
        private readonly TwoPlayerSession session;
        private readonly TwoPlayerResultsView results;
        private readonly int duck1;
        private readonly int duck2;

        public TwoPlayerResultsState(
            TwoPlayerSession session, TwoPlayerResultsView results,
            int duck1, int duck2)
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session));
            this.results = results ?? throw new ArgumentNullException(nameof(results));
            this.duck1 = duck1;
            this.duck2 = duck2;
        }
        public void Enter() => results.Show(duck1, duck2, session.Player1Score, session.Player2Score);
        public void Tick(float deltaTime) { }
        public void Exit() => results.Hide();
    }
}
