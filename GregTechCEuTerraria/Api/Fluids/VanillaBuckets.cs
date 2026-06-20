#nullable enable
using System.Collections.Generic;
using Terraria.ID;

namespace GregTechCEuTerraria.Api.Fluids;

public static class VanillaBuckets
{
	public const int Amount = 1000;

	public const int EmptyBucket = ItemID.EmptyBucket;

	public readonly record struct Entry(int Item, string FluidId, bool Bottomless);

	private static readonly Entry[] _entries =
	{
		new(ItemID.WaterBucket,             "water",   false),
		new(ItemID.LavaBucket,              "lava",    false),
		new(ItemID.HoneyBucket,             "honey",   false),
		new(ItemID.BottomlessBucket,        "water",   true),
		new(ItemID.BottomlessLavaBucket,    "lava",    true),
		new(ItemID.BottomlessHoneyBucket,   "honey",   true),
		new(ItemID.BottomlessShimmerBucket, "shimmer", true),
	};

	private static readonly Dictionary<int, Entry> _byItem = BuildByItem();
	private static readonly Dictionary<string, int> _normalFillByFluid = BuildFill();

	private static Dictionary<int, Entry> BuildByItem()
	{
		var d = new Dictionary<int, Entry>(_entries.Length);
		foreach (var e in _entries) d[e.Item] = e;
		return d;
	}

	private static Dictionary<string, int> BuildFill()
	{
		var d = new Dictionary<string, int>();
		foreach (var e in _entries)
			if (!e.Bottomless) d[e.FluidId] = e.Item;
		return d;
	}

	public static bool TryGet(int itemType, out Entry entry) => _byItem.TryGetValue(itemType, out entry);

	public static bool IsBottomless(int itemType) =>
		_byItem.TryGetValue(itemType, out var e) && e.Bottomless;

	public static int DrainedItem(int itemType) =>
		_byItem.TryGetValue(itemType, out var e) && !e.Bottomless ? EmptyBucket : 0;

	public static int FillEmptyBucket(string fluidId) => _normalFillByFluid.GetValueOrDefault(fluidId);
}
