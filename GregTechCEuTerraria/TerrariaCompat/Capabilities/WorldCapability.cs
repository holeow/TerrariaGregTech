#nullable enable
using System.Collections.Generic;
using GregTechCEuTerraria.Common.Energy;
using GregTechCEuTerraria.Api.Capability;
using GregTechCEuTerraria.TerrariaCompat.Pipelike.Cable;
using GregTechCEuTerraria.TerrariaCompat.Machine;

namespace GregTechCEuTerraria.TerrariaCompat.Capabilities;

public static class WorldCapability
{
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
		return LeafInventoryAt(x, y);
	}

	public static IItemHandler? LeafInventoryAt(int x, int y)
	{
		var chest = Handlers.VanillaChestItemHandler.At(x, y);
		if (chest != null) return chest;
		var extractinator = Handlers.ExtractinatorItemHandler.At(x, y);
		if (extractinator != null) return extractinator;
		if (MagicStoragePresent)
		{
			var ms = Handlers.MagicStorageItemHandler.At(x, y);
			if (ms != null) return ms;
		}
		return null;
	}

	internal static readonly bool MagicStoragePresent =
		Terraria.ModLoader.ModLoader.HasMod("MagicStorage");

	public static IFluidHandler? FluidHandlerAt(int x, int y, IODirection arrivalSide)
	{
		if (MachineCellResolver.TryFindMachineAt(x, y, out var machine))
			return machine.GetFluidHandlerCap(arrivalSide);
		if (TerrariaCompat.Pipelike.Fluid.FluidPipeLayerSystem.Pipes.Has(x, y))
			return TerrariaCompat.Pipelike.Fluid.FluidPipeLayerSystem
				.EnsureSides(x, y).GetFluidHandlerCap(arrivalSide, useCoverCapability: true);
		return null; // vanilla Terraria has no fluid-container tiles TODO sink
	}

	public static bool HasInventoryAt(int x, int y, IODirection arrivalSide)
		=> ItemHandlerAt(x, y, arrivalSide) != null || FluidHandlerAt(x, y, arrivalSide) != null;

	public static string TileDisplayName(int x, int y)
	{
		if (x < 0 || y < 0 || x >= Terraria.Main.maxTilesX || y >= Terraria.Main.maxTilesY) return "Empty";
		if (MachineCellResolver.TryFindMachineAt(x, y, out var mach))
			return mach.Definition?.Label ?? mach.Name;
		Terraria.Tile tile = Terraria.Main.tile[x, y];
		if (!tile.HasTile) return "Empty";
		var modTile = Terraria.ModLoader.TileLoader.GetTile(tile.TileType);
		if (modTile is not null) return modTile.Name;
		bool container = Terraria.Main.tileContainer[tile.TileType];
		int style = container ? tile.TileFrameX / 36 : 0;
		string name = MapObjectName(tile.TileType, style);
		if (string.IsNullOrEmpty(name) && container)
			name = ContainerItemName(tile.TileType, style);
		return string.IsNullOrEmpty(name) ? "Tile" : name;
	}

	private static Dictionary<(int Tile, int Style), int>? _containerItems;

	private static string ContainerItemName(int tileType, int style)
	{
		_containerItems ??= BuildContainerItemMap();
		return _containerItems.TryGetValue((tileType, style), out int itemType)
			? Terraria.Lang.GetItemNameValue(itemType)
			: "Chest";
	}

	private static Dictionary<(int, int), int> BuildContainerItemMap()
	{
		var map = new Dictionary<(int, int), int>();
		foreach (var kv in Terraria.ID.ContentSamples.ItemsByType)
		{
			Terraria.Item it = kv.Value;
			if (it is null || it.IsAir) continue;
			if ((uint)it.createTile >= (uint)Terraria.Main.tileContainer.Length) continue;
			if (!Terraria.Main.tileContainer[it.createTile]) continue;
			map.TryAdd((it.createTile, it.placeStyle), it.type);
		}
		return map;
	}

	public static string MapObjectName(int tileType, int style = 0)
	{
		try { return Terraria.Lang.GetMapObjectName(Terraria.Map.MapHelper.TileToLookup(tileType, style)) ?? ""; }
		catch { return ""; }
	}

	public static IODirection ToIODirection(Api.Cover.CoverSide side) => side switch
	{
		Api.Cover.CoverSide.Up => IODirection.Up,
		Api.Cover.CoverSide.Down => IODirection.Down,
		Api.Cover.CoverSide.Left => IODirection.Left,
		Api.Cover.CoverSide.Right => IODirection.Right,
		_ => IODirection.None,
	};

	public static Api.Cover.CoverSide? ToCoverSide(IODirection side) => side switch
	{
		IODirection.Up    => Api.Cover.CoverSide.Up,
		IODirection.Down  => Api.Cover.CoverSide.Down,
		IODirection.Left  => Api.Cover.CoverSide.Left,
		IODirection.Right => Api.Cover.CoverSide.Right,
		_                 => null,
	};

	public static IEnumerable<(IODirection side, int x, int y)> Perimeter(
		int originX, int originY, int width, int height)
	{
		for (int dx = 0; dx < width; dx++)
			yield return (IODirection.Up, originX + dx, originY - 1);
		for (int dx = 0; dx < width; dx++)
			yield return (IODirection.Down, originX + dx, originY + height);
		for (int dy = 0; dy < height; dy++)
			yield return (IODirection.Left, originX - 1, originY + dy);
		for (int dy = 0; dy < height; dy++)
			yield return (IODirection.Right, originX + width, originY + dy);
	}

	public static IEnumerable<(IODirection side, int x, int y)> Perimeter(MetaMachine machine) =>
		Perimeter(machine.Position.X, machine.Position.Y, machine.Size.Width, machine.Size.Height);

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
