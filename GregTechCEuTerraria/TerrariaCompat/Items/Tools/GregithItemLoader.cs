#nullable enable
using System.Collections.Generic;
using System.Globalization;
using GregTechCEuTerraria.Api.Data.Chemical.Material;
using GregTechCEuTerraria.Api.Tool;
using GregTechCEuTerraria.Common.Materials;
using Terraria.ModLoader;

namespace GregTechCEuTerraria.TerrariaCompat.Items.Tools;

public static class GregithItemLoader
{
	private const int MinTools = 8;

	private static readonly Dictionary<string, int> _byMaterialId = new();
	public static IReadOnlyDictionary<string, int> ByMaterialId => _byMaterialId;

	public static void Register(Mod mod)
	{
		_byMaterialId.Clear();
		int registered = 0;
		var allGregithTypes = new List<int>();
		var allToolPool = new List<int>();
		Material? overclockMaterial = null;

		foreach (var (_, material) in MaterialRegistry.All)
		{
			if (!material.HasTool()) continue;
			if (ToolItemLoader.ExcludedMaterials.Contains(material.Id)) continue;

			var ingredients = new List<int>();
			foreach (var typeName in material.Tool!.Types)
			{
				var type = GTToolType.Get(typeName);
				if (type == null || type.IsElectric) continue;
				if (type == GTToolType.MORTAR) continue;
				string toolId = $"gtceu:{type.ResolveId(material.Id)}";
				if (ToolItemLoader.TryGet(toolId, out int itemType))
					ingredients.Add(itemType);
			}

			if (ingredients.Count < MinTools) continue;

			int tier = ToolTier.For(material);
			string id = $"{material.Id}_gregith";
			string label = $"{TitleCase(material.Id)} Gregith";

			var item = new GregithItem(id, label, material, tier, ingredients.ToArray());
			mod.AddContent(item);
			_byMaterialId[material.Id] = item.Type;
			registered++;

			allGregithTypes.Add(item.Type);
			allToolPool.AddRange(ingredients);
			if (material.Id == "neutronium" || overclockMaterial == null)
				overclockMaterial = material;
		}

		if (allGregithTypes.Count >= 2 && overclockMaterial != null)
		{
			const int overclockTier = ToolTier.TierCount - 1;
			var oc = new GregithItem("overclocked_gregith", "Overclocked Gregith",
				overclockMaterial, overclockTier, allToolPool.ToArray(),
				overclocked: true, recipeItemTypes: allGregithTypes.ToArray());
			mod.AddContent(oc);
			_byMaterialId["overclocked"] = oc.Type;
			registered++;
		}

		mod.Logger.Info($"GregithItemLoader: registered {registered} Gregith weapons.");
	}

	public static void Unload() => _byMaterialId.Clear();

	private static string TitleCase(string id) =>
		CultureInfo.InvariantCulture.TextInfo.ToTitleCase(id.Replace('_', ' '));
}
