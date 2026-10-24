
using DuckSoup.Features.Match.Models;

namespace DuckSoup.Features.Match.Contracts
{
    public interface IMatchResultsView
    {
        void ShowRound(RoundSettlementResult result);

        void ShowFinal(RoundSettlementResult result);

        void Hide();
    }
}
