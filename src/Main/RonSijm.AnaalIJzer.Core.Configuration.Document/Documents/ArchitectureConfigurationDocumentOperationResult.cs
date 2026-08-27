namespace RonSijm.AnaalIJzer.Core.Configuration.Document.Documents;

public sealed class ArchitectureConfigurationDocumentOperationResult
{
	private ArchitectureConfigurationDocumentOperationResult(bool succeeded, string message)
	{
		Succeeded = succeeded;
		Message = message;
	}

	public bool Succeeded { get; }

	public string Message { get; }

	public static ArchitectureConfigurationDocumentOperationResult Success(string message)
	{
		var result = new ArchitectureConfigurationDocumentOperationResult(true, message);

		return result;
	}

	public static ArchitectureConfigurationDocumentOperationResult Failure(string message)
	{
		var result = new ArchitectureConfigurationDocumentOperationResult(false, message);

		return result;
	}
}
