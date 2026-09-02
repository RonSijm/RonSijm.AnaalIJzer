using System.Collections.Immutable;
using AwesomeAssertions;
using RonSijm.AnaalIJzer.Core.Editor.Snapshots;
using RonSijm.AnaalIJzer.GraphModel.Model;
using RonSijm.AnaalIJzer.VisualStudio.Graphs;
using Xunit;

namespace RonSijm.AnaalIJzer.VisualStudio.Tests.Graphs;

public sealed class ArchitectureGraphToolWindowStateTests
{
	[Fact]
	public void PublishContext_PreservesWorkspacePathsAcrossGraphOnlyRefresh()
	{
		var initialSnapshot = CreateGraphSnapshot("first.anl");
		var refreshedSnapshot = CreateGraphSnapshot("second.anl");

		try
		{
			ArchitectureGraphToolWindowState.Publish(new ArchitectureGraphToolWindowContext(
				initialSnapshot,
				@"D:\repo\Shop\Example.cs",
				@"D:\repo\Shop\Shop.csproj",
				@"D:\repo\Shop\Shop.slnx"));

			ArchitectureGraphToolWindowState.Publish(refreshedSnapshot);

			ArchitectureGraphToolWindowState.Current.Should().BeSameAs(refreshedSnapshot);
			ArchitectureGraphToolWindowState.CurrentContext.DocumentPath.Should().Be(@"D:\repo\Shop\Example.cs");
			ArchitectureGraphToolWindowState.CurrentContext.ProjectPath.Should().Be(@"D:\repo\Shop\Shop.csproj");
			ArchitectureGraphToolWindowState.CurrentContext.SolutionPath.Should().Be(@"D:\repo\Shop\Shop.slnx");
		}
		finally
		{
			ArchitectureGraphToolWindowState.PublishDetached(ArchitectureGraphSnapshot.Empty);
		}
	}

	[Fact]
	public void PublishDetached_ClearsWorkspaceContext()
	{
		try
		{
			ArchitectureGraphToolWindowState.Publish(new ArchitectureGraphToolWindowContext(
				CreateGraphSnapshot("first.anl"),
				@"D:\repo\Shop\Example.cs",
				@"D:\repo\Shop\Shop.csproj",
				@"D:\repo\Shop\Shop.slnx"));

			ArchitectureGraphToolWindowState.PublishDetached(CreateGraphSnapshot("detached.anl"));

			ArchitectureGraphToolWindowState.CurrentContext.HasWorkspaceContext.Should().BeFalse();
			ArchitectureGraphToolWindowState.CurrentContext.DocumentPath.Should().BeNull();
			ArchitectureGraphToolWindowState.CurrentContext.ProjectPath.Should().BeNull();
			ArchitectureGraphToolWindowState.CurrentContext.SolutionPath.Should().BeNull();
		}
		finally
		{
			ArchitectureGraphToolWindowState.PublishDetached(ArchitectureGraphSnapshot.Empty);
		}
	}

	private static ArchitectureGraphSnapshot CreateGraphSnapshot(string path)
	{
		var result = new ArchitectureGraphSnapshot(
			true,
			false,
			ImmutableArray<ArchitectureGraphLayer>.Empty,
			ImmutableArray<ArchitectureGraphRule>.Empty,
			ImmutableArray<string>.Empty,
			ImmutableArray<string>.Empty,
			new RonSijm.AnaalIJzer.Core.Configuration.Document.Model.ArchitectureConfigurationSource(
				RonSijm.AnaalIJzer.Core.Configuration.Document.Model.ArchitectureConfigurationSourceKind.XmlFile,
				path));

		return result;
	}
}
