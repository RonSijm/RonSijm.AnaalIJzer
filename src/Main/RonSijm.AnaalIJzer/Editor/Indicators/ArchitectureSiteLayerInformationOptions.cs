namespace RonSijm.AnaalIJzer.Indicators;

public sealed class ArchitectureSiteLayerInformationOptions(
    bool showConstructorLayerInformation = false,
    bool showMethodLayerInformation = false,
    bool showMethodReturnLayerInformation = false,
    bool showFieldLayerInformation = false,
    bool showPropertyLayerInformation = false,
    bool showLocalLayerInformation = false,
    bool showNewLayerInformation = false,
    bool showGenericInvocationLayerInformation = false,
    bool showGenericArgumentLayerInformation = false,
    bool showInheritanceLayerInformation = false,
    bool showInterfaceImplementationLayerInformation = false,
    bool showAttributeLayerInformation = false,
    bool showStaticMemberLayerInformation = false)
{
    public bool ShowConstructorLayerInformation { get; } = showConstructorLayerInformation;

    public bool ShowMethodLayerInformation { get; } = showMethodLayerInformation;

    public bool ShowMethodReturnLayerInformation { get; } = showMethodReturnLayerInformation;

    public bool ShowFieldLayerInformation { get; } = showFieldLayerInformation;

    public bool ShowPropertyLayerInformation { get; } = showPropertyLayerInformation;

    public bool ShowLocalLayerInformation { get; } = showLocalLayerInformation;

    public bool ShowNewLayerInformation { get; } = showNewLayerInformation;

    public bool ShowGenericInvocationLayerInformation { get; } = showGenericInvocationLayerInformation;

    public bool ShowGenericArgumentLayerInformation { get; } = showGenericArgumentLayerInformation;

    public bool ShowInheritanceLayerInformation { get; } = showInheritanceLayerInformation;

    public bool ShowInterfaceImplementationLayerInformation { get; } = showInterfaceImplementationLayerInformation;

    public bool ShowAttributeLayerInformation { get; } = showAttributeLayerInformation;

    public bool ShowStaticMemberLayerInformation { get; } = showStaticMemberLayerInformation;

    public bool AnyEnabled
	{
		get
		{
			var result = ShowConstructorLayerInformation
			             || ShowMethodLayerInformation
			             || ShowMethodReturnLayerInformation
			             || ShowFieldLayerInformation
			             || ShowPropertyLayerInformation
			             || ShowLocalLayerInformation
			             || ShowNewLayerInformation
			             || ShowGenericInvocationLayerInformation
			             || ShowGenericArgumentLayerInformation
			             || ShowInheritanceLayerInformation
			             || ShowInterfaceImplementationLayerInformation
			             || ShowAttributeLayerInformation
			             || ShowStaticMemberLayerInformation;

			return result;
		}
	}

	public static ArchitectureSiteLayerInformationOptions None { get; } = new();

	public static ArchitectureSiteLayerInformationOptions All { get; } = new(
		true,
		true,
		true,
		true,
		true,
		true,
		true,
		true,
		true,
		true,
		true,
		true,
		true);

	public bool IsEnabled(string site)
	{
		var result = site switch
		{
			ArchitectureDependencySites.Constructor => ShowConstructorLayerInformation,
			ArchitectureDependencySites.Method => ShowMethodLayerInformation,
			ArchitectureDependencySites.MethodReturn => ShowMethodReturnLayerInformation,
			ArchitectureDependencySites.Field => ShowFieldLayerInformation,
			ArchitectureDependencySites.Property => ShowPropertyLayerInformation,
			ArchitectureDependencySites.Local => ShowLocalLayerInformation,
			ArchitectureDependencySites.New => ShowNewLayerInformation,
			ArchitectureDependencySites.GenericInvocation => ShowGenericInvocationLayerInformation,
			ArchitectureDependencySites.GenericArgument => ShowGenericArgumentLayerInformation,
			ArchitectureDependencySites.Inheritance => ShowInheritanceLayerInformation,
			ArchitectureDependencySites.InterfaceImplementation => ShowInterfaceImplementationLayerInformation,
			ArchitectureDependencySites.Attribute => ShowAttributeLayerInformation,
			ArchitectureDependencySites.StaticMember => ShowStaticMemberLayerInformation,
			_ => false
		};

		return result;
	}
}
