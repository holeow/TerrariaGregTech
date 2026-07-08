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

public sealed class UIMeStorageSlot : UIElement
{
	private readonly MeStorageMachine _store;
	private const float VanillaNativeSlotPixels = 52f;

	private static readonly Item[] _temp = { new() };

	public UIMeStorageSlot(MeStorageMachine store, int sizePx)
	{
		_store = store;
		Width = StyleDimension.FromPixels(sizePx);
		Height = StyleDimension.FromPixels(sizePx);
	}

	public override void LeftMouseDown(UIMouseEvent evt)
	{
		base.LeftMouseDown(evt);
		if (!Main.mouseItem.IsAir)
		{
			MachineActions.Send(new MeStorageAction(MeStorageAction.Op.Insert, Main.mouseItem), _store);
			SoundEngine.PlaySound(SoundID.Grab);
		}
		else if (_store.StoredTypeCount > 0)
		{
			MachineActions.Send(new MeStorageAction(MeStorageAction.Op.Dump), _store);
			SoundEngine.PlaySound(SoundID.Grab);
		}
	}

	protected override void DrawSelf(SpriteBatch spriteBatch)
	{
		var bounds = GetDimensions().ToRectangle();

		if (_store.StoredKey is AEFluidKey fk)
		{
			BrowserFluidSlot.Draw(spriteBatch, bounds, fk.GetFluid());
			if (_store.StoredAmount > 0)
			{
				string ftext = UINumberFormat.Fluid(_store.StoredAmount);
				var ffont = FontAssets.ItemStack.Value;
				var fsize = ChatManager.GetStringSize(ffont, ftext, new Vector2(0.7f));
				ChatManager.DrawColorCodedStringWithShadow(spriteBatch, ffont, ftext,
					new Vector2(bounds.Right - fsize.X - 4, bounds.Bottom - fsize.Y - 2),
					Color.White, 0f, Vector2.Zero, new Vector2(0.7f));
			}
			if (IsMouseHovering)
			{
				Main.LocalPlayer.mouseInterface = true;
				BrowserFluidSlot.EmitTooltip(fk.GetFluid(), (int)System.Math.Min(_store.StoredAmount, int.MaxValue));
			}
			return;
		}

		var stored = _store.FirstStoredStack();
		_temp[0] = stored.IsAir ? new Item() : stored.Clone();
		if (!_temp[0].IsAir) _temp[0].stack = 1;

		float oldScale = Main.inventoryScale;
		Main.inventoryScale = bounds.Width / VanillaNativeSlotPixels;
		try
		{
			if (IsMouseHovering)
			{
				Main.LocalPlayer.mouseInterface = true;
				if (!_temp[0].IsAir)
				{
					ItemSlot.OverrideHover(_temp, ItemSlot.Context.ChestItem, 0);
					ItemSlot.MouseHover(_temp, ItemSlot.Context.ChestItem, 0);
				}
			}
			ItemSlot.Draw(spriteBatch, _temp, ItemSlot.Context.ChestItem, 0,
				new Vector2(bounds.X, bounds.Y));

			long amount = _store.FirstStoredAmount();
			if (!_temp[0].IsAir && amount > 0)
			{
				string text = UINumberFormat.Count(amount);
				DynamicSpriteFont font = FontAssets.ItemStack.Value;
				const float overlayScale = 0.7f;
				var size = ChatManager.GetStringSize(font, text, new Vector2(overlayScale));
				var pos = new Vector2(bounds.Right - size.X - 4, bounds.Bottom - size.Y - 2);
				ChatManager.DrawColorCodedStringWithShadow(spriteBatch, font, text, pos, Color.White, 0f,
					Vector2.Zero, new Vector2(overlayScale));
			}
		}
		finally
		{
			Main.inventoryScale = oldScale;
		}
	}
}
