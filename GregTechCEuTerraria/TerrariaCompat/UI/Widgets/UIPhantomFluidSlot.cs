#nullable enable
using GregTechCEuTerraria.AppliedEnergistics.Api.Stacks;
using GregTechCEuTerraria.Api.Cover;
using GregTechCEuTerraria.Api.Fluids;
using GregTechCEuTerraria.TerrariaCompat.Machine;
using GregTechCEuTerraria.TerrariaCompat.Machine.Rendering;
using GregTechCEuTerraria.TerrariaCompat.Net.Actions;
using GregTechCEuTerraria.TerrariaCompat.UI.MeCraft;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.UI;

namespace GregTechCEuTerraria.TerrariaCompat.UI.Widgets;

public sealed class UIPhantomFluidSlot : UIElement
{
	public const int NativeUnscaledSize = 22;

	private readonly ICoverable _entity;
	private readonly CoverSide _side;
	private readonly int _index;

	private bool AmountMatters => (_entity.GetCoverAtSide(_side)?.UiFluidFilter?.MaxStackSize ?? 1) > 1;


	public UIPhantomFluidSlot(ICoverable entity, CoverSide side, int index)
	{
		_entity = entity;
		_side = side;
		_index = index;
		Width = StyleDimension.FromPixels(NativeUnscaledSize);
		Height = StyleDimension.FromPixels(NativeUnscaledSize);
	}

	protected override void DrawSelf(SpriteBatch spriteBatch)
	{
		var filter = _entity.GetCoverAtSide(_side)?.UiFluidFilter;
		FluidStack stack = filter is not null && _index < filter.Matches.Length
			? filter.Matches[_index] : FluidStack.Empty;

		var bounds = GetDimensions().ToRectangle();
		var tex = TextureAssets.MagicPixel.Value;

		if (ItemDrag.TryDropFluid(bounds, out var droppedFluid, out _))
		{
			CoverActions.Send(CoverFilterAction.MatcherSetFluid(_side, _index, droppedFluid), _entity);
			SoundEngine.PlaySound(SoundID.MenuTick);
		}

		spriteBatch.Draw(tex, bounds, new Color(25, 30, 50) * 0.9f);

		if (!stack.IsEmpty)
		{
			var inner = new Rectangle(bounds.X + 2, bounds.Y + 2, bounds.Width - 4, bounds.Height - 4);
			if (!FluidIconRenderer.Draw(spriteBatch, stack.Type!, inner))
				spriteBatch.Draw(tex, inner, FluidIconRenderer.RgbColor(stack.Type!.Color));
		}

		PhantomSlotChrome.DrawHoverBorder(spriteBatch, bounds, IsMouseHovering);

		if (!stack.IsEmpty && AmountMatters)
		{
			string txt = UINumberFormat.Fluid(stack.Amount);
			var font = FontAssets.ItemStack.Value;
			const float scale = 0.55f;
			var size = font.MeasureString(txt) * scale;
			var pos = new Vector2(
				bounds.Right - size.X - 2,
				bounds.Bottom - size.Y - 1);
			Terraria.Utils.DrawBorderString(spriteBatch, txt, pos, Color.White, scale);
		}

		if (IsMouseHovering)
		{
			Main.LocalPlayer.mouseInterface = true;
			Main.LocalPlayer.cursorItemIconEnabled = false;
			Main.instance.MouseText(BuildTooltip(stack, AmountMatters));
		}
	}

	private static string BuildTooltip(FluidStack stack, bool amountShown)
	{
		if (stack.IsEmpty)
			return PhantomSlotChrome.EmptyTooltip(PhantomSlotChrome.Kind.Fluid);
		string? amount = amountShown ? stack.Amount.ToString("N0") + " mB" : null;
		return PhantomSlotChrome.FilledTooltip(stack.Type!.DisplayName, amount, amountShown);
	}

	public override void LeftMouseDown(UIMouseEvent evt)  { base.LeftMouseDown(evt);  HandleLeft(); }
	public override void RightMouseDown(UIMouseEvent evt) { base.RightMouseDown(evt); HandleClear(); }

	private void HandleLeft()
	{
		var filter = _entity.GetCoverAtSide(_side)?.UiFluidFilter;
		FluidStack cur = filter is not null && _index < filter.Matches.Length
			? filter.Matches[_index] : FluidStack.Empty;

		if (!Main.mouseItem.IsAir)
		{
			CoverActions.Send(
				CoverFilterAction.Matcher(_side, fluid: true, _index, 0, false, Main.mouseItem), _entity);
			SoundEngine.PlaySound(SoundID.MenuTick);
			return;
		}

		if (cur.IsEmpty)
		{
			ItemPickerSystem.Open(
				_ => { },
				fluidId => CoverActions.Send(CoverFilterAction.MatcherSetFluid(_side, _index, fluidId), _entity),
				allowedItems: System.Array.Empty<int>());
			SoundEngine.PlaySound(SoundID.MenuTick);
			return;
		}

		if (!AmountMatters) return;

		MeCraftSystem.OpenForAmount(AEFluidKey.Of(cur.Type!), cur.Amount, 1, "Set Amount", "Set",
			"Amount this slot matches",
			amt => CoverActions.Send(CoverFilterAction.MatcherSetAmount(_side, fluid: true, _index, amt), _entity),
			parentLayer: UILayers.TopModal);
		SoundEngine.PlaySound(SoundID.MenuTick);
	}

	private void HandleClear()
	{
		CoverActions.Send(
			CoverFilterAction.Matcher(_side, fluid: true, _index, 2, false, new Item()), _entity);
		SoundEngine.PlaySound(SoundID.MenuTick);
	}
}
