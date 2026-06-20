#nullable enable
using System;
using System.Collections.Generic;
using GregTechCEuTerraria.Common.Materials;
using GregTechCEuTerraria.TerrariaCompat.Items.Registry;
using GregTechCEuTerraria.TerrariaCompat.Pipelike.Cable;
using Terraria.ModLoader;

namespace GregTechCEuTerraria.TerrariaCompat.Items.Cables;

public static class WireItemRegistry
{
	// Key = (materialId, wireSize, insulated)
	private static readonly Dictionary<(string, byte, bool), WireItem> _items = new();

	public static int Count => _items.Count;

	public static int? Get(string materialId, byte wireSize, bool insulated) =>
		_items.TryGetValue((materialId, wireSize, insulated), out var it) ? it.Type : null;

	internal static void Index(string materialId, byte wireSize, bool insulated, WireItem item) =>
		_items[(materialId, wireSize, insulated)] = item;

	public static CableCell? BuildCell(string materialId, byte wireSize, bool insulated) =>
		_items.TryGetValue((materialId, wireSize, insulated), out var it)
			? it.BuildCell() : (CableCell?)null;

	private const string PipeItemClass = "com.gregtechceu.gtceu.api.item.MaterialPipeBlockItem";

	private static readonly Dictionary<string, byte> PrefixToSize = new()
	{
		["wireGtSingle"]     = 1,
		["wireGtDouble"]     = 2,
		["wireGtQuadruple"]  = 4,
		["wireGtOctal"]      = 8,
		["wireGtHex"]        = 16,
		["cableGtSingle"]    = 1,
		["cableGtDouble"]    = 2,
		["cableGtQuadruple"] = 4,
		["cableGtOctal"]     = 8,
		["cableGtHex"]       = 16,
	};

	public static void Register(Mod mod)
	{
		_items.Clear();

		int missingMaterial = 0, missingCableTier = 0;
		foreach (var e in RegistryDump.Entries)
		{
			if (e.Class != PipeItemClass) continue;
			if (e.Prefix is null || !PrefixToSize.TryGetValue(e.Prefix, out byte size)) continue;
			if (e.Material is null) { missingMaterial++; continue; }

			var material = MaterialRegistry.Get(e.Material);
			if (material is null) { missingMaterial++; continue; }
			if (material.CableTier is null) missingCableTier++;

			bool insulated = e.Prefix.StartsWith("cable", StringComparison.Ordinal);
			var item = new WireItem(e.BareId, material, size, insulated);
			mod.AddContent(item);
			_items[(material.Id, size, insulated)] = item;
		}

		mod.Logger.Info($"WireItemRegistry: registered {_items.Count} wire + cable items from the registry dump" +
			(missingMaterial > 0 ? $" ({missingMaterial} skipped - material not in MaterialRegistry)" : "") +
			(missingCableTier > 0 ? $" ({missingCableTier} have no CableTier - extractor gap, ULV fallback)" : "") + ".");
	}

	public static void Unload() => _items.Clear();
}
