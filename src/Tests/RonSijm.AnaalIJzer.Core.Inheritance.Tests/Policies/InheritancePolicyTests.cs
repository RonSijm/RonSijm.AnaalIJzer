using System.Collections.Immutable;
using RonSijm.AnaalIJzer.Core.Inheritance.Policies;

namespace RonSijm.AnaalIJzer.Core.Inheritance.Tests.Policies;

public sealed class InheritancePolicyTests
{
	[Fact]
	public void Evaluate_RejectsClassWithoutRequiredBaseType()
	{
		var symbol = GetNamedTypeSymbol(
			"""
			namespace Demo.Framework
			{
				public abstract class Entity { }
			}

			namespace Demo.Persistence
			{
				public class CandyEntity { }
			}
			""",
			"Demo.Persistence.CandyEntity");
		var policy = new InheritancePolicy(
			"PersistenceEntities",
			ImmutableHashSet.Create("Class"),
			ImmutableHashSet.Create("Entity"),
			ImmutableHashSet<string>.Empty,
			"Persistence entities inherit Entity.",
			"Architecture.anl",
			12,
			5);

		var evaluation = policy.Evaluate(symbol);

		evaluation.Should().NotBeNull();
		evaluation.Value.ViolationKind.Should().Be(InheritanceViolationKind.MissingRequiredBaseType);
		evaluation.Value.Reason.Should().Contain("requires a base type matching Entity");
	}

	[Fact]
	public void Evaluate_AllowsClassWithRequiredBaseType()
	{
		var symbol = GetNamedTypeSymbol(
			"""
			namespace Demo.Framework
			{
				public abstract class Entity { }
			}

			namespace Demo.Persistence
			{
				public class CandyEntity : Demo.Framework.Entity { }
			}
			""",
			"Demo.Persistence.CandyEntity");
		var policy = new InheritancePolicy(
			"PersistenceEntities",
			ImmutableHashSet.Create("Class"),
			ImmutableHashSet.Create("Entity"),
			ImmutableHashSet<string>.Empty,
			null,
			"Architecture.anl",
			12,
			5);

		var evaluation = policy.Evaluate(symbol);

		evaluation.Should().BeNull();
	}

	[Fact]
	public void Evaluate_RejectsTypeMissingRequiredInterfaces()
	{
		var symbol = GetNamedTypeSymbol(
			"""
			namespace Demo.Contracts
			{
				public interface IEntityMarker { }
				public interface IAuditedEntity { }
			}

			namespace Demo.Persistence
			{
				public class CandyEntity : Demo.Contracts.IEntityMarker { }
			}
			""",
			"Demo.Persistence.CandyEntity");
		var policy = new InheritancePolicy(
			"PersistenceEntities",
			ImmutableHashSet.Create("Class"),
			ImmutableHashSet<string>.Empty,
			ImmutableHashSet.Create("IEntityMarker", "IAuditedEntity"),
			null,
			"Architecture.anl",
			14,
			5);

		var evaluation = policy.Evaluate(symbol);

		evaluation.Should().NotBeNull();
		evaluation.Value.ViolationKind.Should().Be(InheritanceViolationKind.MissingRequiredInterface);
		evaluation.Value.Reason.Should().Contain("IAuditedEntity");
	}

	[Fact]
	public void Evaluate_IgnoresSymbolsOutsideConfiguredTypeKinds()
	{
		var symbol = GetNamedTypeSymbol(
			"""
			namespace Demo.Persistence
			{
				public interface ICandyEntity { }
			}
			""",
			"Demo.Persistence.ICandyEntity");
		var policy = new InheritancePolicy(
			"PersistenceEntities",
			ImmutableHashSet.Create("Class"),
			ImmutableHashSet.Create("Entity"),
			ImmutableHashSet<string>.Empty,
			null,
			"Architecture.anl",
			16,
			5);

		var evaluation = policy.Evaluate(symbol);

		evaluation.Should().BeNull();
	}

	private static INamedTypeSymbol GetNamedTypeSymbol(string source, string metadataName)
	{
		var syntaxTree = CSharpSyntaxTree.ParseText(source);
		var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
			.Split(Path.PathSeparator)
			.Select(path => MetadataReference.CreateFromFile(path))
			.Cast<MetadataReference>()
			.ToArray();
		var compilation = CSharpCompilation.Create(
			"InheritancePolicyTests",
			[syntaxTree],
			references,
			new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
		var diagnostics = compilation.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ToArray();
		var emitResult = compilation.Emit(Stream.Null);

		diagnostics.Should().BeEmpty();
		emitResult.Success.Should().BeTrue();

		var result = compilation.GetTypeByMetadataName(metadataName);

		result.Should().NotBeNull();

		return result;
	}
}
