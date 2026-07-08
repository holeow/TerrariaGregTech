#nullable enable
using GregTechCEuTerraria.Api.Recipe.Content;
using GregTechCEuTerraria.Api.Recipe.Ingredient;

namespace GregTechCEuTerraria.Api.Capability.Recipe;

public sealed class ItemRecipeCapability : RecipeCapability<Ingredient>
{
	public static readonly ItemRecipeCapability CAP = new();

	private ItemRecipeCapability() : base("item") { }

	public override Ingredient CopyInner(Ingredient content) => SizedIngredient.Copy(content);

	public override Ingredient CopyWithModifier(Ingredient content, ContentModifier modifier)
	{
		if (content is SizedIngredient sized)
			return SizedIngredient.Create(sized.Inner, modifier.Apply(sized.Amount));
		if (content is IntProviderIngredient provider)
		{
			int mean = (provider.CountProvider.GetMinValue() + provider.CountProvider.GetMaxValue()) / 2;
			return SizedIngredient.Create(provider.Inner, modifier.Apply(System.Math.Max(1, mean)));
		}
		return SizedIngredient.Create(content, modifier.Apply(1));
	}
}
