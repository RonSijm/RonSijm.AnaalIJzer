using RonSijm.AnaalIJzer.ConfigurationEditing.Document;
using RonSijm.AnaalIJzer.Core.Configuration.Document.Documents;

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

	internal static ArchitectureConfigurationEditResult FromDocumentResult(ArchitectureConfigurationDocumentOperationResult result)
	{
		var convertedResult = result.Succeeded
			? Success(result.Message)
			: Failure(result.Message);

		return convertedResult;
	}
}
