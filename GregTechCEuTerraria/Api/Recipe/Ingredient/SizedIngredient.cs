#nullable enable
using System.Collections.Generic;
using Terraria;

namespace GregTechCEuTerraria.Api.Recipe.Ingredient;

public class SizedIngredient : Ingredient
{
	public Ingredient Inner { get; }

	private int _amount;
	public int Amount
	{
		get => _amount;
		set { _amount = value; _changed = true; }
	}

	private Item[]? _itemStacks;
	private bool _changed = true;

	public SizedIngredient(Ingredient inner, int amount)
	{
		Inner = inner;
		_amount = amount;
	}

	public static SizedIngredient Create(Ingredient inner, int amount) => new(inner, amount);
	public static SizedIngredient Create(Ingredient inner) => new(inner, 1);

	public static Ingredient Copy(Ingredient ingredient)
	{
		if (ingredient is SizedIngredient sized)
		{
			if (sized.Inner is IntProviderIngredient) return Copy(sized.Inner);
			return Create(sized.Inner, sized.Amount);
		}
		if (ingredient is IntCircuitIngredient) return ingredient;
		if (ingredient is IntProviderIngredient ipi)
		{
			var copied = IntProviderIngredient.Of(ipi.Inner, ipi.CountProvider);
			if (ipi.SampledCount != -1) copied.SampledCount = ipi.SampledCount;
			return copied;
		}
		return ingredient;
	}

	public override bool Test(Item item) => Inner.Test(item);

	public override IReadOnlyList<Item> GetItems()
	{
		if (Inner is IntProviderIngredient ipi) return ipi.GetItems();
		if (_changed || _itemStacks is null)
		{
			var innerStacks = Inner.GetItems();
			_itemStacks = new Item[innerStacks.Count];
			for (int i = 0; i < _itemStacks.Length; i++)
			{
				var copy = innerStacks[i].Clone();
				copy.stack = _amount;
				_itemStacks[i] = copy;
			}
			_changed = false;
		}
		return _itemStacks;
	}

	public override bool IsEmpty => Inner.IsEmpty;
	public override string GetTypeName() => "gtceu:sized";
}
