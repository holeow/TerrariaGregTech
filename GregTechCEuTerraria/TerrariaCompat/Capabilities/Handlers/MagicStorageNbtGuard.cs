#nullable enable
using GregTechCEuTerraria.TerrariaCompat.Items;
using GregTechCEuTerraria.TerrariaCompat.Items.Fluids;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Chat;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace GregTechCEuTerraria.TerrariaCompat.Capabilities.Handlers;

public static class MagicStorageNbtGuard
{
	private const string Message =
		"Magic Storage has some NBT-holding items (non-empty drums, non-empty super tanks, non-empty fluid cells). " +
		"This can lead to item duping because Magic Storage doesn't handle items with custom data properly. " +
		"Please don't store those kinds of items in Magic Storage, use chests or crates for now";

	private static bool _warned;

	public static bool HasWarned => _warned;

	public static void Reset() => _warned = false;

	public static bool HoldsCustomData(Item? item)
	{
		if (item is null || item.IsAir) return false;
		if (item.ModItem is FluidCellItem cell && !cell.GetFluidStack().IsEmpty) return true;
		if (item.TryGetGlobalItem<MachinePortableData>(out var data) && data.Data is { Count: > 0 }) return true;
		return false;
	}

	public static void Warn()
	{
		if (_warned) return;
		_warned = true;

		ModContent.GetInstance<GregTechCEuTerraria>().Logger.Error(Message);

		var color = new Color(255, 120, 120);
		if (Main.netMode == NetmodeID.Server)
			ChatHelper.BroadcastChatMessage(NetworkText.FromLiteral(Message), color);
		else if (Main.netMode == NetmodeID.SinglePlayer)
			Main.NewText(Message, color.R, color.G, color.B);
	}
}
