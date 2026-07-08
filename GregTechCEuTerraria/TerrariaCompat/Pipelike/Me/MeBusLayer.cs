#nullable enable
using System.Collections.Generic;
using GregTechCEuTerraria.Api.Capability;
using GregTechCEuTerraria.AppliedEnergistics.Api.Config;
using GregTechCEuTerraria.AppliedEnergistics.Api.Stacks;

namespace GregTechCEuTerraria.TerrariaCompat.Pipelike.Me;

public enum MeBusKind : byte
{
	None    = 0,
	Storage = 1,
	Import  = 2,
	Export  = 3,
}

public enum MeBusSchedulingMode : byte
{
	Default    = 0,
	RoundRobin = 1,
	Random     = 2,
}

public sealed class MeBusAttachment
{
	public const int FilterSize = 18;

	public MeBusKind Kind;
	public AccessRestriction Access;
	public int Priority;
	public int Speed;
	public bool CraftMissing;
	public bool CraftOnly;
	public MeBusSchedulingMode Scheduling;
	public int NextSlot;
	public bool FilterOnExtract = true;
	public bool ExtractableOnly = false;
	public readonly AEKey?[] Filter = new AEKey?[FilterSize];

	public const int MaxSpeed = 4;
	public static int OperationsForSpeed(int level) => level switch
	{
		<= 0 => 1, 1 => 8, 2 => 32, 3 => 64, _ => 96,
	};

	public MeBusAttachment(MeBusKind kind,
		AccessRestriction access = AccessRestriction.READ_WRITE, int priority = 0, int speed = 0)
	{
		Kind = kind;
		Access = access;
		Priority = priority;
		Speed = System.Math.Clamp(speed, 0, MaxSpeed);
	}

	public MeBusAttachment Clone()
	{
		var c = new MeBusAttachment(Kind, Access, Priority, Speed)
		{
			CraftMissing = CraftMissing,
			CraftOnly = CraftOnly,
			Scheduling = Scheduling,
			NextSlot = NextSlot,
			FilterOnExtract = FilterOnExtract,
			ExtractableOnly = ExtractableOnly,
		};
		System.Array.Copy(Filter, c.Filter, FilterSize);
		return c;
	}

	public bool ImportAllows(AEKey what)
	{
		bool any = false;
		foreach (var f in Filter)
		{
			if (f is null) continue;
			any = true;
			if (f.Equals(what)) return true;
		}
		return !any;
	}

	public bool PartitionListed(AEKey what)
	{
		foreach (var f in Filter)
			if (f is not null && f.Equals(what)) return true;
		return false;
	}
}

public sealed class MeBusLayer
{
	private readonly Dictionary<(int x, int y), MeBusAttachment?[]> _cells = new();

	public bool IsDirty { get; private set; }
	public void ClearDirty() => IsDirty = false;

	public IReadOnlyDictionary<(int x, int y), MeBusAttachment?[]> All => _cells;

	public static int SideIndex(IODirection side) => side switch
	{
		IODirection.Up => 0, IODirection.Down => 1, IODirection.Left => 2, IODirection.Right => 3, _ => -1,
	};

	public static IODirection SideFromIndex(int i) => i switch
	{
		0 => IODirection.Up, 1 => IODirection.Down, 2 => IODirection.Left, 3 => IODirection.Right, _ => IODirection.None,
	};

	public MeBusAttachment? Get(int x, int y, IODirection side)
	{
		int i = SideIndex(side);
		if (i < 0) return null;
		return _cells.TryGetValue((x, y), out var arr) ? arr[i] : null;
	}

	public bool HasAny(int x, int y) => _cells.ContainsKey((x, y));

	public void Set(int x, int y, IODirection side, MeBusAttachment? attachment)
	{
		int i = SideIndex(side);
		if (i < 0) return;
		if (!_cells.TryGetValue((x, y), out var arr))
		{
			if (attachment is null) return;
			arr = new MeBusAttachment?[4];
			_cells[(x, y)] = arr;
		}
		arr[i] = attachment;
		IsDirty = true;

		bool any = false;
		foreach (var a in arr) if (a != null) { any = true; break; }
		if (!any) _cells.Remove((x, y));
	}

	public void Clear()
	{
		if (_cells.Count == 0) return;
		_cells.Clear();
		IsDirty = true;
	}
}
