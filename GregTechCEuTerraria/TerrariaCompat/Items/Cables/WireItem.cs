#nullable enable
using GregTechCEuTerraria.Common.Energy;
using GregTechCEuTerraria.Api.Capability;
using GregTechCEuTerraria.TerrariaCompat.Items.Pipes;
using GregTechCEuTerraria.TerrariaCompat.Machine.Rendering;
using GregTechCEuTerraria.TerrariaCompat.Pipelike.Cable;
using GregTechCEuTerraria.Common.Materials;
using GregTechCEuTerraria.Api.Data.Chemical.Material;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace GregTechCEuTerraria.TerrariaCompat.Items.Cables;

public sealed class WireItem : ModItem, ITextureWarmUp
{
	private readonly string? _id;
	[CloneByReference] private readonly Material? _material;
	private readonly byte _wireSize;
	private readonly bool _insulated;
	private int _removeCooldown;

	public WireItem() { }
	public WireItem(string id, Material material, byte wireSize, bool insulated)
	{
		_id = id;
		_material = material;
		_wireSize = wireSize;
		_insulated = insulated;
	}

	public override string Name => _id ?? nameof(WireItem);

	internal string? MaterialId => _material?.Id;
	internal byte WireSize => _wireSize;
	internal bool Insulated => _insulated;

	public override string Texture => "GregTechCEuTerraria/Content/Textures/block/material_sets/dull/wire_end";
	protected override bool CloneNewInstances => true;
	public override bool IsLoadingEnabled(Mod mod) => _material != null;

	public override void SetDefaults()
	{
		Item.maxStack = Item.CommonMaxStack;
		Item.width = 32;
		Item.height = 32;
		Item.useTime = 2;
		Item.useAnimation = 6;
		Item.useStyle = ItemUseStyleID.Swing;
		Item.autoReuse = true;
		Item.consumable = false;  // manual stack management
		Item.rare = ItemRarityID.White;
		Item.UseSound = null;
	}

	public override void ModifyTooltips(System.Collections.Generic.List<TooltipLine> tooltips)
	{
		base.ModifyTooltips(tooltips);
		if (_material is null) return;
		var cell = BuildCell();
		long voltage = VoltageTiers.Voltage(cell.Voltage);

		tooltips.Add(new TooltipLine(Mod, "WireTier",
			$"{VoltageTiers.ShortName(cell.Voltage)} - {voltage:N0} EU/t"));
		tooltips.Add(new TooltipLine(Mod, "WireAmp",
			$"{cell.TotalAmperage}A ({cell.BaseAmperage} x {cell.WireSize})"));
		tooltips.Add(new TooltipLine(Mod, "WireLoss",
			$"Loss: {cell.LossPerAmp} EU per amp per cable"));
		Tools.GregTechMultitool.AppendHint(Mod, tooltips);
	}

	public override bool? UseItem(Player player)
	{
		if (_material is null) return null;
		if (Main.myPlayer != player.whoAmI) return null;

		int x = (int)(Main.MouseWorld.X / 16);
		int y = (int)(Main.MouseWorld.Y / 16);
		if (x <= 0 || x >= Main.maxTilesX - 1 || y <= 0 || y >= Main.maxTilesY - 1)
			return false;
		if (Item.stack <= 0) return false;

		if (!CableLayerHandle.Instance.TryPlace(BuildCell(), x, y, player))
			return false;
		Terraria.Audio.SoundEngine.PlaySound(SoundID.Item50, new Microsoft.Xna.Framework.Vector2(x * 16, y * 16));
		Item.stack--;
		return true;
	}

	public override void HoldItem(Player player)
	{
		EnsureTextureBaked();
		if (Main.myPlayer != player.whoAmI) return;
		if (_material is null) return;
		string label = _insulated ? "Cable" : "Wire";
		PipeHeldItemBehavior.Tick(player, label, CableLayerHandle.Instance,
			WireBody, ref _removeCooldown, Item.useTime);
	}

	void ITextureWarmUp.WarmUpTexture() => EnsureTextureBaked();

	private void EnsureTextureBaked()
	{
		Color core = MaterialColor();
		float dotScale = DotScaleForWireSize(_wireSize);
		if (_insulated)
		{
			float coreScale = System.Math.Max(0.15f, dotScale - 0.12f);
			ItemIconBaker.Install(Item.type,
				new IconLayer(Texture, CableRenderer.JacketColor(core), dotScale),
				new IconLayer(Texture, core, coreScale));
		}
		else
		{
			ItemIconBaker.Install(Item.type, new IconLayer(Texture, core, dotScale));
		}
	}

	private static float DotScaleForWireSize(byte wireSize) => wireSize switch
	{
		1  => 0.35f,
		2  => 0.50f,
		4  => 0.65f,
		8  => 0.80f,
		16 => 1.00f,
		_  => 0.50f,
	};

	private static string? WireBody(int x, int y)
	{
		var cell = CableLayerSystem.Cables.CellAt(x, y);
		if (cell is null) return null;

		var net = TerrariaCompat.Pipelike.Cable.EnergyNetSystem.NetAt(x, y);

		string kind = cell.Value.Insulated ? "Cable" : "Wire";
		string sizeWord = WireSizeWord(cell.Value.WireSize);
		long voltage = VoltageTiers.Voltage(cell.Value.Voltage);
		string cellLine = $"{PipeHeldItemBehavior.HumanizeMaterial(cell.Value.MaterialId)} {(cell.Value.WireSize > 1 ? sizeWord + " " : "")}{kind}";
		string electrical = $"{VoltageTiers.ShortName(cell.Value.Voltage)} {voltage:N0} EU/t   {cell.Value.TotalAmperage}A   loss {cell.Value.LossPerAmp}/A";

		string networkLine = net != null
			? $"Network: {net.Cells.Count} cables   {VoltageTiers.ShortName(net.EffectiveTier)}   cap {net.PerTickCapacity:N0} EU/t ({net.MaxAmperage}A)"
			: "Network: not initialized";

		string endpointsLine = net != null
			? $"Endpoints: {net.Producers.Count} producers, {net.Consumers.Count} consumers"
			: "Endpoints: -";

		string throughputLine;
		if (net != null)
		{
			var (ex, de) = TerrariaCompat.Pipelike.Cable.EnergyNetSystem.GetThroughput(net);
			throughputLine = $"Throughput: {ex:N0} / {de:N0} EU/t   loss {(ex - de):N0}";
		}
		else
		{
			throughputLine = "Throughput: -";
		}

		string body = string.Join("\n", cellLine, electrical, networkLine, endpointsLine, throughputLine);

		if (net is not null)
		{
			float lossPct = net.GetCableLossPercent(x, y);
			if (lossPct >= 0.5f)
			{
				int pct = (int)(lossPct * 100);
				body += "\n" +
					$"[c/FF5555:! High-loss path: ~{pct}% of source voltage already lost here]\n" +
					$"[c/FF8888:Energy delivered downstream of this cable is heavily reduced.]\n" +
					$"[c/FF8888:Shorten the cable run, use a higher-tier wire (lower loss), or insulate it.]";
			}
		}

		return body;
	}

	public static bool CutCableAt(Player player, int x, int y) =>
		CableLayerHandle.Instance.CutAt(x, y, player);

	internal CableCell BuildCell() => BuildCellWithSize(_wireSize);

	internal CableCell BuildCellWithSize(byte wireSize)
	{
		var mat = _material!;
		var tier = TryParseTier(mat.CableTier) ?? VoltageTier.ULV;
		int baseAmp = mat.CableAmperage ?? 1;
		return new CableCell(mat.Id, wireSize, _insulated, tier, baseAmp, ComputeLoss(mat, wireSize));
	}

	private int ComputeLoss(Material mat, byte wireSize)
	{
		int lossMult = _insulated ? 1 : (wireSize <= 2 ? 2 : 3);
		int baseLoss = mat.CableLoss ?? 0;
		bool superconductor = mat.CableIsSuperconductor ?? false;
		if (!superconductor && baseLoss == 0)
			return (int)(0.75 * lossMult);
		return baseLoss * lossMult;
	}

	private static VoltageTier? TryParseTier(string? name) =>
		System.Enum.TryParse<VoltageTier>(name, ignoreCase: false, out var t) ? t : null;

	private Color MaterialColor()
	{
		uint c = _material?.Color ?? 0xFFFFFFu;
		return new Color((byte)((c >> 16) & 0xFF), (byte)((c >> 8) & 0xFF), (byte)(c & 0xFF));
	}

	public static string WireSizeWord(byte wireSize) => wireSize switch
	{
		1  => "single",
		2  => "double",
		4  => "quadruple",
		8  => "octal",
		16 => "hex",
		_  => "single",
	};
}
