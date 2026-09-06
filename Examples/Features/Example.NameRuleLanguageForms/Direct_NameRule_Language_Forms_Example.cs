// ReSharper disable All - Justification: Example File

using System.Reflection;

[assembly: AssemblyMetadata("AnaalIJzerSettings", """
<ArchitecturalLevels>
  <Layer name="Application">
    <Class endsWith="Service" />
    <NameRules>
      <RequireMatchingNames description="Id values keep their meaning at every direct language form.">
        <Name endsWith="Id" />
      </RequireMatchingNames>
    </NameRules>
  </Layer>
</ArchitecturalLevels>
""")]

namespace Example.NameRuleLanguageForms;

public sealed class DirectFormsService
{
    // ARCH008: compound assignment still moves animalId into fruitId.
    public void AddToFruitId(int animalId)
    {
        var fruitId = 0;
        fruitId += animalId;
    }

    // ARCH008 twice: deconstruction compares corresponding source and target components.
    public void SwapIds(int fruitId, int animalId)
    {
        (fruitId, animalId) = (animalId, fruitId);
    }

    // ARCH008: conversions and conditional branches keep their source meaning.
    public int ChooseFruitId(bool chooseAnimal, int fruitId, int animalId)
    {
        return chooseAnimal ? (int)animalId : fruitId;
    }

    // ARCH008: expression-bodied returns have the method-return site.
    public int GetFruitId(int animalId) => animalId;

    // ARCH008 twice: named arguments follow their resolved parameter names.
    public void SendSwappedIds(int fruitId, int animalId)
    {
        Save(animalId: fruitId, fruitId: animalId);
    }

    private static void Save(int fruitId, int animalId = 0)
    {
    }
}
