using RonSijm.AnaalIJzer.Outputs.Configuration;

namespace RonSijm.AnaalIJzer.Outputs.Tests.Configuration;

public sealed class ArchitectureConfigurationExplainerTests
{
	[Fact]
	public void Explainer_ExplainsVisibilityAllowAndBlockLists()
	{
		var path = Path.Combine(Path.GetTempPath(), "AnaalIJzer-" + Guid.NewGuid().ToString("N") + ".anl");
		try
		{
			File.WriteAllText(
				path,
				"""
				<ArchitecturalLevels>
				  <Layer name="QuerySurface">
				    <Class endsWith="Queryable" />
				    <VisibilityPolicy targets="Type" allowedAccessibilities="Internal, File" description="Keep query surfaces internal." />
				    <VisibilityPolicy targets="Property" blockedAccessibilities="Public" />
				  </Layer>
				</ArchitecturalLevels>
				""");

			var markdown = ArchitectureConfigurationExplainer.GenerateMarkdown(path);

			markdown.Should().Contain("Visibility policy for `Type` allows only `Internal, File`");
			markdown.Should().Contain("Visibility policy for `Property` blocks `Public`");
			markdown.Should().Contain("Keep query surfaces internal");
		}
		finally
		{
			if (File.Exists(path))
			{
				File.Delete(path);
			}
		}
	}

	[Fact]
	public void Explainer_SeparatesApiExposureFromInternalUse()
	{
		var path = Path.Combine(Path.GetTempPath(), "AnaalIJzer-" + Guid.NewGuid().ToString("N") + ".anl");
		try
		{
			File.WriteAllText(
				path,
				"""
				<ArchitecturalLevels>
				  <Layer name="Application">
				    <Class endsWith="Service" />
				    <ApiSurface requireRecognizedTypes="true" description="Contracts only.">
				      <TransitiveExposure maxDepth="4" description="Inspect public contracts." />
				      <AllowedLayer path="/Contracts" allowedSites="MethodReturn" />
				      <BlockedLayer path="/QuerySurface" />
				    </ApiSurface>
				  </Layer>
				  <Layer name="Contracts"><Class endsWith="Projection" /></Layer>
				  <Layer name="QuerySurface"><Class endsWith="Queryable" /></Layer>
				</ArchitecturalLevels>
				""");

			var markdown = ArchitectureConfigurationExplainer.GenerateMarkdown(path);

			markdown.Should().Contain("API surface policy controls externally visible signatures");
			markdown.Should().Contain("This does not control whether the layer may use a type internally");
			markdown.Should().Contain("Unclassified exposed types are rejected");
			markdown.Should().Contain("allows exposure of `/Contracts` (allowedSites=\"MethodReturn\")");
			markdown.Should().Contain("blocks exposure of `/QuerySurface`");
			markdown.Should().Contain("maximum depth of `4`");
			markdown.Should().Contain("bounded, cached, cycle-safe");
			markdown.Should().Contain("Description: Inspect public contracts.");
		}
		finally
		{
			if (File.Exists(path))
			{
				File.Delete(path);
			}
		}
	}

	[Fact]
	public void Explainer_ExplainsBothNameRuleKinds()
	{
		var path = Path.Combine(Path.GetTempPath(), "AnaalIJzer-" + Guid.NewGuid().ToString("N") + ".anl");
		try
		{
			File.WriteAllText(
				path,
				"""
				<ArchitecturalLevels>
				  <Layer name="Application">
				    <Class endsWith="Service" />
				    <NameRules>
				      <RequireMatchingNames allowedSites="Local" valueTracking="IntraProcedural" />
				      <RequireDeclarationNameMatchesType allowedSites="Method">
				        <Type endsWith="Id" />
				      </RequireDeclarationNameMatchesType>
				    </NameRules>
				  </Layer>
				</ArchitecturalLevels>
				""");

			var markdown = ArchitectureConfigurationExplainer.GenerateMarkdown(path);

			markdown.Should().Contain("Require matching value names");
			markdown.Should().Contain("valueTracking=\"IntraProcedural\"");
			markdown.Should().Contain("Require declaration name to match its type");
			markdown.Should().Contain("Type endsWith=\"Id\"");
		}
		finally
		{
			if (File.Exists(path))
			{
				File.Delete(path);
			}
		}
	}

	[Fact]
	public void Explainer_ExplainsInheritancePolicy()
	{
		var path = Path.Combine(Path.GetTempPath(), "AnaalIJzer-" + Guid.NewGuid().ToString("N") + ".anl");
		try
		{
			File.WriteAllText(
				path,
				"""
				<ArchitecturalLevels>
				  <Layer name="PersistenceEntities">
				    <Namespace startsWith="Shop.Persistence" />
				    <InheritancePolicy
				      typeKinds="Class"
				      requiredBaseTypes="Entity"
				      requiredInterfaces="IAuditedEntity"
				      description="Persistence entities use the shared entity contract." />
				  </Layer>
				</ArchitecturalLevels>
				""");

			var markdown = ArchitectureConfigurationExplainer.GenerateMarkdown(path);

			markdown.Should().Contain("Inheritance policy requires `Class` declarations to inherit `Entity` and implement `IAuditedEntity`");
			markdown.Should().Contain("Persistence entities use the shared entity contract.");
		}
		finally
		{
			if (File.Exists(path))
			{
				File.Delete(path);
			}
		}
	}

	[Fact]
	public void Explainer_ExplainsGenericReturnValueMatchers()
	{
		var path = Path.Combine(Path.GetTempPath(), "AnaalIJzer-" + Guid.NewGuid().ToString("N") + ".anl");
		try
		{
			File.WriteAllText(
				path,
				"""
				<ArchitecturalLevels>
				  <Layer name="Kitchen">
				    <Class endsWith="Kitchen" />
				    <ReturnValuePolicy description="No sentinel meals.">
				      <Literal value="null" />
				      <Literal value="" />
				      <Invocation withAttribute="JetBrains.Annotations.CanBeNullAttribute" />
				    </ReturnValuePolicy>
				  </Layer>
				</ArchitecturalLevels>
				""");

			var markdown = ArchitectureConfigurationExplainer.GenerateMarkdown(path);

			markdown.Should().Contain("Return-value policy forbids configured direct returned expressions");
			markdown.Should().Contain("Forbids returned literal value=\"null\"");
			markdown.Should().Contain("Forbids returned invocation withAttribute=\"JetBrains.Annotations.CanBeNullAttribute\"");
			markdown.Should().Contain("No sentinel meals.");
		}
		finally
		{
			if (File.Exists(path))
			{
				File.Delete(path);
			}
		}
	}

	[Fact]
	public void Explainer_ExplainsForbiddenOperationPolicies()
	{
		var path = Path.Combine(Path.GetTempPath(), "AnaalIJzer-" + Guid.NewGuid().ToString("N") + ".anl");
		try
		{
			File.WriteAllText(
				path,
				"""
				<ArchitecturalLevels>
				  <Layer name="Kitchen">
				    <Class endsWith="Kitchen" />
				    <ForbiddenOperations description="Kitchens use a shared clock.">
				      <ForbiddenOperation allowedSites="StaticMember">
				        <OperationMatcher kind="PropertyRead" staticAccess="true">
				          <ContainingType exactFullName="System.DateTime" />
				          <Member exactName="UtcNow" memberKind="Property" />
				        </OperationMatcher>
				      </ForbiddenOperation>
				    </ForbiddenOperations>
				  </Layer>
				</ArchitecturalLevels>
				""");

			var markdown = ArchitectureConfigurationExplainer.GenerateMarkdown(path);

			markdown.Should().Contain("Forbidden-operation policy blocks selected resolved members");
			markdown.Should().Contain("Blocks a selected operation (allowedSites=\"StaticMember\")");
			markdown.Should().Contain("ContainingType exactFullName=\"System.DateTime\"");
			markdown.Should().Contain("Member exactName=\"UtcNow\" memberKind=\"Property\"");
			markdown.Should().Contain("Kitchens use a shared clock.");
		}
		finally
		{
			if (File.Exists(path))
			{
				File.Delete(path);
			}
		}
	}

	[Fact]
	public void Explainer_ExplainsBehavioralOperationPolicies()
	{
		var path = Path.Combine(Path.GetTempPath(), "AnaalIJzer-" + Guid.NewGuid().ToString("N") + ".anl");
		try
		{
			File.WriteAllText(
				path,
				"""
				<ArchitecturalLevels>
				  <Layer name="Kitchen">
				    <Class endsWith="Kitchen" />
				    <BehavioralOperations description="Validate before saving.">
				      <RequiredOperationBefore ordering="Dominance" allowedSites="Method">
				        <DeclarationMatcher>
				          <Member exactName="Submit" memberKind="Method" />
				        </DeclarationMatcher>
				        <OperationMatcher kind="Invocation">
				          <Member exactName="Validate" memberKind="Method" />
				        </OperationMatcher>
				        <BeforeOperation>
				          <OperationMatcher kind="Invocation">
				            <Member exactName="Save" memberKind="Method" />
				          </OperationMatcher>
				        </BeforeOperation>
				      </RequiredOperationBefore>
				    </BehavioralOperations>
				  </Layer>
				</ArchitecturalLevels>
				""");

			var markdown = ArchitectureConfigurationExplainer.GenerateMarkdown(path);

			markdown.Should().Contain("Behavioral-operation policies prove configured operation presence, ordering, or count facts");
			markdown.Should().Contain("Requires an operation before another operation (ordering=\"Dominance\", allowedSites=\"Method\")");
			markdown.Should().Contain("Applies to declarations selected by:");
			markdown.Should().Contain("Must happen before:");
			markdown.Should().Contain("Validate before saving.");
		}
		finally
		{
			if (File.Exists(path))
			{
				File.Delete(path);
			}
		}
	}

	[Fact]
	public void Explainer_ExplainsExplicitOperationContracts()
	{
		var path = Path.Combine(Path.GetTempPath(), "AnaalIJzer-" + Guid.NewGuid().ToString("N") + ".anl");
		try
		{
			File.WriteAllText(
				path,
				"""
				<ArchitecturalLevels>
				  <Operations description="Named restaurant work.">
				    <Operation name="PlacePizzaOrder" allowedOwnerLayers="Application" allowedEntryPointLayers="Controller" description="The waiter sends an order to the kitchen.">
				      <Owner>
				        <DeclarationMatcher>
				          <ContainingType endsWith="Kitchen" />
				          <Member exactName="PlacePizzaOrder" memberKind="Method" />
				        </DeclarationMatcher>
				      </Owner>
				      <Request><Class exactName="PlacePizzaOrderRequest" /></Request>
				      <EntryPoint>
				        <DeclarationMatcher><Member exactName="PlacePizzaOrder" memberKind="Method" /></DeclarationMatcher>
				      </EntryPoint>
				    </Operation>
				  </Operations>
				</ArchitecturalLevels>
				""");

			var markdown = ArchitectureConfigurationExplainer.GenerateMarkdown(path);

			markdown.Should().Contain("Explicit operation contracts connect selected owner methods");
			markdown.Should().Contain("Operation `PlacePizzaOrder`");
			markdown.Should().Contain("allowedOwnerLayers=\"Application\"");
			markdown.Should().Contain("Owner declaration:");
			markdown.Should().Contain("Request type: Class exactName=\"PlacePizzaOrderRequest\"");
		}
		finally
		{
			if (File.Exists(path))
			{
				File.Delete(path);
			}
		}
	}

	[Fact]
	public void Explainer_ExplainsAssemblyAttributePolicies()
	{
		var path = Path.Combine(Path.GetTempPath(), "AnaalIJzer-" + Guid.NewGuid().ToString("N") + ".anl");
		try
		{
			File.WriteAllText(
				path,
				"""
				<ArchitecturalLevels>
				  <AssemblyAttributePolicy description="Friend access stays reviewed.">
				    <Forbidden>
				      <Attribute exactFullName="System.Runtime.CompilerServices.InternalsVisibleToAttribute">
				        <Argument index="0" exactName="NotAllowedExample" />
				      </Attribute>
				    </Forbidden>
				  </AssemblyAttributePolicy>
				</ArchitecturalLevels>
				""");

			var markdown = ArchitectureConfigurationExplainer.GenerateMarkdown(path);

			markdown.Should().Contain("Assembly-attribute policy checks semantic attributes emitted for the compiled assembly");
			markdown.Should().Contain("blocks matching selected assembly attributes");
			markdown.Should().Contain("System.Runtime.CompilerServices.InternalsVisibleToAttribute");
			markdown.Should().Contain("argument #0");
			markdown.Should().Contain("NotAllowedExample");
		}
		finally
		{
			if (File.Exists(path))
			{
				File.Delete(path);
			}
		}
	}
}
