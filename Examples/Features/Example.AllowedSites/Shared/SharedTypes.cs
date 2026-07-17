// ReSharper disable All - Justification: Example File
using System;

namespace Example.AllowedSites.Shared;

public sealed class AllowedConstructorType;

public sealed class AllowedMethodType;

public sealed class AllowedMethodReturnType;

public sealed class AllowedFieldType;

public sealed class AllowedPropertyType;

public sealed class AllowedLocalType;

public sealed class AllowedNewType;

public sealed class AllowedGenericInvocationType;

public sealed class AllowedGenericArgumentType;

public class AllowedInheritanceType;

public interface IAllowedInterfaceImplementationType;

public sealed class AllowedAttributeType : Attribute;

public sealed class AllowedStaticMemberType
{
    public static void Use() { }
}

public sealed class BlockedConstructorType;

public sealed class BlockedMethodType;

public sealed class BlockedMethodReturnType;

public sealed class BlockedFieldType;

public sealed class BlockedPropertyType;

public sealed class BlockedLocalType;

public sealed class BlockedNewType;

public sealed class BlockedGenericInvocationType;

public sealed class BlockedGenericArgumentType;

public class BlockedInheritanceType;

public interface IBlockedInterfaceImplementationType;

public sealed class BlockedAttributeType : Attribute;

public sealed class BlockedStaticMemberType
{
    public static void Use() { }
}