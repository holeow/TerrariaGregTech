#nullable enable
using System;
using GregTechCEuTerraria.Common.Energy;

namespace GregTechCEuTerraria.TerrariaCompat.Machine.Multiblock;

public abstract class TieredWorkableElectricMultiblockMachine : WorkableElectricMultiblockMachine
{
	public int HardwareTier { get; private set; }

	public int CurrentOverclockTier { get; protected set; }

	protected TieredWorkableElectricMultiblockMachine() : base() { }

	public void BindHardwareTier(int tier)
	{
		HardwareTier = tier;
		CurrentOverclockTier = tier;
	}

	public override int MinOverclockTier => 0;

	public override void SetOverclockTier(int tier)
	{
		if (!IsServer) return;
		if (tier < MinOverclockTier || tier > MaxOverclockTier) return;
		CurrentOverclockTier = tier;
		Recipe.MarkLastRecipeDirty();
	}

	public override long OverclockVoltage =>
		Math.Min(VoltageTiers.Voltage((VoltageTier)CurrentOverclockTier), base.OverclockVoltage);

	public override int GetTier() => Math.Min(HardwareTier, base.GetTier());

	public override long GetMaxVoltage() =>
		Math.Min(VoltageTiers.Voltage((VoltageTier)HardwareTier), base.GetMaxVoltage());
}
