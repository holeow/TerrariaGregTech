#nullable enable
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GregTechCEuTerraria.TerrariaCompat.UI.Widgets;

public static class PhantomSlotChrome
{
	public enum Kind { Item, Fluid, ItemOrFluid }

	public static string EmptyTooltip(Kind kind) => kind switch
	{
		Kind.Fluid       => "Empty matcher slot\n[c/AAAAAA:LMB] pick a fluid   [c/AAAAAA:drag] a fluid here",
		Kind.ItemOrFluid => "Empty matcher slot\n[c/AAAAAA:LMB] pick an item or fluid   [c/AAAAAA:drag] here",
		_                => "Empty matcher slot\n[c/AAAAAA:LMB] pick an item   [c/AAAAAA:drag] an item here",
	};

	public static string FilledTooltip(string name, string? amountText, bool canSetAmount)
	{
		var sb = new StringBuilder();
		sb.Append(name);
		if (!string.IsNullOrEmpty(amountText)) { sb.Append("   "); sb.Append(amountText); }
		sb.Append('\n');
		sb.Append(canSetAmount ? "[c/AAAAAA:LMB] set amount   [c/AAAAAA:RMB] clear" : "[c/AAAAAA:RMB] clear");
		return sb.ToString();
	}

	public static void DrawHoverBorder(SpriteBatch sb, Rectangle bounds, bool hovering)
		=> TankFrame.DrawBorder(sb, bounds, hovering
			? Color.Lerp(TankFrame.BorderColor, Color.White, 0.5f)
			: TankFrame.BorderColor);
}
