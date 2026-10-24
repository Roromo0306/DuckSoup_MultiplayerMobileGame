
using DuckSoup.Features.Menu.Models;

namespace DuckSoup.Features.Menu.Contracts
{
    /// <summary>
    /// Contrato para guardar el último
    /// modo de juego seleccionado.
    /// </summary>
    public interface IMenuModeStore
    {
        GameMode LoadMode();

        void SaveMode(GameMode mode);
    }
}
