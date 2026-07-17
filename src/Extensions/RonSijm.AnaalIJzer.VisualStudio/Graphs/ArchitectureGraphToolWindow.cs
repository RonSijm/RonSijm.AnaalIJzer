using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;

namespace RonSijm.AnaalIJzer.VisualStudio;

[Guid("db8e5592-809a-43ed-b2da-9cc2a44867cc")]
public sealed class ArchitectureGraphToolWindow : ToolWindowPane
{
	public ArchitectureGraphToolWindow() : base(null)
	{
		Caption = "AnaalIJzer Dependency Graphs";
		Content = new ArchitectureGraphToolWindowControl();
	}
}
