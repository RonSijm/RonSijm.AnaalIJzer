namespace RonSijm.AnaalIJzer.Core.Configuration.Document.Model;

public sealed class ArchitectureConfigurationCreationTarget(
	string title,
	string description,
	ArchitectureConfigurationSource source,
	ArchitectureConfigurationRegistrationKind registrationKind = ArchitectureConfigurationRegistrationKind.None,
	string registrationPath = "")
{
	public string Title { get; } = title;

	public string Description { get; } = description;

	public ArchitectureConfigurationSource Source { get; } = source;

	public ArchitectureConfigurationRegistrationKind RegistrationKind { get; } = registrationKind;

	public string RegistrationPath { get; } = registrationPath;
}
