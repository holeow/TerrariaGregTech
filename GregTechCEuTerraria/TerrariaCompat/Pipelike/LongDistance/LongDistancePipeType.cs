#nullable enable
namespace GregTechCEuTerraria.TerrariaCompat.Pipelike.LongDistance;

public enum LongDistancePipeType : byte
{
	Item  = 0,
	Fluid = 1,
}

public static class LongDistancePipeTypeExtensions
{
	public static int NodeMark(this LongDistancePipeType type) => (int)type + 1;

	public static int MinLength(this LongDistancePipeType type)
	{
		var cfg = Config.GTConfig.Instance;
		return type == LongDistancePipeType.Item
			? cfg.LdItemPipeMinDistance
			: cfg.LdFluidPipeMinDistance;
	}
}
