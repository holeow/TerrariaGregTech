#nullable enable
using GregTechCEuTerraria.AppliedEnergistics.Api.Storage;

namespace GregTechCEuTerraria.TerrariaCompat.Pipelike.Me;

public interface IMeInventoryExposer : IMeNetworkConnected
{
	MEStorage? GetExposedInventory();

	MeNetwork? HomeNetwork { get; }
}
