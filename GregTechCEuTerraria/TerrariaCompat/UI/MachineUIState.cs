#nullable enable
using GregTechCEuTerraria.Api.Capability;
using System.Collections.Generic;
using System.Linq;
using GregTechCEuTerraria.TerrariaCompat.Machine;
using GregTechCEuTerraria.TerrariaCompat.Recipes;
using GregTechCEuTerraria.Api.Machine.Feature;
using GregTechCEuTerraria.Api.Recipe;
using GregTechCEuTerraria.Api.Recipe.Ingredient;
using GregTechCEuTerraria.Api.Capability.Recipe;
using GregTechCEuTerraria.Api.Cover;
using GregTechCEuTerraria.TerrariaCompat.UI.Widgets;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.ModLoader;
using Terraria.UI;

namespace GregTechCEuTerraria.TerrariaCompat.UI;

public sealed class MachineUIState : UIModalWindow
{
	private const float SlotGap = 1f;

	public static bool ShowCoversOutputs = false;

	private MetaMachine? _entity;
	private MachineUILayout? _layout;
	private float _leftStackOffsetPx;
	private bool _pendingRebuild;
	private (float W, float H)? _uiDims;
	private CoverSide? _openCoverSide;
	private UIElement? _coverPopup;
	private CoverSide? _pendingCoverOpen;
	private int _coverPopupFilterSig; // (0/1/2 = none/simple/tag)
	private UIElement? _machinePanel;

	public MetaMachine? Entity => _entity;

	public void Bind(MetaMachine entity, MachineUILayout layout)
	{
		_entity = entity;
		_layout = layout;
		_openCoverSide = null;
		_coverPopup = null;
		_pendingCoverOpen = null;
		_machinePanel = null;
		_uiDims = null;
		RemoveAllChildren();
		BuildPanel();
	}

	public void Unbind()
	{
		_entity = null;
		_layout = null;
		_openCoverSide = null;
		_coverPopup = null;
		_pendingCoverOpen = null;
		_machinePanel = null;
		_uiDims = null;
		RemoveAllChildren();
	}

	public void RequestCoverSettings(CoverSide side) => _pendingCoverOpen = side;

	private void ToggleCoverSettings(CoverSide side)
	{
		if (_entity is null || _layout is null) return;
		bool wasSame = _openCoverSide == side;
		CloseCoverSettings();
		if (wasSame) return;
		OpenCoverSettings(side);
	}

	private void OpenCoverSettings(CoverSide side)
	{
		if (_entity is null || _layout is null) return;

		var popup = CoverSettingsUI.Build(_entity, side, _layout.Scale);
		if (popup is null) return;
		popup.HAlign = 0.5f;
		popup.VAlign = 0f;
		float panelBottom = 0f;
		if (_machinePanel is not null)
		{
			var d = _machinePanel.GetDimensions();
			panelBottom = d.Y + d.Height;
		}
		popup.Top = StyleDimension.FromPixels(panelBottom + 8f);
		Append(popup);
		_coverPopup = popup;
		_openCoverSide = side;
		_coverPopupFilterSig = CoverSettingsUI.FilterEditorSignature(_entity, side);
	}

	private void CloseCoverSettings()
	{
		Widgets.UITextField.UnfocusAll();
		_coverPopup?.Remove();
		_coverPopup = null;
		_openCoverSide = null;
	}

	public void RequestRebuild() => _pendingRebuild = true;

	private void Rebuild()
	{
		if (_entity is null || _layout is null) return;
		Widgets.UITextField.UnfocusAll();
		_openCoverSide = null;
		_coverPopup = null;
		_pendingCoverOpen = null;
		_machinePanel = null;
		RemoveAllChildren();
		BuildPanel();
	}

	public override void Update(GameTime gameTime)
	{
		base.Update(gameTime);
		if (_pendingRebuild && !Main.mouseLeft && !Main.mouseRight)
		{
			_pendingRebuild = false;
			Rebuild();
			return;
		}
		if (_pendingCoverOpen is { } pending)
		{
			_pendingCoverOpen = null;
			ToggleCoverSettings(pending);
		}
		if (_openCoverSide is { } open)
		{
			if (_entity is null || _entity.GetCoverAtSide(open) is null)
				CloseCoverSettings();
			else if (!Main.mouseLeft && !Main.mouseRight
			         && CoverSettingsUI.FilterEditorSignature(_entity, open) != _coverPopupFilterSig)
			{
				CloseCoverSettings();
				OpenCoverSettings(open);
			}
		}
	}


	private void BuildPanel()
	{
		if (_entity is null || _layout is null) return;

		if (_layout.BuildOverride is { } custom)
		{
			custom(this, _entity);
			return;
		}

		float s = _layout.Scale;
		_leftStackOffsetPx = 0f;

		var panel = new UITerrariaPanel
		{
			Width = StyleDimension.FromPixels(_layout.Width * s),
			Height = StyleDimension.FromPixels(_layout.Height * s),
			HAlign = 0.5f,
			VAlign = 0.3f,
		};

		if (!string.IsNullOrEmpty(_layout.Title))
		{
			var title = new UIText(_layout.Title, 1.0f, large: false)
			{
				Left = StyleDimension.FromPixels(8 * s),
				Top = StyleDimension.FromPixels(6 * s),
				TextColor = Common.Energy.VoltageTiers.TextColor(_entity.Tier),
			};
			panel.Append(title);
		}

		foreach (var spec in _layout.Widgets)
		{
			var element = spec.Create(_entity);
			float w = element.Width.Pixels * s;
			float h = element.Height.Pixels * s;
			if (element is Widgets.UISlot or Widgets.UIFluidSlot)
			{
				w -= SlotGap;
				h -= SlotGap;
			}
			element.Width = StyleDimension.FromPixels(w);
			element.Height = StyleDimension.FromPixels(h);
			element.Left = StyleDimension.FromPixels(spec.X * s);
			element.Top = StyleDimension.FromPixels(spec.Y * s);
			panel.Append(element);
		}

		Append(panel);
		_machinePanel = panel;

		if (_layout.LeftPanel is not null)
			AppendLeftSidePanel(_layout.LeftPanel);
		if (_layout.TopPanel is not null)
			AppendTopSidePanel(_layout.TopPanel);
		if (_layout.BottomPanel is not null)
			AppendBottomSidePanel(_layout.BottomPanel);

		if (ShowCoversOutputs)
			AppendIOConfigPanel(_entity, panel);

		AppendMachineControlsPanel(_entity);

		if (_entity is TerrariaCompat.Machine.Multiblock.WorkableMultiblockMachine wmm
			&& wmm.GetRecipeTypes().Length > 1)
			AppendModeSelectPanel(wmm, panel);

		bool hasArrow = _layout.Widgets.Any(w => w is ProgressArrowWidgetSpec);
		if (!hasArrow && RecipeBrowserLauncher.CanOpen(_entity))
		{
			var entity = _entity;
			const int boxW = 112, boxH = 22;
			var showBtn = new Widgets.UITextButton(
				() => RecipeBrowserLauncher.ButtonLabel,
				() => RecipeBrowserLauncher.OpenForMachine(entity),
				null,
				RecipeBrowserLauncher.ArrowTooltip, boxW, boxH)
			{
				Left = StyleDimension.FromPixels(_layout.Width * s - boxW - 8f),
				Top = StyleDimension.FromPixels(6f * s),
			};
			panel.Append(showBtn);
		}
	}

	private void AppendIOConfigPanel(MetaMachine entity, UITerrariaPanel machinePanel)
	{
		bool wantsItem  = entity.SupportsAutoOutputItems;
		bool wantsFluid = entity.SupportsAutoOutputFluids;
		bool wantsCover = entity.SupportsCovers;
		var part        = entity as Machine.Multiblock.Part.TieredIOPartMachine;
		bool wantsPart  = part is not null;

		float s = _layout!.Scale;
		const int ClusterSize = UIDirectionSelector.ClusterSize; // 54
		const int ToggleSize  = 18;
		const int Gap         = 4;
		const int Padding     = 6;

		int clusterCount = (wantsCover ? 1 : 0) + (wantsItem ? 1 : 0) + (wantsFluid ? 1 : 0) + (wantsPart ? 1 : 0);
		if (clusterCount == 0) return;
		int innerW = ClusterSize * clusterCount + Gap * (clusterCount - 1);
		int innerH = ClusterSize;
		int outerW = innerW + Padding * 2;
		int outerH = innerH + Padding * 2;

		var (_, machineTop, _) = MachinePanelScreenAnchor();
		float ioH       = outerH * s;
		const float GapAbove = 6f;
		float ioTop = System.Math.Max(8f, machineTop - ioH - GapAbove);

		var ioPanel = new UITerrariaPanel
		{
			Width = StyleDimension.FromPixels(outerW * s),
			Height = StyleDimension.FromPixels(ioH),
			HAlign = 0.5f,
			VAlign = 0f,
			Top = StyleDimension.FromPixels(ioTop),
		};

		int colX = Padding;

		if (wantsCover)
		{
			ioPanel.Append(new UICoverPanel(entity, RequestCoverSettings)
			{
				Left   = StyleDimension.FromPixels(colX * s),
				Top    = StyleDimension.FromPixels(Padding * s),
				Width  = StyleDimension.FromPixels(ClusterSize * s),
				Height = StyleDimension.FromPixels(ClusterSize * s),
			});
			colX += ClusterSize + Gap;
		}

		if (wantsItem)
		{
			var itemCluster = new UIDirectionSelector(
				UIDirectionSelector.Mode.Items,
				() => entity.AutoOutput?.ItemOutputDirection ?? IODirection.None,
				d => TerrariaCompat.Net.Actions.MachineActions.Send(TerrariaCompat.Net.Actions.IOConfigSetAction.OfDirection(TerrariaCompat.Net.Actions.IOConfigSetAction.Field.ItemOutputSide, d), entity),
				() => entity.AutoOutput?.IsAutoOutputItems ?? false,
				b => TerrariaCompat.Net.Actions.MachineActions.Send(TerrariaCompat.Net.Actions.IOConfigSetAction.OfBool(TerrariaCompat.Net.Actions.IOConfigSetAction.Field.ItemAutoOutput, b), entity),
				autoOutputToggleable: false)
			{
				Left = StyleDimension.FromPixels(colX * s),
				Top  = StyleDimension.FromPixels(Padding * s),
				Width = StyleDimension.FromPixels(ClusterSize * s),
				Height = StyleDimension.FromPixels(ClusterSize * s),
			};
			ioPanel.Append(itemCluster);

			ioPanel.Append(new UIPowerToggle(
				() => entity.AutoOutput?.IsAutoOutputItems ?? false,
				v => TerrariaCompat.Net.Actions.MachineActions.Send(TerrariaCompat.Net.Actions.IOConfigSetAction.OfBool(TerrariaCompat.Net.Actions.IOConfigSetAction.Field.ItemAutoOutput, v), entity))
			{
				Left = StyleDimension.FromPixels(colX * s),
				Top  = StyleDimension.FromPixels((Padding + ClusterSize - ToggleSize) * s),
				Width = StyleDimension.FromPixels(ToggleSize * s),
				Height = StyleDimension.FromPixels(ToggleSize * s),
				TooltipFor = on => $"Auto-output items: {(on ? "ON" : "OFF")}",
			});

			ioPanel.Append(new UIOverlayToggle(
				"GregTechCEuTerraria/Content/Textures/gui/overlay/tool_allow_input",
				() => entity.AutoOutput?.AllowItemInputFromOutputSide ?? false,
				v => TerrariaCompat.Net.Actions.MachineActions.Send(TerrariaCompat.Net.Actions.IOConfigSetAction.OfBool(TerrariaCompat.Net.Actions.IOConfigSetAction.Field.AllowItemInputFromOutput, v), entity))
			{
				Left = StyleDimension.FromPixels((colX + ClusterSize - ToggleSize) * s),
				Top  = StyleDimension.FromPixels((Padding + ClusterSize - ToggleSize) * s),
				Width = StyleDimension.FromPixels(ToggleSize * s),
				Height = StyleDimension.FromPixels(ToggleSize * s),
				TooltipFor = on => $"Allow input from output side: {(on ? "ON" : "OFF")}\nShould be ON if you want Pattern Provider to push from that side",
			});
			colX += ClusterSize + Gap;
		}

		if (wantsFluid)
		{
			var fluidCluster = new UIDirectionSelector(
				UIDirectionSelector.Mode.Fluids,
				() => entity.AutoOutput?.FluidOutputDirection ?? IODirection.None,
				d => TerrariaCompat.Net.Actions.MachineActions.Send(TerrariaCompat.Net.Actions.IOConfigSetAction.OfDirection(TerrariaCompat.Net.Actions.IOConfigSetAction.Field.FluidOutputSide, d), entity),
				() => entity.AutoOutput?.IsAutoOutputFluids ?? false,
				b => TerrariaCompat.Net.Actions.MachineActions.Send(TerrariaCompat.Net.Actions.IOConfigSetAction.OfBool(TerrariaCompat.Net.Actions.IOConfigSetAction.Field.FluidAutoOutput, b), entity),
				autoOutputToggleable: false)
			{
				Left = StyleDimension.FromPixels(colX * s),
				Top  = StyleDimension.FromPixels(Padding * s),
				Width = StyleDimension.FromPixels(ClusterSize * s),
				Height = StyleDimension.FromPixels(ClusterSize * s),
			};
			ioPanel.Append(fluidCluster);

			ioPanel.Append(new UIPowerToggle(
				() => entity.AutoOutput?.IsAutoOutputFluids ?? false,
				v => TerrariaCompat.Net.Actions.MachineActions.Send(TerrariaCompat.Net.Actions.IOConfigSetAction.OfBool(TerrariaCompat.Net.Actions.IOConfigSetAction.Field.FluidAutoOutput, v), entity))
			{
				Left = StyleDimension.FromPixels(colX * s),
				Top  = StyleDimension.FromPixels((Padding + ClusterSize - ToggleSize) * s),
				Width = StyleDimension.FromPixels(ToggleSize * s),
				Height = StyleDimension.FromPixels(ToggleSize * s),
				TooltipFor = on => $"Auto-output fluids: {(on ? "ON" : "OFF")}",
			});

			ioPanel.Append(new UIOverlayToggle(
				"GregTechCEuTerraria/Content/Textures/gui/overlay/tool_allow_input",
				() => entity.AutoOutput?.AllowFluidInputFromOutputSide ?? false,
				v => TerrariaCompat.Net.Actions.MachineActions.Send(TerrariaCompat.Net.Actions.IOConfigSetAction.OfBool(TerrariaCompat.Net.Actions.IOConfigSetAction.Field.AllowFluidInputFromOutput, v), entity))
			{
				Left = StyleDimension.FromPixels((colX + ClusterSize - ToggleSize) * s),
				Top  = StyleDimension.FromPixels((Padding + ClusterSize - ToggleSize) * s),
				Width = StyleDimension.FromPixels(ToggleSize * s),
				Height = StyleDimension.FromPixels(ToggleSize * s),
				TooltipFor = on => $"Allow input from output side: {(on ? "ON" : "OFF")}\nShould be ON if you want Pattern Provider to push from that side",
			});
		}

		if (wantsPart)
		{
			var partCluster = new UIDirectionSelector(
				part!.PartIoConfigMode,
				() => part.IoDirection,
				d => TerrariaCompat.Net.Actions.MachineActions.Send(new TerrariaCompat.Net.Actions.PartIoDirectionSetAction(d), entity),
				() => part.WorkingEnabled,
				b => TerrariaCompat.Net.Actions.MachineActions.Send(new TerrariaCompat.Net.Actions.PowerToggleAction(b), entity),
				autoOutputToggleable: false)
			{
				Left = StyleDimension.FromPixels(colX * s),
				Top  = StyleDimension.FromPixels(Padding * s),
				Width = StyleDimension.FromPixels(ClusterSize * s),
				Height = StyleDimension.FromPixels(ClusterSize * s),
			};
			ioPanel.Append(partCluster);
		}

		Append(ioPanel);
	}

	private static bool HasIOConfigClusters(MetaMachine entity)
	{
		bool wantsItem  = entity.SupportsAutoOutputItems;
		bool wantsFluid = entity.SupportsAutoOutputFluids;
		bool wantsCover = entity.SupportsCovers;
		bool wantsPart  = entity is Machine.Multiblock.Part.TieredIOPartMachine;
		return wantsCover || wantsItem || wantsFluid || wantsPart;
	}

	private void AppendMachineControlsPanel(MetaMachine entity)
	{
		float s = _layout!.Scale;
		const int BtnSize = 22;
		const int Padding = 4;
		const int RowGap = 4;
		const float PanelGap = 6f;

		bool hasPower   = entity.SupportsWorkingEnabledToggle;
		bool hasCovers  = HasIOConfigClusters(entity);
		bool hasCharger = entity.GetSlotGroup(Machine.SlotGroup.Charger) is not null;
		var distinctPart = entity is Api.Machine.Feature.Multiblock.IDistinctPart dp
			&& entity is Machine.Multiblock.Part.TieredIOPartMachine iop
			&& iop.Io == Api.Capability.Recipe.IO.IN ? dp : null;

		int count = (hasPower ? 1 : 0) + (hasCovers ? 1 : 0) + (hasCharger ? 1 : 0) + (distinctPart != null ? 1 : 0);
		if (count == 0) return;

		int outerW = BtnSize + Padding * 2;
		int outerH = BtnSize * count + RowGap * (count - 1) + Padding * 2;

		var (machineLeft, machineTop, _) = MachinePanelScreenAnchor();

		var panel = new UITerrariaPanel
		{
			Width  = StyleDimension.FromPixels(outerW * s),
			Height = StyleDimension.FromPixels(outerH * s),
			HAlign = 0f,
			VAlign = 0f,
			Left = StyleDimension.FromPixels(machineLeft - outerW * s - PanelGap),
			Top  = StyleDimension.FromPixels(machineTop + _leftStackOffsetPx),
		};
		Append(panel);
		_leftStackOffsetPx += outerH * s + PanelGap;

		int rowY = Padding;

		if (hasPower)
		{
			panel.Append(new Widgets.UIPowerToggle(
				() => entity.WorkingEnabled,
				v => TerrariaCompat.Net.Actions.MachineActions.Send(new TerrariaCompat.Net.Actions.PowerToggleAction(v), entity),
				width: (int)(BtnSize * s),
				height: (int)(BtnSize * s))
			{
				Left = StyleDimension.FromPixels(Padding * s),
				Top  = StyleDimension.FromPixels(rowY * s),
			});
			rowY += BtnSize + RowGap;
		}

		if (hasCovers)
		{
			panel.Append(new Widgets.UIShowCoversToggle(
				() => ShowCoversOutputs,
				v => { ShowCoversOutputs = v; RequestRebuild(); },
				width: (int)(BtnSize * s),
				height: (int)(BtnSize * s))
			{
				Left = StyleDimension.FromPixels(Padding * s),
				Top  = StyleDimension.FromPixels(rowY * s),
			});
			rowY += BtnSize + RowGap;
		}

		if (hasCharger)
		{
			var bgTex = ModContent.Request<Microsoft.Xna.Framework.Graphics.Texture2D>(
				"GregTechCEuTerraria/Content/Textures/gui/overlay/charger_slot_overlay");
			panel.Append(new UIImage(bgTex)
			{
				Left = StyleDimension.FromPixels(Padding * s),
				Top  = StyleDimension.FromPixels(rowY * s),
				Width  = StyleDimension.FromPixels(BtnSize * s),
				Height = StyleDimension.FromPixels(BtnSize * s),
				ScaleToFit = true,
				AllowResizingDimensions = false,
			});
			panel.Append(new Widgets.UISlot(entity, Machine.SlotGroup.Charger, slotIndex: 0)
			{
				Left = StyleDimension.FromPixels(Padding * s),
				Top  = StyleDimension.FromPixels(rowY * s),
				Width  = StyleDimension.FromPixels(BtnSize * s),
				Height = StyleDimension.FromPixels(BtnSize * s),
				EmptyHint = $"Can charge tools and batteries up to {Common.Energy.VoltageTiers.ShortName(entity.Tier)} energy tier",
			});
			rowY += BtnSize + RowGap;
		}

		if (distinctPart is not null)
		{
			var btn = new Widgets.UIToggleButton(
				"GregTechCEuTerraria/Content/Textures/gui/widget/button_distinct_buses",
				() => distinctPart.IsDistinct(),
				v => TerrariaCompat.Net.Actions.MachineActions.Send(new TerrariaCompat.Net.Actions.DistinctSetAction(v), entity),
				"")
			{
				Left = StyleDimension.FromPixels(Padding * s),
				Top  = StyleDimension.FromPixels(rowY * s),
				Width  = StyleDimension.FromPixels(BtnSize * s),
				Height = StyleDimension.FromPixels(BtnSize * s),
			};
			btn.IconSrcRectFor = on =>
			{
				var tex = ModContent.Request<Texture2D>(
					"GregTechCEuTerraria/Content/Textures/gui/widget/button_distinct_buses",
					ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
				int half = tex.Height / 2;
				return on
					? new Rectangle(0, 0,    tex.Width, half)
					: new Rectangle(0, half, tex.Width, half);
			};
			btn.TooltipFor = on => "Distinct Buses: " + (on ? "Yes" : "No");
			panel.Append(btn);
			rowY += BtnSize + RowGap;
		}
	}

	private void AppendTopSidePanel(MachineUILayout.SatellitePanelSpec spec)
	{
		const int Padding = 6, LabelHeight = 11;
		const float Gap = 6f;
		bool hasTitle = !string.IsNullOrEmpty(spec.Title);
		int innerTop = Padding + (hasTitle ? LabelHeight + 2 : 0);
		int outerW = spec.Width + Padding * 2;
		int outerH = innerTop + spec.Height + Padding;

		var (machineLeft, machineTop, machineW) = MachinePanelScreenAnchor();
		float left = machineLeft + (machineW - outerW) / 2f;
		float top = System.Math.Max(8f, machineTop - outerH - Gap);

		var sidePanel = new UITerrariaPanel
		{
			Width  = StyleDimension.FromPixels(outerW),
			Height = StyleDimension.FromPixels(outerH),
			HAlign = 0f,
			VAlign = 0f,
			Left = StyleDimension.FromPixels(left),
			Top  = StyleDimension.FromPixels(top),
		};

		if (hasTitle)
			sidePanel.Append(new UIText(spec.Title, 0.6f)
			{
				Left = StyleDimension.FromPixels(Padding),
				Top  = StyleDimension.FromPixels(Padding),
			});

		var el = spec.Element;
		el.Left   = StyleDimension.FromPixels(Padding);
		el.Top    = StyleDimension.FromPixels(innerTop);
		el.Width  = StyleDimension.FromPixels(spec.Width);
		el.Height = StyleDimension.FromPixels(spec.Height);
		sidePanel.Append(el);

		Append(sidePanel);
	}

	private void AppendBottomSidePanel(MachineUILayout.SatellitePanelSpec spec)
	{
		const int Padding = 6, LabelHeight = 11;
		const float Gap = 6f;
		bool hasTitle = !string.IsNullOrEmpty(spec.Title);
		int innerTop = Padding + (hasTitle ? LabelHeight + 2 : 0);
		int outerW = spec.Width + Padding * 2;
		int outerH = innerTop + spec.Height + Padding;

		float s = _layout!.Scale;
		var (machineLeft, machineTop, machineW) = MachinePanelScreenAnchor();
		float machineH = _layout.Height * s;
		float left = machineLeft + (machineW - outerW) / 2f;
		float top = machineTop + machineH + Gap;

		var sidePanel = new UITerrariaPanel
		{
			Width  = StyleDimension.FromPixels(outerW),
			Height = StyleDimension.FromPixels(outerH),
			HAlign = 0f,
			VAlign = 0f,
			Left = StyleDimension.FromPixels(left),
			Top  = StyleDimension.FromPixels(top),
		};

		if (hasTitle)
			sidePanel.Append(new UIText(spec.Title, 0.6f)
			{
				Left = StyleDimension.FromPixels(Padding),
				Top  = StyleDimension.FromPixels(Padding),
			});

		var el = spec.Element;
		el.Left   = StyleDimension.FromPixels(Padding);
		el.Top    = StyleDimension.FromPixels(innerTop);
		el.Width  = StyleDimension.FromPixels(spec.Width);
		el.Height = StyleDimension.FromPixels(spec.Height);
		sidePanel.Append(el);

		Append(sidePanel);
	}

	private void AppendLeftSidePanel(MachineUILayout.SatellitePanelSpec spec)
	{
		const int Padding = 6, LabelHeight = 11;
		const float Gap = 6f;
		bool hasTitle = !string.IsNullOrEmpty(spec.Title);
		int innerTop = Padding + (hasTitle ? LabelHeight + 2 : 0);
		int outerW = spec.Width + Padding * 2;
		int outerH = innerTop + spec.Height + Padding;

		var (machineLeft, machineTop, _) = MachinePanelScreenAnchor();

		var sidePanel = new UITerrariaPanel
		{
			Width  = StyleDimension.FromPixels(outerW),
			Height = StyleDimension.FromPixels(outerH),
			HAlign = 0f,
			VAlign = 0f,
			Left = StyleDimension.FromPixels(machineLeft - outerW - Gap),
			Top  = StyleDimension.FromPixels(machineTop + _leftStackOffsetPx),
		};

		if (hasTitle)
			sidePanel.Append(new UIText(spec.Title, 0.6f)
			{
				Left = StyleDimension.FromPixels(Padding),
				Top  = StyleDimension.FromPixels(Padding),
			});

		var el = spec.Element;
		el.Left   = StyleDimension.FromPixels(Padding);
		el.Top    = StyleDimension.FromPixels(innerTop);
		el.Width  = StyleDimension.FromPixels(spec.Width);
		el.Height = StyleDimension.FromPixels(spec.Height);
		sidePanel.Append(el);

		Append(sidePanel);
		_leftStackOffsetPx += outerH + Gap;
	}

	private void AppendModeSelectPanel(
		TerrariaCompat.Machine.Multiblock.WorkableMultiblockMachine multi,
		UITerrariaPanel machinePanel)
	{
		float s = _layout!.Scale;
		var recipeTypes = multi.GetRecipeTypes();
		const int RowH    = 16;
		const int RowGap  = 2;
		const int LabelH  = 9;
		const int Padding = 4;
		const int InnerW  = 86;
		int innerH  = LabelH + 2 + recipeTypes.Length * RowH + (recipeTypes.Length - 1) * RowGap;
		int outerW  = InnerW + Padding * 2;
		int outerH  = innerH + Padding * 2;
		const float Gap = 6f;

		var (machineLeft, machineTop, machineW) = MachinePanelScreenAnchor();
		float left = machineLeft + (machineW - outerW * s) / 2f;
		float top  = System.Math.Max(8f, machineTop - outerH * s - Gap);

		var modePanel = new UITerrariaPanel
		{
			Width  = StyleDimension.FromPixels(outerW * s),
			Height = StyleDimension.FromPixels(outerH * s),
			HAlign = 0f,
			VAlign = 0f,
			Left = StyleDimension.FromPixels(left),
			Top  = StyleDimension.FromPixels(top),
		};
		Append(modePanel);

		var label = new UIText("Mode", 0.55f)
		{
			Left = StyleDimension.FromPixels(Padding * s),
			Top  = StyleDimension.FromPixels(Padding * s),
		};
		modePanel.Append(label);

		int rowsTop = Padding + LabelH + 2;
		for (int i = 0; i < recipeTypes.Length; i++)
		{
			int captured = i;
			string typeName = Api.Machine.Multiblock.MultiblockDisplayText.Tr($"gtceu.{recipeTypes[i].RegistryName}");
			var btn = new Widgets.UITextButton(
				() => typeName,
				onLeft: () => TerrariaCompat.Net.Actions.MachineActions.Send(
					new TerrariaCompat.Net.Actions.ActiveRecipeTypeSetAction(captured), multi),
				tooltip: $"Switch to {typeName}",
				width: (int)(InnerW * s),
				height: (int)(RowH * s))
			{
				IsActive = () => multi.ActiveRecipeType == captured,
				Left = StyleDimension.FromPixels(Padding * s),
				Top  = StyleDimension.FromPixels((rowsTop + captured * (RowH + RowGap)) * s),
			};
			modePanel.Append(btn);
		}
	}

	private (float W, float H) UiDimensions()
	{
		if (_uiDims is { } d) return d;
		float uiScale = Main.UIScale <= 0 ? 1f : Main.UIScale;
		var orig = Terraria.GameInput.PlayerInput.OriginalScreenSize;
		var dims = (orig.X / uiScale, orig.Y / uiScale);
		_uiDims = dims;
		return dims;
	}

	private (float Left, float Top, float MachineWidth) MachinePanelScreenAnchor()
	{
		float s = _layout!.Scale;
		var (uiW, uiH) = UiDimensions();
		float machineW = _layout.Width * s;
		float machineH = _layout.Height * s;
		float machineLeft = 0.5f * (uiW - machineW);
		float machineTop  = 0.3f * (uiH - machineH);
		return (machineLeft, machineTop, machineW);
	}

}
