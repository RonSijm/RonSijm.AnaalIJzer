using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;

namespace RonSijm.IJzer.VisualStudio.Graphs;

[Guid("db8e5592-809a-43ed-b2da-9cc2a44867cc")]
public sealed class ArchitectureGraphToolWindow : ToolWindowPane
{
    public ArchitectureGraphToolWindow() : base(null)
    {
        Caption = "IJzer Dependency Graphs";
        Content = new ArchitectureGraphToolWindowControl();
    }
}