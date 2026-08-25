using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RonSijm.AnaalIJzer.Conditions;
using RonSijm.AnaalIJzer.Engine.DependencyRules;
using RonSijm.AnaalIJzer.Engine.NameRules;

namespace RonSijm.AnaalIJzer.Core.NameRules.Tests.NameRules;

public sealed class NameRuleCoreTests
{
	[Fact]
	public void Normalize_StripsVerbPrefixesSeparatorsAndThisSegments()
	{
		var result = NameRuleNameNormalizer.Normalize("this.GetPatient_Id");

		result.Should().Be("patient.id");
	}

	[Fact]
	public void MatchingRule_Evaluate_ReturnsViolationForNormalizedMismatch()
	{
		var rule = CreateRule();
		var source = CreateValueSubject("patientId");
		var target = CreateValueSubject("doctorId");

		var result = rule.Evaluate(source, target, DependencySites.Method);

		result.Should().NotBeNull();
		result!.Value.RuleKind.Should().Be(NameRuleKind.RequireMatchingNames);
		result.Value.Reason.Should().Contain("source 'patientId' normalizes to 'patient.id'");
		result.Value.Reason.Should().Contain("target 'doctorId' normalizes to 'doctor.id'");
	}

	[Fact]
	public void MatchingRule_Evaluate_AllowsMatchingAllowMappingOnAllowedSite()
	{
		var allowMapping = new NameRuleAllowMapping(
			[ExactName("patientId")],
			[ExactName("doctorId")],
			new DependencySiteFilter(ImmutableHashSet.Create(StringComparer.Ordinal, DependencySites.Method), ImmutableHashSet<string>.Empty),
			null);
		var rule = CreateRule(allowMappings: [allowMapping]);
		var source = CreateValueSubject("patientId");
		var target = CreateValueSubject("doctorId");

		var result = rule.Evaluate(source, target, DependencySites.Method);

		result.Should().BeNull();
	}

	[Fact]
	public void MatchingRule_Evaluate_ExplainsWhenAllowMappingMatchesButSiteIsBlocked()
	{
		var allowMapping = new NameRuleAllowMapping(
			[ExactName("patientId")],
			[ExactName("doctorId")],
			new DependencySiteFilter(ImmutableHashSet.Create(StringComparer.Ordinal, DependencySites.Method), ImmutableHashSet<string>.Empty),
			null);
		var rule = CreateRule(allowMappings: [allowMapping]);
		var source = CreateValueSubject("patientId");
		var target = CreateValueSubject("doctorId");

		var result = rule.Evaluate(source, target, DependencySites.Constructor);

		result.Should().NotBeNull();
		result!.Value.Reason.Should().Contain("a matching <Allow> mapping is configured");
		result.Value.Reason.Should().Contain("allowedSites does not include Constructor");
	}

	[Fact]
	public void SubjectFactory_CreateType_UnwrapsNullableAndPreservesArrayDisplay()
	{
		var compilation = CreateCompilation("""
			namespace Demo;
			public sealed class PatientId { }
			public sealed class Holder
			{
				public PatientId[] Patients { get; set; } = [];
				public int? OptionalCount { get; set; }
			}
			""");
		var holder = compilation.GetTypeByMetadataName("Demo.Holder")!;
		var patientsType = ((IPropertySymbol)holder.GetMembers("Patients").Single()).Type;
		var optionalCountType = ((IPropertySymbol)holder.GetMembers("OptionalCount").Single()).Type;

		var patientsResult = NameRuleSubjectFactory.CreateType(patientsType);
		var optionalCountResult = NameRuleSubjectFactory.CreateType(optionalCountType);

		patientsResult.Should().NotBeNull();
		patientsResult!.Value.DisplayName.Should().Be("PatientId[]");
		optionalCountResult.Should().NotBeNull();
		optionalCountResult!.Value.DisplayName.Should().Be("Int32");
	}

	[Fact]
	public void SemanticResolver_CreateExpressionSubject_PrefersContainingTypeForMemberAccess()
	{
		var (model, memberAccess) = GetSingleNode<MemberAccessExpressionSyntax>("""
			namespace Demo;
			public sealed class Request
			{
				public PatientId PatientId { get; set; } = new();
			}

			public sealed class PatientId { }

			public sealed class Handler
			{
				public PatientId Run(Request request)
				{
					return request.PatientId;
				}
			}
			""");

		var result = NameRuleSemanticSubjectResolver.CreateExpressionSubject(memberAccess, model, CancellationToken.None);

		result.Should().NotBeNull();
		result!.Value.DisplayName.Should().Be("Request.PatientId");
		result.Value.CandidateNames.Should().Contain("PatientId");
		result.Value.CandidateNames.Should().Contain("request.PatientId");
	}

	[Fact]
	public void SemanticResolver_GetAssignmentSite_UsesPropertyAndLocalSites()
	{
		var (propertyModel, propertyAssignment) = GetSingleNode<AssignmentExpressionSyntax>("""
			namespace Demo;
			public sealed class Holder
			{
				public int PatientId { get; set; }

				public void Run()
				{
					PatientId = 42;
				}
			}
			""", "PatientId = 42");
		var (localModel, localAssignment) = GetSingleNode<AssignmentExpressionSyntax>("""
			namespace Demo;
			public sealed class Holder
			{
				public void Run()
				{
					var patientId = 0;
					patientId = 42;
				}
			}
			""", "patientId = 42");
		var propertyTarget = (ExpressionSyntax)propertyAssignment.Left;
		var localTarget = (ExpressionSyntax)localAssignment.Left;

		var propertyResult = NameRuleSemanticSubjectResolver.GetAssignmentSite(propertyTarget, propertyModel, CancellationToken.None);
		var localResult = NameRuleSemanticSubjectResolver.GetAssignmentSite(localTarget, localModel, CancellationToken.None);

		propertyResult.Should().Be(DependencySites.Property);
		localResult.Should().Be(DependencySites.Local);
	}

	private static NameMatchingRule CreateRule(ImmutableArray<NameRuleAllowMapping> allowMappings = default)
	{
		var result = new NameMatchingRule(
			NameRuleKind.RequireMatchingNames,
			NameRuleTrigger.ValueMovement,
			ImmutableArray<PatternMatcher>.Empty,
			ImmutableArray<PatternMatcher>.Empty,
			ImmutableArray<PatternMatcher>.Empty,
			allowMappings.IsDefault ? ImmutableArray<NameRuleAllowMapping>.Empty : allowMappings,
			DependencySiteFilter.All,
			"Application",
			null,
			"Architecture.anl",
			12,
			3);

		return result;
	}

	private static NameRuleSubject CreateValueSubject(string name)
	{
		var result = new NameRuleSubject(name, [name], symbol: null);

		return result;
	}

	private static PatternMatcher ExactName(string value)
	{
		var result = new PatternMatcher(MatchTarget.TypeName, MatchKind.Equals, value);

		return result;
	}

	private static CSharpCompilation CreateCompilation(string source)
	{
		var tree = CSharpSyntaxTree.ParseText(source, cancellationToken: CancellationToken.None);
		var result = CSharpCompilation.Create(
			"Demo",
			[tree],
			[
				MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
				MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
				MetadataReference.CreateFromFile(typeof(System.Runtime.AssemblyTargetedPatchBandAttribute).Assembly.Location)
			]);

		return result;
	}

	private static (SemanticModel Model, TNode Node) GetSingleNode<TNode>(string source, string? containingText = null)
		where TNode : SyntaxNode
	{
		var compilation = CreateCompilation(source);
		var tree = compilation.SyntaxTrees.Single();
		var model = compilation.GetSemanticModel(tree);
		var root = tree.GetRoot(CancellationToken.None);
		var node = containingText is null
			? root.DescendantNodes().OfType<TNode>().Single()
			: root.DescendantNodes().OfType<TNode>().Single(candidate => candidate.ToString().Contains(containingText, StringComparison.Ordinal));
		var result = (model, node);

		return result;
	}
}
