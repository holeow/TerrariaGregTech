#nullable enable
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace GregTechCEuTerraria.TerrariaCompat.NPCs.EBFChan;

// Spawns on world load (similar to MagicStorage guide)
public static class EBFChanSpawner
{
	public static bool TrySpawnHomeless(int worldX, int worldY)
	{
		if (Main.netMode == NetmodeID.MultiplayerClient)
			return false;

		int type = ModContent.NPCType<EBFChanNPC>();
		if (NPC.AnyNPCs(type))
			return false;

		int who = NPC.NewNPC(new EntitySource_SpawnNPC(), worldX, worldY, type, 1);
		if (who < 0 || who >= Main.maxNPCs)
			return false;

		NPC npc = Main.npc[who];
		npc.homeless = true;
		npc.netUpdate = true;
		WorldGen.QuickFindHome(who);
		return true;
	}

	public static bool TrySpawnAtWorldSpawn()
		=> TrySpawnHomeless(Main.spawnTileX * 16, Main.spawnTileY * 16);
}
