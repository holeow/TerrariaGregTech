#nullable enable
using Terraria;

namespace GregTechCEuTerraria.TerrariaCompat.Machine.Multiblock.Primitive;

public static class PumpBiomeModifier
{
	public const int BUCKET_VOLUME = 1000;

	public static int GetForTile(int tileX, int tileY) => ForBiome(BiomeProbe.GetForTile(tileX, tileY));

	public static int ForBiome(BiomeProbe.Biome biome) => biome switch
	{
		BiomeProbe.Biome.Underworld => BUCKET_VOLUME / 100,
		BiomeProbe.Biome.Ocean      => BUCKET_VOLUME,
		BiomeProbe.Biome.Mushroom   => BUCKET_VOLUME * 5 / 2,
		BiomeProbe.Biome.Jungle     => BUCKET_VOLUME / 2,
		BiomeProbe.Biome.Snow       => BUCKET_VOLUME / 10,
		BiomeProbe.Biome.Hallow     => BUCKET_VOLUME * 15 / 100,
		BiomeProbe.Biome.Corruption => BUCKET_VOLUME * 35 / 100,
		BiomeProbe.Biome.Crimson    => BUCKET_VOLUME * 35 / 100,
		BiomeProbe.Biome.Desert     => BUCKET_VOLUME / 20,
		_                           => BUCKET_VOLUME / 4,
	};
}
