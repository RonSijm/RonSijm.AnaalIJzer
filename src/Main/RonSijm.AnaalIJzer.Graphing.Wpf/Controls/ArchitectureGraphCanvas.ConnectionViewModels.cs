using System.Collections.Immutable;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using RonSijm.AnaalIJzer.ConfigurationEditing.Editing;
using RonSijm.AnaalIJzer.ConfigurationEditing.Model;
using RonSijm.AnaalIJzer.ConfigurationEditing.Sites;
using RonSijm.AnaalIJzer.Graphing.ViewModels;
using RonSijm.AnaalIJzer.Graphing.Wpf.Styling;

namespace RonSijm.AnaalIJzer.Graphing.Wpf.Controls;

public sealed partial class ArchitectureGraphCanvas
{
	private sealed class NodifyGraphConnectorViewModel(string layerPath, string title, bool isOutput)
        : INotifyPropertyChanged
    {
		private Point anchor;

        public event PropertyChangedEventHandler? PropertyChanged;

		public string LayerPath { get; } = layerPath;

        public string Title { get; } = title;

        public bool IsOutput { get; } = isOutput;

        public bool IsInput
		{
			get
			{
				var result = !IsOutput;

				return result;
			}
		}

		public Point Anchor
		{
			get { return anchor; }
			set
			{
				if (anchor == value)
				{
					return;
				}

				anchor = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Anchor)));
			}
		}

		public string ToolTip
		{
			get
			{
				var result = LayerPath + " " + Title + " connector";

				return result;
			}
		}
	}

	private sealed class NodifyGraphConnectionViewModel : INotifyPropertyChanged
	{
		private readonly Action<ArchitectureConfigurationEditResult, bool>? editResultHandler;
		private readonly Func<string, bool>? confirmationHandler;
		private readonly ArchitectureGraphCanvasTheme theme;
		private ImmutableArray<string> allowedSites;
		private ImmutableArray<string> blockedSites;
		private string labelText;
		private string toolTip;

		private NodifyGraphConnectionViewModel(
			ArchitectureGraphEdgeViewModel edge,
			NodifyGraphConnectorViewModel output,
			NodifyGraphConnectorViewModel input,
			Action<ArchitectureConfigurationEditResult, bool>? editResultHandler,
			Func<string, bool>? confirmationHandler,
			ArchitectureGraphCanvasTheme theme)
		{
			this.editResultHandler = editResultHandler;
			this.confirmationHandler = confirmationHandler;
			this.theme = theme;
			Output = output;
			Input = input;
			From = edge.From;
			To = edge.To;
			EditHandle = edge.EditHandle;
			Kind = edge.Kind;
			SiteText = edge.SiteText;
			AppliesToDescendants = edge.AppliesToDescendants;
			IsActive = edge.IsActive;
			IsBlocked = edge.IsBlocked;
			IsEvidence = edge.IsEvidence;
			ViolationCount = edge.ViolationCount;
			ObservedUsageCount = edge.ObservedUsageCount;
			EvidenceDetails = edge.Description ?? string.Empty;
			allowedSites = edge.AllowedSites;
			blockedSites = edge.BlockedSites;
			labelText = IsEvidence ? edge.SiteText : FormatLabelText(edge.SiteText, edge.AppliesToDescendants);
			toolTip = IsEvidence
				? FormatEvidenceToolTip(edge.From, edge.To, edge.SiteText, EvidenceDetails)
				: FormatEdgeToolTip(edge.Kind, edge.From, edge.To, edge.SiteText, edge.AppliesToDescendants);
			RemoveCommand = new DelegateCommand(_ => Remove(), _ => !IsEvidence && EditHandle.CanEdit);
			AllowAllSitesCommand = new DelegateCommand(_ => SetSites(ArchitectureSiteFilterEditMode.All, ImmutableArray<string>.Empty), _ => !IsEvidence && EditHandle.CanEdit);
			AllowedSiteOptions = ArchitectureDependencySiteNames.All.Select(site => new NodifySiteFilterOptionViewModel(site, allowedSites.Contains(site, StringComparer.Ordinal), new DelegateCommand(_ => ToggleAllowedSite(site)))).ToImmutableArray();
			BlockedSiteOptions = ArchitectureDependencySiteNames.All.Select(site => new NodifySiteFilterOptionViewModel(site, blockedSites.Contains(site, StringComparer.Ordinal), new DelegateCommand(_ => ToggleBlockedSite(site)))).ToImmutableArray();
		}

		public event PropertyChangedEventHandler? PropertyChanged;

		public NodifyGraphConnectorViewModel Output { get; }

		public NodifyGraphConnectorViewModel Input { get; }

		public ArchitectureDependencyRuleEditHandle EditHandle { get; }

		public string Kind { get; }

		public string From { get; }

		public string To { get; }

		public string SiteText { get; }

		public bool AppliesToDescendants { get; }

		public bool IsActive { get; }

		public bool IsBlocked { get; }

		public bool IsEvidence { get; }

		public int ViolationCount { get; }

		public int ObservedUsageCount { get; }

		public string EvidenceDetails { get; }

		public bool CanEditRule
		{
			get
			{
				var result = !IsEvidence && EditHandle.CanEdit;

				return result;
			}
		}

		public string LabelText
		{
			get { return labelText; }
			private set
			{
				if (labelText == value)
				{
					return;
				}

				labelText = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LabelText)));
			}
		}

		public string ToolTip
		{
			get { return toolTip; }
			private set
			{
				if (toolTip == value)
				{
					return;
				}

				toolTip = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ToolTip)));
			}
		}

		public ICommand RemoveCommand { get; }

		public ICommand AllowAllSitesCommand { get; }

		public ImmutableArray<NodifySiteFilterOptionViewModel> AllowedSiteOptions { get; }

		public ImmutableArray<NodifySiteFilterOptionViewModel> BlockedSiteOptions { get; }

		public bool UsesAllSites
		{
			get
			{
				var result = allowedSites.Length == 0 && blockedSites.Length == 0;

				return result;
			}
		}

		public Brush Stroke
		{
			get
			{
				var result = IsEvidence ? theme.ErrorConnection : IsBlocked ? theme.ErrorConnection : IsActive ? theme.ActiveConnection : theme.Connection;

				return result;
			}
		}

		public double StrokeThickness
		{
			get
			{
				var result = IsEvidence ? 3.2 : IsActive ? 2.8 : 1.9;

				return result;
			}
		}

		public DoubleCollection? StrokeDashArray
		{
			get
			{
				var result = IsEvidence ? new DoubleCollection([2, 3]) : IsBlocked ? new DoubleCollection([4, 3]) : null;

				return result;
			}
		}

		public Brush TextBackground
		{
			get
			{
				var result = IsEvidence ? theme.ErrorConnection : IsBlocked ? theme.ErrorConnection : IsActive ? theme.ActiveConnection : theme.Connection;

				return result;
			}
		}

		public static NodifyGraphConnectionViewModel Create(
			ArchitectureGraphEdgeViewModel edge,
			NodifyGraphConnectorViewModel output,
			NodifyGraphConnectorViewModel input,
			Action<ArchitectureConfigurationEditResult, bool>? editResultHandler,
			Func<string, bool>? confirmationHandler,
			ArchitectureGraphCanvasTheme theme)
		{
			var result = new NodifyGraphConnectionViewModel(edge, output, input, editResultHandler, confirmationHandler, theme);

			return result;
		}

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

			var result = ArchitectureConfigurationEditService.RemoveDependency(EditHandle);
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

			var result = ArchitectureConfigurationEditService.SetDependencySites(EditHandle, mode, sites);
			if (result.Succeeded)
			{
				allowedSites = mode == ArchitectureSiteFilterEditMode.AllowedSites ? sites : ImmutableArray<string>.Empty;
				blockedSites = mode == ArchitectureSiteFilterEditMode.BlockedSites ? sites : ImmutableArray<string>.Empty;
				RefreshSitePresentation();
			}

			ReportEditResult(result);
		}

		private void RefreshSitePresentation()
		{
			foreach (var option in AllowedSiteOptions)
			{
				option.IsChecked = allowedSites.Contains(option.Site, StringComparer.Ordinal);
			}

			foreach (var option in BlockedSiteOptions)
			{
				option.IsChecked = blockedSites.Contains(option.Site, StringComparer.Ordinal);
			}

			var siteText = FormatSiteText(allowedSites, blockedSites);
			LabelText = FormatLabelText(siteText, AppliesToDescendants);
			ToolTip = FormatEdgeToolTip(Kind, From, To, siteText, AppliesToDescendants);
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(UsesAllSites)));
		}

		private void ReportEditResult(ArchitectureConfigurationEditResult result, bool clearSelection = false)
		{
			editResultHandler?.Invoke(result, clearSelection);
		}

		private static ImmutableArray<string> ToggleSite(ImmutableArray<string> currentSites, string site)
		{
			var builder = ImmutableArray.CreateBuilder<string>();
			builder.AddRange(currentSites.Where(current => !string.Equals(current, site, StringComparison.Ordinal)));
			if (builder.Count == currentSites.Length)
			{
				builder.Add(site);
			}

			var result = ArchitectureDependencySiteNames.All.Where(builder.Contains).ToImmutableArray();

			return result;
		}

		private static string FormatLabelText(string siteText, bool appliesToDescendants)
		{
			var result = siteText + (appliesToDescendants ? ", cascades" : string.Empty);

			return result;
		}

		private static string FormatSiteText(ImmutableArray<string> allowedSites, ImmutableArray<string> blockedSites)
		{
			if (allowedSites.Length > 0)
			{
				return "allowed sites: " + string.Join(", ", allowedSites);
			}

			if (blockedSites.Length > 0)
			{
				return "blocked sites: " + string.Join(", ", blockedSites);
			}

			return "all sites";
		}

		private static string FormatEdgeToolTip(string kind, string from, string to, string siteText, bool appliesToDescendants)
		{
			var cascade = appliesToDescendants ? Environment.NewLine + "Applies to descendants." : string.Empty;
			var result = kind + ": " + from + " -> " + to + Environment.NewLine + siteText + cascade;

			return result;
		}

		private static string FormatEvidenceToolTip(string from, string to, string siteText, string details)
		{
			var detailText = string.IsNullOrWhiteSpace(details) ? string.Empty : Environment.NewLine + details;
			var result = "Observed code dependency: " + from + " -> " + to + Environment.NewLine + siteText + detailText;

			return result;
		}
	}

	private sealed class NodifySiteFilterOptionViewModel(string site, bool isChecked, ICommand command)
        : INotifyPropertyChanged
    {
		private bool isChecked = isChecked;

        public event PropertyChangedEventHandler? PropertyChanged;

		public string Site { get; } = site;

        public bool IsChecked
		{
			get { return isChecked; }
			set
			{
				if (isChecked == value)
				{
					return;
				}

				isChecked = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsChecked)));
			}
		}

		public ICommand Command { get; } = command;
    }

	private sealed class DelegateCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
        : ICommand
    {
        public event EventHandler? CanExecuteChanged;

		public bool CanExecute(object? parameter)
		{
			var result = canExecute?.Invoke(parameter) ?? true;

			return result;
		}

		public void Execute(object? parameter)
		{
			execute(parameter);
		}

		public void RaiseCanExecuteChanged()
		{
			CanExecuteChanged?.Invoke(this, EventArgs.Empty);
		}
	}
}
