namespace RonSijm.AnaalIJzer.Conditions;

public readonly struct MatchCondition(MatchKind kind, string value)
{
    public MatchKind Kind { get; } = kind;

    public string Value { get; } = value;
}
