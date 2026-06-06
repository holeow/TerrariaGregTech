#nullable enable
using System;
using System.Collections.Generic;
using GregTechCEuTerraria.Api.Capability;
using GregTechCEuTerraria.Api.Machine;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader.IO;

namespace GregTechCEuTerraria.Api.Cover;

// Adaptation of com.gregtechceu.gtceu.api.capability.ICoverable
public interface ICoverable
{
	// ===== Holder identity (covers read these) ===============================

	Point16 GetBlockPos();
	bool IsRemote { get; }
	void NotifyBlockUpdate();
	TickableSubscription? SubscribeServerTick(Action runnable);
	void Unsubscribe(TickableSubscription? subscription);

	// Covers use `% N` against it to stagger their
	// periodic work (conveyor / pump / voiding tick every 5).
	long GetOffsetTimer();

	// ===== Per-side storage (implementor provides the backing array) =========

	CoverBehavior? GetCoverAtSide(CoverSide side);
	void SetCoverAtSide(CoverBehavior? cover, CoverSide side);
	bool CanPlaceCoverOnSide(CoverDefinition definition, CoverSide side);

	// ===== Per-side capability resolution ====================================
	IItemHandler?  GetItemHandlerCap(IODirection side, bool useCoverCapability);
	IFluidHandler? GetFluidHandlerCap(IODirection side, bool useCoverCapability);

	// ===== Place / remove ==========
	bool PlaceCoverOnSide(CoverSide side, Item itemStack, CoverDefinition definition)
	{
		var cover = definition.CreateCoverBehavior(this, side);
		if (!CanPlaceCoverOnSide(definition, side) || !cover.CanAttach())
			return false;
		if (GetCoverAtSide(side) != null)
			RemoveCover(side);
		cover.OnAttached(itemStack);
		cover.OnLoad();
		SetCoverAtSide(cover, side);
		NotifyBlockUpdate();
		return true;
	}

	List<Item> RemoveCover(CoverSide side)
	{
		var drops = new List<Item>();
		var cover = GetCoverAtSide(side);
		if (cover == null) return drops;
		if (!cover.GetPickItem().IsAir) drops.Add(cover.GetPickItem());
		drops.AddRange(cover.GetAdditionalDrops());
		cover.OnRemoved();
		SetCoverAtSide(null, side);
		NotifyBlockUpdate();
		return drops;
	}

	IEnumerable<CoverBehavior> GetCovers()
	{
		foreach (var side in CoverSides.All)
		{
			var cover = GetCoverAtSide(side);
			if (cover != null) yield return cover;
		}
	}

	bool HasCover(CoverSide side) => GetCoverAtSide(side) != null;

	bool HasAnyCover()
	{
		foreach (var side in CoverSides.All)
			if (GetCoverAtSide(side) != null) return true;
		return false;
	}

	void OnCoversUnload()
	{
		foreach (var cover in GetCovers()) cover.OnUnload();
	}

	void OnCoversNeighborChanged()
	{
		foreach (var cover in GetCovers()) cover.OnNeighborChanged();
	}

	// ===== Persistence ========================================================

	void SaveCovers(TagCompound tag)
	{
		foreach (var side in CoverSides.All)
		{
			var cover = GetCoverAtSide(side);
			if (cover == null) continue;
			var coverTag = new TagCompound { ["id"] = cover.CoverDefinition.Id };
			cover.Save(coverTag);
			tag[$"cover_{(int)side}"] = coverTag;
		}
	}

	void LoadCovers(TagCompound tag)
	{
		foreach (var side in CoverSides.All)
		{
			string key = $"cover_{(int)side}";
			var existing = GetCoverAtSide(side);
			if (!tag.ContainsKey(key))
			{
				if (existing != null)
				{
					existing.OnUnload();
					SetCoverAtSide(null, side);
				}
				continue;
			}
			var coverTag = tag.GetCompound(key);
			var definition = CoverRegistry.Get(coverTag.GetString("id"));
			if (definition == null) continue;
			if (existing != null && existing.CoverDefinition == definition)
			{
				existing.Load(coverTag);
				continue;
			}
			existing?.OnUnload();
			var cover = definition.CreateCoverBehavior(this, side);
			cover.Load(coverTag);
			SetCoverAtSide(cover, side);
			cover.OnLoad();
		}
	}
}
