// ReSharper disable All - Justification: Example File
// Shared roles used by every dependency-site example in this project.

namespace Example.Arch_DEP_001.NonConstructorInjection;

public interface IWaiter { }

public interface IChef { }

public class DirectChef : IChef { }