using System.Collections.Immutable;
using System.ComponentModel;
using System.Windows.Input;
using System.Windows.Media;
using RonSijm.AnaalIJzer.ConfigurationEditing.Editing;
using RonSijm.AnaalIJzer.ConfigurationEditing.Model;
using RonSijm.AnaalIJzer.ConfigurationEditing.Sites;
using RonSijm.AnaalIJzer.GraphApplication;
using RonSijm.AnaalIJzer.GraphApplication.Selection;
using RonSijm.AnaalIJzer.Graphing.ViewModels;
using RonSijm.AnaalIJzer.Graphing.Wpf.Styling;

namespace RonSijm.AnaalIJzer.GraphEditor.Wpf.Controls;

internal sealed partial class ArchitectureGraphCanvas
{
	private sealed partial class NodifyGraphConnectionViewModel : INotifyPropertyChanged
	{
		private readonly IArchitectureGraphEditService _editService;
		private readonly Action<ArchitectureConfigurationEditResult, bool>? _editResultHandler;
		private readonly Action<ArchitectureGraphSelection>? _selectionHandler;
		private readonly Func<string, bool>? _confirmationHandler;
		private readonly ArchitectureGraphCanvasTheme _theme;
		private ImmutableArray<string> _allowedSites;
		private ImmutableArray<string> _blockedSites;
		private string _labelText;
		private string _toolTip;

		private NodifyGraphConnectionViewModel(
			ArchitectureGraphEdgeViewModel edge,
			NodifyGraphConnectorViewModel output,
			NodifyGraphConnectorViewModel input,
			IArchitectureGraphEditService editService,
			Action<ArchitectureConfigurationEditResult, bool>? editResultHandler,
			Action<ArchitectureGraphSelection>? selectionHandler,
			Func<string, bool>? confirmationHandler,
			ArchitectureGraphCanvasTheme theme)
		{
			this._editService = editService;
			this._editResultHandler = editResultHandler;
			this._selectionHandler = selectionHandler;
			this._confirmationHandler = confirmationHandler;
			this._theme = theme;
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
			_allowedSites = edge.AllowedSites;
			_blockedSites = edge.BlockedSites;
			_labelText = IsEvidence ? edge.SiteText : FormatLabelText(edge.SiteText, edge.AppliesToDescendants);
			_toolTip = IsEvidence
				? FormatEvidenceToolTip(edge.From, edge.To, edge.SiteText, EvidenceDetails)
				: FormatEdgeToolTip(edge.Kind, edge.From, edge.To, edge.SiteText, edge.AppliesToDescendants);
			RemoveCommand = new DelegateCommand(_ => Remove(), _ => !IsEvidence && EditHandle.CanEdit);
			AllowAllSitesCommand = new DelegateCommand(_ => SetSites(ArchitectureSiteFilterEditMode.All, ImmutableArray<string>.Empty), _ => !IsEvidence && EditHandle.CanEdit);
			ShowConfigurationFixesCommand = new DelegateCommand(_ => ShowConfigurationFixes(), _ => _selectionHandler is not null);
			AllowedSiteOptions = ArchitectureDependencySiteNames.All.Select(site => new NodifySiteFilterOptionViewModel(site, _allowedSites.Contains(site, StringComparer.Ordinal), new DelegateCommand(_ => ToggleAllowedSite(site)))).ToImmutableArray();
			BlockedSiteOptions = ArchitectureDependencySiteNames.All.Select(site => new NodifySiteFilterOptionViewModel(site, _blockedSites.Contains(site, StringComparer.Ordinal), new DelegateCommand(_ => ToggleBlockedSite(site)))).ToImmutableArray();
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

		public bool CanEditRule => !IsEvidence && EditHandle.CanEdit;

		public string LabelText
		{
			get { return _labelText; }
			private set
			{
				if (_labelText == value)
				{
					return;
				}

				_labelText = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LabelText)));
			}
		}

		public string ToolTip
		{
			get { return _toolTip; }
			private set
			{
				if (_toolTip == value)
				{
					return;
				}

				_toolTip = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ToolTip)));
			}
		}

		public ICommand RemoveCommand { get; }

		public ICommand AllowAllSitesCommand { get; }

		public ICommand ShowConfigurationFixesCommand { get; }

		public ImmutableArray<NodifySiteFilterOptionViewModel> AllowedSiteOptions { get; }

		public ImmutableArray<NodifySiteFilterOptionViewModel> BlockedSiteOptions { get; }

		public bool UsesAllSites => _allowedSites.Length == 0 && _blockedSites.Length == 0;

		public Brush Stroke => IsEvidence ? _theme.ErrorConnection : IsBlocked ? _theme.ErrorConnection : IsActive ? _theme.ActiveConnection : _theme.Connection;

		public double StrokeThickness => IsEvidence ? 3.2 : IsActive ? 2.8 : 1.9;

		public DoubleCollection? StrokeDashArray => IsEvidence ? new DoubleCollection([2, 3]) : IsBlocked ? new DoubleCollection([4, 3]) : null;

		public Brush TextBackground => IsEvidence ? _theme.ErrorConnection : IsBlocked ? _theme.ErrorConnection : IsActive ? _theme.ActiveConnection : _theme.Connection;

		public static NodifyGraphConnectionViewModel Create(
			ArchitectureGraphEdgeViewModel edge,
			NodifyGraphConnectorViewModel output,
			NodifyGraphConnectorViewModel input,
			IArchitectureGraphEditService editService,
			Action<ArchitectureConfigurationEditResult, bool>? editResultHandler,
			Action<ArchitectureGraphSelection>? selectionHandler,
			Func<string, bool>? confirmationHandler,
			ArchitectureGraphCanvasTheme theme)
		{
			var result = new NodifyGraphConnectionViewModel(edge, output, input, editService, editResultHandler, selectionHandler, confirmationHandler, theme);

			return result;
		}
	}
}
