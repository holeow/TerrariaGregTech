#nullable enable
using System;
using System.Collections.Generic;
using System.Numerics;
using GregTechCEuTerraria.Api.Machine.Multiblock;
using GregTechCEuTerraria.Api.Machine.Trait;
using Terraria.ModLoader.IO;

namespace GregTechCEuTerraria.TerrariaCompat.Machine.Multiblock.Electric;

public sealed class PowerStationEnergyBank : MachineTrait
{
	public static readonly MachineTraitType<PowerStationEnergyBank> TYPE = new(allowMultipleInstances: false);
	public override MachineTraitType TraitType => TYPE;

	private const string NbtSize   = "Size";
	private const string NbtStored = "Stored";
	private const string NbtMax    = "Max";

	private long[] _storage   = Array.Empty<long>();
	private long[] _maximums  = Array.Empty<long>();
	private int    _index;
	private BigInteger _capacity = BigInteger.Zero;

	public BigInteger Capacity => _capacity;

	public PowerStationEnergyBank(IReadOnlyList<IBatteryData> batteries) : base()
	{
		SetupBatteries(batteries);
	}

	private void SetupBatteries(IReadOnlyList<IBatteryData> batteries)
	{
		_storage  = new long[batteries.Count];
		_maximums = new long[batteries.Count];
		for (int i = 0; i < batteries.Count; i++)
			_maximums[i] = batteries[i].Capacity;
		_capacity = Summarize(_maximums);
	}

	public void Rebuild(IReadOnlyList<IBatteryData> batteries)
	{
		if (batteries.Count == 0)
			throw new System.InvalidOperationException(
				"Cannot rebuild Power Substation power bank with no batteries!");
		long[] oldStorage = (long[])_storage.Clone();
		SetupBatteries(batteries);
		_index = 0;
		foreach (long stored in oldStorage) Fill(stored);
	}

	public long Fill(long amount)
	{
		if (amount < 0)
			throw new System.ArgumentException("Amount cannot be negative!", nameof(amount));

		if (_index != _storage.Length - 1 && _storage[_index] == _maximums[_index])
			_index++;

		long maxFill = System.Math.Min(_maximums[_index] - _storage[_index], amount);

		if (maxFill == 0 && _index == _storage.Length - 1) return 0;

		_storage[_index] += maxFill;
		amount -= maxFill;

		if (amount > 0 && _index != _storage.Length - 1)
			return maxFill + Fill(amount);

		return maxFill;
	}

	public long Drain(long amount)
	{
		if (amount < 0)
			throw new System.ArgumentException("Amount cannot be negative!", nameof(amount));

		if (_index != 0 && _storage[_index] == 0) _index--;

		long maxDrain = System.Math.Min(_storage[_index], amount);

		if (maxDrain == 0 && _index == 0) return 0;

		_storage[_index] -= maxDrain;
		amount -= maxDrain;

		if (amount > 0 && _index != 0)
		{
			_index--;
			return maxDrain + Drain(amount);
		}

		return maxDrain;
	}

	public BigInteger GetStored() => Summarize(_storage);

	public bool HasEnergy()
	{
		foreach (long l in _storage) if (l > 0) return true;
		return false;
	}

	private static BigInteger Summarize(long[] values)
	{
		BigInteger retVal = BigInteger.Zero;
		long currentSum = 0;
		foreach (long value in values)
		{
			if (currentSum != 0 && value > long.MaxValue - currentSum)
			{
				retVal += new BigInteger(currentSum);
				currentSum = 0;
			}
			currentSum += value;
		}
		if (currentSum != 0) retVal += new BigInteger(currentSum);
		return retVal;
	}

	public long GetPassiveDrainPerTick()
	{
		long[] maxExcl = new long[_maximums.Length];
		int idx = 0;
		int numExcl = 0;
		foreach (long maximum in _maximums)
		{
			if (maximum / PowerSubstationMachine.PassiveDrainDivisor >=
			    PowerSubstationMachine.PassiveDrainMaxPerStorage)
				numExcl++;
			else
				maxExcl[idx++] = maximum;
		}
		System.Array.Resize(ref maxExcl, idx);
		BigInteger capacityExcl = Summarize(maxExcl);

		return (long)(
			capacityExcl / new BigInteger(PowerSubstationMachine.PassiveDrainDivisor)
			+ new BigInteger(PowerSubstationMachine.PassiveDrainMaxPerStorage * numExcl));
	}

	public override void Save(TagCompound tag)
	{
		tag[NbtSize] = _storage.Length;
		for (int i = 0; i < _storage.Length; i++)
		{
			var sub = new TagCompound();
			if (_storage[i] > 0) sub[NbtStored] = _storage[i];
			sub[NbtMax] = _maximums[i];
			tag[i.ToString()] = sub;
		}
	}

	public override void Load(TagCompound tag)
	{
		int size = tag.GetInt(NbtSize);
		_storage  = new long[size];
		_maximums = new long[size];
		for (int i = 0; i < size; i++)
		{
			var sub = tag.Get<TagCompound>(i.ToString());
			if (sub.ContainsKey(NbtStored)) _storage[i] = sub.GetLong(NbtStored);
			_maximums[i] = sub.GetLong(NbtMax);
		}
		_capacity = Summarize(_maximums);
	}
}
