// Ported to C#/Terraria for GregTechCEuTerraria from Applied Energistics 2's crafting-link
// registry (appeng.me.service.CraftingService link management), Forge 1.20.1. LGPL-3.0-only.
// See AE2 LICENSE
#nullable enable
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace GregTechCEuTerraria.TerrariaCompat.AppliedEnergistics.Crafting;

public sealed class CraftingLinkManager : ModSystem
{
	private static readonly Dictionary<Guid, CraftingLinkNexus> _links = new();

	public override void ClearWorld() => _links.Clear();

	public override void PostUpdateWorld()
	{
		if (_links.Count == 0) return;
		var mgr = this;
		List<Guid>? dead = null;
		foreach (var kv in _links)
			if (kv.Value.IsDead(mgr))
				(dead ??= new()).Add(kv.Key);
		if (dead != null)
			foreach (var id in dead)
				_links.Remove(id);
	}

	public static void AddLink(CraftingLink link)
	{
		if (link.IsStandalone) return;
		if (!_links.TryGetValue(link.CraftId, out var nexus))
			_links[link.CraftId] = nexus = new CraftingLinkNexus(link.CraftId);
		link.SetNexus(nexus);
	}

	public bool HasCpu(CraftingCpuLogic? cpu) => cpu != null && cpu.IsAlive;
}
