#nullable enable
using GregTechCEuTerraria.Api.Capability;

namespace GregTechCEuTerraria.TerrariaCompat.Capabilities;

// Neutral, GregTech-free seam for "resolve the item / fluid handler this object
// exposes on a given side." MetaMachine implements it (forwarding to its
// cover-aware GetItemHandlerCap / GetFluidHandlerCap), so a consumer (future AE2
// port, automation layer) gets a sided handler from an entity reference without
// casting to MetaMachine - it imports only this interface plus Api.Capability.
//
// Mirrors Forge's ICapabilityProvider.getCapability(cap, Direction), narrowed to
// the two handler caps. Entity-reference counterpart of WorldCapability's
// position-based ItemHandlerAt / FluidHandlerAt.
public interface ISidedCapabilityProvider
{
	// `side` is the face being queried, in the provider's own coordinate frame.
	// Returns the cover-gated handler (= the useCoverCapability:true resolve),
	// or null if this provider exposes no handler of that kind on that side.
	IItemHandler? GetItemHandler(IODirection side);

	IFluidHandler? GetFluidHandler(IODirection side);
}
