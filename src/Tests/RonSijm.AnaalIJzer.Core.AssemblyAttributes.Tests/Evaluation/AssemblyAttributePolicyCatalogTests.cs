namespace RonSijm.AnaalIJzer.Core.AssemblyAttributes.Tests.Evaluation;

public sealed class AssemblyAttributePolicyCatalogTests
{
	[Fact]
	public void ForbiddenRule_RejectsOnlyTheSelectedFriendAssembly()
	{
		var attributes = GetAttributes("""
			using System.Runtime.CompilerServices;
			[assembly: InternalsVisibleTo("AllowedExample")]
			[assembly: InternalsVisibleTo("NotAllowedExample")]
			""");
		var catalog = new AssemblyAttributePolicyCatalog(
		[
			new AssemblyAttributePolicy(
				[],
				[CreateRule("NotAllowedExample")],
				"Only reviewed projects receive kitchen keys.",
				"Architecture.anl",
				1,
				1)
		]);

		var evaluations = attributes
			.Select(attribute => catalog.Evaluate(attribute))
			.Where(evaluation => evaluation is not null)
			.Select(evaluation => evaluation!.Value)
			.ToArray();

		var evaluation = evaluations.Should().ContainSingle().Subject;
		evaluation.Rule.DisplayName.Should().Contain("NotAllowedExample");
		evaluation.Reason.Should().Contain("blocks");
	}

	[Fact]
	public void AllowedRule_RejectsAnotherValueOfTheSameSelectedAttribute()
	{
		var attributes = GetAttributes("""
			using System.Runtime.CompilerServices;
			[assembly: InternalsVisibleTo("AllowedExample")]
			[assembly: InternalsVisibleTo("NotAllowedExample")]
			""");
		var catalog = new AssemblyAttributePolicyCatalog(
		[
			new AssemblyAttributePolicy(
				[CreateRule("AllowedExample")],
				[],
				null,
				"Architecture.anl",
				1,
				1)
		]);

		var evaluations = attributes
			.Select(attribute => catalog.Evaluate(attribute))
			.Where(evaluation => evaluation is not null)
			.Select(evaluation => evaluation!.Value)
			.ToArray();

		var evaluation = evaluations.Should().ContainSingle().Subject;
		evaluation.Reason.Should().Contain("no allowed rule");
	}

	[Fact]
	public void AllowedRules_AreAlternativesForTheSameSelectedAttribute()
	{
		var attributes = GetAttributes("""
			using System.Runtime.CompilerServices;
			[assembly: InternalsVisibleTo("ApprovedKitchen")]
			[assembly: InternalsVisibleTo("ApprovedBakery")]
			[assembly: InternalsVisibleTo("NotAllowedExample")]
			""");
		var catalog = new AssemblyAttributePolicyCatalog(
		[
			new AssemblyAttributePolicy(
				[CreateRule("ApprovedKitchen"), CreateRule("ApprovedBakery")],
				[],
				null,
				"Architecture.anl",
				1,
				1)
		]);

		var evaluations = attributes
			.Select(attribute => catalog.Evaluate(attribute))
			.Where(evaluation => evaluation is not null)
			.Select(evaluation => evaluation!.Value)
			.ToArray();

		evaluations.Should().ContainSingle();
	}

	[Fact]
	public void ForbiddenRule_WinsWhenAnAllowedRuleAlsoMatches()
	{
		var attribute = GetAttributes("""
			using System.Runtime.CompilerServices;
			[assembly: InternalsVisibleTo("NotAllowedExample")]
			""").Single();
		var catalog = new AssemblyAttributePolicyCatalog(
		[
			new AssemblyAttributePolicy(
				[CreateRule("NotAllowedExample")],
				[CreateRule("NotAllowedExample")],
				null,
				"Architecture.anl",
				1,
				1)
		]);

		var evaluation = catalog.Evaluate(attribute);

		evaluation.Should().NotBeNull();
		evaluation!.Value.Reason.Should().Contain("blocks");
	}

	[Fact]
	public void NamedAndPositionalArguments_MustBothMatch()
	{
		var attributes = GetAttributes("""
			using System;
			[assembly: FriendAccess("AllowedExample", Purpose = "Tests")]
			[assembly: FriendAccess("AllowedExample", Purpose = "Production")]

			[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
			public sealed class FriendAccessAttribute(string assemblyName) : Attribute
			{
				public string Purpose { get; set; } = string.Empty;
			}
			""");
		var rule = new AssemblyAttributeRule(
			new PatternMatcher(MatchTarget.TypeName, MatchKind.Equals, "FriendAccessAttribute"),
			[
				new AssemblyAttributeArgumentMatcher(0, null, new PatternMatcher(MatchTarget.TypeName, MatchKind.Equals, "AllowedExample")),
				new AssemblyAttributeArgumentMatcher(null, "Purpose", new PatternMatcher(MatchTarget.TypeName, MatchKind.Equals, "Production"))
			],
			"FriendAccess Production",
			null,
			"Architecture.anl",
			1,
			1);
		var catalog = new AssemblyAttributePolicyCatalog(
		[
			new AssemblyAttributePolicy([rule], [], null, "Architecture.anl", 1, 1)
		]);

		var evaluations = attributes
			.Select(attribute => catalog.Evaluate(attribute))
			.Where(evaluation => evaluation is not null)
			.Select(evaluation => evaluation!.Value)
			.ToArray();

		evaluations.Should().ContainSingle();
	}

	private static AssemblyAttributeRule CreateRule(string friendAssemblyName)
	{
		var result = new AssemblyAttributeRule(
			new PatternMatcher(MatchTarget.TypeName, MatchKind.EqualsFullName, "System.Runtime.CompilerServices.InternalsVisibleToAttribute"),
			[new AssemblyAttributeArgumentMatcher(0, null, new PatternMatcher(MatchTarget.TypeName, MatchKind.Equals, friendAssemblyName))],
			"InternalsVisibleTo " + friendAssemblyName,
			null,
			"Architecture.anl",
			1,
			1);

		return result;
	}

	private static ImmutableArray<AttributeData> GetAttributes(string source)
	{
		var syntaxTree = CSharpSyntaxTree.ParseText(source);
		var compilation = CSharpCompilation.Create(
			"AssemblyAttributePolicyTests",
			[syntaxTree],
			TrustedPlatformReferences.Value,
			new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
		compilation.GetDiagnostics().Should().NotContain(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
		var result = compilation.Assembly.GetAttributes();

		return result;
	}

	private static readonly Lazy<MetadataReference[]> TrustedPlatformReferences = new(() =>
	{
		var result = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
			.Split(Path.PathSeparator)
			.Select(path => MetadataReference.CreateFromFile(path))
			.ToArray();

		return result;
	});
}
