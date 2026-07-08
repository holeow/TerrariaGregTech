#nullable enable
using GregTechCEuTerraria.AppliedEnergistics.Api.Stacks;
using GregTechCEuTerraria.TerrariaCompat.AppliedEnergistics;
using GregTechCEuTerraria.TerrariaCompat.Net.Actions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.UI;
using Terraria.UI.Chat;

namespace GregTechCEuTerraria.TerrariaCompat.UI.Widgets;

public sealed class UIMeInterfaceStorageSlot : UIElement
{
	private readonly MeInterfaceMachine _iface;
	private readonly int _slot;
	private const float VanillaNativeSlotPixels = 52f;
	private static readonly Item[] _temp = { new() };

	public UIMeInterfaceStorageSlot(MeInterfaceMachine iface, int slot, int sizePx)
	{
		_iface = iface;
		_slot = slot;
		Width = StyleDimension.FromPixels(sizePx);
		Height = StyleDimension.FromPixels(sizePx);
	}

	public override void LeftMouseDown(UIMouseEvent evt)
	{
		base.LeftMouseDown(evt);
		var stack = _iface.StorageStackAt(_slot);
		if (!Main.mouseItem.IsAir || stack?.What is AEItemKey)
			MachineActions.Send(MeInterfaceAction.Pickup(_slot, Main.mouseItem), _iface);
	}

	public override void RightMouseDown(UIMouseEvent evt)
	{
		base.RightMouseDown(evt);
		var stack = _iface.StorageStackAt(_slot);
		if (!Main.mouseItem.IsAir || stack?.What is AEItemKey)
			MachineActions.Send(MeInterfaceAction.SplitOrPlaceSingle(_slot, Main.mouseItem), _iface);
	}

	protected override void DrawSelf(SpriteBatch sb)
	{
		var bounds = GetDimensions().ToRectangle();
		sb.Draw(TextureAssets.MagicPixel.Value, bounds, new Color(20, 22, 34));

		var stack = _iface.StorageStackAt(_slot);
		if (stack?.What is AEFluidKey fk)
		{
			BrowserFluidSlot.Draw(sb, bounds, fk.GetFluid());
			SlotRender.DrawAmount(sb, bounds, stack.What, stack.Amount);
			if (IsMouseHovering)
			{
				Main.LocalPlayer.mouseInterface = true;
				BrowserFluidSlot.EmitTooltip(fk.GetFluid(), (int)System.Math.Min(stack.Amount, int.MaxValue));
			}
			return;
		}

		if (stack?.What is AEItemKey itemKey)
		{
			var disp = itemKey.GetReadOnlyStack().Clone();
			disp.stack = 1;
			_temp[0] = disp;
			float old = Main.inventoryScale;
			Main.inventoryScale = bounds.Width / VanillaNativeSlotPixels;
			try
			{
				if (IsMouseHovering)
				{
					Main.LocalPlayer.mouseInterface = true;
					ItemSlot.OverrideHover(_temp, ItemSlot.Context.ChestItem, 0);
					ItemSlot.MouseHover(_temp, ItemSlot.Context.ChestItem, 0);
				}
				ItemSlot.Draw(sb, _temp, ItemSlot.Context.ChestItem, 0, new Vector2(bounds.X, bounds.Y));
			}
			finally { Main.inventoryScale = old; }
			SlotRender.DrawAmount(sb, bounds, stack.What, stack.Amount);
		}
	}
}
