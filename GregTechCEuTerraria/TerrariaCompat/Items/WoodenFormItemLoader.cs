#nullable enable
using GregTechCEuTerraria.TerrariaCompat.Items.Registry;
using Terraria.ModLoader;

namespace GregTechCEuTerraria.TerrariaCompat.Items;

public static class WoodenFormItemLoader
{
	public static void Register(Mod mod)
	{
		Add(mod, "empty_wooden_form");
		Add(mod, "brick_wooden_form");
	}

	private static void Add(Mod mod, string id)
	{
		if (mod.TryFind<ModItem>(id, out _)) return;
		string label = RegistryDump.TryGet(id, out var e) ? e.Name : id;
		int rarity = RegistryDump.TryGet(id, out e) ? e.Rarity : 0;
		mod.AddContent(new WoodenFormItem(id, label, rarity));
	}
}
