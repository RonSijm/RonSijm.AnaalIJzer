using System.ComponentModel;
using RonSijm.AnaalIJzer.Core.Indicators;

namespace RonSijm.AnaalIJzer.VisualStudio.Options;

public sealed partial class AnaalIJzerOptionsPage
{
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
			if (_isLoadingSettings)
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
