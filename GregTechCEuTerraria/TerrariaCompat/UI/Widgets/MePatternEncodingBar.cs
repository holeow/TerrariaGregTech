#nullable enable
using System.Collections.Generic;
using GregTechCEuTerraria.Api.Recipe;
using GregTechCEuTerraria.Api.Recipe.Ingredient;
using GregTechCEuTerraria.AppliedEnergistics.Api.Stacks;
using GregTechCEuTerraria.TerrariaCompat.AppliedEnergistics;
using GregTechCEuTerraria.TerrariaCompat.AppliedEnergistics.Crafting;
using GregTechCEuTerraria.TerrariaCompat.Machine;
using GregTechCEuTerraria.TerrariaCompat.Net.Actions;
using GregTechCEuTerraria.TerrariaCompat.Recipes;
using Terraria.UI;

namespace GregTechCEuTerraria.TerrariaCompat.UI.Widgets;

public sealed class MePatternEncodingBar : UIElement
{
	private const int Cols = 5;
	private const int Slot = 36;
	private const int Step = Slot + 4;
	private const int OutCol = Cols * Step + 6;
	private const int PatCol = OutCol + Step + 4;
	private const int BtnCol = PatCol + Slot + 6;
	public const int BarWidth  = BtnCol + 84;
	public const int BarHeight = 28 + Cols * Step;

	public MePatternEncodingBar(IMePatternEncodingHost term)
	{
		int lvAssembler = Terraria.ModLoader.ModContent.TryFind<Terraria.ModLoader.ModItem>(
			"GregTechCEuTerraria", "lv_assembler", out var asm) ? asm.Type : 0;

		Add(new UIIconRadioButton(
			new int[] { Terraria.ID.ItemID.WorkBench, Terraria.ID.ItemID.Furnace },
			() => term.Encoding.Mode == MePatternType.Crafting,
			() => MachineActions.Send(MePatternEncodingAction.SetMode(MePatternType.Crafting), term.Machine),
			"Crafting pattern - put pattern provider touching Terraria crafting station",
			46, 22), 0, 0);

		Add(new UIIconRadioButton(
			new int[] { lvAssembler, Terraria.ID.ItemID.Chest },
			() => term.Encoding.Mode == MePatternType.Processing,
			() => MachineActions.Send(MePatternEncodingAction.SetMode(MePatternType.Processing), term.Machine),
			"Processing pattern - put pattern provider touching chest or another storage container",
			46, 22), 50, 0);

		Btn(100, 1, 120, 20, () => "Select Recipe", () => PickRecipe(term));

		for (int i = 0; i < PatternEncodingState.InputSlots; i++)
			Add(new UIPatternSlot(term, output: false, i, Slot),
				(i % Cols) * Step, 28 + (i / Cols) * Step);

		Add(new UIDynamicLabel(() =>
			term.Encoding.Mode == MePatternType.Crafting && term.Encoding.Inputs.IsEmpty()
				? "Use\n\"Select\nRecipe\""
				: "", 0.65f), 8, 36);

		for (int i = 0; i < PatternEncodingState.OutputSlots; i++)
			Add(new UIPatternSlot(term, output: true, i, Slot), OutCol, 28 + i * Step);

		if (!Config.GTConfig.Instance.FreeMePatterns)
			Add(new UISlot(term.Machine, term.BlankSlotGroup, 0, ItemSlot.Context.ChestItem)
				{ EmptyHint = "Put blank ME patterns here" }, PatCol, 28, Slot);
		Add(new UISlot(term.Machine, term.EncodedSlotGroup, 0, ItemSlot.Context.ChestItem)
			{ EmptyHint = "The encoded pattern appears here - take it out and drop it in a Pattern\nProvider. Drop an existing pattern here to load it back into the grid for editing" }, PatCol, 28 + Step, Slot);

		Append(new UITextButton(() => "Encode",
			() => MachineActions.Send(MePatternEncodingAction.Encode(), term.Machine), null,
			null,
			84, 24)
		{
			Left = StyleDimension.FromPixels(BtnCol),
			Top = StyleDimension.FromPixels(28),
			IsDisabled = () => !term.Encoding.CanEncode && !term.Encoding.HasEncodedOutput,
			DisabledTooltip = "Can't encode: in Crafting mode this recipe has no vanilla station form "
				+ "(switch to Processing), or there's nothing to encode",
		});
		Btn(BtnCol, 58, 84, 22, () => "Clear",
			() => MachineActions.Send(MePatternEncodingAction.Clear(), term.Machine));

		Append(new UITextButton(() => "Cycle Output",
			() => MachineActions.Send(MePatternEncodingAction.CycleOutput(), term.Machine), null,
			"Rotate which output is the primary (first) output",
			84, 22)
		{
			Left = StyleDimension.FromPixels(BtnCol),
			Top  = StyleDimension.FromPixels(88),
			IsVisible = () => term.Encoding.CanCycleOutputs,
		});

		Append(new UITextButton(() => "x2",
			() => MachineActions.Send(MePatternEncodingAction.Scale(true), term.Machine), null,
			"Double every input and output amount",
			40, 22)
		{
			Left = StyleDimension.FromPixels(BtnCol),
			Top  = StyleDimension.FromPixels(118),
			IsVisible = () => term.Encoding.CanScale,
		});
		Append(new UITextButton(() => "/2",
			() => MachineActions.Send(MePatternEncodingAction.Scale(false), term.Machine), null,
			"Halve every input and output amount",
			40, 22)
		{
			Left = StyleDimension.FromPixels(BtnCol + 44),
			Top  = StyleDimension.FromPixels(118),
			IsVisible = () => term.Encoding.CanScale,
			IsDisabled = () => !term.Encoding.CanHalve,
			DisabledTooltip = "Can't halve: some amount is odd",
		});
	}

	private void Add(UIElement el, int x, int y, int size = -1)
	{
		el.Left = StyleDimension.FromPixels(x);
		el.Top  = StyleDimension.FromPixels(y);
		if (size > 0)
		{
			el.Width  = StyleDimension.FromPixels(size);
			el.Height = StyleDimension.FromPixels(size);
		}
		Append(el);
	}

	private void Btn(int x, int y, int w, int h, System.Func<string> label, System.Action onLeft, string? tooltip = null)
	{
		Append(new UITextButton(label, onLeft, null, tooltip, w, h)
		{
			Left = StyleDimension.FromPixels(x),
			Top  = StyleDimension.FromPixels(y),
		});
	}

	private static void PickRecipe(IMePatternEncodingHost term)
	{
		GlobalRecipeBrowserSystem.Open();
	}

	public static void FillFromRecipe(IMePatternEncodingHost term, GTRecipe gt)
	{
		var tr = CraftingRecipeResolver.FindForGtRecipe(gt, out _);
		if (tr != null) FillCrafting(term, gt, tr);
		else FillProcessing(term, gt);
	}

	private static void FillCrafting(IMePatternEncodingHost term, GTRecipe gt, Terraria.Recipe tr)
	{
		var inputs = new List<(AEKey, long)>();
		var tags = new List<string?>();
		foreach (var req in tr.requiredItem)
		{
			if (req is not { IsAir: false }) continue;
			inputs.Add((AEItemKey.OfType(req.type), req.stack));
			tags.Add(TagForItem(gt, req.type));
		}
		int station = tr.requiredTile.Count > 0 ? tr.requiredTile[0] : -1;
		var output = new[] { ((AEKey)AEItemKey.OfType(tr.createItem.type), (long)tr.createItem.stack) };
		MachineActions.Send(MePatternEncodingAction.SetContents(
			MePatternType.Crafting, station, inputs.ToArray(), output, tags.ToArray()), term.Machine);
	}

	private static void FillProcessing(IMePatternEncodingHost term, GTRecipe gt)
	{
		var inputs = new List<(AEKey, long)>();
		var outputs = new List<(AEKey, long)>();
		var tags = new List<string?>();
		foreach (var (ing, count) in gt.GetItemInputs())
			if (ResolveKey(ing) is { } k) { inputs.Add((k, count)); tags.Add(TagNameOf(ing)); }
		foreach (var (fi, amt) in gt.GetFluidInputs())
			if (ResolveFluid(fi) is { } f) { inputs.Add((AEFluidKey.Of(f), amt)); tags.Add(fi.TagName); }
		foreach (var (ing, count) in gt.GetItemOutputs())
			if (ResolveKey(ing) is { } k) outputs.Add((k, count));
		foreach (var (fi, amt) in gt.GetFluidOutputs())
			if (ResolveFluid(fi) is { } f) outputs.Add((AEFluidKey.Of(f), amt));
		MachineActions.Send(MePatternEncodingAction.SetContents(
			MePatternType.Processing, -1, inputs.ToArray(), outputs.ToArray(), tags.ToArray()), term.Machine);
	}

	private static string? TagNameOf(Ingredient ing)
	{
		var inner = ing is SizedIngredient s ? s.Inner : ing;
		return inner is TagIngredient tag ? tag.TagName : null;
	}

	private static string? TagForItem(GTRecipe gt, int itemType)
	{
		foreach (var (ing, _) in gt.GetItemInputs())
		{
			var tag = TagNameOf(ing);
			if (tag == null) continue;
			foreach (var m in ing.GetItems())
				if (m.type == itemType) return tag;
		}
		return null;
	}

	private static AEKey? ResolveKey(Ingredient ing)
	{
		var items = ing.GetItems();
		return items.Count > 0 && !items[0].IsAir ? AEItemKey.Of(items[0]) : null;
	}

	private static Api.Fluids.FluidType? ResolveFluid(FluidIngredient fi) =>
		fi.ExactType ?? (fi.GetFluids().Count > 0 ? fi.GetFluids()[0] : null);
}
