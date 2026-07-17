namespace RonSijm.AnaalIJzer.ConfigurationEditing.Editing;

public sealed class ArchitectureConfigurationEditResult
{
	private ArchitectureConfigurationEditResult(bool succeeded, string message)
	{
		Succeeded = succeeded;
		Message = message;
	}

	public bool Succeeded { get; }

	public string Message { get; }

	public static ArchitectureConfigurationEditResult Success(string message)
	{
		var result = new ArchitectureConfigurationEditResult(true, message);

		return result;
	}

	public static ArchitectureConfigurationEditResult Failure(string message)
	{
		var result = new ArchitectureConfigurationEditResult(false, message);

		return result;
	}
}
