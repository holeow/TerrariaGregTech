#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GregTechCEuTerraria.Api.Fluids;
using GregTechCEuTerraria.Common.Materials;
using GregTechCEuTerraria.TerrariaCompat.Items.Fluids;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.Bestiary;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace GregTechCEuTerraria.TerrariaCompat.NPCs.EBFChan;

// Npc that guides the player through the gregtech eventually
[AutoloadHead]
public class EBFChanNPC : ModNPC
{
	public const string ShopName = "Shop";

	private const string AssetDir = "GregTechCEuTerraria/Content/TerrariaCompat/NPCs/EBFChan/";

	public override string Texture => AssetDir + "EBFChan";

	public override void SetStaticDefaults()
	{
		Main.npcFrameCount[Type] = 22;

		NPCID.Sets.HatOffsetY[Type] = 4;

		NPCID.Sets.NPCBestiaryDrawModifiers drawModifiers = new() { Velocity = 1f, Direction = 1 };
		NPCID.Sets.NPCBestiaryDrawOffset.Add(Type, drawModifiers);

		Language.GetOrRegister("Mods.GregTechCEuTerraria.NPCs.EBFChanNPC.DisplayName", () => "EBF-chan");
		Language.GetOrRegister("Mods.GregTechCEuTerraria.NPCs.EBFChanNPC.Bestiary",
			() => "The spirit of a Electric Blast Furnace, settled and friendly now that her Fallen shell is dealt with. She runs a little stall of heat-treated ingredients - and is always hot to the touch.");
		Language.GetOrRegister("Mods.GregTechCEuTerraria.NPCs.EBFChanNPC.DeathMessage", () => "{0} let her coils go cold...");
		Language.GetOrRegister("Mods.GregTechCEuTerraria.NPCs.EBFChanNPC.Chat", () =>
			"""
			V Theres MORE TIPS button V

			Dont like microcrafting - install MagicStorage. Caution: magic storage cant do fluid crafting (e.g. treated wood) yet

			Dont like mining - check Mining Hammer, it mines fast

			Dont like primitive age - I boosted PBF speed so its crazy fast, you wont need to wait for steel

			How to see recipes - check TMI button on the right side of your inventory

			How to progress - check Questbook button on the right side of your inventory (inaccurate for now)

			How to intersect pipes - check Pipe Intersection item (crafted from stone)

			How to connect pipes - put pipe near machine and then click RMB onto it with empty hand

			How to connect wires - put behind machines

			How to build multiblocks - put multiblock controller, it will show ghost tiles

			How to get rubber - 1. from gel 2. cut trees using saw to get rubber wood, into extractor 3. from EBF Chan

			How to get sulfur - 1. from hell 2. from ore processing 3. from EBF Chan

			How to get ores - upper layers of caves spawn gt overworld ores, lower layers and hell spawn nether/end gt ores

			How to store lots of steam - check large steel tank

			How to get lots of resources - 1. steam/electric miner 2. Extractinator accepts pipes 3. Large Miner / Drilling Rig
			""");
		Language.GetOrRegister("Mods.GregTechCEuTerraria.NPCs.EBFChanNPC.MoreTips", () => "More tips");

		Language.GetOrRegister("Mods.GregTechCEuTerraria.NPCs.EBFChanNPC.Census.SpawnCondition", () => "{$Census.SpawnConditions.Guide}");
	}

	public override void SetDefaults()
	{
		NPC.townNPC = true;
		NPC.friendly = true;
		NPC.width = 18;
		NPC.height = 40;
		NPC.aiStyle = NPCAIStyleID.Passive;
		NPC.damage = 10;
		NPC.defense = 15;
		NPC.lifeMax = 250;
		NPC.HitSound = SoundID.NPCHit4;
		NPC.DeathSound = SoundID.NPCDeath14;
		NPC.knockBackResist = 0.5f;

		AnimationType = NPCID.Guide;
	}

	public override LocalizedText DeathMessage => Language.GetText("Mods.GregTechCEuTerraria.NPCs.EBFChanNPC.DeathMessage");

	public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
	{
		bestiaryEntry.Info.AddRange(new IBestiaryInfoElement[]
		{
			BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.Surface,
			new FlavorTextBestiaryInfoElement("Mods.GregTechCEuTerraria.NPCs.EBFChanNPC.Bestiary"),
		});
	}

	// Available from world start
	public override bool CanTownNPCSpawn(int numTownNPCs) => true;

	public override ITownNPCProfile TownNPCProfile() =>
		new Profiles.DefaultNPCProfile(Texture, ModContent.GetModHeadSlot(HeadTexture));

	private const int ChatLineBudget = 9; // panel hard-truncates at 10

	private static string[] _chatPages = Array.Empty<string>();
	private static int _chatPage;

	public override string GetChat()
	{
		_chatPages = BuildChatPages();
		_chatPage = 0;
		return _chatPages.Length > 0 ? _chatPages[0] : "";
	}

	public override void SetChatButtons(ref string button, ref string button2)
	{
		button = Language.GetTextValue("LegacyInterface.28"); // "Shop"
		if (_chatPages.Length > 1)
			button2 = Language.GetTextValue("Mods.GregTechCEuTerraria.NPCs.EBFChanNPC.MoreTips");
	}

	public override void OnChatButtonClicked(bool firstButton, ref string shop)
	{
		if (firstButton)
		{
			shop = ShopName;
			return;
		}

		if (_chatPages.Length == 0) return;
		_chatPage = (_chatPage + 1) % _chatPages.Length;
		Main.npcChatText = _chatPages[_chatPage];
	}

	private static string[] BuildChatPages()
	{
		string full = Language.GetTextValue("Mods.GregTechCEuTerraria.NPCs.EBFChanNPC.Chat");
		string[] tips = full.Replace("\r\n", "\n")
			.Split(new[] { "\n\n" }, StringSplitOptions.RemoveEmptyEntries);

		var pages = new List<string>();
		var sb = new StringBuilder();
		foreach (string raw in tips)
		{
			string tip = raw.Trim();
			if (tip.Length == 0) continue;

			if (sb.Length == 0)
			{
				sb.Append(tip);
				continue;
			}

			string candidate = sb + "\n\n" + tip;
			if (MeasureWrappedLines(candidate) > ChatLineBudget)
			{
				pages.Add(sb.ToString());
				sb.Clear();
				sb.Append(tip);
			}
			else
			{
				sb.Clear();
				sb.Append(candidate);
			}
		}
		if (sb.Length > 0) pages.Add(sb.ToString());
		if (pages.Count == 0) pages.Add(full);
		return pages.ToArray();
	}

	private static int MeasureWrappedLines(string text)
	{
		if (Main.dedServ) return 1;
		Terraria.Utils.WordwrapString(text, FontAssets.MouseText.Value, 460, 100, out int lines);
		return Math.Max(1, lines);
	}

	public override void AddShops()
	{
		var shop = new NPCShop(Type, ShopName);

		AddHydrogenCell(shop, Item.buyPrice(gold: 2));

		AddIfPresent(shop, "aluminium_dust", Item.buyPrice(silver: 20));
		AddIfPresent(shop, "iron_dust", Item.buyPrice(silver: 8));
		AddIfPresent(shop, "copper_dust", Item.buyPrice(silver: 8));
		AddIfPresent(shop, "tin_dust", Item.buyPrice(silver: 8));
		AddIfPresent(shop, "nickel_dust", Item.buyPrice(silver: 12));
		AddIfPresent(shop, "lead_dust", Item.buyPrice(silver: 10));
		AddIfPresent(shop, "sulfur_dust", Item.buyPrice(silver: 6));
		AddIfPresent(shop, "redstone_dust", Item.buyPrice(silver: 6));
		AddIfPresent(shop, "raw_rubber_dust", Item.buyPrice(silver: 15));

		AddIfPresent(shop, "steel_ingot", Item.buyPrice(gold: 10));
		AddIfPresent(shop, "aluminium_ingot", Item.buyPrice(gold: 50));
		AddIfPresent(shop, "kanthal_ingot", Item.buyPrice(platinum: 1));

		shop.Register();
	}

	private void AddHydrogenCell(NPCShop shop, int priceCopper)
	{
		if (!Mod.TryFind<ModItem>("fluid_cell", out var cellMi)) return;
		var it = new Item(cellMi.Type);
		FluidType? hydrogen = MaterialRegistry.All.TryGetValue("hydrogen", out var mat)
			? mat.FluidProperty?.Get() ?? mat.FluidProperty?.Fluids.FirstOrDefault()
			: null;
		if (it.ModItem is FluidCellItem cell && hydrogen != null)
			cell.Fill(new FluidStack(hydrogen, cell.Capacity), simulate: false);
		it.shopCustomPrice = priceCopper;
		shop.Add(it);
	}

	private void AddIfPresent(NPCShop shop, string itemId, int priceCopper)
	{
		if (Mod.TryFind<ModItem>(itemId, out var mi))
			shop.Add(new Item(mi.Type) { shopCustomPrice = priceCopper });
	}
}
