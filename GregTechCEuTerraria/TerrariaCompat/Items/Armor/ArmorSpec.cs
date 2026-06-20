#nullable enable
using System.Collections.Generic;
using GregTechCEuTerraria.Common.Energy;
using Terraria.ID;

namespace GregTechCEuTerraria.TerrariaCompat.Items.Armor;

public enum ArmorSuite { Nano, Quark }
public enum ArmorPiece { Helmet, Chest, Legs }

public sealed class ArmorSpec
{
	public string Id { get; init; } = "";
	public string Label { get; init; } = "";
	public ArmorSuite Suite { get; init; }
	public ArmorPiece Piece { get; init; }
	public VoltageTier Tier { get; init; }
	public int EnergyPerUse { get; init; }
	public long Capacity { get; init; }
	public int FullDefense { get; init; }
	public int FloorDefense { get; init; }
	public string SpriteFolder { get; init; } = "";
	public int Rarity { get; init; }
	public bool HasFlight { get; init; }

	private const string ArmorRoot = "GregTechCEuTerraria/Content/TerrariaCompat/Armors";

	public string IconPath => $"{ArmorRoot}/{SpriteFolder}/{IconFile}";
	public string EquipPath => $"{ArmorRoot}/{SpriteFolder}/{EquipFile}";

	private string IconFile => Piece switch
	{
		ArmorPiece.Chest => SpriteFolder + "Breastplate",
		ArmorPiece.Legs  => SpriteFolder + "Leggings",
		_                => SpriteFolder,
	};

	private string EquipFile => Piece switch
	{
		ArmorPiece.Chest => SpriteFolder + "Body_Body",
		ArmorPiece.Legs  => SpriteFolder + "Legs_Legs",
		_                => SpriteFolder + "Head_Head",
	};
}

public static class ArmorCatalog
{
	private const int  NanoEu  = 512;
	private const long NanoCap = 6_400_000L;
	private const int  QuarkEu  = 8192;
	private const long QuarkCap = 100_000_000L;

	public static readonly IReadOnlyList<ArmorSpec> All = new[]
	{
		new ArmorSpec { Id = "nanomuscle_helmet",     Label = "NanoMuscle™ Suite Helmet",     Suite = ArmorSuite.Nano,  Piece = ArmorPiece.Helmet, Tier = VoltageTier.HV, EnergyPerUse = NanoEu,  Capacity = NanoCap,  FullDefense = 10, FloorDefense = 2, SpriteFolder = "nanofiber", Rarity = ItemRarityID.LightPurple },
		new ArmorSpec { Id = "nanomuscle_chestplate", Label = "NanoMuscle™ Suite Chestplate", Suite = ArmorSuite.Nano,  Piece = ArmorPiece.Chest,  Tier = VoltageTier.HV, EnergyPerUse = NanoEu,  Capacity = NanoCap,  FullDefense = 16, FloorDefense = 3, SpriteFolder = "nanofiber", Rarity = ItemRarityID.LightPurple },
		new ArmorSpec { Id = "nanomuscle_leggings",   Label = "NanoMuscle™ Suite Leggings",   Suite = ArmorSuite.Nano,  Piece = ArmorPiece.Legs,   Tier = VoltageTier.HV, EnergyPerUse = NanoEu,  Capacity = NanoCap,  FullDefense = 12, FloorDefense = 2, SpriteFolder = "nanofiber", Rarity = ItemRarityID.LightPurple },

		new ArmorSpec { Id = "quarktech_helmet",      Label = "QuarkTech™ Suite Helmet",      Suite = ArmorSuite.Quark, Piece = ArmorPiece.Helmet, Tier = VoltageTier.IV, EnergyPerUse = QuarkEu, Capacity = QuarkCap, FullDefense = 13, FloorDefense = 3, SpriteFolder = "quarktech", Rarity = ItemRarityID.Yellow },
		new ArmorSpec { Id = "quarktech_chestplate",  Label = "QuarkTech™ Suite Chestplate",  Suite = ArmorSuite.Quark, Piece = ArmorPiece.Chest,  Tier = VoltageTier.IV, EnergyPerUse = QuarkEu, Capacity = QuarkCap, FullDefense = 26, FloorDefense = 5, SpriteFolder = "quarktech", Rarity = ItemRarityID.Yellow },
		new ArmorSpec { Id = "quarktech_leggings",    Label = "QuarkTech™ Suite Leggings",    Suite = ArmorSuite.Quark, Piece = ArmorPiece.Legs,   Tier = VoltageTier.IV, EnergyPerUse = QuarkEu, Capacity = QuarkCap, FullDefense = 17, FloorDefense = 3, SpriteFolder = "quarktech", Rarity = ItemRarityID.Yellow },

		new ArmorSpec { Id = "advanced_nanomuscle_chestplate", Label = "Advanced NanoMuscle™ Suite Chestplate", Suite = ArmorSuite.Nano,  Piece = ArmorPiece.Chest, Tier = VoltageTier.HV, EnergyPerUse = NanoEu,  Capacity = NanoCap,  FullDefense = 16, FloorDefense = 3, SpriteFolder = "advanced_nanofiber", Rarity = ItemRarityID.Pink,    HasFlight = true },
		new ArmorSpec { Id = "advanced_quarktech_chestplate",  Label = "Advanced QuarkTech™ Suite Chestplate",  Suite = ArmorSuite.Quark, Piece = ArmorPiece.Chest, Tier = VoltageTier.IV, EnergyPerUse = QuarkEu, Capacity = QuarkCap, FullDefense = 26, FloorDefense = 5, SpriteFolder = "advanced_quarktech", Rarity = ItemRarityID.Cyan,    HasFlight = true },
	};
}
