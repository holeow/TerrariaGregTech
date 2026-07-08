#nullable enable
using GregTechCEuTerraria.TerrariaCompat.Capabilities;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace GregTechCEuTerraria.TerrariaCompat.UI;

public static class MagicStorageUi
{
	public static bool IsOpen => WorldCapability.MagicStoragePresent && IsOpenImpl();

	[JITWhenModsEnabled("MagicStorage")]
	private static bool IsOpenImpl() =>
		MagicStorage.StoragePlayer.LocalPlayer.ViewingStorage() != Point16.NegativeOne;
}
