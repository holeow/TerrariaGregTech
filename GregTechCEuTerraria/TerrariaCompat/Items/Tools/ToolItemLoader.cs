#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using GregTechCEuTerraria.Api.Tool;
using GregTechCEuTerraria.Common.Materials;
using Microsoft.Xna.Framework;
using Terraria.ModLoader;

namespace GregTechCEuTerraria.TerrariaCompat.Items.Tools;

public static class ToolItemLoader
{
	private const string ToolTexDir = "GregTechCEuTerraria/Content/Textures/item/tools/";

	private static readonly Dictionary<string, int> _byUpstreamId = new();
	public static IReadOnlyDictionary<string, int> ByUpstreamId => _byUpstreamId;

	public static bool TryGet(string upstreamId, out int itemType) =>
		_byUpstreamId.TryGetValue(upstreamId, out itemType);

	private static readonly (GTToolType Base, string Tag)[] _catalystClasses =
	{
		(GTToolType.HARD_HAMMER, "gtceu:tools/crafting_hammers"),
		(GTToolType.SOFT_MALLET, "gtceu:tools/crafting_mallets"),
		(GTToolType.KNIFE,       "gtceu:tools/crafting_knives"),
		(GTToolType.FILE,        "gtceu:tools/crafting_files"),
		(GTToolType.SAW,         "gtceu:tools/crafting_saws"),
		(GTToolType.WRENCH,      "gtceu:tools/crafting_wrenches"),
		(GTToolType.SCREWDRIVER, "gtceu:tools/crafting_screwdrivers"),
		(GTToolType.WIRE_CUTTER, "gtceu:tools/crafting_wire_cutters"),
		(GTToolType.MORTAR,      "gtceu:tools/crafting_mortars"),
		(GTToolType.CROWBAR,     "gtceu:tools/crafting_crowbars"),
	};

	private static readonly (GTToolType Base, string Tag)[] _forgeToolTags =
	{
		(GTToolType.HARD_HAMMER,   "forge:tools/hammers"),
		(GTToolType.SOFT_MALLET,   "forge:tools/mallets"),
		(GTToolType.KNIFE,         "forge:tools/knives"),
		(GTToolType.FILE,          "forge:tools/files"),
		(GTToolType.SAW,           "forge:tools/saws"),
		(GTToolType.WRENCH,        "forge:tools/wrenches"),
		(GTToolType.SCREWDRIVER,   "forge:tools/screwdrivers"),
		(GTToolType.WIRE_CUTTER,   "forge:tools/wire_cutters"),
		(GTToolType.MORTAR,        "forge:tools/mortars"),
		(GTToolType.MINING_HAMMER, "forge:tools/mining_hammers"),
	};

	private static readonly Dictionary<string, List<int>> _craftingTagItems = new();
	public static IReadOnlyDictionary<string, List<int>> CraftingTagItems => _craftingTagItems;

	private static readonly Dictionary<int, ToolItem> _byType = new();
	public static void EnsureBaked(int toolType)
	{
		if (_byType.TryGetValue(toolType, out var tool)) tool.EnsureTextureBaked();
	}

	public static void Register(Mod mod)
	{
		_byUpstreamId.Clear();
		_craftingTagItems.Clear();
		_byType.Clear();

		var bundled = mod.GetFileNames().ToHashSet(StringComparer.OrdinalIgnoreCase);
		bool TexExists(string stem) => bundled.Contains($"Content/Textures/item/tools/{stem}.rawimg");

		int registered = 0, skipped = 0;
		foreach (var (_, material) in MaterialRegistry.All)
		{
			if (!material.HasTool()) continue;

			Color primary = RGB(material.Color);
			Color secondary = material.SecondaryColor is { } sc ? RGB(sc) : primary;

			foreach (var typeName in material.Tool!.Types)
			{
				var type = GTToolType.Get(typeName);
				if (type == null) continue;

				string id = type.ResolveId(material.Id);
				if (mod.TryFind<ModItem>(id, out _)) continue;
				if (!ToolModel.Layers.TryGetValue(type.Name, out var stems)) { skipped++; continue; }

				var layers = BuildLayers(stems, primary, secondary, TexExists, out string? headTex);
				if (layers == null) { skipped++; continue; }

				var item = new ToolItem(id, TitleCase(id), type, material, layers, headTex!);
				mod.AddContent(item);
				_byUpstreamId[$"gtceu:{id}"] = item.Type;
				_byType[item.Type] = item;
				registered++;

				foreach (var (baseType, tag) in _catalystClasses)
				{
					if (!type.ToolClasses.Contains(baseType)) continue;
					if (!_craftingTagItems.TryGetValue(tag, out var list))
						_craftingTagItems[tag] = list = new List<int>();
					list.Add(item.Type);
				}

				foreach (var (baseType, tag) in _forgeToolTags)
				{
					if (!type.ToolClasses.Contains(baseType)) continue;
					if (!_craftingTagItems.TryGetValue(tag, out var list))
						_craftingTagItems[tag] = list = new List<int>();
					list.Add(item.Type);
				}
			}
		}

		mod.Logger.Info($"ToolItemLoader: registered {registered} tools" +
			(skipped > 0 ? $" ({skipped} skipped - missing model/texture)" : "") + ".");
	}

	public static void Unload()
	{
		_byUpstreamId.Clear();
		_craftingTagItems.Clear();
		_byType.Clear();
	}

	private static ToolLayer[]? BuildLayers(string[] stems, Color primary, Color secondary,
		Func<string, bool> texExists, out string? headTex)
	{
		headTex = null;
		var layers = new List<ToolLayer>(stems.Length);
		for (int i = 0; i < stems.Length; i++)
		{
			string stem = stems[i];
			if (stem == ToolModel.Void) continue;
			if (!texExists(stem)) return null;

			string path = ToolTexDir + stem;
			Color tint = i switch { 1 => primary, 2 => secondary, _ => Color.White };
			layers.Add(new ToolLayer(path, tint));
			if (i == 1) headTex = path;
		}
		if (layers.Count == 0) return null;
		headTex ??= layers[0].TexturePath;
		return layers.ToArray();
	}

	private static Color RGB(uint? c) =>
		c is { } v ? new Color((byte)(v >> 16), (byte)(v >> 8), (byte)v) : Color.White;

	private static string TitleCase(string id) =>
		CultureInfo.InvariantCulture.TextInfo.ToTitleCase(id.Replace('_', ' '));
}
