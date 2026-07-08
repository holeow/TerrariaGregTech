#nullable enable
using GregTechCEuTerraria.TerrariaCompat.Lore;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace GregTechCEuTerraria.TerrariaCompat.Items;

public sealed class HistoryOfLogisticsItem : ModItem
{
	public override string Name => "the_history_of_logistics";
	public override string Texture => "GregTechCEuTerraria/Content/TerrariaCompat/TooManyItemsItem";

	private const string LoreTitle = "The History of Logistics";

	private const string LoreBody =
@"I want people to know the history of logistics mods because those people throughout all these years have been literally changing how we approach to factory games gamedesign overall (and basically invented factory games as a genre if you think about it).

(I checked stuff through forums/wikis/etc, but there still can be minor mistakes)

In early versions of Minecraft, there were no logistics options besides minecarts with chests

In April 2011 (MC Beta1.4), [c/FFC94A:SpaceToad] has created BuildCraft that added item pipes, filtering and two months later added autocrafting workbench. That allowed making full passive factory setups but required manual routing setups for everything

In October 2011 (MC Beta1.8.1), [c/FFC94A:Eloraam] has updated RedPower to version 2 and added pneumatic tubes which worked by different principles - they were aware of filtered destinations, and that approach is mostly used in logistical mods nowadays because it allows more control over the whole pipe network

In November 2011 (MC Beta1.8.1), [c/FFC94A:Krapht] has created Logistics Pipes, an addon for BuildCraft that first added storage buses concept and storage terminal with ondemand crafting requests, this concept mostly stays the same even nowadays but most people think its AE that introduced it, Stigler's law in action...

In May 2012 (MC 1.2.5) [c/FFC94A:Technic team] has created Tekkit Classic modpack which was one of the first modpacks that popularized putting many tech mods together and the demand on truly universal logistics solution of massive scale started to grow. Developer of this port has started playing modded minecraft specifically from this version of Tekkit by the way

In December 2012 (MC 1.4.7) [c/FFC94A:AlgorithmX2] has created Applied Energistics, it standartized storage terminal and pattern encoding/pattern providers and it didnt change much after that point. Adding new recipe autocraft has become a process that took a couple of clicks and allowed complex microcrafting-centered mods to appear

In March 2013 (MC 1.5) hoppers were added to minecraft (probably inspired by hoppers from Better Than Wolves mod by [c/FFC94A:FlowerChild])

In August 2013 (MC 1.6.4) [c/FFC94A:CrazyPants] has created Ender IO which allowed more compact setups as you can put several pipes, and (alongside AE) was one of the first mods to get rid of calculating actual item transport and connect input and outputs directly on a logical level. Til this days it stays as one of the most convenient ways of handling local subsystems automation

In March 2014 (MC 1.7.2) [c/FFC94A:AlgorithmX2] has released Applied Energistics 2 which was a full rewrite and added channels, p2p, crafting plans, AE2 has became the ultimate storage mod that's integrated to 95% of tech modpacks at this point. In september [c/FFC94A:AlgorithmX2] has open sourced it and left and after that [c/FFC94A:yueh], [c/FFC94A:thatsIch] and others has continued to develop the mod to the newer versions

Its 2026 now, and its completely obvious that AE2 has won the logistics arms race and emerged into its own style of gameplay and consistently stays popular and some mods explicitly design their gameplay around features AE2 provides (hi GT Stocking Bus). These 10 years have shown that AE/Logistical Pipes is a wonderful thing as a concept and doesnt really have any alternatives that are as powerful and universal


There are other things that have affected Minecraft logistics in some way, like:

MineFactory by [c/FFC94A:Feanorith] (2011) and Immersive Engineering by [c/FFC94A:BluSunrize] (2015) introduces conveyors which moved physical entities but haven't really become popular in minecraft until Create came out

Mekanism by [c/FFC94A:aidancbrady] (2012) was one of the earliest mods to properly unify the inter-mod transport of all the 3 main matter types

Steve's Factory Manager by [c/FFC94A:Vswe] (2013) allowed node graph programmable logistics

Thermal Expansion by [c/FFC94A:Team CoFH] (2013) has lead to standardization of many modded concepts and interactions

Forge Capability system by [c/FFC94A:Forge team] (2016) allowed to completely unify inter-mod logistics

Refined Storage by [c/FFC94A:raoulvdberge] (2016) provided a casual alternative to AE2 while keeping its basic functionality

XNet by [c/FFC94A:McJty] (2016) took the idea of p2p transport of EnderIO to extreme, and centralized IO setup

Integrated Dynamics/Tunnels by [c/FFC94A:kroeser] (2016) allowed doing arbitrary execution with complex card expressions

Tom's Simple Storage Mod by [c/FFC94A:tom5454] (2019) and Ars Nouveau Storage by [c/FFC94A:baileyholl] (2022) added even more casual storage with terminal which fits the early game gap where AE2 is too expensive

Create by [c/FFC94A:simibubi] (2019) allowed more immersive and vanilla-friendly transport solutions. Later on Create Frogports allowed making complex passive automation setups

Laser IO by [c/FFC94A:Direwolf20] (2022) allowed compact local logistics experience similar to EnderIO but taking even less space";

	public override void SetStaticDefaults()
	{
		Language.GetOrRegister($"Mods.GregTechCEuTerraria.Items.{Name}.DisplayName",
			() => LoreTitle);
		Language.GetOrRegister($"Mods.GregTechCEuTerraria.Items.{Name}.Tooltip",
			() => "Right-click to read");
		Language.GetOrRegister($"Mods.GregTechCEuTerraria.Items.{Name}.Lore",
			() => LoreBody);
	}

	public override void SetDefaults()
	{
		Item.width = 32;
		Item.height = 32;
		Item.maxStack = 1;
		Item.rare = ItemRarityID.Cyan;
		Item.value = 0;
		Item.useStyle = ItemUseStyleID.HoldUp;
		Item.useTime = 20;
		Item.useAnimation = 20;
		Item.autoReuse = false;
		Item.consumable = false;
	}

	public override bool AltFunctionUse(Player player) => true;

	public override bool CanUseItem(Player player) => player.altFunctionUse == 2;

	public override bool? UseItem(Player player)
	{
		if (player.altFunctionUse != 2 || player.whoAmI != Main.myPlayer) return null;
		OpenLore();
		return true;
	}

	public override bool CanRightClick() => true;

	public override void RightClick(Player player) => OpenLore();

	public override bool ConsumeItem(Player player) => false;

	private void OpenLore()
	{
		if (LoreUISystem.IsOpen) return;
		string body = Language.GetTextValue($"Mods.GregTechCEuTerraria.Items.{Name}.Lore");
		string[] lines = body.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');
		LoreUISystem.Open(LoreTitle, lines);
	}
}
