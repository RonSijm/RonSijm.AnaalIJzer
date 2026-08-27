using System.ComponentModel;
using RonSijm.AnaalIJzer.Core.Indicators;

namespace RonSijm.AnaalIJzer.VisualStudio.Options;

public sealed partial class AnaalIJzerOptionsPage
{
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
			if (_isLoadingSettings)
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
}
