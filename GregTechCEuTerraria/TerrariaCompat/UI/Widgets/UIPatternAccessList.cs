#nullable enable
using System.Collections.Generic;
using System.Text;
using GregTechCEuTerraria.AppliedEnergistics.Api.Config;
using GregTechCEuTerraria.TerrariaCompat.AppliedEnergistics;
using GregTechCEuTerraria.TerrariaCompat.Pipelike.Me;
using GregTechCEuTerraria.TerrariaCompat.UI.PatternAccess;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.GameInput;
using Terraria.UI;

namespace GregTechCEuTerraria.TerrariaCompat.UI.Widgets;

public sealed class UIPatternAccessList : UIElement
{
	private const int Margin = 6, SearchH = 22;
	private const int SlotSize = 44, SlotGap = 2;
	private const int SlotCols = 9;
	private const int ScrollbarWidth = Scrollbar.Width;
	private static int SlotRows => (PatternProviderMachine.PatternSlots + SlotCols - 1) / SlotCols;
	private const int CardHeaderH = 18, CardPad = 4;
	private static int CardH => CardHeaderH + SlotRows * (SlotSize + SlotGap) + CardPad;
	private const int CardGap = 4;
	private const int GroupHeaderH = 18;

	private readonly IMePatternAccessHost _term;
	private readonly UISearchBar _search;
	private readonly UITextButton _showModeButton;
	private readonly UIElement _scrollArea;
	private string _query = "";
	private ShowPatternProviders _showMode = ShowPatternProviders.Visible;
	private readonly HashSet<(short x, short y)> _pinned = new();
	private int _scroll;
	private int _contentH;
	private string _signature = "";
	private readonly List<(UIElement card, int baseY)> _cards = new();

	private readonly Scrollbar _bar = new();
	private Rectangle _trackRect, _thumbRect;
	private bool _showBar;

	public UIPatternAccessList(IMePatternAccessHost term)
	{
		_term = term;
		Width = StyleDimension.FromPercent(1f);
		Height = StyleDimension.FromPercent(1f);

		_search = new UISearchBar("Search name / output...   RMB clear", t =>
		{
			_query = (t ?? "").Trim().ToLowerInvariant();
			_scroll = 0;
			_signature = ""; // force rebuild
		})
		{
			Left = StyleDimension.FromPixels(Margin),
			Top = StyleDimension.FromPixels(Margin),
			Width = new StyleDimension(-(Margin * 2 + 86), 1f),
			Height = StyleDimension.FromPixels(SearchH),
		};
		Append(_search);

		_showModeButton = new UITextButton(() => "Show: " + ShowModeLabel(_showMode),
			onLeft: () => { _showMode = NextShowMode(_showMode); _signature = ""; },
			onRight: null,
			tooltip: "Which providers to list:\nVisible (not hidden) / Not Full / All",
			width: 80, height: SearchH)
		{
			Left = new StyleDimension(-(Margin + 80), 1f),
			Top = StyleDimension.FromPixels(Margin),
		};
		Append(_showModeButton);

		_scrollArea = new UIElement
		{
			Left = StyleDimension.FromPixels(Margin),
			Top = StyleDimension.FromPixels(Margin + SearchH + 2),
			// Reserve a gutter on the right for the scrollbar.
			Width = new StyleDimension(-(Margin * 2 + ScrollbarWidth), 1f),
			Height = new StyleDimension(-(Margin * 2 + SearchH + 2), 1f),
			OverflowHidden = true,
		};
		Append(_scrollArea);
	}

	public override void ScrollWheel(UIScrollWheelEvent evt)
	{
		base.ScrollWheel(evt);
		if (!_scrollArea.IsMouseHovering) return;
		Scrollbar.Wheel(evt, ref _scroll);
	}

	public override void Update(GameTime gameTime)
	{
		base.Update(gameTime);

		string sig = ComputeSignature();
		if (sig != _signature)
		{
			_signature = sig;
			Rebuild();
		}

		int viewH = (int)_scrollArea.GetInnerDimensions().Height;
		int totalH = _contentH;
		int maxScroll = System.Math.Max(0, totalH - viewH);

		UpdateScrollbar(viewH, totalH, maxScroll);

		if (_scroll > maxScroll) _scroll = maxScroll;
		if (_scroll < 0) _scroll = 0;

		foreach (var (card, baseY) in _cards)
			card.Top = StyleDimension.FromPixels(baseY - _scroll);
		_scrollArea.Recalculate();
	}

	private void UpdateScrollbar(int viewH, int totalH, int maxScroll)
	{
		_showBar = totalH > viewH;
		if (!_showBar) { _thumbRect = Rectangle.Empty; return; }

		var sa = _scrollArea.GetDimensions().ToRectangle();
		_trackRect = new Rectangle(sa.Right + 2, sa.Y, ScrollbarWidth, sa.Height);
		_thumbRect = _bar.Update(_trackRect, maxScroll, (float)viewH / totalH, ref _scroll, ModalEscape.PollCursor());
	}

	private string ComputeSignature()
	{
		var sb = new StringBuilder();
		sb.Append((int)_showMode).Append('|').Append(_query).Append('|');
		foreach (var p in _term.Providers)
			sb.Append(p.ProviderPos.X).Append(',').Append(p.ProviderPos.Y).Append(':')
			  .Append(p.ProviderName).Append(':').Append(p.IsVisibleInTerminal ? '1' : '0')
			  .Append(':').Append(p.Patterns.Count).Append(';');
		return sb.ToString();
	}

	private void Rebuild()
	{
		_scrollArea.RemoveAllChildren();
		_cards.Clear();

		var groups = new List<(string name, List<IMePatternProvider> providers)>();
		var index = new Dictionary<string, int>();
		foreach (var p in _term.Providers)
		{
			if (!PassesShowMode(p) || !MatchesQuery(p)) continue;
			if (!index.TryGetValue(p.ProviderName, out int gi))
			{
				gi = groups.Count;
				index[p.ProviderName] = gi;
				groups.Add((p.ProviderName, new List<IMePatternProvider>()));
			}
			groups[gi].providers.Add(p);
		}

		int y = 0;
		foreach (var (name, providers) in groups)
		{
			var header = BuildHeader(name, providers.Count > 0 ? providers[0].TerminalIconItemType : 0);
			header.Top = StyleDimension.FromPixels(y);
			_scrollArea.Append(header);
			_cards.Add((header, y));
			y += GroupHeaderH + CardGap;

			foreach (var p in providers)
			{
				var card = BuildCard(p);
				card.Top = StyleDimension.FromPixels(y);
				_scrollArea.Append(card);
				_cards.Add((card, y));
				y += CardH + CardGap;
			}
		}
		_contentH = y;
		_scrollArea.Recalculate();
	}

	private bool PassesShowMode(IMePatternProvider p)
	{
		switch (_showMode)
		{
			case ShowPatternProviders.Visible:
				return p.IsVisibleInTerminal;
			case ShowPatternProviders.NotFull:
				var pos = (p.ProviderPos.X, p.ProviderPos.Y);
				bool shown = p.IsVisibleInTerminal && (_pinned.Contains(pos) || !IsFull(p));
				if (shown) _pinned.Add(pos);
				return shown;
			default:
				return true;
		}
	}

	private static bool IsFull(IMePatternProvider p) => p.Patterns.Count >= PatternProviderMachine.PatternSlots;

	private static string ShowModeLabel(ShowPatternProviders m) => m switch
	{
		ShowPatternProviders.NotFull => "Not Full",
		ShowPatternProviders.All => "All",
		_ => "Visible",
	};

	private static ShowPatternProviders NextShowMode(ShowPatternProviders m) => m switch
	{
		ShowPatternProviders.Visible => ShowPatternProviders.NotFull,
		ShowPatternProviders.NotFull => ShowPatternProviders.All,
		_ => ShowPatternProviders.Visible,
	};

	private UIElement BuildHeader(string name, int iconItemType)
	{
		var header = new UIElement
		{
			Left = StyleDimension.FromPixels(0),
			Width = StyleDimension.FromPercent(1f),
			Height = StyleDimension.FromPixels(GroupHeaderH),
		};
		int textX = 2;
		if (iconItemType > 0)
		{
			header.Append(new GroupIcon(iconItemType)
			{
				Left = StyleDimension.FromPixels(1),
				Top = StyleDimension.FromPixels(1),
				Width = StyleDimension.FromPixels(GroupHeaderH - 2),
				Height = StyleDimension.FromPixels(GroupHeaderH - 2),
			});
			textX = GroupHeaderH + 2;
		}
		header.Append(new UIText(name, 0.9f)
		{
			Left = StyleDimension.FromPixels(textX),
			Top = StyleDimension.FromPixels(3),
		});
		return header;
	}

	private sealed class GroupIcon : UIElement
	{
		private readonly int _itemType;
		private static readonly Item[] _tmp = { new() };
		public GroupIcon(int itemType) => _itemType = itemType;
		protected override void DrawSelf(SpriteBatch sb)
		{
			if (_itemType <= 0 || _itemType >= TextureAssets.Item.Length) return;
			var b = GetDimensions().ToRectangle();
			_tmp[0].SetDefaults(_itemType);
			float old = Main.inventoryScale;
			Main.inventoryScale = b.Width / 52f;
			try { ItemSlot.Draw(sb, _tmp, ItemSlot.Context.CraftingMaterial, 0, new Vector2(b.X, b.Y)); }
			finally { Main.inventoryScale = old; }
		}
	}

	private bool MatchesQuery(IMePatternProvider p)
	{
		if (_query.Length == 0) return true;
		if (p.ProviderName.ToLowerInvariant().Contains(_query)) return true;
		foreach (var pat in p.Patterns)
			foreach (var (what, _) in pat.Outputs)
				if (what != null && what.GetDisplayName().ToLowerInvariant().Contains(_query))
					return true;
		return false;
	}

	private UIElement BuildCard(IMePatternProvider provider)
	{
		var pos = provider.ProviderPos;
		var card = new UIElement
		{
			Left = StyleDimension.FromPixels(0),
			Width = StyleDimension.FromPercent(1f),
			Height = StyleDimension.FromPixels(CardH),
		};
		card.Append(new BgPanel());

		card.Append(new UITextButton(() => "Show in World",
			onLeft: () => { PatternLocatorSystem.Locate(pos); MachineUISystem.Close(); },
			onRight: null,
			width: 88, height: 16)
		{
			Left = new StyleDimension(-92, 1f),
			Top = StyleDimension.FromPixels(2),
		});

		for (int i = 0; i < PatternProviderMachine.PatternSlots; i++)
			card.Append(new UIPatternAccessSlot(_term.Machine, pos, i, SlotSize, () => _query)
			{
				Left = StyleDimension.FromPixels(CardPad + (i % SlotCols) * (SlotSize + SlotGap)),
				Top = StyleDimension.FromPixels(CardHeaderH + (i / SlotCols) * (SlotSize + SlotGap)),
			});

		return card;
	}

	protected override void DrawSelf(SpriteBatch sb)
	{
		var outer = GetDimensions().ToRectangle();
		sb.Draw(TextureAssets.MagicPixel.Value, outer, new Color(20, 22, 50) * 0.45f);

		if (IsMouseHovering)
		{
			Main.LocalPlayer.mouseInterface = true;
			PlayerInput.LockVanillaMouseScroll("GregTechCEuTerraria/MePatternAccess");
		}

		if (_term.Providers.Count == 0)
			Terraria.Utils.DrawBorderString(sb,
				_term.Network is null ? "[Not connected]" : "No pattern providers",
				new Vector2(outer.X + Margin + 4, outer.Y + Margin + SearchH + 8), Color.LightGray, 0.8f);

		if (_showBar)
			_bar.Draw(sb, _trackRect, _thumbRect, ModalEscape.PollCursor());
	}

	private sealed class BgPanel : UIElement
	{
		public BgPanel()
		{
			Width = StyleDimension.FromPercent(1f);
			Height = StyleDimension.FromPercent(1f);
		}
		protected override void DrawSelf(SpriteBatch sb)
		{
			var b = GetDimensions().ToRectangle();
			sb.Draw(TextureAssets.MagicPixel.Value, b, new Color(30, 34, 64) * 0.8f);
		}
	}
}
