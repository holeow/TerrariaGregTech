#nullable enable
using System;
using MagicStorage.Components;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace GregTechCEuTerraria.TerrariaCompat.Capabilities.Handlers;

public sealed class MagicStorageNbtScanSystem : ModSystem
{
	private const int GraceTicks = 180;

	private bool _scanned;
	private int _graceElapsed;

	public override void OnWorldLoad()
	{
		_scanned = false;
		_graceElapsed = 0;
		MagicStorageNbtGuard.Reset();
	}

	public override void OnWorldUnload() => MagicStorageNbtGuard.Reset();

	public override void PostUpdateWorld()
	{
		if (_scanned) return;
		if (!AnyActivePlayer()) return;
		if (_graceElapsed++ < GraceTicks) return;
		_scanned = true;

		if (!WorldCapability.MagicStoragePresent) return;
		try
		{
			ScanHearts();
		}
		catch (Exception e)
		{
			ModContent.GetInstance<GregTechCEuTerraria>().Logger.Error("[MagicStorageNbtScan] scan threw", e);
		}
	}

	private static bool AnyActivePlayer()
	{
		for (int i = 0; i < Main.maxPlayers; i++)
			if (Main.player[i] is { active: true }) return true;
		return false;
	}

	[JITWhenModsEnabled("MagicStorage")]
	private static void ScanHearts()
	{
		var logger = ModContent.GetInstance<GregTechCEuTerraria>().Logger;
		Item? first = null;
		Point16 firstHeart = default;
		int count = 0;

		foreach (var te in TileEntity.ByID.Values)
		{
			if (te is not TEStorageHeart heart) continue;
			foreach (var item in heart.GetStoredItems())
			{
				if (!MagicStorageNbtGuard.HoldsCustomData(item)) continue;
				count++;
				if (first is null) { first = item; firstHeart = heart.Position; }
				logger.Warn($"[MagicStorageNbtScan] bad item: '{MagicStorageNbtGuard.Describe(item)}' " +
					$"type={item.type} mod={item.ModItem?.GetType().Name ?? "-"} stack={item.stack} " +
					$"heart=({heart.Position.X},{heart.Position.Y})");
			}
		}

		if (first is null)
		{
			logger.Info("[MagicStorageNbtScan] no un-pipeable items found in Magic Storage");
			return;
		}

		logger.Warn($"[MagicStorageNbtScan] {count} un-pipeable item stack(s) in Magic Storage");
		MagicStorageNbtGuard.Warn($"ScanHearts heart=({firstHeart.X},{firstHeart.Y})", first);
	}
}
