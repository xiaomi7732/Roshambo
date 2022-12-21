using Roshambo.Models;

namespace Roshambo.Services;

internal static class RoshamboOptionExtensions
{
    public static RelAction ToAction(this RoshamboOption option, string urlBase)
    {
        switch (option)
        {
            case RoshamboOption.Rock:
                return new RockAction(urlBase);
            case RoshamboOption.Paper:
                return new PaperAction(urlBase);
            case RoshamboOption.Scissor:
                return new ScissorAction(urlBase);
            default:
                throw new NotSupportedException($"Unsupported roshambo option of: {option}");
        }
    }
}

