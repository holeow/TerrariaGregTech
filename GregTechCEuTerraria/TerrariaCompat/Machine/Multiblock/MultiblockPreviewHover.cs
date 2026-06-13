#nullable enable
using System.Collections.Generic;
using GregTechCEuTerraria.Api.Pattern;
using Terraria;
using Terraria.DataStructures;
using Terraria.Localization;
using Terraria.ModLoader;

namespace GregTechCEuTerraria.TerrariaCompat.Machine.Multiblock;

public static class MultiblockPreviewHover
{
	public static bool TryFind(int tileX, int tileY,
		out MultiblockControllerMachine controller,
		out char ch,
		out TraceabilityPredicate predicate)
	{
		controller = null!;
		ch = ' ';
		predicate = null!;

		PruneStaleControllers();
		foreach (var (_, te) in TileEntity.ByPosition)
		{
			if (te is not MultiblockControllerMachine c) continue;
			if (c.IsFormed) continue;
			if (!IsControllerTileAlive(c)) continue;
			if (!c.TryGetPreviewCell(tileX, tileY, out ch, out predicate)) continue;
			controller = c;
			return true;
		}
		return false;
	}

	internal static bool IsControllerTileAlive(MultiblockControllerMachine c)
	{
		var p = c.Position;
		if (p.X < 0 || p.X >= Terraria.Main.maxTilesX) return false;
		if (p.Y < 0 || p.Y >= Terraria.Main.maxTilesY) return false;
		return c.IsTileValidForEntity(p.X, p.Y);
	}

	private static readonly System.Collections.Generic.List<TileEntity> _stale = new();
	private static void PruneStaleControllers()
	{
		if (Terraria.Main.netMode == Terraria.ID.NetmodeID.MultiplayerClient) return;
		foreach (var (_, te) in TileEntity.ByPosition)
		{
			if (te is MultiblockControllerMachine c && !IsControllerTileAlive(c))
				_stale.Add(te);
		}
		if (_stale.Count == 0) return;
		foreach (var te in _stale)
		{
			if (te is MultiblockControllerMachine c) c.OnKill();
			TileEntity.ByID.Remove(te.ID);
			TileEntity.ByPosition.Remove(te.Position);
		}
		_stale.Clear();
	}

	public static void AppendTooltip(List<string> lines,
		MultiblockControllerMachine controller,
		TraceabilityPredicate predicate,
		int tileX, int tileY)
	{
		var hovered = Terraria.Main.tile[tileX, tileY];
		ushort currentTileType = hovered.HasTile ? hovered.TileType : (ushort)0;
		lines.Add($"[c/AAEEFF:{controller.DisplayName} - Multiblock Slot]");

		MachineTooltipLookup.AppendDescriptionAndBuilder(lines, controller.MachineKey, controller.MachineId, controller.Definition);

		if (predicate.IsAir())
		{
			lines.Add("Expected: [c/AAAAAA:air]");
		}
		else if (predicate.IsAny())
		{
			lines.Add("Expected: [c/AAAAAA:any block]");
		}
		else if (predicate.IsController)
		{
			lines.Add("Expected: [c/AAEEFF:controller (this machine)]");
		}
		else
		{
			var candidates = GatherCandidateNames(predicate);
			if (candidates.Count == 0)
				lines.Add("Expected: [c/AAAAAA:(no candidates registered)]");
			else
			{
				lines.Add("Expected:");
				foreach (var name in candidates)
					lines.Add("  " + name);
			}
		}

		if (currentTileType == 0)
		{
			lines.Add("Placed: [c/AAAAAA:empty]");
		}
		else
		{
			string placedName = TileTypeName(currentTileType);
			int matchX = tileX, matchY = tileY;
			var data = Terraria.ObjectData.TileObjectData.GetTileData(hovered);
			if (data != null && (data.Width > 1 || data.Height > 1))
			{
				matchX -= (hovered.TileFrameX / 18) % data.Width;
				matchY -= (hovered.TileFrameY / 18) % data.Height;
			}
			bool matches = predicate.IsAny()
				|| (predicate.IsAir() && currentTileType == 0)
				|| PredicateMatchesTileAt(predicate, matchX, matchY);
			string marker = matches ? "[c/44FF44:✓]" : "[c/FF4444:✗]";
			lines.Add($"Placed: {placedName} {marker}");
		}
	}

	private static List<string> GatherCandidateNames(TraceabilityPredicate predicate)
	{
		var names    = new List<string>();
		var seenType = new HashSet<int>();
		var seenKey  = new HashSet<string>(System.StringComparer.Ordinal);
		Add(predicate.Common);
		Add(predicate.Limited);
		return names;

		void Add(List<SimplePredicate> bucket)
		{
			foreach (var sp in bucket)
			{
				if (sp.Candidates is null) continue;
				var items = sp.Candidates();
				if (items is null) continue;
				foreach (var item in items)
				{
					if (item is null || item.IsAir) continue;
					if (!seenType.Add(item.type)) continue;
					string key = StripTierId(item.ModItem?.Name ?? item.type.ToString());
					if (!seenKey.Add(key)) continue;
					names.Add(StripTier(MetaMachine.StripColorTags(Lang.GetItemName(item.type).Value)));
				}
			}
		}
	}

	private static string StripTier(string name)
	{
		int sp = name.IndexOf(' ');
		if (sp > 0 && System.Array.IndexOf(_tierShortNames, name.Substring(0, sp)) >= 0)
			return name.Substring(sp + 1);
		int lsp = name.LastIndexOf(' ');
		if (lsp > 0 && System.Array.IndexOf(_tierShortNames, name.Substring(lsp + 1)) >= 0)
			return name.Substring(0, lsp);
		return name;
	}

	private static string StripTierId(string id)
	{
		int us = id.IndexOf('_');
		if (us <= 0) return id;
		string first = id.Substring(0, us);
		foreach (var t in _tierShortNames)
			if (string.Equals(first, t, System.StringComparison.OrdinalIgnoreCase))
				return id.Substring(us + 1);
		return id;
	}

	private static readonly string[] _tierShortNames =
	{
		"ULV", "LV", "MV", "HV", "EV", "IV", "LuV", "ZPM",
		"UV",  "UHV","UEV","UIV","UXV","OpV","MAX",
	};

	public static bool PredicateMatchesTileAt(TraceabilityPredicate predicate, int tileX, int tileY)
	{
		if (tileX < 0 || tileX >= Terraria.Main.maxTilesX) return false;
		if (tileY < 0 || tileY >= Terraria.Main.maxTilesY) return false;

		var state = new MultiblockState(tileX, tileY);
		state.Clean();
		if (!state.Update(tileX, tileY, predicate)) return false;
		return RunBucket(predicate.Common, state) || RunBucket(predicate.Limited, state);

		static bool RunBucket(List<SimplePredicate> bucket, MultiblockState state)
		{
			foreach (var sp in bucket)
			{
				if (sp.Predicate is null) continue;
				try { if (sp.Predicate(state)) return true; }
				catch { /* UI thread */ }
			}
			return false;
		}
	}

	public static int PlacedSiblingItemType(int tileX, int tileY)
	{
		if (tileX < 0 || tileX >= Terraria.Main.maxTilesX) return 0;
		if (tileY < 0 || tileY >= Terraria.Main.maxTilesY) return 0;
		var t = Terraria.Main.tile[tileX, tileY];
		if (!t.HasTile) return 0;
		var modTile = TileLoader.GetTile(t.TileType);
		if (modTile != null && modTile.Mod.TryFind<ModItem>(modTile.Name, out var mi))
			return mi.Type;
		return 0;
	}

	private static string TileTypeName(ushort tileType)
	{
		var modTile = TileLoader.GetTile(tileType);
		if (modTile != null)
		{
			if (modTile.Mod.TryFind<ModItem>(modTile.Name, out var modItem))
				return Lang.GetItemName(modItem.Type).Value;
			return modTile.Name;
		}
		string vanillaName = Lang.GetMapObjectName(Terraria.Map.MapHelper.TileToLookup(tileType, 0));
		return string.IsNullOrEmpty(vanillaName) ? $"Tile #{tileType}" : vanillaName;
	}
}
