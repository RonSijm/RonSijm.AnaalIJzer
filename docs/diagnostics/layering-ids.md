### Why three IDs for layering instead of one?

The original design folded every layering problem under `ARCH001`. The three reasons are independent and call for different remediation:

- **Missing or site-filtered edge (ARCH001)** - most often a real architectural mistake, or a sign the configuration is incomplete. Fix the dependency, add an `<AllowedDependency>` edge, or adjust the edge's `allowedSites` / `blockedSites`.
- **Wrong direction (ARCH004)** - almost always a real architectural mistake. The fix is usually inversion of control (introduce an abstraction in the lower layer), never adding a reverse edge.
- **Same layer (ARCH005)** - sometimes intentional (helper types collaborating within a layer). Many teams want to suppress this category project-wide while keeping ARCH001/004 as errors.

Splitting the IDs makes the three policies independently configurable in `.editorconfig` or `<NoWarn>`, surfaces the reason directly in the IDE error list without parsing the message, and makes the architectural intent of each rule self-documenting.
