#nullable enable
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using GregTechCEuTerraria.Api.Capability.Recipe;
using GregTechCEuTerraria.Api.Recipe;
using GregTechCEuTerraria.Api.Recipe.Ingredient;
using GregTechCEuTerraria.Common.Energy;
using GregTechCEuTerraria.TerrariaCompat.Items.Cables;
using Terraria.ID;

namespace GregTechCEuTerraria.TerrariaCompat.Recipes;

// Recipes the port needs but all.json doesn't have
// JSON shape goes through the same GTRecipeSerializer + IngredientResolver as the bundle
public static class CompatRecipes
{
	public static readonly System.Collections.Generic.HashSet<string> OverriddenIds = new();


	// compat_{ulv,lv}_{input,output}_{hatch,bus} - make ULV/LV hatches/buses in hand.
	private const string HatchesAndBuses = """
	[
	  { "id": "crafting_shaped/compat_ulv_input_hatch", "type": "minecraft:crafting_shaped",
	    "inputs":  { "item": [
	      { "content": { "item": "gtceu:ulv_machine_hull" } },
	      { "content": { "tag": "forge:glass" } },
	      { "content": { "type": "gtceu:sized", "count": 2,
	        "ingredient": { "item": "gtceu:rubber_ingot" } } }
	    ] },
	    "outputs": { "item": [ { "content": { "item": "gtceu:ulv_input_hatch" } } ] } },

	  { "id": "crafting_shaped/compat_ulv_output_hatch", "type": "minecraft:crafting_shaped",
	    "inputs":  { "item": [
	      { "content": { "item": "gtceu:ulv_machine_hull" } },
	      { "content": { "tag": "forge:glass" } },
	      { "content": { "type": "gtceu:sized", "count": 2,
	        "ingredient": { "item": "gtceu:rubber_ingot" } } }
	    ] },
	    "outputs": { "item": [ { "content": { "item": "gtceu:ulv_output_hatch" } } ] } },

	  { "id": "crafting_shaped/compat_lv_input_hatch", "type": "minecraft:crafting_shaped",
	    "inputs":  { "item": [
	      { "content": { "item": "gtceu:lv_machine_hull" } },
	      { "content": { "item": "gtceu:wood_drum" } },
	      { "content": { "type": "gtceu:sized", "count": 2,
	        "ingredient": { "item": "gtceu:rubber_ingot" } } }
	    ] },
	    "outputs": { "item": [ { "content": { "item": "gtceu:lv_input_hatch" } } ] } },

	  { "id": "crafting_shaped/compat_lv_output_hatch", "type": "minecraft:crafting_shaped",
	    "inputs":  { "item": [
	      { "content": { "item": "gtceu:lv_machine_hull" } },
	      { "content": { "item": "gtceu:wood_drum" } },
	      { "content": { "type": "gtceu:sized", "count": 2,
	        "ingredient": { "item": "gtceu:rubber_ingot" } } }
	    ] },
	    "outputs": { "item": [ { "content": { "item": "gtceu:lv_output_hatch" } } ] } },

	  { "id": "crafting_shaped/compat_ulv_input_bus", "type": "minecraft:crafting_shaped",
	    "inputs":  { "item": [
	      { "content": { "item": "gtceu:ulv_machine_hull" } },
	      { "content": { "tag": "forge:chests/wooden" } },
	      { "content": { "type": "gtceu:sized", "count": 2,
	        "ingredient": { "item": "gtceu:rubber_ingot" } } }
	    ] },
	    "outputs": { "item": [ { "content": { "item": "gtceu:ulv_input_bus" } } ] } },

	  { "id": "crafting_shaped/compat_ulv_output_bus", "type": "minecraft:crafting_shaped",
	    "inputs":  { "item": [
	      { "content": { "item": "gtceu:ulv_machine_hull" } },
	      { "content": { "tag": "forge:chests/wooden" } },
	      { "content": { "type": "gtceu:sized", "count": 2,
	        "ingredient": { "item": "gtceu:rubber_ingot" } } }
	    ] },
	    "outputs": { "item": [ { "content": { "item": "gtceu:ulv_output_bus" } } ] } },

	  { "id": "crafting_shaped/compat_lv_input_bus", "type": "minecraft:crafting_shaped",
	    "inputs":  { "item": [
	      { "content": { "item": "gtceu:lv_machine_hull" } },
	      { "content": { "item": "gtceu:wood_crate" } },
	      { "content": { "type": "gtceu:sized", "count": 2,
	        "ingredient": { "item": "gtceu:rubber_ingot" } } }
	    ] },
	    "outputs": { "item": [ { "content": { "item": "gtceu:lv_input_bus" } } ] } },

	  { "id": "crafting_shaped/compat_lv_output_bus", "type": "minecraft:crafting_shaped",
	    "inputs":  { "item": [
	      { "content": { "item": "gtceu:lv_machine_hull" } },
	      { "content": { "item": "gtceu:wood_crate" } },
	      { "content": { "type": "gtceu:sized", "count": 2,
	        "ingredient": { "item": "gtceu:rubber_ingot" } } }
	    ] },
	    "outputs": { "item": [ { "content": { "item": "gtceu:lv_output_bus" } } ] } }
	]
	""";

	// steam_boiler/compat_wood - use wood as boiler fuel
	// compat_rubber_ingot + compat_rubber_plate - make rubber plates in hand
	// compat_wrought_iron - Iron smelting into WroughtIron
	private const string Bootstrap = """
	[
	  { "id": "steam_boiler/compat_wood", "type": "gtceu:steam_boiler", "duration": 300,
	    "inputs": { "item": [ { "content": { "type": "gtceu:sized", "count": 1,
	      "ingredient": { "tag": "minecraft:logs" } } } ] } },

	  { "id": "smelting/compat_rubber_ingot", "type": "minecraft:smelting",
	    "inputs":  { "item": [ { "content": { "type": "gtceu:sized", "count": 2,
	      "ingredient": { "item": "gtceu:sticky_resin" } } } ] },
	    "outputs": { "item": [ { "content": { "item": "gtceu:rubber_ingot" } } ] } },

	  { "id": "smelting/compat_wrought_iron", "type": "minecraft:smelting",
	    "inputs":  { "item": [ { "content": { "item": "minecraft:iron_ingot" } } ] },
	    "outputs": { "item": [ { "content": { "item": "gtceu:wrought_iron_ingot" } } ] } },

	  { "id": "crafting_shaped/compat_rubber_plate", "type": "minecraft:crafting_shaped",
	    "inputs":  { "item": [
	      { "content": { "type": "gtceu:sized", "count": 2,
	        "ingredient": { "item": "gtceu:rubber_ingot" } } },
	      { "content": { "tag": "gtceu:tools/crafting_hammers" } }
	    ] },
	    "outputs": { "item": [ { "content": { "item": "gtceu:rubber_plate" } } ] } }
	]
	""";

	// coke clay without brick form
	private const string Misc = """
	[
	  { "id": "crafting_shaped/compat_compressed_coke_clay_formless", "type": "minecraft:crafting_shaped",
	    "inputs":  { "item": [
	      { "content": { "type": "gtceu:sized", "count": 3,
	        "ingredient": { "item": "minecraft:clay_ball" } } },
	      { "content": { "type": "gtceu:sized", "count": 5,
	        "ingredient": { "tag": "minecraft:sand" } } }
	    ] },
	    "outputs": { "item": [ { "content": { "type": "gtceu:sized", "count": 2,
	      "ingredient": { "item": "gtceu:compressed_coke_clay" } } } ] } }
	]
	""";

	private const string SimplePipes = """
	[
	  { "id": "crafting_shaped/compat_simple_item_pipe", "type": "minecraft:crafting_shaped",
	    "inputs":  { "item": [
	      { "content": { "type": "gtceu:sized", "count": 3,
	        "ingredient": { "item": "minecraft:stone" } } }
	    ] },
	    "outputs": { "item": [ { "content": { "type": "gtceu:sized", "count": 4,
	      "ingredient": { "item": "gtceu:simple_item_pipe" } } } ] } },

	  { "id": "crafting_shaped/compat_simple_fluid_pipe", "type": "minecraft:crafting_shaped",
	    "inputs":  { "item": [
	      { "content": { "type": "gtceu:sized", "count": 3,
	        "ingredient": { "tag": "minecraft:logs" } } }
	    ] },
	    "outputs": { "item": [ { "content": { "type": "gtceu:sized", "count": 4,
	      "ingredient": { "item": "gtceu:simple_fluid_pipe" } } } ] } },

	  { "id": "crafting_shaped/compat_simple_item_pipe_small", "type": "minecraft:crafting_shaped",
	    "inputs":  { "item": [
	      { "content": { "type": "gtceu:sized", "count": 2,
	        "ingredient": { "item": "minecraft:stone" } } }
	    ] },
	    "outputs": { "item": [ { "content": { "type": "gtceu:sized", "count": 4,
	      "ingredient": { "item": "gtceu:simple_item_pipe_small" } } } ] } },

	  { "id": "crafting_shaped/compat_simple_item_pipe_large", "type": "minecraft:crafting_shaped",
	    "inputs":  { "item": [
	      { "content": { "type": "gtceu:sized", "count": 5,
	        "ingredient": { "item": "minecraft:stone" } } }
	    ] },
	    "outputs": { "item": [ { "content": { "type": "gtceu:sized", "count": 4,
	      "ingredient": { "item": "gtceu:simple_item_pipe_large" } } } ] } },

	  { "id": "crafting_shaped/compat_simple_item_pipe_huge", "type": "minecraft:crafting_shaped",
	    "inputs":  { "item": [
	      { "content": { "type": "gtceu:sized", "count": 8,
	        "ingredient": { "item": "minecraft:stone" } } }
	    ] },
	    "outputs": { "item": [ { "content": { "type": "gtceu:sized", "count": 4,
	      "ingredient": { "item": "gtceu:simple_item_pipe_huge" } } } ] } },

	  { "id": "crafting_shaped/compat_simple_fluid_pipe_tiny", "type": "minecraft:crafting_shaped",
	    "inputs":  { "item": [
	      { "content": { "type": "gtceu:sized", "count": 1,
	        "ingredient": { "tag": "minecraft:logs" } } }
	    ] },
	    "outputs": { "item": [ { "content": { "type": "gtceu:sized", "count": 4,
	      "ingredient": { "item": "gtceu:simple_fluid_pipe_tiny" } } } ] } },

	  { "id": "crafting_shaped/compat_simple_fluid_pipe_small", "type": "minecraft:crafting_shaped",
	    "inputs":  { "item": [
	      { "content": { "type": "gtceu:sized", "count": 2,
	        "ingredient": { "tag": "minecraft:logs" } } }
	    ] },
	    "outputs": { "item": [ { "content": { "type": "gtceu:sized", "count": 4,
	      "ingredient": { "item": "gtceu:simple_fluid_pipe_small" } } } ] } },

	  { "id": "crafting_shaped/compat_simple_fluid_pipe_large", "type": "minecraft:crafting_shaped",
	    "inputs":  { "item": [
	      { "content": { "type": "gtceu:sized", "count": 6,
	        "ingredient": { "tag": "minecraft:logs" } } }
	    ] },
	    "outputs": { "item": [ { "content": { "type": "gtceu:sized", "count": 4,
	      "ingredient": { "item": "gtceu:simple_fluid_pipe_large" } } } ] } },

	  { "id": "crafting_shaped/compat_simple_fluid_pipe_huge", "type": "minecraft:crafting_shaped",
	    "inputs":  { "item": [
	      { "content": { "type": "gtceu:sized", "count": 12,
	        "ingredient": { "tag": "minecraft:logs" } } }
	    ] },
	    "outputs": { "item": [ { "content": { "type": "gtceu:sized", "count": 4,
	      "ingredient": { "item": "gtceu:simple_fluid_pipe_huge" } } } ] } }
	]
	""";

	// boss summons
	private const string Casings = """
	[
	  { "id": "chemical_reactor/compat_dirty_stainless_steel_casing", "type": "gtceu:chemical_reactor", "duration": 300,
	    "inputs":  { "item": [
	      { "content": { "item": "gtceu:clean_machine_casing" } },
	      { "content": { "type": "gtceu:sized", "count": 4, "ingredient": { "item": "minecraft:dirt" } } }
	    ] },
	    "tickInputs": { "eu": [ { "content": 480 } ] },
	    "outputs": { "item": [ { "content": { "item": "gtceu:dirty_stainless_steel_casing" } } ] } },

	  { "id": "chemical_bath/compat_frozen_frostproof_casing", "type": "gtceu:chemical_bath", "duration": 200,
	    "inputs":  {
	      "item":  [ { "content": { "item": "gtceu:frostproof_machine_casing" } } ],
	      "fluid": [ { "content": { "amount": 1000, "value": { "tag": "forge:distilled_water" } } } ]
	    },
	    "tickInputs": { "eu": [ { "content": 480 } ] },
	    "outputs": { "item": [ { "content": { "item": "gtceu:frozen_frostproof_casing" } } ] } },

	  { "id": "chemical_reactor/compat_acid_etched_inert_casing", "type": "gtceu:chemical_reactor", "duration": 200,
	    "inputs":  {
	      "item":  [ { "content": { "item": "gtceu:inert_machine_casing" } } ],
	      "fluid": [ { "content": { "amount": 1000, "value": { "fluid": "gtceu:sulfuric_acid" } } } ]
	    },
	    "tickInputs": { "eu": [ { "content": 480 } ] },
	    "outputs": { "item": [ { "content": { "item": "gtceu:acid_etched_inert_casing" } } ] } },

	  { "id": "large_chemical_reactor/compat_unstable_compressor_charge", "type": "gtceu:large_chemical_reactor", "duration": 240,
	    "inputs":  {
	      "item":  [
	        { "content": { "item": "gtceu:solid_machine_casing" } },
	        { "content": { "type": "gtceu:sized", "count": 4, "ingredient": { "item": "gtceu:industrial_tnt" } } },
	        { "content": { "type": "gtceu:sized", "count": 4, "ingredient": { "item": "gtceu:titanium_plate" } } }
	      ],
	      "fluid": [ { "content": { "amount": 1000, "value": { "fluid": "gtceu:lubricant" } } } ]
	    },
	    "tickInputs": { "eu": [ { "content": 1920 } ] },
	    "outputs": { "item": [ { "content": { "item": "gtceu:unstable_compressor_charge" } } ] } }
	]
	""";

	// compat_clay_from_sand (+ _hand) - there is no infinite clay source, so we need a way to get it from sand
	private const string Clay = """
	[
	  { "id": "chemical_bath/compat_clay_from_sand", "type": "gtceu:chemical_bath", "duration": 200,
	    "inputs":  {
	      "item":  [ { "content": { "type": "gtceu:sized", "count": 10, "ingredient": { "item": "minecraft:sand" } } } ],
	      "fluid": [ { "content": { "amount": 1000, "value": { "tag": "forge:water" } } } ]
	    },
	    "tickInputs": { "eu": [ { "content": 16 } ] },
	    "outputs": { "item": [ { "content": { "type": "gtceu:sized", "count": 10, "ingredient": { "item": "minecraft:clay" } } } ] } },

	  { "id": "crafting_shaped_fluid_container/compat_clay_from_sand_hand", "type": "gtceu:crafting_shaped_fluid_container",
	    "inputs":  {
	      "item": [
	        { "content": { "type": "gtceu:sized", "count": 8, "ingredient": { "item": "minecraft:sand" } } },
	        { "content": { "type": "gtceu:fluid_container", "fluid": { "amount": 1000, "value": { "tag": "forge:water" } } } }
	      ]
	    },
	    "outputs": { "item": [ { "content": { "type": "gtceu:sized", "count": 8, "ingredient": { "item": "minecraft:clay" } } } ] } }
	]
	""";

	// compat_pressure_plate - needed for early game covers but aren't craftable in vanilla terraria (post-dungeon selling)
	private const string TerrariaIntermediates = """
	[
	  { "id": "assembler/compat_heavy_weighted_pressure_plate", "type": "gtceu:assembler", "duration": 100,
	    "inputs":  { "item": [ { "content": { "type": "gtceu:sized", "count": 2,
	      "ingredient": { "tag": "forge:plates/iron" } } } ] },
	    "tickInputs": { "eu": [ { "content": 16 } ] },
	    "outputs": { "item": [ { "content": { "item": "minecraft:heavy_weighted_pressure_plate" } } ] } },

	  { "id": "assembler/compat_light_weighted_pressure_plate", "type": "gtceu:assembler", "duration": 100,
	    "inputs":  { "item": [ { "content": { "type": "gtceu:sized", "count": 2,
	      "ingredient": { "tag": "forge:plates/gold" } } } ] },
	    "tickInputs": { "eu": [ { "content": 16 } ] },
	    "outputs": { "item": [ { "content": { "item": "minecraft:light_weighted_pressure_plate" } } ] } }
	]
	""";

	// compat_terra_prisma - because some people dont like bullet hell
	private const string TerraPrisma = """
	[
	  { "id": "assembler/compat_terra_prisma", "type": "gtceu:assembler", "duration": 2400,
	    "inputs":  {
	      "item":  [
	        { "content": { "item": "gtceu:wetware_processor_assembly" } },
	        { "content": { "type": "gtceu:sized", "count": 4, "ingredient": { "item": "gtceu:zpm_emitter" } } },
	        { "content": { "type": "gtceu:sized", "count": 2, "ingredient": { "item": "gtceu:zpm_field_generator" } } },
	        { "content": { "type": "gtceu:sized", "count": 8, "ingredient": { "item": "gtceu:europium_plate" } } },
	        { "content": { "type": "gtceu:sized", "count": 16, "ingredient": { "item": "gtceu:fine_europium_wire" } } }
	      ],
	      "fluid": [ { "content": { "amount": 1000, "value": { "fluid": "gtceu:soldering_alloy" } } } ]
	    },
	    "tickInputs": { "eu": [ { "content": 131072 } ] },
	    "recipeConditions": [ { "type": "gtceu:cleanroom" } ],
	    "outputs": { "item": [ { "content": { "item": "terraria:EmpressBlade" } } ] } }
	]
	""";

	// Terraria Amber to redstone to unlock skyblock
	private const string RedstoneGem = """
	[
	  { "id": "macerator/compat_macerate_redstone_gem", "type": "gtceu:macerator", "duration": 25,
	    "inputs":  { "item": [ { "content": { "type": "gtceu:sized", "count": 1,
	      "ingredient": { "item": "gtceu:redstone_gem" } } } ] },
	    "tickInputs": { "eu": [ { "content": 2 } ] },
	    "outputs": { "item": [ { "content": { "type": "gtceu:sized", "count": 16,
	      "ingredient": { "item": "gtceu:redstone_dust" } } } ] } }
	]
	""";

	// Terraria coin conversion in the packer
	private const string Coins = """
	[
	  { "id": "packer/compat_pack_copper_coins", "type": "gtceu:packer", "duration": 100,
	    "inputs": { "item": [
	      { "content": { "type": "gtceu:sized", "count": 100, "ingredient": { "item": "terraria:CopperCoin" } } },
	      { "chance": 0, "content": { "type": "gtceu:circuit", "configuration": 1 } }
	    ] },
	    "tickInputs": { "eu": [ { "content": 4 } ] },
	    "outputs": { "item": [ { "content": { "item": "terraria:SilverCoin" } } ] } },

	  { "id": "packer/compat_pack_silver_coins", "type": "gtceu:packer", "duration": 100,
	    "inputs": { "item": [
	      { "content": { "type": "gtceu:sized", "count": 100, "ingredient": { "item": "terraria:SilverCoin" } } },
	      { "chance": 0, "content": { "type": "gtceu:circuit", "configuration": 1 } }
	    ] },
	    "tickInputs": { "eu": [ { "content": 4 } ] },
	    "outputs": { "item": [ { "content": { "item": "terraria:GoldCoin" } } ] } },

	  { "id": "packer/compat_pack_gold_coins", "type": "gtceu:packer", "duration": 100,
	    "inputs": { "item": [
	      { "content": { "type": "gtceu:sized", "count": 100, "ingredient": { "item": "terraria:GoldCoin" } } },
	      { "chance": 0, "content": { "type": "gtceu:circuit", "configuration": 1 } }
	    ] },
	    "tickInputs": { "eu": [ { "content": 4 } ] },
	    "outputs": { "item": [ { "content": { "item": "terraria:PlatinumCoin" } } ] } },

	  { "id": "packer/compat_unpack_silver_coin", "type": "gtceu:packer", "duration": 100,
	    "inputs": { "item": [
	      { "content": { "item": "terraria:SilverCoin" } },
	      { "chance": 0, "content": { "type": "gtceu:circuit", "configuration": 2 } }
	    ] },
	    "tickInputs": { "eu": [ { "content": 4 } ] },
	    "outputs": { "item": [ { "content": { "type": "gtceu:sized", "count": 100, "ingredient": { "item": "terraria:CopperCoin" } } } ] } },

	  { "id": "packer/compat_unpack_gold_coin", "type": "gtceu:packer", "duration": 100,
	    "inputs": { "item": [
	      { "content": { "item": "terraria:GoldCoin" } },
	      { "chance": 0, "content": { "type": "gtceu:circuit", "configuration": 2 } }
	    ] },
	    "tickInputs": { "eu": [ { "content": 4 } ] },
	    "outputs": { "item": [ { "content": { "type": "gtceu:sized", "count": 100, "ingredient": { "item": "terraria:SilverCoin" } } } ] } },

	  { "id": "packer/compat_unpack_platinum_coin", "type": "gtceu:packer", "duration": 100,
	    "inputs": { "item": [
	      { "content": { "item": "terraria:PlatinumCoin" } },
	      { "chance": 0, "content": { "type": "gtceu:circuit", "configuration": 2 } }
	    ] },
	    "tickInputs": { "eu": [ { "content": 4 } ] },
	    "outputs": { "item": [ { "content": { "type": "gtceu:sized", "count": 100, "ingredient": { "item": "terraria:GoldCoin" } } } ] } }
	]
	""";

	private const string LegacyConversions = """
	[
	  { "id": "crafting_shaped/compat_convert_legacy_platinum_ingot", "type": "minecraft:crafting_shaped",
	    "inputs":  { "item": [ { "content": { "item": "GregTechCEuTerraria/platinum_ingot" } } ] },
	    "outputs": { "item": [ { "content": { "item": "terraria:PlatinumBar" } } ] } },

	  { "id": "crafting_shaped/compat_convert_legacy_silver_ingot", "type": "minecraft:crafting_shaped",
	    "inputs":  { "item": [ { "content": { "item": "GregTechCEuTerraria/silver_ingot" } } ] },
	    "outputs": { "item": [ { "content": { "item": "terraria:SilverBar" } } ] } },

	  { "id": "crafting_shaped/compat_convert_legacy_tin_ingot", "type": "minecraft:crafting_shaped",
	    "inputs":  { "item": [ { "content": { "item": "GregTechCEuTerraria/tin_ingot" } } ] },
	    "outputs": { "item": [ { "content": { "item": "terraria:TinBar" } } ] } },

	  { "id": "crafting_shaped/compat_convert_legacy_lead_ingot", "type": "minecraft:crafting_shaped",
	    "inputs":  { "item": [ { "content": { "item": "GregTechCEuTerraria/lead_ingot" } } ] },
	    "outputs": { "item": [ { "content": { "item": "terraria:LeadBar" } } ] } },

	  { "id": "crafting_shaped/compat_convert_legacy_amethyst_gem", "type": "minecraft:crafting_shaped",
	    "inputs":  { "item": [ { "content": { "item": "GregTechCEuTerraria/amethyst_gem" } } ] },
	    "outputs": { "item": [ { "content": { "item": "terraria:Amethyst" } } ] } },

	  { "id": "crafting_shaped/compat_convert_legacy_ruby_gem", "type": "minecraft:crafting_shaped",
	    "inputs":  { "item": [ { "content": { "item": "GregTechCEuTerraria/ruby_gem" } } ] },
	    "outputs": { "item": [ { "content": { "item": "terraria:Ruby" } } ] } },

	  { "id": "crafting_shaped/compat_convert_legacy_sapphire_gem", "type": "minecraft:crafting_shaped",
	    "inputs":  { "item": [ { "content": { "item": "GregTechCEuTerraria/sapphire_gem" } } ] },
	    "outputs": { "item": [ { "content": { "item": "terraria:Sapphire" } } ] } },

	  { "id": "crafting_shaped/compat_convert_legacy_topaz_gem", "type": "minecraft:crafting_shaped",
	    "inputs":  { "item": [ { "content": { "item": "GregTechCEuTerraria/topaz_gem" } } ] },
	    "outputs": { "item": [ { "content": { "item": "terraria:Topaz" } } ] } }
	]
	""";

	private static readonly string[] JsonGroups =
	{
		HatchesAndBuses, Bootstrap, Misc, SimplePipes, Casings, Clay,
		TerrariaIntermediates, TerraPrisma, RedstoneGem, Coins, LegacyConversions,
	};

	public sealed record RecipePatch(string BaseId, string NewId, string MergeJson)
	{
		public bool IsOverride => NewId == BaseId;
	}

	private static RecipePatch Override(string baseId, string mergeJson) => new(baseId, baseId, mergeJson);
	private static RecipePatch Derive(string baseId, string newId, string mergeJson) => new(baseId, newId, mergeJson);

	public static readonly List<RecipePatch> Patches = new()
	{
		// Steam miners, steam macerator remove diamond ingredient
		Override("shaped/steam_miner_bronze", """
			{ "key": { "D": { "tag": null, "item": "gtceu:bronze_rod" } } }
			"""),

		Override("shaped/steam_miner_steel", """
			{ "key": { "D": { "tag": null, "item": "gtceu:steel_rod" } } }
			"""),

		Override("shaped/steam_macerator_bronze", """
			{ "key": { "D": { "tag": null, "item": "gtceu:bronze_rod" } } }
			"""),

		// raw_rubber_dust processing buff
		Override("extractor/raw_rubber_from_resin", """
			{ "outputs": { "item": [ { "content": { "type": "gtceu:sized", "count": 8,
			  "ingredient": { "item": "gtceu:raw_rubber_dust" } } } ] } }
			"""),

		Override("centrifuge/sticky_resin_separation", """
			{ "outputs": { "item": [
			  { "content": { "type": "gtceu:sized", "count": 8, "ingredient": { "item": "gtceu:raw_rubber_dust" } } },
			  { "chance": 1500, "content": { "type": "gtceu:sized", "count": 1, "ingredient": { "item": "gtceu:plant_ball" } } }
			] } }
			"""),

		Override("extractor/raw_rubber_from_slime", """
			{ "outputs": { "item": [ { "content": { "type": "gtceu:sized", "count": 5,
			  "ingredient": { "item": "gtceu:raw_rubber_dust" } } } ] } }
			"""),

		// Nether star alternative
		Derive("implosion_compressor/implode_dust_nether_star_dynamite",
			"implosion_compressor/compat_implode_mana_crystal_nether_star_dynamite", """
			{ "inputs": { "item": [
			  { "content": { "type": "gtceu:sized", "count": 5, "ingredient": { "item": "terraria:ManaCrystal" } } },
			  { "content": { "type": "gtceu:sized", "count": 2, "ingredient": { "item": "gtceu:dynamite" } } }
			] } }
			"""),

		Derive("implosion_compressor/implode_dust_nether_star_powderbarrel",
			"implosion_compressor/compat_implode_mana_crystal_nether_star_powderbarrel", """
			{ "inputs": { "item": [
			  { "content": { "type": "gtceu:sized", "count": 5, "ingredient": { "item": "terraria:ManaCrystal" } } },
			  { "content": { "type": "gtceu:sized", "count": 8, "ingredient": { "item": "gtceu:powderbarrel" } } }
			] } }
			"""),

		Derive("implosion_compressor/implode_dust_nether_star_tnt",
			"implosion_compressor/compat_implode_mana_crystal_nether_star_tnt", """
			{ "inputs": { "item": [
			  { "content": { "type": "gtceu:sized", "count": 5, "ingredient": { "item": "terraria:ManaCrystal" } } },
			  { "content": { "type": "gtceu:sized", "count": 4, "ingredient": { "item": "minecraft:tnt" } } }
			] } }
			"""),

		// Terraria bottomless bucket infinite liquid
		Derive("extractor/extract_iron_ingot",
			"extractor/compat_extract_bottomless_water", """
			{ "inputs":  { "item":  [ { "chance": 0, "content": { "item": "terraria:BottomlessBucket" } } ] },
			  "outputs": { "fluid": [ { "content": { "amount": 1000, "value": { "fluid": "gtceu:water" } } } ] } }
			"""),

		Derive("extractor/extract_steel_ingot",
			"extractor/compat_extract_bottomless_honey", """
			{ "inputs":  { "item":  [ { "chance": 0, "content": { "item": "terraria:BottomlessHoneyBucket" } } ] },
			  "outputs": { "fluid": [ { "content": { "amount": 1000, "value": { "fluid": "gtceu:honey" } } } ] } }
			"""),

		Derive("extractor/extract_hssg_ingot",
			"extractor/compat_extract_bottomless_lava", """
			{ "inputs":  { "item":  [ { "chance": 0, "content": { "item": "terraria:BottomlessLavaBucket" } } ] },
			  "outputs": { "fluid": [ { "content": { "amount": 1000, "value": { "fluid": "gtceu:lava" } } } ] } }
			"""),

		Derive("extractor/extract_hsse_ingot",
			"extractor/compat_extract_bottomless_shimmer", """
			{ "inputs":     { "item":  [ { "chance": 0, "content": { "item": "terraria:BottomlessShimmerBucket" } } ] },
			  "outputs":    { "fluid": [ { "content": { "amount": 1000, "value": { "fluid": "gtceu:shimmer" } } } ] },
			  "tickInputs": { "eu":    [ { "content": 1920 } ] } }
			"""),

		// Get liquid shimmer
		Derive("extractor/extract_hssg_ingot",
			"extractor/compat_extract_shimmer_block", """
			{ "inputs":  { "item":  [ { "content": { "item": "terraria:ShimmerBlock" } } ] },
			  "outputs": { "fluid": [ { "content": { "amount": 1000, "value": { "fluid": "gtceu:shimmer" } } } ] } }
			"""),

		// Chum Bucket - fermented biomass
		Derive("canner/spray_can_white_dye",
			"extractor/compat_extract_chum_bucket", """
			{ "type": "gtceu:extractor", "category": "gtceu:extractor",
			  "inputs":  { "fluid": null,
			               "item":  [ { "content": { "item": "terraria:ChumBucket" } } ] },
			  "outputs": { "item":  [ { "content": { "item": "terraria:EmptyBucket" } } ],
			               "fluid": [ { "content": { "amount": 1000, "value": { "fluid": "gtceu:fermented_biomass" } } } ] } }
			"""),

		Derive("canner/spray_can_white_dye",
			"canner/compat_can_chum_bucket", """
			{ "inputs":  { "fluid": [ { "content": { "amount": 1000, "value": { "fluid": "gtceu:fermented_biomass" } } } ],
			               "item":  [ { "content": { "item": "terraria:EmptyBucket" } } ] },
			  "outputs": { "item":  [ { "content": { "item": "terraria:ChumBucket" } } ] } }
			"""),

		// Ultimate Manual Crafting Station
		Derive("electric_blast_furnace/aluminium_from_ruby_dust",
			"electric_blast_furnace/compat_ultimate_manual_station",
			"""{ "inputs": { "item": [ { "content": { "item": "GregTechCEuTerraria/manual_hammer" } }, { "content": { "item": "GregTechCEuTerraria/manual_mallet" } }, { "content": { "item": "GregTechCEuTerraria/manual_knife" } }, { "content": { "item": "GregTechCEuTerraria/manual_file" } }, { "content": { "item": "GregTechCEuTerraria/manual_saw" } }, { "content": { "item": "GregTechCEuTerraria/manual_wrench" } }, { "content": { "item": "GregTechCEuTerraria/manual_screwdriver" } }, { "content": { "item": "GregTechCEuTerraria/manual_wire_cutter" } }, { "content": { "item": "GregTechCEuTerraria/manual_mortar" } }, { "content": { "item": "GregTechCEuTerraria/manual_crowbar" } }, { "content": { "item": "gtceu:nether_star" } } ] }, "outputs": { "item": [ { "content": { "item": "GregTechCEuTerraria/ultimate_manual" } } ] } }"""),
	};

	// Per-tier crafting recipes for our custom-block machines
	private static readonly string[] LampTiers =
	{
		"ulv","lv","mv","hv","ev","iv","luv","zpm","uv","uhv","uev","uiv","uxv","opv","max",
	};
	private static readonly string[] SolarPanelTiers =
	{
		"ulv","lv",
	};

	private static string LampRecipe(string tier) =>
		$$"""
		{ "id": "crafting_shaped/compat_{{tier}}_lamp", "type": "minecraft:crafting_shaped",
		  "inputs":  { "item": [
		    { "content": { "item": "gtceu:{{tier}}_machine_casing" } },
		    { "content": { "item": "minecraft:torch" } }
		  ] },
		  "outputs": { "item": [ { "content": { "item": "gtceu:{{tier}}_lamp" } } ] } }
		""";

	private static string SolarPanelRecipe(string tier) =>
		$$"""
		{ "id": "crafting_shaped/compat_{{tier}}_solar_panel_machine", "type": "minecraft:crafting_shaped",
		  "inputs":  { "item": [
		    { "content": { "item": "gtceu:{{tier}}_steam_turbine" } },
		    { "content": { "item": "gtceu:{{tier}}_solar_panel" } }
		  ] },
		  "outputs": { "item": [ { "content": { "item": "gtceu:{{tier}}_solar_panel_machine" } } ] } }
		""";

	// Terraria superconductors
	private static readonly (byte Size, int Circuit, int InCount, int OutCount, int DurMult)[] WireLadder =
	{
		(1,  1,  1, 2, 1),
		(2,  2,  1, 1, 1),
		(4,  4,  2, 1, 2),
		(8,  8,  4, 1, 4),
		(16, 16, 8, 1, 8),
	};

	private const int SuperconductorBaseDuration = 100;

	private static string SuperconductorWireRecipe(
		SuperconductorWireLoader.ScTier tier,
		(byte Size, int Circuit, int InCount, int OutCount, int DurMult) rung,
		string barItemName, string idSuffix)
	{
		string wireId = SuperconductorWireLoader.WireItemName(tier, rung.Size);
		string sizeWord = WireItem.WireSizeWord(rung.Size);
		int eut = VoltageTiers.VA((int)tier.Tier);
		int dur = SuperconductorBaseDuration * rung.DurMult;
		return $$"""
		{ "id": "wiremill/compat_sc_{{tier.MetalId}}_{{sizeWord}}{{idSuffix}}", "type": "gtceu:wiremill", "duration": {{dur}},
		  "inputs": { "item": [
		    { "content": { "type": "gtceu:sized", "count": {{rung.InCount}}, "ingredient": { "item": "terraria:{{barItemName}}" } } },
		    { "chance": 0, "content": { "type": "gtceu:circuit", "configuration": {{rung.Circuit}} } }
		  ] },
		  "tickInputs": { "eu": [ { "content": {{eut}} } ] },
		  "outputs": { "item": [
		    { "content": { "type": "gtceu:sized", "count": {{rung.OutCount}}, "ingredient": { "item": "GregTechCEuTerraria/{{wireId}}" } } }
		  ] } }
		""";
	}

	public static List<(string Station, GTRecipe Recipe)> Build(IIngredientResolver resolver)
	{
		var result = new List<(string, GTRecipe)>();
		foreach (var group in JsonGroups)
			ParseArray(group, resolver, result);
		foreach (var tier in LampTiers)
			ParseOne(LampRecipe(tier), resolver, result);
		foreach (var tier in SolarPanelTiers)
			ParseOne(SolarPanelRecipe(tier), resolver, result);

		foreach (var scTier in SuperconductorWireLoader.Tiers)
			foreach (var rung in WireLadder)
			{
				bool multiBar = scTier.BarItemNames.Length > 1;
				foreach (var bar in scTier.BarItemNames)
				{
					string suffix = multiBar ? "_" + bar.ToLowerInvariant() : "";
					ParseOne(SuperconductorWireRecipe(scTier, rung, bar, suffix), resolver, result);
				}
			}

		return result;
	}

	private static void ParseArray(string json, IIngredientResolver resolver, List<(string, GTRecipe)> result)
	{
		using var doc = JsonDocument.Parse(json);
		foreach (var el in doc.RootElement.EnumerateArray())
			ParseElement(el, resolver, result);
	}

	private static void ParseOne(string json, IIngredientResolver resolver, List<(string, GTRecipe)> result)
	{
		using var doc = JsonDocument.Parse(json);
		ParseElement(doc.RootElement, resolver, result);
	}

	private static void ParseElement(JsonElement el, IIngredientResolver resolver, List<(string, GTRecipe)> result)
	{
		string id = el.GetProperty("id").GetString()!;
		var recipe = GTRecipeSerializer.Read(el, resolver, id);
		result.Add((recipe.RecipeType.RegistryName, recipe));
	}

	public static (string Station, GTRecipe Recipe) MaterializePatch(
		RecipePatch patch, string baseRawJson, IIngredientResolver resolver)
	{
		var merged = JsonNode.Parse(baseRawJson)!.AsObject();
		var delta = JsonNode.Parse(patch.MergeJson)!.AsObject();
		MergeInto(merged, delta);
		merged["id"] = patch.NewId;

		using var doc = JsonDocument.Parse(merged.ToJsonString());
		var el = doc.RootElement;
		var recipe = VanillaRecipeJson.IsVanillaShape(el)
			? VanillaRecipeJson.Read(el, resolver, patch.NewId)
			: GTRecipeSerializer.Read(el, resolver, patch.NewId);
		return (recipe.RecipeType.RegistryName, recipe);
	}

	// JSON Merge Patch (RFC 7386)
	private static void MergeInto(JsonObject target, JsonObject patch)
	{
		foreach (var (key, value) in patch)
		{
			if (value is null) { target.Remove(key); continue; }
			if (value is JsonObject patchChild && target[key] is JsonObject targetChild)
				MergeInto(targetChild, patchChild);
			else
				target[key] = value.DeepClone();
		}
	}

	private static readonly (int VanillaItemId, string Material)[] VanillaOreMaterials =
	{
		(ItemID.IronOre,     "iron"),
		(ItemID.LeadOre,     "lead"),
		(ItemID.CopperOre,   "copper"),
		(ItemID.TinOre,      "tin"),
		(ItemID.GoldOre,     "gold"),
		(ItemID.PlatinumOre, "platinum"),
		(ItemID.SilverOre,   "silver"),
		(ItemID.TungstenOre, "tungstate"),
	};

	public static List<GTRecipe> BuildVanillaOreMaceratorRecipes(
		IReadOnlyDictionary<string, List<GTRecipe>> byStation)
	{
		var result = new List<GTRecipe>();
		if (!byStation.TryGetValue("macerator", out var macerator)) return result;

		var bySrcId = new Dictionary<string, GTRecipe>(macerator.Count);
		foreach (var r in macerator) bySrcId[r.Id] = r;

		var outputScale = Api.Recipe.Content.ContentModifier.Multiplier_(16);
		var euScale     = Api.Recipe.Content.ContentModifier.Multiplier_(2);

		foreach (var (vanillaItemId, material) in VanillaOreMaterials)
		{
			string srcId = $"macerator/macerate_raw_{material}_ore_to_crushed_ore";
			if (!bySrcId.TryGetValue(srcId, out var src)) continue;

			var inputs = new Dictionary<object, List<Api.Recipe.Content.Content>>(src.Inputs.Count);
			foreach (var (cap, list) in src.Inputs)
			{
				if (ReferenceEquals(cap, ItemRecipeCapability.CAP))
				{
					var payload = new SizedIngredient(
						new ItemStackIngredient(vanillaItemId, $"terraria:{material}_ore"), 1);
					int maxChance = Api.Recipe.Chance.Logic.ChanceLogic.GetMaxChancedValue();
					inputs[cap] = new List<Api.Recipe.Content.Content> { new(payload, maxChance, maxChance, 0) };
				}
				else
				{
					inputs[cap] = list.Select(c => c.Copy(cap)).ToList();
				}
			}

			var outputs     = outputScale.ApplyContents(src.Outputs);
			var tickInputs  = euScale.ApplyContents(src.TickInputs);
			var tickOutputs = Api.Recipe.Content.ContentModifier.IDENTITY.ApplyContents(src.TickOutputs);

			var derived = new GTRecipe(
				src.RecipeType,
				$"macerator/compat_macerate_terraria_{material}_ore_to_crushed_ore",
				inputs, outputs, tickInputs, tickOutputs,
				new Dictionary<object, Api.Recipe.Chance.Logic.ChanceLogic>(src.InputChanceLogics),
				new Dictionary<object, Api.Recipe.Chance.Logic.ChanceLogic>(src.OutputChanceLogics),
				new Dictionary<object, Api.Recipe.Chance.Logic.ChanceLogic>(src.TickInputChanceLogics),
				new Dictionary<object, Api.Recipe.Chance.Logic.ChanceLogic>(src.TickOutputChanceLogics),
				new List<Api.Recipe.RecipeCondition>(src.Conditions),
				new List<object>(src.IngredientActions),
				src.Data,
				src.Duration,
				src.RecipeCategory,
				src.GroupColor);

			result.Add(derived);
		}

		return result;
	}
}
