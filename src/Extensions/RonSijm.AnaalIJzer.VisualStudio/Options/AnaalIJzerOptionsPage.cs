using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;
using RonSijm.AnaalIJzer.Graph;
using RonSijm.AnaalIJzer.Indicators;
using RonSijm.AnaalIJzer.Snapshots;
using RonSijm.AnaalIJzer.VisualStudio.Diagnostics;

namespace RonSijm.AnaalIJzer.VisualStudio.Options;

[Guid("5d45288a-bff3-44d8-96a9-d7271b425a30")]
public sealed class AnaalIJzerOptionsPage : DialogPage
{
	private bool isLoadingSettings;

	[Category("Layer indicators")]
	[DisplayName("Show layer badges")]
	[Description("Show the resolved AnaalIJzer layer path after type declaration identifiers.")]
	public bool EnableInlineLayerBadges { get; set; } = true;

	[Category("Layer indicators")]
	[DisplayName("Show layer metadata above declarations")]
	[Description("Show a clickable CodeLens-style AnaalIJzer layer summary above type declarations.")]
	public bool EnableLayerCodeLens { get; set; } = true;

	[Category("Layer indicators")]
	[DisplayName("Show layer badges when not in layer")]
	[Description("Show a neutral badge after type declarations that are not assigned to any configured AnaalIJzer layer.")]
	public bool ShowLayerBadgesWhenNotInLayer { get; set; }

	[Category("Layer indicators")]
	[DisplayName("Show global layer rules in badge hover")]
	[Description("Include wildcard dependency rules such as '* (any layer)' in layer badge hover details.")]
	public bool ShowGlobalLayerRulesInBadges { get; set; }

	[Category("Layer indicators")]
	[DisplayName("Show mini call graph in badge hover")]
	[Description("Show a compact dependency chain in layer badge hover details when the configured graph is a straight one-to-one chain.")]
	public bool ShowLinearCallChainInBadges { get; set; } = true;

	[Category("Layer indicators")]
	[DisplayName("Gutter glyphs")]
	[Description("Show a glyph beside type declarations that belong to an AnaalIJzer layer.")]
	public bool EnableLayerGlyphs { get; set; } = true;

	[Category("Layer indicators")]
	[DisplayName("Highlight code in layer")]
	[Description("Show a region-like block highlight around type declarations that belong to an AnaalIJzer layer.")]
	public bool EnableLayerBlockHighlight { get; set; } = true;

	[Category("Layer indicators")]
	[DisplayName("Tint layer declaration text")]
	[Description("Apply the older line background tint to type declarations that belong to an AnaalIJzer layer.")]
	public bool EnableLayerBackgroundTint { get; set; }

	[Category("Layer information")]
	[DisplayName("Show All Layer Information")]
	[Description("Enable or disable inline dependency-layer labels for every supported dependency site.")]
	[RefreshProperties(RefreshProperties.All)]
	public bool ShowAllLayerInformation
	{
		get
		{
			var result = ArchitectureOptionsBulkToggle.AreAllLayerInformationOptionsEnabled(ToSiteLayerInformationOptions());

			return result;
		}
		set
		{
			if (isLoadingSettings)
			{
				return;
			}

			ApplySiteLayerInformationOptions(ArchitectureOptionsBulkToggle.CreateLayerInformationOptions(value));
		}
	}

	[Category("Layer information")]
	[DisplayName("Show Constructor Layer Information")]
	[Description("Show inline dependency-layer labels for constructor and primary-constructor dependency sites.")]
	public bool ShowConstructorLayerInformation { get; set; }

	[Category("Layer information")]
	[DisplayName("Show Method Layer Information")]
	[Description("Show inline dependency-layer labels for method parameter dependency sites.")]
	public bool ShowMethodLayerInformation { get; set; }

	[Category("Layer information")]
	[DisplayName("Show MethodReturn Layer Information")]
	[Description("Show inline dependency-layer labels for method return-type dependency sites.")]
	public bool ShowMethodReturnLayerInformation { get; set; }

	[Category("Layer information")]
	[DisplayName("Show Field Layer Information")]
	[Description("Show inline dependency-layer labels for field dependency sites.")]
	public bool ShowFieldLayerInformation { get; set; }

	[Category("Layer information")]
	[DisplayName("Show Property Layer Information")]
	[Description("Show inline dependency-layer labels for property dependency sites.")]
	public bool ShowPropertyLayerInformation { get; set; }

	[Category("Layer information")]
	[DisplayName("Show Local Layer Information")]
	[Description("Show inline dependency-layer labels for local-variable dependency sites.")]
	public bool ShowLocalLayerInformation { get; set; }

	[Category("Layer information")]
	[DisplayName("Show New Layer Information")]
	[Description("Show inline dependency-layer labels for object creation dependency sites.")]
	public bool ShowNewLayerInformation { get; set; }

	[Category("Layer information")]
	[DisplayName("Show GenericInvocation Layer Information")]
	[Description("Show inline dependency-layer labels for generic method invocation type-argument dependency sites.")]
	public bool ShowGenericInvocationLayerInformation { get; set; }

	[Category("Layer information")]
	[DisplayName("Show GenericArgument Layer Information")]
	[Description("Show inline dependency-layer labels for generic type argument dependency sites.")]
	public bool ShowGenericArgumentLayerInformation { get; set; }

	[Category("Layer information")]
	[DisplayName("Show Inheritance Layer Information")]
	[Description("Show inline dependency-layer labels for base-class dependency sites.")]
	public bool ShowInheritanceLayerInformation { get; set; }

	[Category("Layer information")]
	[DisplayName("Show InterfaceImplementation Layer Information")]
	[Description("Show inline dependency-layer labels for implemented-interface dependency sites.")]
	public bool ShowInterfaceImplementationLayerInformation { get; set; }

	[Category("Layer information")]
	[DisplayName("Show Attribute Layer Information")]
	[Description("Show inline dependency-layer labels for attribute dependency sites.")]
	public bool ShowAttributeLayerInformation { get; set; }

	[Category("Layer information")]
	[DisplayName("Show StaticMember Layer Information")]
	[Description("Show inline dependency-layer labels for static member access dependency sites.")]
	public bool ShowStaticMemberLayerInformation { get; set; }

	[Category("Sites diagnostics")]
	[DisplayName("Show All Site Diagnostics")]
	[Description("Enable or disable inline site-diagnostic labels for every supported dependency site.")]
	[RefreshProperties(RefreshProperties.All)]
	public bool ShowAllSiteDiagnostics
	{
		get
		{
			var result = ArchitectureOptionsBulkToggle.AreAllSiteDiagnosticOptionsEnabled(ToSiteDiagnosticOptions());

			return result;
		}
		set
		{
			if (isLoadingSettings)
			{
				return;
			}

			ApplySiteDiagnosticOptions(ArchitectureOptionsBulkToggle.CreateSiteDiagnosticOptions(value));
		}
	}

	[Category("Sites diagnostics")]
	[DisplayName("Show Constructor Site Diagnostics")]
	[Description("Show inline labels for constructor and primary-constructor dependency sites.")]
	public bool ShowConstructorSiteDiagnostics { get; set; }

	[Category("Sites diagnostics")]
	[DisplayName("Show Method Site Diagnostics")]
	[Description("Show inline labels for method parameter dependency sites.")]
	public bool ShowMethodSiteDiagnostics { get; set; }

	[Category("Sites diagnostics")]
	[DisplayName("Show MethodReturn Site Diagnostics")]
	[Description("Show inline labels for method return-type dependency sites.")]
	public bool ShowMethodReturnSiteDiagnostics { get; set; }

	[Category("Sites diagnostics")]
	[DisplayName("Show Field Site Diagnostics")]
	[Description("Show inline labels for field dependency sites.")]
	public bool ShowFieldSiteDiagnostics { get; set; }

	[Category("Sites diagnostics")]
	[DisplayName("Show Property Site Diagnostics")]
	[Description("Show inline labels for property dependency sites.")]
	public bool ShowPropertySiteDiagnostics { get; set; }

	[Category("Sites diagnostics")]
	[DisplayName("Show Local Site Diagnostics")]
	[Description("Show inline labels for local-variable dependency sites.")]
	public bool ShowLocalSiteDiagnostics { get; set; }

	[Category("Sites diagnostics")]
	[DisplayName("Show New Site Diagnostics")]
	[Description("Show inline labels for object creation dependency sites.")]
	public bool ShowNewSiteDiagnostics { get; set; }

	[Category("Sites diagnostics")]
	[DisplayName("Show GenericInvocation Site Diagnostics")]
	[Description("Show inline labels for generic method invocation type-argument dependency sites.")]
	public bool ShowGenericInvocationSiteDiagnostics { get; set; }

	[Category("Sites diagnostics")]
	[DisplayName("Show GenericArgument Site Diagnostics")]
	[Description("Show inline labels for generic type argument dependency sites.")]
	public bool ShowGenericArgumentSiteDiagnostics { get; set; }

	[Category("Sites diagnostics")]
	[DisplayName("Show Inheritance Site Diagnostics")]
	[Description("Show inline labels for base-class dependency sites.")]
	public bool ShowInheritanceSiteDiagnostics { get; set; }

	[Category("Sites diagnostics")]
	[DisplayName("Show InterfaceImplementation Site Diagnostics")]
	[Description("Show inline labels for implemented-interface dependency sites.")]
	public bool ShowInterfaceImplementationSiteDiagnostics { get; set; }

	[Category("Sites diagnostics")]
	[DisplayName("Show Attribute Site Diagnostics")]
	[Description("Show inline labels for attribute dependency sites.")]
	public bool ShowAttributeSiteDiagnostics { get; set; }

	[Category("Sites diagnostics")]
	[DisplayName("Show StaticMember Site Diagnostics")]
	[Description("Show inline labels for static member access dependency sites.")]
	public bool ShowStaticMemberSiteDiagnostics { get; set; }

	[Category("Dependency graphs")]
	[DisplayName("Graph focus mode")]
	[Description("Controls whether the dependency graph tool window shows all graphs, highlights graphs that affect the active editor, or filters to only those graphs.")]
	public ArchitectureGraphFocusMode DependencyGraphFocusMode { get; set; } = ArchitectureGraphFocusMode.HighlightCurrent;

	[Category("Dependency graphs")]
	[DisplayName("Open .anl files in diagram editor")]
	[Description("When an AnaalIJzer .anl settings file is opened or selected, show it in the dependency graph editor automatically.")]
	public bool OpenAnlFilesInGraphEditor { get; set; } = true;

	[Category("Dependency graphs")]
	[DisplayName("Include code evidence")]
	[Description("Include project-code evidence in dependency graph snapshots so layer boxes can show matching types and observed violations.")]
	public bool IncludeCodeEvidenceInDependencyGraphs { get; set; }

	public override void LoadSettingsFromStorage()
	{
		isLoadingSettings = true;
		try
		{
			base.LoadSettingsFromStorage();
		}
		finally
		{
			isLoadingSettings = false;
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

	internal ArchitectureEditorOptions ToEditorOptions()
	{
		var siteDiagnostics = ToSiteDiagnosticOptions();
		var siteLayerInformation = ToSiteLayerInformationOptions();
		var result = new ArchitectureEditorOptions(
			EnableInlineLayerBadges,
			EnableLayerGlyphs,
			EnableLayerBlockHighlight,
			false,
			DependencyGraphFocusMode,
			siteDiagnostics,
			ShowLayerBadgesWhenNotInLayer,
			EnableLayerBackgroundTint,
			ShowGlobalLayerRulesInBadges,
			ShowLinearCallChainInBadges,
			siteLayerInformation,
			EnableLayerCodeLens,
			OpenAnlFilesInGraphEditor,
			IncludeCodeEvidenceInDependencyGraphs);

		return result;
	}

	internal ArchitectureSiteDiagnosticOptions ToSiteDiagnosticOptions()
	{
		var result = new ArchitectureSiteDiagnosticOptions(
			ShowConstructorSiteDiagnostics,
			ShowMethodSiteDiagnostics,
			ShowMethodReturnSiteDiagnostics,
			ShowFieldSiteDiagnostics,
			ShowPropertySiteDiagnostics,
			ShowLocalSiteDiagnostics,
			ShowNewSiteDiagnostics,
			ShowGenericInvocationSiteDiagnostics,
			ShowGenericArgumentSiteDiagnostics,
			ShowInheritanceSiteDiagnostics,
			ShowInterfaceImplementationSiteDiagnostics,
			ShowAttributeSiteDiagnostics,
			ShowStaticMemberSiteDiagnostics);

		return result;
	}

	internal ArchitectureSiteLayerInformationOptions ToSiteLayerInformationOptions()
	{
		var result = new ArchitectureSiteLayerInformationOptions(
			ShowConstructorLayerInformation,
			ShowMethodLayerInformation,
			ShowMethodReturnLayerInformation,
			ShowFieldLayerInformation,
			ShowPropertyLayerInformation,
			ShowLocalLayerInformation,
			ShowNewLayerInformation,
			ShowGenericInvocationLayerInformation,
			ShowGenericArgumentLayerInformation,
			ShowInheritanceLayerInformation,
			ShowInterfaceImplementationLayerInformation,
			ShowAttributeLayerInformation,
			ShowStaticMemberLayerInformation);

		return result;
	}

	internal void ApplyEditorOptions(ArchitectureEditorOptions options)
	{
		EnableInlineLayerBadges = options.EnableInlineLayerBadges;
		EnableLayerCodeLens = options.EnableLayerCodeLens;
		EnableLayerGlyphs = options.EnableLayerGlyphs;
		EnableLayerBlockHighlight = options.EnableLayerBlockHighlight;
		EnableLayerBackgroundTint = options.EnableLayerTextBackgroundTint;
		ShowLayerBadgesWhenNotInLayer = options.ShowLayerBadgesWhenNotInLayer;
		ShowGlobalLayerRulesInBadges = options.ShowGlobalLayerRulesInBadges;
		ShowLinearCallChainInBadges = options.ShowLinearCallChainInBadges;
		DependencyGraphFocusMode = options.DependencyGraphFocusMode;
		OpenAnlFilesInGraphEditor = options.OpenAnlFilesInGraphEditor;
		IncludeCodeEvidenceInDependencyGraphs = options.IncludeCodeEvidenceInDependencyGraphs;
		ApplySiteDiagnosticOptions(options.SiteDiagnostics);
		ApplySiteLayerInformationOptions(options.SiteLayerInformation);
	}

	private void ApplySiteDiagnosticOptions(ArchitectureSiteDiagnosticOptions options)
	{
		ShowConstructorSiteDiagnostics = options.ShowConstructorSiteDiagnostics;
		ShowMethodSiteDiagnostics = options.ShowMethodSiteDiagnostics;
		ShowMethodReturnSiteDiagnostics = options.ShowMethodReturnSiteDiagnostics;
		ShowFieldSiteDiagnostics = options.ShowFieldSiteDiagnostics;
		ShowPropertySiteDiagnostics = options.ShowPropertySiteDiagnostics;
		ShowLocalSiteDiagnostics = options.ShowLocalSiteDiagnostics;
		ShowNewSiteDiagnostics = options.ShowNewSiteDiagnostics;
		ShowGenericInvocationSiteDiagnostics = options.ShowGenericInvocationSiteDiagnostics;
		ShowGenericArgumentSiteDiagnostics = options.ShowGenericArgumentSiteDiagnostics;
		ShowInheritanceSiteDiagnostics = options.ShowInheritanceSiteDiagnostics;
		ShowInterfaceImplementationSiteDiagnostics = options.ShowInterfaceImplementationSiteDiagnostics;
		ShowAttributeSiteDiagnostics = options.ShowAttributeSiteDiagnostics;
		ShowStaticMemberSiteDiagnostics = options.ShowStaticMemberSiteDiagnostics;
	}

	private void ApplySiteLayerInformationOptions(ArchitectureSiteLayerInformationOptions options)
	{
		ShowConstructorLayerInformation = options.ShowConstructorLayerInformation;
		ShowMethodLayerInformation = options.ShowMethodLayerInformation;
		ShowMethodReturnLayerInformation = options.ShowMethodReturnLayerInformation;
		ShowFieldLayerInformation = options.ShowFieldLayerInformation;
		ShowPropertyLayerInformation = options.ShowPropertyLayerInformation;
		ShowLocalLayerInformation = options.ShowLocalLayerInformation;
		ShowNewLayerInformation = options.ShowNewLayerInformation;
		ShowGenericInvocationLayerInformation = options.ShowGenericInvocationLayerInformation;
		ShowGenericArgumentLayerInformation = options.ShowGenericArgumentLayerInformation;
		ShowInheritanceLayerInformation = options.ShowInheritanceLayerInformation;
		ShowInterfaceImplementationLayerInformation = options.ShowInterfaceImplementationLayerInformation;
		ShowAttributeLayerInformation = options.ShowAttributeLayerInformation;
		ShowStaticMemberLayerInformation = options.ShowStaticMemberLayerInformation;
	}
}
