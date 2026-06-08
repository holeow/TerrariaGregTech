#nullable enable
using System;
using System.Collections.Generic;
using GregTechCEuTerraria.Api.Capability;
using GregTechCEuTerraria.Api.Cover;
using GregTechCEuTerraria.Api.Data.Chemical.Material.Properties;
using GregTechCEuTerraria.Api.Machine;
using GregTechCEuTerraria.TerrariaCompat.Capabilities;
using GregTechCEuTerraria.TerrariaCompat.Machine;
using Terraria;

namespace GregTechCEuTerraria.TerrariaCompat.Pipelike.ItemPipe;

public sealed class ItemRoutePath
{
	public (int X, int Y) TargetPipe   { get; }
	public CoverSide TargetFacing       { get; }
	public int Distance                 { get; }
	public ItemPipeProperties Properties { get; }
	public bool Restrictive             { get; }

	private readonly Func<Terraria.Item, bool> _filters;

	public ItemRoutePath(
		(int X, int Y) targetPipe, CoverSide facing, int distance,
		ItemPipeProperties properties, bool restrictive,
		IReadOnlyList<Func<Terraria.Item, bool>> filters)
	{
		TargetPipe = targetPipe;
		TargetFacing = facing;
		Distance = distance;
		Properties = properties;
		Restrictive = restrictive;
		_filters = stack =>
		{
			for (int i = 0; i < filters.Count; i++)
				if (!filters[i](stack)) return false;
			return true;
		};
	}

	public (int X, int Y) TargetPipePos => TargetPipe;

	public IItemHandler? GetHandler() => PipeNeighborProbe.ResolveItem(TargetPipe.X, TargetPipe.Y, TargetFacing).handler;

	public bool MatchesFilters(Terraria.Item stack) => _filters(stack);

	public (int X, int Y, CoverSide F) ToFacingPos() => (TargetPipe.X, TargetPipe.Y, TargetFacing);
}
