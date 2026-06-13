#nullable enable
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace GregTechCEuTerraria.TerrariaCompat.Machine.Rendering;

public sealed class MachinePaintBakeSystem : ModSystem
{
	public override void Load()
	{
		if (!Main.dedServ)
			Main.OnPreDraw += OnPreDraw;
	}

	public override void Unload() => Main.OnPreDraw -= OnPreDraw;

	private static void OnPreDraw(GameTime _) => MachineRenderer.ProcessPendingPaintBakes();
}
