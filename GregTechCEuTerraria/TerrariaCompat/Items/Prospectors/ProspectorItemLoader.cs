#nullable enable
using System;
using GregTechCEuTerraria.Common.Energy;
using GregTechCEuTerraria.TerrariaCompat.Items.Registry;
using Terraria.ModLoader;

namespace GregTechCEuTerraria.TerrariaCompat.Items.Prospectors;

public static class ProspectorItemLoader
{
	private const string ComponentItemClass = "com.gregtechceu.gtceu.api.item.ComponentItem";

	private static (int Radius, string Label)? SpecFor(string bareId) => bareId switch
	{
		"prospector.lv"  => (100,  "Basic Ore Scanner"),
		"prospector.hv"  => (500,  "Advanced Ore Scanner"),
		"prospector.luv" => (1000, "Elite Ore Scanner"),
		_                => null,
	};

	public static void Register(Mod mod)
	{
		int registered = 0;
		foreach (var e in RegistryDump.Entries)
		{
			if (e.Class != ComponentItemClass) continue;
			if (!e.BareId.StartsWith("prospector.", StringComparison.Ordinal)) continue;

			if (SpecFor(e.BareId) is not { } spec)
			{
				mod.Logger.Warn($"ProspectorItemLoader: no radius mapping for {e.Id} - skipped.");
				continue;
			}
			if (e.Electric is not { } es) continue;

			var tier = (VoltageTier)Math.Clamp(es.Tier, 0, (int)VoltageTier.MAX);
			mod.AddContent(new ProspectorItem(e.BareId, spec.Label, tier, es.Capacity, spec.Radius));
			registered++;
		}

		mod.Logger.Info($"ProspectorItemLoader: registered {registered} ore scanners from the registry dump.");
	}
}
