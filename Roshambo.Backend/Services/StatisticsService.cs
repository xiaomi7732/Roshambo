using System.Collections.Concurrent;
using Roshambo.Models;

namespace Roshambo.Services;

internal sealed class StatisticsService : IAsyncDisposable
{
    private static readonly SemaphoreSlim _fileLock = new SemaphoreSlim(1, 1);
    private readonly ResultStorageService _resultStore;
    private readonly UserDataUtility _userDataUtility;
    private readonly ILogger<StatisticsService> _logger;

    private ConcurrentDictionary<UserId, (ulong, ulong, ulong)>? _inMemoryCache = null;
    private Dictionary<UserId, (ulong, ulong, ulong)>? _dataBaseline = null;

    public StatisticsService(
        ResultStorageService resultStore,
        UserDataUtility userDataUtility,
        ILogger<StatisticsService> logger)
    {
        _resultStore = resultStore ?? throw new ArgumentNullException(nameof(resultStore));
        _userDataUtility = userDataUtility ?? throw new ArgumentNullException(nameof(userDataUtility));
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

        if (_inMemoryCache is null || _dataBaseline is null)
        {
            throw new InvalidOperationException("In memory cache and data baseline should have been created.");
        }

        Dictionary<UserId, (ulong, ulong, ulong)> merged = _userDataUtility.MergeData(_inMemoryCache, _dataBaseline);

        _logger.LogInformation("Merged:");
        foreach (var item in merged)
        {
            _logger.LogInformation("{key}: {value1} {value2} {value3}", item.Key, item.Value.Item1, item.Value.Item2, item.Value.Item3);
        }

        if (merged.ContainsKey(userId))
        {
            return merged[userId];
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
        _dataBaseline = new Dictionary<UserId, (ulong, ulong, ulong)>();
        _inMemoryCache = new ConcurrentDictionary<UserId, (ulong, ulong, ulong)>();
        try
        {
            await foreach ((UserId key, ulong humanWinning, ulong computerWinning, ulong drawCount) in ReadLinesAsync(cancellationToken))
            {
                _logger.LogInformation("Adding cache item: {0} {1} {2} {3}", key, humanWinning, computerWinning, drawCount);
                _dataBaseline.TryAdd(key, (humanWinning, computerWinning, drawCount));
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

    private IAsyncEnumerable<(UserId key, ulong humanWinning, ulong computerWinning, ulong drawCount)> ReadLinesAsync(CancellationToken cancellationToken)
        => _resultStore.LoadResultAsync(cancellationToken);

    private async Task WriteAsync(CancellationToken cancellationToken)
    {
        if (_inMemoryCache is null)
        {
            return;
        }

        await _fileLock.WaitAsync().ConfigureAwait(false);
        try
        {
            Dictionary<UserId, (ulong, ulong, ulong)> baseData = new Dictionary<UserId, (ulong, ulong, ulong)>();
            await foreach ((UserId UserId, ulong HumanWinning, ulong ComputerWinning, ulong Draw) item in _resultStore.LoadResultAsync(cancellationToken))
            {
                baseData.Add(item.UserId, (item.HumanWinning, item.ComputerWinning, item.Draw));
            }
            Dictionary<UserId, (ulong, ulong, ulong)> merged = _userDataUtility.MergeData(_inMemoryCache, baseData);

            await _resultStore.SaveResultAsync(merged.Select(item => (item.Key, item.Value.Item1, item.Value.Item2, item.Value.Item3)), cancellationToken).ConfigureAwait(false);
        }
        catch(Exception ex)
        {
            _logger.LogError(ex, "Failed persistent data for this session.");
        }
        finally
        {
            _fileLock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        await WriteAsync(default).ConfigureAwait(false);
    }
}