using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using RonSijm.AnaalIJzer.Core.Matchers;
using RonSijm.AnaalIJzer.Core.OperationContracts.Model;

namespace RonSijm.AnaalIJzer.Core.OperationContracts.Evaluation;

/// <summary>Evaluates only the source facts explicitly selected by an operation contract.</summary>
public static class OperationContractEvaluator
{
    public static ImmutableArray<OperationContractEvaluation> EvaluateOwner(OperationContractDefinition definition, IMethodSymbol method, string? layerPath)
    {
        var evaluations = ImmutableArray.CreateBuilder<OperationContractEvaluation>();
        if (!IsAllowedHostLayer(definition.AllowedOwnerLayers, layerPath))
        {
            evaluations.Add(CreateEvaluation(definition, OperationContractParticipantRole.Owner, OperationContractViolationKind.OwnerOutsideAllowedLayer, "the owner declaration is not in one of the operation's allowed owner layers"));
        }

        if (definition.RequestMatcher is { } requestMatcher && !HasMatchingParameter(method, requestMatcher))
        {
            evaluations.Add(CreateEvaluation(definition, OperationContractParticipantRole.Owner, OperationContractViolationKind.OwnerMissingRequest, "the owner declaration does not accept the configured request type"));
        }

        if (definition.ResponseMatcher is { } responseMatcher && !MatchesType(responseMatcher, method.ReturnType))
        {
            evaluations.Add(CreateEvaluation(definition, OperationContractParticipantRole.Owner, OperationContractViolationKind.OwnerInvalidResponse, "the owner declaration does not return the configured response type"));
        }

        var result = evaluations.ToImmutable();

        return result;
    }

    public static ImmutableArray<OperationContractEvaluation> EvaluateEntryPoint(OperationContractDefinition definition, IMethodSymbol method, string? layerPath, bool invokesOwner)
    {
        var evaluations = ImmutableArray.CreateBuilder<OperationContractEvaluation>();
        if (!IsAllowedHostLayer(definition.AllowedEntryPointLayers, layerPath))
        {
            evaluations.Add(CreateEvaluation(definition, OperationContractParticipantRole.EntryPoint, OperationContractViolationKind.EntryPointOutsideAllowedLayer, "the entry-point declaration is not in one of the operation's allowed entry-point layers"));
        }

        if (definition.RequestMatcher is { } requestMatcher && !HasMatchingParameter(method, requestMatcher))
        {
            evaluations.Add(CreateEvaluation(definition, OperationContractParticipantRole.EntryPoint, OperationContractViolationKind.EntryPointMissingRequest, "the entry-point declaration does not accept the configured request type"));
        }

        if (definition.ResponseMatcher is { } responseMatcher && !MatchesType(responseMatcher, method.ReturnType))
        {
            evaluations.Add(CreateEvaluation(definition, OperationContractParticipantRole.EntryPoint, OperationContractViolationKind.EntryPointInvalidResponse, "the entry-point declaration does not return the configured response type"));
        }

        if (!invokesOwner)
        {
            evaluations.Add(CreateEvaluation(definition, OperationContractParticipantRole.EntryPoint, OperationContractViolationKind.EntryPointDoesNotInvokeOwner, "the entry-point declaration does not directly invoke the configured owner"));
        }

        var result = evaluations.ToImmutable();

        return result;
    }

    public static bool MatchesOwner(OperationContractDefinition definition, IMethodSymbol method)
    {
        var result = definition.Owner.Matches(method);

        return result;
    }

    public static bool MatchesEntryPoint(OperationContractDeclarationSelector selector, IMethodSymbol method)
    {
        var result = selector.Matches(method);

        return result;
    }

    public static bool MatchesType(PatternMatcher matcher, ITypeSymbol type)
    {
        var namespaceName = type.ContainingNamespace.IsGlobalNamespace ? string.Empty : type.ContainingNamespace.ToDisplayString();
        var result = matcher.TryMatch(type.Name, namespaceName, type) is not null;

        return result;
    }

    private static bool HasMatchingParameter(IMethodSymbol method, PatternMatcher matcher)
    {
        var result = method.Parameters.Any(parameter => MatchesType(matcher, parameter.Type));

        return result;
    }

    private static bool IsAllowedHostLayer(ImmutableHashSet<string> allowedLayers, string? layerPath)
    {
        if (allowedLayers.Count == 0)
        {
            return true;
        }

        var resolvedLayerPath = layerPath ?? string.Empty;
        if (string.IsNullOrWhiteSpace(resolvedLayerPath))
        {
            return false;
        }

        foreach (var allowedLayer in allowedLayers)
        {
            if (resolvedLayerPath == allowedLayer || resolvedLayerPath.StartsWith(allowedLayer + "/", StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static OperationContractEvaluation CreateEvaluation(OperationContractDefinition definition, OperationContractParticipantRole participantRole, OperationContractViolationKind violationKind, string reason)
    {
        var result = new OperationContractEvaluation(definition, participantRole, violationKind, reason);

        return result;
    }
}