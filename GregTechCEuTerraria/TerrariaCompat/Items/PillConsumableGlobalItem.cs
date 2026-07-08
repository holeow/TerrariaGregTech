#nullable enable
using System.Collections.Generic;
using GregTechCEuTerraria.TerrariaCompat.Items.Registry;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace GregTechCEuTerraria.TerrariaCompat.Items;

public sealed class PillConsumableGlobalItem : GlobalItem
{
	private const string Paracetamol = "paracetamol_pill";
	private const string RadAway     = "rad_away_pill";

	private const int FireResistTicks = 60 * 60 * 3;
	private static readonly int[] FeverDebuffs =
	{
		BuffID.OnFire, BuffID.OnFire3, BuffID.Burning, BuffID.Frostburn, BuffID.Chilled, BuffID.Frozen,
	};

	private static bool IsPill(Item item, out string name)
	{
		name = "";
		if (item.ModItem is not RegistryItem) return false;
		name = item.ModItem.Name;
		return name is Paracetamol or RadAway;
	}

	private static bool HasRemovableDebuff(Player player)
	{
		for (int i = 0; i < Player.MaxBuffs; i++)
		{
			int type = player.buffType[i];
			if (type > 0 && type != BuffID.PotionSickness && player.buffTime[i] > 0 && Main.debuff[type])
				return true;
		}
		return false;
	}

	public override void SetDefaults(Item item)
	{
		if (!IsPill(item, out var name)) return;

		item.consumable = true;
		item.useStyle = ItemUseStyleID.EatFood;
		item.useAnimation = 17;
		item.useTime = 17;
		item.UseSound = SoundID.Item2;
		item.potion = true;
	}

	public override bool CanUseItem(Item item, Player player)
	{
		if (IsPill(item, out var name) && name == RadAway)
			return HasRemovableDebuff(player);
		return base.CanUseItem(item, player);
	}

	public override bool? UseItem(Item item, Player player)
	{
		if (!IsPill(item, out var name)) return null;
		if (player.whoAmI != Main.myPlayer) return true;

		if (name == RadAway)
		{
			for (int i = 0; i < Player.MaxBuffs; i++)
			{
				int type = player.buffType[i];
				if (type <= 0 || type == BuffID.PotionSickness || player.buffTime[i] <= 0 || !Main.debuff[type]) continue;
				player.DelBuff(i);
				i--;
			}
			return true;
		}

		if (name == Paracetamol)
		{
			foreach (int type in FeverDebuffs)
				player.ClearBuff(type);
			player.AddBuff(BuffID.ObsidianSkin, FireResistTicks);
			return true;
		}

		return null;
	}

	public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
	{
		if (!IsPill(item, out var name)) return;
		if (name == RadAway)
			tooltips.Add(new TooltipLine(Mod, "RadAwayEffect", "Removes all negative effects"));
		else if (name == Paracetamol)
			tooltips.Add(new TooltipLine(Mod, "ParacetamolEffect", "Clears fire and cold effects, granting fire resistance"));
	}
}
