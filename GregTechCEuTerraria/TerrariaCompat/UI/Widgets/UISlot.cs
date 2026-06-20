#nullable enable
using GregTechCEuTerraria.TerrariaCompat.Machine;
using GregTechCEuTerraria.TerrariaCompat.Net.Actions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;

namespace GregTechCEuTerraria.TerrariaCompat.UI.Widgets;

public sealed class UISlot : UIElement
{
	public const int NativeUnscaledSize = 22;
	private const float VanillaNativeSlotPixels = 52f;

	private readonly MetaMachine _entity;
	private readonly SlotGroup _group;
	private readonly int _slotIndex;
	private readonly int _context;
	private readonly bool _isOutput;
	private readonly System.Func<bool>? _isBlocked;
	private readonly string? _emptyOverlayAsset;
	private ReLogic.Content.Asset<Texture2D>? _emptyOverlayTex;

	private readonly Item[]? _slotsForRender;

	public string? EmptyHint { get; set; }

	private readonly System.Func<bool>? _invalid;

	public UISlot(MetaMachine entity, SlotGroup group, int slotIndex,
		int context = ItemSlot.Context.ChestItem,
		System.Func<bool>? isBlocked = null,
		string? emptyOverlayAsset = null,
		System.Func<bool>? invalid = null)
	{
		_entity = entity;
		_group = group;
		_slotIndex = slotIndex;
		_context = context;
		_isOutput = group == SlotGroup.InventoryOutput;
		_isBlocked = isBlocked;
		_emptyOverlayAsset = emptyOverlayAsset;
		_invalid = invalid;
		_slotsForRender = entity.GetSlotGroup(group);
		Width = StyleDimension.FromPixels(NativeUnscaledSize);
		Height = StyleDimension.FromPixels(NativeUnscaledSize);
	}

	private static readonly Item[] _temp = { new() };

	protected override void DrawSelf(SpriteBatch spriteBatch)
	{
		if (_slotsForRender is null || _slotIndex >= _slotsForRender.Length) return;

		var bounds = GetDimensions().ToRectangle();

		_temp[0] = _slotsForRender[_slotIndex];

		float oldScale = Main.inventoryScale;
		Main.inventoryScale = bounds.Width / VanillaNativeSlotPixels;
		try
		{
			if (IsMouseHovering)
			{
				Main.LocalPlayer.mouseInterface = true;
				if (_temp[0].IsAir && EmptyHint is { } emptyHint)
						Main.instance.MouseText(emptyHint);
					else
					{
						ItemSlot.OverrideHover(_temp, _context, 0);
				ItemSlot.MouseHover(_temp, _context, 0);
					}
			}
			ItemSlot.Draw(spriteBatch, _temp, _context, 0, new Vector2(bounds.X, bounds.Y));

				if (!_temp[0].IsAir && _invalid is { } invalid && invalid())
					spriteBatch.Draw(Terraria.GameContent.TextureAssets.MagicPixel.Value, bounds,
						new Color(200, 40, 40) * 0.45f);

			if (_emptyOverlayAsset is { } asset && _temp[0].IsAir)
			{
				_emptyOverlayTex ??= ModContent.Request<Texture2D>(asset);
				if (_emptyOverlayTex?.Value is { } overlayTex)
				{
					float s = Main.inventoryScale;
					var dest = new Rectangle(
						bounds.X + (bounds.Width  - (int)(overlayTex.Width  * s)) / 2,
						bounds.Y + (bounds.Height - (int)(overlayTex.Height * s)) / 2,
						(int)(overlayTex.Width  * s),
						(int)(overlayTex.Height * s));
					spriteBatch.Draw(overlayTex, dest, Color.White);
				}
			}
		}
		finally
		{
			Main.inventoryScale = oldScale;
		}
	}

	public override void LeftMouseDown(UIMouseEvent evt)
	{
		base.LeftMouseDown(evt);
		Dispatch(left: true);
	}

	public override void RightMouseDown(UIMouseEvent evt)
	{
		base.RightMouseDown(evt);
		Dispatch(left: false);
	}

	private void Dispatch(bool left)
	{
		if (_slotsForRender is null || _slotIndex >= _slotsForRender.Length) return;

		if (_isBlocked is { } gate && gate()) return;

		bool shiftHeld = Main.keyState.IsKeyDown(Keys.LeftShift)
		              || Main.keyState.IsKeyDown(Keys.RightShift);
		var slotSnap   = _slotsForRender[_slotIndex];
		bool slotEmpty = slotSnap.IsAir;
		bool cursorHeld = !Main.mouseItem.IsAir;

		if (left && shiftHeld)
		{
			if (slotEmpty) return;
			int amount = SlotAction.FitCapacity(Main.LocalPlayer.inventory, slotSnap, 0, 50);
			if (amount <= 0) return;
			MachineActions.Send(new SlotAction(_group, _slotIndex, SlotAction.Kind.ShiftClickOut, amount), _entity);
			SoundEngine.PlaySound(SoundID.Grab);
			return;
		}

		if (cursorHeld)
		{
			if (_isOutput) return;
			if (!slotEmpty && (slotSnap.type != Main.mouseItem.type
			                   || !ItemLoader.CanStack(slotSnap, Main.mouseItem)))
				return;

			int count = left ? Main.mouseItem.stack : 1;
			var moving = Main.mouseItem.Clone();
			moving.stack = count;
			Main.mouseItem.stack -= count;
			if (Main.mouseItem.stack <= 0) Main.mouseItem.TurnToAir();

			MachineActions.Send(new SlotAction(_group, _slotIndex, SlotAction.Kind.Deposit, moving), _entity);
			SoundEngine.PlaySound(left ? SoundID.Grab : SoundID.MenuTick);
		}
		else
		{
			if (slotEmpty) return;
			MachineActions.Send(new SlotAction(_group, _slotIndex, SlotAction.Kind.Pickup, left ? 0 : 1), _entity);
			SoundEngine.PlaySound(SoundID.Grab);
		}
	}

}
