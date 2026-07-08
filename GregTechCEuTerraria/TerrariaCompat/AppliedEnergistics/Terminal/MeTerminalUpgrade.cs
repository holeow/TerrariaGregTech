#nullable enable
using System.Collections.Generic;

namespace GregTechCEuTerraria.TerrariaCompat.AppliedEnergistics.Terminal;

public sealed class MeTerminalUpgrade
{
	public string Id { get; }
	public string CardItemName { get; }
	public string DisplayName { get; }
	public int CardItemType { get; internal set; } = -1;

	public MeTerminalUpgrade(string id, string cardItemName, string displayName)
	{
		Id = id;
		CardItemName = cardItemName;
		DisplayName = displayName;
	}
}

public static class MeTerminalUpgrades
{
	private static readonly List<MeTerminalUpgrade> _all = new();
	private static readonly Dictionary<string, MeTerminalUpgrade> _byId = new();
	private static readonly Dictionary<int, MeTerminalUpgrade> _byItemType = new();

	public static IReadOnlyList<MeTerminalUpgrade> All => _all;

	public static void Clear()
	{
		_all.Clear();
		_byId.Clear();
		_byItemType.Clear();
	}

	public static void Register(MeTerminalUpgrade upgrade)
	{
		_all.Add(upgrade);
		_byId[upgrade.Id] = upgrade;
	}

	public static void BindItemType(string id, int itemType)
	{
		if (!_byId.TryGetValue(id, out var upgrade)) return;
		upgrade.CardItemType = itemType;
		_byItemType[itemType] = upgrade;
	}

	public static MeTerminalUpgrade? ById(string id) =>
		_byId.TryGetValue(id, out var u) ? u : null;

	public static MeTerminalUpgrade? ByItemType(int itemType) =>
		_byItemType.TryGetValue(itemType, out var u) ? u : null;
}
