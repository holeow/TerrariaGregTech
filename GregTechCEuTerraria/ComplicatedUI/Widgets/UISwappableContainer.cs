#nullable enable
using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.UI;

namespace GregTechCEuTerraria.TerrariaCompat.UI.Widgets;

public sealed class UISwappableContainer : UIElement
{
	private readonly Func<int> _sig;
	private readonly Action<UISwappableContainer> _build;
	private int? _builtSig;

	public UISwappableContainer(Func<int> signature, Action<UISwappableContainer> build)
	{
		_sig = signature;
		_build = build;
	}

	public override void Update(GameTime gameTime)
	{
		base.Update(gameTime);
		int sig = _sig();
		if (sig == _builtSig) return;
		if (Main.mouseLeft || Main.mouseRight) return;
		_builtSig = sig;
		RemoveAllChildren();
		UITextField.UnfocusAll();
		_build(this);
		Recalculate();
	}
}
