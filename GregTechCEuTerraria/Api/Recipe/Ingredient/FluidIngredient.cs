#nullable enable
using System.Collections.Generic;
using GregTechCEuTerraria.Api.Fluids;
using GregTechCEuTerraria.Api.Fluids.Attribute;

namespace GregTechCEuTerraria.Api.Recipe.Ingredient;

public class FluidIngredient : Ingredient
{
	public FluidType? ExactType { get; }
	public string? TagName { get; }
	public FluidAttribute? Attribute { get; }
	public int Amount { get; set; }
	private readonly IReadOnlyList<FluidType> _matchingFluids;

	public FluidIngredient(FluidType exact, int amount)
	{
		ExactType = exact;
		Amount = amount;
		_matchingFluids = new[] { exact };
	}

	public FluidIngredient(string tagName, IReadOnlyList<FluidType> resolvedFluids, int amount)
	{
		TagName = tagName;
		Amount = amount;
		_matchingFluids = resolvedFluids;
	}

	public FluidIngredient(FluidAttribute attribute, IReadOnlyList<FluidType> resolvedFluids, int amount)
	{
		Attribute = attribute;
		Amount = amount;
		_matchingFluids = resolvedFluids;
	}

	public bool TestFluid(FluidType? fluid)
	{
		if (fluid is null) return false;
		if (ExactType is not null) return ReferenceEquals(fluid, ExactType) || fluid.Id == ExactType.Id;
		if (Attribute is not null) return fluid.HasAttribute(Attribute);
		foreach (var f in _matchingFluids)
			if (f.Id == fluid.Id) return true;
		return false;
	}

	public bool TestStack(FluidStack stack) =>
		!stack.IsEmpty && TestFluid(stack.Type) && stack.Amount >= Amount;

	public IReadOnlyList<FluidType> GetFluids() => _matchingFluids;

	public virtual FluidIngredient Copy()
	{
		if (ExactType is not null) return new FluidIngredient(ExactType, Amount);
		if (Attribute is not null) return new FluidIngredient(Attribute, _matchingFluids, Amount);
		return new FluidIngredient(TagName ?? "", _matchingFluids, Amount);
	}

	public FluidStack[] GetStacks()
	{
		var result = new FluidStack[_matchingFluids.Count];
		for (int i = 0; i < _matchingFluids.Count; i++)
			result[i] = new FluidStack(_matchingFluids[i], Amount);
		return result;
	}

	public override bool Test(Terraria.Item item) => false;
	public override IReadOnlyList<Terraria.Item> GetItems() => System.Array.Empty<Terraria.Item>();

	public override bool IsEmpty => _matchingFluids.Count == 0 || Amount <= 0;

	public override string GetTypeName() => "gtceu:fluid";

	public override string ToString() =>
		ExactType is not null ? $"FluidIngredient({ExactType.Id} x {Amount}mB)" :
		TagName  is not null ? $"FluidIngredient({TagName} x {Amount}mB)" :
		Attribute is not null ? $"FluidIngredient(@{Attribute.Id} x {Amount}mB)" :
		"FluidIngredient(EMPTY)";
}
