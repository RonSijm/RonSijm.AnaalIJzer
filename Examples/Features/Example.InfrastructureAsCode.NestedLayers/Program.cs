// ReSharper disable All - Justification: Example File

using RonSijm.IJzer.IaC;

var ordering = new Layer("Ordering")
{
    Description = "The ordering boundary owns order workflows and storage.",
    Elements =
    {
        new NamespaceMatcher
        {
            StartsWith = "ExampleCompany.Ordering",
            Description = "Every nested ordering role remains inside the ordering namespace."
        },
        new Layer("Application")
        {
            Description = "Coordinates ordering use cases.",
            Elements = { new NamespaceMatcher { StartsWith = "ExampleCompany.Ordering.Application" } }
        },
        new Layer("Repository")
        {
            Description = "Stores orders for the ordering boundary.",
            Elements = { new NamespaceMatcher { StartsWith = "ExampleCompany.Ordering.Repository" } }
        },
        new AllowedDependency("Application", "Repository")
        {
            Description = "Ordering use cases may use their own repositories."
        },
        new AllowedDependency("Application", "/Billing/Contracts")
        {
            Description = "Ordering may leave its boundary through Billing contracts."
        }
    }
};

var billing = new Layer("Billing")
{
    Description = "The billing boundary exposes contracts without exposing its implementation.",
    Elements =
    {
        new NamespaceMatcher { StartsWith = "ExampleCompany.Billing" },
        new Layer("Contracts")
        {
            Description = "The public entry point into Billing.",
            Elements = { new NamespaceMatcher { StartsWith = "ExampleCompany.Billing.Contracts" } }
        },
        new Layer("Implementation")
        {
            Description = "Billing details that stay behind its contracts.",
            Elements = { new NamespaceMatcher { StartsWith = "ExampleCompany.Billing.Implementation" } }
        },
        new AllowedDependency("/Ordering/Application", "Contracts")
        {
            Description = "Ordering may enter Billing through its contracts."
        },
        new AllowedDependency("Implementation", "Contracts")
        {
            Description = "Billing implementations may implement their own contracts.",
            AllowedSites = { DependencySite.InterfaceImplementation }
        }
    }
};

var configuration = new ArchitectureConfiguration
{
    Description = "Nested boundaries require both the outer relationship and the relevant inner gates.",
    Elements =
    {
        ordering,
        billing,
        new AllowedDependency("Ordering", "Billing")
        {
            Description = "Ordering and Billing may communicate when their inner gates also permit it."
        }
    }
};

var outputPath = args.FirstOrDefault() ?? Path.Combine(AppContext.BaseDirectory, "Architecture.anl");
configuration.Save(outputPath, new ArchitectureConfigurationExportOptions
{
    SchemaLocation = "../../../src/Main/RonSijm.IJzer/Scheme/IJzer.xsd"
});

Console.WriteLine($"Generated {Path.GetFullPath(outputPath)}");
