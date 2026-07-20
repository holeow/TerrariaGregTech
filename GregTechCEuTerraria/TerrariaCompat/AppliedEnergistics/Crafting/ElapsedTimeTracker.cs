// Ported to C#/Terraria for GregTechCEuTerraria from Applied Energistics 2
// (appeng.crafting.execution.ElapsedTimeTracker), Forge 1.20.1. LGPL-3.0-only. See AE2 LICENSE.
//
// Tracks elapsed wall-clock time + per-key-type started/completed work units
// (for the CPU status UI's progress + elapsed readout).
#nullable enable
using System.Collections.Generic;
using System.Diagnostics;
using GregTechCEuTerraria.AppliedEnergistics.Api.Stacks;
using Terraria.ModLoader.IO;

namespace GregTechCEuTerraria.TerrariaCompat.AppliedEnergistics.Crafting;

public sealed class ElapsedTimeTracker
{
	private static readonly double NsPerTick = 1_000_000_000.0 / Stopwatch.Frequency;
	private static long NowNs() => (long)(Stopwatch.GetTimestamp() * NsPerTick);

	private long _lastTime = NowNs();
	private long _elapsedTime = 0;
	private readonly Dictionary<AEKeyType, long> _started = new();
	private readonly Dictionary<AEKeyType, long> _completed = new();

	public ElapsedTimeTracker() { }

	public ElapsedTimeTracker(TagCompound data)
	{
		_elapsedTime = data.GetLong("elapsedTime");
		ReadMap(data.GetCompound("startedWork"), _started);
		ReadMap(data.GetCompound("completedWork"), _completed);
	}

	public TagCompound WriteToNBT()
	{
		var data = new TagCompound { ["elapsedTime"] = _elapsedTime };
		data["startedWork"] = WriteMap(_started);
		data["completedWork"] = WriteMap(_completed);
		return data;
	}

	private static void ReadMap(TagCompound tag, Dictionary<AEKeyType, long> output)
	{
		foreach (var keyType in AEKeyTypes.GetAll())
			if (tag.ContainsKey(keyType.GetId()))
				output[keyType] = tag.GetLong(keyType.GetId());
	}

	private static TagCompound WriteMap(Dictionary<AEKeyType, long> input)
	{
		var result = new TagCompound();
		foreach (var entry in input)
			result[entry.Key.GetId()] = entry.Value;
		return result;
	}

	private void UpdateTime()
	{
		long currentTime = NowNs();
		_elapsedTime += currentTime - _lastTime;
		_lastTime = currentTime;
	}

	private static long SaturatedSum(long a, long b)
	{
		var result = a + b;
		return result < 0 ? long.MaxValue : result;
	}

	public void DecrementItems(long itemDiff, AEKeyType keyType)
	{
		UpdateTime();
		_completed[keyType] = SaturatedSum(_completed.GetValueOrDefault(keyType), itemDiff);
	}

	public void AddMaxItems(long itemDiff, AEKeyType keyType)
	{
		UpdateTime();
		_started[keyType] = SaturatedSum(_started.GetValueOrDefault(keyType), itemDiff);
	}

	public long GetElapsedTime()
	{
		bool allDone = true;
		foreach (var keyType in AEKeyTypes.GetAll())
			if (_completed.GetValueOrDefault(keyType) < _started.GetValueOrDefault(keyType))
			{
				allDone = false;
				break;
			}
		return allDone ? _elapsedTime : _elapsedTime + (NowNs() - _lastTime);
	}

	public float GetProgress()
	{
		double started = 0, completed = 0;
		foreach (var keyType in AEKeyTypes.GetAll())
		{
			started += _started.GetValueOrDefault(keyType) / (double)keyType.GetAmountPerUnit();
			completed += _completed.GetValueOrDefault(keyType) / (double)keyType.GetAmountPerUnit();
		}
		if (started <= 0) return 0;
		var p = (float)(completed / started);
		return p < 0 ? 0 : (p > 1 ? 1 : p);
	}

	public long GetRemainingItemCount() => (long)(int.MaxValue - (double)GetProgress() * int.MaxValue);
	public long GetStartItemCount() => int.MaxValue;

	public string DescribeTotalStarted()
	{
		var parts = new List<string>();
		foreach (var keyType in AEKeyTypes.GetAll())
		{
			long started = _started.GetValueOrDefault(keyType);
			if (started <= 0) continue;
			long units = started / keyType.GetAmountPerUnit();
			parts.Add($"{keyType.FormatAmount(units, AmountFormat.FULL)} {keyType.GetDescription()}");
		}
		return string.Join(", ", parts);
	}
}
