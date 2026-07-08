#nullable enable
using System;
using System.Collections.Generic;
using GregTechCEuTerraria.AppliedEnergistics.Api.Stacks;
using GregTechCEuTerraria.TerrariaCompat.AppliedEnergistics.Crafting;
using GregTechCEuTerraria.TerrariaCompat.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GregTechCEuTerraria.TerrariaCompat.UI.MeCraft;

public sealed class CraftGraphView : GraphRenderer
{
	private const float ColW = 96f;
	private const float RowH = 50f;
	private const float NodeSize = 44f;

	private readonly Dictionary<string, AEKey> _keyById = new();

	public CraftGraphView()
	{
		Background = new Color(16, 18, 40);
		FitMaxZoom = 1.2f;
	}

	protected override bool NodesInteractive => true;

	protected override void DrawOverlay(SpriteBatch sb)
	{
		if (Hovered is null) return;
		if (!_keyById.TryGetValue(Hovered, out var key)) return;
		var click = BrowserSlotInteraction.Poll();
		if (key is AEItemKey ik)
			BrowserSlotInteraction.HandleItem(click, ik.GetItem(), inFavoritesPane: false);
		else if (key is AEFluidKey fk)
			BrowserSlotInteraction.HandleFluid(click, fk.GetFluid(), recipeAmountMb: null, inFavoritesPane: false);
	}

	public void Build(CraftingPlanSummary summary, bool missingOnly = false)
	{
		_keyById.Clear();
		var byKey = new Dictionary<AEKey, CraftingPlanSummaryEntry>();
		foreach (var e in summary.Entries) byKey[e.What] = e;

		var patterns = summary.Patterns;

		var producerOf = new Dictionary<AEKey, (int p, int j)>();
		for (int p = 0; p < patterns.Count; p++)
			for (int j = 0; j < patterns[p].Outputs.Length; j++)
				producerOf.TryAdd(patterns[p].Outputs[j].what, (p, j));

		var nodeKey = new List<AEKey>();
		var nodeAmt = new List<long>();
		var nodeKind = new List<byte>();   // 0 = produced, 1 = raw/stored, 2 = raw/missing
		var indexOf = new Dictionary<string, int>();
		int AddNode(string id, AEKey key, long amount, byte kind)
		{
			if (indexOf.TryGetValue(id, out int ex)) return ex;
			int i = nodeKey.Count;
			nodeKey.Add(key); nodeAmt.Add(amount); nodeKind.Add(kind);
			indexOf[id] = i;
			return i;
		}

		for (int p = 0; p < patterns.Count; p++)
		{
			long t = patterns[p].Times;
			var outs = patterns[p].Outputs;
			for (int j = 0; j < outs.Length; j++)
				AddNode($"o:{p}:{j}", outs[j].what, outs[j].amount * t, 0);
		}

		var rawEdges = new List<(int from, int to)>();
		for (int p = 0; p < patterns.Count; p++)
		{
			long t = patterns[p].Times;
			var ins = patterns[p].Inputs;
			var outs = patterns[p].Outputs;
			for (int kk = 0; kk < ins.Length; kk++)
			{
				AEKey inKey = ins[kk].what;
				long inAmt = ins[kk].amount * t;
				int from;
				if (producerOf.TryGetValue(inKey, out var prod))
					from = indexOf[$"o:{prod.p}:{prod.j}"];
				else
				{
					byKey.TryGetValue(inKey, out var e);
					byte kind = (byte)(e != null && e.MissingAmount > 0 ? 2 : 1);
					from = AddNode($"r:{p}:{kk}", inKey, inAmt, kind);
				}
				for (int j = 0; j < outs.Length; j++)
				{
					int to = indexOf[$"o:{p}:{j}"];
					if (from != to) rawEdges.Add((from, to));
				}
			}
		}

		int n = nodeKey.Count;

		var incoming = new Dictionary<int, List<int>>();
		var outgoing = new Dictionary<int, List<int>>();
		var edgeSet = new HashSet<long>();
		var edgePairs = new List<(int from, int to)>();
		foreach (var (a, b) in rawEdges)
		{
			long sig = ((long)a << 32) | (uint)b;
			if (!edgeSet.Add(sig)) continue;
			edgePairs.Add((a, b));
			if (!incoming.TryGetValue(b, out var l)) incoming[b] = l = new();
			l.Add(a);
			if (!outgoing.TryGetValue(a, out var o)) outgoing[a] = o = new();
			o.Add(b);
		}

		var keep = new bool[n];
		if (missingOnly)
		{
			var fwd = new bool[n];
			var bwd = new bool[n];
			var stack = new Stack<int>();
			for (int i = 0; i < n; i++)
				if (nodeKind[i] == 2) { fwd[i] = true; bwd[i] = true; stack.Push(i); }
			while (stack.Count > 0)
			{
				int v = stack.Pop();
				if (outgoing.TryGetValue(v, out var outs))
					foreach (int w in outs) if (!fwd[w]) { fwd[w] = true; stack.Push(w); }
			}
			for (int i = 0; i < n; i++) if (bwd[i]) stack.Push(i);
			while (stack.Count > 0)
			{
				int v = stack.Pop();
				if (incoming.TryGetValue(v, out var preds))
					foreach (int a in preds) if (!bwd[a]) { bwd[a] = true; stack.Push(a); }
			}
			for (int i = 0; i < n; i++) keep[i] = fwd[i] || bwd[i];
		}
		else
		{
			for (int i = 0; i < n; i++) keep[i] = true;
		}

		var childList = new List<int>[n];
		var weight = new int[n];
		var level = new int[n];
		var visited = new bool[n];

		int BuildTree(int v, int lvl)
		{
			visited[v] = true;
			level[v] = lvl;
			var kids = new List<int>();
			if (incoming.TryGetValue(v, out var preds))
				foreach (int p in preds)
					if (keep[p] && !visited[p]) kids.Add(p);
			kids.Sort();
			childList[v] = kids;
			if (kids.Count == 0) { weight[v] = 1; return 1; }
			int w = 0;
			foreach (int c in kids) w += BuildTree(c, lvl + 1);
			weight[v] = Math.Max(1, w);
			return weight[v];
		}

		bool IsSink(int i)
		{
			if (!outgoing.TryGetValue(i, out var succs)) return true;
			foreach (int s in succs) if (keep[s]) return false;
			return true;
		}

		var roots = new List<int>();
		for (int i = 0; i < n; i++)
			if (keep[i] && IsSink(i)) roots.Add(i);
		roots.Sort();
		foreach (int root in roots)
			if (!visited[root]) BuildTree(root, 0);
		for (int i = 0; i < n; i++)
			if (keep[i] && !visited[i]) { roots.Add(i); BuildTree(i, 0); }

		var y = new float[n];
		float cursor = 0f;
		float AssignY(int v)
		{
			var kids = childList[v];
			if (kids.Count == 0)
			{
				y[v] = cursor;
				cursor += RowH;
				return y[v];
			}
			float sum = 0f;
			foreach (int c in kids) sum += AssignY(c);
			y[v] = sum / kids.Count;
			return y[v];
		}
		foreach (int r in roots) AssignY(r);

		var nodes = new List<GraphNode>(n);
		for (int i = 0; i < n; i++)
		{
			if (!keep[i]) continue;
			AEKey key = nodeKey[i];
			Color bg = nodeKind[i] == 2 ? new Color(120, 40, 40)
				: nodeKind[i] == 1 ? new Color(30, 34, 64)
				: new Color(34, 90, 50);
			AEKey k = key;
			Color bgc = bg * 0.85f;
			long amt = nodeAmt[i];
			_keyById["k" + i] = key;

			var world = new Vector2(level[i] * ColW, y[i]);

			nodes.Add(new GraphNode
			{
				Id = "k" + i,
				World = world,
				Size = NodeSize,
				Draw = (sb, rect, hov) =>
				{
					CraftCell.Draw(sb, rect, k, amt, bgc, hov);
					if (hov) DrawBorder(sb, rect, Color.White, 2);
				},
			});
		}

		var edges = new List<GraphEdge>(edgePairs.Count);
		foreach (var (a, b) in edgePairs)
			if (keep[a] && keep[b])
				edges.Add(new GraphEdge { From = "k" + a, To = "k" + b, Color = new Color(90, 110, 150), Arrow = false });

		SetGraph(nodes, edges, fit: true);
	}
}
