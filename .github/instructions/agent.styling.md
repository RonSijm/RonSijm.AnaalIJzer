# Agent Style Guide

- Keep get-only auto-properties on one line: `public ImmutableHashSet<string> WildcardTargets { get; } = wildcardTargets;`
- Keep constructor/object-creation calls on one line: `return new LayerMatch(rule.Layer, result.Length > 0 ? result : null, rule.XmlLineNumber, rule.XmlLinePosition);`
- Keep method signatures on one line: `private static bool IsExcepted(ImmutableArray<PatternMatcher> exceptions, string typeName, string namespaceName, ITypeSymbol? symbol)`
- For single-expression method returns, assign the expression to a local `result` variable, add a blank line, then `return result;`.
