#nullable enable
using System.Collections.Generic;
using GregTechCEuTerraria.AppliedEnergistics.Api.Stacks;
using GregTechCEuTerraria.TerrariaCompat.Tiles.CraftingStations;
using Terraria.ID;
using Terraria.ModLoader.IO;

namespace GregTechCEuTerraria.TerrariaCompat.AppliedEnergistics.Crafting;

public sealed class MePattern
{
	public MePatternType Type { get; }
	public int StationTile { get; }
	public string? StationKey { get; }

	private readonly (AEKey what, long amount)[] _inputs;
	private readonly (AEKey what, long amount)[] _outputs;
	private readonly string?[] _inputTags;

	private MePattern(MePatternType type, int station, string? stationKey,
		(AEKey, long)[] inputs, (AEKey, long)[] outputs, string?[] inputTags)
	{
		Type = type;
		StationTile = station;
		StationKey = stationKey;
		_inputs = inputs;
		_outputs = outputs;
		_inputTags = inputTags;
	}

	public static MePattern Crafting(AEKey output, long amount, int stationTile,
		IReadOnlyList<(AEKey what, long amount)> inputs, IReadOnlyList<string?>? inputTags = null)
	{
		var (ins, tags) = FilterInputs(inputs, inputTags);
		return new(MePatternType.Crafting, stationTile, StationKeyForTile(stationTile),
			ins, new[] { (output, amount) }, tags);
	}

	public static MePattern Processing(IReadOnlyList<(AEKey what, long amount)> inputs,
		IReadOnlyList<(AEKey what, long amount)> outputs, IReadOnlyList<string?>? inputTags = null)
	{
		var (ins, tags) = FilterInputs(inputs, inputTags);
		return new(MePatternType.Processing, -1, null, ins, FilterStacks(outputs), tags);
	}

	internal static string? StationKeyForTile(int tileType)
	{
		if (tileType < 0) return null;
		return CraftingStationRegistry.TryGetStationKey(tileType, out var k) ? k : null;
	}

	private static (int station, string? key) NormalizeStation(int station, string? key,
		List<(AEKey what, long amount)> outputs, List<(AEKey what, long amount)> inputs)
	{
		if (key != null)
		{
			if (CraftingStationRegistry.TryGetTile(key, out int t)) return (t, key);
		}
		else
		{
			if (station < 0) return (-1, null);
			if (station < TileID.Count) return (station, null);
		}

		int rederived = CraftingRecipeResolver.ReDeriveStationTile(outputs, inputs);
		if (rederived >= 0) return (rederived, StationKeyForTile(rederived));
		return (station, key);
	}

	private static ((AEKey, long)[] inputs, string?[] tags) FilterInputs(
		IReadOnlyList<(AEKey what, long amount)> src, IReadOnlyList<string?>? tags)
	{
		var inList = new List<(AEKey, long)>(src.Count);
		var tagList = new List<string?>(src.Count);
		for (int i = 0; i < src.Count; i++)
		{
			var e = src[i];
			if (e.what != null && e.amount > 0)
			{
				inList.Add((e.what, e.amount));
				tagList.Add(tags != null && i < tags.Count ? tags[i] : null);
			}
		}
		return (inList.ToArray(), tagList.ToArray());
	}

	private static (AEKey, long)[] FilterStacks(IReadOnlyList<(AEKey what, long amount)> src)
	{
		var list = new List<(AEKey, long)>(src.Count);
		foreach (var e in src)
			if (e.what != null && e.amount > 0) list.Add((e.what, e.amount));
		return list.ToArray();
	}

	public IReadOnlyList<(AEKey what, long amount)> Inputs => _inputs;
	public IReadOnlyList<(AEKey what, long amount)> Outputs => _outputs;

	public string? InputTag(int i) => i >= 0 && i < _inputTags.Length ? _inputTags[i] : null;

	public override bool Equals(object? obj)
	{
		if (obj is not MePattern o) return false;
		if (Type != o.Type || StationTile != o.StationTile) return false;
		return StacksEqual(_inputs, o._inputs) && StacksEqual(_outputs, o._outputs)
			&& TagsEqual(_inputTags, o._inputTags);
	}

	private static bool StacksEqual((AEKey what, long amount)[] a, (AEKey what, long amount)[] b)
	{
		if (a.Length != b.Length) return false;
		for (int i = 0; i < a.Length; i++)
			if (a[i].amount != b[i].amount || !a[i].what.Equals(b[i].what)) return false;
		return true;
	}

	private static bool TagsEqual(string?[] a, string?[] b)
	{
		if (a.Length != b.Length) return false;
		for (int i = 0; i < a.Length; i++)
			if (a[i] != b[i]) return false;
		return true;
	}

	public override int GetHashCode()
	{
		var hc = new System.HashCode();
		hc.Add((byte)Type);
		hc.Add(StationTile);
		for (int i = 0; i < _inputs.Length; i++)
		{
			hc.Add(_inputs[i].what);
			hc.Add(_inputs[i].amount);
			hc.Add(_inputTags[i]);
		}
		foreach (var (what, amount) in _outputs) { hc.Add(what); hc.Add(amount); }
		return hc.ToHashCode();
	}

	public AEKey PrimaryOutput => _outputs[0].what;
	public long PrimaryOutputAmount => _outputs[0].amount;

	public TagCompound Encode()
	{
		var t = new TagCompound
		{
			["type"] = (byte)Type,
			["station"] = StationTile,
			["in"] = WriteInputs(),
			["out"] = WriteStacks(_outputs),
		};
		if (StationKey != null) t["stationkey"] = StationKey;
		return t;
	}

	public static MePattern? Decode(TagCompound tag)
	{
		if (!tag.ContainsKey("type")) return null;
		var type = (MePatternType)tag.GetByte("type");
		int station = tag.ContainsKey("station") ? tag.GetInt("station") : -1;
		string? stationKey = tag.ContainsKey("stationkey") ? tag.GetString("stationkey") : null;
		var (inputs, tags) = ReadInputs(tag, "in");
		var outputs = ReadStacks(tag, "out");
		if (outputs.Count == 0) return null;

		if (type == MePatternType.Crafting)
			(station, stationKey) = NormalizeStation(station, stationKey, outputs, inputs);

		return new MePattern(type, station, stationKey, inputs.ToArray(), outputs.ToArray(), tags.ToArray());
	}

	private List<TagCompound> WriteInputs()
	{
		var list = new List<TagCompound>(_inputs.Length);
		for (int i = 0; i < _inputs.Length; i++)
		{
			var t = new TagCompound { ["k"] = _inputs[i].what.ToTagGeneric(), ["n"] = _inputs[i].amount };
			if (_inputTags[i] != null) t["tag"] = _inputTags[i]!;
			list.Add(t);
		}
		return list;
	}

	private static List<TagCompound> WriteStacks((AEKey what, long amount)[] stacks)
	{
		var list = new List<TagCompound>(stacks.Length);
		foreach (var (what, amount) in stacks)
			list.Add(new TagCompound { ["k"] = what.ToTagGeneric(), ["n"] = amount });
		return list;
	}

	private static (List<(AEKey, long)>, List<string?>) ReadInputs(TagCompound tag, string key)
	{
		var ins = new List<(AEKey, long)>();
		var tags = new List<string?>();
		if (!tag.ContainsKey(key)) return (ins, tags);
		foreach (var e in tag.GetList<TagCompound>(key))
		{
			var k = AEKey.FromTagGeneric(e.GetCompound("k"));
			if (k != null)
			{
				ins.Add((k, e.GetLong("n")));
				tags.Add(e.ContainsKey("tag") ? e.GetString("tag") : null);
			}
		}
		return (ins, tags);
	}

	private static List<(AEKey what, long amount)> ReadStacks(TagCompound tag, string key)
	{
		var result = new List<(AEKey, long)>();
		if (!tag.ContainsKey(key)) return result;
		foreach (var e in tag.GetList<TagCompound>(key))
		{
			var k = AEKey.FromTagGeneric(e.GetCompound("k"));
			if (k != null) result.Add((k, e.GetLong("n")));
		}
		return result;
	}
}
