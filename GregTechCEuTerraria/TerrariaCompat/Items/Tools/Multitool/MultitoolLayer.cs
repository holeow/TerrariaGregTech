#nullable enable
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;

namespace GregTechCEuTerraria.TerrariaCompat.Items.Tools.Multitool;

internal abstract class MultitoolLayer
{
	public abstract string Id { get; }
	public abstract string Name { get; }
	public virtual bool Enabled => true;

	public abstract int IconItem(Player p);

	protected int ArmedOrFirstIcon(Player p, List<MultitoolVariant> vs)
	{
		var armed = MultitoolState.ArmedFor(Id);
		if (armed != null)
		{
			int idx = vs.FindIndex(v => v.Key == armed);
			if (idx >= 0) return vs[idx].IconItem;
			if (TryBuildVariant(p, armed, out var v)) return v.IconItem;
		}
		return vs.Count > 0 ? vs[0].IconItem : 0;
	}

	public virtual IReadOnlyList<int> WidthOptions => Array.Empty<int>();
	public virtual string WidthLabel(int w) => "x" + w;

	public abstract List<MultitoolVariant> Variants(Player p);

	public virtual int AffordableTiles(in MultitoolVariant v, int width)
		=> width > 0 ? v.Units / width : v.Units;

	public abstract bool HasCellAt(int x, int y);

	public virtual bool TryPick(int x, int y, out string variantKey, out int width)
	{
		variantKey = ""; width = 0; return false;
	}

	public virtual bool TryBuildVariant(Player p, string key, out MultitoolVariant variant)
	{
		variant = default; return false;
	}

	public abstract int CommitPlace(Player p, IReadOnlyList<Point> path, in MultitoolVariant v, int width);
	public abstract int CommitCut(Player p, IReadOnlyList<Point> path);
}

internal readonly struct MultitoolVariant
{
	public readonly string Key;
	public readonly int IconItem;
	public readonly string ValueLabel;
	public readonly int Units;

	public MultitoolVariant(string key, int iconItem, string valueLabel, int units)
	{
		Key = key;
		IconItem = iconItem;
		ValueLabel = valueLabel;
		Units = units;
	}

	public bool IsValid => !string.IsNullOrEmpty(Key);
}

internal static class MultitoolLayers
{
	private static readonly List<MultitoolLayer> _all = new();

	public static IReadOnlyList<MultitoolLayer> All { get { Ensure(); return _all; } }

	private static void Ensure()
	{
		if (_all.Count > 0) return;
		_all.Add(new CableMultitoolLayer());
		_all.Add(new MeCableMultitoolLayer());
		_all.Add(new ItemPipeMultitoolLayer());
		_all.Add(new FluidPipeMultitoolLayer());
		_all.Add(new LaserMultitoolLayer());
		_all.Add(new OpticalMultitoolLayer());
		_all.Add(new LongDistanceMultitoolLayer());
	}

	public static MultitoolLayer Active
	{
		get
		{
			Ensure();
			return _all.Find(l => l.Id == MultitoolState.ActiveLayerId) ?? _all[0];
		}
	}

	public static void EnsureSelection(Player p)
	{
		var layer = Active;
		var opts = layer.WidthOptions;
		if (opts.Count > 0)
		{
			bool ok = false;
			foreach (var o in opts) if (o == MultitoolState.Width) { ok = true; break; }
			if (!ok) MultitoolState.Width = opts[0];
		}
		TryResolveArmedVariant(p, out _);
	}

	public static bool TryResolveArmedVariant(Player p, out MultitoolVariant variant)
	{
		var key = MultitoolState.ArmedVariantKey;
		var vs = Active.Variants(p);
		if (key != null)
		{
			int idx = vs.FindIndex(v => v.Key == key);
			if (idx >= 0) { variant = vs[idx]; return true; }
			if (Active.TryBuildVariant(p, key, out variant)) return true;
		}
		if (vs.Count == 0) { variant = default; return false; }
		MultitoolState.ArmedVariantKey = vs[0].Key;
		variant = vs[0];
		return true;
	}
}
