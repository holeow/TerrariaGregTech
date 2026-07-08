#nullable enable
using GregTechCEuTerraria.AppliedEnergistics.Api.Stacks;
using GregTechCEuTerraria.TerrariaCompat.UI.Widgets;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.UI;
using Terraria.UI.Chat;

namespace GregTechCEuTerraria.TerrariaCompat.UI.MeCraft;

internal static class CraftCell
{
	public const int Size = 44;
	public const int Pad = 2;
	public const int Step = Size + Pad;
	private const float VanillaNativeSlotPixels = 52f;
	private static readonly Item[] _slot = { new() };

	public static void Draw(SpriteBatch sb, Rectangle rect, AEKey what, long amount, Color bg, bool hovered,
		string? warning = null, string? info = null)
	{
		sb.Draw(TextureAssets.MagicPixel.Value, rect, bg);

		float old = Main.inventoryScale;
		Main.inventoryScale = rect.Width / VanillaNativeSlotPixels;
		try
		{
			if (what is AEItemKey ik)
			{
				var s = ik.GetReadOnlyStack().Clone();
				s.stack = 1;
				_slot[0] = s;
				if (hovered)
				{
					Main.LocalPlayer.mouseInterface = true;
					if (warning != null) CraftWarningTooltipGlobal.Push(warning);
					if (info != null) CraftWarningTooltipGlobal.PushInfo(info);
					ItemSlot.OverrideHover(_slot, ItemSlot.Context.CraftingMaterial, 0);
					ItemSlot.MouseHover(_slot, ItemSlot.Context.CraftingMaterial, 0);
					BrowserHover.SetItem(ik.GetItem());
				}
				ItemSlot.Draw(sb, _slot, ItemSlot.Context.CraftingMaterial, 0, new Vector2(rect.X, rect.Y));
			}
			else if (what is AEFluidKey fk)
			{
				var fluid = fk.GetFluid();
				BrowserFluidSlot.Draw(sb, rect, fluid, amountMb: 0, labelScale: rect.Width / (float)Size);
				if (hovered)
				{
					Main.LocalPlayer.mouseInterface = true;
					string? extra = null;
					if (warning != null) extra = "\n[c/FF6666:" + warning + "]";
					if (info != null) extra = (extra ?? "") + "\n" + info;
					BrowserFluidSlot.EmitTooltip(fluid, amountMb: 0, extraLine: extra);
					BrowserHover.SetFluid(fluid.Id, fluid.DisplayName);
				}
			}
		}
		finally { Main.inventoryScale = old; }

		if (amount > 0)
		{
			var font = FontAssets.ItemStack.Value;
			string text = UINumberFormat.Amount(what, amount);
			float f = (float)rect.Width / Size;
			float scale = 0.7f * f;
			var size = ChatManager.GetStringSize(font, text, new Vector2(scale));
			ChatManager.DrawColorCodedStringWithShadow(sb, font, text,
				new Vector2(rect.Right - size.X - 3f * f, rect.Bottom - 15f * f),
				Color.White, 0f, Vector2.Zero, new Vector2(scale));
		}
	}

	public static string Fmt(long n)
		=> n >= 1_000_000 ? (n / 1_000_000.0).ToString("0.#") + "M"
		 : n >= 10_000 ? (n / 1_000.0).ToString("0.#") + "k"
		 : n.ToString();
}
