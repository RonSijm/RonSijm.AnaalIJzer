using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RonSijm.AnaalIJzer.Core.Contracts.Contracts;
using RonSijm.AnaalIJzer.Core.Inheritance.Policies;
using RonSijm.AnaalIJzer.Core.LayerModel;
using RonSijm.AnaalIJzer.Core.ReturnValues.Policies;

namespace RonSijm.AnaalIJzer.Core.PolicyEvaluation.Engine.DependencyRules;

public readonly partial struct LayerRegistry
{
	public TypePolicyViolation? EvaluateTypePolicy(LayerMatch layerMatch, string typeName, string namespaceName, ITypeSymbol? symbol)
	{
		if (layerMatch.Layer.IsForbidden)
		{
			return null;
		}

		if (TryFindGlobalForbiddenMatch(typeName, namespaceName, symbol, out var globalForbiddenRule, out var globalForbiddenSuffix))
		{
			return CreateForbiddenViolation(globalForbiddenRule, globalForbiddenSuffix, layerMatch.Layer.Name, "global");
		}

		foreach (var layer in layerMatch.Layers)
		{
			if (_catalog.NodesByPath.TryGetValue(layer.Name, out var node)
			    && TryFindPolicyMatch(node.ForbiddenTypeMatchers, typeName, namespaceName, symbol, out var rule, out var suffix))
			{
				return CreateForbiddenViolation(rule, suffix, layerMatch.Layer.Name, $"layer '{layer.Name}'");
			}
		}

		if (!_catalog.AllowedTypeMatchers.IsDefaultOrEmpty
		    && !MatchesAnyPolicy(_catalog.AllowedTypeMatchers, typeName, namespaceName, symbol))
		{
			return new TypePolicyViolation("the global <Allowed> list has no matching rule", layerMatch.Layer.Name, null, null, null);
		}

		foreach (var layer in layerMatch.Layers)
		{
			if (_catalog.NodesByPath.TryGetValue(layer.Name, out var node)
			    && !node.AllowedTypeMatchers.IsDefaultOrEmpty
			    && !MatchesAnyPolicy(node.AllowedTypeMatchers, typeName, namespaceName, symbol))
			{
				return new TypePolicyViolation($"the <Allowed> list scoped to layer '{layer.Name}' has no matching rule", layerMatch.Layer.Name, null, null, null);
			}
		}

		return null;
	}

	public ContractPolicyEvaluation? EvaluateContractPolicies(LayerMatch layerMatch, ContractDeclarationShape shape)
	{
		foreach (var layer in layerMatch.Layers)
		{
			if (!_catalog.NodesByPath.TryGetValue(layer.Name, out var node))
			{
				continue;
			}

			foreach (var policy in node.ContractPolicies)
			{
				var result = policy.Evaluate(shape);
				if (result is not null)
				{
					return result;
				}
			}
		}

		return null;
	}

	public InheritancePolicyEvaluation? EvaluateInheritancePolicies(LayerMatch layerMatch, INamedTypeSymbol symbol)
	{
		foreach (var layer in layerMatch.Layers)
		{
			if (!_catalog.NodesByPath.TryGetValue(layer.Name, out var node))
			{
				continue;
			}

			foreach (var policy in node.InheritancePolicies)
			{
				var result = policy.Evaluate(symbol);
				if (result is not null)
				{
					return result;
				}
			}
		}

		return null;
	}

	public ReturnValuePolicyEvaluation? EvaluateReturnValuePolicies(LayerMatch layerMatch, ExpressionSyntax expression, SemanticModel semanticModel, CancellationToken cancellationToken)
	{
		foreach (var layer in layerMatch.Layers)
		{
			if (!_catalog.NodesByPath.TryGetValue(layer.Name, out var node))
			{
				continue;
			}

			foreach (var policy in node.ReturnValuePolicies)
			{
				var result = policy.Evaluate(expression, semanticModel, cancellationToken);
				if (result is not null)
				{
					return result;
				}
			}
		}

		return null;
	}
}
