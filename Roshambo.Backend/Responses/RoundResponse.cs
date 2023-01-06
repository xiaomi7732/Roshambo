using Roshambo.Models;

namespace Roshambo.Services;

public class RoundResponse : RoshamboResponse
{
    [Obsolete("Use next instead. Keep just for backward compatibility.")]
    public IEnumerable<RelModel> Actions => Next;

    public RoundResult? Round { get; init; } = default!;
    public Statistics Statistics { get; init; } = default!;
    public Statistics UserStatistics { get; set; } = default!;
}