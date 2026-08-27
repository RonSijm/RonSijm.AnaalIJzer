namespace RonSijm.AnaalIJzer.IntegrationTests.Support;

internal static class DocumentationFeatureMatrixFixture
{
	public const string Configuration = """
<ArchitecturalLevels enableDocumentation="true"
                     documentationPath="docs\architecture-documentation.md"
                     enableReport="true"
                     reportPath="docs\architectural-violations.md"
                     requireRecognizedDependencies="Constructor, Local"
                     enforceAcyclic="true"
                     enforceObservedAcyclic="true"
                     description="Feature matrix configuration for documentation coverage.">
  <ExceptionPolicy requireReason="true"
                   requireOwner="true"
                   requireExpiresOn="true"
                   warnBeforeDays="30"
                   description="Temporary exceptions must stay attributable and time-boxed." />
  <Allowed description="Global approved dependency type names.">
    <Class startsWith="I"
           endsWith="Contract"
           typeKind="Interface"
           description="Interface contracts are globally approved." />
  </Allowed>
  <Forbidden description="Global forbidden naming policies.">
    <Class endsWith="Store"
           typeKind="Class"
           comment="Use Repository instead."
           description="Store suffixes are legacy names.">
      <Fix Rename="Repository"
           description="Offer the repository suffix as a rename target." />
      <Exceptions description="Legacy store names are grandfathered.">
        <Class typeName="LegacyStore"
               reason="Legacy migration is still in progress."
               owner="Ordering Team"
               expiresOn="2026-08-30"
               description="Legacy store exception." />
      </Exceptions>
    </Class>
  </Forbidden>
  <Layer name="Ordering"
         requireRecognizedDependencies="MethodReturn"
         description="Ordering boundary with nested application and repository roles.">
    <Namespace startsWith="CandyShop.Ordering"
               description="Ordering namespace scope." />
    <Layer name="Application"
           description="Ordering application services.">
      <Class endsWith="Service"
             typeKind="Class"
             description="Service classes in the ordering boundary." />
      <Allowed description="Application dependency allow-list.">
        <Class endsWith="Contract"
               typeKind="Interface"
               description="Application code may consume contract interfaces." />
      </Allowed>
    </Layer>
    <Layer name="Repository"
           description="Ordering persistence implementation.">
      <Assembly exactName="CandyShop.Persistence"
                description="Repository implementation assembly." />
      <Class endsWith="Repository"
             typeKind="Class"
             description="Repository implementation classes." />
      <Forbidden description="Repository scoped forbidden types.">
        <Namespace contains=".Legacy"
                   description="Legacy persistence namespace is blocked." />
      </Forbidden>
    </Layer>
    <AllowedDependency from="Application"
                       to="Repository"
                       allowedSites="Constructor, Method"
                       description="Services may depend on repositories through constructor and method sites." />
    <BlockedDependency from="Application"
                       to="Repository"
                       allowedSites="MethodReturn"
                       description="Services may not expose repositories as method return values." />
  </Layer>
  <Layer name="Shared"
         description="Shared contracts used by other boundaries.">
    <Class endsWith="Contract"
           typeKind="Interface"
           description="Shared contract interfaces." />
  </Layer>
  <AllowedDependency from="*"
                     to="Shared"
                     blockedSites="Field"
                     appliesToDescendants="true"
                     description="Any layer may use shared contracts except as stored fields." />
  <AllowedDependency from="Ordering"
                     to="Shared"
                     allowedSites="Constructor, MethodReturn"
                     description="Ordering may depend on shared contracts and return them." />
</ArchitecturalLevels>
""";
}
