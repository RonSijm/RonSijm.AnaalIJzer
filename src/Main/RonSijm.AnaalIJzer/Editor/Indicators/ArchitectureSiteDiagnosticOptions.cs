namespace RonSijm.AnaalIJzer.Indicators;

public sealed class ArchitectureSiteDiagnosticOptions(
    bool showConstructorSiteDiagnostics = false,
    bool showMethodSiteDiagnostics = false,
    bool showMethodReturnSiteDiagnostics = false,
    bool showFieldSiteDiagnostics = false,
    bool showPropertySiteDiagnostics = false,
    bool showLocalSiteDiagnostics = false,
    bool showNewSiteDiagnostics = false,
    bool showGenericInvocationSiteDiagnostics = false,
    bool showGenericArgumentSiteDiagnostics = false,
    bool showInheritanceSiteDiagnostics = false,
    bool showInterfaceImplementationSiteDiagnostics = false,
    bool showAttributeSiteDiagnostics = false,
    bool showStaticMemberSiteDiagnostics = false)
{
    public bool ShowConstructorSiteDiagnostics { get; } = showConstructorSiteDiagnostics;

    public bool ShowMethodSiteDiagnostics { get; } = showMethodSiteDiagnostics;

    public bool ShowMethodReturnSiteDiagnostics { get; } = showMethodReturnSiteDiagnostics;

    public bool ShowFieldSiteDiagnostics { get; } = showFieldSiteDiagnostics;

    public bool ShowPropertySiteDiagnostics { get; } = showPropertySiteDiagnostics;

    public bool ShowLocalSiteDiagnostics { get; } = showLocalSiteDiagnostics;

    public bool ShowNewSiteDiagnostics { get; } = showNewSiteDiagnostics;

    public bool ShowGenericInvocationSiteDiagnostics { get; } = showGenericInvocationSiteDiagnostics;

    public bool ShowGenericArgumentSiteDiagnostics { get; } = showGenericArgumentSiteDiagnostics;

    public bool ShowInheritanceSiteDiagnostics { get; } = showInheritanceSiteDiagnostics;

    public bool ShowInterfaceImplementationSiteDiagnostics { get; } = showInterfaceImplementationSiteDiagnostics;

    public bool ShowAttributeSiteDiagnostics { get; } = showAttributeSiteDiagnostics;

    public bool ShowStaticMemberSiteDiagnostics { get; } = showStaticMemberSiteDiagnostics;

    public bool AnyEnabled
	{
		get
		{
			var result = ShowConstructorSiteDiagnostics
			             || ShowMethodSiteDiagnostics
			             || ShowMethodReturnSiteDiagnostics
			             || ShowFieldSiteDiagnostics
			             || ShowPropertySiteDiagnostics
			             || ShowLocalSiteDiagnostics
			             || ShowNewSiteDiagnostics
			             || ShowGenericInvocationSiteDiagnostics
			             || ShowGenericArgumentSiteDiagnostics
			             || ShowInheritanceSiteDiagnostics
			             || ShowInterfaceImplementationSiteDiagnostics
			             || ShowAttributeSiteDiagnostics
			             || ShowStaticMemberSiteDiagnostics;

			return result;
		}
	}

	public static ArchitectureSiteDiagnosticOptions None { get; } = new();

	public static ArchitectureSiteDiagnosticOptions All { get; } = new(
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
			ArchitectureDependencySites.Constructor => ShowConstructorSiteDiagnostics,
			ArchitectureDependencySites.Method => ShowMethodSiteDiagnostics,
			ArchitectureDependencySites.MethodReturn => ShowMethodReturnSiteDiagnostics,
			ArchitectureDependencySites.Field => ShowFieldSiteDiagnostics,
			ArchitectureDependencySites.Property => ShowPropertySiteDiagnostics,
			ArchitectureDependencySites.Local => ShowLocalSiteDiagnostics,
			ArchitectureDependencySites.New => ShowNewSiteDiagnostics,
			ArchitectureDependencySites.GenericInvocation => ShowGenericInvocationSiteDiagnostics,
			ArchitectureDependencySites.GenericArgument => ShowGenericArgumentSiteDiagnostics,
			ArchitectureDependencySites.Inheritance => ShowInheritanceSiteDiagnostics,
			ArchitectureDependencySites.InterfaceImplementation => ShowInterfaceImplementationSiteDiagnostics,
			ArchitectureDependencySites.Attribute => ShowAttributeSiteDiagnostics,
			ArchitectureDependencySites.StaticMember => ShowStaticMemberSiteDiagnostics,
			_ => false
		};

		return result;
	}
}
