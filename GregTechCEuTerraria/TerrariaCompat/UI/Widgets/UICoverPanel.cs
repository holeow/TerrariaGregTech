#nullable enable
using GregTechCEuTerraria.Api.Cover;
using GregTechCEuTerraria.TerrariaCompat.Items.Covers;
using GregTechCEuTerraria.TerrariaCompat.Machine;
using GregTechCEuTerraria.TerrariaCompat.Net.Actions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;

namespace GregTechCEuTerraria.TerrariaCompat.UI.Widgets;

public sealed class UICoverPanel : UIElement
{
	public const int Cell = 18;
	public const int ClusterSize = Cell * 3;

	private const float VanillaNativeSlotPixels = 52f;

	private readonly MetaMachine _entity;
	private readonly System.Action<CoverSide>? _onOpenSettings;

	private readonly Item[] _cellItem = new Item[1];
	private static readonly Item Empty = new();

	private static Asset<Texture2D>? _slotOverlay;

	public UICoverPanel(MetaMachine entity, System.Action<CoverSide>? onOpenSettings = null)
	{
		_entity = entity;
		_onOpenSettings = onOpenSettings;
		Width = StyleDimension.FromPixels(ClusterSize);
		Height = StyleDimension.FromPixels(ClusterSize);
	}

	private static CoverSide? CellFor(int col, int row) => (col, row) switch
	{
		(1, 0) => CoverSide.Up,
		(0, 1) => CoverSide.Left,
		(2, 1) => CoverSide.Right,
		(1, 2) => CoverSide.Down,
		_      => null,
	};

	protected override void DrawSelf(SpriteBatch spriteBatch)
	{
		LoadAssets();
		var bounds = GetDimensions().ToRectangle();
		float cellW = bounds.Width / 3f;
		float cellH = bounds.Height / 3f;
		var mouse = ModalEscape.PollCursor();

		bool leftPress  = MouseClick.LeftPressed;
		bool rightPress = MouseClick.RightPressed;
		CoverSide? hovered = null;

		float oldScale = Main.inventoryScale;
		try
		{
			for (int row = 0; row < 3; row++)
			for (int col = 0; col < 3; col++)
			{
				var side = CellFor(col, row);
				if (side is null) continue;
				var cellRect = new Rectangle(
					bounds.X + (int)(col * cellW),
					bounds.Y + (int)(row * cellH),
					(int)cellW, (int)cellH);
				if (cellRect.Contains(mouse)) hovered = side;
				DrawCell(spriteBatch, cellRect, side.Value);
			}
		}
		finally
		{
			Main.inventoryScale = oldScale;
		}

		if (hovered is not null)
		{
			Main.LocalPlayer.mouseInterface = true;
			var cover = _entity.GetCoverAtSide(hovered.Value);
			string tip;
			if (cover is null)
			{
				tip = $"{hovered.Value} cover slot\n(empty - hold a cover and click to attach)";
			}
			else
			{
				tip = $"{hovered.Value} cover: {cover.AttachItem.Name}";
				var status = cover.GetStatusText();
				if (!string.IsNullOrEmpty(status)) tip += "\n" + status;
			}
			Main.instance.MouseText(tip);
			if (leftPress)  HandleLeftClick(hovered.Value, cover);
			if (rightPress) HandleRightClick(hovered.Value, cover);
		}
	}

	private void DrawCell(SpriteBatch sb, Rectangle dest, CoverSide side)
	{
		if (_slotOverlay?.Value is { } overlay)
			sb.Draw(overlay, dest, Color.White);

		var cover = _entity.GetCoverAtSide(side);
		_cellItem[0] = cover?.AttachItem ?? Empty;

		Main.inventoryScale = dest.Width / VanillaNativeSlotPixels;
		ItemSlot.Draw(sb, _cellItem, ItemSlot.Context.ChestItem, 0,
			new Vector2(dest.X, dest.Y));
	}

	private void HandleLeftClick(CoverSide side, CoverBehavior? cover)
	{
		if (cover is null)
		{
			if (Main.mouseItem.ModItem is not CoverItem coverItem) return;
			CoverActions.Send(CoverAction.Place(side, coverItem.CoverId, Main.mouseItem), _entity);
			SoundEngine.PlaySound(SoundID.Grab);
		}
		else if (Main.mouseItem.IsAir)
		{
			CoverActions.Send(CoverAction.Remove(side), _entity);
			SoundEngine.PlaySound(SoundID.Grab);
		}
	}

	private void HandleRightClick(CoverSide side, CoverBehavior? cover)
	{
		if (cover is null) return;
		if (cover is IUICover)
		{
			_onOpenSettings?.Invoke(side);
			SoundEngine.PlaySound(SoundID.MenuTick);
		}
	}

	private static void LoadAssets()
	{
		_slotOverlay ??= ModContent.Request<Texture2D>(
			"GregTechCEuTerraria/Content/Textures/gui/icon/io_config/cover_slot_overlay");
	}
}
