#nullable enable
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.UI;

namespace GregTechCEuTerraria.TerrariaCompat.UI;

public sealed class GraphNode
{
	public string Id = "";
	public Vector2 World;
	public float Size = 38f;
	public Action<SpriteBatch, Rectangle, bool>? Draw;
}

public sealed class GraphEdge
{
	public string From = "";
	public string To = "";
	public Color Color = new(90, 95, 120);
	public Func<Color>? ColorFn;
	public bool Arrow = true;

	public Color Resolve() => ColorFn?.Invoke() ?? Color;
}

public abstract class GraphRenderer : UIElement
{
	protected readonly List<GraphNode> Nodes = new();
	protected readonly List<GraphEdge> Edges = new();
	private readonly Dictionary<string, GraphNode> _index = new();

	protected Color Background = new(20, 22, 34);
	protected float MinZoom = 0.2f, MaxZoom = 2.5f;
	protected float FitMinZoom = 0.25f, FitMaxZoom = 1.5f;

	private Vector2 _pan;
	private float _zoom = 1f;
	private bool _needsFit;

	private bool _dragging;
	private bool _dragMoved;
	private Vector2 _dragLast;
	private GraphNode? _dragNode;
	private bool _pressOnNode;

	public string? Hovered { get; private set; }

	protected void SetGraph(IEnumerable<GraphNode> nodes, IEnumerable<GraphEdge> edges, bool fit = true)
	{
		Nodes.Clear();
		Edges.Clear();
		Nodes.AddRange(nodes);
		Edges.AddRange(edges);
		ReindexNodes();
		if (fit) _needsFit = true;
	}

	protected void ReindexNodes()
	{
		_index.Clear();
		foreach (GraphNode n in Nodes)
			_index[n.Id] = n;
	}

	protected GraphNode? NodeById(string id) => _index.TryGetValue(id, out GraphNode? n) ? n : null;

	public void RequestFit() => _needsFit = true;

	protected Vector2 Pan => _pan;
	public float Zoom => _zoom;

	protected virtual bool AllowNodeDrag => false;
	protected virtual bool NodesInteractive => false;
	protected virtual bool ShouldHandleInput() => true;
	protected virtual void OnNodeClicked(string id) { }
	protected virtual void OnBackgroundClicked() { }
	protected virtual void OnNodeDragged(string id, Vector2 world) { }
	protected virtual void OnNodeDragEnded(string id) { }
	protected virtual void DrawOverlay(SpriteBatch sb) { }
	protected virtual void DrawHoverTooltip(string id) { }

	public Vector2 ToScreen(Vector2 world)
	{
		CalculatedStyle d = GetDimensions();
		var centre = new Vector2(d.X + d.Width * 0.5f, d.Y + d.Height * 0.5f);
		return centre + (world - _pan) * _zoom;
	}

	public Vector2 ScreenToWorld(Vector2 screen)
	{
		CalculatedStyle d = GetDimensions();
		var centre = new Vector2(d.X + d.Width * 0.5f, d.Y + d.Height * 0.5f);
		return _pan + (screen - centre) / _zoom;
	}

	protected float NodeScreenSize(GraphNode n) => n.Size * _zoom;

	private void AutoFit()
	{
		CalculatedStyle d = GetDimensions();
		if (Nodes.Count == 0 || d.Width <= 0)
		{
			_pan = Vector2.Zero;
			_zoom = 1f;
			return;
		}

		Vector2 min = Nodes[0].World;
		Vector2 max = min;
		float maxNode = 0f;
		foreach (GraphNode n in Nodes)
		{
			min = Vector2.Min(min, n.World);
			max = Vector2.Max(max, n.World);
			maxNode = MathF.Max(maxNode, n.Size);
		}

		_pan = (min + max) * 0.5f;
		float spanX = max.X - min.X + maxNode * 2f;
		float spanY = max.Y - min.Y + maxNode * 2f;
		float fit = Math.Min((d.Width - 24f) / spanX, (d.Height - 24f) / spanY);
		_zoom = MathHelper.Clamp(fit, FitMinZoom, FitMaxZoom);
	}

	private GraphNode? HitTestNode(Vector2 mouse)
	{
		for (int i = Nodes.Count - 1; i >= 0; i--)
		{
			GraphNode n = Nodes[i];
			float half = NodeScreenSize(n) * 0.5f;
			Vector2 c = ToScreen(n.World);
			if (mouse.X >= c.X - half && mouse.X <= c.X + half
				&& mouse.Y >= c.Y - half && mouse.Y <= c.Y + half)
				return n;
		}
		return null;
	}

	public override void ScrollWheel(UIScrollWheelEvent evt)
	{
		base.ScrollWheel(evt);
		Vector2 mouse = ModalEscape.PollCursorScreen();
		if (!ContainsPoint(mouse))
			return;
		float factor = evt.ScrollWheelValue > 0 ? 1.15f : 1f / 1.15f;
		float newZoom = MathHelper.Clamp(_zoom * factor, MinZoom, MaxZoom);
		if (newZoom == _zoom)
			return;

		Vector2 worldUnderCursor = ScreenToWorld(mouse);
		_zoom = newZoom;
		CalculatedStyle d = GetDimensions();
		var centre = new Vector2(d.X + d.Width * 0.5f, d.Y + d.Height * 0.5f);
		_pan = worldUnderCursor - (mouse - centre) / _zoom;
	}

	public override void Update(GameTime gameTime)
	{
		base.Update(gameTime);

		if (_needsFit && GetDimensions().Width > 0)
		{
			AutoFit();
			_needsFit = false;
		}

		if (!ContainsPoint(ModalEscape.PollCursorScreen()) || !ShouldHandleInput())
		{
			_dragging = false;
			_dragNode = null;
			Hovered = null;
			return;
		}

		Main.LocalPlayer.mouseInterface = true;
		Vector2 mouse = ModalEscape.PollCursorScreen();
		Hovered = HitTestNode(mouse)?.Id;

		if (Main.mouseLeft)
		{
			if (!_dragging)
			{
				_dragging = true;
				_dragMoved = false;
				_dragLast = mouse;
				_dragNode = AllowNodeDrag ? HitTestNode(mouse) : null;
				_pressOnNode = _dragNode == null && NodesInteractive && HitTestNode(mouse) != null;
			}
			else
			{
				Vector2 delta = mouse - _dragLast;
				if (delta.LengthSquared() > 16f)
					_dragMoved = true;

				if (_dragNode != null)
				{
					if (_dragMoved)
						OnNodeDragged(_dragNode.Id, ScreenToWorld(mouse));
				}
				else if (_zoom > 0f && !_pressOnNode)
				{
					_pan -= delta / _zoom;
				}
				_dragLast = mouse;
			}
		}
		else
		{
			if (_dragging && !_dragMoved)
			{
				GraphNode? hit = HitTestNode(mouse);
				if (hit != null)
					OnNodeClicked(hit.Id);
				else
					OnBackgroundClicked();
			}
			else if (_dragNode != null && _dragMoved)
			{
				OnNodeDragEnded(_dragNode.Id);
			}
			_dragging = false;
			_dragNode = null;
			_pressOnNode = false;
		}
	}

	protected override void DrawSelf(SpriteBatch sb)
	{
		CalculatedStyle d = GetDimensions();
		sb.Draw(TextureAssets.MagicPixel.Value, d.ToRectangle(), Background);

		if (Nodes.Count == 0)
			return;

		ScissorDraw.Draw(sb, GetClippingRectangle(sb), () => DrawGraph(sb));
	}

	private void DrawGraph(SpriteBatch sb)
	{
		Rectangle bounds = GetDimensions().ToRectangle();

		foreach (GraphEdge e in Edges)
		{
			if (!_index.TryGetValue(e.From, out GraphNode? a) || !_index.TryGetValue(e.To, out GraphNode? b))
				continue;
			Vector2 from = ToScreen(a.World);
			Vector2 to = ToScreen(b.World);
			Color color = e.Resolve();
			DrawLine(sb, from, to, color, 2f);
			if (e.Arrow)
				DrawArrowhead(sb, from, to, color, NodeScreenSize(b));
		}

		string? hov = ContainsPoint(ModalEscape.PollCursorScreen()) && ShouldHandleInput() ? Hovered : null;
		foreach (GraphNode n in Nodes)
		{
			Vector2 c = ToScreen(n.World);
			int size = (int)NodeScreenSize(n);
			var rect = new Rectangle((int)(c.X - size * 0.5f), (int)(c.Y - size * 0.5f), size, size);
			if (!bounds.Intersects(rect))
				continue;
			n.Draw?.Invoke(sb, rect, n.Id == hov);
		}

		DrawOverlay(sb);

		if (hov != null)
			DrawHoverTooltip(hov);
	}

	public static void DrawLine(SpriteBatch sb, Vector2 a, Vector2 b, Color color, float thickness)
	{
		Vector2 diff = b - a;
		float length = diff.Length();
		if (length < 0.5f)
			return;
		sb.Draw(TextureAssets.MagicPixel.Value, a, new Rectangle(0, 0, 1, 1), color, diff.ToRotation(),
			new Vector2(0f, 0.5f), new Vector2(length, thickness), SpriteEffects.None, 0f);
	}

	public static void DrawArrowhead(SpriteBatch sb, Vector2 from, Vector2 to, Color color, float toNodeSize)
	{
		Vector2 dir = to - from;
		if (dir.LengthSquared() < 1f)
			return;
		dir.Normalize();

		Vector2 tip = to - dir * (toNodeSize * 0.5f + 3f);
		const float barb = 9f;
		DrawLine(sb, tip, tip + (-dir).RotatedBy(0.5) * barb, color, 2f);
		DrawLine(sb, tip, tip + (-dir).RotatedBy(-0.5) * barb, color, 2f);
	}

	public static void DrawBorder(SpriteBatch sb, Rectangle r, Color color, int thickness)
	{
		Texture2D px = TextureAssets.MagicPixel.Value;
		sb.Draw(px, new Rectangle(r.X, r.Y, r.Width, thickness), color);
		sb.Draw(px, new Rectangle(r.X, r.Bottom - thickness, r.Width, thickness), color);
		sb.Draw(px, new Rectangle(r.X, r.Y, thickness, r.Height), color);
		sb.Draw(px, new Rectangle(r.Right - thickness, r.Y, thickness, r.Height), color);
	}
}
