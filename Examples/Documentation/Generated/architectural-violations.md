# Architectural Violation Report

**Assembly**: `Example.ReportDemo`  
**Generated**: 2026-07-06T14:44:36Z

---

## Summary

| Rule | Violations |
|------|------------|
| ARCH001 — Illegal layer dependency | 1 |
| ARCH002 — Unrecognized dependency | 1 |
| ARCH003 — Type policy violation | 1 |
| ARCH004 — Wrong-direction dependency | 1 |
| ARCH005 — Same-layer dependency | 1 |
| **Total** | **5** |

---

## ARCH001 — Illegal Layer Dependencies

| Caller (layer) | Dependency (layer) | Reason |
|----------------|--------------------|--------|
| `AdminEndpoint` (Presentation) | `IOrderRepository` (Persistence) | no allowed dependency gate from 'Presentation' to 'Persistence' is configured in the root boundary |

## ARCH004 — Wrong-Direction Dependencies

The caller depends on a layer that is configured to depend on it. Reverse the dependency or invert it with an abstraction.

| Caller (layer) | Dependency (layer) | Reason |
|----------------|--------------------|--------|
| `OrderRepository` (Persistence) | `IOrderService` (Application) | this dependency goes the wrong direction — the reverse ('Application' → 'Persistence') is configured |

## ARCH005 — Same-Layer Dependencies

Types within the same layer may not depend on each other. Extract the shared concept to a lower layer or merge the responsibilities.

| Caller (layer) | Dependency | Reason |
|----------------|------------|--------|
| `OrderService` (Application) | `ISecondaryOrderService` | types in the same layer ('Application') may not depend on each other |

## ARCH002 — Unrecognized Dependencies

These types are injected into layered callers but are not configured in `Architecture.anl`.

| Caller (layer) | Unrecognized dependency | Note |
|----------------|-------------------------|------|
| `OrderCoordinator` (Application) | `MysteryDependency` |  |

---

## Suggested Configuration

Add the following to `Architecture.anl` to resolve all ARCH002 violations:

```xml
<!-- Resolves 1 violation(s) from layer 'Application' -->
<Layer name="MysteryDependency">
    <Class endsWith="MysteryDependency" />
</Layer>
<AllowedDependency from="Application" to="MysteryDependency" />
```

> **Note**: Review layer names and allowed paths before applying.

## ARCH003 — Type Policy Violations

These dependency types match an applicable `Forbidden` policy or fail an applicable `Allowed` policy.

| Caller (layer) | Dependency | Reason |
|----------------|------------|--------|
| `OrderManager` (Application) | `OrderStore` | the type matches a global &lt;Forbidden&gt; rule: Persistence types must use the Repository suffix. |

