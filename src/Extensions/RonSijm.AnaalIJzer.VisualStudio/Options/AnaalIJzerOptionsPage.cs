using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;
using RonSijm.AnaalIJzer.VisualStudio.Diagnostics;

namespace RonSijm.AnaalIJzer.VisualStudio.Options;

[Guid("5d45288a-bff3-44d8-96a9-d7271b425a30")]
public sealed partial class AnaalIJzerOptionsPage : DialogPage
{
	private bool _isLoadingSettings;

	public override void LoadSettingsFromStorage()
	{
		_isLoadingSettings = true;
		try
		{
			base.LoadSettingsFromStorage();
		}
		finally
		{
			_isLoadingSettings = false;
		}

		ArchitectureVisualStudioOptions.Publish(ToEditorOptions());
		ArchitectureVisualStudioLog.Info("Options loaded from storage.");
	}

	protected override void OnApply(PageApplyEventArgs e)
	{
		base.OnApply(e);
		ArchitectureVisualStudioOptions.Publish(ToEditorOptions());
		ArchitectureVisualStudioLog.Info("Options applied from settings page.");
	}
}
