
using DuckSoup.Domain;
using DuckSoup.Features.Match.Models;
using DuckSoup.Features.Round.Models;

namespace DuckSoup.Features.Match.Contracts
{
    public interface IRoundSettlementService
    {
        RoundSettlementResult Settle(
            RoundModel round,
            PlayerId roundWinner);
    }
}
