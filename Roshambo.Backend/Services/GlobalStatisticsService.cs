using Roshambo.Models;

namespace Roshambo.Services;

internal class GlobalStatisticsService
{
    public async Task<GlobalStatistics> GetGlobalStatisticsAsync(CancellationToken cancellationToken)
    {
        (ulong humanWinning, ulong computerWinning) = await GetWinningCountsAsync(cancellationToken).ConfigureAwait(false);
        GlobalStatistics statistics = new GlobalStatistics()
        {
            HumanWinning = humanWinning,
            ComputerWinning = computerWinning,
        };

        return statistics;
    }

    public Task<(ulong humanWinning, ulong computerWinning)> GetWinningCountsAsync(CancellationToken cancellationToken)
    {
        // TODO: Implement logic
        return Task.FromResult<(ulong, ulong)>((10, 20));
    }
}