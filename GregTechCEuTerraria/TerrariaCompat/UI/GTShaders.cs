#nullable enable
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;

namespace GregTechCEuTerraria.TerrariaCompat.UI;

public static class GTShaders
{
	private static Asset<Effect>? _energyBar;

	public static Effect? EnergyBar => _energyBar?.Value;

	public static void Load(Mod mod)
	{
		if (Main.dedServ) return;
		_energyBar = mod.Assets.Request<Effect>("Effects/EnergyBar", AssetRequestMode.ImmediateLoad);
	}

	public static void Unload()
	{
		_energyBar = null;
	}
}
