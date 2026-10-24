
namespace DuckSoup.Features.Menu.Contracts
{
    /// <summary>
    /// Contrato para consultar y guardar
    /// los resultados del modo individual.
    /// </summary>
    public interface IPlayerProgressStore
    {
        int GetTotalRecipesCompleted();

        int GetTableHighScore(string tableId);

        void RegisterSinglePlayerRun(
            string tableId,
            int completedRecipes);
    }
}
