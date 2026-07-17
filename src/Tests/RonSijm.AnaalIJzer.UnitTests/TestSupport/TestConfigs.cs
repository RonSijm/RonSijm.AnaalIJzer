namespace RonSijm.AnaalIJzer.UnitTests.TestSupport;

internal static class TestConfigs
{
	/// Controller -> Manager -> Repository, no other edges.
	public const string DefaultConfig = """
	                                    <ArchitecturalLevels>
	                                        <Layer name="Controller">
	                                            <Class endsWith="Controller" />
	                                        </Layer>
	                                        <Layer name="Manager">
	                                            <Class endsWith="Manager" />
	                                        </Layer>
	                                        <Layer name="Repository">
	                                            <Class endsWith="Repository" />
	                                        </Layer>
	                                        <AllowedDependency from="Controller" to="Manager" />
	                                        <AllowedDependency from="Manager"    to="Repository" />
	                                    </ArchitecturalLevels>
	                                    """;

	public const string RequireRecognizedDependenciesConfig = """
	                                   <ArchitecturalLevels requireRecognizedDependencies="Constructor">
	                                       <Layer name="Manager">
	                                           <Class endsWith="Manager" />
	                                       </Layer>
	                                       <Layer name="Repository">
	                                           <Class endsWith="Repository" />
	                                       </Layer>
	                                       <AllowedDependency from="Manager" to="Repository" />
	                                   </ArchitecturalLevels>
	                                   """;
}
