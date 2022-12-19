using Roshambo.Models;

namespace Roshambo.Services;

internal class RoshamboService
{
    public (RoshamboResult, RoshamboOption computerAction) Go(RoshamboOption userOption)
    {
        RoshamboOption computerMove = (RoshamboOption)new Random().Next(0, 3); // 0, 1, or 2.

        if (computerMove == userOption)
        {
            return (RoshamboResult.Draw, computerMove);
        }
        else if (userOption == RoshamboOption.Rock)
        {
            if (computerMove == RoshamboOption.Paper)
            {
                return (RoshamboResult.ComputerWin, computerMove);
            }
            else
            {
                return (RoshamboResult.HumanWin, computerMove);
            }
        }
        else if (userOption == RoshamboOption.Paper)
        {
            if (computerMove == RoshamboOption.Rock)
            {
                return (RoshamboResult.HumanWin, computerMove);
            }
            else
            {
                return (RoshamboResult.ComputerWin, computerMove);
            }
        }
        else
        {
            if (computerMove == RoshamboOption.Rock)
            {
                return (RoshamboResult.ComputerWin, computerMove);
            }
            else
            {
                return (RoshamboResult.HumanWin, computerMove);
            }
        }
    }
}

internal enum RoshamboOption
{
    Rock,
    Paper,
    Scissor,
}

