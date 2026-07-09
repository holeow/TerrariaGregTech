#nullable enable
using GregTechCEuTerraria.Api.Machine.Trait;
using GregTechCEuTerraria.Api.Recipe;

namespace GregTechCEuTerraria.TerrariaCompat.Recipes;

public sealed class NativeCraftingCondition : RecipeCondition
{
	private readonly string _text;
	public NativeCraftingCondition(string text) => _text = text;

	public override bool Test(RecipeLogic logic) => true;
	public override string GetTooltips() => _text;
	public override string GetTypeName() => "native_crafting";
}
