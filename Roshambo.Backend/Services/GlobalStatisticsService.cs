using System.Text;
using Roshambo.Models;

namespace Roshambo.Services;

internal class GlobalStatisticsService
{
    private static readonly SemaphoreSlim _fileLock = new SemaphoreSlim(1, 1);
    private readonly ILogger<GlobalStatisticsService> _logger;
    private const string FileName = "Result.csv"; // Human Win, Computer Win, Draw

    public GlobalStatisticsService(ILogger<GlobalStatisticsService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    public async Task<GlobalStatistics> GetGlobalStatisticsAsync(CancellationToken cancellationToken)
    {
        (ulong humanWinning, ulong computerWinning, ulong drawCount) = await GetWinningCountsAsync(cancellationToken).ConfigureAwait(false);
        GlobalStatistics statistics = new GlobalStatistics()
        {
            HumanWinning = humanWinning,
            ComputerWinning = computerWinning,
            Draw = drawCount,
        };

        return statistics;
    }

    public async Task<(ulong humanWinning, ulong computerWinning, ulong drawCount)> GetWinningCountsAsync(CancellationToken cancellationToken)
    {
        await _fileLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            (ulong humanWinning, ulong computerWinning, ulong draw) = (0, 0, 0);

            try
            {
                _logger.LogInformation("Reading result from result");
                (humanWinning, computerWinning, draw) = await ReadAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed reading result.");
            }

            return (humanWinning, computerWinning, draw);
        }
        finally
        {
            _fileLock.Release();
        }
    }

    public async Task IncreaseAsync(RoshamboResult winner, CancellationToken cancellationToken)
    {
        await _fileLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            (ulong humanWinning, ulong computerWinning, ulong draw) = (0, 0, 0);

            try
            {
                (humanWinning, computerWinning, draw) = await ReadAsync(cancellationToken).ConfigureAwait(false);
            }
            catch
            {
            }

            switch (winner)
            {
                case RoshamboResult.HumanWin:
                    humanWinning++;
                    break;
                case RoshamboResult.ComputerWin:
                    computerWinning++;
                    break;
                case RoshamboResult.Draw:
                    draw++;
                    break;
                default:
                    throw new NotSupportedException($"Unrecognized winner result of {winner}");
            }

            _logger.LogInformation("Writing result: {0} - {1} - {2}", humanWinning, computerWinning, draw);
            await WriterAsync(humanWinning, computerWinning, draw, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _fileLock.Release();
        }
    }

    private async Task<(ulong humanWinning, ulong computerWinning, ulong drawCount)> ReadAsync(CancellationToken cancellationToken)
    {
        string resultText = await File.ReadAllTextAsync(GetFileResultFilePath(), Encoding.UTF8, cancellationToken).ConfigureAwait(false);
        string[] resultTokens = resultText.Split(',', StringSplitOptions.RemoveEmptyEntries);
        return (ulong.Parse(resultTokens[0]), ulong.Parse(resultTokens[1]), ulong.Parse(resultTokens[2]));
    }

    private async Task WriterAsync(ulong humanWinning, ulong computerWinning, ulong drawCount, CancellationToken cancellationToken)
    {
        string resultText = string.Join(',', humanWinning, computerWinning, drawCount);
        await File.WriteAllTextAsync(GetFileResultFilePath(), resultText, Encoding.UTF8, cancellationToken).ConfigureAwait(false);
    }

    private string GetFileResultFilePath()
        => Path.GetFullPath(Path.Combine(Path.GetTempPath(), FileName));
}