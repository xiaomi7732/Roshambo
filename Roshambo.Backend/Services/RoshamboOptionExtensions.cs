using Roshambo.Models;

namespace Roshambo.Services;

internal static class RoshamboOptionExtensions
{
    public static RelAction ToAction(this RoshamboOption option)
    {
        switch (option)
        {
            case RoshamboOption.Rock:
                return new RockAction();
            case RoshamboOption.Paper:
                return new PaperAction();
            case RoshamboOption.Scissor:
                return new ScissorAction();
            default:
                throw new NotSupportedException($"Unsupported roshambo option of: {option}");
        }
    }
}

