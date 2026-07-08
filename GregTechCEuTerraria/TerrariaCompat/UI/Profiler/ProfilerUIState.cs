#nullable enable
using System.Collections.Generic;
using GregTechCEuTerraria.TerrariaCompat.Profiler;
using GregTechCEuTerraria.TerrariaCompat.UI.Widgets;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace GregTechCEuTerraria.TerrariaCompat.UI.Profiler;

public sealed class ProfilerUIState : FreeModalWindow
{
	private UITerrariaPanel _panel = null!;
	private UIPanel _graphPanel = null!;
	private CounterGraph _graph = null!;
	private UIText _graphLabel = null!;
	private UIList _list = null!;
	private UIScrollbar _bar = null!;
	private UIText _dumpBtn = null!;
	private bool _built;

	private ProfilerCounter? _selected;
	private readonly List<RowWidget> _rows = new();

	private const int Pad = 8;
	private const int TitleH = 28;
	private const int GraphH = 110;
	private const int ListTop = Pad + TitleH + GraphH + 8;
	private const int MoveKnobW = 20;
	private const int ResizeKnobSz = 28;

	protected override void RebuildWindow()
	{
		var root = RootSize();
		ResolveSize(root.X, root.Y);
		float w = CurW, h = CurH;

		if (!_built) { BuildStructure(); _built = true; }

		_panel.Width  = StyleDimension.FromPixels(w);
		_panel.Height = StyleDimension.FromPixels(h);

		int listH = (int)h - ListTop - Pad;
		_list.Height = StyleDimension.FromPixels(listH);
		_bar.Height  = StyleDimension.FromPixels(listH);

		LayoutHeaderButtons(_panel, w, Pad, TitleH);
		_dumpBtn.Left = StyleDimension.FromPixels(HeaderButtonsLeft(w, Pad) - 6 - w);
		LayoutResizeKnob(_panel, w, h, ResizeKnobSz, "Drag to resize the profiler");
		ApplyCenteredMoveClamp(_panel, root, w, h);

		Recalculate();
	}

	private void BuildStructure()
	{
		_panel = new UITerrariaPanel { HAlign = 0.5f, VAlign = 0.5f };
		Append(_panel);

		var moveKnob = NewMoveKnob("Drag to move the profiler");
		moveKnob.Left   = StyleDimension.FromPixels(Pad);
		moveKnob.Top    = StyleDimension.FromPixels(Pad);
		moveKnob.Width  = StyleDimension.FromPixels(MoveKnobW);
		moveKnob.Height = StyleDimension.FromPixels(TitleH);
		_panel.Append(moveKnob);

		var title = new UIText("GregTech Profiler", 1.05f)
		{
			Left = StyleDimension.FromPixels(Pad + MoveKnobW + 6),
			Top  = StyleDimension.FromPixels(Pad),
			IgnoresMouseInteraction = true,
		};
		_panel.Append(title);

		_dumpBtn = new UIText("[Dump JSON]", 0.9f)
		{
			HAlign = 1f,
			Top  = StyleDimension.FromPixels(Pad + 4),
		};
		_dumpBtn.OnLeftClick += (_, _) =>
		{
			string path = ProfilerSystem.DumpToFile();
			Main.NewText($"[GregTech] Profile saved to {path}", 180, 220, 255);
		};
		_panel.Append(_dumpBtn);

		_graphPanel = new UIPanel
		{
			Left = StyleDimension.FromPixels(Pad),
			Top  = StyleDimension.FromPixels(Pad + TitleH),
			Width  = StyleDimension.FromPixelsAndPercent(-Pad * 2, 1f),
			Height = StyleDimension.FromPixels(GraphH),
		};
		_panel.Append(_graphPanel);

		_graphLabel = new UIText("Click a row to graph it", 0.85f)
		{
			Left = StyleDimension.FromPixels(6),
			Top  = StyleDimension.FromPixels(2),
		};
		_graphPanel.Append(_graphLabel);

		_graph = new CounterGraph()
		{
			Left = StyleDimension.FromPixels(0),
			Top  = StyleDimension.FromPixels(18),
			Width  = StyleDimension.FromPixelsAndPercent(0, 1f),
			Height = StyleDimension.FromPixelsAndPercent(-18, 1f),
		};
		_graphPanel.Append(_graph);

		_list = new UIList
		{
			Left = StyleDimension.FromPixels(Pad),
			Top  = StyleDimension.FromPixels(ListTop),
			Width  = StyleDimension.FromPixelsAndPercent(-Pad * 2 - 20, 1f),
			ListPadding = 2f,
		};
		_panel.Append(_list);

		_bar = new UIScrollbar
		{
			HAlign = 1f,
			Left = StyleDimension.FromPixels(-Pad),
			Top  = StyleDimension.FromPixels(ListTop),
		};
		_panel.Append(_bar);
		_list.SetScrollbar(_bar);
	}

	protected override void ApplyOffsetLive()
	{
		if (_panel is null) return;
		_panel.Left = StyleDimension.FromPixels(OffsetX);
		_panel.Top  = StyleDimension.FromPixels(OffsetY);
		Recalculate();
	}

	private int _lastCounterCount = -1;

	protected override void OnModalUpdate(GameTime gameTime)
	{
		var all = global::GregTechCEuTerraria.TerrariaCompat.Profiler.Profiler.All;
		if (_lastCounterCount != all.Count)
		{
			_lastCounterCount = all.Count;
			Rebuild(all);
		}

		if (_selected is not null)
		{
			_graph.Bind(_selected);
			var (cur, _, _, _) = _selected.Summarize();
			_graphLabel.SetText(
				$"{_selected.Category}.{_selected.Name} - {ProfilerFormat.Format(_selected, cur)}");
		}
	}

	private void Rebuild(System.Collections.Generic.IReadOnlyList<ProfilerCounter> all)
	{
		_rows.Clear();
		_list.Clear();

		var groups = new Dictionary<string, List<ProfilerCounter>>();
		foreach (var c in all)
		{
			if (!groups.TryGetValue(c.Category, out var list))
				groups[c.Category] = list = new List<ProfilerCounter>();
			list.Add(c);
		}

		var orderedCategories = new List<string>(groups.Keys);
		orderedCategories.Sort((a, b) =>
		{
			int da = ProfilerFormat.CategoryOrder(a);
			int db = ProfilerFormat.CategoryOrder(b);
			if (da != db) return da.CompareTo(db);
			return string.CompareOrdinal(a, b);
		});

		foreach (var cat in orderedCategories)
		{
			var members = groups[cat];
			members.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));
			_list.Add(new CategoryHeader(cat, members, this));
			foreach (var c in members)
			{
				var row = new RowWidget(c, this);
				_rows.Add(row);
				_list.Add(row);
			}
		}
	}

	internal void Select(ProfilerCounter c) => _selected = c;

	internal bool IsRowVisible(UIElement el)
	{
		var lr = _list.GetDimensions().ToRectangle();
		var r  = el.GetDimensions().ToRectangle();
		return r.Bottom >= lr.Top && r.Top <= lr.Bottom;
	}

	private sealed class CategoryHeader : UIElement
	{
		private readonly string _category;
		private readonly List<ProfilerCounter> _members;
		private readonly ProfilerUIState _owner;
		public CategoryHeader(string category, List<ProfilerCounter> members, ProfilerUIState owner)
		{
			_category = category;
			_members  = members;
			_owner    = owner;
			Width  = StyleDimension.FromPercent(1f);
			Height = StyleDimension.FromPixels(22);
			IgnoresMouseInteraction = true;
		}
		protected override void DrawSelf(SpriteBatch sb)
		{
			if (!_owner.IsRowVisible(this)) return;
			var rect = GetDimensions().ToRectangle();
			var px   = TextureAssets.MagicPixel.Value;
			sb.Draw(px, rect, new Color(40, 48, 70, 200));
			sb.Draw(px, new Rectangle(rect.X, rect.Bottom - 1, rect.Width, 1),
				new Color(110, 130, 180));

			string title = ProfilerFormat.CategoryTitle(_category);
			Terraria.Utils.DrawBorderString(sb, title,
				new Vector2(rect.X + 6, rect.Y + 3),
				new Color(220, 230, 255), 0.82f);

			if (TryAggregate(out double sum))
			{
				string formatted = ProfilerFormat.Format(_members[0], sum);
				Terraria.Utils.DrawBorderString(sb, $"Σ {formatted}",
					new Vector2(rect.Right - 6 - Terraria.GameContent.FontAssets.MouseText.Value.MeasureString($"Σ {formatted}").X * 0.82f, rect.Y + 3),
					new Color(180, 200, 240), 0.82f);
			}
		}

		private bool TryAggregate(out double sum)
		{
			sum = 0;
			bool ok = _category.EndsWith(".bytes", System.StringComparison.Ordinal)
				   || _category == "net.in.count"  || _category == "server.net.in.count"
				   || _category == "net.out.count" || _category == "server.net.out.count"
				   || _category == "net.skipped"   || _category == "server.net.skipped"
				   || _category == "tick"          || _category == "server.tick";
			if (!ok) return false;
			foreach (var c in _members)
			{
				var (cur, _, _, _) = c.Summarize();
				sum += cur;
			}
			return true;
		}
	}

	private sealed class RowWidget : UIElement
	{
		private readonly ProfilerCounter _c;
		private readonly ProfilerUIState _owner;
		public RowWidget(ProfilerCounter c, ProfilerUIState owner)
		{
			_c = c; _owner = owner;
			Width  = StyleDimension.FromPercent(1f);
			Height = StyleDimension.FromPixels(18);
		}
		public override void LeftClick(UIMouseEvent evt)
		{
			base.LeftClick(evt);
			_owner.Select(_c);
		}
		protected override void DrawSelf(SpriteBatch sb)
		{
			if (!_owner.IsRowVisible(this)) return;
			var rect = GetDimensions().ToRectangle();
			var px   = TextureAssets.MagicPixel.Value;
			bool hover    = ContainsPoint(ModalEscape.PollCursorScreen());
			bool selected = ReferenceEquals(_owner._selected, _c);

			if (selected)      sb.Draw(px, rect, new Color(80, 110, 180, 100));
			else if (hover)    sb.Draw(px, rect, new Color(255, 255, 255, 22));

			var (cur, min, max, avg) = _c.Summarize();
			var severity = ProfilerFormat.Severity(_c, cur);

			sb.Draw(px, new Rectangle(rect.X, rect.Y, 3, rect.Height),
				ProfilerFormat.StripeColor(severity));

			Terraria.Utils.DrawBorderString(sb, _c.Name,
				new Vector2(rect.X + 14, rect.Y + 1),
				new Color(240, 240, 240), 0.78f);

			string curStr = ProfilerFormat.Format(_c, cur);
			Terraria.Utils.DrawBorderString(sb, curStr,
				new Vector2(rect.X + rect.Width * 0.45f, rect.Y + 1),
				ProfilerFormat.ValueColor(_c, severity), 0.80f);

			string trail = _c.Kind == ProfilerKind.Gauge
				? $"min {ProfilerFormat.Format(_c, min)}  max {ProfilerFormat.Format(_c, max)}"
				: $"avg {ProfilerFormat.Format(_c, avg)}  max {ProfilerFormat.Format(_c, max)}";
			Terraria.Utils.DrawBorderString(sb, trail,
				new Vector2(rect.X + rect.Width * 0.72f, rect.Y + 1),
				new Color(160, 160, 170), 0.74f);
		}
	}

	private sealed class CounterGraph : UIElement
	{
		private ProfilerCounter? _c;
		public void Bind(ProfilerCounter c) => _c = c;

		protected override void DrawSelf(SpriteBatch sb)
		{
			var d  = GetDimensions().ToRectangle();
			var px = TextureAssets.MagicPixel.Value;
			sb.Draw(px, d, new Color(0, 0, 0, 100));
			sb.Draw(px, new Rectangle(d.X, d.Y, d.Width, 1), new Color(80, 80, 100));
			sb.Draw(px, new Rectangle(d.X, d.Bottom - 1, d.Width, 1), new Color(80, 80, 100));
			sb.Draw(px, new Rectangle(d.X, d.Y, 1, d.Height), new Color(80, 80, 100));
			sb.Draw(px, new Rectangle(d.Right - 1, d.Y, 1, d.Height), new Color(80, 80, 100));
			if (_c is null) return;

			int n = _c.Samples.Length;
			double maxSample = 0;
			for (int i = 0; i < n; i++) if (_c.Samples[i] > maxSample) maxSample = _c.Samples[i];
			if (maxSample <= 0) maxSample = 1;

			int innerX = d.X + 2, innerY = d.Y + 2;
			int innerW = d.Width - 4, innerH = d.Height - 4;
			if (innerW <= 0 || innerH <= 0) return;

			if (n <= innerW)
			{
				float barW = innerW / (float)n;
				for (int i = 0; i < n; i++)
				{
					int idx = (_c.SampleHead + i) % n;
					double s = _c.Samples[idx];
					int barH = (int)(s / maxSample * innerH);
					if (barH < 1 && s > 0) barH = 1;
					var rect = new Rectangle(
						innerX + (int)(i * barW),
						innerY + innerH - barH,
						System.Math.Max(1, (int)barW),
						barH);
					var col = ProfilerFormat.StripeColor(ProfilerFormat.Severity(_c, s));
					sb.Draw(px, rect, col * 0.85f);
				}
			}
			else
			{
				double samplesPerBucket = (double)n / innerW;
				int sampleIdx = 0;
				for (int x = 0; x < innerW; x++)
				{
					int bucketEnd = (int)((x + 1) * samplesPerBucket);
					if (bucketEnd > n) bucketEnd = n;
					double bucketMax = 0;
					for (; sampleIdx < bucketEnd; sampleIdx++)
					{
						int idx = (_c.SampleHead + sampleIdx) % n;
						double s = _c.Samples[idx];
						if (s > bucketMax) bucketMax = s;
					}
					int barH = (int)(bucketMax / maxSample * innerH);
					if (barH < 1 && bucketMax > 0) barH = 1;
					if (barH <= 0) continue;
					var col = ProfilerFormat.StripeColor(ProfilerFormat.Severity(_c, bucketMax));
					sb.Draw(px,
						new Rectangle(innerX + x, innerY + innerH - barH, 1, barH),
						col * 0.85f);
				}
			}

			if (_c.Kind == ProfilerKind.Timer)
			{
				int budgetY = innerY + innerH - (int)(ProfilerFormat.FrameMs / maxSample * innerH);
				if (budgetY > innerY && budgetY < innerY + innerH)
					sb.Draw(px, new Rectangle(innerX, budgetY, innerW, 1),
						new Color(255, 90, 80, 180));
			}

			double windowSec = n * global::GregTechCEuTerraria.TerrariaCompat.Profiler.Profiler.SamplePeriodFrames / 60.0;
			string windowLabel = windowSec >= 60
				? $"{windowSec / 60.0:0.#} min"
				: $"{windowSec:0} s";
			Terraria.Utils.DrawBorderString(sb, $"window {windowLabel}",
				new Vector2(d.Right - 90, d.Bottom - 14),
				new Color(160, 160, 170), 0.65f);

			Terraria.Utils.DrawBorderString(sb, $"max ~ {ProfilerFormat.Format(_c, maxSample)}",
				new Vector2(d.X + 6, d.Y + 4), new Color(200, 200, 200), 0.70f);
		}
	}
}
