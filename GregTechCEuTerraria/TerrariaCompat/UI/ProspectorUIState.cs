#nullable enable
using System.Collections.Generic;
using GregTechCEuTerraria.TerrariaCompat.Items.Prospectors;
using GregTechCEuTerraria.TerrariaCompat.UI.Widgets;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.UI;

namespace GregTechCEuTerraria.TerrariaCompat.UI;

public sealed class ProspectorUIState : UIModalWindow
{
	private Item? _prospector;
	private string _status = "";
	private ushort _selectedType;

	public Item? Prospector => _prospector;
	private ProspectorItem? Pi => _prospector?.ModItem as ProspectorItem;

	public void Bind(Item prospector)
	{
		_prospector = prospector;
		_selectedType = 0;
		Rebuild();
	}

	public void Unbind()
	{
		_prospector = null;
		_status = "";
		RemoveAllChildren();
	}

	private void Rebuild()
	{
		RemoveAllChildren();

		var pi = Pi;
		if (pi is null) return;

		List<ProspectorScan.OreHit> hits;
		pi.TryScan(Main.LocalPlayer, out hits, out _status);

		const int W = 320, H = 320, Pad = 10, BarW = 20;

		var panel = new UITerrariaPanel
		{
			Width  = StyleDimension.FromPixels(W),
			Height = StyleDimension.FromPixels(H),
			HAlign = 0.5f,
			VAlign = 0.4f,
		};

		panel.Append(new UIText("Ore Scanner", 0.85f)
		{
			Left = StyleDimension.FromPixels(Pad),
			Top  = StyleDimension.FromPixels(Pad),
		});

		panel.Append(new UITextButton(
			() => "Scanner: " + (Pi?.Active == true ? "ON" : "OFF"),
			onLeft:  ToggleActive,
			onRight: ToggleActive,
			tooltip: "Enable / disable the scanner (also toggleable by right-clicking it in your inventory).",
			width:  120,
			height: 28)
		{
			Left = StyleDimension.FromPixels(W - 120 - Pad),
			Top  = StyleDimension.FromPixels(Pad),
		});

		panel.Append(new UIDynamicLabel(
			getter: StatusText,
			scale:  0.7f,
			color:  Color.White)
		{
			Left = StyleDimension.FromPixels(Pad),
			Top  = StyleDimension.FromPixels(40),
		});

		int listTop = 90;
		int listH = H - listTop - Pad;

		var list = new UIList
		{
			Left   = StyleDimension.FromPixels(Pad),
			Top    = StyleDimension.FromPixels(listTop),
			Width  = StyleDimension.FromPixels(W - 2 * Pad - BarW - 4),
			Height = StyleDimension.FromPixels(listH),
			ListPadding = 2f,
		};
		var bar = new UIScrollbar
		{
			Left   = StyleDimension.FromPixels(W - Pad - BarW),
			Top    = StyleDimension.FromPixels(listTop),
			Width  = StyleDimension.FromPixels(BarW),
			Height = StyleDimension.FromPixels(listH),
		};
		list.SetScrollbar(bar);

		foreach (var hit in hits)
			list.Add(new OreRow(this, hit));

		panel.Append(list);
		panel.Append(bar);
		Append(panel);
	}

	private string StatusText()
	{
		var pi = Pi;
		if (pi is null) return "";
		return $"{_status}\nScan cost: {pi.ScanCost:N0} EU";
	}

	private void ToggleActive()
	{
		if (Pi is not { } m) return;
		m.Active = !m.Active;
		if (!m.Active) { m.ClearTarget(); _selectedType = 0; }
	}

	private void SelectOre(ProspectorScan.OreHit hit)
	{
		if (Pi is not { } m) return;
		if (!m.Active) m.Active = true;
		m.SetTarget(hit.NearestTile.X, hit.NearestTile.Y);
		_selectedType = hit.TileType;
		SoundEngine.PlaySound(SoundID.MenuTick);
	}

	private sealed class OreRow : UIElement
	{
		private readonly ProspectorUIState _owner;
		private readonly ProspectorScan.OreHit _hit;

		public OreRow(ProspectorUIState owner, ProspectorScan.OreHit hit)
		{
			_owner = owner;
			_hit = hit;
			Width = StyleDimension.Fill;
			Height = StyleDimension.FromPixels(26);
			OnLeftClick += (_, _) => _owner.SelectOre(_hit);
		}

		protected override void DrawSelf(SpriteBatch sb)
		{
			CalculatedStyle d = GetDimensions();
			var rect = d.ToRectangle();

			bool selected = _owner._selectedType == _hit.TileType;
			if (selected)
				sb.Draw(TextureAssets.MagicPixel.Value, rect, new Color(120, 230, 120) * 0.30f);
			else if (IsMouseHovering)
				sb.Draw(TextureAssets.MagicPixel.Value, rect, Color.White * 0.10f);

			if (_hit.IconItem > 0)
				Questbook.QuestbookIcon.Draw(sb, _hit.IconItem, new Vector2(d.X + 16, d.Y + 13), 22f);

			Terraria.Utils.DrawBorderString(sb, $"{_hit.Name}  x{_hit.Count}",
				new Vector2(d.X + 32, d.Y + 5), selected ? new Color(150, 255, 150) : Color.White, 0.72f);

			int distTiles = (int)(_hit.NearestDistPx / 16f);
			Terraria.Utils.DrawBorderString(sb, $"{distTiles}t",
				new Vector2(d.X + d.Width - 44, d.Y + 5), new Color(180, 180, 195), 0.72f);
		}
	}
}
