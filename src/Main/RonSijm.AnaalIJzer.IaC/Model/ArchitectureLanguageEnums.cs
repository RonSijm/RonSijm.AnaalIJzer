namespace RonSijm.AnaalIJzer.IaC;

public enum DependencySite
{
    Constructor,
    Method,
    MethodReturn,
    Field,
    Property,
    Local,
    New,
    GenericInvocation,
    GenericArgument,
    Inheritance,
    InterfaceImplementation,
    Attribute,
    StaticMember
}

public enum DeclaredTypeKind
{
    Class,
    Interface,
    Struct,
    Record,
    RecordStruct,
    Enum,
    Delegate
}

public enum DeclarationTarget
{
    Type,
    NestedType,
    Constructor,
    Method,
    Property,
    Field,
    Event,
    Operator,
    Conversion
}

public enum DeclaredAccessibility
{
    Public,
    Internal,
    Protected,
    ProtectedInternal,
    PrivateProtected,
    Private,
    File
}

public enum ContractMemberKind
{
    Constructor,
    Method,
    Property,
    Event,
    Field,
    Operator,
    Conversion
}

public enum PropertyAccessor
{
    Get,
    Set,
    Init
}

public enum CodeObservationKind
{
    Throw,
    Invocation,
    New,
    Identifier,
    MemberAccess,
    Literal
}

public enum ReturnValueKind
{
    Literal,
    Invocation,
    New,
    Identifier,
    MemberAccess
}

public enum SemanticOperationKind
{
    Invocation,
    PropertyRead,
    PropertyWrite,
    FieldRead,
    FieldWrite,
    EventAccess,
    ObjectCreation,
    Conversion,
    Assignment,
    Return,
    Argument
}

public enum SemanticOperationMemberKind
{
    Method,
    Constructor,
    Property,
    Field,
    Event
}

public enum BehavioralOperationOrdering
{
    Dominance,
    Lexical
}

public enum NameRuleValueTracking
{
    Direct,
    IntraProcedural
}

public enum NamespaceHierarchyRelation
{
    DescendantToAncestor,
    AncestorToDescendant,
    SiblingToSibling,
    SameNamespace
}

public enum SourceLocationBase
{
    Project,
    Configuration,
    Absolute
}

public enum GeneratedCodeMode
{
    Exclude,
    IncludeConfigured,
    IncludeAll
}
