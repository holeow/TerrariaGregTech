#nullable enable
using GregTechCEuTerraria.Api.Capability;
using GregTechCEuTerraria.AppliedEnergistics.Api.Stacks;
using GregTechCEuTerraria.TerrariaCompat.UI.Widgets;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.UI;

namespace GregTechCEuTerraria.TerrariaCompat.UI.MeBus;

public sealed class MeBusFilterSlot : UIElement
{
	private readonly MeBusSettingsState _state;
	private readonly IODirection _side;
	private readonly int _slot;
	private const float VanillaNativeSlotPixels = 52f;
	private static readonly Item[] _temp = { new() };

	public MeBusFilterSlot(MeBusSettingsState state, IODirection side, int slot, int sizePx)
	{
		_state = state;
		_side = side;
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
			_state.SetFilterSlot(_side, _slot, AEItemKey.Of(cursor));
			return;
		}
		ItemPickerSystem.Open(
			itemType => _state.SetFilterSlot(_side, _slot, AEItemKey.OfType(itemType)),
			fluidId =>
			{
				var fluid = Api.Fluids.FluidRegistry.Get(fluidId);
				if (fluid != null) _state.SetFilterSlot(_side, _slot, AEFluidKey.Of(fluid));
			});
	}

	public override void RightMouseDown(UIMouseEvent evt)
	{
		base.RightMouseDown(evt);
		_state.SetFilterSlot(_side, _slot, null);
	}

	protected override void DrawSelf(SpriteBatch sb)
	{
		var bounds = GetDimensions().ToRectangle();

		if (ItemDrag.TryDropItem(bounds, out int di))
			_state.SetFilterSlot(_side, _slot, AEItemKey.OfType(di));
		else if (ItemDrag.TryDropFluid(bounds, out var df, out _))
		{
			var fluid = Api.Fluids.FluidRegistry.Get(df);
			if (fluid != null) _state.SetFilterSlot(_side, _slot, AEFluidKey.Of(fluid));
		}

		var px = TextureAssets.MagicPixel.Value;
		sb.Draw(px, bounds, new Color(24, 28, 56));

		var key = _state.FilterKeyAt(_side, _slot);
		string? name = null;
		if (key is AEItemKey itemKey)
		{
			var stack = itemKey.GetReadOnlyStack().Clone();
			stack.stack = 1;
			_temp[0] = stack;
			name = stack.Name;
			float old = Main.inventoryScale;
			Main.inventoryScale = bounds.Width / VanillaNativeSlotPixels;
			try { ItemSlot.Draw(sb, _temp, ItemSlot.Context.CraftingMaterial, 0, new Vector2(bounds.X, bounds.Y)); }
			finally { Main.inventoryScale = old; }
		}
		else if (key is AEFluidKey fluidKey)
		{
			var fluid = fluidKey.GetFluid();
			BrowserFluidSlot.Draw(sb, bounds, fluid);
			name = fluid.DisplayName;
		}

		PhantomSlotChrome.DrawHoverBorder(sb, bounds, IsMouseHovering);

		if (IsMouseHovering)
		{
			Main.LocalPlayer.mouseInterface = true;
			Main.instance.MouseText(key == null
				? PhantomSlotChrome.EmptyTooltip(PhantomSlotChrome.Kind.ItemOrFluid)
				: PhantomSlotChrome.FilledTooltip(name!, null, canSetAmount: false));
		}
	}
}
