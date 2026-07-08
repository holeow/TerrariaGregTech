#nullable enable
using System;
using GregTechCEuTerraria.TerrariaCompat.AppliedEnergistics;
using GregTechCEuTerraria.TerrariaCompat.Items.Patterns;
using GregTechCEuTerraria.TerrariaCompat.Machine;
using GregTechCEuTerraria.TerrariaCompat.Net.Actions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.UI;

namespace GregTechCEuTerraria.TerrariaCompat.UI.Widgets;

public sealed class UIPatternAccessSlot : UIElement
{
	private readonly MetaMachine _term;
	private readonly Point16 _providerPos;
	private readonly int _slot;
	private readonly Func<string> _query;
	private const float VanillaNativeSlotPixels = 52f;
	private static readonly Item[] _temp = { new() };

	public UIPatternAccessSlot(MetaMachine term, Point16 providerPos, int slot, int sizePx,
		Func<string> query)
	{
		_term = term;
		_providerPos = providerPos;
		_slot = slot;
		_query = query;
		Width = StyleDimension.FromPixels(sizePx);
		Height = StyleDimension.FromPixels(sizePx);
	}

	private Item Current()
	{
		if (TileEntity.ByPosition.TryGetValue(_providerPos, out var te)
			&& te is PatternProviderMachine p)
		{
			var slots = p.GetSlotGroup(SlotGroup.InventoryInput);
			if (slots is not null && _slot >= 0 && _slot < slots.Length) return slots[_slot];
		}
		return new Item();
	}

	private static bool ShiftHeld =>
		Main.keyState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift)
		|| Main.keyState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.RightShift);

	private static bool CtrlHeld =>
		Main.keyState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftControl)
		|| Main.keyState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.RightControl);

	public override void LeftMouseDown(UIMouseEvent evt)
	{
		base.LeftMouseDown(evt);
		if (CtrlHeld && Main.GameModeInfo.IsJourneyMode)
		{
			if (Current().IsAir || !Main.mouseItem.IsAir) return;
			MachineActions.Send(new MePatternAccessAction(_providerPos, _slot,
				MePatternAccessAction.Kind.CreativeDuplicate, Main.mouseItem), _term);
			SoundEngine.PlaySound(SoundID.Grab);
			return;
		}
		if (ShiftHeld)
		{
			if (Current().IsAir) return;
			MachineActions.Send(new MePatternAccessAction(_providerPos, _slot,
				MePatternAccessAction.Kind.ShiftClick, new Item()), _term);
			SoundEngine.PlaySound(SoundID.Grab);
			return;
		}
		if (Current().IsAir && Main.mouseItem.IsAir) return;
		MachineActions.Send(new MePatternAccessAction(_providerPos, _slot,
			MePatternAccessAction.Kind.PickupOrSetDown, Main.mouseItem), _term);
		SoundEngine.PlaySound(SoundID.Grab);
	}

	public override void RightMouseDown(UIMouseEvent evt)
	{
		base.RightMouseDown(evt);
		if (Current().IsAir && Main.mouseItem.IsAir) return;
		MachineActions.Send(new MePatternAccessAction(_providerPos, _slot,
			MePatternAccessAction.Kind.SplitOrPlaceSingle, Main.mouseItem), _term);
		SoundEngine.PlaySound(SoundID.Grab);
	}

	private bool Invalid()
	{
		if (TileEntity.ByPosition.TryGetValue(_providerPos, out var te) && te is PatternProviderMachine p)
		{
			var it = Current();
			if (!it.IsAir && it.ModItem is EncodedPatternItem e && e.Pattern != null)
				return !p.CanFulfill(e.Pattern);
		}
		return false;
	}

	private bool MatchesQuery()
	{
		string q = _query();
		if (string.IsNullOrEmpty(q)) return false;
		var it = Current();
		if (it.IsAir || it.ModItem is not EncodedPatternItem e || e.Pattern == null) return false;
		foreach (var (what, _) in e.Pattern.Outputs)
			if (what != null && what.GetDisplayName().ToLowerInvariant().Contains(q)) return true;
		return false;
	}

	protected override void DrawSelf(SpriteBatch sb)
	{
		var bounds = GetDimensions().ToRectangle();
		sb.Draw(TextureAssets.MagicPixel.Value, bounds, new Color(24, 28, 56));
		if (MatchesQuery())
			sb.Draw(TextureAssets.MagicPixel.Value, bounds, new Color(80, 200, 80) * 0.45f);

		_temp[0] = Current();
		bool hover = IsMouseHovering;
		float old = Main.inventoryScale;
		Main.inventoryScale = bounds.Width / VanillaNativeSlotPixels;
		try
		{
			if (hover && !_temp[0].IsAir)
			{
				ItemSlot.OverrideHover(_temp, ItemSlot.Context.ChestItem, 0);
				ItemSlot.MouseHover(_temp, ItemSlot.Context.ChestItem, 0);
			}
			ItemSlot.Draw(sb, _temp, ItemSlot.Context.ChestItem, 0, new Vector2(bounds.X, bounds.Y));
		}
		finally { Main.inventoryScale = old; }

		if (!_temp[0].IsAir && Invalid())
			sb.Draw(TextureAssets.MagicPixel.Value, bounds, new Color(200, 40, 40) * 0.45f);

		if (hover) Main.LocalPlayer.mouseInterface = true;
	}
}
