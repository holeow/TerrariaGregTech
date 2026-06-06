#nullable enable
using System.Collections.Generic;
using GregTechCEuTerraria.Common.Energy;
using GregTechCEuTerraria.Api.Capability;
using GregTechCEuTerraria.TerrariaCompat.Pipelike.Cable;
using GregTechCEuTerraria.TerrariaCompat.Machine;

namespace GregTechCEuTerraria.TerrariaCompat.Capabilities;

// Forge-capability emulation narrowed to our 2-layer world
// Centralizes the resolution of multi-cell origin
public static class WorldCapability
{
	// T = any interface a MetaMachine implements (IEnergyContainer / IItemHandler / IFluidHandler).
	public static T? Get<T>(int x, int y) where T : class
	{
		if (!MachineCellResolver.TryFindMachineAt(x, y, out var machine))
			return null;
		return machine as T;
	}

	public static IItemHandler? ItemHandlerAt(int x, int y, IODirection arrivalSide)
	{
		if (MachineCellResolver.TryFindMachineAt(x, y, out var machine))
			return machine.GetItemHandlerCap(arrivalSide);
		if (TerrariaCompat.Pipelike.ItemPipe.ItemPipeLayerSystem.Pipes.Has(x, y))
			return TerrariaCompat.Pipelike.ItemPipe.ItemPipeLayerSystem
				.EnsureSides(x, y).GetItemHandlerCap(arrivalSide, useCoverCapability: true);
		return Handlers.VanillaChestItemHandler.At(x, y);
	}

	public static IFluidHandler? FluidHandlerAt(int x, int y, IODirection arrivalSide)
	{
		if (MachineCellResolver.TryFindMachineAt(x, y, out var machine))
			return machine.GetFluidHandlerCap(arrivalSide);
		if (TerrariaCompat.Pipelike.Fluid.FluidPipeLayerSystem.Pipes.Has(x, y))
			return TerrariaCompat.Pipelike.Fluid.FluidPipeLayerSystem
				.EnsureSides(x, y).GetFluidHandlerCap(arrivalSide, useCoverCapability: true);
		return null; // vanilla Terraria has no fluid-container tiles TODO sink
	}

	public static IODirection ToIODirection(Api.Cover.CoverSide side) => side switch
	{
		Api.Cover.CoverSide.Up => IODirection.Up,
		Api.Cover.CoverSide.Down => IODirection.Down,
		Api.Cover.CoverSide.Left => IODirection.Left,
		Api.Cover.CoverSide.Right => IODirection.Right,
		_ => IODirection.None,
	};

	public static IEnumerable<(IODirection side, int x, int y)> Perimeter(
		int originX, int originY, int width, int height)
	{
		// Top edge (Up)
		for (int dx = 0; dx < width; dx++)
			yield return (IODirection.Up, originX + dx, originY - 1);
		// Bottom edge (Down) - Terraria Y grows downward, so "down" = origin+height
		for (int dx = 0; dx < width; dx++)
			yield return (IODirection.Down, originX + dx, originY + height);
		// Left edge
		for (int dy = 0; dy < height; dy++)
			yield return (IODirection.Left, originX - 1, originY + dy);
		// Right edge
		for (int dy = 0; dy < height; dy++)
			yield return (IODirection.Right, originX + width, originY + dy);
	}

	public static IEnumerable<(IODirection side, int x, int y)> Perimeter(MetaMachine machine) =>
		Perimeter(machine.Position.X, machine.Position.Y, machine.Size.Width, machine.Size.Height);

	// For a cable at (cableX, cableY), return the IEnergyContainer endpoint
	// that sits at the same cell (the "wire behind machine" connectivity
	// model
	public static (IODirection sideFromCable, IEnergyContainer ep)? CableEndpointAtCell(int cableX, int cableY)
	{
		var ep = Get<IEnergyContainer>(cableX, cableY);
		return ep is null ? null : (IODirection.None, ep);
	}

	public static IEnumerable<(int x, int y, CableCell cable)>
		CablesAtFootprint(MetaMachine machine)
	{
		var layer = CableLayerSystem.Cables;
		int ox = machine.Position.X, oy = machine.Position.Y;
		var (w, h) = machine.Size;
		for (int dx = 0; dx < w; dx++)
		for (int dy = 0; dy < h; dy++)
		{
			var cell = layer.CellAt(ox + dx, oy + dy);
			if (cell is CableCell cc)
				yield return (ox + dx, oy + dy, cc);
		}
	}

	// Inverse of CableEndpointAtCell
	public static IEnumerable<(IODirection side, int x, int y, CableCell cable)>
		AdjacentCables(MetaMachine machine)
	{
		var layer = CableLayerSystem.Cables;
		foreach (var (side, x, y) in Perimeter(machine))
		{
			var cell = layer.CellAt(x, y);
			if (cell is CableCell cc)
				yield return (side, x, y, cc);
		}
	}
}
