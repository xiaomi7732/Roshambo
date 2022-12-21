namespace Roshambo.Models;

public class CustomRel : RelModel
{
    public CustomRel(string rel, string href, HttpMethod? httpMethod = null, string? key = null)
    {
        if (string.IsNullOrEmpty(rel))
        {
            throw new ArgumentException($"'{nameof(rel)}' cannot be null or empty.", nameof(rel));
        }

        if (string.IsNullOrEmpty(href))
        {
            throw new ArgumentException($"'{nameof(href)}' cannot be null or empty.", nameof(href));
        }

        Rel = rel;
        Href = href;
        httpMethod ??= HttpMethod.Get;
        Method = httpMethod.ToString().ToLowerInvariant();
        Key = key;
    }

    public override string Href { get; }
}