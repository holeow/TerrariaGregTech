#nullable enable
using System.Collections.Generic;
using System.IO;
using GregTechCEuTerraria.AppliedEnergistics.Api.Stacks;
using GregTechCEuTerraria.TerrariaCompat.AppliedEnergistics;
using GregTechCEuTerraria.TerrariaCompat.AppliedEnergistics.Crafting;
using GregTechCEuTerraria.TerrariaCompat.Machine;
using Terraria;

namespace GregTechCEuTerraria.TerrariaCompat.Net.Actions;

public sealed class MePatternEncodingAction : IMachineAction
{
	public enum Op : byte { SetMode = 0, SetSlot = 1, SetContents = 2, Clear = 3, Encode = 4, CycleOutput = 5, Scale = 6, SetTagAmount = 7 }

	public PacketType Type => PacketType.MePatternEncoding;

	private Op _op;
	private MePatternType _mode;
	private int _station;
	private bool _output;
	private int _slot;
	private AEKey? _key;
	private long _amount;
	private (AEKey what, long amount)[] _inputs = System.Array.Empty<(AEKey, long)>();
	private (AEKey what, long amount)[] _outputs = System.Array.Empty<(AEKey, long)>();
	private string?[] _inputTags = System.Array.Empty<string?>();

	public MePatternEncodingAction() { }

	public static MePatternEncodingAction SetMode(MePatternType mode) =>
		new() { _op = Op.SetMode, _mode = mode };
	public static MePatternEncodingAction SetSlot(bool output, int slot, AEKey? key, long amount) =>
		new() { _op = Op.SetSlot, _output = output, _slot = slot, _key = key, _amount = amount };
	public static MePatternEncodingAction SetContents(MePatternType mode, int station,
		(AEKey, long)[] inputs, (AEKey, long)[] outputs, string?[]? inputTags = null) =>
		new()
		{
			_op = Op.SetContents, _mode = mode, _station = station,
			_inputs = inputs, _outputs = outputs,
			_inputTags = inputTags ?? System.Array.Empty<string?>(),
		};
	public static MePatternEncodingAction Clear() => new() { _op = Op.Clear };
	public static MePatternEncodingAction Encode() => new() { _op = Op.Encode };
	public static MePatternEncodingAction CycleOutput() => new() { _op = Op.CycleOutput };
	public static MePatternEncodingAction Scale(bool multiply) => new() { _op = Op.Scale, _output = multiply };
	public static MePatternEncodingAction SetTagAmount(int slot, long amount) =>
		new() { _op = Op.SetTagAmount, _slot = slot, _amount = amount };

	public void Write(BinaryWriter w)
	{
		w.Write((byte)_op);
		switch (_op)
		{
			case Op.SetMode:
				w.Write((byte)_mode);
				break;
			case Op.SetSlot:
				w.Write(_output);
				w.Write((byte)_slot);
				AEKey.WriteOptionalKey(w, _key);
				w.Write(_amount);
				break;
			case Op.SetContents:
				w.Write((byte)_mode);
				w.Write(_station);
				WritePairs(w, _inputs);
				WritePairs(w, _outputs);
				w.Write((byte)_inputTags.Length);
				foreach (var t in _inputTags)
				{
					w.Write(t != null);
					if (t != null) w.Write(t);
				}
				break;
			case Op.Scale:
				w.Write(_output);
				break;
			case Op.SetTagAmount:
				w.Write((byte)_slot);
				w.Write(_amount);
				break;
		}
	}

	public void Read(BinaryReader r)
	{
		_op = (Op)r.ReadByte();
		switch (_op)
		{
			case Op.SetMode:
				_mode = (MePatternType)r.ReadByte();
				break;
			case Op.SetSlot:
				_output = r.ReadBoolean();
				_slot = r.ReadByte();
				_key = AEKey.ReadOptionalKey(r);
				_amount = r.ReadInt64();
				break;
			case Op.SetContents:
				_mode = (MePatternType)r.ReadByte();
				_station = r.ReadInt32();
				_inputs = ReadPairs(r);
				_outputs = ReadPairs(r);
				int nTags = r.ReadByte();
				_inputTags = new string?[nTags];
				for (int i = 0; i < nTags; i++)
					_inputTags[i] = r.ReadBoolean() ? r.ReadString() : null;
				break;
			case Op.Scale:
				_output = r.ReadBoolean();
				break;
			case Op.SetTagAmount:
				_slot = r.ReadByte();
				_amount = r.ReadInt64();
				break;
		}
	}

	private static void WritePairs(BinaryWriter w, (AEKey what, long amount)[] pairs)
	{
		w.Write((byte)pairs.Length);
		foreach (var (what, amount) in pairs)
		{
			AEKey.WriteOptionalKey(w, what);
			w.Write(amount);
		}
	}

	private static (AEKey what, long amount)[] ReadPairs(BinaryReader r)
	{
		int n = r.ReadByte();
		var list = new List<(AEKey, long)>(n);
		for (int i = 0; i < n; i++)
		{
			var key = AEKey.ReadOptionalKey(r);
			long amt = r.ReadInt64();
			if (key != null) list.Add((key, amt));
		}
		return list.ToArray();
	}

	public void Apply(MetaMachine entity, int byWhoAmI)
	{
		if (entity is not IMePatternEncodingHost host) return;
		var enc = host.Encoding;
		switch (_op)
		{
			case Op.SetMode:     enc.ApplySetMode(_mode); break;
			case Op.SetSlot:     enc.ApplySetSlot(_output, _slot, _key, _amount); break;
			case Op.SetContents: enc.ApplySetContents(_mode, _station, _inputs, _outputs, _inputTags); break;
			case Op.Clear:       enc.ApplyClear(); break;
			case Op.CycleOutput: enc.ApplyCycleOutput(); break;
			case Op.Scale:       enc.ApplyScale(_output); break;
			case Op.SetTagAmount: enc.ApplySetTagAmount(_slot, _amount); break;
			case Op.Encode:
				if (byWhoAmI >= 0 && byWhoAmI < Main.maxPlayers)
					enc.ApplyEncode(Main.player[byWhoAmI]);
				break;
		}
	}
}
