#nullable enable
using GregTechCEuTerraria.TerrariaCompat.Recipe.Condition;
using GregTechCEuTerraria.Api.Machine.Trait;
using GregTechCEuTerraria.Api.Recipe;
using GregTechCEuTerraria.TerrariaCompat.Machine;

namespace GregTechCEuTerraria.Common.Recipe.Condition;

public sealed class DimensionCondition : RecipeCondition
{
	public string DimensionId { get; }

	public DimensionCondition() : this("") { }
	public DimensionCondition(string dimensionId) { DimensionId = dimensionId; }

	public override bool Test(RecipeLogic logic)
	{
		if (logic.GetRLMachine() is not MetaMachine mte) return true;
		int y = mte.Position.Y;
		return DimensionId switch
		{
			"minecraft:overworld"  => LocationZone.IsOverworld(y),
			"minecraft:the_nether" => LocationZone.IsUnderworld(y),
			"minecraft:the_end"    => LocationZone.IsSpace(y),
			_ => true,
		};
	}

	public override string GetTooltips() => DimensionId switch
	{
		"minecraft:overworld"  => "Requires zone: Surface",
		"minecraft:the_nether" => "Requires zone: The Underworld",
		"minecraft:the_end"    => "Requires zone: Space",
		_ => "",
	};
	public override string GetTypeName() => "gtceu:dimension";
}
