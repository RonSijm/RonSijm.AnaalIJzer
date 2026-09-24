using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace RonSijm.AnaalIJzer.Core.SemanticOperations.Analysis;

/// <summary>Creates a control-flow graph from an operation block or one of its supported Roslyn roots.</summary>
public static class ControlFlowGraphFactory
{
    public static bool TryCreate(IOperation operation, out ControlFlowGraph graph)
    {
        var root = FindSupportedRoot(operation);
        if (root is null)
        {
            graph = null!;
            var missingRootResult = false;

            return missingRootResult;
        }

        try
        {
            graph = root switch
            {
                IBlockOperation block => ControlFlowGraph.Create(block),
                IMethodBodyOperation methodBody => ControlFlowGraph.Create(methodBody),
                IConstructorBodyOperation constructorBody => ControlFlowGraph.Create(constructorBody),
                IFieldInitializerOperation fieldInitializer => ControlFlowGraph.Create(fieldInitializer),
                IPropertyInitializerOperation propertyInitializer => ControlFlowGraph.Create(propertyInitializer),
                IParameterInitializerOperation parameterInitializer => ControlFlowGraph.Create(parameterInitializer),
                IAttributeOperation attribute => ControlFlowGraph.Create(attribute),
                _ => null!,
            };
        }
        catch (ArgumentException)
        {
            graph = null!;
            var exceptionResult = false;

            return exceptionResult;
        }

        var result = graph is not null;

        return result;
    }

    private static IOperation? FindSupportedRoot(IOperation operation)
    {
        for (var current = operation; current is not null; current = current.Parent)
        {
            switch (current)
            {
                case IMethodBodyOperation:
                case IConstructorBodyOperation:
                case IFieldInitializerOperation:
                case IPropertyInitializerOperation:
                case IParameterInitializerOperation:
                case IAttributeOperation:
                    return current;
                case IBlockOperation { Parent: null }:
                    return current;
            }
        }

        IOperation? result = null;

        return result;
    }
}