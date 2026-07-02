using System.Reflection;

[assembly: AssemblyMetadata("AnaalIJzerSettings", """
<ArchitecturalLevels>
  <Layer name="Application">
    <Class endsWith="Service" />
  </Layer>

  <Layer name="Command">
    <Class endsWith="Command" />
  </Layer>

  <Allowed>
    <Class startsWith="Create" />
    <Class startsWith="Cancel" />
  </Allowed>

  <AllowedDependency from="Application" to="Command" />
</ArchitecturalLevels>
""")]

public class CreateOrderCommand { }
public class CancelOrderCommand { }
public class ProcessOrderCommand { }

// Create and Cancel are approved command verbs.
public class CheckoutService(CreateOrderCommand create, CancelOrderCommand cancel) { }

// ARCH003: ProcessOrderCommand is in a configured layer, but its verb is not globally allowed.
public class WorkflowService(ProcessOrderCommand process) { }
