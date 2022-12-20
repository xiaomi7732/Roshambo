namespace Roshambo.Models;

/// <summary>
/// User id. Case insensitive.
/// </summary>
public class UserId : IEquatable<UserId>
{
    public UserId()
    {
    }

    public UserId(Guid id)
        : this(id.ToString("d"))
    {
    }

    public UserId(string id)
    {
        Value = id;
    }

    public string Value { get; } = Guid.NewGuid().ToString("d");

    public override string ToString()
    {
        return Value;
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public bool Equals(UserId? other)
    {
        if (other is null)
        {
            return false;
        }

        return string.Equals(other.Value, Value, StringComparison.OrdinalIgnoreCase);
    }

    public static UserId Anonymous = new UserId(Guid.Empty);

    public bool IsAnonymous()
        => Equals(Anonymous);
}