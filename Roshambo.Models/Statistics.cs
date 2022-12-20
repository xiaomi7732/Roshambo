namespace Roshambo.Models;

public class Statistics
{
    public ulong HumanWinning { get; init; }
    public ulong ComputerWinning { get; init; }
    public ulong Draw { get; set; }
}