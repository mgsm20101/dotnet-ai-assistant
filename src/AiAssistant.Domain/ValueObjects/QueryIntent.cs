namespace AiAssistant.Domain.ValueObjects;

/// <summary>Classified intent of an incoming user query.</summary>
public sealed record QueryIntent
{
    public static readonly QueryIntent Factual     = new("factual");
    public static readonly QueryIntent Calculation = new("calculation");
    public static readonly QueryIntent Unknown     = new("unknown");

    public string Value { get; }

    private QueryIntent(string value) => Value = value;

    public static QueryIntent From(string raw) => raw.ToLowerInvariant() switch
    {
        "factual"     => Factual,
        "calculation" => Calculation,
        _             => Unknown,
    };

    public override string ToString() => Value;
}
