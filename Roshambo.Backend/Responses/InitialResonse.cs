using Roshambo.Models;

namespace Roshambo.Services;

public class InitialResponse : RoshamboResponse
{
    public UserId SuggestedUserId { get; init; } = default!;
}