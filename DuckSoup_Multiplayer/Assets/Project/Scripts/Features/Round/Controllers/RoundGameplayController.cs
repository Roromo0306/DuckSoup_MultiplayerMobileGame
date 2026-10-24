
using System;
using DuckSoup.Domain;
using DuckSoup.Features.Round.Contracts;

namespace DuckSoup.Features.Round.Controllers
{
    /// <summary>
    /// Recibe las intenciones de interacción
    /// procedentes de las vistas.
    ///
    /// Delega las reglas en el servicio
    /// y proporciona snapshots para la UI.
    /// </summary>
    public sealed class RoundGameplayController
    {
        private readonly IRoundReadModel readModel;
        private readonly IRoundInteractionService interactionService;

        public bool IsFinished =>
            interactionService.IsFinished;

        public PlayerId Winner =>
            interactionService.Winner;

        public RoundGameplayController(
            IRoundReadModel readModel,
            IRoundInteractionService interactionService)
        {
            this.readModel = readModel ??
                throw new ArgumentNullException(
                    nameof(readModel));

            this.interactionService = interactionService ??
                throw new ArgumentNullException(
                    nameof(interactionService));
        }

        public RoundSnapshot GetSnapshot()
        {
            return readModel.GetSnapshot();
        }

        public bool TryBeginToken(
            string tokenId,
            int pointerId)
        {
            return interactionService.TryBeginGesture(
                tokenId, pointerId);
        }

        public bool TryDeliverToken(
            string tokenId,
            int pointerId,
            PlayerId playerId,
            string cardId)
        {
            return interactionService.TryDeliver(
                tokenId,
                pointerId,
                playerId,
                cardId);
        }

        public void CancelTokenGesture(
            string tokenId,
            int pointerId)
        {
            interactionService.CancelGesture(
                tokenId,
                pointerId);
        }

        public void CancelAllGestures()
        {
            interactionService.CancelAllGestures();
        }
    }
}

