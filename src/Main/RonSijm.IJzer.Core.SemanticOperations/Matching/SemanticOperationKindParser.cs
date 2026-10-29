using RonSijm.IJzer.Core.SemanticOperations.Model;

namespace RonSijm.IJzer.Core.SemanticOperations.Matching;

public static class SemanticOperationKindParser
{
    public static bool TryParse(string value, out SemanticOperationKind kind)
    {
        var result = Enum.TryParse(value.Trim(), true, out kind)
            && Enum.IsDefined(typeof(SemanticOperationKind), kind);

        return result;
    }
}