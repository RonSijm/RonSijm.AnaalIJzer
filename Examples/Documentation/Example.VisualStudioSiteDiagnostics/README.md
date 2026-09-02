# Example.VisualStudioSiteDiagnostics

This is a clean Visual Studio companion demonstration rather than a failing diagnostic example. Open `All_Site_Diagnostics_Showcase.cs`, then turn on either `Show all site diagnostics` or `Show all layer information` in `AnaalIJzer > Editor`.

The single showcase class visibly exercises all supported dependency sites:

- `Constructor`, `Method`, `MethodReturn`, `Field`, and `Property`
- `Local`, `New`, `GenericInvocation`, and `GenericArgument`
- `Inheritance`, `InterfaceImplementation`, `Attribute`, and `StaticMember`

The inline configuration assigns the showcase class to `Showcase`, assigns every referenced helper type to `Ingredient`, and allows that one dependency. The project therefore builds without AnaalIJzer diagnostics while still giving every editor label a real resolved layer and dependency site to display.

It is the intended source for the all-site Visual Studio screenshot captures described in [`Examples/Assets/VisualStudio`](../../Assets/VisualStudio).
