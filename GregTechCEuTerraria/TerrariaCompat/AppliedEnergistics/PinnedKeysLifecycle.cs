#nullable enable
using GregTechCEuTerraria.AppliedEnergistics.Client.Gui.Me.Common;
using GregTechCEuTerraria.TerrariaCompat.AppliedEnergistics.Crafting;
using Terraria;
using Terraria.ModLoader;

namespace GregTechCEuTerraria.TerrariaCompat.AppliedEnergistics;

public sealed class PinnedKeysLifecycle : ModSystem
{
	public override void OnWorldLoad()
	{
		if (Main.dedServ) return;
		PinnedKeys.ClearPinnedKeys();
		PendingCraftingJobs.ClearPendingJobs();
	}

	public override void PostUpdateEverything()
	{
		if (Main.dedServ) return;
		if (!Main.playerInventory) PinnedKeys.Prune();
	}
}
