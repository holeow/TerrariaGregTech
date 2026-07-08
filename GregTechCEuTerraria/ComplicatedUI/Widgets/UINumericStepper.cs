#nullable enable
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Terraria;
using Terraria.UI;

namespace GregTechCEuTerraria.TerrariaCompat.UI.Widgets;

public sealed class UINumericStepper : UIElement
{
	private readonly string _label;
	private readonly Func<long> _getter;
	private readonly Action<long> _setter;
	private readonly long _min, _max, _step;
	private readonly int _labelWidth;
	private readonly UITextField _field;

	public UINumericStepper(string label, Func<long> getter, Action<long> setter,
		long min, long max, long step, int labelWidth, int width = 0)
	{
		_label  = label;
		_getter = getter;
		_setter = setter;
		_min    = min;
		_max    = max;
		_step   = step;
		_labelWidth = labelWidth;
		Width  = StyleDimension.FromPixels(width > 0 ? width : labelWidth + 100);
		Height = StyleDimension.FromPixels(16);

		Append(new UITextButton(() => "-",
			onLeft:  () => Apply(-_step * Mult()),
			onRight: () => Apply(_step * Mult()),
			tooltip: "(Shift x10, Ctrl x100; RMB inverts)", width: 16, height: 16)
		{ Left = StyleDimension.FromPixels(_labelWidth), Top = StyleDimension.FromPixels(0) });

		_field = new UITextField(
			current:   () => _getter().ToString(),
			onConfirm: s => { if (long.TryParse(s.Trim(), out long v)) _setter(Math.Clamp(v, _min, _max)); },
			maxLength: 12,
			filter:    c => char.IsDigit(c) || c == '-')
		{
			Left   = StyleDimension.FromPixels(_labelWidth + 18),
			Top    = StyleDimension.FromPixels(0),
			Width  = new StyleDimension(-(_labelWidth + 36), 1f),
			Height = StyleDimension.FromPixels(16),
		};
		Append(_field);

		Append(new UITextButton(() => "+",
			onLeft:  () => Apply(_step * Mult()),
			onRight: () => Apply(-_step * Mult()),
			tooltip: "(Shift x10, Ctrl x100; RMB inverts)", width: 16, height: 16)
		{ Left = new StyleDimension(-16, 1f), Top = StyleDimension.FromPixels(0) });
	}

	private static long Mult()
	{
		long m = 1;
		if (Main.keyState.IsKeyDown(Keys.LeftShift) || Main.keyState.IsKeyDown(Keys.RightShift)) m *= 10;
		if (Main.keyState.IsKeyDown(Keys.LeftControl) || Main.keyState.IsKeyDown(Keys.RightControl)) m *= 100;
		return m;
	}

	private void Apply(long delta) => _setter(Math.Clamp(_getter() + delta, _min, _max));

	protected override void DrawSelf(SpriteBatch sb)
	{
		if (_label.Length == 0) return;
		var bounds = GetDimensions().ToRectangle();
		Terraria.Utils.DrawBorderString(sb, _label,
			new Vector2(bounds.X, bounds.Y + 2), Color.White, 0.72f);
	}
}
