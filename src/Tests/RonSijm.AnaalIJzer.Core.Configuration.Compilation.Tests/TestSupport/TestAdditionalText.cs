namespace RonSijm.AnaalIJzer.Core.Configuration.Compilation.Tests.TestSupport;

internal sealed class TestAdditionalText(string path, string content) : AdditionalText
{
    private readonly SourceText _text = SourceText.From(content);

    public override string Path { get; } = path;

    public override SourceText GetText(CancellationToken cancellationToken = default)
    {
        var result = _text;

        return result;
    }
}
