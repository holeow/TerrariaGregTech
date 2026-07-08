#nullable enable
using System;
using System.Collections.Generic;
using GregTechCEuTerraria.TerrariaCompat.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;

namespace GregTechCEuTerraria.TerrariaCompat.Questbook;

public sealed class QuestGraph : GraphRenderer
{
	private const float Grid = 62f;
	private const float NodeWorld = 38f;

	private readonly QuestbookUIState _owner;

	private List<NodeData> _src = [];
	private readonly Dictionary<string, NodeData> _byQuestId = [];

	public QuestGraph(QuestbookUIState owner) => _owner = owner;

	internal void LoadChapter(ChapterData chapter)
	{
		_src = chapter.Nodes;
		Rebuild();
		RequestFit();
	}

	internal void RefreshIndex() => Rebuild();

	private void Rebuild()
	{
		_byQuestId.Clear();
		foreach (NodeData n in _src)
			_byQuestId[n.Quest] = n;

		var nodes = new List<GraphNode>();
		foreach (NodeData n in _src)
		{
			if (!QuestbookSystem.QuestsById.ContainsKey(n.Quest))
				continue;
			string id = n.Quest;
			var gn = new GraphNode
			{
				Id = id,
				World = new Vector2(n.X, n.Y) * Grid,
				Size = NodeWorld * SizeOf(id),
			};
			gn.Draw = (sb, rect, hov) => DrawNode(sb, id, rect, hov);
			nodes.Add(gn);
		}

		var edges = new List<GraphEdge>();
		foreach (NodeData n in _src)
		{
			if (!QuestbookSystem.QuestsById.TryGetValue(n.Quest, out QuestData? q))
				continue;
			foreach (string dep in q.Deps)
				if (_byQuestId.ContainsKey(dep))
				{
					string d = dep;
					edges.Add(new GraphEdge
					{
						From = dep,
						To = n.Quest,
						ColorFn = () => QuestbookProgress.IsComplete(d)
							? new Color(120, 200, 120) : new Color(90, 95, 120),
					});
				}
		}

		SetGraph(nodes, edges, fit: false);
	}

	internal (float X, float Y) ViewCenterGrid() => (Snap(Pan.X / Grid), Snap(Pan.Y / Grid));

	private static float Snap(float v) => MathF.Round(v * 2f) / 2f;

	private static float SizeOf(string questId)
		=> QuestbookSystem.QuestsById.TryGetValue(questId, out QuestData? q) ? Math.Max(0.25f, q.Size) : 1f;

	protected override bool AllowNodeDrag => QuestbookEditor.Enabled && !QuestbookEditor.AwaitingDep;

	protected override bool ShouldHandleInput() => !_owner.IsPointerOverQuestPanel();

	public override void Update(GameTime gameTime)
	{
		foreach (KeyValuePair<string, NodeData> kv in _byQuestId)
			if (NodeById(kv.Key) is { } gn)
				gn.World = new Vector2(kv.Value.X, kv.Value.Y) * Grid;

		base.Update(gameTime);
	}

	protected override void OnNodeClicked(string id)
	{
		if (!QuestbookSystem.QuestsById.TryGetValue(id, out QuestData? q))
			return;

		if (QuestbookEditor.Enabled && QuestbookEditor.AwaitingDep)
		{
			if (QuestbookEditor.DepPickArmed)
			{
				string? target = QuestbookEditor.OnDepPickClick(q.Id);
				if (target != null && QuestbookSystem.QuestsById.TryGetValue(target, out QuestData? tq))
					_owner.RefreshDetail(tq);
			}
			return;
		}

		_owner.SelectQuest(q);
	}

	protected override void OnBackgroundClicked()
	{
		if (QuestbookEditor.Enabled && QuestbookEditor.AwaitingDep && QuestbookEditor.DepPickArmed)
			QuestbookEditor.CancelAddDep();
	}

	protected override void OnNodeDragged(string id, Vector2 world)
	{
		if (!_byQuestId.TryGetValue(id, out NodeData? nd))
			return;
		nd.X = Snap(world.X / Grid);
		nd.Y = Snap(world.Y / Grid);
		if (NodeById(id) is { } gn)
			gn.World = new Vector2(nd.X, nd.Y) * Grid;
	}

	protected override void OnNodeDragEnded(string id) => QuestbookEditor.OnNodeMoved();

	protected override void DrawOverlay(SpriteBatch sb)
	{
		if (QuestbookEditor.AwaitingDep && QuestbookEditor.DepTarget is { } target
			&& NodeById(target) is { } tNode)
		{
			Vector2 at = ToScreen(tNode.World);
			DrawLine(sb, at, ModalEscape.PollCursorScreen(), new Color(255, 180, 60), 2f);
			int sz = (int)NodeScreenSize(tNode);
			DrawBorder(sb, new Rectangle((int)(at.X - sz * 0.5f), (int)(at.Y - sz * 0.5f), sz, sz),
				new Color(255, 180, 60), 3);
		}
	}

	protected override void DrawHoverTooltip(string id)
	{
		if (QuestbookSystem.QuestsById.TryGetValue(id, out QuestData? q))
			Main.instance.MouseText(q.Title);
	}

	private void DrawNode(SpriteBatch sb, string questId, Rectangle rect, bool hovered)
	{
		if (!QuestbookSystem.QuestsById.TryGetValue(questId, out QuestData? q))
			return;

		Texture2D px = TextureAssets.MagicPixel.Value;
		bool complete = QuestbookProgress.IsComplete(q.Id);
		bool selected = _owner.SelectedQuestId == q.Id;

		sb.Draw(px, rect, new Color(46, 50, 76));

		if (QuestbookSystem.Resolved.TryGetValue(q.Id, out ResolvedQuest? resolved) && resolved.IconType > 0)
			QuestbookIcon.Draw(sb, resolved.IconType, rect.Center.ToVector2(), rect.Width - 8f);

		Color border = selected ? Color.Yellow
			: complete ? new Color(120, 230, 120)
			: hovered ? Color.White
			: new Color(95, 100, 130);
		DrawBorder(sb, rect, border, complete || selected ? 3 : 2);
	}
}
