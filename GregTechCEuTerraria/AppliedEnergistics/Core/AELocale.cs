// English fallbacks for the AppliedEnergistics zone's display strings (upstream's
// GuiText components). Registered at Mod.Load via Language.GetOrRegister so the
// keys populate the committed hjson; call sites resolve through the constants.

#nullable enable
using Terraria.Localization;

namespace GregTechCEuTerraria.AppliedEnergistics.Core;

public static class AELocale
{
	public const string KeyTypeItems = "Mods.GregTechCEuTerraria.AppliedEnergistics.KeyType.Items";
	public const string KeyTypeFluids = "Mods.GregTechCEuTerraria.AppliedEnergistics.KeyType.Fluids";
	public const string StorageMENetwork = "Mods.GregTechCEuTerraria.AppliedEnergistics.Storage.MENetwork";
	public const string StorageExternal = "Mods.GregTechCEuTerraria.AppliedEnergistics.Storage.External";

	public const string SelectItemFluid = "Mods.GregTechCEuTerraria.AppliedEnergistics.SelectItemFluid";

	public const string TerminalNotConnected = "Mods.GregTechCEuTerraria.AppliedEnergistics.Terminal.NotConnected";
	public const string TerminalStorageDevices = "Mods.GregTechCEuTerraria.AppliedEnergistics.Terminal.StorageDevices";

	public const string CraftSubmitOk = "Mods.GregTechCEuTerraria.AppliedEnergistics.CraftSubmit.Ok";
	public const string CraftSubmitIncompletePlan = "Mods.GregTechCEuTerraria.AppliedEnergistics.CraftSubmit.IncompletePlan";
	public const string CraftSubmitNoCpu = "Mods.GregTechCEuTerraria.AppliedEnergistics.CraftSubmit.NoCpu";
	public const string CraftSubmitCpuBusy = "Mods.GregTechCEuTerraria.AppliedEnergistics.CraftSubmit.CpuBusy";
	public const string CraftSubmitCpuOffline = "Mods.GregTechCEuTerraria.AppliedEnergistics.CraftSubmit.CpuOffline";
	public const string CraftSubmitCpuTooSmall = "Mods.GregTechCEuTerraria.AppliedEnergistics.CraftSubmit.CpuTooSmall";
	public const string CraftSubmitMissing = "Mods.GregTechCEuTerraria.AppliedEnergistics.CraftSubmit.Missing";
	public const string CraftSubmitMissingGeneric = "Mods.GregTechCEuTerraria.AppliedEnergistics.CraftSubmit.MissingGeneric";

	public const string CraftDone = "Mods.GregTechCEuTerraria.AppliedEnergistics.Craft.Done";
	public const string CraftDoneSomeone = "Mods.GregTechCEuTerraria.AppliedEnergistics.Craft.Someone";
	public const string CraftDoneItems = "Mods.GregTechCEuTerraria.AppliedEnergistics.Craft.Items";

	public static void RegisterAll()
	{
		Language.GetOrRegister(KeyTypeItems, () => "Items");
		Language.GetOrRegister(KeyTypeFluids, () => "Fluids");
		Language.GetOrRegister(StorageMENetwork, () => "ME Network Storage");
		Language.GetOrRegister(StorageExternal, () => "External Storage");

		Language.GetOrRegister(SelectItemFluid, () => "Select item/fluid");

		Language.GetOrRegister(TerminalNotConnected, () => "Not connected");
		Language.GetOrRegister(TerminalStorageDevices, () => "{0} storage devices");

		Language.GetOrRegister(CraftSubmitOk, () => "Crafting started");
		Language.GetOrRegister(CraftSubmitIncompletePlan, () => "Missing items - cannot craft");
		Language.GetOrRegister(CraftSubmitNoCpu, () => "No Crafting CPU available");
		Language.GetOrRegister(CraftSubmitCpuBusy, () => "Crafting CPU is busy");
		Language.GetOrRegister(CraftSubmitCpuOffline, () => "Crafting CPU is offline");
		Language.GetOrRegister(CraftSubmitCpuTooSmall, () => "Crafting CPU is too small for this job");
		Language.GetOrRegister(CraftSubmitMissing, () => "Missing {0}x {1}");
		Language.GetOrRegister(CraftSubmitMissingGeneric, () => "Missing an ingredient");

		Language.GetOrRegister(CraftDone, () => "[ME] {0} crafted {1}x {2} in {3} ({4} items processed)");
		Language.GetOrRegister(CraftDoneSomeone, () => "Someone");
		Language.GetOrRegister(CraftDoneItems, () => "items");
	}
}
