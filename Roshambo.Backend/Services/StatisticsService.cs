using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Roshambo.Models;

namespace Roshambo.Services;

internal sealed class StatisticsService : IAsyncDisposable
{
    private static readonly SemaphoreSlim _fileLock = new SemaphoreSlim(1, 1);
    private readonly ILogger<StatisticsService> _logger;
    private const string FileName = "Result.csv"; // Human Win, Computer Win, Draw

    private ConcurrentDictionary<UserId, (ulong, ulong, ulong)>? _inMemoryCache = null;

    public StatisticsService(ILogger<StatisticsService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Get statistics for global.
    /// </summary>
    public Task<Statistics> GetGlobalStatisticsAsync(CancellationToken cancellationToken)
        => GetStatisticsForAsync(UserId.Anonymous, cancellationToken);

    /// <summary>
    /// Increase 1 round for a given user.
    /// </summary>
    public async Task IncreaseAsync(UserId userId, RoshamboResult winner, CancellationToken cancellationToken)
    {
        await EnsureCacheReadyAsync(cancellationToken).ConfigureAwait(false);

        if (_inMemoryCache is null)
        {
            throw new InvalidOperationException("Memory cache for the result hasn't been initialized yet.");
        }

        // Update value for normal user.
        _inMemoryCache.AddOrUpdate(userId, addValueFactory: uid =>
        {
            return UpdateScore(0, 0, 0, winner);
        }, updateValueFactory: (uid, old) =>
        {
            (ulong humanWinning, ulong computerWinning, ulong draw) = old;
            return UpdateScore(humanWinning, computerWinning, draw, winner);
        });

        if (userId is not null && !userId.IsAnonymous())
        {
            // Update value for global
            _inMemoryCache.AddOrUpdate(UserId.Anonymous, addValueFactory: uid =>
                {
                    return UpdateScore(0, 0, 0, winner);
                }, updateValueFactory: (uid, old) =>
                {
                    (ulong humanWinning, ulong computerWinning, ulong draw) = old;
                    return UpdateScore(humanWinning, computerWinning, draw, winner);
                });
        }

        await Task.Yield();
    }

    /// <summary>
    /// Get statistics for the current user.
    /// </summary>
    public async Task<Statistics> GetStatisticsForAsync(UserId userId, CancellationToken cancellationToken)
    {
        (ulong humanWinning, ulong computerWinning, ulong drawCount) = await GetWinningCountsAsync(userId, cancellationToken).ConfigureAwait(false);
        Statistics statistics = new Statistics()
        {
            HumanWinning = humanWinning,
            ComputerWinning = computerWinning,
            Draw = drawCount,
        };

        return statistics;
    }

    /// <summary>
    /// Gets winning count for the global.
    /// </summary>
    private async Task<(ulong humanWinning, ulong computerWinning, ulong drawCount)> GetWinningCountsAsync(UserId userId, CancellationToken cancellationToken)
    {
        await EnsureCacheReadyAsync(cancellationToken).ConfigureAwait(false);

        if (_inMemoryCache!.ContainsKey(userId))
        {
            return _inMemoryCache![userId];
        }
        _logger.LogWarning("No data found for user id: {0}", userId);
        return (0, 0, 0);
    }

    private (ulong humanWinning, ulong computerWinning, ulong draw) UpdateScore(ulong originHumanWinning, ulong originComputerWinning, ulong originDraw, RoshamboResult winner)
    {
        switch (winner)
        {
            case RoshamboResult.HumanWin:
                originHumanWinning++;
                break;
            case RoshamboResult.ComputerWin:
                originComputerWinning++;
                break;
            case RoshamboResult.Draw:
                originDraw++;
                break;
            default:
                throw new NotSupportedException($"Unrecognized winner result of {winner}");
        }
        // origin values has been mutated.
        return (originHumanWinning, originComputerWinning, originDraw);
    }

    private async Task<ConcurrentDictionary<UserId, (ulong, ulong, ulong)>> EnsureCacheReadyAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation(nameof(EnsureCacheReadyAsync));

        if (_inMemoryCache is not null)
        {
            return _inMemoryCache;
        }

        _logger.LogInformation("No in memory cache ... Build one.");

        // Create the cache
        await _fileLock.WaitAsync();
        _inMemoryCache = new ConcurrentDictionary<UserId, (ulong, ulong, ulong)>();
        try
        {
            await foreach ((UserId key, ulong humanWinning, ulong computerWinning, ulong drawCount) in ReadLinesAsync(cancellationToken))
            {
                _logger.LogInformation("Adding cache item: {0} {1} {2} {3}", key, humanWinning, computerWinning, drawCount);
                _inMemoryCache.TryAdd(key, (humanWinning, computerWinning, drawCount));
            }

            if (_inMemoryCache.Count == 0)
            {
                _inMemoryCache.TryAdd(UserId.Anonymous, (0, 0, 0));
            }

            return _inMemoryCache;
        }
        catch (FileNotFoundException)
        {
            _logger.LogInformation(nameof(FileNotFoundException));

            _inMemoryCache = new ConcurrentDictionary<UserId, (ulong, ulong, ulong)>();
            _inMemoryCache.TryAdd(UserId.Anonymous, (0, 0, 0));
            return _inMemoryCache;
        }
        finally
        {
            _fileLock.Release();
        }
    }

    private async IAsyncEnumerable<(UserId key, ulong humanWinning, ulong computerWinning, ulong drawCount)> ReadLinesAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        string fileName = GetFileResultFilePath();
        using Stream inputtingStream = File.OpenRead(fileName);
        using StreamReader reader = new StreamReader(inputtingStream);
        string? line = await reader.ReadLineAsync().ConfigureAwait(false);
        _logger.LogInformation("Read line from result file: {0}, line: {1}", fileName, line);
        if (line is null)
        {
            yield break;
        }

        yield return ParseLine(line);
    }

    private (UserId key, ulong humanWinning, ulong computerWinning, ulong drawCount) ParseLine(string line)
    {
        string[] resultTokens = line.Split(',', StringSplitOptions.RemoveEmptyEntries);
        if (resultTokens.Length == 3) // Old format
        {
            return (UserId.Anonymous, ulong.Parse(resultTokens[0]), ulong.Parse(resultTokens[1]), ulong.Parse(resultTokens[2]));
        }
        else if (resultTokens.Length == 4)
        {
            return (new UserId(resultTokens[0]), ulong.Parse(resultTokens[1]), ulong.Parse(resultTokens[2]), ulong.Parse(resultTokens[3]));
        }
        throw new InvalidCastException($"Can't deserialize result line: {line}");
    }

    private async Task WriterAsync(CancellationToken cancellationToken)
    {
        if (_inMemoryCache is null)
        {
            return;
        }

        await _fileLock.WaitAsync().ConfigureAwait(false);
        try
        {
            Dictionary<UserId, (ulong, ulong, ulong)> dictClone = new Dictionary<UserId, (ulong, ulong, ulong)>(_inMemoryCache);

            string writingFileName = Path.GetTempFileName();
            using (Stream outputStream = File.OpenWrite(writingFileName))
            using (StreamWriter writer = new StreamWriter(outputStream))
            {
                string? line = null;
                foreach (KeyValuePair<UserId, (ulong HumanWinning, ulong ComputerWinning, ulong Draw)> item in dictClone)
                {
                    line = string.Join(',', item.Key.Value, item.Value.HumanWinning, item.Value.ComputerWinning, item.Value.Draw);
                    await writer.WriteLineAsync(line).ConfigureAwait(false);
                }
            }

            File.Move(writingFileName, GetFileResultFilePath(), overwrite: true);
        }
        finally
        {
            _fileLock.Release();
        }
    }

    private string GetFileResultFilePath()
        => Path.GetFullPath(Path.Combine(Path.GetTempPath(), FileName));

    public async ValueTask DisposeAsync()
    {
        await WriterAsync(default).ConfigureAwait(false);
    }
}