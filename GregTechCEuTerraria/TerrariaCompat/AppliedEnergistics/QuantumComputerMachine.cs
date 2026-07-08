#nullable enable
using System.Collections.Generic;
using GregTechCEuTerraria.AppliedEnergistics.Api.Config;
using GregTechCEuTerraria.AppliedEnergistics.Api.Networking.Security;
using GregTechCEuTerraria.AppliedEnergistics.Api.Stacks;
using GregTechCEuTerraria.AppliedEnergistics.Api.Storage;
using GregTechCEuTerraria.TerrariaCompat.AppliedEnergistics.Crafting;
using GregTechCEuTerraria.TerrariaCompat.Machine;
using GregTechCEuTerraria.TerrariaCompat.Pipelike.Me;
using Terraria.ModLoader.IO;

namespace GregTechCEuTerraria.TerrariaCompat.AppliedEnergistics;

public sealed class QuantumComputerMachine : MetaMachine, IMeStorageDevice, ICraftingCpuHost
{
	private const long Bytes = 1_000_000;

	private readonly CraftingCpuLogic _logic;
	private readonly CpuSink _sink;
	private GenericStack? _displayOutput;

	public QuantumComputerMachine()
	{
		_logic = new CraftingCpuLogic(this);
		_sink = new CpuSink(_logic);
	}

	protected override string Label => "Quantum Computer";
	public override bool SupportsCovers => false;

	public CraftingCpuLogic Logic => _logic;
	public GenericStack? DisplayOutput => _displayOutput;

	public MEStorage GetMeStorage() => _sink;
	public int StoragePriority => int.MaxValue;

	public override bool IsActive => Logic.HasJob;

	public bool IsOnline => Network != null;
	public long AvailableStorage => Bytes;
	public int CoProcessors => 63;
	public MeNetwork? Network => MeNetworkSystem.NetAdjacentTo(this);
	public IActionSource Src => IActionSource.Empty();
	public void UpdateOutput(GenericStack? what) => _displayOutput = what;
	public void MarkDirty() { }

	public void NotifyJobStatus(ExecutingCraftingJob job, CraftingJobStatus status)
	{
		if (job.PlayerId is not int pid) return;
		var what = job.FinalOutput.What;
		if (what == null) return;
		var st = status switch
		{
			CraftingJobStatus.Finished => PendingCraftingJobs.Status.FINISHED,
			CraftingJobStatus.Cancelled => PendingCraftingJobs.Status.CANCELLED,
			_ => PendingCraftingJobs.Status.STARTED,
		};
		Net.MeCraftJobStatusPacket.Notify(job.Link.CraftId, what, job.FinalOutput.Amount,
			job.RemainingAmount, st, pid);
	}

	protected override void OnTick()
	{
		if (!IsServer) return;
		_logic.TickCraftingLogic();
	}

	public override void SaveData(TagCompound tag)
	{
		base.SaveData(tag);
		var cpu = new TagCompound();
		_logic.WriteToNBT(cpu);
		tag["cpu"] = cpu;
		if (_displayOutput != null) tag["disp"] = GenericStack.WriteTag(_displayOutput);
	}

	public override void LoadData(TagCompound tag)
	{
		base.LoadData(tag);
		if (tag.ContainsKey("cpu")) _logic.ReadFromNBT(tag.GetCompound("cpu"));
		_displayOutput = tag.ContainsKey("disp") ? GenericStack.ReadTag(tag.GetCompound("disp")) : null;
	}

	public override void AppendTooltip(List<string> lines)
	{
		base.AppendTooltip(lines);
		lines.Add(Network == null ? "[c/FF8888:Not connected]" : "ME Network: connected");
		lines.Add($"Storage: {Bytes:N0} bytes   Parallel: {CoProcessors + 1}");
		if (_displayOutput != null)
			lines.Add($"Crafting: {_displayOutput.Amount:N0}x {_displayOutput.What.GetDisplayName()}");
		else
			lines.Add("Idle");
	}

	private sealed class CpuSink : MEStorage
	{
		private readonly CraftingCpuLogic _logic;
		public CpuSink(CraftingCpuLogic logic) => _logic = logic;
		public string GetDescription() => "Quantum Computer";
		public bool IsPreferredStorageFor(AEKey what, IActionSource source) => true;
		public long Insert(AEKey what, long amount, Actionable mode, IActionSource source)
			=> _logic.Insert(what, amount, mode);
	}
}
