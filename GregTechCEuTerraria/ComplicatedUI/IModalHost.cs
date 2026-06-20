#nullable enable
namespace GregTechCEuTerraria.TerrariaCompat.UI;

public interface IModalHost
{
	void RequestClose();

	bool PinSupported { get; }

	bool Pinned { get; set; }
}
