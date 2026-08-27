using System.Collections.Immutable;
using RonSijm.AnaalIJzer.Core.Matchers;
using RonSijm.AnaalIJzer.Core.Matchers.Conditions;

namespace RonSijm.AnaalIJzer.Core.Exceptions.Tests.Exceptions;

public sealed class ArchitectureExceptionCoreTests
{
	[Fact]
	public void ExceptionPolicy_RequiresMetadata_WhenAnyRequirementIsEnabled()
	{
		var policy = new ArchitectureExceptionPolicy(true, true, false, false, 14, null, "Architecture.anl", 1, 1);

		var result = policy.RequiresMetadata;

		result.Should().BeTrue();
	}

	[Theory]
	[InlineData(ArchitectureExceptionStatus.Active, true)]
	[InlineData(ArchitectureExceptionStatus.ExpiringSoon, true)]
	[InlineData(ArchitectureExceptionStatus.Invalid, false)]
	[InlineData(ArchitectureExceptionStatus.Expired, false)]
	[InlineData(ArchitectureExceptionStatus.Stale, false)]
	public void ExceptionDefinition_IsActive_DependsOnStatus(ArchitectureExceptionStatus status, bool expected)
	{
		var definition = CreateDefinition("Target", status);

		var result = definition.IsActive;

		result.Should().Be(expected);
	}

	[Fact]
	public void ExceptionMatcher_ReturnsDeepestNestedMatchingDepth()
	{
		var matcher = new ExceptionMatcher(
			CreateDefinition("Target", ArchitectureExceptionStatus.Active),
			[
				new ExceptionMatcher(
					CreateDefinition("Target", ArchitectureExceptionStatus.Active),
					ImmutableArray<ExceptionMatcher>.Empty)
			]);

		var result = matcher.FindDeepestMatchingDepth("Target", "Example", symbol: null, depth: 1);

		result.Should().Be(2);
	}

	[Fact]
	public void ExceptionMatcher_IgnoresInactiveDefinitionsAndTheirChildren()
	{
		var matcher = new ExceptionMatcher(
			CreateDefinition("Target", ArchitectureExceptionStatus.Invalid),
			[
				new ExceptionMatcher(
					CreateDefinition("Target", ArchitectureExceptionStatus.Active),
					ImmutableArray<ExceptionMatcher>.Empty)
			]);

		var result = matcher.FindDeepestMatchingDepth("Target", "Example", symbol: null, depth: 1);

		result.Should().Be(0);
	}

	[Fact]
	public void ClockFreeze_OverridesAndRestoresUtcNow()
	{
		var frozen = new DateTime(2001, 2, 3, 4, 5, 6, DateTimeKind.Utc);

		using (ArchitectureClock.Freeze(frozen))
		{
			ArchitectureClock.UtcNow.Should().Be(frozen);
			ArchitectureClock.UtcToday.Should().Be(frozen.Date);
		}

		ArchitectureClock.UtcNow.Should().NotBe(frozen);
	}

	[Fact]
	public void Evaluator_ReturnsInvalidReview_WhenReasonIsRequiredButMissing()
	{
		var policy = new ArchitectureExceptionPolicy(true, true, false, false, 14, null, "Architecture.anl", 1, 1);
		var metadata = new ArchitectureExceptionMetadata(null, "Team", null, null);

		var result = ArchitectureExceptionEvaluator.Evaluate(policy, "Class", "typeName=\"Target\"", metadata, "Application", "Architecture.anl", 10, 4);

		result.Should().NotBeNull();
		result.Value.Status.Should().Be(ArchitectureExceptionStatus.Invalid);
		result.Value.Message.Should().Contain("missing required reason metadata");
	}

	[Fact]
	public void Evaluator_ReturnsInvalidReview_WhenExpiresOnTextIsInvalid()
	{
		var policy = new ArchitectureExceptionPolicy(true, false, false, false, 14, null, "Architecture.anl", 1, 1);
		var metadata = new ArchitectureExceptionMetadata("Temporary", "Team", "not-a-date", null);

		var result = ArchitectureExceptionEvaluator.Evaluate(policy, "Class", "typeName=\"Target\"", metadata, "Application", "Architecture.anl", 10, 4);

		result.Should().NotBeNull();
		result.Value.Status.Should().Be(ArchitectureExceptionStatus.Invalid);
		result.Value.Message.Should().Contain("invalid expiresOn date");
	}

	[Fact]
	public void Evaluator_ReturnsExpiredReview_WhenExpiryIsInThePast()
	{
		using var frozenClock = ArchitectureClock.Freeze(new DateTime(2026, 8, 16, 0, 0, 0, DateTimeKind.Utc));

		var policy = new ArchitectureExceptionPolicy(true, false, false, false, 14, null, "Architecture.anl", 1, 1);
		var metadata = new ArchitectureExceptionMetadata("Temporary", "Team", "2026-08-15", new DateTime(2026, 8, 15, 0, 0, 0, DateTimeKind.Utc));

		var result = ArchitectureExceptionEvaluator.Evaluate(policy, "Class", "typeName=\"Target\"", metadata, "Application", "Architecture.anl", 10, 4);

		result.Should().NotBeNull();
		result.Value.Status.Should().Be(ArchitectureExceptionStatus.Expired);
		result.Value.Message.Should().Contain("has expired on 2026-08-15");
	}

	[Fact]
	public void Evaluator_ReturnsExpiringSoonReview_WhenInsideWarningWindow()
	{
		using var frozenClock = ArchitectureClock.Freeze(new DateTime(2026, 8, 16, 0, 0, 0, DateTimeKind.Utc));

		var policy = new ArchitectureExceptionPolicy(true, false, false, false, 14, null, "Architecture.anl", 1, 1);
		var metadata = new ArchitectureExceptionMetadata("Temporary", "Team", "2026-08-20", new DateTime(2026, 8, 20, 0, 0, 0, DateTimeKind.Utc));

		var result = ArchitectureExceptionEvaluator.Evaluate(policy, "Class", "typeName=\"Target\"", metadata, "Application", "Architecture.anl", 10, 4);

		result.Should().NotBeNull();
		result.Value.Status.Should().Be(ArchitectureExceptionStatus.ExpiringSoon);
		result.Value.Message.Should().Contain("expires in 4 days on 2026-08-20");
	}

	[Fact]
	public void Evaluator_ReturnsNull_WhenPolicyIsDisabled()
	{
		var metadata = new ArchitectureExceptionMetadata("Temporary", "Team", null, null);

		var result = ArchitectureExceptionEvaluator.Evaluate(ArchitectureExceptionPolicy.Disabled, "Class", "typeName=\"Target\"", metadata, "Application", "Architecture.anl", 10, 4);

		result.Should().BeNull();
	}

	[Fact]
	public void Evaluator_CreateStaleMessage_UsesDefinitionVocabulary()
	{
		var definition = CreateDefinition("Target", ArchitectureExceptionStatus.Active);

		var result = ArchitectureExceptionEvaluator.CreateStaleMessage(definition, "layer");

		result.Should().Contain("Class");
		result.Should().Contain("typeName=\"Target\"");
		result.Should().Contain("matches no type in the inspected layer");
	}

	private static ArchitectureExceptionDefinition CreateDefinition(string targetName, ArchitectureExceptionStatus status)
	{
		var matcher = new PatternMatcher(MatchTarget.TypeName, MatchKind.Equals, targetName);
		var metadata = new ArchitectureExceptionMetadata("Temporary", "Team", null, null);
		var result = new ArchitectureExceptionDefinition(
			"Class",
			$"typeName=\"{targetName}\"",
			matcher,
			metadata,
			ImmutableArray<ArchitectureExceptionDefinition>.Empty,
			"Application",
			"Architecture.anl",
			1,
			1,
			status);

		return result;
	}
}
