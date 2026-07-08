#nullable enable
using GregTechCEuTerraria.Api.Recipe.Content;
using GregTechCEuTerraria.Api.Recipe.Ingredient;

namespace GregTechCEuTerraria.Api.Capability.Recipe;

public sealed class FluidRecipeCapability : RecipeCapability<FluidIngredient>
{
	public static readonly FluidRecipeCapability CAP = new();

	private FluidRecipeCapability() : base("fluid") { }

	public override FluidIngredient CopyInner(FluidIngredient content) => content.Copy();

	public override FluidIngredient CopyWithModifier(FluidIngredient content, ContentModifier modifier)
	{
		if (content.IsEmpty) return CopyInner(content);
		if (content is IntProviderFluidIngredient provider)
		{
			int mean = (provider.CountProvider.GetMinValue() + provider.CountProvider.GetMaxValue()) / 2;
			return BuildCopyWithAmount(provider.Inner, modifier.Apply(System.Math.Max(1, mean)));
		}
		return BuildCopyWithAmount(content, modifier.Apply(content.Amount));
	}

	private static FluidIngredient BuildCopyWithAmount(FluidIngredient src, int newAmount)
	{
		if (src.ExactType is not null)
			return new FluidIngredient(src.ExactType, newAmount);
		if (src.TagName is not null)
			return new FluidIngredient(src.TagName, src.GetFluids(), newAmount);
		if (src.Attribute is not null)
			return new FluidIngredient(src.Attribute, src.GetFluids(), newAmount);
		return src;
	}
}
