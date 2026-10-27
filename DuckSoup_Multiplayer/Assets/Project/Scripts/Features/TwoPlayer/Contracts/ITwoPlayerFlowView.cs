namespace DuckSoup.Features.TwoPlayer.Contracts
{
    /// <summary>Contrato entre los estados y la presentacion del modo 2P.</summary>
    public interface ITwoPlayerFlowView
    {
        void SetInputEnabled(bool enabled);
        void ShowOverlay(string text);
        void HideOverlay();
        void RefreshBoard();
        void RefreshHud(float time, int player1Score, int player2Score);
    }
}
