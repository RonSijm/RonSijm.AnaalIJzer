# Example.NameRuleIntraProceduralTracking

The two services contain the same neutral local alias. `DirectTicketService` is quiet because `Direct` mode only sees `pending`. `TrackedTicketService` uses `valueTracking="IntraProcedural"`, so it follows the unambiguous `customerId -> pending` alias and reports three `ARCH_NAME_008` diagnostics when that value reaches an `orderId` parameter, an `orderId` method return, and an `orderId` parameter from a lambda body.

```cmd
dotnet build Examples\Features\Example.NameRuleIntraProceduralTracking -c Release
```

The tracking deliberately stops at method calls, collections, delegates, reflection, and ambiguous control-flow joins. Method, accessor, constructor, and local-function bodies use Roslyn control-flow graphs. Lambda bodies use an ordered conservative scan because Roslyn does not expose a standalone lambda control-flow graph. It is a bounded local-provenance check, not a whole-program taint analysis.
