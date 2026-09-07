using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace RonSijm.AnaalIJzer.Core.AssemblyAttributes.Model;

/// <summary>Root-level policies that inspect the attributes emitted on one compiled assembly.</summary>
public readonly struct AssemblyAttributePolicyCatalog(ImmutableArray<AssemblyAttributePolicy> policies)
{
	public static AssemblyAttributePolicyCatalog Empty { get; } = new([]);

	public ImmutableArray<AssemblyAttributePolicy> Policies { get; } = policies.IsDefault ? [] : policies;

	public bool HasPolicies => !Policies.IsDefaultOrEmpty;

	public AssemblyAttributePolicyEvaluation? Evaluate(AttributeData attribute)
	{
		foreach (var policy in Policies)
		{
			var evaluation = policy.Evaluate(attribute);
			if (evaluation is not null)
			{
				return evaluation;
			}
		}

		return null;
	}
}
