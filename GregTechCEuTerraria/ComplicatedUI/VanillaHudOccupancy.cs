#nullable enable
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace GregTechCEuTerraria.TerrariaCompat.UI;

public sealed class VanillaHudOccupancy : ModSystem
{
	public const string ProviderKey = "vanilla_hud";

	public static Rectangle InventoryRegion => new(16, 16, 612, 286);

	private const int BottomButtonSize = 52;
	private const int BottomButtonInset = 8;
	public const int BottomReserve = BottomButtonSize + BottomButtonInset;

	public override void Load()
	{
		if (Main.dedServ) return;
		ScreenLayout.RegisterProvider(ProviderKey, Occupied);
	}

	public override void Unload() => ScreenLayout.RemoveProvider(ProviderKey);

	private static IEnumerable<Rectangle> Occupied()
	{
		if (!Main.playerInventory) yield break;

		var player = Main.LocalPlayer;
		int invBottom = Main.instance.invBottom;

		yield return InventoryRegion;

		if (player.chest != -1 || Main.npcShop > 0)
			yield return new Rectangle(73, invBottom, 424, 170);

		bool craftingArea = !Main.InReforgeMenu && !Main.InGuideCraftMenu;

		if (Main.InReforgeMenu)
			yield return new Rectangle(40, 258, 190, 100);

		int adjY = (Main.screenHeight - 600) / 2;
		int middleY = (int)(Main.screenHeight / 600f * 250f);
		if (Main.screenHeight < 700) { adjY = (Main.screenHeight - 508) / 2; middleY = (int)(Main.screenHeight / 600f * 200f); }
		else if (Main.screenHeight < 850) middleY = (int)(Main.screenHeight / 600f * 225f);

		if (Main.InGuideCraftMenu)
			yield return new Rectangle(60, 325 + adjY, 84, 72);

		if (craftingArea)
		{
			int top = 410 + adjY - middleY;
			int bottom = 410 + adjY + middleY;
			yield return new Rectangle(10, top, 120, bottom - top);
		}

		var screen = ScreenLayout.Screen;
		yield return new Rectangle(screen.Right - BottomReserve, screen.Bottom - BottomReserve,
			BottomButtonSize, BottomButtonSize);
	}
}
