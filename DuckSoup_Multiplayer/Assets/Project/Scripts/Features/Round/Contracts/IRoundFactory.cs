
using DuckSoup.Features.Round.Models;

namespace DuckSoup.Features.Round.Contracts
{
    public interface IRoundFactory
    {
        RoundModel CreateRound(string roundId);
    }
}
