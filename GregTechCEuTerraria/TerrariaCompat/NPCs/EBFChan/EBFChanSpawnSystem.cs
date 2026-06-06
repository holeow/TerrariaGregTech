#nullable enable
using System.Collections.Generic;
using Terraria.IO;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.WorldBuilding;

namespace GregTechCEuTerraria.TerrariaCompat.NPCs.EBFChan;

// Spawns EBF-chan at world creation - the same shape as MagicStorage's Automaton
public class EBFChanSpawnSystem : ModSystem
{
	private static bool _everSpawned;

	public override void OnWorldLoad() => _everSpawned = false;
	public override void OnWorldUnload() => _everSpawned = false;

	public override void SaveWorldData(TagCompound tag)
	{
		if (_everSpawned) tag["ebfChanSpawned"] = true;
	}

	public override void LoadWorldData(TagCompound tag)
	{
		_everSpawned = tag.ContainsKey("ebfChanSpawned");
	}

	public override void ModifyWorldGenTasks(List<GenPass> tasks, ref double totalWeight)
	{
		int index = tasks.FindIndex(static pass => pass.Name == "Guide");
		if (index < 0)
			index = tasks.Count - 1;
		tasks.Insert(index + 1, new EBFChanGenPass("GregTech: EBF-chan", 0.016f));
	}

	// Existing worlds (created before this feature) never ran the gen pass
	public override void PostWorldLoad()
	{
		if (_everSpawned)
			return;
		EBFChanSpawner.TrySpawnAtWorldSpawn();
		_everSpawned = true;
	}

	internal static void MarkSpawned() => _everSpawned = true;

	private sealed class EBFChanGenPass : GenPass
	{
		public EBFChanGenPass(string name, float loadWeight) : base(name, loadWeight) { }

		protected override void ApplyPass(GenerationProgress progress, GameConfiguration configuration)
		{
			progress.Message = "Settling EBF-chan";
			EBFChanSpawner.TrySpawnAtWorldSpawn();
			MarkSpawned();
			progress.Set(1.0f);
		}
	}
}
