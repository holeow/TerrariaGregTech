#nullable enable
namespace GregTechCEuTerraria.AppliedEnergistics.Api.Stacks;

public interface IWrappedGenericStack
{
	AEKey? What { get; }
	long Amount { get; }
}
