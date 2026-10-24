
using System;
using DuckSoup.Application.StateMachine;
using DuckSoup.Features.Match.Contracts;
using DuckSoup.Features.Match.Models;

namespace DuckSoup.Features.Match.States
{
    public sealed class MatchResultsState : IGameState
    {
        private readonly IMatchResultsView view;
        private readonly RoundSettlementResult result;

        public MatchResultsState(
            IMatchResultsView view,
            RoundSettlementResult result)
        {
            this.view = view ??
                throw new ArgumentNullException(nameof(view));

            this.result = result ??
                throw new ArgumentNullException(nameof(result));
        }

        public void Enter()
        {
            view.ShowFinal(result);
        }

        public void Tick(float deltaTime)
        {
        }

        public void Exit()
        {
            view.Hide();
        }
    }
}
