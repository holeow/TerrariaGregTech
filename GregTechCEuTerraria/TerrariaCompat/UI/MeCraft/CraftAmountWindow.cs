#nullable enable
using System;
using GregTechCEuTerraria.AppliedEnergistics.Api.Stacks;
using GregTechCEuTerraria.TerrariaCompat.Net;
using GregTechCEuTerraria.TerrariaCompat.UI.Widgets;
using Terraria.DataStructures;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace GregTechCEuTerraria.TerrariaCompat.UI.MeCraft;

public sealed class CraftAmountWindow : UIModalWindow
{
	private Point16 _termPos;
	private AEKey? _key;
	private long _amount = 1;
	private long _minAmount = 1;
	private string _title = "Request Crafting";
	private string _startLabel = "Next";
	private string _startTooltip = "Calculate the crafting plan";
	private Action<long>? _onConfirm;

	public void Bind(Point16 termPos, AEKey key, long defaultAmount)
	{
		_termPos = termPos;
		_key = key;
		_onConfirm = null;
		_minAmount = 1;
		_title = "Request Crafting";
		_startLabel = "Next";
		_startTooltip = "Calculate the crafting plan";
		_amount = Math.Clamp(defaultAmount <= 0 ? 1 : defaultAmount, _minAmount, int.MaxValue);
		RemoveAllChildren();
		BuildPanel();
	}

	public void Bind(AEKey key, long defaultAmount, long minAmount, string title, string startLabel,
		string startTooltip, Action<long> onConfirm)
	{
		_key = key;
		_onConfirm = onConfirm;
		_minAmount = Math.Max(0, minAmount);
		_title = title;
		_startLabel = startLabel;
		_startTooltip = startTooltip;
		_amount = Math.Clamp(defaultAmount, _minAmount, int.MaxValue);
		RemoveAllChildren();
		BuildPanel();
	}

	public void Unbind()
	{
		UITextField.UnfocusAll();
		RemoveAllChildren();
		_key = null;
		_onConfirm = null;
	}

	private void BuildPanel()
	{
		if (_key is null) return;

		const int sh = 30;
		var panel = new UITerrariaPanel
		{
			Width = StyleDimension.FromPixels(390),
			Height = StyleDimension.FromPixels(175),
			HAlign = 0.5f,
			VAlign = 0.4f,
		};

		panel.Append(new UIText(_title, 1.05f)
		{ Left = StyleDimension.FromPixels(14), Top = StyleDimension.FromPixels(12) });

		panel.Append(new UIText(_key.GetDisplayName(), 0.85f)
		{ Left = StyleDimension.FromPixels(14), Top = StyleDimension.FromPixels(40) });

		const int y = 74;
		Step(panel, "-100", -100, 23, y, 42, sh);
		Step(panel, "-10", -10, 68, y, 42, sh);
		Step(panel, "-1", -1, 113, y, 34, sh);
		panel.Append(new UITextField(
			current: () => _amount.ToString(),
			onConfirm: txt => { if (long.TryParse(txt, out var v)) SetAmount(v); },
			maxLength: 10,
			filter: ch => ch >= '0' && ch <= '9',
			placeholder: "1")
		{
			Left = StyleDimension.FromPixels(150),
			Top = StyleDimension.FromPixels(y),
			Width = StyleDimension.FromPixels(90),
			Height = StyleDimension.FromPixels(sh),
		});
		Step(panel, "+1", +1, 243, y, 34, sh);
		Step(panel, "+10", +10, 280, y, 42, sh);
		Step(panel, "+100", +100, 325, y, 42, sh);

		panel.Append(new UITextButton(
			label: () => _startLabel,
			onLeft: Start,
			tooltip: _startTooltip,
			width: 140, height: 34, textScale: 0.95f)
		{ Left = StyleDimension.FromPixels(125), Top = StyleDimension.FromPixels(122) });

		Append(panel);
	}

	private void Step(UIElement parent, string label, int delta, int x, int y, int w, int h)
	{
		parent.Append(new UITextButton(
			label: () => label,
			onLeft: () => SetAmount(_amount + delta),
			tooltip: null,
			width: w, height: h, textScale: 0.85f)
		{ Left = StyleDimension.FromPixels(x), Top = StyleDimension.FromPixels(y) });
	}

	private void SetAmount(long v) => _amount = Math.Clamp(v, _minAmount, int.MaxValue);

	private void Start()
	{
		if (_key is null) return;
		if (_onConfirm is { } cb)
		{
			long amt = _amount;
			MeCraftSystem.Close();
			cb(amt);
		}
		else MeCraftPackets.Begin(_termPos, _key, _amount);
	}
}
