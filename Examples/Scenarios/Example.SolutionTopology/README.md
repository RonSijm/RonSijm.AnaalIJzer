# Example.SolutionTopology

This scenario keeps the compiler analyzer deliberately quiet: solution topology is a
workspace concern, because a single project cannot know which other projects belong to
the loaded solution.

The configuration classifies the three projects as `DiningRoom`, `Kitchen`, and
`Pantry`. The order screen may reference the kitchen, but the kitchen's direct project
reference to the pantry is deliberately blocked.

The ordinary build succeeds:

```cmd
dotnet build Example.SolutionTopology.slnx -c Release
```

The participating projects are `Example.SolutionTopology.Web`,
`Example.SolutionTopology.Application`, and
`Example.SolutionTopology.Infrastructure`.

The solution inspection fails with one `ARCH_SOL_001` finding:

```cmd
arse inspect --solution Example.SolutionTopology.slnx --enforce-topology
```

That result points at the actual `Example.SolutionTopology.Application ->
Example.SolutionTopology.Infrastructure` project reference and explains the configured
module rule that rejected it.
