using System.Runtime.CompilerServices;
using Roshambo.Models;

namespace Roshambo.Services;

internal sealed class ResultStorageService
{
    private const string BlobName = "e467c2dd-cb8b-4674-972a-dddf84ce02fc.roshambo.result.csv";
    private readonly IStorageService _storageService;

    public ResultStorageService(IStorageService storageService)
    {
        _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
    }

    /// <summary>
    /// Writes data to storage
    /// </summary>
    /// <param name="humanWinning"></param>
    /// <param name="computerWinning"></param>
    /// <param name="data"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task SaveResultAsync(IEnumerable<(UserId userId, ulong humanWinning, ulong computerWinning, ulong draw)> data, CancellationToken cancellationToken)
    {
        using Stream outputStream = _storageService.OpenWrite(BlobName, cancellationToken);
        using StreamWriter writer = new StreamWriter(outputStream);

        foreach ((UserId UserId, ulong HumanWinning, ulong ComputerWinning, ulong Draw) line in data)
        {
            await writer.WriteLineAsync(Serialize(line.UserId, line.HumanWinning, line.ComputerWinning, line.Draw));
        }
    }

    /// <summary>
    /// Reads data from blob storage
    /// </summary>
    public async IAsyncEnumerable<(UserId userId, ulong humanWinning, ulong computerWinning, ulong draw)> LoadResultAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        using Stream? inputStream = await _storageService.OpenReadAsync(BlobName, cancellationToken);
        if(inputStream is null)
        {
            yield break;
        }

        using StreamReader reader = new StreamReader(inputStream);
        while (!reader.EndOfStream)
        {
            string? line = await reader.ReadLineAsync();
            if (string.IsNullOrEmpty(line))
            {
                continue;
            }
            yield return Deserialize(line);
        }
    }

    private string Serialize(UserId userId, ulong humanWinning, ulong computerWinning, ulong draw)
    {
        return string.Join(',', userId.Value, humanWinning, computerWinning, draw);
    }

    private (UserId, ulong, ulong, ulong) Deserialize(string line)
    {
        string[] resultTokens = line.Split(',', StringSplitOptions.RemoveEmptyEntries);
        if (resultTokens.Length == 4)
        {
            return (new UserId(resultTokens[0]), ulong.Parse(resultTokens[1]), ulong.Parse(resultTokens[2]), ulong.Parse(resultTokens[3]));
        }
        throw new InvalidCastException($"Can't deserialize result line: {line}");
    }
}