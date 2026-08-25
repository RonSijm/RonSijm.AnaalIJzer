using System.Collections.Immutable;
using System.ComponentModel;
using System.Windows.Input;
using System.Windows.Media;
using RonSijm.AnaalIJzer.ConfigurationEditing.Editing;
using RonSijm.AnaalIJzer.ConfigurationEditing.Model;
using RonSijm.AnaalIJzer.ConfigurationEditing.Sites;
using RonSijm.AnaalIJzer.GraphApplication;
using RonSijm.AnaalIJzer.Graphing.ViewModels;
using RonSijm.AnaalIJzer.Graphing.Wpf.Styling;

namespace RonSijm.AnaalIJzer.GraphEditor.Wpf.Controls;

internal sealed partial class ArchitectureGraphCanvas
{
	private sealed partial class NodifyGraphConnectionViewModel : INotifyPropertyChanged
	{
		private readonly IArchitectureGraphEditService editService;
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
			IArchitectureGraphEditService editService,
			Action<ArchitectureConfigurationEditResult, bool>? editResultHandler,
			Func<string, bool>? confirmationHandler,
			ArchitectureGraphCanvasTheme theme)
		{
			this.editService = editService;
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
			IArchitectureGraphEditService editService,
			Action<ArchitectureConfigurationEditResult, bool>? editResultHandler,
			Func<string, bool>? confirmationHandler,
			ArchitectureGraphCanvasTheme theme)
		{
			var result = new NodifyGraphConnectionViewModel(edge, output, input, editService, editResultHandler, confirmationHandler, theme);

			return result;
		}
	}
}
