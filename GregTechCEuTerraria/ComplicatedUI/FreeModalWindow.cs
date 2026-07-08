#nullable enable
using System.Collections.Generic;
using GregTechCEuTerraria.TerrariaCompat.UI.Widgets;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.UI;

namespace GregTechCEuTerraria.TerrariaCompat.UI;

public abstract class FreeModalWindow : UIModalWindow
{
	public static bool DragActive { get; private set; }

	protected enum DragMode { None, Move, Resize }
	private DragMode _dragMode = DragMode.None;
	private Vector2 _dragLast;
	private bool _geometryDirty;
	private UIElement? _dragVeil;

	protected float OffsetX, OffsetY;
	protected float UserW, UserH;

	protected float CurW, CurH;
	protected float MinW, MaxW, MinH, MaxH;
	protected float OffMinX, OffMaxX, OffMinY, OffMaxY;

	private int _builtScreenW = -1, _builtScreenH = -1;
	private float _builtUiScale = -1f;

	protected static float ClampOffset(float v, float lo, float hi)
		=> lo <= hi ? System.Math.Clamp(v, lo, hi) : System.Math.Clamp(v, hi, lo);

	protected abstract void RebuildWindow();
	protected abstract void ApplyOffsetLive();

	protected void MarkGeometryDirty() => _geometryDirty = true;

	protected void ForceRebuild() => DoRebuild();

	protected virtual void OnModalUpdate(GameTime gameTime) { }

	protected UIDragKnob NewMoveKnob(string tooltip = "Drag to move")
		=> new(() => StartDrag(DragMode.Move), UIDragKnob.Kind.Move, tooltip);

	protected UIDragKnob NewResizeKnob(string tooltip = "Drag to resize")
		=> new(() => StartDrag(DragMode.Resize), UIDragKnob.Kind.Resize, tooltip);

	protected UIDragKnob? ResizeKnob { get; private set; }

	protected void LayoutResizeKnob(UIElement panel, float w, float h, float size,
		string tooltip = "Drag to resize")
	{
		ResizeKnob ??= NewResizeKnob(tooltip);
		ResizeKnob.Width  = StyleDimension.FromPixels(size);
		ResizeKnob.Height = StyleDimension.FromPixels(size);
		ResizeKnob.Left   = StyleDimension.FromPixels(w - size - 2);
		ResizeKnob.Top    = StyleDimension.FromPixels(h - size - 2);
		if (ResizeKnob.Parent != panel) panel.Append(ResizeKnob);
	}

	protected void RaiseResizeKnobToTop(UIElement panel)
	{
		if (ResizeKnob is not null && ResizeKnob.Parent == panel)
		{ panel.RemoveChild(ResizeKnob); panel.Append(ResizeKnob); }
	}

	public IModalHost? Host { get; set; }

	private const int CloseBtnW = 24, PinBtnW = 34, HeaderBtnGap = 6;
	private UITextButton? _closeBtn, _pinBtn;

	protected void LayoutHeaderButtons(UIElement panel, float w, float pad, float btnH)
	{
		_closeBtn ??= new UITextButton(() => "X", () => Host?.RequestClose(),
			tooltip: "Close", width: CloseBtnW, height: (int)btnH);
		_pinBtn ??= new UITextButton(() => "Pin",
			() => { if (Host is { } h) h.Pinned = !h.Pinned; },
			tooltip: "Pin open (Escape won't close it)", width: PinBtnW, height: (int)btnH)
		{
			IsActive  = () => Host?.Pinned == true,
			IsVisible = () => Host?.PinSupported == true,
		};

		_closeBtn.Height = StyleDimension.FromPixels(btnH);
		_pinBtn.Height   = StyleDimension.FromPixels(btnH);
		_closeBtn.Left = StyleDimension.FromPixels(w - pad - CloseBtnW);
		_closeBtn.Top  = StyleDimension.FromPixels(pad);
		_pinBtn.Left   = StyleDimension.FromPixels(w - pad - CloseBtnW - HeaderBtnGap - PinBtnW);
		_pinBtn.Top    = StyleDimension.FromPixels(pad);

		if (_pinBtn.Parent   != panel) panel.Append(_pinBtn);
		if (_closeBtn.Parent != panel) panel.Append(_closeBtn);
	}

	protected float HeaderButtonsLeft(float w, float pad)
	{
		float left = w - pad - CloseBtnW;
		if (Host?.PinSupported == true) left -= HeaderBtnGap + PinBtnW;
		return left;
	}

	protected void ApplyCenteredMoveClamp(UIElement panel, Vector2 root, float w, float h,
		float margin = 8f)
	{
		float gLeft = root.X / 2f - w / 2f, gTop = root.Y / 2f - h / 2f;
		OffMinX = margin - gLeft;  OffMaxX = (root.X - margin) - (gLeft + w);
		OffMinY = margin - gTop;   OffMaxY = (root.Y - margin) - (gTop + h);
		OffsetX = ClampOffset(OffsetX, OffMinX, OffMaxX);
		OffsetY = ClampOffset(OffsetY, OffMinY, OffMaxY);
		panel.Left = StyleDimension.FromPixels(OffsetX);
		panel.Top  = StyleDimension.FromPixels(OffsetY);
	}

	protected virtual float HeaderDragHeight => 36f;

	private void MaybeStartHeaderDrag()
	{
		if (_dragMode != DragMode.None) return;
		float hH = HeaderDragHeight;
		if (hH <= 0f) return;
		if (!MouseClick.LeftPressed) return;

		UIElement? panel = null;
		foreach (var c in Children) { panel = c; break; }
		if (panel is null || panel == _dragVeil) return;

		var pr = panel.GetDimensions();
		var mouse = ModalEscape.UiCursor;
		if (mouse.X < pr.X || mouse.X > pr.X + pr.Width
			|| mouse.Y < pr.Y || mouse.Y > pr.Y + hH) return;

		var hit = GetElementAt(mouse);
		if (hit is null || hit == this || hit == panel || hit is UIDragKnob)
			StartDrag(DragMode.Move);
	}

	public const float DefaultW = 800f;
	public const float DefaultH = 450f;
	public const float MinModalW = 480f;
	public const float MinModalH = 270f;

	protected void ResolveSize(float uiW, float uiH, float margin = 24f)
		=> ResolveSize(uiW, uiH, DefaultW, DefaultH, MinModalW, MinModalH, margin);

	protected void ResolveSize(float uiW, float uiH, float prefW, float prefH,
		float minW, float minH, float margin = 24f)
	{
		MaxW = uiW - margin; MaxH = uiH - margin;
		MinW = System.Math.Min(minW, MaxW); MinH = System.Math.Min(minH, MaxH);
		float w = System.Math.Clamp(prefW, MinW, MaxW);
		float h = System.Math.Clamp(prefH, MinH, MaxH);
		if (UserW > 0f) { w = System.Math.Clamp(UserW, MinW, MaxW); UserW = w; }
		if (UserH > 0f) { h = System.Math.Clamp(UserH, MinH, MaxH); UserH = h; }
		CurW = w; CurH = h;
	}

	protected Vector2 RootSize()
	{
		float uiScale = Main.UIScale <= 0 ? 1f : Main.UIScale;
		var d = GetDimensions();
		return new Vector2(
			d.Width  > 1f ? d.Width  : Main.screenWidth  / uiScale,
			d.Height > 1f ? d.Height : Main.screenHeight / uiScale);
	}

	protected void StartDrag(DragMode mode)
	{
		_dragMode = mode;
		_dragLast = ModalEscape.UiCursor;
		if (mode == DragMode.Resize)
		{
			if (UserW <= 0f) UserW = CurW;
			if (UserH <= 0f) UserH = CurH;
		}
	}

	private void ProcessDrag()
	{
		if (_dragMode == DragMode.None) { DragActive = false; return; }
		if (!Main.mouseLeft) { _dragMode = DragMode.None; DragActive = false; return; }
		DragActive = true;
		Main.LocalPlayer.mouseInterface = true;

		var cur = ModalEscape.UiCursor;
		var delta = cur - _dragLast;
		_dragLast = cur;
		if (delta == Vector2.Zero) return;

		if (_dragMode == DragMode.Move)
		{
			OffsetX = ClampOffset(OffsetX + delta.X, OffMinX, OffMaxX);
			OffsetY = ClampOffset(OffsetY + delta.Y, OffMinY, OffMaxY);
			ApplyOffsetLive();
		}
		else
		{
			float nw = System.Math.Clamp(UserW + delta.X, MinW, MaxW);
			float nh = System.Math.Clamp(UserH + delta.Y, MinH, MaxH);
			OffsetX += (nw - UserW) / 2f;
			OffsetY += (nh - UserH) / 2f;
			UserW = nw; UserH = nh;
			_geometryDirty = true;
		}
	}

	public override IEnumerable<Rectangle> OccupiedRects()
	{
		foreach (var e in Elements)
		{
			if (e == _dragVeil) continue;
			var r = e.GetDimensions().ToRectangle();
			if (r.Width > 0 && r.Height > 0) yield return r;
		}
	}

	private void SyncDragVeil()
	{
		if (_dragMode != DragMode.None)
		{
			_dragVeil ??= new UIElement
			{
				Width  = StyleDimension.FromPercent(1f),
				Height = StyleDimension.FromPercent(1f),
			};
			if (_dragVeil.Parent != this) { Append(_dragVeil); Recalculate(); }
		}
		else if (_dragVeil is not null && _dragVeil.Parent == this)
		{
			RemoveChild(_dragVeil);
		}
	}

	protected bool ScreenChanged()
		=> Main.screenWidth  != _builtScreenW
		|| Main.screenHeight != _builtScreenH
		|| System.Math.Abs(Main.UIScale - _builtUiScale) > 0.001f;

	private void DoRebuild()
	{
		RebuildWindow();
		_builtScreenW = Main.screenWidth;
		_builtScreenH = Main.screenHeight;
		_builtUiScale = Main.UIScale;
	}

	public override void OnInitialize() => DoRebuild();

	public override void OnDeactivate()
	{
		_dragMode = DragMode.None;
		DragActive = false;
	}

	public override void Update(GameTime gameTime)
	{
		base.Update(gameTime);
		MaybeStartHeaderDrag();
		ProcessDrag();
		OnModalUpdate(gameTime);
		if (ScreenChanged() || _geometryDirty) { _geometryDirty = false; DoRebuild(); }
		SyncDragVeil();
	}
}
