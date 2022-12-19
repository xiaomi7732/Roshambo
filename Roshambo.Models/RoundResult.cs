namespace Roshambo.Models;

public class RoundResult
{
    public RoshamboResult Result { get; set; }

    public RelAction ComputerMove { get; set; } = default!;

    public ulong HumanWinning { get; set; }
    public ulong ComputerWinning { get; set; }
    public ulong Draw { get; set; }

}