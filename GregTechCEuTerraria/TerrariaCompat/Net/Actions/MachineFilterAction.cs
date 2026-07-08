#nullable enable
using System.IO;
using GregTechCEuTerraria.Api.Cover.Filter;
using GregTechCEuTerraria.TerrariaCompat.Machine;
using Terraria;

namespace GregTechCEuTerraria.TerrariaCompat.Net.Actions;

public sealed class MachineFilterAction : IMachineAction
{
	public enum Op : byte
	{
		MatcherClick    = 0,
		ToggleBlacklist = 1,
		ToggleIgnoreNbt = 2,
		CycleFilterType = 3,
		SetTagExpr      = 4,
		SetAmount       = 5,
	}

	public PacketType Type => PacketType.MachineFilter;

	private Op _op;
	private int _index;
	private byte _button;
	private bool _shift;
	private Item _held = new();
	private string _text = "";
	private long _amount;

	public MachineFilterAction() { }

	public static MachineFilterAction Matcher(int index, int button, bool shift, Item held) =>
		new() { _op = Op.MatcherClick, _index = index, _button = (byte)button, _shift = shift, _held = held.Clone() };

	public static MachineFilterAction SetAmount(int index, long amount) =>
		new() { _op = Op.SetAmount, _index = index, _amount = amount };

	public static MachineFilterAction Toggle(Op toggleOp) =>
		new() { _op = toggleOp };

	public static MachineFilterAction Cycle() => new() { _op = Op.CycleFilterType };

	public static MachineFilterAction TagExpr(string expr) =>
		new() { _op = Op.SetTagExpr, _text = expr ?? "" };

	public void Write(BinaryWriter w)
	{
		w.Write((byte)_op);
		w.Write(_index);
		w.Write(_button);
		w.Write(_shift);
		w.WriteItem(_held);
		w.Write(_text);
		w.Write(_amount);
	}

	public void Read(BinaryReader r)
	{
		_op = (Op)r.ReadByte();
		_index = r.ReadInt32();
		_button = r.ReadByte();
		_shift = r.ReadBoolean();
		_held = r.ReadItem();
		_text = r.ReadString();
		_amount = r.ReadInt64();
	}

	public void Apply(MetaMachine entity, int byWhoAmI)
	{
		if (entity is not IFilterableMachine fm) return;

		switch (_op)
		{
			case Op.MatcherClick:
				ItemFilterEdit.MatcherClick(fm.SimpleFilter, _index, _button, _shift, _held);
				break;

			case Op.ToggleBlacklist:
				fm.SimpleFilter.SetBlackList(!fm.SimpleFilter.IsBlackList);
				break;

			case Op.ToggleIgnoreNbt:
				fm.SimpleFilter.SetIgnoreNbt(!fm.SimpleFilter.IgnoreNbt);
				break;

			case Op.CycleFilterType:
				fm.FilterOrdinal = fm.FilterOrdinal == 1 ? 0 : 1;
				break;

			case Op.SetTagExpr:
				fm.TagFilter.SetOreDict(TagItemFilter.NormalizeExpression(_text));
				break;

			case Op.SetAmount:
				if (_index >= 0 && _index < fm.SimpleFilter.Matches.Length && !fm.SimpleFilter.Matches[_index].IsAir)
				{
					fm.SimpleFilter.Matches[_index].stack =
						(int)System.Math.Clamp(_amount, 1, fm.SimpleFilter.MaxStackSize);
					fm.SimpleFilter.OnUpdated();
				}
				break;
		}
	}
}
