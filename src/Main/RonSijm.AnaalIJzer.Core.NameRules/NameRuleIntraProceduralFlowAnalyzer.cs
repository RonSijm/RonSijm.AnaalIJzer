using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.CodeAnalysis.Operations;
using RonSijm.AnaalIJzer.Core.Indicators;

namespace RonSijm.AnaalIJzer.Core.NameRules;

/// <summary>
///     Recovers only local, intraprocedural name provenance. Unknown calls, collections,
///     callback boundaries, and branch disagreement intentionally erase provenance.
/// </summary>
public static class NameRuleIntraProceduralFlowAnalyzer
{
    public static ImmutableArray<NameRuleProvenanceFlow> Analyze(ControlFlowGraph graph, ISymbol owningSymbol)
    {
        var blockStates = CalculateExitStates(graph);
        var flows = ImmutableArray.CreateBuilder<NameRuleProvenanceFlow>();
        foreach (var block in graph.Blocks)
        {
            var state = GetIncomingState(block, blockStates);
            ProcessBlock(block, state, owningSymbol, flows);
        }

        var result = flows.ToImmutable();

        return result;
    }

    public static ImmutableArray<NameRuleProvenanceFlow> AnalyzeLinearly(IOperation operation, ISymbol owningSymbol)
    {
        var flows = ImmutableArray.CreateBuilder<NameRuleProvenanceFlow>();
        var state = ProvenanceState.Empty;
        ProcessLinearOperation(operation, state, owningSymbol, flows);
        var result = flows.ToImmutable();

        return result;
    }

    private static ImmutableDictionary<BasicBlock, ProvenanceState> CalculateExitStates(ControlFlowGraph graph)
    {
        var states = ImmutableDictionary.CreateBuilder<BasicBlock, ProvenanceState>();
        var maximumIterations = Math.Max(1, graph.Blocks.Length * graph.Blocks.Length);
        for (var iteration = 0; iteration < maximumIterations; iteration++)
        {
            var changed = false;
            foreach (var block in graph.Blocks)
            {
                var incoming = GetIncomingState(block, states);
                var state = incoming;
                ProcessBlock(block, state, owningSymbol: null, flows: null);
                if (states.TryGetValue(block, out var existing) && existing.Equals(state))
                {
                    continue;
                }

                states[block] = state;
                changed = true;
            }

            if (!changed)
            {
                break;
            }
        }

        var result = states.ToImmutable();

        return result;
    }

    private static ProvenanceState GetIncomingState(BasicBlock block, IReadOnlyDictionary<BasicBlock, ProvenanceState> states)
    {
        var predecessorStates = ImmutableArray.CreateBuilder<ProvenanceState>();
        foreach (var predecessor in block.Predecessors)
        {
            if (!states.TryGetValue(predecessor.Source, out var state))
            {
                var missingResult = ProvenanceState.Empty;

                return missingResult;
            }

            predecessorStates.Add(state);
        }

        var result = ProvenanceState.Merge(predecessorStates.ToImmutable());

        return result;
    }

    private static void ProcessBlock(BasicBlock block, ProvenanceState state, ISymbol? owningSymbol, ImmutableArray<NameRuleProvenanceFlow>.Builder? flows)
    {
        foreach (var operation in block.Operations)
        {
            ProcessOperation(operation, state, owningSymbol, flows);
        }

        if (block.BranchValue is not null)
        {
            if (HasReturnBranch(block))
            {
                ProcessReturnValue(block.BranchValue, owningSymbol, state, flows);
            }
            else
            {
                ProcessOperation(block.BranchValue, state, owningSymbol, flows);
            }
        }
    }

    private static bool HasReturnBranch(BasicBlock block)
    {
        var result = block.FallThroughSuccessor?.Semantics == ControlFlowBranchSemantics.Return
            || block.ConditionalSuccessor?.Semantics == ControlFlowBranchSemantics.Return;

        return result;
    }

    private static void ProcessLinearOperation(IOperation operation, ProvenanceState state, ISymbol owningSymbol, ImmutableArray<NameRuleProvenanceFlow>.Builder flows)
    {
        switch (operation)
        {
            case IAnonymousFunctionOperation:
                return;
            case IBlockOperation:
                foreach (var child in operation.ChildOperations)
                {
                    ProcessLinearOperation(child, state, owningSymbol, flows);
                }

                return;
            case IConditionalOperation:
            case ILoopOperation:
            case ISwitchOperation:
            case ITryOperation:
                state.Clear();
                return;
            default:
                ProcessOperation(operation, state, owningSymbol, flows);
                return;
        }
    }

    private static void ProcessOperation(IOperation operation, ProvenanceState state, ISymbol? owningSymbol, ImmutableArray<NameRuleProvenanceFlow>.Builder? flows)
    {
        switch (operation)
        {
            case IVariableDeclarationGroupOperation declarationGroup:
                foreach (var declaration in declarationGroup.Declarations)
                {
                    foreach (var declarator in declaration.Declarators)
                    {
                        ProcessVariableDeclarator(declarator, state, flows);
                    }
                }

                return;
            case IVariableDeclaratorOperation declarator:
                ProcessVariableDeclarator(declarator, state, flows);
                return;
            case IExpressionStatementOperation expressionStatement:
                ProcessOperation(expressionStatement.Operation, state, owningSymbol, flows);
                return;
            case ISimpleAssignmentOperation assignment:
                ProcessAssignment(assignment, state, flows);
                return;
            case ICompoundAssignmentOperation compoundAssignment:
                ProcessCompoundAssignment(compoundAssignment, state, flows);
                return;
            case IInvocationOperation invocation:
                ProcessArguments(invocation.Arguments, DependencySites.Method, state, flows);
                return;
            case IObjectCreationOperation creation:
                ProcessArguments(creation.Arguments, DependencySites.Constructor, state, flows);
                return;
            case IReturnOperation returnOperation:
                ProcessReturn(returnOperation, owningSymbol, state, flows);
                return;
        }
    }

    private static void ProcessVariableDeclarator(IVariableDeclaratorOperation declarator, ProvenanceState state, ImmutableArray<NameRuleProvenanceFlow>.Builder? flows)
    {
        if (declarator.Symbol is not ILocalSymbol local)
        {
            return;
        }

        var value = declarator.Initializer?.Value;
        if (value is null)
        {
            state.Remove(local);
            return;
        }

        var target = NameRuleSemanticSubjectResolver.CreateSymbolSubject(local, local.Name, preferContainingType: false);
        var provenance = TryResolveProvenance(value, state);
        if (target is not null && provenance is not null)
        {
            AddFlowIfIndirect(flows, provenance.Value, value, target.Value, DependencySites.Local);
        }

        UpdateLocalProvenance(local, provenance, state);
    }

    private static void ProcessAssignment(ISimpleAssignmentOperation assignment, ProvenanceState state, ImmutableArray<NameRuleProvenanceFlow>.Builder? flows)
    {
        var provenance = TryResolveProvenance(assignment.Value, state);
        var target = CreateSubject(assignment.Target);
        var site = GetAssignmentSite(assignment.Target);
        if (target is not null && provenance is not null)
        {
            AddFlowIfIndirect(flows, provenance.Value, assignment.Value, target.Value, site);
        }

        if (TryGetLocal(assignment.Target, out var local))
        {
            UpdateLocalProvenance(local, provenance, state);
        }
    }

    private static void ProcessCompoundAssignment(ICompoundAssignmentOperation assignment, ProvenanceState state, ImmutableArray<NameRuleProvenanceFlow>.Builder? flows)
    {
        var provenance = TryResolveProvenance(assignment.Value, state);
        var target = CreateSubject(assignment.Target);
        var site = GetAssignmentSite(assignment.Target);
        if (target is not null && provenance is not null)
        {
            AddFlowIfIndirect(flows, provenance.Value, assignment.Value, target.Value, site);
        }

        if (TryGetLocal(assignment.Target, out var local))
        {
            state.Remove(local);
        }
    }

    private static void ProcessArguments(ImmutableArray<IArgumentOperation> arguments, string site, ProvenanceState state, ImmutableArray<NameRuleProvenanceFlow>.Builder? flows)
    {
        foreach (var argument in arguments)
        {
            if (argument.Parameter is null)
            {
                continue;
            }

            if (argument.Parameter.RefKind == RefKind.Out)
            {
                ProcessOutArgument(argument, state, flows);
                continue;
            }

            var provenance = TryResolveProvenance(argument.Value, state);
            var target = NameRuleSemanticSubjectResolver.CreateSymbolSubject(argument.Parameter, argument.Parameter.Name, preferContainingType: false);
            if (target is not null && provenance is not null)
            {
                AddFlowIfIndirect(flows, provenance.Value, argument.Value, target.Value, site);
            }

            if (argument.Parameter.RefKind == RefKind.Ref && TryGetLocal(argument.Value, out var refLocal))
            {
                state.Remove(refLocal);
            }
        }
    }

    private static void ProcessOutArgument(IArgumentOperation argument, ProvenanceState state, ImmutableArray<NameRuleProvenanceFlow>.Builder? flows)
    {
        if (argument.Parameter is null || !TryGetLocal(argument.Value, out var local))
        {
            return;
        }

        var subject = NameRuleSemanticSubjectResolver.CreateSymbolSubject(argument.Parameter, argument.Parameter.Name, preferContainingType: false);
        if (subject is null)
        {
            state.Remove(local);
            return;
        }

        var provenance = new NameRuleProvenance(subject.Value, argument.Parameter);
        var target = NameRuleSemanticSubjectResolver.CreateSymbolSubject(local, local.Name, preferContainingType: false);
        if (target is not null)
        {
            AddFlowIfIndirect(flows, provenance, argument.Value, target.Value, DependencySites.Method);
        }

        state.Set(local, provenance);
    }

    private static void ProcessReturn(IReturnOperation returnOperation, ISymbol? owningSymbol, ProvenanceState state, ImmutableArray<NameRuleProvenanceFlow>.Builder? flows)
    {
        if (returnOperation.ReturnedValue is null)
        {
            return;
        }

        ProcessReturnValue(returnOperation.ReturnedValue, owningSymbol, state, flows);
    }

    private static void ProcessReturnValue(IOperation returnedValue, ISymbol? owningSymbol, ProvenanceState state, ImmutableArray<NameRuleProvenanceFlow>.Builder? flows)
    {
        if (owningSymbol is not IMethodSymbol method || method.MethodKind == MethodKind.AnonymousFunction || !TryCreateReturnTarget(method, out var target, out var site))
        {
            return;
        }

        var provenance = TryResolveProvenance(returnedValue, state);
        if (provenance is not null)
        {
            AddFlowIfIndirect(flows, provenance.Value, returnedValue, target, site);
        }
    }

    private static bool TryCreateReturnTarget(IMethodSymbol method, out NameRuleSubject target, out string site)
    {
        if (method.AssociatedSymbol is IPropertySymbol property)
        {
            var propertySubject = NameRuleSemanticSubjectResolver.CreateSymbolSubject(property, property.Name, preferContainingType: false);
            if (propertySubject is not null)
            {
                target = propertySubject.Value;
                site = DependencySites.Property;
                var propertyResult = true;

                return propertyResult;
            }
        }

        var methodSubject = NameRuleSemanticSubjectResolver.CreateSymbolSubject(method, method.Name, preferContainingType: false);
        if (methodSubject is null)
        {
            target = default;
            site = string.Empty;
            var missingResult = false;

            return missingResult;
        }

        target = methodSubject.Value;
        site = DependencySites.MethodReturn;
        var result = true;

        return result;
    }

    private static void AddFlowIfIndirect(ImmutableArray<NameRuleProvenanceFlow>.Builder? flows, NameRuleProvenance provenance, IOperation immediateOperation, NameRuleSubject target, string site)
    {
        if (flows is null)
        {
            return;
        }

        var immediateSource = CreateSubject(immediateOperation);
        if (immediateSource is not null && string.Equals(immediateSource.Value.NormalizedName, provenance.Subject.NormalizedName, StringComparison.Ordinal))
        {
            return;
        }

        flows.Add(new NameRuleProvenanceFlow(provenance.Subject, immediateSource, target, site, immediateOperation.Syntax.GetLocation()));
    }

    private static NameRuleProvenance? TryResolveProvenance(IOperation operation, ProvenanceState state)
    {
        var unwrapped = Unwrap(operation);
        switch (unwrapped)
        {
            case ILocalReferenceOperation localReference:
                var localResult = state.TryGet(localReference.Local, out var localProvenance) ? localProvenance : null;

                return localResult;
            case IParameterReferenceOperation parameterReference:
                return CreateProvenance(parameterReference.Parameter);
            case IFieldReferenceOperation fieldReference:
                return CreateProvenance(fieldReference.Field);
            case IPropertyReferenceOperation propertyReference:
                return CreateProvenance(propertyReference.Property);
            default:
                return null;
        }
    }

    private static NameRuleProvenance? CreateProvenance(ISymbol symbol)
    {
        var subject = CreateSymbolSubject(symbol);
        NameRuleProvenance? result = subject is null ? null : new NameRuleProvenance(subject.Value, symbol);

        return result;
    }

    private static NameRuleSubject? CreateSubject(IOperation operation)
    {
        var unwrapped = Unwrap(operation);
        var symbol = unwrapped switch
        {
            ILocalReferenceOperation localReference => (ISymbol)localReference.Local,
            IParameterReferenceOperation parameterReference => parameterReference.Parameter,
            IFieldReferenceOperation fieldReference => fieldReference.Field,
            IPropertyReferenceOperation propertyReference => propertyReference.Property,
            _ => null,
        };
        var result = symbol is null ? null : CreateSymbolSubject(symbol);

        return result;
    }

    private static NameRuleSubject? CreateSymbolSubject(ISymbol symbol)
    {
        var preferContainingType = symbol is IFieldSymbol or IPropertySymbol;
        var result = NameRuleSemanticSubjectResolver.CreateSymbolSubject(symbol, symbol.Name, preferContainingType);

        return result;
    }

    private static IOperation Unwrap(IOperation operation)
    {
        var current = operation;
        while (current is IConversionOperation conversion)
        {
            current = conversion.Operand;
        }

        var result = current;

        return result;
    }

    private static string GetAssignmentSite(IOperation target)
    {
        var unwrapped = Unwrap(target);
        var result = unwrapped switch
        {
            IFieldReferenceOperation => DependencySites.Field,
            IPropertyReferenceOperation => DependencySites.Property,
            ILocalReferenceOperation => DependencySites.Local,
            IParameterReferenceOperation => DependencySites.Method,
            _ => DependencySites.Local,
        };

        return result;
    }

    private static bool TryGetLocal(IOperation operation, out ILocalSymbol local)
    {
        var unwrapped = Unwrap(operation);
        if (unwrapped is ILocalReferenceOperation localReference)
        {
            local = localReference.Local;
            var result = true;

            return result;
        }

        local = null!;
        var missingResult = false;

        return missingResult;
    }

    private static void UpdateLocalProvenance(ILocalSymbol local, NameRuleProvenance? provenance, ProvenanceState state)
    {
        if (provenance is null)
        {
            state.Remove(local);
            return;
        }

        state.Set(local, provenance.Value);
    }

    private readonly struct NameRuleProvenance(NameRuleSubject subject, ISymbol originSymbol)
    {
        public NameRuleSubject Subject { get; } = subject;
        public ISymbol OriginSymbol { get; } = originSymbol;

        public bool Equals(NameRuleProvenance other)
        {
            var result = string.Equals(Subject.DisplayName, other.Subject.DisplayName, StringComparison.Ordinal)
                && SymbolEqualityComparer.Default.Equals(OriginSymbol, other.OriginSymbol);

            return result;
        }
    }

    private sealed class ProvenanceState
    {
        private readonly Dictionary<ILocalSymbol, NameRuleProvenance> _values = new(SymbolEqualityComparer.Default);

        public static ProvenanceState Empty => new();

        public bool TryGet(ILocalSymbol local, out NameRuleProvenance? provenance)
        {
            var found = _values.TryGetValue(local, out var value);
            provenance = found ? value : null;

            return found;
        }

        public void Set(ILocalSymbol local, NameRuleProvenance provenance)
        {
            _values[local] = provenance;
        }

        public void Remove(ILocalSymbol local)
        {
            _values.Remove(local);
        }

        public void Clear()
        {
            _values.Clear();
        }

        public bool Equals(ProvenanceState other)
        {
            if (_values.Count != other._values.Count)
            {
                return false;
            }

            foreach (var entry in _values)
            {
                var local = entry.Key;
                var provenance = entry.Value;
                if (!other._values.TryGetValue(local, out var otherProvenance) || !provenance.Equals(otherProvenance))
                {
                    return false;
                }
            }

            var result = true;

            return result;
        }

        public ProvenanceState Clone()
        {
            var result = new ProvenanceState();
            foreach (var entry in _values)
            {
                var local = entry.Key;
                var provenance = entry.Value;
                result._values.Add(local, provenance);
            }

            return result;
        }

        public static ProvenanceState Merge(ImmutableArray<ProvenanceState> states)
        {
            if (states.IsDefaultOrEmpty)
            {
                return Empty;
            }

            var result = states[0].Clone();
            foreach (var local in result._values.Keys.ToArray())
            {
                var provenance = result._values[local];
                if (states.Skip(1).Any(state => !state._values.TryGetValue(local, out var other) || !provenance.Equals(other)))
                {
                    result._values.Remove(local);
                }
            }

            return result;
        }
    }
}