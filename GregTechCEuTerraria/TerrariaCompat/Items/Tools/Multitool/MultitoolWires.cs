#nullable enable
using System.Collections.Generic;
using GregTechCEuTerraria.TerrariaCompat.Items.Cables;
using GregTechCEuTerraria.TerrariaCompat.Pipelike.Cable;
using GregTechCEuTerraria.TerrariaCompat.Utils;
using Terraria;
using Terraria.ID;

namespace GregTechCEuTerraria.TerrariaCompat.Items.Tools.Multitool;

internal static class MultitoolWires
{
	internal static int CountUnits(Player p, string materialId, bool insulated)
	{
		int u = 0;
		for (int i = 0; i < 58; i++)
		{
			var it = p.inventory[i];
			if (it is null || it.IsAir) continue;
			if (it.ModItem is WireItem w && w.Insulated == insulated && w.MaterialId == materialId)
				u += w.WireSize * it.stack;
		}
		return u;
	}

	internal static CableCell? BuildCell(string materialId, byte width, bool insulated)
	{
		for (byte s = 1; s <= 16; s = (byte)(s << 1))
		{
			int? type = WireItemRegistry.Get(materialId, s, insulated);
			if (type is null) continue;
			if (ContentSamples.ItemsByType[type.Value].ModItem is WireItem w)
				return w.BuildCellWithSize(width);
		}
		return null;
	}

	internal static void SpendUnits(Player p, string materialId, bool insulated, int units)
	{
		if (units <= 0) return;

		var slots = new List<(int slot, byte size)>();
		for (int i = 0; i < 58; i++)
		{
			var it = p.inventory[i];
			if (it is null || it.IsAir) continue;
			if (it.ModItem is WireItem w && w.Insulated == insulated && w.MaterialId == materialId)
				slots.Add((i, w.WireSize));
		}
		slots.Sort((a, b) => a.size.CompareTo(b.size));

		int removed = 0;
		foreach (var (slot, size) in slots)
		{
			var it = p.inventory[slot];
			while (it.stack > 0 && removed < units)
			{
				it.stack--;
				removed += size;
			}
			if (it.stack <= 0) it.TurnToAir();
			if (removed >= units) break;
		}

		int overshoot = removed - units;
		if (overshoot > 0)
		{
			int? single = WireItemRegistry.Get(materialId, 1, insulated);
			if (single is not null)
				PlayerGive.Give(p, p.GetSource_ItemUse(p.HeldItem), single.Value, overshoot);
		}
	}
}
