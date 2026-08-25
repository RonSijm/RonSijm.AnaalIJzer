using System.Collections.Immutable;
using RonSijm.AnaalIJzer.ConfigurationEditing.Editing;
using RonSijm.AnaalIJzer.ConfigurationEditing.Model;
using RonSijm.AnaalIJzer.ConfigurationEditing.Sites;

namespace RonSijm.AnaalIJzer.GraphEditor.Wpf.Controls;

internal sealed partial class ArchitectureGraphCanvas
{
	private sealed partial class NodifyGraphConnectionViewModel
	{
		private void Remove()
		{
			if (IsEvidence)
			{
				return;
			}

			if (confirmationHandler is not null && !confirmationHandler("Remove " + Kind + " from '" + From + "' to '" + To + "'?"))
			{
				return;
			}

			var result = editService.RemoveDependency(EditHandle);
			ReportEditResult(result, true);
		}

		private void ToggleAllowedSite(string site)
		{
			var sites = ToggleSite(allowedSites, site);
			SetSites(sites.Length == 0 ? ArchitectureSiteFilterEditMode.All : ArchitectureSiteFilterEditMode.AllowedSites, sites);
		}

		private void ToggleBlockedSite(string site)
		{
			var sites = ToggleSite(blockedSites, site);
			SetSites(sites.Length == 0 ? ArchitectureSiteFilterEditMode.All : ArchitectureSiteFilterEditMode.BlockedSites, sites);
		}

		private void SetSites(ArchitectureSiteFilterEditMode mode, ImmutableArray<string> sites)
		{
			if (IsEvidence)
			{
				return;
			}

			var result = editService.SetDependencySites(EditHandle, mode, sites);
			if (result.Succeeded)
			{
				allowedSites = mode == ArchitectureSiteFilterEditMode.AllowedSites ? sites : ImmutableArray<string>.Empty;
				blockedSites = mode == ArchitectureSiteFilterEditMode.BlockedSites ? sites : ImmutableArray<string>.Empty;
				RefreshSitePresentation();
			}

			ReportEditResult(result);
		}

		private void ReportEditResult(ArchitectureConfigurationEditResult result, bool clearSelection = false)
		{
			editResultHandler?.Invoke(result, clearSelection);
		}
	}
}
