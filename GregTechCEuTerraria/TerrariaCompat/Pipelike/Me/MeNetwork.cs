#nullable enable
using System.Collections.Generic;
using GregTechCEuTerraria.AppliedEnergistics.Api.Stacks;
using GregTechCEuTerraria.AppliedEnergistics.Api.Storage;
using GregTechCEuTerraria.AppliedEnergistics.Me.Storage;
using GregTechCEuTerraria.TerrariaCompat.AppliedEnergistics.Crafting;

namespace GregTechCEuTerraria.TerrariaCompat.Pipelike.Me;

public sealed class MeNetwork
{
	public IReadOnlyDictionary<(int x, int y), MeCableCell> Cells { get; }

	public (int x, int y) AnchorCell { get; }

	private readonly List<IMeStorageDevice> _devices = new();
	private readonly List<(int priority, MEStorage storage)> _extraStorages = new();
	private int _sourceCount;
	private NetworkStorage? _storage;

	private readonly List<IMePatternProvider> _providers = new();

	public MeNetwork(IReadOnlyDictionary<(int x, int y), MeCableCell> cableCells, (int x, int y) anchor)
	{
		Cells = cableCells;
		AnchorCell = anchor;
	}

	internal void AddDevice(IMeStorageDevice device)
	{
		_devices.Add(device);
		_storage = null;
	}

	internal void AddSource(int priority, params MEStorage[] storages)
	{
		foreach (var s in storages)
			_extraStorages.Add((priority, s));
		_sourceCount++;
		_storage = null;
	}

	public IReadOnlyList<IMeStorageDevice> Devices => _devices;

	public int MountedStorageCount => _devices.Count + _sourceCount;

	internal void AddProvider(IMePatternProvider provider) => _providers.Add(provider);

	public IReadOnlyList<IMePatternProvider> Providers => _providers;

	private int _interfaceCount;
	internal void AddInterface() => _interfaceCount++;
	public int InterfaceCount => _interfaceCount;

	public HashSet<AEKey> GetCraftables()
	{
		var set = new HashSet<AEKey>();
		foreach (var p in _providers)
			foreach (var pat in p.Patterns)
				if (pat.PrimaryOutput != null)
					set.Add(pat.PrimaryOutput);
		return set;
	}

	public bool IsCraftable(AEKey what)
	{
		foreach (var p in _providers)
			foreach (var pat in p.Patterns)
				if (what.Equals(pat.PrimaryOutput)) return true;
		return false;
	}

	public IReadOnlyList<MePattern> GetCraftingFor(AEKey what)
	{
		var result = new List<MePattern>();
		var seen = new HashSet<MePattern>();
		foreach (var p in _providers)
			foreach (var pat in p.Patterns)
			{
				bool produces = false;
				foreach (var (w, _) in pat.Outputs)
					if (what.Equals(w)) { produces = true; break; }
				if (produces && seen.Add(pat)) result.Add(pat);
			}
		return result;
	}

	public IReadOnlyList<IMePatternProvider> GetProviders(MePattern details)
	{
		var result = new List<IMePatternProvider>();
		foreach (var p in _providers)
			foreach (var pat in p.Patterns)
				if (pat.Equals(details)) { result.Add(p); break; }
		return result;
	}

	public bool CanFulfill(MePattern details)
	{
		foreach (var p in _providers)
		{
			if (p is not IMeCraftingProvider cp) continue;
			foreach (var pat in p.Patterns)
				if (pat.Equals(details))
				{
					if (cp.CanFulfill(details)) return true;
					break;
				}
		}
		return false;
	}

	public MEStorage GetStorage()
	{
		if (_storage is null)
		{
			var ns = new NetworkStorage();
			foreach (var device in _devices)
				ns.Mount(device.StoragePriority, device.GetMeStorage());
			foreach (var (priority, storage) in _extraStorages)
				ns.Mount(priority, storage);
			_storage = ns;
		}
		return _storage;
	}
}
