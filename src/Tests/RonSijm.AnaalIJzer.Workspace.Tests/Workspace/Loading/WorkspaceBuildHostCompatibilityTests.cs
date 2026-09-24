using RonSijm.AnaalIJzer.Workspace.Loading;

namespace RonSijm.AnaalIJzer.Workspace.Tests.Workspace.Loading;

public sealed class WorkspaceBuildHostCompatibilityTests
{
    [Theory]
    [InlineData(4, 14, 4, 14, true)]
    [InlineData(4, 14, 4, 13, false)]
    [InlineData(4, 14, 5, 14, false)]
    public void AreCompatible_RequiresMatchingMajorAndMinorVersions(
        int workspaceMajor,
        int workspaceMinor,
        int buildHostMajor,
        int buildHostMinor,
        bool expected)
    {
        var workspaceVersion = new Version(workspaceMajor, workspaceMinor);
        var buildHostVersion = new Version(buildHostMajor, buildHostMinor);

        var result = WorkspaceBuildHostCompatibility.AreCompatible(workspaceVersion, buildHostVersion);

        result.Should().Be(expected);
    }
}