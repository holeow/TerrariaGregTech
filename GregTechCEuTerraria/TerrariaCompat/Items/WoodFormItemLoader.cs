#nullable enable
using GregTechCEuTerraria.TerrariaCompat.Items.Registry;
using Terraria.ModLoader;

namespace GregTechCEuTerraria.TerrariaCompat.Items;

public static class WoodFormItemLoader
{
	public static void Register(Mod mod)
	{
		Add(mod, "rubber_log", isLog: true, "block/rubber_log");
		Add(mod, "rubber_planks", isLog: false, "block/rubber_planks");
		Add(mod, "treated_wood_planks", isLog: false, "block/treated_wood_planks");
		Add(mod, "rubber_wood", isLog: true, "block/rubber_log");
	}

	private static void Add(Mod mod, string id, bool isLog, string woodColorTexture)
	{
		if (mod.TryFind<ModItem>(id, out _)) return;
		string label = RegistryDump.TryGet(id, out var e) ? e.Name : id;
		int rarity = RegistryDump.TryGet(id, out e) ? e.Rarity : 0;
		mod.AddContent(new WoodFormItem(id, label, rarity, isLog, woodColorTexture));
	}
}
