### Forbidden operation policies

`<ForbiddenOperations>` rejects one selected resolved API operation inside an owning layer and its descendants. It is intentionally narrower than `<Forbidden>`: you can forbid `DateTime.UtcNow` without forbidding `DateTime`, or forbid `Environment.MachineName` while still allowing `Environment.NewLine`.

This is a semantic policy. AnaalIjzer compares Roslyn symbols, so an alias and a fully qualified spelling resolve to the same member. It does not need, and does not take, a dependency on the assembly that defines the selected member or attribute.

```xml
<Layer name="Kitchen">
  <Class endsWith="Kitchen" />

  <ForbiddenOperations description="Kitchens obtain time through the restaurant clock.">
    <ForbiddenOperation allowedSites="StaticMember"
                        description="A direct system-clock read hides a dependency.">
      <OperationMatcher kind="PropertyRead" staticAccess="true">
        <ContainingType exactFullName="System.DateTime" />
        <Member exactName="UtcNow" memberKind="Property" />
      </OperationMatcher>
    </ForbiddenOperation>
  </ForbiddenOperations>
</Layer>
```

That produces `ARCH021` for `DateTime.UtcNow` in `Kitchen` code. An injected `PizzaClock.UtcNow` property is unaffected because it is a different resolved symbol.

### How matching works

- Every `<ForbiddenOperation>` is a separate forbidden rule.
- Sibling `<OperationMatcher>` children in one rule are alternatives: matching **any** one reports `ARCH021`.
- `ContainingType` and `Member` inside one matcher are both required.
- Multiple matcher attributes on either child are also combined, using the normal AND-within / OR-between matcher model.
- `staticAccess="true"` or `staticAccess="false"` narrows the matcher. Omit it when both forms are meaningful.
- `allowedSites` and `blockedSites` use the same site filters as dependency rules. A static access is reported as `StaticMember` even if the surrounding expression assigns it to a local or returns it.
- An outer layer policy applies to nested child layers. A child layer cannot cancel a selected operation forbidden by its parent.

### Operation kinds

| `kind` value | Resolved operation |
|---|---|
| `Invocation` | Method or reduced extension-method call |
| `PropertyRead`, `PropertyWrite` | Property access or assignment |
| `FieldRead`, `FieldWrite` | Field access or assignment |
| `EventAccess` | Event subscription or access |
| `ObjectCreation` | A resolved constructor call |
| `Conversion` | A user-defined conversion operator |
| `Assignment` | A resolved property, field, or event assignment |
| `Return` | A return operation, without a selected member |
| `Argument` | An argument operation, without a selected member |

`<Member>` is available only for the operation kinds that select a member. Its optional `memberKind` is one of `Method`, `Constructor`, `Property`, `Field`, or `Event`; incompatible combinations are configuration errors (`ARCH006`).

### Common patterns

```xml
<!-- Do not block the caller while a pizza is prepared. -->
<ForbiddenOperation allowedSites="Method">
  <OperationMatcher kind="Invocation" staticAccess="false">
    <ContainingType typeName="Task" />
    <Member exactName="Wait" memberKind="Method" />
  </OperationMatcher>
</ForbiddenOperation>

<!-- Do not retrieve a dependency through IServiceProvider from application code. -->
<ForbiddenOperation allowedSites="MethodReturn">
  <OperationMatcher kind="Invocation" staticAccess="false">
    <ContainingType exactFullName="System.IServiceProvider" />
    <Member exactName="GetService" memberKind="Method" />
  </OperationMatcher>
</ForbiddenOperation>
```

There is no automatic code fix for `ARCH021`. A selected operation tells the analyzer what is not permitted, but it cannot decide whether your replacement should be an injected adapter, `await`, an explicit result type, or a different composition boundary.

When Sites Diagnostics is enabled in the Visual Studio companion, a matching operation is shown with its regular site label and an `ARCH021` policy-status explanation in QuickInfo. This remains opt-in with the rest of the site indicators, so a policy does not add editor adornments by default.

**Focused examples:**

- [`Example.Arch021.ClockAccess`](../../Examples/Diagnostics/Example.Arch021.ClockAccess) - `DateTime.UtcNow`, `DateTime.Now`, and `DateTime.Today`.
- [`Example.Arch021.BlockingTaskAccess`](../../Examples/Diagnostics/Example.Arch021.BlockingTaskAccess) - `Task.Wait()` at `Method` and `Task<T>.Result` at `Local`.
- [`Example.Arch021.ServiceLocation`](../../Examples/Diagnostics/Example.Arch021.ServiceLocation) - `IServiceProvider.GetService` outside the composition root.
- [`Example.Arch021.SelectedEnvironmentMember`](../../Examples/Diagnostics/Example.Arch021.SelectedEnvironmentMember) - one forbidden `Environment` property while another remains allowed.
