#nullable enable
using System;
using System.Collections.Generic;
using GregTechCEuTerraria.TerrariaCompat.Utils;
using Terraria;

namespace GregTechCEuTerraria.TerrariaCompat.Items.Tools.Multitool;

internal static class MultitoolBudget
{
	internal static int CountUnits(Player p, Func<Item, int> unitOf)
	{
		int u = 0;
		for (int i = 0; i < 58; i++)
		{
			var it = p.inventory[i];
			if (it is null || it.IsAir) continue;
			int pu = unitOf(it);
			if (pu > 0) u += pu * it.stack;
		}
		return u;
	}

	internal static void SpendUnits(Player p, Func<Item, int> unitOf, int units, int refundType, int refundUnit)
	{
		if (units <= 0) return;

		var slots = new List<(int slot, int u)>();
		for (int i = 0; i < 58; i++)
		{
			var it = p.inventory[i];
			if (it is null || it.IsAir) continue;
			int pu = unitOf(it);
			if (pu > 0) slots.Add((i, pu));
		}
		slots.Sort((a, b) => a.u.CompareTo(b.u));

		int removed = 0;
		foreach (var (slot, pu) in slots)
		{
			var it = p.inventory[slot];
			while (it.stack > 0 && removed < units)
			{
				it.stack--;
				removed += pu;
			}
			if (it.stack <= 0) it.TurnToAir();
			if (removed >= units) break;
		}

		int overshoot = removed - units;
		if (overshoot > 0 && refundType > 0 && refundUnit > 0)
		{
			int give = overshoot / refundUnit;
			if (give > 0)
				PlayerGive.Give(p, p.GetSource_ItemUse(p.HeldItem), refundType, give);
		}
	}
}
