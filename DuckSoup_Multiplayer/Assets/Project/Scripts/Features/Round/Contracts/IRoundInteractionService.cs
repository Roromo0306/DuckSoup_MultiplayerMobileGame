
using DuckSoup.Domain;

namespace DuckSoup.Features.Round.Contracts
{
    public interface IRoundInteractionService
    {
        bool IsFinished { get; }

        PlayerId Winner { get; }

        bool TryBeginGesture(
            string tokenId,
            int pointerId);

        bool TryDeliver(
            string tokenId,
            int pointerId,
            PlayerId playerId,
            string cardId);

        void CancelGesture(
            string tokenId,
            int pointerId);

        void CancelAllGestures();

        /// <summary>
        /// Actualiza los temporizadores de revelado.
        /// Devuelve true si cambia algún token.
        /// </summary>
        bool Tick(float deltaTime);
    }
}
