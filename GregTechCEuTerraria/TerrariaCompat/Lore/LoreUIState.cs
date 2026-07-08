#nullable enable
using System.Collections.Generic;
using GregTechCEuTerraria.TerrariaCompat.UI;
using GregTechCEuTerraria.TerrariaCompat.UI.Widgets;
using Microsoft.Xna.Framework;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace GregTechCEuTerraria.TerrariaCompat.Lore;

public sealed class LoreUIState : FreeModalWindow
{
	private UITerrariaPanel _panel = null!;
	private UIText _title = null!;
	private UIMultiLineDynamicLabel _body = null!;
	private bool _built;

	private readonly string _titleText;
	private readonly IReadOnlyList<string> _lines;

	private const int Pad = 12;
	private const int TitleH = 28;
	private const int MoveKnobW = 20;
	private const int ResizeKnobSz = 26;

	public LoreUIState(string title, IReadOnlyList<string> lines)
	{
		_titleText = title;
		_lines = lines;
	}

	protected override void RebuildWindow()
	{
		var root = RootSize();
		ResolveSize(root.X, root.Y, 860f, 660f, MinModalW, MinModalH);
		float w = CurW, h = CurH;

		if (!_built) BuildStructure();

		_panel.Width  = StyleDimension.FromPixels(w);
		_panel.Height = StyleDimension.FromPixels(h);

		int bodyTop = Pad + TitleH + 8;
		_body.Left   = StyleDimension.FromPixels(Pad);
		_body.Top    = StyleDimension.FromPixels(bodyTop);
		_body.Width  = StyleDimension.FromPixels(w - 2 * Pad);
		_body.Height = StyleDimension.FromPixels(h - bodyTop - Pad);

		LayoutHeaderButtons(_panel, w, Pad, TitleH);
		LayoutResizeKnob(_panel, w, h, ResizeKnobSz, "Drag to resize");
		ApplyCenteredMoveClamp(_panel, root, w, h);

		Recalculate();
		_built = true;
	}

	private void BuildStructure()
	{
		_panel = new UITerrariaPanel
		{
			HAlign = 0.5f,
			VAlign = 0.5f,
			BackgroundColor = new Color(20, 22, 50) * 0.92f,
		};
		Append(_panel);

		var moveKnob = NewMoveKnob("Drag to move");
		moveKnob.Left   = StyleDimension.FromPixels(Pad);
		moveKnob.Top    = StyleDimension.FromPixels(Pad);
		moveKnob.Width  = StyleDimension.FromPixels(MoveKnobW);
		moveKnob.Height = StyleDimension.FromPixels(TitleH);
		_panel.Append(moveKnob);

		_title = new UIText(_titleText, 1.05f)
		{
			Left = StyleDimension.FromPixels(Pad + MoveKnobW + 6),
			Top  = StyleDimension.FromPixels(Pad + 2),
			IgnoresMouseInteraction = true,
		};
		_panel.Append(_title);

		_body = new UIMultiLineDynamicLabel(() => _lines, scale: 0.9f, lineHeight: 20f);
		_panel.Append(_body);
	}

	protected override void ApplyOffsetLive()
	{
		if (_panel is null) return;
		_panel.Left = StyleDimension.FromPixels(OffsetX);
		_panel.Top  = StyleDimension.FromPixels(OffsetY);
		Recalculate();
	}
}
