namespace RonSijm.AnaalIJzer.Core.Visibility.Tests;

public sealed class VisibilityPolicyTokenTests
{
	public static TheoryData<string, string> AccessibilityCases =>
		new()
		{
			{ "public", "Public" },
			{ "INTERNAL", "Internal" },
			{ "Protected", "Protected" },
			{ "protectedinternal", "ProtectedInternal" },
			{ "PrivateProtected", "PrivateProtected" },
			{ "private", "Private" },
			{ "file", "File" }
		};

	public static TheoryData<string, string> TargetCases =>
		new()
		{
			{ "type", "Type" },
			{ "CONSTRUCTOR", "Constructor" },
			{ "Method", "Method" },
			{ "property", "Property" },
			{ "Field", "Field" },
			{ "event", "Event" },
			{ "Operator", "Operator" },
			{ "conversion", "Conversion" },
			{ "NestedType", "NestedType" }
		};

	[Theory]
	[MemberData(nameof(AccessibilityCases))]
	public void AccessibilityTokens_ParseCaseInsensitively(string value, string expected)
	{
		var parsed = ArchitectureAccessibilityParser.TryParse(value, out var accessibility);

		parsed.Should().BeTrue();
		accessibility.ToString().Should().Be(expected);
	}

	[Theory]
	[MemberData(nameof(TargetCases))]
	public void TargetTokens_ParseCaseInsensitively(string value, string expected)
	{
		var parsed = VisibilityPolicyTargetParser.TryParse(value, out var target);

		parsed.Should().BeTrue();
		target.ToString().Should().Be(expected);
	}

	[Theory]
	[InlineData("")]
	[InlineData("Unknown")]
	[InlineData("999")]
	public void UnknownTokens_AreRejected(string value)
	{
		ArchitectureAccessibilityParser.TryParse(value, out _).Should().BeFalse();
		VisibilityPolicyTargetParser.TryParse(value, out _).Should().BeFalse();
	}
}
