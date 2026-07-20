#nullable enable
using System;
using System.Collections.Generic;
using GregTechCEuTerraria.AppliedEnergistics.Api.Stacks;
using GregTechCEuTerraria.AppliedEnergistics.Helpers.ExternalStorage;
using GregTechCEuTerraria.TerrariaCompat.AppliedEnergistics.Crafting;
using GregTechCEuTerraria.TerrariaCompat.Items.Patterns;
using Terraria;
using Terraria.ModLoader.IO;

namespace GregTechCEuTerraria.TerrariaCompat.AppliedEnergistics;

public sealed class PatternEncodingState
{
	public const int InputSlots = 25;
	public const int OutputSlots = 5;

	private readonly GenericStackInv _inputs = new(null, GenericStackInv.Mode.CONFIG_STACKS, InputSlots);
	private readonly GenericStackInv _outputs = new(null, GenericStackInv.Mode.CONFIG_STACKS, OutputSlots);
	private MePatternType _mode = MePatternType.Crafting;
	private string[] _stations = System.Array.Empty<string>();

	private readonly string?[] _inputTag = new string?[InputSlots];

	public string? GetTag(int slot) =>
		slot >= 0 && slot < _inputTag.Length ? _inputTag[slot] : null;

	public bool HasAlternatives(int slot) => GetTag(slot) != null;

	public IReadOnlyList<int> GetAlternatives(int slot)
	{
		var tag = GetTag(slot);
		if (tag == null) return System.Array.Empty<int>();
		return Api.Recipe.Ingredient.IIngredientResolver.Default?.ResolveItemTag(tag)
			?? System.Array.Empty<int>();
	}

	public IReadOnlyList<string> GetFluidAlternatives(int slot)
	{
		var tag = GetTag(slot);
		if (tag == null) return System.Array.Empty<string>();
		var fluids = Api.Recipe.Ingredient.IIngredientResolver.Default?.ResolveFluidTag(tag);
		if (fluids == null) return System.Array.Empty<string>();
		var ids = new List<string>(fluids.Count);
		foreach (var f in fluids) ids.Add(f.Id);
		return ids;
	}

	private bool IsAllowedAlternative(int slot, AEKey key)
	{
		if (key is AEItemKey ik)
		{
			foreach (int t in GetAlternatives(slot))
				if (t == ik.GetItem()) return true;
		}
		else if (key is AEFluidKey fk)
		{
			string id = fk.GetFluid().Id;
			foreach (var fid in GetFluidAlternatives(slot))
				if (fid == id) return true;
		}
		return false;
	}

	private readonly Item[] _blank = { new() };
	private readonly Item[] _encoded = { new() };

	public MePatternType Mode => _mode;
	public IReadOnlyList<string> Stations => _stations;
	public bool HasEncodedOutput => _encoded[0].ModItem is EncodedPatternItem;
	public GenericStackInv Inputs => _inputs;
	public GenericStackInv Outputs => _outputs;
	public Item[] Blank => _blank;
	public Item[] Encoded => _encoded;

	public bool AcceptsEncodedDeposit(int index, Item item) =>
		index == 0 && item.ModItem is EncodedPatternItem e && e.Pattern != null;

	public void OnEncodedSlotChanged()
	{
		if (_encoded[0].ModItem is EncodedPatternItem e && e.Pattern is { } p)
		{
			var tags = new List<string?>(p.Inputs.Count);
			for (int i = 0; i < p.Inputs.Count; i++) tags.Add(p.InputTag(i));
			ApplySetContents(p.Type,
				p.Type == MePatternType.Crafting ? p.StationIds : System.Array.Empty<string>(),
				p.Inputs, p.Outputs, tags);
		}
	}

	public void ApplySetMode(MePatternType mode) => _mode = mode;

	private int? _encSig;
	private bool _canEncode;
	public bool CanEncode
	{
		get
		{
			int sig = EncodeSignature();
			if (_encSig != sig) { _encSig = sig; _canEncode = ComputeCanEncode(); }
			return _canEncode;
		}
	}

	private int EncodeSignature()
	{
		var hc = new HashCode();
		hc.Add((byte)_mode);
		foreach (var s in _stations) hc.Add(s);
		for (int i = 0; i < _inputs.Size(); i++) { var s = _inputs.GetStack(i); hc.Add(s?.What); hc.Add(s?.Amount ?? 0); }
		for (int i = 0; i < _outputs.Size(); i++) { var s = _outputs.GetStack(i); hc.Add(s?.What); hc.Add(s?.Amount ?? 0); }
		return hc.ToHashCode();
	}

	private bool ComputeCanEncode()
	{
		var outputs = Collect(_outputs);
		if (outputs.Count == 0) return false;
		if (_mode == MePatternType.Processing)
			return Collect(_inputs).Count > 0;
		var pattern = MePattern.Crafting(outputs[0].what, outputs[0].amount, _stations, Collect(_inputs));
		return CraftingRecipeResolver.Find(pattern) != null;
	}

	public void ApplySetSlot(bool output, int slot, AEKey? key, long amount)
	{
		var inv = output ? _outputs : _inputs;
		if (slot < 0 || slot >= inv.Size()) return;

		if (!output && GetTag(slot) != null)
		{
			if (key == null)
			{
				if (_mode != MePatternType.Processing) return;
				inv.SetStack(slot, null);
				_inputTag[slot] = null;
				return;
			}
			if (!IsAllowedAlternative(slot, key)) return;
			long amt = inv.GetStack(slot)?.Amount ?? 1;
			inv.SetStack(slot, new GenericStack(key, amt));
			_inputTag[slot] = null;
			return;
		}

		if (_mode != MePatternType.Processing) return;

		inv.SetStack(slot, key == null ? null : new GenericStack(key, Math.Max(1, amount)));
	}

	public void ApplySetTagAmount(int slot, long amount)
	{
		if (slot < 0 || slot >= _inputs.Size() || GetTag(slot) == null) return;
		var s = _inputs.GetStack(slot);
		if (s == null) return;
		_inputs.SetStack(slot, new GenericStack(s.What, Math.Max(1, amount)));
	}

	public void ApplySetContents(MePatternType mode, IReadOnlyList<string> stations,
		IReadOnlyList<(AEKey what, long amount)> inputs,
		IReadOnlyList<(AEKey what, long amount)> outputs,
		IReadOnlyList<string?>? inputTags = null)
	{
		_mode = mode;
		_stations = ToStationArray(stations);
		_inputs.Clear();
		_outputs.Clear();
		System.Array.Clear(_inputTag);
		for (int i = 0; i < inputs.Count && i < InputSlots; i++)
		{
			_inputs.SetStack(i, new GenericStack(inputs[i].what, Math.Max(1, inputs[i].amount)));
			if (inputTags != null && i < inputTags.Count)
				_inputTag[i] = inputTags[i];
		}
		for (int i = 0; i < outputs.Count && i < OutputSlots; i++)
			_outputs.SetStack(i, new GenericStack(outputs[i].what, Math.Max(1, outputs[i].amount)));
	}

	public void ApplyClear()
	{
		_inputs.Clear();
		_outputs.Clear();
		System.Array.Clear(_inputTag);
		_stations = System.Array.Empty<string>();
	}

	private static string[] ToStationArray(IReadOnlyList<string> stations)
	{
		if (stations.Count == 0) return System.Array.Empty<string>();
		var list = new List<string>(stations.Count);
		foreach (var s in stations)
			if (!string.IsNullOrEmpty(s) && !list.Contains(s)) list.Add(s);
		list.Sort(System.StringComparer.Ordinal);
		return list.Count == 0 ? System.Array.Empty<string>() : list.ToArray();
	}

	public bool CanCycleOutputs
	{
		get
		{
			if (_mode != MePatternType.Processing) return false;
			int count = 0;
			for (int i = 0; i < _outputs.Size(); i++)
				if (_outputs.GetStack(i) != null) count++;
			return count > 1;
		}
	}

	public void ApplyCycleOutput()
	{
		if (_mode != MePatternType.Processing) return;
		int len = _outputs.Size();
		var newOutputs = new GenericStack?[len];
		for (int i = 0; i < len; i++)
		{
			if (_outputs.GetStack(i) == null) continue;
			for (int j = 1; j < len; j++)
			{
				var next = _outputs.GetStack((i + j) % len);
				if (next != null) { newOutputs[i] = next; break; }
			}
		}
		for (int i = 0; i < len; i++)
			_outputs.SetStack(i, newOutputs[i]);
	}

	public bool CanScale => _mode == MePatternType.Processing && (!_inputs.IsEmpty() || !_outputs.IsEmpty());
	public bool CanHalve => CanScale && AllEven(_inputs) && AllEven(_outputs);

	public void ApplyScale(bool multiply)
	{
		if (!CanScale) return;
		if (!multiply && !CanHalve) return;
		ScaleInv(_inputs, multiply);
		ScaleInv(_outputs, multiply);
	}

	private static void ScaleInv(GenericStackInv inv, bool multiply)
	{
		for (int i = 0; i < inv.Size(); i++)
		{
			var s = inv.GetStack(i);
			if (s == null) continue;
			long amt = multiply ? s.Amount * 2 : s.Amount / 2;
			if (amt < 1) amt = 1;
			inv.SetStack(i, new GenericStack(s.What, amt));
		}
	}

	private static bool AllEven(GenericStackInv inv)
	{
		for (int i = 0; i < inv.Size(); i++)
		{
			var s = inv.GetStack(i);
			if (s != null && s.Amount % 2 != 0) return false;
		}
		return true;
	}

	public void ApplyEncode(Player player)
	{
		if (!CanEncode) { ClearPattern(); return; }

		var inputs = Collect(_inputs);
		var outputs = Collect(_outputs);
		MePattern pattern;
		var tags = CollectInputTags();
		if (_mode == MePatternType.Crafting)
		{
			if (outputs.Count == 0) return;
			pattern = MePattern.Crafting(outputs[0].what, outputs[0].amount, _stations, inputs, tags);
		}
		else
		{
			if (inputs.Count == 0 || outputs.Count == 0) return;
			pattern = MePattern.Processing(inputs, outputs, tags);
		}

		var outItem = _encoded[0];
		bool outIsPattern = !outItem.IsAir
			&& (outItem.ModItem is EncodedPatternItem || outItem.ModItem is BlankPatternItem);
		if (!outItem.IsAir && !outIsPattern) return;

		if (outItem.IsAir && !Config.GTConfig.Instance.FreeMePatterns)
		{
			if (_blank[0].IsAir) return;
			_blank[0].stack--;
			if (_blank[0].stack <= 0) _blank[0].TurnToAir();
		}
		_encoded[0] = EncodedPatternItem.Create(pattern);
	}

	private void ClearPattern()
	{
		if (_encoded[0].ModItem is not EncodedPatternItem) return;
		int count = _encoded[0].stack;
		var blank = new Item();
		blank.SetDefaults(Terraria.ModLoader.ModContent.ItemType<BlankPatternItem>());
		blank.stack = count;
		_encoded[0] = blank;
	}

	private static List<(AEKey what, long amount)> Collect(GenericStackInv inv)
	{
		var list = new List<(AEKey, long)>();
		for (int i = 0; i < inv.Size(); i++)
		{
			var s = inv.GetStack(i);
			if (s != null) list.Add((s.What, s.Amount));
		}
		return list;
	}

	private List<string?> CollectInputTags()
	{
		var list = new List<string?>();
		for (int i = 0; i < _inputs.Size(); i++)
			if (_inputs.GetStack(i) != null) list.Add(_inputTag[i]);
		return list;
	}

	public void Save(TagCompound tag)
	{
		tag["mode"] = (byte)_mode;
		if (_stations.Length > 0) tag["stations"] = new List<string>(_stations);
		_inputs.WriteToChildTag(tag, "in");
		_outputs.WriteToChildTag(tag, "out");
		for (int i = 0; i < _inputTag.Length; i++)
			if (_inputTag[i] != null) tag[$"tag{i}"] = _inputTag[i]!;
		if (!_blank[0].IsAir) tag["blank"] = ItemIO.Save(_blank[0]);
		if (!_encoded[0].IsAir) tag["enc"] = ItemIO.Save(_encoded[0]);
	}

	public void Load(TagCompound tag)
	{
		_mode = tag.ContainsKey("mode") ? (MePatternType)tag.GetByte("mode") : MePatternType.Crafting;
		_stations = tag.ContainsKey("stations")
			? ToStationArray(new List<string>(tag.GetList<string>("stations")))
			: System.Array.Empty<string>();
		_inputs.ReadFromChildTag(tag, "in");
		_outputs.ReadFromChildTag(tag, "out");
		for (int i = 0; i < _inputTag.Length; i++)
			_inputTag[i] = tag.ContainsKey($"tag{i}") ? tag.GetString($"tag{i}") : null;
		_blank[0] = tag.ContainsKey("blank") ? ItemIO.Load(tag.GetCompound("blank")) : new Item();
		_encoded[0] = tag.ContainsKey("enc") ? ItemIO.Load(tag.GetCompound("enc")) : new Item();
	}
}
