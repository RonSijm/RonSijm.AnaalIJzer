; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
ARCH_CONF_003 | Architecture.Configuration | Error | Invalid architecture configuration
ARCH_CONF_006 | Architecture.Configuration | Error | Cyclic configured dependency graph
ARCH_EXC_009 | Architecture.Exception | Warning | Architecture exception requires review
ARCH_PROJ_001 | Architecture.Project | Error | Direct project reference is not allowed
ARCH_PKG_001 | Architecture.Package | Error | Direct package reference is not allowed
ARCH_ASSM_001 | Architecture.Assembly | Error | Assembly attribute is not allowed
ARCH_DEP_001 | Architecture.Dependency | Error | Source dependency is not allowed
ARCH_DEP_002 | Architecture.Dependency | Error | Required dependency classification is missing
ARCH_DEP_004 | Architecture.Dependency | Error | Source dependency uses the reverse configured direction
ARCH_DEP_005 | Architecture.Dependency | Error | Peer-scope dependency is not allowed
ARCH_DEP_006 | Architecture.Dependency | Error | Observed source dependency graph contains a cycle
ARCH_TYPE_001 | Architecture.Type | Error | Type is not allowed by an effective type policy
ARCH_BOUND_007 | Architecture.Boundary | Error | Dependency enters through a disallowed boundary entry point
ARCH_NS_007 | Architecture.Namespace | Error | Namespace hierarchy boundary is crossed incorrectly
ARCH_SRC_007 | Architecture.Source | Error | Type declaration violates source placement
ARCH_API_001 | Architecture.Api | Error | Direct public API exposure is not allowed
ARCH_API_010 | Architecture.Api | Error | Transitive public API exposure is not allowed
ARCH_VIS_001 | Architecture.Visibility | Error | Declared accessibility is not allowed
ARCH_CONT_008 | Architecture.Contract | Error | Contract declaration shape does not match policy
ARCH_INH_001 | Architecture.Inheritance | Error | Inheritance or interface implementation is not allowed
ARCH_NAME_008 | Architecture.Name | Error | Name does not match the configured semantic or structural rule
ARCH_RET_001 | Architecture.Return | Error | Returned expression is not allowed
ARCH_OPER_001 | Architecture.Operation | Error | Selected source operation is not allowed
ARCH_OPER_002 | Architecture.Operation | Error | Required source operation is missing
ARCH_OPER_011 | Architecture.Operation | Error | Source operation count exceeds configured cardinality
ARCH_OPER_012 | Architecture.Operation | Error | Source operations violate configured ordering
ARCH_OPCT_001 | Architecture.OperationContract | Error | Operation-contract participant is not allowed
ARCH_OPCT_002 | Architecture.OperationContract | Error | Required operation-contract participant or invocation is missing
ARCH_OPCT_008 | Architecture.OperationContract | Error | Operation-contract response shape does not match policy

### Removed Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
ARCH001 | Architecture | Error | Replaced by ARCH_DEP_001
ARCH002 | Architecture | Error | Replaced by ARCH_DEP_002
ARCH003 | Architecture | Error | Replaced by ARCH_TYPE_001
ARCH004 | Architecture | Error | Replaced by ARCH_DEP_004
ARCH005 | Architecture | Error | Replaced by ARCH_DEP_005
