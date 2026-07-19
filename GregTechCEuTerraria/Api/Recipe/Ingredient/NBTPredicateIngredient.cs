#nullable enable
using System;
using System.Collections.Generic;
using GregTechCEuTerraria.Api.Recipe.Ingredient.Nbtpredicate;
using Terraria;
using Terraria.ModLoader.IO;

namespace GregTechCEuTerraria.Api.Recipe.Ingredient;

public sealed class NBTPredicateIngredient : Ingredient
{
	public static readonly NBTPredicate ALWAYS_TRUE = TrueNBTPredicate.INSTANCE;

	public static Func<string, string, int>? ResolveItemTypeFromNbt;

	public static Action<Item, string>? ApplyOutputNbt;

	public int ItemType { get; }
	public NBTPredicate Predicate { get; }
	public string UpstreamId { get; }

	public string? OutputNbt { get; }

	private IReadOnlyList<Item>? _exampleList;

	public NBTPredicateIngredient(int itemType, NBTPredicate predicate, string upstreamId = "", string? outputNbt = null)
	{
		ItemType = itemType;
		Predicate = predicate;
		UpstreamId = upstreamId;
		OutputNbt = outputNbt;
	}

	public static NBTPredicateIngredient Of(int itemType, NBTPredicate predicate, string upstreamId = "", string? outputNbt = null) =>
		new(itemType, predicate, upstreamId, outputNbt);

	public static NBTPredicateIngredient Of(int itemType, string upstreamId = "") =>
		Of(itemType, ALWAYS_TRUE, upstreamId);

	public override bool Test(Item item)
	{
		if (item is null) return false;
		if (item.type != ItemType) return false;
		return Predicate.Test(null);
	}

	public override IReadOnlyList<Item> GetItems()
	{
		if (_exampleList is null)
		{
			var ex = new Item();
			ex.SetDefaults(ItemType);
			if (!string.IsNullOrEmpty(OutputNbt)) ApplyOutputNbt?.Invoke(ex, OutputNbt!);
			_exampleList = new[] { ex };
		}
		return _exampleList;
	}

	public override bool IsEmpty => ItemType == 0;

	public override string GetTypeName() => "forge:nbt";

	public override string ToString() => $"NBTPredicate({(UpstreamId.Length > 0 ? UpstreamId : ItemType.ToString())}, {Predicate.GetTypeName()})";
}
