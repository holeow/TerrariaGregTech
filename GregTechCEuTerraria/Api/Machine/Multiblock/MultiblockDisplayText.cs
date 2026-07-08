#nullable enable
using System;
using System.Collections.Generic;
using GregTechCEuTerraria.Api.Capability;
using GregTechCEuTerraria.Api.Capability.Recipe;
using GregTechCEuTerraria.Api.Machine.Trait;
using GregTechCEuTerraria.Api.Recipe;
using GregTechCEuTerraria.Api.Recipe.Chance.Boost;
using GregTechCEuTerraria.Api.Recipe.Ingredient;
using GregTechCEuTerraria.Common.Energy;
using Terraria;
using Terraria.Localization;
using RecipeContent = GregTechCEuTerraria.Api.Recipe.Content.Content;

namespace GregTechCEuTerraria.Api.Machine.Multiblock;

public static class MultiblockDisplayText
{
	private const string EmptyComponent = "";

	public static Func<string, string> FailReasonResolver { get; set; } = id => id;

	public static Func<string, string> KeyResolver { get; set; } = k => k;

	public static string? UnformedHint { get; set; }

	internal static string Tr(string key, params object[] args)
	{
		LocalizedText text = Language.GetText(KeyResolver(key));
		if (args.Length == 0) return text.Value;
		try { return text.Format(args); }
		catch { return text.Value; }
	}

	public static Builder Create(List<string> textList, bool isStructureFormed) =>
		Create(textList, isStructureFormed, true);

	public static Builder Create(List<string> textList, bool isStructureFormed,
		bool showIncompleteStructureWarning) =>
		new(textList, isStructureFormed, showIncompleteStructureWarning);

	public sealed class Builder
	{
		private readonly List<string> _textList;
		private readonly bool _isStructureFormed;

		private bool _isWorkingEnabled;
		private bool _isActive;

		private string _idlingKey  = "gtceu.multiblock.idling";
		private string _pausedKey  = "gtceu.multiblock.work_paused";
		private string _runningKey = "gtceu.multiblock.running";

		internal Builder(List<string> textList, bool isStructureFormed, bool showIncompleteStructureWarning)
		{
			_textList = textList;
			_isStructureFormed = isStructureFormed;

			if (!isStructureFormed && showIncompleteStructureWarning)
			{
				textList.Add(Tr("gtceu.multiblock.invalid_structure"));
				if (UnformedHint != null)
					textList.Add(UnformedHint);
			}
		}

		public Builder SetWorkingStatus(bool isWorkingEnabled, bool isActive)
		{
			_isWorkingEnabled = isWorkingEnabled;
			_isActive = isActive;
			return this;
		}

		public Builder SetWorkingStatusKeys(string? idlingKey, string? pausedKey, string? runningKey)
		{
			if (idlingKey  is not null) _idlingKey  = idlingKey;
			if (pausedKey  is not null) _pausedKey  = pausedKey;
			if (runningKey is not null) _runningKey = runningKey;
			return this;
		}

		public Builder AddEnergyUsageLine(IEnergyContainer? energyContainer)
		{
			if (!_isStructureFormed) return this;
			if (energyContainer != null && energyContainer.EnergyCapacity > 0)
			{
				long maxVoltage = Math.Max(energyContainer.InputVoltage, energyContainer.OutputVoltage);
				string energyFormatted = maxVoltage.ToString("N0");
				int voltageTier = VoltageTiers.FloorTierByVoltage(maxVoltage);
				string voltageName = VoltageTiers.ShortName((VoltageTier)voltageTier);
				_textList.Add(Tr("gtceu.multiblock.max_energy_per_tick",
					energyFormatted, voltageName));
			}
			return this;
		}

		public Builder AddEnergyTierLine(int tier)
		{
			if (!_isStructureFormed) return this;
			if (tier < (int)VoltageTier.ULV || tier > (int)VoltageTier.MAX) return this;
			string voltageName = VoltageTiers.ShortName((VoltageTier)tier);
			_textList.Add(Tr("gtceu.multiblock.max_recipe_tier", voltageName));
			return this;
		}

		public Builder AddEnergyUsageExactLine(long energyUsage)
		{
			if (!_isStructureFormed) return this;
			if (energyUsage > 0)
			{
				string energyFormatted = energyUsage.ToString("N0");
				string voltageName = VoltageTiers.ShortName(
					(VoltageTier)VoltageTiers.TierByVoltage(energyUsage));
				_textList.Add(Tr("gtceu.multiblock.energy_consumption",
					energyFormatted, voltageName));
			}
			return this;
		}

		public Builder AddEnergyProductionLine(long maxVoltage, long recipeEUt)
		{
			if (!_isStructureFormed) return this;
			if (maxVoltage != 0 && maxVoltage >= -recipeEUt)
			{
				string energyFormatted = maxVoltage.ToString("N0");
				string voltageName = VoltageTiers.ShortName(
					(VoltageTier)VoltageTiers.FloorTierByVoltage(maxVoltage));
				_textList.Add(Tr("gtceu.multiblock.max_energy_per_tick",
					energyFormatted, voltageName));
			}
			return this;
		}

		public Builder AddEnergyProductionAmpsLine(long maxVoltage, int amperage)
		{
			if (!_isStructureFormed) return this;
			if (maxVoltage != 0 && amperage != 0)
			{
				string energyFormatted = maxVoltage.ToString("N0");
				string voltageName = VoltageTiers.ShortName(
					(VoltageTier)VoltageTiers.FloorTierByVoltage(maxVoltage));
				_textList.Add(Tr("gtceu.multiblock.max_energy_per_tick_amps",
					energyFormatted, amperage, voltageName));
			}
			return this;
		}

		public Builder AddComputationUsageLine(int maxCWUt)
		{
			if (!_isStructureFormed) return this;
			if (maxCWUt > 0)
			{
				_textList.Add(Tr("gtceu.multiblock.computation.max",
					maxCWUt.ToString("N0")));
			}
			return this;
		}

		public Builder AddComputationUsageExactLine(int currentCWUt)
		{
			if (!_isStructureFormed) return this;
			if (_isActive && currentCWUt > 0)
			{
				_textList.Add(Tr("gtceu.multiblock.computation.usage",
					currentCWUt.ToString("N0") + " CWU/t"));
			}
			return this;
		}

		public Builder AddWorkingStatusLine(RecipeLogic? recipeLogic = null)
		{
			if (!_isStructureFormed) return this;
			if (!_isWorkingEnabled) return AddWorkPausedLine(false);
			if (recipeLogic != null && recipeLogic.IsWaiting()
				&& recipeLogic.GetWaitingReason() is { } reason)
				return AddWaitingLine(reason);
			if (_isActive) return AddRunningPerfectlyLine(false);
			return AddIdlingLine(false);
		}

		private Builder AddWaitingLine(string reason)
		{
			if (!_isStructureFormed) return this;
			_textList.Add("[c/FFCC44:Waiting:] " + FailReasonResolver(reason));
			return this;
		}

		public Builder AddWorkPausedLine(bool checkState)
		{
			if (!_isStructureFormed) return this;
			if (!checkState || !_isWorkingEnabled)
				_textList.Add(Tr(_pausedKey));
			return this;
		}

		public Builder AddRunningPerfectlyLine(bool checkState)
		{
			if (!_isStructureFormed) return this;
			if (!checkState || _isActive)
				_textList.Add(Tr(_runningKey));
			return this;
		}

		public Builder AddIdlingLine(bool checkState)
		{
			if (!_isStructureFormed) return this;
			if (!checkState || (_isWorkingEnabled && !_isActive))
				_textList.Add(Tr(_idlingKey));
			return this;
		}

		public Builder AddProgressLineOnlyPercent(double progressPercent)
		{
			if (!_isStructureFormed || !_isActive) return this;
			int currentProgress = (int)(progressPercent * 100);
			_textList.Add(Tr("gtceu.multiblock.progress_percent", currentProgress));
			return this;
		}

		public Builder AddProgressLine(RecipeLogic recipeLogic)
		{
			if (recipeLogic.HasCustomProgressLine())
				return AddCustomProgressLine(recipeLogic);
			return AddProgressLine(recipeLogic.GetProgress(), recipeLogic.GetMaxProgress(),
				recipeLogic.GetProgressPercent());
		}

		public Builder AddProgressLine(double currentDuration, double maxDuration, double progressPercent)
		{
			if (!_isStructureFormed || !_isActive) return this;
			int currentProgress = (int)(progressPercent * 100);
			double currentInSec = currentDuration / 20.0;
			double maxInSec     = maxDuration     / 20.0;
			_textList.Add(Tr("gtceu.multiblock.progress",
				currentInSec.ToString("0.00"),
				maxInSec.ToString("0.00"),
				currentProgress));
			return this;
		}

		public Builder AddCustomProgressLine(RecipeLogic recipeLogic)
		{
			if (!_isStructureFormed || !_isActive) return this;
			string? line = recipeLogic.GetCustomProgressLine();
			if (line is not null) _textList.Add(line);
			return this;
		}

		public Builder AddRecipeFailReasonLine(RecipeLogic recipeLogic)
		{
			if (!_isStructureFormed || !recipeLogic.IsIdle()) return this;
			var reasons = recipeLogic.GetFailureReasons();
			if (reasons.Count == 0) return this;

			HashSet<string>? seen = null;
			bool headerAdded = false;
			foreach (var reason in reasons)
			{
				string text = FailReasonResolver(reason);
				seen ??= new HashSet<string>();
				if (!seen.Add(text)) continue;
				if (!headerAdded)
				{
					_textList.Add(Tr("gtceu.recipe_logic.setup_fail"));
					headerAdded = true;
				}
				_textList.Add(" - " + text);
			}
			return this;
		}

		public Builder AddBatchModeLine(bool batchEnabled, int batchAmount)
		{
			if (batchEnabled && batchAmount > 0)
			{
				_textList.Add(Tr("gtceu.multiblock.batch_enabled",
					batchAmount.ToString("N0")));
			}
			return this;
		}

		public Builder AddSubtickParallelsLine(int subtickParallels)
		{
			if (subtickParallels > 1)
			{
				_textList.Add(Tr("gtceu.multiblock.subtick_parallels",
					subtickParallels.ToString("N0")));
			}
			return this;
		}

		public Builder AddTotalRunsLine(int totalRuns)
		{
			if (totalRuns > 1)
			{
				_textList.Add(Tr("gtceu.multiblock.total_runs",
					totalRuns.ToString("N0")));
			}
			return this;
		}

		public Builder AddOutputLines(GTRecipe? recipe)
		{
			if (!_isStructureFormed || !_isActive) return this;
			if (recipe is null) return this;

			int recipeTier = RecipeHelper.GetPreOCRecipeEuTier(recipe);
			int chanceTier = recipeTier + recipe.OcLevel;
			var function   = recipe.RecipeType.ChanceFunction;
			double maxDurationSec = recipe.Duration / 20.0;
			var itemOutputs  = recipe.GetOutputContents(ItemRecipeCapability.CAP);
			var fluidOutputs = recipe.GetOutputContents(FluidRecipeCapability.CAP);
			int runs = recipe.GetTotalRuns();

			foreach (var item in itemOutputs)
			{
				bool rounded = false;
				Terraria.Item stack;
				int count = 0;
				double countD = 1;
				string displaycount;
				if (item.Payload is IntProviderIngredient provider)
				{
					rounded = true;
					var maxStack = provider.GetMaxSizeStack();
					if (maxStack.Count == 0) continue;
					stack = maxStack[0];
					displaycount = Tr("gtceu.gui.content.range",
						provider.CountProvider.GetMinValue(),
						provider.CountProvider.GetMaxValue());
					if (item.Chance < item.MaxChance)
					{
						countD = countD * runs * function.GetBoostedChance(item, recipeTier, chanceTier) / item.MaxChance;
					}
					countD = countD * provider.GetMidRoll();
				}
				else
				{
					var stacks = ItemRecipeCapability.CAP.Of(item.Payload).GetItems();
					if (stacks.Count == 0) continue;
					stack = stacks[0];
					count = stack.stack;
					countD *= count;
					if (item.Chance < item.MaxChance)
					{
						rounded = true;
						countD = countD * runs * function.GetBoostedChance(item, recipeTier, chanceTier) / item.MaxChance;
					}
					count = Math.Max(1, (int)Math.Round(countD));
					displaycount = count.ToString();
				}
				string itemName = Util.TerrariaText.ItemName(stack.type);
				if (countD < maxDurationSec)
				{
					string key = "gtceu.multiblock.output_line." + (rounded ? "2" : "0");
					_textList.Add(Tr(key, itemName, displaycount,
						(maxDurationSec / countD).ToString("0.00")));
				}
				else
				{
					string key = "gtceu.multiblock.output_line." + (rounded ? "3" : "1");
					_textList.Add(Tr(key, itemName, displaycount,
						(countD / maxDurationSec).ToString("0.00")));
				}
			}

			foreach (var fluid in fluidOutputs)
			{
				bool rounded = false;
				Api.Fluids.FluidStack stack;
				int amount;
				double amountD = 1;
				string displaycount;
				if (fluid.Payload is IntProviderFluidIngredient provider)
				{
					rounded = true;
					var maxStack = provider.GetMaxSizeFluid();
					if (maxStack.Length == 0) continue;
					stack = maxStack[0];
					displaycount = Tr("gtceu.gui.content.range",
						provider.CountProvider.GetMinValue(),
						provider.CountProvider.GetMaxValue());
					if (fluid.Chance < fluid.MaxChance)
					{
						amountD = amountD * runs * function.GetBoostedChance(fluid, recipeTier, chanceTier) / fluid.MaxChance;
					}
					amountD = amountD * provider.GetMidRoll();
				}
				else
				{
					var stacks = FluidRecipeCapability.CAP.Of(fluid.Payload).GetStacks();
					if (stacks.Length == 0) continue;
					stack = stacks[0];
					amount = stack.Amount;
					amountD *= amount;
					if (fluid.Chance < fluid.MaxChance)
					{
						rounded = true;
						amountD = amountD * runs * function.GetBoostedChance(fluid, recipeTier, chanceTier) / fluid.MaxChance;
					}
					amount = Math.Max(1, (int)Math.Round(amountD));
					displaycount = amount.ToString();
				}
				string fluidName = stack.Type?.DisplayName ?? "?";
				if (amountD < maxDurationSec)
				{
					string key = "gtceu.multiblock.output_line." + (rounded ? "2" : "0");
					_textList.Add(Tr(key, fluidName, displaycount,
						(maxDurationSec / amountD).ToString("0.00")));
				}
				else
				{
					string key = "gtceu.multiblock.output_line." + (rounded ? "3" : "1");
					_textList.Add(Tr(key, fluidName, displaycount,
						(amountD / maxDurationSec).ToString("0.00")));
				}
			}
			return this;
		}

		public Builder AddMachineModeLine(GTRecipeType recipeType, bool hasMultipleModes)
		{
			if (!_isStructureFormed || !hasMultipleModes) return this;
			string modeName = Tr($"gtceu.{recipeType.RegistryName}");
			_textList.Add(Tr("gtceu.gui.machinemode", modeName));
			return this;
		}

		public Builder AddParallelsLine(int numParallels) => AddParallelsLine(numParallels, false);

		public Builder AddParallelsLine(int numParallels, bool exact)
		{
			if (!_isStructureFormed) return this;
			if (numParallels > 1)
			{
				string key = exact ? "gtceu.multiblock.parallel.exact" : "gtceu.multiblock.parallel";
				_textList.Add(Tr(key, numParallels.ToString("N0")));
			}
			return this;
		}

		public Builder AddLowPowerLine(bool isLowPower)
		{
			if (!_isStructureFormed) return this;
			if (isLowPower)
				_textList.Add(Tr("gtceu.multiblock.not_enough_energy"));
			return this;
		}

		public Builder AddLowComputationLine(bool isLowComputation)
		{
			if (!_isStructureFormed) return this;
			if (isLowComputation)
				_textList.Add(Tr("gtceu.multiblock.computation.not_enough_computation"));
			return this;
		}

		public Builder AddLowDynamoTierLine(bool isTooLow)
		{
			if (!_isStructureFormed) return this;
			if (isTooLow)
				_textList.Add(Tr("gtceu.multiblock.not_enough_energy_output"));
			return this;
		}

		public Builder AddMaintenanceProblemLines(byte maintenanceProblems)
		{
			if (!_isStructureFormed) return this;
			if (maintenanceProblems <= 0b111111 && maintenanceProblems > 0)
			{
				AddMaintenanceProblemHeader();
				if ((maintenanceProblems        & 1) == 0)
					_textList.Add(Tr("gtceu.multiblock.universal.problem.wrench"));
				if (((maintenanceProblems >> 1) & 1) == 0)
					_textList.Add(Tr("gtceu.multiblock.universal.problem.screwdriver"));
				if (((maintenanceProblems >> 2) & 1) == 0)
					_textList.Add(Tr("gtceu.multiblock.universal.problem.soft_mallet"));
				if (((maintenanceProblems >> 3) & 1) == 0)
					_textList.Add(Tr("gtceu.multiblock.universal.problem.hard_hammer"));
				if (((maintenanceProblems >> 4) & 1) == 0)
					_textList.Add(Tr("gtceu.multiblock.universal.problem.wire_cutter"));
				if (((maintenanceProblems >> 5) & 1) == 0)
					_textList.Add(Tr("gtceu.multiblock.universal.problem.crowbar"));
			}
			return this;
		}

		private void AddMaintenanceProblemHeader() =>
			_textList.Add(Tr("gtceu.multiblock.universal.has_problems"));

		public Builder AddMufflerObstructedLine(bool isObstructed)
		{
			if (!_isStructureFormed) return this;
			if (isObstructed)
			{
				_textList.Add(Tr("gtceu.multiblock.universal.muffler_obstructed"));
				_textList.Add(Tr("gtceu.multiblock.universal.muffler_obstructed.tooltip"));
			}
			return this;
		}

		public Builder AddFuelNeededLine(string? fuelName, int previousRecipeDuration)
		{
			if (!_isStructureFormed || !_isActive || fuelName is null) return this;
			_textList.Add(Tr("gtceu.multiblock.turbine.fuel_needed",
				fuelName, previousRecipeDuration.ToString("N0")));
			return this;
		}

		public Builder AddEmptyLine()
		{
			_textList.Add(EmptyComponent);
			return this;
		}

		public Builder AddCustom(Action<List<string>> customConsumer)
		{
			customConsumer(_textList);
			return this;
		}

		public Builder AddCurrentEnergyProductionLine(long euOutput)
		{
			_textList.Add(Tr("gtceu.multiblock.turbine.energy_per_tick_maxed",
				euOutput.ToString("N0")));
			return this;
		}
	}
}
