#nullable enable
using System;
using System.Collections.Generic;
using GregTechCEuTerraria.Common.Materials;
using GregTechCEuTerraria.Api.Data.Chemical.Material;
using GregTechCEuTerraria.TerrariaCompat.Items.Ammo;
using GregTechCEuTerraria.TerrariaCompat.Items.Registry;
using GregTechCEuTerraria.TerrariaCompat.Machine.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace GregTechCEuTerraria.TerrariaCompat.Items;

public sealed class MaterialItem : ModItem, ITextureWarmUp
{
	private readonly string? _id;
	[CloneByReference] private readonly Material? _material;
	[CloneByReference] private readonly MaterialPrefix? _prefix;
	[CloneByReference] private readonly IReadOnlyList<RegistryDump.RenderLayer> _layers;

	private const int AnimFrameTicks = 5;
	private const string TextureRoot = "GregTechCEuTerraria/Content/Textures/";
	private const string NetherStarGemId = "nether_star_gem";
	private const int NetherStarFrames = 8;
	private const int NetherStarFrameTicks = 5;

	public MaterialItem()
	{
		_layers = Array.Empty<RegistryDump.RenderLayer>();
	}

	public MaterialItem(string id, Material material, MaterialPrefix prefix,
		IReadOnlyList<RegistryDump.RenderLayer> layers)
	{
		_id = id;
		_material = material;
		_prefix = prefix;
		_layers = layers;
	}

	public override string Name => _id ?? nameof(MaterialItem);
	protected override bool CloneNewInstances => true;
	public override bool IsLoadingEnabled(Mod mod) => _id != null;

	public int? MaterialColorRgb => _material?.Color is uint c ? (int)(c & 0xFFFFFF) : null;

	public override string Texture => _layers.Count > 0
		? TextureRoot + _layers[0].Texture
		: "Terraria/Images/Item_22";

	public override void SetStaticDefaults()
	{
		base.SetStaticDefaults();
		if (_prefix?.Id == "exquisite_gem")
		{
			Language.GetOrRegister("Mods.GregTechCEuTerraria.ExquisiteGem.Equip",
				() => "Equip as an accessory");
			if (_material is not null && EffectText(_material.Id) is string desc)
				Language.GetOrRegister($"Mods.GregTechCEuTerraria.ExquisiteGem.Effect.{_material.Id}",
					() => desc);
		}
		if (Main.dedServ) return;
		if (_id == NetherStarGemId)
		{
			Main.RegisterItemAnimation(Item.type,
				new Terraria.DataStructures.DrawAnimationVertical(NetherStarFrameTicks, NetherStarFrames) { PingPong = true });
			return;
		}
		if (_layers.Count == 0) return;
		string path = TextureRoot + _layers[0].Texture;
		if (!ModContent.HasAsset(path)) return;
		var tex = ModContent.Request<Texture2D>(path, AssetRequestMode.ImmediateLoad).Value;
		if (tex.Width > 0 && tex.Height > tex.Width && tex.Height % tex.Width == 0)
		{
			int frameCount = tex.Height / tex.Width;
			Main.RegisterItemAnimation(Item.type,
				new Terraria.DataStructures.DrawAnimationVertical(AnimFrameTicks, frameCount));
		}
	}

	public override void SetDefaults()
	{
		Item.maxStack = 9999;
		Item.width = 32;
		Item.height = 32;
		Item.value = 0;
		Item.rare = ItemRarityID.Blue;
		if (_prefix?.Id is "block" or "raw_ore_block" or "frame" && _id is not null)
		{
			int? tileType = Tiles.MaterialBlockTileRegistry.Get(_id);
			if (tileType.HasValue)
			{
				Item.DefaultToPlaceableTile(tileType.Value);
				Item.useTime = Players.CenteredPlacementPlayer.PlaceUseTime;
				Item.useAnimation = Players.CenteredPlacementPlayer.PlaceUseAnimation;
			}
		}

		if (_prefix?.Id == "exquisite_gem")
		{
			Item.accessory = true;
			Item.rare = ItemRarityID.Green;
			Item.defense = _material?.Id switch
			{
				"rutile" => 2,
				"emerald" or "grossular" or "rock_salt" => 1,
				_ => 0,
			};
		}

		if (_prefix?.Id == "round" && _material is not null)
		{
			int hl = _material.HarvestLevel ?? 2;
			Item.ammo = AmmoID.Bullet;
			Item.shoot = ModContent.ProjectileType<RoundProjectile>();
			Item.DamageType = DamageClass.Ranged;
			Item.damage = 3 + hl * 3;
			Item.knockBack = 3f;
			Item.shootSpeed = 16f;
			Item.consumable = true;
		}
	}

	public override void ModifyTooltips(List<TooltipLine> tooltips)
	{
		base.ModifyTooltips(tooltips);
		AppendExquisiteGemTooltip(tooltips);
		string? formula = _material?.Formula;
		if (string.IsNullOrEmpty(formula)) return;
		var line = new TooltipLine(Mod, "ChemicalFormula", formula)
		{
			OverrideColor = new Color(255, 255, 85),
		};
		int nameIdx = tooltips.FindIndex(t => t.Mod == "Terraria" && t.Name == "ItemName");
		if (nameIdx >= 0) tooltips.Insert(nameIdx + 1, line);
		else tooltips.Add(line);
	}

	private void AppendExquisiteGemTooltip(List<TooltipLine> tooltips)
	{
		if (_prefix?.Id != "exquisite_gem" || _material is null) return;
		tooltips.Add(new TooltipLine(Mod, "ExquisiteGemEquip",
			Language.GetTextValue("Mods.GregTechCEuTerraria.ExquisiteGem.Equip"))
		{
			OverrideColor = new Color(170, 170, 255),
		});
		if (EffectText(_material.Id) is not null)
			tooltips.Add(new TooltipLine(Mod, "ExquisiteGemEffect",
				Language.GetTextValue($"Mods.GregTechCEuTerraria.ExquisiteGem.Effect.{_material.Id}"))
			{
				OverrideColor = new Color(120, 255, 120),
			});
	}

	private static string? EffectText(string materialId) => materialId switch
	{
		"diamond" => "2% damage reduction",
		"almandine" => "10% thorns",
		"cinnabar" => "Immune to Poisoned",
		"ruby" => "+15 max life",
		"pyrope" => "+30 max life",
		"sapphire" => "+20 max mana",
		"lapis" => "+40 max mana, but take 25% more damage",
		"lazurite" => "+80 max mana, but take 100% more damage",
		"sodalite" => "8% reduced mana cost",
		"green_sapphire" => "Higher jumps",
		"red_garnet" => "+8% melee damage",
		"realgar" => "+16% melee damage, but take 25% more damage",
		"topaz" => "+8% ranged damage",
		"amethyst" => "+8% magic damage",
		"nether_quartz" => "+16% magic damage, but take 25% more damage",
		"spessartine" => "+8% summon damage",
		"uvarovite" => "+16% summon damage, but take 25% more damage",
		"monazite" => "+30% minion damage, but take 100% more damage",
		"opal" => "+5% damage",
		"glass" => "+25% critical strike chance, but take 50% more damage",
		"andradite" => "+8% melee critical strike chance",
		"yellow_garnet" => "+8% ranged critical strike chance",
		"certus_quartz" => "+8% magic critical strike chance",
		"blue_topaz" => "+8% summon critical strike chance",
		"olivine" => "+10% movement speed",
		"apatite" => "+10 fishing power",
		"malachite" => "+8% mining speed",
		"quartzite" => "+2 tile reach",
		"echo_shard" => "Grants Dangersense",
		"salt" => "Reduced enemy aggression",
		"coal" => "Emits light",
		"coke" => "Immune to On Fire!",
		_ => null,
	};

	public override void UpdateAccessory(Player player, bool hideVisual)
	{
		base.UpdateAccessory(player, hideVisual);
		if (_prefix?.Id != "exquisite_gem" || _material is null) return;
		switch (_material.Id)
		{
			case "diamond":
				player.endurance += 0.02f;
				break;
			case "almandine":
				player.thorns += 0.10f;
				break;
			case "cinnabar":
				player.buffImmune[BuffID.Poisoned] = true;
				break;
			case "ruby":
				player.statLifeMax2 += 15;
				break;
			case "pyrope":
				player.statLifeMax2 += 30;
				break;
			case "sapphire":
				player.statManaMax2 += 20;
				break;
			case "lazurite":
				player.statManaMax2 += 80;
				player.GetModPlayer<Players.ExquisiteGemPlayer>().DamageTakenBonus += 1.0f;
				break;
			case "lapis":
				player.statManaMax2 += 40;
				player.GetModPlayer<Players.ExquisiteGemPlayer>().DamageTakenBonus += 0.25f;
				break;
			case "sodalite":
				player.manaCost -= 0.08f;
				break;
			case "green_sapphire":
				player.jumpSpeedBoost += 1.5f;
				break;
			case "red_garnet":
				player.GetDamage(DamageClass.Melee) += 0.08f;
				break;
			case "realgar":
				player.GetDamage(DamageClass.Melee) += 0.16f;
				player.GetModPlayer<Players.ExquisiteGemPlayer>().DamageTakenBonus += 0.25f;
				break;
			case "topaz":
				player.GetDamage(DamageClass.Ranged) += 0.08f;
				break;
			case "amethyst":
				player.GetDamage(DamageClass.Magic) += 0.08f;
				break;
			case "nether_quartz":
				player.GetDamage(DamageClass.Magic) += 0.16f;
				player.GetModPlayer<Players.ExquisiteGemPlayer>().DamageTakenBonus += 0.25f;
				break;
			case "spessartine":
				player.GetDamage(DamageClass.Summon) += 0.08f;
				break;
			case "uvarovite":
				player.GetDamage(DamageClass.Summon) += 0.16f;
				player.GetModPlayer<Players.ExquisiteGemPlayer>().DamageTakenBonus += 0.25f;
				break;
			case "opal":
				player.GetDamage(DamageClass.Generic) += 0.05f;
				break;
			case "glass":
				player.GetCritChance(DamageClass.Generic) += 25f;
				player.GetModPlayer<Players.ExquisiteGemPlayer>().DamageTakenBonus += 0.5f;
				break;
			case "andradite":
				player.GetCritChance(DamageClass.Melee) += 8f;
				break;
			case "yellow_garnet":
				player.GetCritChance(DamageClass.Ranged) += 8f;
				break;
			case "certus_quartz":
				player.GetCritChance(DamageClass.Magic) += 8f;
				break;
			case "blue_topaz":
				player.GetCritChance(DamageClass.Summon) += 8f;
				break;
			case "monazite":
				player.GetDamage(DamageClass.Summon) += 0.30f;
				player.GetModPlayer<Players.ExquisiteGemPlayer>().DamageTakenBonus += 1.0f;
				break;
			case "olivine":
				player.moveSpeed += 0.10f;
				break;
			case "apatite":
				player.fishingSkill += 10;
				break;
			case "malachite":
				player.pickSpeed -= 0.08f;
				break;
			case "quartzite":
				player.blockRange += 2;
				break;
			case "echo_shard":
				player.dangerSense = true;
				break;
			case "salt":
				player.aggro -= 200;
				break;
			case "coal":
				Lighting.AddLight(player.Center, 0.6f, 0.55f, 0.45f);
				break;
			case "coke":
				player.buffImmune[BuffID.OnFire] = true;
				break;
		}
	}

	public override void UpdateInventory(Player player)
	{
		base.UpdateInventory(player);
		if (_prefix?.Id == "hot_ingot")
			player.AddBuff(BuffID.OnFire, 120);
	}

	public override void HoldItem(Player player)
	{
		base.HoldItem(player);
		EnsureTextureBaked();
	}

	void ITextureWarmUp.WarmUpTexture() => EnsureTextureBaked();

	private void EnsureTextureBaked()
	{
		if (_id == NetherStarGemId)
		{
			ItemIconBaker.InstallGreyscaleFromVanilla(Item.type, ItemID.FallenStar);
			return;
		}
		if (_layers.Count == 0) return;
		var iconLayers = new IconLayer[_layers.Count];
		for (int i = 0; i < _layers.Count; i++)
			iconLayers[i] = new IconLayer(TextureRoot + _layers[i].Texture, Tint(_layers[i].Argb));
		ItemIconBaker.Install(Item.type, iconLayers);
	}

	private static Color Tint(int argb) => new(
		(byte)((argb >> 16) & 0xFF),
		(byte)((argb >> 8) & 0xFF),
		(byte)(argb & 0xFF),
		(byte)((argb >> 24) & 0xFF));
}
