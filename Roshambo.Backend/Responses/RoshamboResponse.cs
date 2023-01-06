using Roshambo.Models;

namespace Roshambo.Services;

public class RoshamboResponse
{
    public RelModel Self { get; init; } = default!;

    public IEnumerable<RelModel> Next { get; set; } = Enumerable.Empty<RelModel>();
}