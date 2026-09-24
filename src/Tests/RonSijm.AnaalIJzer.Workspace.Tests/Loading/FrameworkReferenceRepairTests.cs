using Microsoft.CodeAnalysis.CSharp;
using RonSijm.AnaalIJzer.Workspace.Loading;

namespace RonSijm.AnaalIJzer.Workspace.Tests.Loading;

public sealed class FrameworkReferenceRepairTests
{
    [Fact]
    public void RepairIfNeeded_AddsInstalledFrameworkReferencesWhenSystemIsMissing()
    {
        WorkspaceBuildEnvironment.Initialize();
        var syntaxTree = CSharpSyntaxTree.ParseText("public sealed class Pizza { }", cancellationToken: TestContext.Current.CancellationToken);
        var compilation = CSharpCompilation.Create("Pizza", [syntaxTree], options: new CSharpCompilationOptions(Microsoft.CodeAnalysis.OutputKind.DynamicallyLinkedLibrary));

        var result = FrameworkReferenceRepair.RepairIfNeeded(compilation, "net10.0");

        result.GetTypeByMetadataName("System.Object").Should().NotBeNull();
        result.GetDiagnostics(TestContext.Current.CancellationToken).Where(diagnostic => diagnostic.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error).Should().BeEmpty();
    }
}