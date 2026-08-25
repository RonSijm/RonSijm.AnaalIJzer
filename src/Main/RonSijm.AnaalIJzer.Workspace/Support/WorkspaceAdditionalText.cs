using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace RonSijm.AnaalIJzer.Workspace;

internal sealed class WorkspaceAdditionalText(string path, SourceText text) : AdditionalText
{
	public override string Path { get; } = path;

	public override SourceText GetText(CancellationToken cancellationToken = default)
	{
		var result = text;

		return result;
	}

	public static WorkspaceAdditionalText FromFile(string path, CancellationToken cancellationToken)
	{
		var content = File.ReadAllText(path);
		cancellationToken.ThrowIfCancellationRequested();
		var text = SourceText.From(content);
		var result = new WorkspaceAdditionalText(path, text);

		return result;
	}

	public static WorkspaceAdditionalText FromText(string path, string content)
	{
		var text = SourceText.From(content);
		var result = new WorkspaceAdditionalText(path, text);

		return result;
	}
}
