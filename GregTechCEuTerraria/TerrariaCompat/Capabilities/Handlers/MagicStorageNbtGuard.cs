#nullable enable
using GregTechCEuTerraria.TerrariaCompat.Items;
using GregTechCEuTerraria.TerrariaCompat.Items.Fluids;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Chat;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.Default;

namespace GregTechCEuTerraria.TerrariaCompat.Capabilities.Handlers;

public static class MagicStorageNbtGuard
{
	private const string Message =
		"Magic Storage has some NBT-holding items (non-empty drums, non-empty super tanks, non-empty fluid cells) " +
		"or items from missing mods (shown as \"Unloaded Item\"). " +
		"You won't be able to pipe/terminal those items because Magic Storage doesn't handle items with custom data properly. " +
		"Please don't store those kinds of items in Magic Storage, use chests or crates for now";

	private static bool _warned;
	private static uint _lastWarnTick;

	public static bool HasWarned => _warned;

	public static void Reset() { _warned = false; _lastWarnTick = 0; }

	public static string Describe(Item item)
	{
		if (item.ModItem is UnloadedItem unloaded)
			return $"{unloaded.ItemName} [{unloaded.ModName}]";
		return item.Name;
	}

	public static bool HoldsCustomData(Item? item)
	{
		if (item is null || item.IsAir) return false;
		if (item.ModItem is UnloadedItem) return true;
		if (item.ModItem is FluidCellItem cell && !cell.GetFluidStack().IsEmpty) return true;
		if (item.TryGetGlobalItem<MachinePortableData>(out var data) && data.Data is { Count: > 0 }) return true;
		return false;
	}

	public static void Warn(string source = "", Item? item = null)
	{
		if (_warned && Main.GameUpdateCount - _lastWarnTick < 120) return;
		_lastWarnTick = Main.GameUpdateCount;

		var logger = ModContent.GetInstance<GregTechCEuTerraria>().Logger;
		string detail = item is null || item.IsAir
			? source
			: $"{source} item='{Describe(item)}' type={item.type} mod={item.ModItem?.GetType().Name ?? "-"} stack={item.stack}";
		logger.Warn($"[MagicStorageNbtGuard] triggered by {detail} (netMode={Main.netMode}, tick={Main.GameUpdateCount})");

		if (!_warned)
			logger.Error(Message);
		_warned = true;

		string chat = item is null || item.IsAir
			? Message
			: $"{Message} (e.g. \"{Describe(item)}\" x{item.stack})";

		var color = new Color(255, 120, 120);
		if (Main.netMode == NetmodeID.Server)
			ChatHelper.BroadcastChatMessage(NetworkText.FromLiteral(chat), color);
		else if (Main.netMode == NetmodeID.SinglePlayer)
			Main.NewText(chat, color.R, color.G, color.B);
	}
}
