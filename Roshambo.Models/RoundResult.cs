namespace Roshambo.Models;

public class RoundResult
{
    public RoshamboResult Result { get; set; }

    public RelAction ComputerMove { get; set; } = default!;
    public RelAction UserMove { get; set; } = default!;
}