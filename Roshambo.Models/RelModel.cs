using System.Text.Json.Serialization;

namespace Roshambo.Models;

[JsonDerivedType(typeof(RockAction))]
[JsonDerivedType(typeof(PaperAction))]
[JsonDerivedType(typeof(ScissorAction))]
[JsonDerivedType(typeof(SelfRel))]
[JsonDerivedType(typeof(ReadyRel))]
public class RelModel
{
    /// <summary>
    /// Gets the relation.
    /// </summary>
    public string Rel { get; init; } = default!;

    /// <summary>
    /// Gets the HRef.
    /// </summary>
    public virtual string Href { get; init; } = default!;

    /// <summary>
    /// Gets the method.
    /// </summary>
    public string Method { get; init; } = HttpMethod.Get.ToString().ToLowerInvariant();
}