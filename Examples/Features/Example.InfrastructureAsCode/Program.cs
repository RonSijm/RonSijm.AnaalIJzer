// ReSharper disable All - Justification: Example File

using RonSijm.IJzer.IaC;

var configuration = new ArchitectureConfiguration
{
    Description = "A customer asks a waiter, and the waiter asks a chef.",
    Elements =
    {
        new Layer(nameof(Customer))
        {
            Elements = { new ClassMatcher { ExactName = nameof(Customer) } }
        },
        new Layer(nameof(Waiter))
        {
            Elements = { new ClassMatcher { ExactName = nameof(Waiter) } }
        },
        new Layer(nameof(Chef))
        {
            Elements = { new ClassMatcher { ExactName = nameof(Chef) } }
        },
        new AllowedDependency(nameof(Customer), nameof(Waiter))
        {
            Description = "A customer may ask a waiter for food."
        },
        new AllowedDependency(nameof(Waiter), nameof(Chef))
        {
            Description = "A waiter may pass an order to a chef."
        }
    }
};

var outputPath = args.FirstOrDefault()
                 ?? Path.Combine(AppContext.BaseDirectory, "Architecture.anl");
configuration.Save(outputPath, new ArchitectureConfigurationExportOptions
{
    SchemaLocation = "../../../src/Main/RonSijm.IJzer/Scheme/IJzer.xsd"
});

Console.WriteLine($"Generated {Path.GetFullPath(outputPath)}");

public sealed class Customer;
public sealed class Waiter;
public sealed class Chef;
