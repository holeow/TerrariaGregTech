// Ported to C#/Terraria for GregTechCEuTerraria from Applied Energistics 2
// (appeng.helpers.MultiCraftingTracker), Forge 1.20.1. LGPL-3.0-only. See AE2 LICENSE.

#nullable enable
using System.Collections.Generic;
using GregTechCEuTerraria.AppliedEnergistics.Api.Stacks;
using GregTechCEuTerraria.TerrariaCompat.Pipelike.Me;
using Terraria.ModLoader.IO;

namespace GregTechCEuTerraria.TerrariaCompat.AppliedEnergistics.Crafting;

public sealed class MultiCraftingTracker
{
	private readonly int _size;
	private readonly IMeCraftingRequester _owner;
	private readonly CraftingLink?[] _links;

	public MultiCraftingTracker(IMeCraftingRequester owner, int size)
	{
		_owner = owner;
		_size = size;
		_links = new CraftingLink?[size];
	}

	public bool HandleCrafting(int x, AEKey what, long amount, MeNetwork? net)
	{
		if (GetLink(x) != null) return false;
		if (net == null) return false;

		var result = MeCraftingService.Request(net, what, amount, _owner);
		if (result.IsSuccess && result.Link != null)
		{
			SetLink(x, result.Link);
			return true;
		}
		return false;
	}

	public IReadOnlyCollection<CraftingLink> GetRequestedJobs()
	{
		var list = new List<CraftingLink>();
		foreach (var l in _links)
			if (l != null) list.Add(l);
		return list;
	}

	public void JobStateChange(CraftingLink link)
	{
		for (int x = 0; x < _links.Length; x++)
			if (_links[x] == link) { SetLink(x, null); return; }
	}

	public int GetSlot(CraftingLink link)
	{
		for (int x = 0; x < _links.Length; x++)
			if (_links[x] == link) return x;
		return -1;
	}

	public bool IsBusy(int slot) => GetLink(slot) != null;

	private CraftingLink? GetLink(int slot) => _links[slot];

	private void SetLink(int slot, CraftingLink? link)
	{
		_links[slot] = link;
		for (int x = 0; x < _links.Length; x++)
		{
			var g = _links[x];
			if (g != null && (g.IsCanceled || g.IsDone)) _links[x] = null;
		}
	}

	public void WriteToNBT(TagCompound tag)
	{
		for (int x = 0; x < _size; x++)
		{
			var link = GetLink(x);
			if (link == null) continue;
			var ln = new TagCompound();
			link.WriteToNBT(ln);
			tag["links-" + x] = ln;
		}
	}

	public void ReadFromNBT(TagCompound tag)
	{
		for (int x = 0; x < _size; x++)
		{
			string key = "links-" + x;
			if (!tag.ContainsKey(key)) continue;
			var link = new CraftingLink(tag.GetCompound(key), _owner);
			CraftingLinkManager.AddLink(link);
			SetLink(x, link);
		}
	}
}
