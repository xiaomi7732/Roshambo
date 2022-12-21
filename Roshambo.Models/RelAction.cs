namespace Roshambo.Models;

public abstract class RelAction : RelModel
{
    public RelAction(string urlBase)
    {
        Rel = "ready";
        Method = HttpMethod.Post.ToString().ToLowerInvariant();
        Key = Name;
        ActionBase = $"{urlBase}/rounds/";
    }

    public abstract string Name { get; }
    public virtual string ActionBase { get; } = "/rounds/";
    public override string Href => $"{ActionBase}{Name}";
}

public class RockAction : RelAction
{
    public RockAction(string urlBase)
        : base(urlBase)
    { }

    public const string ActionName = "rock";
    public override string Name => ActionName;
}

public class PaperAction : RelAction
{
    public PaperAction(string urlBase)
        : base(urlBase)
    { }
    public const string ActionName = "paper";

    public override string Name => ActionName;
}

public class ScissorAction : RelAction
{
    public ScissorAction(string urlBase)
        : base(urlBase)
    { }
    public const string ActionName = "scissor";

    public override string Name => ActionName;
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
    public override string Href { get; }
}