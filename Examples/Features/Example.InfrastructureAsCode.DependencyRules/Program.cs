// ReSharper disable All - Justification: Example File

using RonSijm.IJzer.IaC;

var configuration = new ArchitectureConfiguration
{
    Description = "A customer orders through a waiter while pantry access stays with the chef.",
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
        new Layer(nameof(Pantry))
        {
            Elements = { new ClassMatcher { ExactName = nameof(Pantry) } }
        },
        new AllowedDependency(nameof(Customer), nameof(Waiter))
        {
            Description = "A customer may receive a waiter through a constructor or call one from a method.",
            AllowedSites = { DependencySite.Constructor, DependencySite.Method }
        },
        new AllowedDependency(nameof(Waiter), nameof(Chef))
        {
            Description = "A waiter may pass an order to a chef."
        },
        new AllowedDependency(nameof(Chef), nameof(Pantry))
        {
            Description = "A chef may use ingredients from the pantry."
        },
        new BlockedDependency(nameof(Customer), nameof(Pantry))
        {
            Description = "A customer may order ingredients, but may not rummage through the pantry."
        }
    }
};

var outputPath = args.FirstOrDefault() ?? Path.Combine(AppContext.BaseDirectory, "Architecture.anl");
configuration.Save(outputPath, new ArchitectureConfigurationExportOptions
{
    SchemaLocation = "../../../src/Main/RonSijm.IJzer/Scheme/IJzer.xsd"
});

Console.WriteLine($"Generated {Path.GetFullPath(outputPath)}");

public sealed class Customer;
public sealed class Waiter;
public sealed class Chef;
public sealed class Pantry;
