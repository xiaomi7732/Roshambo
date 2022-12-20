using Roshambo.Models;

namespace Roshambo.Services;

internal class RoshamboService
{
    private readonly GlobalStatisticsService _statisticsService;

    public RoshamboService(GlobalStatisticsService statisticsService)
    {
        _statisticsService = statisticsService ?? throw new ArgumentNullException(nameof(statisticsService));
    }

    public async Task<(RoshamboResult, RoshamboOption computerAction)> GoAsync(string userId, RoshamboOption userOption, CancellationToken cancellationToken)
    {
        RoshamboOption computerMove = (RoshamboOption)new Random().Next(0, 3); // 0, 1, or 2.

        if (computerMove == userOption)
        {
            await _statisticsService.IncreaseAsync(userId, RoshamboResult.Draw, cancellationToken);
            return (RoshamboResult.Draw, computerMove);
        }
        else if (userOption == RoshamboOption.Rock)
        {
            if (computerMove == RoshamboOption.Paper)
            {
                await _statisticsService.IncreaseAsync(userId, RoshamboResult.ComputerWin, cancellationToken);
                return (RoshamboResult.ComputerWin, computerMove);
            }
            else
            {
                await _statisticsService.IncreaseAsync(userId, RoshamboResult.HumanWin, cancellationToken);
                return (RoshamboResult.HumanWin, computerMove);
            }
        }
        else if (userOption == RoshamboOption.Paper)
        {
            if (computerMove == RoshamboOption.Rock)
            {
                await _statisticsService.IncreaseAsync(userId, RoshamboResult.HumanWin, cancellationToken);
                return (RoshamboResult.HumanWin, computerMove);
            }
            else
            {
                await _statisticsService.IncreaseAsync(userId, RoshamboResult.ComputerWin, cancellationToken);
                return (RoshamboResult.ComputerWin, computerMove);
            }
        }
        else
        {
            if (computerMove == RoshamboOption.Rock)
            {
                await _statisticsService.IncreaseAsync(userId, RoshamboResult.ComputerWin, cancellationToken);
                return (RoshamboResult.ComputerWin, computerMove);
            }
            else
            {
                await _statisticsService.IncreaseAsync(userId, RoshamboResult.HumanWin, cancellationToken);
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

