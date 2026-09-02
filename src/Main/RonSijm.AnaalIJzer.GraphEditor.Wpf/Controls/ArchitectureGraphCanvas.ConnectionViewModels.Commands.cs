using System.Collections.Immutable;
using RonSijm.AnaalIJzer.ConfigurationEditing.Editing;
using RonSijm.AnaalIJzer.GraphApplication.Selection;

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

			if (_confirmationHandler is not null && !_confirmationHandler("Remove " + Kind + " from '" + From + "' to '" + To + "'?"))
			{
				return;
			}

			var result = _editService.RemoveDependency(EditHandle);
			ReportEditResult(result, true);
		}

		private void ShowConfigurationFixes()
		{
			if (IsEvidence)
			{
				_selectionHandler?.Invoke(ArchitectureGraphSelection.ForCodeEvidence(From, To, SiteText, EvidenceDetails));
				return;
			}

			_selectionHandler?.Invoke(ArchitectureGraphSelection.ForDependency(EditHandle));
		}

		private void ToggleAllowedSite(string site)
		{
			var sites = ToggleSite(_allowedSites, site);
			SetSites(sites.Length == 0 ? ArchitectureSiteFilterEditMode.All : ArchitectureSiteFilterEditMode.AllowedSites, sites);
		}

		private void ToggleBlockedSite(string site)
		{
			var sites = ToggleSite(_blockedSites, site);
			SetSites(sites.Length == 0 ? ArchitectureSiteFilterEditMode.All : ArchitectureSiteFilterEditMode.BlockedSites, sites);
		}

		private void SetSites(ArchitectureSiteFilterEditMode mode, ImmutableArray<string> sites)
		{
			if (IsEvidence)
			{
				return;
			}

			var result = _editService.SetDependencySites(EditHandle, mode, sites);
			if (result.Succeeded)
			{
				_allowedSites = mode == ArchitectureSiteFilterEditMode.AllowedSites ? sites : ImmutableArray<string>.Empty;
				_blockedSites = mode == ArchitectureSiteFilterEditMode.BlockedSites ? sites : ImmutableArray<string>.Empty;
				RefreshSitePresentation();
			}

			ReportEditResult(result);
		}

		private void ReportEditResult(ArchitectureConfigurationEditResult result, bool clearSelection = false)
		{
			_editResultHandler?.Invoke(result, clearSelection);
		}
	}
}
