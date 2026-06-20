#nullable enable
using System;
using System.Collections.Generic;
using GregTechCEuTerraria.TerrariaCompat.Capabilities;
using GregTechCEuTerraria.TerrariaCompat.Items.Fluids;
using Microsoft.Xna.Framework.Input;
using Terraria;
using Terraria.GameInput;
using Terraria.Localization;
using Terraria.ModLoader;

namespace GregTechCEuTerraria.TerrariaCompat.UI;

public sealed class RecipeBrowserKeybinds : ModSystem
{
	public static ModKeybind? HowToObtain;
	public static ModKeybind? UsedAsIngredient;

	private static readonly InputMode[] KeyboardModes = { InputMode.Keyboard, InputMode.KeyboardUI };

	private bool _ownObtain;
	private bool _ownUsed;

	public override void Load()
	{
		Language.GetOrRegister(
			"Mods.GregTechCEuTerraria.Keybinds.RecipeBrowserHowToObtain.DisplayName",
			() => "Recipe browser - how to obtain hovered item");
		Language.GetOrRegister(
			"Mods.GregTechCEuTerraria.Keybinds.RecipeBrowserUsedAsIngredient.DisplayName",
			() => "Recipe browser - recipes that use hovered item");

		HowToObtain      = KeybindLoader.RegisterKeybind(Mod, "RecipeBrowserHowToObtain", Keys.R);
		UsedAsIngredient = KeybindLoader.RegisterKeybind(Mod, "RecipeBrowserUsedAsIngredient", Keys.U);
	}

	public override void Unload()
	{
		HowToObtain = null;
		UsedAsIngredient = null;
	}

	public override void PostUpdateInput()
	{
		if (Main.dedServ) return;
		Handle(HowToObtain, GlobalRecipeBrowserState.BrowseFilter.Output, ref _ownObtain);
		Handle(UsedAsIngredient, GlobalRecipeBrowserState.BrowseFilter.Input, ref _ownUsed);
	}

	private void Handle(ModKeybind? kb, GlobalRecipeBrowserState.BrowseFilter dir, ref bool owned)
	{
		if (kb is null) return;

		bool held, justPressed;
		try { held = kb.Current; justPressed = kb.JustPressed; }
		catch (KeyNotFoundException) { return; }

		if (!held) owned = false;

		if (justPressed && TryOpenBrowser(dir))
			owned = true;

		if (owned && held)
			ConsumeKey(kb);
	}

	private static bool TryOpenBrowser(GlobalRecipeBrowserState.BrowseFilter dir)
	{
		if (BrowserHover.Fresh)
		{
			if (BrowserHover.TagItems is not null && BrowserHover.TagLabel is not null)
			{
				GlobalRecipeBrowserSystem.OpenFilteredTag(
					BrowserHover.TagLabel, BrowserHover.TagItems, dir);
				return true;
			}
			if (BrowserHover.ItemType > 0)
			{
				GlobalRecipeBrowserSystem.OpenFiltered(BrowserHover.ItemType, dir);
				return true;
			}
			if (BrowserHover.FluidId is not null)
			{
				GlobalRecipeBrowserSystem.OpenFilteredFluid(
					BrowserHover.FluidId, BrowserHover.FluidLabel ?? BrowserHover.FluidId, dir);
				return true;
			}
			return false;
		}

		Item h = Main.HoverItem;
		if (h is null || h.IsAir) return false;
		if (TryResolveHoveredFluid(h, out string? fluidId, out string? label))
		{
			GlobalRecipeBrowserSystem.OpenFilteredFluid(fluidId!, label!, dir);
			return true;
		}
		GlobalRecipeBrowserSystem.OpenFiltered(h.type, dir);
		return true;
	}

	private static bool TryResolveHoveredFluid(Item item, out string? fluidId, out string? label)
	{
		var vanilla = VanillaFluidBridge.StackFor(item.type);
		if (!vanilla.IsEmpty)
		{
			fluidId = vanilla.Type!.Id;
			label   = vanilla.Type.DisplayName;
			return true;
		}
		if (item.ModItem is FluidBucketItem bucket && bucket.Fluid is { } fluid)
		{
			fluidId = fluid.Id;
			label   = fluid.DisplayName;
			return true;
		}
		if (item.ModItem is Api.Capability.IFluidHandlerItem container)
		{
			var stack = container.GetTank(0);
			if (!stack.IsEmpty)
			{
				fluidId = stack.Type!.Id;
				label   = stack.Type.DisplayName;
				return true;
			}
		}
		fluidId = null;
		label   = null;
		return false;
	}

	private static void ConsumeKey(ModKeybind kb)
	{
		foreach (var mode in KeyboardModes)
		{
			List<string> keys;
			try { keys = kb.GetAssignedKeys(mode); }
			catch { continue; }
			ConsumePhysicalKeys(keys, mode);
		}
	}

	private static void ConsumePhysicalKeys(ICollection<string> physicalKeys, InputMode mode)
		=> ModalEscape.ConsumePhysicalKeys(physicalKeys, mode);
}
