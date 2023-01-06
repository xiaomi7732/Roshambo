namespace Roshambo.Models;

public abstract class RelAction : RelModel
{
    public RelAction(string urlBase)
    {
        Rel = "ready";
        Method = HttpMethod.Post.ToString().ToLowerInvariant();
        ActionBase = $"{urlBase}/rounds/";
    }

    public string Key { get; init; } = "unknown";
    protected string ActionBase { get; } = "/rounds/";
    public override string Href
    {
        get => $"{ActionBase}{Key}";
        init => throw new InvalidOperationException("Can't specify Href for a rel action.");
    }

    [Obsolete("Use Key instead. Remove this after the client is updated.")]
    public string Name => Key;
}

public class RockAction : RelAction
{
    public RockAction(string urlBase)
        : base(urlBase)
    {
        Key = ActionName;
    }

    public const string ActionName = "rock";
}

public class PaperAction : RelAction
{
    public PaperAction(string urlBase)
        : base(urlBase)
    {
        Key = ActionName;
    }
    public const string ActionName = "paper";

}

public class ScissorAction : RelAction
{
    public ScissorAction(string urlBase)
        : base(urlBase)
    {
        Key = ActionName;
    }
    public const string ActionName = "scissor";
}

public class SelfRel : RelModel
{
    public SelfRel(string href, HttpMethod? method = null)
    {
        Rel = "self";
        method ??= HttpMethod.Get;
        Method = method.ToString().ToLowerInvariant();
        Href = href;
    }
}

public class ReadyRel : RelModel
{
    public ReadyRel()
    {
        Rel = "ready";
    }
}