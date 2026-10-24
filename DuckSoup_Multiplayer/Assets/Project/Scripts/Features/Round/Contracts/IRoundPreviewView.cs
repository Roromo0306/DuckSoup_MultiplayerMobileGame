
namespace DuckSoup.Features.Round.Contracts
{
    public interface IRoundPreviewView
    {
        void Render(RoundSnapshot snapshot);

        /// <summary>
        /// Actualiza las vistas existentes.
        /// No recrea los prefabs.
        /// </summary>
        void Refresh();

        void Clear();
    }
}
