#nullable enable
using Terraria;
using Terraria.ModLoader.IO;
using GregTechCEuTerraria.Api;
using GregTechCEuTerraria.Api.Capability;
using GregTechCEuTerraria.Api.Machine;
using GregTechCEuTerraria.Api.Machine.Trait;
using GregTechCEuTerraria.TerrariaCompat.Machine;

namespace GregTechCEuTerraria.Common.Machine.Trait;

public sealed class AutoOutputTrait : MachineTrait
{
	public static readonly MachineTraitType<AutoOutputTrait> TYPE = new(allowMultipleInstances: false);
	public override MachineTraitType TraitType => TYPE;

	private readonly IItemHandler?  _itemHandler;
	private readonly IFluidHandler? _fluidHandler;
	private readonly int _itemSlotStart;
	private readonly int _itemSlotCount;
	private readonly int _fluidTankStart;
	private readonly int _fluidTankCount;

	private IODirection _itemOutputDirection  = IODirection.None;
	private IODirection _fluidOutputDirection = IODirection.None;
	private bool _autoOutputItems = true;
	private bool _autoOutputFluids = true;
	private bool _allowItemInputFromOutputSide = true;
	private bool _allowFluidInputFromOutputSide = true;
	private int  _ticksPerCycle = 5;

	private System.Func<IODirection, bool> _itemOutputDirectionValidator  = _ => true;
	private System.Func<IODirection, bool> _fluidOutputDirectionValidator = _ => true;

	private TickableSubscription? _itemOutputSub;
	private TickableSubscription? _fluidOutputSub;

	public AutoOutputTrait(int itemSlotStart, int itemSlotCount, int fluidTankStart, int fluidTankCount)
	{
		_itemSlotStart  = itemSlotStart;
		_itemSlotCount  = itemSlotCount;
		_fluidTankStart = fluidTankStart;
		_fluidTankCount = fluidTankCount;
	}

	private AutoOutputTrait(IItemHandler? itemHandler, IFluidHandler? fluidHandler)
	{
		_itemHandler  = itemHandler;
		_fluidHandler = fluidHandler;
	}

	public static AutoOutputTrait OfItems(IItemHandler  handler) => new(handler, null);
	public static AutoOutputTrait OfFluids(IFluidHandler handler) => new(null,    handler);

	public static AutoOutputTrait OfItems(int slotStart, int slotCount)  => new(slotStart, slotCount, 0, 0);
	public static AutoOutputTrait OfFluids(int tankStart, int tankCount) => new(0, 0, tankStart, tankCount);

	public bool SupportsAutoOutputItems  => _itemHandler  is not null || _itemSlotCount  > 0;
	public bool SupportsAutoOutputFluids => _fluidHandler is not null || _fluidTankCount > 0;

	public IODirection ItemOutputDirection  => SupportsAutoOutputItems  ? _itemOutputDirection  : IODirection.None;
	public IODirection FluidOutputDirection => SupportsAutoOutputFluids ? _fluidOutputDirection : IODirection.None;
	public bool IsAutoOutputItems  => _autoOutputItems;
	public bool IsAutoOutputFluids => _autoOutputFluids;
	public bool AllowItemInputFromOutputSide  => _allowItemInputFromOutputSide;
	public bool AllowFluidInputFromOutputSide => _allowFluidInputFromOutputSide;

	public bool BlocksInputFrom(IODirection side, bool fluid)
	{
		if (side == IODirection.None) return false;
		return fluid
			? side == FluidOutputDirection && !_allowFluidInputFromOutputSide
			: side == ItemOutputDirection  && !_allowItemInputFromOutputSide;
	}

	public int TicksPerCycle { get => _ticksPerCycle; set => _ticksPerCycle = value; }

	public void SetAllowAutoOutputItems(bool allow)
	{
		if (!SupportsAutoOutputItems) return;
		_autoOutputItems = allow;
		UpdateItemOutputSubscription();
	}

	public void SetAllowAutoOutputFluids(bool allow)
	{
		if (!SupportsAutoOutputFluids) return;
		_autoOutputFluids = allow;
		UpdateFluidOutputSubscription();
	}

	public void SetItemOutputDirection(IODirection direction)
	{
		if (!SupportsAutoOutputItems) return;
		if (!_itemOutputDirectionValidator(direction)) return;
		_itemOutputDirection = direction;
		UpdateItemOutputSubscription();
	}

	public void SetFluidOutputDirection(IODirection direction)
	{
		if (!SupportsAutoOutputFluids) return;
		if (!_fluidOutputDirectionValidator(direction)) return;
		_fluidOutputDirection = direction;
		UpdateFluidOutputSubscription();
	}

	public void SetItemOutputDirectionValidator(System.Func<IODirection, bool> validator) =>
		_itemOutputDirectionValidator = validator;

	public void SetFluidOutputDirectionValidator(System.Func<IODirection, bool> validator) =>
		_fluidOutputDirectionValidator = validator;

	public void SetAllowItemInputFromOutputSide(bool allow)  => _allowItemInputFromOutputSide  = allow;
	public void SetAllowFluidInputFromOutputSide(bool allow) => _allowFluidInputFromOutputSide = allow;

	public override void OnMachineLoad()
	{
		base.OnMachineLoad();
		UpdateItemOutputSubscription();
		UpdateFluidOutputSubscription();
	}

	public override void OnMachineUnload()
	{
		if (_itemOutputSub != null)  { _itemOutputSub.Unsubscribe();  _itemOutputSub  = null; }
		if (_fluidOutputSub != null) { _fluidOutputSub.Unsubscribe(); _fluidOutputSub = null; }
		base.OnMachineUnload();
	}

	private bool ShouldKeepItemSubscription() =>
		SupportsAutoOutputItems && _autoOutputItems && _itemOutputDirection != IODirection.None;

	private bool ShouldKeepFluidSubscription() =>
		SupportsAutoOutputFluids && _autoOutputFluids && _fluidOutputDirection != IODirection.None;

	private void UpdateItemOutputSubscription()
	{
		if (ShouldKeepItemSubscription())
			_itemOutputSub = SubscribeServerTick(_itemOutputSub, AutoOutputItems);
		else if (_itemOutputSub != null)
		{
			_itemOutputSub.Unsubscribe();
			_itemOutputSub = null;
		}
	}

	private void UpdateFluidOutputSubscription()
	{
		if (ShouldKeepFluidSubscription())
			_fluidOutputSub = SubscribeServerTick(_fluidOutputSub, AutoOutputFluids);
		else if (_fluidOutputSub != null)
		{
			_fluidOutputSub.Unsubscribe();
			_fluidOutputSub = null;
		}
	}

	private uint OffsetTimer => (uint)(Main.GameUpdateCount + (uint)(BlockPos.X * 7 + BlockPos.Y * 13));

	private void AutoOutputItems()
	{
		if (OffsetTimer % (uint)TickScale.FromMcTicks(_ticksPerCycle) == 0 && _itemOutputDirection != IODirection.None)
		{
			if (_itemHandler is not null)
				AdjacentItemPush.Push(Machine, _itemHandler, 0, _itemHandler.SlotCount,
					maxPerSlot: 1, side: _itemOutputDirection);
			else
				AdjacentItemPush.Push(Machine, _itemSlotStart, _itemSlotCount,
					maxPerSlot: 1, side: _itemOutputDirection);
		}
		UpdateItemOutputSubscription();
	}

	private void AutoOutputFluids()
	{
		if (OffsetTimer % (uint)TickScale.FromMcTicks(_ticksPerCycle) == 0 && _fluidOutputDirection != IODirection.None)
		{
			if (_fluidHandler is not null)
				AdjacentFluidPush.Push(Machine, _fluidHandler, 0, _fluidHandler.TankCount,
					maxAmount: 1000, side: _fluidOutputDirection);
			else
				AdjacentFluidPush.Push(Machine, _fluidTankStart, _fluidTankCount,
					maxAmount: 1000, side: _fluidOutputDirection);
		}
		UpdateFluidOutputSubscription();
	}

	public override void Save(TagCompound tag)
	{
		tag["itemDir"]      = (byte)_itemOutputDirection;
		tag["fluidDir"]     = (byte)_fluidOutputDirection;
		tag["autoItem"]     = _autoOutputItems;
		tag["autoFluid"]    = _autoOutputFluids;
		tag["allowItemIn"]  = _allowItemInputFromOutputSide;
		tag["allowFluidIn"] = _allowFluidInputFromOutputSide;
	}

	public override void Load(TagCompound tag)
	{
		if (tag.ContainsKey("itemDir"))      _itemOutputDirection           = (IODirection)tag.GetByte("itemDir");
		if (tag.ContainsKey("fluidDir"))     _fluidOutputDirection          = (IODirection)tag.GetByte("fluidDir");
		if (tag.ContainsKey("autoItem"))     _autoOutputItems               = tag.GetBool("autoItem");
		if (tag.ContainsKey("autoFluid"))    _autoOutputFluids              = tag.GetBool("autoFluid");
		if (tag.ContainsKey("allowItemIn"))  _allowItemInputFromOutputSide  = tag.GetBool("allowItemIn");
		if (tag.ContainsKey("allowFluidIn")) _allowFluidInputFromOutputSide = tag.GetBool("allowFluidIn");
	}
}
