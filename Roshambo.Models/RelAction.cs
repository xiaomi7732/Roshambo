namespace Roshambo.Models;

public abstract class RelAction
{
    public string Rel { get; } = "action";
    public abstract string Name { get; }
    public virtual string ActionBase { get; } = "/rounds/";
    public string Href => $"{ActionBase}{Name}";
    public string Method { get; } = "post";
}

public class RockAction : RelAction
{
    public const string ActionName = "rock";
    public override string Name => ActionName;
}

public class PaperAction : RelAction
{
    public const string ActionName = "paper";

    public override string Name => ActionName;
}

public class ScissorAction : RelAction
{
    public const string ActionName = "scissor";

    public override string Name => ActionName;
}