// Wildcard target: <AllowedDependency from="Diagnostics" to="*" />
// ArchitectureDiagnostics is in the Diagnostics layer, which is allowed to
// depend on any other configured layer — no per-layer edge required.

using System.Reflection;

// Inline config keeps example rules next to the code they validate.
[assembly: AssemblyMetadata("AnaalIJzerSettings", """
<ArchitecturalLevels>

  <Layer name="Diagnostics">
    <Class endsWith="Diagnostics" />
  </Layer>

  <Layer name="Application">
    <Class endsWith="Service" />
    <Class endsWith="Manager" />
    <Class endsWith="Coordinator" />
  </Layer>

  <Layer name="Repository">
    <Class endsWith="Repository" />
  </Layer>

  <AllowedDependency from="Application"  to="Repository" />
  <AllowedDependency from="Diagnostics"  to="*" />

</ArchitecturalLevels>
""")]

public interface IOrderRepository { }
public interface IOrderService { }

// Application -> Repository is the explicit edge.
public class OrderService(IOrderRepository orderRepository) { }

// Diagnostics -> Application and Diagnostics -> Repository are allowed by to="*".
public class ArchitectureDiagnostics(IOrderService service, IOrderRepository repository) { }
