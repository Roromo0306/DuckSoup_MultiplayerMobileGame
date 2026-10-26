
namespace DuckSoup.Features.SinglePlayer.Contracts
{
    /// <summary>
    /// Contrato entre los estados y
    /// la interfaz del modo individual.
    /// </summary>
    public interface ISinglePlayerFlowView
    {
        void SetGameplayInputEnabled(bool enabled);

        void ShowOverlay(string message);

        void HideOverlay();

        void RefreshBoard();

        void RefreshHud(
            float remainingSeconds,
            int completedRecipes);
    }
}
