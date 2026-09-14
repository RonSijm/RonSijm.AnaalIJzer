using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using RonSijm.AnaalIJzer.Core.Indicators;

namespace RonSijm.AnaalIJzer.Core.Observations.Tests;

public sealed class DependencySiteObservationScannerTests
{
	[Fact]
	public void Scan_RecognizesEveryArchitecturalDependencySite()
	{
		var cancellationToken = TestContext.Current.CancellationToken;
		var compilation = CSharpCompilation.Create(
			"DependencySites",
			[CSharpSyntaxTree.ParseText(SourceText.From("""
				using System;
				[Marker]
				public class Caller : Base, IContract
				{
					private Target field;
					public Target Property { get; set; }
					public Caller(Target constructor) { field = constructor; }
					public Target Method(Target parameter)
					{
						Target local = parameter;
						var created = new Target();
						GenericMethods.Generic<GenericType<Target>>();
						return Target.Shared;
					}
				}
				public class Base { }
				public interface IContract { }
				public class Target { public static Target Shared { get; } = new Target(); }
				public class MarkerAttribute : Attribute { }
				public class GenericType<T> { }
				public static class GenericMethods { public static void Generic<T>() { } }
				"""), path: "DependencySites.cs", cancellationToken: cancellationToken)],
			[MetadataReference.CreateFromFile(typeof(object).Assembly.Location)]);

		var observations = DependencySiteObservationScanner.Scan(compilation, cancellationToken);
		var sites = observations.Select(observation => observation.Site).Distinct(StringComparer.Ordinal).ToArray();
		var layerObservations = ProjectDependencyScanner.Scan(compilation, _ => "Layer", cancellationToken);
		var layerSites = layerObservations.Select(observation => observation.Site).Distinct(StringComparer.Ordinal).ToArray();

		sites.Should().Contain(DependencySites.All);
		layerSites.Should().Contain(DependencySites.All);
	}
}
