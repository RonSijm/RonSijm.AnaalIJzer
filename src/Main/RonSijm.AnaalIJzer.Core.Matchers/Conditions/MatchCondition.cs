namespace RonSijm.AnaalIJzer.Core.Matchers.Conditions;

public readonly struct MatchCondition(MatchKind kind, string value, MatchOperand operand = MatchOperand.Subject)
{
    public MatchKind Kind { get; } = kind;

    public string Value { get; } = value;

    public MatchOperand Operand { get; } = operand;
}
