using Roshambo.Models;

namespace Roshambo.Services;

public class RoundResponse : RoshamboResponse
{
    public RoundResult? Round { get; init; } = default!;
    public Statistics Statistics { get; init; } = default!;
    public Statistics UserStatistics { get; set; } = default!;
}