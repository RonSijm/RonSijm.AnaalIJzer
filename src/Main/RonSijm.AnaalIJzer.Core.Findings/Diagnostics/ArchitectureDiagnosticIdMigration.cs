namespace RonSijm.AnaalIJzer.Core.Findings.Diagnostics;

public sealed class ArchitectureDiagnosticIdMigration(string oldId, IReadOnlyList<string> newIds, string note)
{
    public string OldId { get; } = oldId;

    public IReadOnlyList<string> NewIds { get; } = newIds;

    public string Note { get; } = note;

    public bool HasAutomaticReplacement => NewIds.Count == 1;
}