using System.Reflection;

[assembly: AssemblyMetadata("AnaalIJzerSettings", """
<ArchitecturalLevels>
  <Layer name="Application">
    <Class endsWith="Service" />
  </Layer>

  <Layer name="Command">
    <Class endsWith="Command" />
    <Allowed>
      <Class startsWith="Create" />
      <Class startsWith="Cancel" />
    </Allowed>
  </Layer>

  <Layer name="Query">
    <Class endsWith="Query" />
    <Forbidden>
      <Class startsWith="Delete" />
    </Forbidden>
  </Layer>

  <Layer name="Audit">
    <Class endsWith="AuditRecord" />
  </Layer>

  <AllowedDependency from="Application" to="Command" />
  <AllowedDependency from="Application" to="Query" />
  <AllowedDependency from="Application" to="Audit" />
</ArchitecturalLevels>
""")]

public class CreateOrderCommand { }
public class ProcessOrderCommand { }
public class FindOrderQuery { }
public class DeleteOrderQuery { }
public class DeleteOrderAuditRecord { }

// The Command allow-list accepts Create, and the Query block-list accepts Find.
// DeleteOrderAuditRecord proves that the Query block-list does not leak into Audit.
public class CheckoutService(CreateOrderCommand create, FindOrderQuery find, DeleteOrderAuditRecord audit) { }

// ARCH003: Process is not an allowed Command verb.
public class WorkflowService(ProcessOrderCommand process) { }

// ARCH003: Delete is explicitly forbidden only inside the Query layer.
public class AdministrationService(DeleteOrderQuery delete) { }
