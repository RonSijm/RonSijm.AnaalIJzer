using RonSijm.AnaalIJzer.Core.SemanticOperations.Model;

namespace RonSijm.AnaalIJzer.Core.OperationPolicies.Behavioral;

/// <summary>A resolved operation together with its position in a control-flow body.</summary>
public readonly struct SemanticOperationOccurrence(SemanticOperation operation, int blockOrdinal, int sourcePosition, int sourceOrder)
{
    public SemanticOperation Operation { get; } = operation;

    public int BlockOrdinal { get; } = blockOrdinal;

    public int SourcePosition { get; } = sourcePosition;

    public int SourceOrder { get; } = sourceOrder;

    public bool IsLexicallyBefore(SemanticOperationOccurrence other)
    {
        var result = SourcePosition < other.SourcePosition
            || SourcePosition == other.SourcePosition && SourceOrder < other.SourceOrder;

        return result;
    }
}