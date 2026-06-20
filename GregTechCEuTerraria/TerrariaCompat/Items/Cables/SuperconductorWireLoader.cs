#nullable enable
using System.Collections.Generic;
using GregTechCEuTerraria.Api.Data.Chemical.Material;
using GregTechCEuTerraria.Common.Energy;
using GregTechCEuTerraria.Common.Materials;
using Terraria.Localization;
using Terraria.ModLoader;

namespace GregTechCEuTerraria.TerrariaCompat.Items.Cables;

public static class SuperconductorWireLoader
{
	public sealed record ScTier(
		VoltageTier Tier,
		string MetalId,
		string DisplayName,
		uint Color,
		int Amperage,
		string[] BarItemNames);

	public static readonly byte[] Sizes = { 1, 2, 4, 8, 16 };

	public static readonly ScTier[] Tiers =
	{
		new(VoltageTier.LV,  "evil",        "Evil Superconductor",        0x5B4D99, 2,  new[] { "DemoniteBar", "CrimtaneBar" }),
		new(VoltageTier.MV,  "meteorite",   "Meteorite Superconductor",   0x6E4A4E, 4,  new[] { "MeteoriteBar" }),
		new(VoltageTier.HV,  "hellstone",   "Hellstone Superconductor",   0x944441, 4,  new[] { "HellstoneBar" }),
		new(VoltageTier.EV,  "hallowed",    "Hallowed Superconductor",    0x907E59, 6,  new[] { "HallowedBar" }),
		new(VoltageTier.IV,  "chlorophyte", "Chlorophyte Superconductor", 0x3B8622, 6,  new[] { "ChlorophyteBar" }),
		new(VoltageTier.LuV, "spectre",     "Spectre Superconductor",     0x2EA8FC, 8,  new[] { "SpectreBar" }),
		new(VoltageTier.ZPM, "shroomite",   "Shroomite Superconductor",   0x3548DB, 8,  new[] { "ShroomiteBar" }),
		new(VoltageTier.UV,  "luminite",    "Luminite Superconductor",    0x50E0A0, 16, new[] { "LunarBar" }),
	};

	public static string MaterialId(ScTier t) => $"superconductor_{t.MetalId}";

	public static string WireItemName(ScTier t, byte size) =>
		$"{t.MetalId}_superconductor_{WireItem.WireSizeWord(size)}";

	private static string SizeLabel(byte size) => size switch
	{
		2  => "Double",
		4  => "Quadruple",
		8  => "Octal",
		16 => "Hex",
		_  => "",
	};

	public static void Register(Mod mod)
	{
		foreach (var t in Tiers)
		{
			var material = new Material
			{
				Id = MaterialId(t),
				Color = t.Color,
				CableTier = VoltageTiers.ShortName(t.Tier),
				CableAmperage = t.Amperage,
				CableLoss = 0,
				CableIsSuperconductor = true,
			};
			MaterialRegistry.Register(material);

			Language.GetOrRegister($"Mods.GregTechCEuTerraria.Materials.{material.Id}", () => t.DisplayName);

			foreach (byte size in Sizes)
			{
				string name = WireItemName(t, size);
				var item = new WireItem(name, material, size, insulated: false);
				mod.AddContent(item);
				WireItemRegistry.Index(material.Id, size, insulated: false, item);

				byte sz = size;
				string label = SizeLabel(sz);
				Language.GetOrRegister($"Mods.GregTechCEuTerraria.Items.{name}.DisplayName",
					() => $"{VoltageTiers.ShortName(t.Tier)} {t.DisplayName}{(label.Length > 0 ? " " + label : "")} Wire");
			}
		}

		mod.Logger.Info($"SuperconductorWireLoader: registered {Tiers.Length} tiers x {Sizes.Length} sizes " +
			$"= {Tiers.Length * Sizes.Length} Terraria-native superconductor wires.");
	}
}
