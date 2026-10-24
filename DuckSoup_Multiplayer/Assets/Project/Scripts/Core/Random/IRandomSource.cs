
namespace DuckSoup.Core.Randomization
{
    public interface IRandomSource
    {
        // Mínimo incluido, máximo excluido.
        int NextInt(
            int minInclusive,
            int maxExclusive);
    }
}
