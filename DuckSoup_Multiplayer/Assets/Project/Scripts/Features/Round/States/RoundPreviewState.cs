
using System;
using DuckSoup.Application.StateMachine;
using DuckSoup.Features.Round.Contracts;

namespace DuckSoup.Features.Round.States
{
    public sealed class RoundPreviewState : IGameState
    {
        private readonly IRoundReadModel readModel;
        private readonly IRoundPreviewView view;
        private readonly IRoundInteractionService interactionService;

        public RoundPreviewState(
            IRoundReadModel readModel,
            IRoundPreviewView view,
            IRoundInteractionService interactionService)
        {
            this.readModel = readModel ??
                throw new ArgumentNullException(
                    nameof(readModel));

            this.view = view ??
                throw new ArgumentNullException(
                    nameof(view));

            this.interactionService = interactionService ??
                throw new ArgumentNullException(
                    nameof(interactionService));
        }

        public void Enter()
        {
            view.Render(readModel.GetSnapshot());
        }

        public void Tick(float deltaTime)
        {
            // El servicio devuelve true únicamente
            // cuando hay un cambio visual.
            bool hasChanged =
                interactionService.Tick(deltaTime);

            if (hasChanged)
            {
                view.Refresh();
            }
        }

        public void Exit()
        {
            view.Clear();
        }
    }
}
