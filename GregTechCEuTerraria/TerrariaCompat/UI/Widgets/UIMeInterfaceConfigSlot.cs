#nullable enable
using GregTechCEuTerraria.AppliedEnergistics.Api.Stacks;
using GregTechCEuTerraria.AppliedEnergistics.Core;
using GregTechCEuTerraria.TerrariaCompat.AppliedEnergistics;
using GregTechCEuTerraria.TerrariaCompat.Net.Actions;
using GregTechCEuTerraria.TerrariaCompat.UI.MeCraft;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.Localization;
using Terraria.UI;
using Terraria.UI.Chat;

namespace GregTechCEuTerraria.TerrariaCompat.UI.Widgets;

public sealed class UIMeInterfaceConfigSlot : UIElement
{
	private readonly MeInterfaceMachine _iface;
	private readonly int _slot;
	private const float VanillaNativeSlotPixels = 52f;
	private static readonly Item[] _temp = { new() };

	public UIMeInterfaceConfigSlot(MeInterfaceMachine iface, int slot, int sizePx)
	{
		_iface = iface;
		_slot = slot;
		Width = StyleDimension.FromPixels(sizePx);
		Height = StyleDimension.FromPixels(sizePx);
	}

	public override void LeftMouseDown(UIMouseEvent evt)
	{
		base.LeftMouseDown(evt);
		var cursor = Main.mouseItem;
		if (!cursor.IsAir)
		{
			var key = AEItemKey.Of(cursor);
			if (key != null)
				MachineActions.Send(MeInterfaceAction.SetConfig(_slot, key, cursor.stack), _iface);
			return;
		}
		var existing = _iface.ConfigKeyAt(_slot);
		if (existing != null)
		{
			long cur = _iface.ConfigAmountAt(_slot);
			MeCraftSystem.OpenForAmount(existing, cur, 1, "Set Stocked Amount", "Set",
				"Amount of this item to keep stocked",
				amt => MachineActions.Send(MeInterfaceAction.SetConfig(_slot, existing, amt), _iface));
		}
		else
		{
			ItemPickerSystem.Open(
				itemType => MachineActions.Send(MeInterfaceAction.SetConfig(_slot, AEItemKey.OfType(itemType), 1), _iface),
				fluidId =>
				{
					var fluid = Api.Fluids.FluidRegistry.Get(fluidId);
					if (fluid != null)
						MachineActions.Send(MeInterfaceAction.SetConfig(_slot, AEFluidKey.Of(fluid), 1), _iface);
				});
		}
	}

	public override void RightMouseDown(UIMouseEvent evt)
	{
		base.RightMouseDown(evt);
		MachineActions.Send(MeInterfaceAction.SetConfig(_slot, null, 0), _iface);
	}

	protected override void DrawSelf(SpriteBatch sb)
	{
		var bounds = GetDimensions().ToRectangle();

		if (ItemDrag.TryDropItem(bounds, out int di))
			MachineActions.Send(MeInterfaceAction.SetConfig(_slot, AEItemKey.OfType(di), 1), _iface);
		else if (ItemDrag.TryDropFluid(bounds, out var df, out _))
		{
			var fluid = Api.Fluids.FluidRegistry.Get(df);
			if (fluid != null)
				MachineActions.Send(MeInterfaceAction.SetConfig(_slot, AEFluidKey.Of(fluid), 1), _iface);
		}

		sb.Draw(TextureAssets.MagicPixel.Value, bounds, new Color(24, 28, 56));

		var key = _iface.ConfigKeyAt(_slot);
		long amount = _iface.ConfigAmountAt(_slot);
		if (key is AEItemKey itemKey)
		{
			var stack = itemKey.GetReadOnlyStack().Clone();
			stack.stack = 1;
			_temp[0] = stack;
			float old = Main.inventoryScale;
			Main.inventoryScale = bounds.Width / VanillaNativeSlotPixels;
			try
			{
				if (IsMouseHovering) ItemSlot.MouseHover(_temp, ItemSlot.Context.CraftingMaterial, 0);
				ItemSlot.Draw(sb, _temp, ItemSlot.Context.CraftingMaterial, 0, new Vector2(bounds.X, bounds.Y));
			}
			finally { Main.inventoryScale = old; }
			SlotRender.DrawAmount(sb, bounds, key, amount);
		}
		else if (key is AEFluidKey fluidKey)
		{
			BrowserFluidSlot.Draw(sb, bounds, fluidKey.GetFluid());
			SlotRender.DrawAmount(sb, bounds, key, amount);
			if (IsMouseHovering) BrowserFluidSlot.EmitTooltip(fluidKey.GetFluid());
		}

		if (IsMouseHovering)
		{
			Main.LocalPlayer.mouseInterface = true;
			if (key is null)
				Main.instance.MouseText(Language.GetTextValue(AELocale.SelectItemFluid));
		}
	}
}
