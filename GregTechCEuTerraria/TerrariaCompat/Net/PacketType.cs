#nullable enable
namespace GregTechCEuTerraria.TerrariaCompat.Net;

public enum PacketType : byte
{
	// Machine GUI lifecycle
	MachineViewBegin,      // C->S  opened a machine's GUI
	MachineViewEnd,        // C->S  closed a machine's GUI

	// Machine GUI mutations
	SlotAction,            // C->S  slot click / shift-click
	FluidSlotAction,       // C->S  bucket fill / drain
	CircuitSet,            // C->S  circuit selector value
	IOConfigSet,           // C->S  output side / input-from-output flags
	PowerToggle,           // C->S  power on/off
	TankConfigSet,         // C->S  tank locked / voiding / auto-output
	ChestAction,           // C->S  chest locked / voiding / auto-output / dump
	PartIoDirection,       // C->S  part IoDirection (auto-IO side)
	ParallelSet,           // C->S  parallel hatch CurrentParallel
	ActiveRecipeTypeSet,   // C->S  cycle a multi's active recipe type
	BoilerThrottleSet,     // C->S  Large Boiler throttle +/-5%
	DistinctSet,           // C->S  IDistinctPart bus distinctness
	JunkToggle,            // C->S  fisher junk-enabled flag
	MachineFilter,         // C->S  machine filter edit
	CreativeChestSet,      // C->S  creative chest config
	CreativeTankSet,       // C->S  creative tank config
	CreativeEnergySet,     // C->S  creative energy container config

	// World right-click interactions
	TransformerToggle,     // C->S  flip transformer direction
	LdEndpointToggle,      // C->S  flip long-distance endpoint IO role
	CrateTape,             // C->S  seal a crate with tape
	DrumScrewdriver,       // C->S  toggle drum auto-output

	// Covers
	CoverAction,           // C->S  place / remove a cover
	CoverConfig,           // C->S  change a cover setting
	CoverFilter,           // C->S  cover filter edit

	// Machine placement, state sync
	MachinePlaced,         // C->S  placed a machine (create entity)
	MachineStateSync,      // S->C  full machine state snapshot
	MachineEnergySync,     // S->C  compact energy-stored sync
	CursorUpdate,          // S->C  authoritative cursor after a SlotAction
	EnderChannelSync,      // S->C  virtual ender channel contents
	MultiblockFormed,      // S->C  formed/unformed edge

	// Cables
	CableSet,              // C<->S place a cable / confirm
	CableRemove,           // C<->S remove a cable / confirm or burnout
	CableLayerRequest,     // C->S  late-join cable layer request
	CableLayerFull,        // S->C  full cable layer dump

	// Pipes
	PipePlaced,            // C<->S place a pipe / confirm
	PipeRemove,            // C<->S remove a pipe / confirm
	PipeLayerRequest,      // C->S  late-join pipe layer request
	PipeLayerFull,         // S->C  full pipe layer dump
	PipeCoverSync,         // S->C  per-cell pipe-cover state
	PipeSideModeSet,       // C->S  pipe per-side mode
	SimplePipeSideSet,     // C->S  simple pipe side (Off/Insert/Extract)
	PipeStats,             // S->C  per-pipe transferred-item counts
	FluidPipeStats,        // S->C  per-fluid-pipe tank contents
	CrossoverChange,       // C<->S Pipe Intersection changed; re-eval nets

	// Energy net, world effects
	EnergyNetStats,        // S->C  per-network throughput
	BlockExplosionEffect,  // S->C  bomb sound + smoke dust at (x,y)
	ItemCollectEffect,     // S->C  item-collected sparkle dust

	// Profiler
	ProfilerSync,          // S->C  server profiler counter snapshot

	// Questbook
	QuestbookStateRequest, // C->S  late-join questbook progress request
	QuestbookSync,         // S->C  full questbook snapshot
	QuestbookComplete,     // C->S  manual quest completion request
}
