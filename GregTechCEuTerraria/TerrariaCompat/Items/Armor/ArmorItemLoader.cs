#nullable enable
using System.Collections.Generic;
using Terraria.ModLoader;

namespace GregTechCEuTerraria.TerrariaCompat.Items.Armor;

public static class ArmorItemLoader
{
	private static readonly Dictionary<string, int> _byUpstreamId = new();
	public static IReadOnlyDictionary<string, int> ByUpstreamId => _byUpstreamId;

	public static bool TryGet(string upstreamId, out int itemType) =>
		_byUpstreamId.TryGetValue(upstreamId, out itemType);

	public static void Register(Mod mod)
	{
		_byUpstreamId.Clear();

		Terraria.Localization.Language.GetOrRegister("Mods.GregTechCEuTerraria.ArmorSet.Nano",
			() => "chance to dodge attacks, +10% movement speed, +10% damage");
		Terraria.Localization.Language.GetOrRegister("Mods.GregTechCEuTerraria.ArmorSet.Quark",
			() => "+15% damage");

		foreach (var spec in ArmorCatalog.All)
		{
			var item = new GTArmorItem(spec);
			mod.AddContent(item);
			_byUpstreamId["gtceu:" + spec.Id] = item.Type;

			var equipType = spec.Piece switch
			{
				ArmorPiece.Chest => EquipType.Body,
				ArmorPiece.Legs  => EquipType.Legs,
				_                => EquipType.Head,
			};
			EquipLoader.AddEquipTexture(mod, spec.EquipPath, equipType, item, spec.Id);
		}
		mod.Logger.Info($"ArmorItemLoader: registered {ArmorCatalog.All.Count} power-armor pieces.");
	}

	public static void Unload() => _byUpstreamId.Clear();
}
