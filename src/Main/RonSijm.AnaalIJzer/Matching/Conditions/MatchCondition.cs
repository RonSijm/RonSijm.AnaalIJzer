namespace RonSijm.AnaalIJzer.Conditions;

internal readonly struct MatchCondition(MatchKind kind, string value)
{
    public MatchKind Kind { get; } = kind;

    public string Value { get; } = value;
}
