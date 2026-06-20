#nullable enable
using GregTechCEuTerraria.Api.Machine.Trait;
using GregTechCEuTerraria.Api.Recipe;

namespace GregTechCEuTerraria.TerrariaCompat.Recipe.Condition;

public sealed class MortarGrindCondition : RecipeCondition
{
	public string Label { get; }

	public MortarGrindCondition() : this("") { }
	public MortarGrindCondition(string label) { Label = label; }

	public override bool Test(RecipeLogic logic) => true;
	public override string GetTooltips() => Label;
	public override string GetTypeName() => "terraria:mortar_grind";
}
