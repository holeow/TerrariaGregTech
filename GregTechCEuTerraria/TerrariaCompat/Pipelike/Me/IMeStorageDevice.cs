#nullable enable
using GregTechCEuTerraria.AppliedEnergistics.Api.Storage;

namespace GregTechCEuTerraria.TerrariaCompat.Pipelike.Me;

public interface IMeStorageDevice : IMeNetworkConnected
{
	MEStorage GetMeStorage();

	int StoragePriority => 0;
}
