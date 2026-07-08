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
		foreach (var te in TileEntity.ByID.Values)
		{
			if (te is not TEStorageHeart heart) continue;
			foreach (var item in heart.GetStoredItems())
			{
				if (!MagicStorageNbtGuard.HoldsCustomData(item)) continue;
				MagicStorageNbtGuard.Warn();
				return;
			}
		}
	}
}
