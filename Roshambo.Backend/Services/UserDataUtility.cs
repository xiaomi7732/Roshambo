using Roshambo.Models;

namespace Roshambo.Services;

sealed internal class UserDataUtility
{
    private UserDataUtility() { }
    public static UserDataUtility Instance { get; } = new UserDataUtility();

    public Dictionary<UserId, (ulong, ulong, ulong)> MergeData(IDictionary<UserId, (ulong, ulong, ulong)> left, IDictionary<UserId, (ulong, ulong, ulong)> right)
    {
        Dictionary<UserId, (ulong, ulong, ulong)> result = new Dictionary<UserId, (ulong, ulong, ulong)>(left);

        foreach (KeyValuePair<UserId, (ulong, ulong, ulong)> item in right)
        {
            if (result.ContainsKey(item.Key))
            {
                // Update existing item.
                (ulong humanWinning, ulong computerWinning, ulong draw) = result[item.Key];
                humanWinning += item.Value.Item1;
                computerWinning += item.Value.Item2;
                draw += item.Value.Item3;
                result[item.Key] = (humanWinning, computerWinning, draw);
            }
            else
            {
                // Add new item.
                result.Add(item.Key, item.Value);
            }
        }
        return result;
    }
}