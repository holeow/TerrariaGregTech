#nullable enable
using GregTechCEuTerraria.AppliedEnergistics.Api.Stacks;
using GregTechCEuTerraria.Common.Energy;
using GregTechCEuTerraria.TerrariaCompat.AppliedEnergistics;
using GregTechCEuTerraria.TerrariaCompat.Machine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace GregTechCEuTerraria.TerrariaCompat.Tiles.Machines;

public class QuantumComputerTile : TieredMachineTile
{
	public QuantumComputerTile() { }
	public QuantumComputerTile(VoltageTier tier, MachineDefinition def) : base(tier, def) { }

	public override void PostDraw(int i, int j, SpriteBatch spriteBatch)
	{
		base.PostDraw(i, j, spriteBatch);

		if (!MachineCellResolver.TryFindAt<QuantumComputerMachine>(i, j, out var cpu)) return;
		if (cpu.DisplayOutput?.What is not AEItemKey key) return;

		var (w, h) = cpu.Size;
		int originX = cpu.Position.X, originY = cpu.Position.Y;
		if (i != originX + w - 1 || j != originY + h - 1) return;

		int itemType = key.GetItem();
		Main.instance.LoadItem(itemType);
		Main.GetItemDrawFrame(itemType, out var tex, out var srcFrame);
		if (tex is null) return;

		Vector2 zero = Main.drawToScreen ? Vector2.Zero
			: new Vector2(Main.offScreenRange, Main.offScreenRange);
		Vector2 pos = new Vector2(originX * 16 - (int)Main.screenPosition.X,
		                          originY * 16 - (int)Main.screenPosition.Y) + zero;
		const int Border = 8;
		var inner = new Rectangle((int)pos.X + Border, (int)pos.Y + Border,
		                          w * 16 - 2 * Border, h * 16 - 2 * Border);
		spriteBatch.Draw(tex, FitCentered(srcFrame, inner), srcFrame,
			Lighting.GetColor(originX, originY));
	}

	private static Rectangle FitCentered(Rectangle src, Rectangle box)
	{
		if (src.Width <= 0 || src.Height <= 0) return box;
		float scale = System.Math.Min((float)box.Width / src.Width, (float)box.Height / src.Height);
		int w = (int)(src.Width * scale), h = (int)(src.Height * scale);
		return new Rectangle(box.X + (box.Width - w) / 2, box.Y + (box.Height - h) / 2, w, h);
	}
}
