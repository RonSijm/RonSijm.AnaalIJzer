// Nested <Exceptions> — overlapping patterns, alternating rule membership.
//
// The base rule assigns every *Repository to the Repository layer. There is no
// Presentation -> Persistence edge, so an endpoint depending on a Repository triggers ARCH001.
//
// Exception depth determines membership (odd = excepted/NOT Repository, even = included/IS Repository):
//
//   Type                               Deepest matching depth   Result
//   ────────────────────────────────   ──────────────────────   ──────────────────
//   InMemoryOrderRepository            1  startsWith "InMemory"              -> NOT Repository
//   InMemoryCachedOrderRepository      2  startsWith "InMemoryCached"        -> IS  Repository, ARCH001
//   InMemoryCachedTestOrderRepository  3  exact type name                    -> NOT Repository
//   LegacyInMemoryCachedOrderRepository 4 exact type name                    -> IS  Repository, ARCH001

using System.Reflection;

// Inline config keeps example rules next to the code they validate.
[assembly: AssemblyMetadata("AnaalIJzerSettings", $"""
<ArchitecturalLevels strict="false">

  <Layer name="Presentation">
    <Class endsWith="Endpoint" />
  </Layer>

  <Layer name="Persistence">
    <Class endsWith="Repository">
      <Exceptions>
        <Class startsWith="InMemory">
          <Exceptions>
            <Class startsWith="InMemoryCached">
              <Exceptions>
                <Class typeName="{nameof(InMemoryCachedTestOrderRepository)}">
                  <Exceptions>
                    <Class typeName="{nameof(LegacyInMemoryCachedOrderRepository)}" />
                  </Exceptions>
                </Class>
              </Exceptions>
            </Class>
          </Exceptions>
        </Class>
      </Exceptions>
    </Class>
  </Layer>

  </ArchitecturalLevels>
""")]

public class InMemoryOrderRepository { }
public class InMemoryCachedOrderRepository { }
public class InMemoryCachedTestOrderRepository { }
public class LegacyInMemoryCachedOrderRepository { }

// Depth 1 (odd): not in Persistence, so no dependency diagnostic is reported.
public class OrderEndpoint(InMemoryOrderRepository fake) { }

// ARCH001: Depth 2 (even): the type is in Persistence.
public class AdminEndpoint(InMemoryCachedOrderRepository repository) { }

// Depth 3 (odd): the exact type is excluded from Persistence again.
public class TestEndpoint(InMemoryCachedTestOrderRepository repository) { }

// ARCH001: Depth 4 (even): the exact type is included in Persistence again.
public class LegacyEndpoint(LegacyInMemoryCachedOrderRepository repository) { }
