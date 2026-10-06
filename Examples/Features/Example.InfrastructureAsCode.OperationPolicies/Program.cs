// ReSharper disable All - Justification: Example File

using RonSijm.AnaalIJzer.IaC;

var clockReads = new ForbiddenOperation
{
    Description = "Direct system-clock reads hide a time dependency.",
    AllowedSites = { DependencySite.StaticMember },
    Matchers =
    {
        CreateClockPropertyMatcher("UtcNow"),
        CreateClockPropertyMatcher("Now"),
        CreateClockPropertyMatcher("Today")
    }
};

var configuration = new ArchitectureConfiguration
{
    Description = "Kitchens receive a clock dependency instead of reading the system clock directly.",
    Elements =
    {
        new Layer("Application")
        {
            Elements =
            {
                new ClassMatcher { EndsWith = "Kitchen" },
                new ForbiddenOperations
                {
                    Description = "Application code may not inspect the machine's clock.",
                    Rules = { clockReads }
                }
            }
        }
    }
};

var outputPath = args.FirstOrDefault() ?? Path.Combine(AppContext.BaseDirectory, "Architecture.anl");
configuration.Save(outputPath, new ArchitectureConfigurationExportOptions
{
    SchemaLocation = "../../../src/Main/RonSijm.AnaalIJzer/Scheme/AnaalIJzer.xsd"
});

Console.WriteLine($"Generated {Path.GetFullPath(outputPath)}");

static OperationMatcher CreateClockPropertyMatcher(string propertyName)
{
    var result = new OperationMatcher
    {
        StaticAccess = true,
        ContainingType = new OperationContainingTypeMatcher { ExactFullName = "System.DateTime" },
        Member = new OperationMemberMatcher { ExactName = propertyName }
    };
    result.Kinds.Add(SemanticOperationKind.PropertyRead);
    result.Member.MemberKinds.Add(SemanticOperationMemberKind.Property);

    return result;
}
