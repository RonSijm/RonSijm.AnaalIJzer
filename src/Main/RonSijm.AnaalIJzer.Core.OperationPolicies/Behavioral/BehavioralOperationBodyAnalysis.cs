using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.CodeAnalysis.Operations;
using RonSijm.AnaalIJzer.Core.SemanticOperations.Analysis;

namespace RonSijm.AnaalIJzer.Core.OperationPolicies.Behavioral;

/// <summary>Collects selected semantic operations and conservative control-flow dominance for one declaration body.</summary>
public sealed class BehavioralOperationBodyAnalysis
{
    private readonly ImmutableDictionary<int, ImmutableHashSet<int>> _dominators;
    private readonly ImmutableHashSet<int> _exitBlockOrdinals;

    private BehavioralOperationBodyAnalysis(ISymbol owningSymbol, Location declarationLocation, ImmutableArray<SemanticOperationOccurrence> operations, ImmutableDictionary<int, ImmutableHashSet<int>> dominators, ImmutableHashSet<int> exitBlockOrdinals)
    {
        OwningSymbol = owningSymbol;
        DeclarationLocation = declarationLocation;
        Operations = operations;
        _dominators = dominators;
        _exitBlockOrdinals = exitBlockOrdinals;
    }

    public ISymbol OwningSymbol { get; }

    public Location DeclarationLocation { get; }

    public ImmutableArray<SemanticOperationOccurrence> Operations { get; }

    public static bool TryCreate(IOperation operationBlock, ISymbol owningSymbol, out BehavioralOperationBodyAnalysis analysis)
    {
        if (!ControlFlowGraphFactory.TryCreate(operationBlock, out var graph))
        {
            analysis = null!;
            var missingGraphResult = false;

            return missingGraphResult;
        }

        var operations = ImmutableArray.CreateBuilder<SemanticOperationOccurrence>();
        var sourceOrder = 0;
        foreach (var block in graph.Blocks)
        {
            foreach (var operation in block.Operations)
            {
                CollectOperations(operation, block.Ordinal, owningSymbol, operations, ref sourceOrder);
            }

            if (block.BranchValue is not null)
            {
                CollectOperations(block.BranchValue, block.Ordinal, owningSymbol, operations, ref sourceOrder);
            }
        }

        var declarationLocation = owningSymbol.Locations.FirstOrDefault(location => location.IsInSource) ?? operationBlock.Syntax.GetLocation();
        analysis = new BehavioralOperationBodyAnalysis(
            owningSymbol,
            declarationLocation,
            operations.ToImmutable(),
            CalculateDominators(graph),
            graph.Blocks.Where(block => block.Kind == BasicBlockKind.Exit).Select(block => block.Ordinal).ToImmutableHashSet());
        var result = true;

        return result;
    }

    public bool Dominates(SemanticOperationOccurrence dominator, SemanticOperationOccurrence dominated)
    {
        if (dominator.BlockOrdinal == dominated.BlockOrdinal)
        {
            var sameBlockResult = dominator.IsLexicallyBefore(dominated);

            return sameBlockResult;
        }

        var result = DominatesBlock(dominator.BlockOrdinal, dominated.BlockOrdinal);

        return result;
    }

    public bool DominatesEveryExit(SemanticOperationOccurrence occurrence)
    {
        if (_exitBlockOrdinals.Count == 0)
        {
            return false;
        }

        foreach (var exitBlockOrdinal in _exitBlockOrdinals)
        {
            if (!DominatesBlock(occurrence.BlockOrdinal, exitBlockOrdinal))
            {
                return false;
            }
        }

        var result = true;

        return result;
    }

    private static void CollectOperations(IOperation operation, int blockOrdinal, ISymbol owningSymbol, ImmutableArray<SemanticOperationOccurrence>.Builder operations, ref int sourceOrder)
    {
        if (operation is IAnonymousFunctionOperation or ILocalFunctionOperation)
        {
            return;
        }

        if (SemanticOperationFactory.TryCreate(operation, owningSymbol, out var semanticOperation))
        {
            operations.Add(new SemanticOperationOccurrence(semanticOperation, blockOrdinal, operation.Syntax.SpanStart, sourceOrder));
            sourceOrder++;
        }

        foreach (var child in operation.ChildOperations)
        {
            CollectOperations(child, blockOrdinal, owningSymbol, operations, ref sourceOrder);
        }
    }

    private bool DominatesBlock(int dominatorBlockOrdinal, int dominatedBlockOrdinal)
    {
        if (!_dominators.TryGetValue(dominatedBlockOrdinal, out var dominators))
        {
            return false;
        }

        var result = dominators.Contains(dominatorBlockOrdinal);

        return result;
    }

    private static ImmutableDictionary<int, ImmutableHashSet<int>> CalculateDominators(ControlFlowGraph graph)
    {
        var allBlockOrdinals = graph.Blocks.Select(block => block.Ordinal).ToImmutableHashSet();
        var dominators = graph.Blocks.ToDictionary(
            block => block.Ordinal,
            block => block.Kind == BasicBlockKind.Entry
                ? ImmutableHashSet.Create(block.Ordinal)
                : allBlockOrdinals);

        var changed = true;
        while (changed)
        {
            changed = false;
            foreach (var block in graph.Blocks)
            {
                if (block.Kind == BasicBlockKind.Entry)
                {
                    continue;
                }

                var predecessors = block.Predecessors.Select(predecessor => predecessor.Source.Ordinal).ToArray();
                var next = predecessors.Length == 0
                    ? ImmutableHashSet.Create(block.Ordinal)
                    : IntersectPredecessorDominators(predecessors, dominators).Add(block.Ordinal);
                if (dominators[block.Ordinal].SetEquals(next))
                {
                    continue;
                }

                dominators[block.Ordinal] = next;
                changed = true;
            }
        }

        var result = dominators.ToImmutableDictionary();

        return result;
    }

    private static ImmutableHashSet<int> IntersectPredecessorDominators(IReadOnlyList<int> predecessors, IReadOnlyDictionary<int, ImmutableHashSet<int>> dominators)
    {
        var result = dominators[predecessors[0]];
        for (var index = 1; index < predecessors.Count; index++)
        {
            result = result.Intersect(dominators[predecessors[index]]).ToImmutableHashSet();
        }

        return result;
    }
}