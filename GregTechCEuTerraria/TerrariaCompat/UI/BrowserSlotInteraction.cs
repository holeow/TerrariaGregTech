#nullable enable
using System.Collections.Generic;
using GregTechCEuTerraria.Api.Capability;
using GregTechCEuTerraria.Api.Fluids;
using GregTechCEuTerraria.TerrariaCompat.Capabilities;
using GregTechCEuTerraria.TerrariaCompat.Items.Fluids;
using Microsoft.Xna.Framework.Input;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace GregTechCEuTerraria.TerrariaCompat.UI;

// LMB / RMB -> how to obtain / used as ingredient
// Alt+LMB -> favorite (unfavorite inside the favorites pane)
// Ctrl+LMB / RMB -> Journey cheat
public static class BrowserSlotInteraction
{
	public const int TungstenSteelFluidCellCapacity = 512_000;

	public readonly struct Click
	{
		public bool Lmb  { get; init; }
		public bool Rmb  { get; init; }
		public bool Alt  { get; init; }
		public bool Ctrl { get; init; }
	}

	public static Click Poll()
	{
		var k = Main.keyState;
		return new Click
		{
			Lmb  = MouseClick.LeftPressed,
			Rmb  = MouseClick.RightPressed,
			Alt  = k.IsKeyDown(Keys.LeftAlt)     || k.IsKeyDown(Keys.RightAlt),
			Ctrl = k.IsKeyDown(Keys.LeftControl) || k.IsKeyDown(Keys.RightControl),
		};
	}

	public static Click PollReleased()
	{
		var k = Main.keyState;
		return new Click
		{
			Lmb  = MouseClick.LeftReleased,
			Rmb  = MouseClick.RightPressed,
			Alt  = k.IsKeyDown(Keys.LeftAlt)     || k.IsKeyDown(Keys.RightAlt),
			Ctrl = k.IsKeyDown(Keys.LeftControl) || k.IsKeyDown(Keys.RightControl),
		};
	}

	public static void HandleItem(Click c, Item display, bool inFavoritesPane,
		int? recipeAmount = null)
	{
		if (display is null || display.IsAir) return;
		bool cheat = (c.Lmb || c.Rmb) && c.Ctrl;
		if (cheat)
		{
			if (Main.GameModeInfo.IsJourneyMode)
			{
				int lmbCount = recipeAmount is > 0 ? recipeAmount.Value : 1;
				SpawnItemStack(display, c.Rmb ? MaxStackFor(display.type) : lmbCount);
			}
			return;
		}
		HandleItem(c, display.type, inFavoritesPane, recipeAmount);
	}

	public static void HandleItem(Click c, int itemType, bool inFavoritesPane,
		int? recipeAmount = null)
	{
		if (itemType <= 0) return;

		if (c.Lmb)
		{
			if (c.Alt)
			{
				FavoriteItem(itemType, inFavoritesPane);
			}
			else if (c.Ctrl)
			{
				if (Main.GameModeInfo.IsJourneyMode)
					SpawnItem(itemType, recipeAmount is > 0 ? recipeAmount.Value : 1);
			}
			else
			{
				HoverItemTracker.PushItem(itemType);
				GlobalRecipeBrowserSystem.OpenFiltered(itemType,
					GlobalRecipeBrowserState.BrowseFilter.Output);
			}
		}
		else if (c.Rmb)
		{
			if (c.Ctrl)
			{
				if (Main.GameModeInfo.IsJourneyMode) SpawnItem(itemType, MaxStackFor(itemType));
			}
			else
			{
				GlobalRecipeBrowserSystem.OpenFiltered(itemType,
					GlobalRecipeBrowserState.BrowseFilter.Input);
			}
		}
	}

	public static void HandleFluid(Click c, FluidType? fluid, int? recipeAmountMb,
		bool inFavoritesPane)
	{
		if (fluid is null) return;
		string id    = fluid.Id;
		string label = fluid.DisplayName;

		if (c.Lmb)
		{
			if (c.Alt)
			{
				FavoriteFluid(id, label, inFavoritesPane);
			}
			else if (c.Ctrl)
			{
				if (Main.GameModeInfo.IsJourneyMode)
					SpawnFluidCell(fluid, recipeAmountMb ?? TungstenSteelFluidCellCapacity);
			}
			else
			{
				HoverItemTracker.PushFluid(id);
				GlobalRecipeBrowserSystem.OpenFilteredFluid(id, label,
					GlobalRecipeBrowserState.BrowseFilter.Output);
			}
		}
		else if (c.Rmb)
		{
			if (c.Ctrl)
			{
				if (Main.GameModeInfo.IsJourneyMode)
					SpawnFluidCell(fluid, TungstenSteelFluidCellCapacity);
			}
			else
			{
				GlobalRecipeBrowserSystem.OpenFilteredFluid(id, label,
					GlobalRecipeBrowserState.BrowseFilter.Input);
			}
		}
	}

	public static void HandleNpc(Click c, int npcType)
	{
		if (npcType <= 0) return;
		if (!c.Ctrl || !Main.GameModeInfo.IsJourneyMode) return;
		if (c.Lmb || c.Rmb) SpawnNpc(npcType);
	}

	public static void HandleTag(Click c, string tagLabel, HashSet<int> members,
		int? recipeAmount = null)
	{
		if (members.Count == 0) return;
		if (c.Alt)
		{
			if (c.Lmb) FavoritesPlayer.Local.BringTagToFront(tagLabel, members);
			return;
		}
		if (c.Ctrl)
		{
			if ((c.Lmb || c.Rmb) && Main.GameModeInfo.IsJourneyMode)
			{
				int first = FirstMember(members);
				if (first > 0)
				{
					int lmbCount = recipeAmount is > 0 ? recipeAmount.Value : 1;
					SpawnItem(first, c.Rmb ? MaxStackFor(first) : lmbCount);
				}
			}
			return;
		}
		if (c.Lmb)
			GlobalRecipeBrowserSystem.OpenFilteredTag(tagLabel, members,
				GlobalRecipeBrowserState.BrowseFilter.Output);
		else if (c.Rmb)
			GlobalRecipeBrowserSystem.OpenFilteredTag(tagLabel, members,
				GlobalRecipeBrowserState.BrowseFilter.Input);
	}

	private static int FirstMember(HashSet<int> members)
	{
		int best = 0;
		foreach (int t in members)
			if (t > 0 && (best == 0 || t < best)) best = t;
		return best;
	}

	private static void FavoriteItem(int itemType, bool inFavoritesPane)
	{
		if (inFavoritesPane) FavoritesPlayer.Local.RemoveItem(itemType);
		else                 FavoritesPlayer.Local.BringItemToFront(itemType);
	}

	private static void FavoriteFluid(string id, string label, bool inFavoritesPane)
	{
		if (inFavoritesPane) FavoritesPlayer.Local.RemoveFluid(id);
		else                 FavoritesPlayer.Local.BringFluidToFront(id, label);
	}

	private static int MaxStackFor(int itemType)
	{
		if (ContentSamples.ItemsByType.TryGetValue(itemType, out var sample) && sample.maxStack > 0)
			return sample.maxStack;
		return 999;
	}

	private static void SpawnItem(int itemType, int stack)
	{
		if (stack <= 0) return;
		var src = new EntitySource_DebugCommand("GTBrowserSlotInteraction");
		global::GregTechCEuTerraria.TerrariaCompat.Utils.PlayerGive.Give(Main.LocalPlayer, src, itemType, stack);
	}

	private static void SpawnItemStack(Item template, int stack)
	{
		if (stack <= 0 || template is null || template.IsAir) return;
		template.stack = stack;
		var src = new EntitySource_DebugCommand("GTBrowserSlotInteraction");
		global::GregTechCEuTerraria.TerrariaCompat.Utils.PlayerGive.Give(Main.LocalPlayer, src, template);
	}

	private static void SpawnFluidCell(FluidType fluid, int amountMb)
	{
		if (amountMb <= 0) return;
		int cellItemType = ModContent.ItemType<TungstenSteelFluidCell>();
		int capped = amountMb > TungstenSteelFluidCellCapacity ? TungstenSteelFluidCellCapacity : amountMb;

		var item = new Item();
		item.SetDefaults(cellItemType);
		item.stack = 1;
		if (item.ModItem is FluidCellItem cell)
		{
			((IFluidHandlerItem)cell)
				.Fill(new FluidStack(fluid, capped), simulate: false);
		}
		var src = new EntitySource_DebugCommand("GTBrowserSlotInteraction");
		global::GregTechCEuTerraria.TerrariaCompat.Utils.PlayerGive.Give(Main.LocalPlayer, src, item);
	}

	private static void SpawnNpc(int npcType)
	{
		var p = Main.LocalPlayer;
		var src = new EntitySource_DebugCommand("GTBrowserSlotInteraction");
		int worldX = (int)p.Center.X;
		int worldY = (int)p.Center.Y - 32;
		NPC.NewNPC(src, worldX, worldY, npcType);
	}
}
