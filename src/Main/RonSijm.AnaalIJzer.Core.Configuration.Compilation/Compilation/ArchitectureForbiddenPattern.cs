namespace RonSijm.AnaalIJzer.Config.Compilation;

internal readonly struct ArchitectureForbiddenPattern
{
    internal ArchitectureForbiddenPattern(string name, string? comment)
    {
        Name = name;
        Comment = comment;
    }

    internal string Name { get; }

    internal string? Comment { get; }
}
