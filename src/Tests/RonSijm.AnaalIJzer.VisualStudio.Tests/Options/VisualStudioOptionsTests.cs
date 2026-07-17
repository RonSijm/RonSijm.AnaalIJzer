using System.Collections.Immutable;
using System.IO;
using System.IO.Compression;
using AwesomeAssertions;
using Microsoft.CodeAnalysis.Text;
using RonSijm.AnaalIJzer.Graph;
using RonSijm.AnaalIJzer.Indicators;
using RonSijm.AnaalIJzer.Snapshots;
using RonSijm.AnaalIJzer.VisualStudio.LayerIndicators;
using RonSijm.AnaalIJzer.VisualStudio.Options;
using RonSijm.AnaalIJzer.VisualStudio.Shell.Commands;
using Xunit;

namespace RonSijm.AnaalIJzer.VisualStudio.Tests.Options;

public sealed class VisualStudioOptionsTests
{
	[Fact]
	public void CommandTable_IsEmbeddedAsRegisteredMenuResource()
	{
		var resources = typeof(ArchitectureVisualStudioOptions).Assembly.GetManifestResourceNames();
		var pkgdefPath = FindGeneratedPackageDefinition();
		var pkgdef = File.ReadAllText(pkgdefPath);

		resources.Should().Contain("Menus.ctmenu");
		pkgdef.Should().Contain(@"[$RootKey$\Menus]");
		pkgdef.Should().Contain(@"""{8db21a41-4f59-40cc-a6ef-8f8c8a24f4c0}""="", Menus.ctmenu, 1""");
	}

	[Fact]
	public void DependencyGraphVsix_IncludesNodifyRuntime()
	{
		using var archive = ZipFile.OpenRead(FindGeneratedVsix());

		archive.Entries.Select(entry => entry.FullName).Should().Contain("Nodify.dll");
	}

	[Fact]
	public void OptionsPage_RegistersMigratedUnifiedSettings()
	{
		var pkgdef = File.ReadAllText(FindGeneratedPackageDefinition());
		var manifest = File.ReadAllText(FindRepositoryFile("src", "Extensions", "RonSijm.AnaalIJzer.VisualStudio", "UnifiedSettings", "AnaalIJzer.registration.json"));

		pkgdef.Should().Contain(@"""IsInUnifiedSettings""=dword:00000001");
		pkgdef.Should().Contain(@"""ShouldShowUnifiedSettingsPlaceholder""=dword:00000000");
		pkgdef.Should().Contain(@"""UnifiedSettingsCategoryMoniker""=""AnaalIJzer""");
		pkgdef.Should().Contain(@"[$RootKey$\SettingsManifests\{8DB21A41-4F59-40CC-A6EF-8F8C8A24F4C0}]");
		pkgdef.Should().Contain(@"""ManifestPath""=""$PackageFolder$\UnifiedSettings\AnaalIJzer.registration.json""");
		pkgdef.Should().Contain(@"{af0e6eb4-77ec-4d32-a9b1-233720e65c03}");

		manifest.Should().Contain(@"""anaalIJzer.editor""");
		manifest.Should().Contain(@"""type"": ""external""");
		manifest.Should().Contain(@"""packageId"": ""8db21a41-4f59-40cc-a6ef-8f8c8a24f4c0""");
		manifest.Should().Contain(@"""serviceId"": ""af0e6eb4-77ec-4d32-a9b1-233720e65c03""");
		manifest.Should().Contain(@"""layerIndicators.enableInlineLayerBadges""");
		manifest.Should().Contain(@"""layerInformation.showAll""");
		manifest.Should().Contain(@"""siteDiagnostics.showAll""");
		manifest.Should().Contain(@"""dependencyGraphs.focusMode""");
		manifest.Should().Contain(@"""dependencyGraphs.openAnlFilesInGraphEditor""");
		manifest.Should().NotContain("legacyOptionPageId");
		manifest.Should().NotContain("legacyOptionCategoryMoniker");
	}

	[Fact]
	public void CommandTable_UsesVisibleViewAndToolsMenuGroups()
	{
		var commands = File.ReadAllText(FindRepositoryFile("src", "Extensions", "RonSijm.AnaalIJzer.VisualStudio", "Shell", "Commands.vsct"));

		commands.Should().Contain("IDG_VS_WNDO_OTRWNDWS1");
		commands.Should().Contain("IDG_VS_TOOLS_EXTENSIBILITY");
		commands.Should().NotContain("IDM_VS_MENU_ADDINS");
	}

	[Fact]
	public void VisualStudioOptions_DefaultsMatchEditorDefaults()
	{
		var options = ArchitectureVisualStudioOptions.Current;

		options.EnableInlineLayerBadges.Should().BeTrue();
		options.EnableLayerCodeLens.Should().BeTrue();
		options.EnableLayerGlyphs.Should().BeTrue();
		options.EnableLayerBlockHighlight.Should().BeTrue();
		options.EnableLayerBackgroundTint.Should().BeTrue();
		options.EnableHighlightCodeInLayer.Should().BeTrue();
		options.EnableLayerTextBackgroundTint.Should().BeFalse();
		options.ShowLayerBadgesWhenNotInLayer.Should().BeFalse();
		options.ShowGlobalLayerRulesInBadges.Should().BeFalse();
		options.ShowLinearCallChainInBadges.Should().BeTrue();
		options.EnableSitesDiagnostics.Should().BeFalse();
		options.SiteDiagnostics.AnyEnabled.Should().BeFalse();
		options.EnableSiteLayerInformation.Should().BeFalse();
		options.SiteLayerInformation.AnyEnabled.Should().BeFalse();
		options.DependencyGraphFocusMode.Should().Be(ArchitectureGraphFocusMode.HighlightCurrent);
		options.OpenAnlFilesInGraphEditor.Should().BeTrue();
	}

	[Fact]
	public void VisualStudioOptions_PublishUpdatesCurrentAndRaisesChanged()
	{
		var raised = false;
		void OnChanged(object? sender, EventArgs args)
		{
			raised = true;
		}

		var options = new ArchitectureEditorOptions(
			enableInlineLayerBadges: false,
			enableLayerGlyphs: false,
			enableLayerBackgroundTint: true,
			enableSitesDiagnostics: false,
			dependencyGraphFocusMode: ArchitectureGraphFocusMode.FilterToCurrent,
			siteDiagnostics: ArchitectureSiteDiagnosticOptions.All,
			showLayerBadgesWhenNotInLayer: true,
			enableLayerTextBackgroundTint: true,
			showGlobalLayerRulesInBadges: true,
			showLinearCallChainInBadges: false,
			siteLayerInformation: ArchitectureSiteLayerInformationOptions.All,
			enableLayerCodeLens: false,
			openAnlFilesInGraphEditor: false);

		ArchitectureVisualStudioOptions.Changed += OnChanged;
		try
		{
			ArchitectureVisualStudioOptions.Publish(options);

			raised.Should().BeTrue();
			ArchitectureVisualStudioOptions.Current.Should().BeSameAs(options);
			ArchitectureVisualStudioOptions.Current.EnableLayerCodeLens.Should().BeFalse();
			ArchitectureVisualStudioOptions.Current.OpenAnlFilesInGraphEditor.Should().BeFalse();
		}
		finally
		{
			ArchitectureVisualStudioOptions.Changed -= OnChanged;
			ArchitectureVisualStudioOptions.Publish(ArchitectureEditorOptions.Default);
		}
	}

	[Theory]
	[InlineData(false, true)]
	[InlineData(true, false)]
	public void SitesDiagnosticsCommandState_TogglesOnlySitesDiagnostics(bool currentValue, bool expectedAnyEnabled)
	{
		var options = new ArchitectureEditorOptions(
			enableInlineLayerBadges: false,
			enableLayerGlyphs: true,
			enableLayerBackgroundTint: false,
			enableSitesDiagnostics: currentValue,
			dependencyGraphFocusMode: ArchitectureGraphFocusMode.FilterToCurrent,
			showLayerBadgesWhenNotInLayer: true,
			enableLayerTextBackgroundTint: true,
			showGlobalLayerRulesInBadges: true,
			showLinearCallChainInBadges: false,
			siteLayerInformation: ArchitectureSiteLayerInformationOptions.All,
			enableLayerCodeLens: false,
			openAnlFilesInGraphEditor: false);

		var result = SitesDiagnosticsCommandState.Toggle(options);

		result.EnableInlineLayerBadges.Should().BeFalse();
		result.EnableLayerCodeLens.Should().BeFalse();
		result.EnableLayerGlyphs.Should().BeTrue();
		result.EnableLayerBackgroundTint.Should().BeFalse();
		result.EnableLayerTextBackgroundTint.Should().BeTrue();
		result.ShowLayerBadgesWhenNotInLayer.Should().BeTrue();
		result.ShowGlobalLayerRulesInBadges.Should().BeTrue();
		result.ShowLinearCallChainInBadges.Should().BeFalse();
		result.EnableSiteLayerInformation.Should().BeTrue();
		result.OpenAnlFilesInGraphEditor.Should().BeFalse();
		result.SiteLayerInformation.AnyEnabled.Should().BeTrue();
		result.EnableSitesDiagnostics.Should().Be(expectedAnyEnabled);
		result.SiteDiagnostics.AnyEnabled.Should().Be(expectedAnyEnabled);
		foreach (var site in ArchitectureDependencySites.All)
		{
			result.IsSiteDiagnosticEnabled(site).Should().Be(expectedAnyEnabled);
		}

		result.DependencyGraphFocusMode.Should().Be(ArchitectureGraphFocusMode.FilterToCurrent);
	}

	[Fact]
	public void SiteDiagnosticOptions_EnableIndividualSites()
	{
		var options = new ArchitectureSiteDiagnosticOptions(showLocalSiteDiagnostics: true, showStaticMemberSiteDiagnostics: true);

		options.AnyEnabled.Should().BeTrue();
		options.IsEnabled(ArchitectureDependencySites.Local).Should().BeTrue();
		options.IsEnabled(ArchitectureDependencySites.StaticMember).Should().BeTrue();
		options.IsEnabled(ArchitectureDependencySites.Constructor).Should().BeFalse();
	}

	[Fact]
	public void SiteLayerInformationOptions_EnableIndividualSites()
	{
		var options = new ArchitectureSiteLayerInformationOptions(showFieldLayerInformation: true, showMethodReturnLayerInformation: true);

		options.AnyEnabled.Should().BeTrue();
		options.IsEnabled(ArchitectureDependencySites.Field).Should().BeTrue();
		options.IsEnabled(ArchitectureDependencySites.MethodReturn).Should().BeTrue();
		options.IsEnabled(ArchitectureDependencySites.Constructor).Should().BeFalse();
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void BulkToggle_CreateLayerInformationOptions_SetsEveryLayerInformationOption(bool enabled)
	{
		var options = ArchitectureOptionsBulkToggle.CreateLayerInformationOptions(enabled);

		ArchitectureOptionsBulkToggle.AreAllLayerInformationOptionsEnabled(options).Should().Be(enabled);
		options.AnyEnabled.Should().Be(enabled);
		foreach (var site in ArchitectureDependencySites.All)
		{
			options.IsEnabled(site).Should().Be(enabled);
		}
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void BulkToggle_CreateSiteDiagnosticOptions_SetsEverySiteDiagnosticOption(bool enabled)
	{
		var options = ArchitectureOptionsBulkToggle.CreateSiteDiagnosticOptions(enabled);

		ArchitectureOptionsBulkToggle.AreAllSiteDiagnosticOptionsEnabled(options).Should().Be(enabled);
		options.AnyEnabled.Should().Be(enabled);
		foreach (var site in ArchitectureDependencySites.All)
		{
			options.IsEnabled(site).Should().Be(enabled);
		}
	}

	[Fact]
	public void LayerCodeLensText_FormatsLayerSummaryWithoutGlobalRulesByDefault()
	{
		var indicator = CreateLayerIndicator();

		var result = ArchitectureLayerCodeLensText.CreateSummary(indicator, ArchitectureEditorOptions.Default);

		result.Should().Be("AnaalIJzer layer: Application | called by 1 layer | can call 1 layer");
	}

	[Fact]
	public void LayerCodeLensText_CanIncludeGlobalRules()
	{
		var indicator = CreateLayerIndicator();
		var options = new ArchitectureEditorOptions(showGlobalLayerRulesInBadges: true);

		var result = ArchitectureLayerCodeLensText.CreateSummary(indicator, options);

		result.Should().Be("AnaalIJzer layer: Application | called by 2 layers | can call 2 layers");
	}

	[Fact]
	public void LayerCodeLensText_FormatsUnclassifiedType()
	{
		var indicator = new ArchitectureLayerIndicator(
			new TextSpan(0, 10),
			new TextSpan(0, 10),
			"MysteryIngredient",
			"not in layer",
			ImmutableArray<string>.Empty,
			null,
			0,
			false);

		var result = ArchitectureLayerCodeLensText.CreateSummary(indicator, ArchitectureEditorOptions.Default);

		result.Should().Be("AnaalIJzer layer: not in a configured layer");
	}

	private static string FindRepositoryFile(params string[] relativeParts)
	{
		var directory = new DirectoryInfo(AppContext.BaseDirectory);
		while (directory is not null)
		{
			var path = Path.Combine([directory.FullName, ..relativeParts]);
			if (File.Exists(path))
			{
				return path;
			}

			directory = directory.Parent;
		}

		throw new FileNotFoundException("Could not find repository file.", Path.Combine(relativeParts));
	}

	private static ArchitectureLayerIndicator CreateLayerIndicator()
	{
		var result = new ArchitectureLayerIndicator(
			new TextSpan(0, 10),
			new TextSpan(0, 10),
			"PizzaKitchen",
			"Application",
			ImmutableArray.Create("Application"),
			null,
			1,
			true,
			ImmutableArray.Create("* (any layer)", "Controller"),
			ImmutableArray.Create("*", "Repository"));

		return result;
	}

	private static string FindGeneratedPackageDefinition()
	{
		var projectBin = Path.Combine(
			FindRepositoryDirectory(),
			"src",
			"Extensions",
			"RonSijm.AnaalIJzer.VisualStudio",
			"bin");
		var result = Directory.EnumerateFiles(projectBin, "RonSijm.AnaalIJzer.VisualStudio.pkgdef", SearchOption.AllDirectories)
			.OrderByDescending(File.GetLastWriteTimeUtc)
			.First();

		return result;
	}

	private static string FindGeneratedVsix()
	{
		var projectBin = Path.Combine(
			FindRepositoryDirectory(),
			"src",
			"Extensions",
			"RonSijm.AnaalIJzer.VisualStudio",
			"bin");
		var result = Directory.EnumerateFiles(projectBin, "RonSijm.AnaalIJzer.VisualStudio.vsix", SearchOption.AllDirectories)
			.OrderByDescending(File.GetLastWriteTimeUtc)
			.First();

		return result;
	}

	private static string FindRepositoryDirectory()
	{
		var directory = new DirectoryInfo(AppContext.BaseDirectory);
		while (directory is not null)
		{
			if (File.Exists(Path.Combine(directory.FullName, "README.md"))
			    && Directory.Exists(Path.Combine(directory.FullName, "src", "Extensions", "RonSijm.AnaalIJzer.VisualStudio")))
			{
				return directory.FullName;
			}

			directory = directory.Parent;
		}

		throw new DirectoryNotFoundException("Could not find repository directory.");
	}
}
