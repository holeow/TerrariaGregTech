#nullable enable
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using GregTechCEuTerraria.Api.Machine.Feature;

namespace GregTechCEuTerraria.TerrariaCompat.Machine;

internal static class MachineLoopVoiceArbiter
{
	private readonly struct Want
	{
		public readonly string Path;
		public readonly int Max;
		public Want(string path, int max) { Path = path; Max = max; }
	}

	private static readonly Dictionary<MetaMachine, Want> _wants = new();

	private static readonly Dictionary<string, List<(MetaMachine m, float distSq, int max)>> _groups = new();
	private static readonly List<MetaMachine> _dead = new();

	public static void SetWant(MetaMachine machine)
	{
		if (Main.dedServ || machine is not IRecipeLogicMachine rl) return;
		var style = StationSounds.TryGetLoop(rl.GetRecipeType()?.RegistryName ?? "");
		if (style is null) { ClearWant(machine); return; }
		_wants[machine] = new Want(style.Value.SoundPath ?? "", Math.Max(1, style.Value.MaxInstances));
	}

	public static void ClearWant(MetaMachine machine)
	{
		if (_wants.Remove(machine) && machine is IRecipeLogicMachine rl)
			rl.StopLoopSound();
	}

	public static void Update()
	{
		if (_wants.Count == 0) return;

		Vector2 cam = Main.Camera.Center;

		foreach (var kvp in _wants)
		{
			var m = kvp.Key;
			if (!TileEntity.ByID.ContainsKey(m.ID)) { _dead.Add(m); continue; }
			float cx = m.Position.X * 16f + m.Size.Width * 8f;
			float cy = m.Position.Y * 16f + m.Size.Height * 8f;
			float dx = cx - cam.X, dy = cy - cam.Y;
			if (!_groups.TryGetValue(kvp.Value.Path, out var list))
				_groups[kvp.Value.Path] = list = new();
			list.Add((m, dx * dx + dy * dy, kvp.Value.Max));
		}

		if (_dead.Count > 0)
		{
			foreach (var m in _dead) ClearWant(m);
			_dead.Clear();
		}

		foreach (var kv in _groups)
		{
			var list = kv.Value;
			if (list.Count == 0) continue;
			list.Sort(static (a, b) => a.distSq.CompareTo(b.distSq));
			int cap = int.MaxValue;
			foreach (var e in list) cap = Math.Min(cap, e.max);
			for (int i = 0; i < list.Count; i++)
			{
				var rl = (IRecipeLogicMachine)list[i].m;
				if (i < cap) rl.EnsureLoopSound(rl.GetWorldPos());
				else         rl.StopLoopSound();
			}
			list.Clear();
		}
	}

	public static void ClearAll()
	{
		foreach (var kvp in _wants)
			if (kvp.Key is IRecipeLogicMachine rl) rl.StopLoopSound();
		_wants.Clear();
		foreach (var kv in _groups) kv.Value.Clear();
		_dead.Clear();
	}
}
