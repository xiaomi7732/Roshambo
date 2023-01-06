using System.Text.Json.Serialization;

namespace Roshambo.Models;

[JsonDerivedType(typeof(RockAction))]
[JsonDerivedType(typeof(PaperAction))]
[JsonDerivedType(typeof(ScissorAction))]
[JsonDerivedType(typeof(CustomRel))]
public abstract class RelModel
{
    /// <summary>
    /// Gets the relation
    /// </summary>
    /// <value></value>
    public string Rel { get; init; } = default!;

    public abstract string Href { get; }

    /// <summary>
    /// Gets the method.
    /// </summary>
    public string Method { get; init; } = HttpMethod.Get.ToString().ToLowerInvariant();

    /// <summary>
    /// Gets the key for the relation.
    /// </summary>
    public string? Key { get; init; }
}