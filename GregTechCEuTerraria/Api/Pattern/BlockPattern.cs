#nullable enable
using System.Collections.Generic;
using GregTechCEuTerraria.Api.Machine.Feature.Multiblock;
using GregTechCEuTerraria.Api.Pattern.Error;
using GregTechCEuTerraria.TerrariaCompat.Machine;
using PredicatesNs = GregTechCEuTerraria.Api.Pattern.Predicates;

namespace GregTechCEuTerraria.Api.Pattern;

public class BlockPattern : IBlockPattern
{
	public readonly string[] Shape;
	public readonly IReadOnlyDictionary<char, TraceabilityPredicate> Predicates;

	public int ControllerCol { get; }
	public int ControllerRow { get; }

	public readonly int Width;
	public readonly int Height;

	private readonly bool _validateReachable;

	public BlockPattern(string[] shape, IReadOnlyDictionary<char, TraceabilityPredicate> predicates,
		bool validateReachable = true)
	{
		Shape = shape;
		_validateReachable = validateReachable;
		if (!predicates.ContainsKey('#'))
		{
			var merged = new Dictionary<char, TraceabilityPredicate>(predicates) { ['#'] = PredicatesNs.Any() };
			Predicates = merged;
		}
		else
		{
			Predicates = predicates;
		}
		Height = shape.Length;
		Width = Height > 0 ? shape[0].Length : 0;
		ValidateShape();
		var (cc, cr) = FindController();
		ControllerCol = cc;
		ControllerRow = cr;
	}

	public BlockPattern GetPreviewPattern() => this;

	private void ValidateShape()
	{
		for (int r = 0; r < Shape.Length; r++)
		{
			if (Shape[r].Length != Width)
				throw new System.ArgumentException(
					$"BlockPattern: row {r} has length {Shape[r].Length}, expected {Width} " +
					"(all rows must be the same width).");
		}
		if (_validateReachable)
			WarnUnreachableRequiredChars(Predicates, Shape);
	}

	public static void WarnUnreachableRequiredChars(
		IReadOnlyDictionary<char, TraceabilityPredicate> predicates, params string[] rows)
	{
		var used = new HashSet<char>();
		foreach (var row in rows)
			foreach (char ch in row)
				used.Add(ch);

		foreach (var kv in predicates)
		{
			if (used.Contains(kv.Key)) continue;
			foreach (var sp in kv.Value.Limited)
			{
				if (sp.MinCount <= 0) continue;
				Terraria.ModLoader.ModLoader.GetMod("GregTechCEuTerraria").Logger.Warn(
					$"BlockPattern: char '{kv.Key}' requires a minimum of {sp.MinCount} but never appears " +
					"in the shape this structure is impossible");
				break;
			}
		}
	}

	private (int Col, int Row) FindController()
	{
		(int Col, int Row)? hit = null;
		for (int r = 0; r < Height; r++)
		{
			for (int c = 0; c < Width; c++)
			{
				char ch = Shape[r][c];
				if (Predicates.TryGetValue(ch, out var p) && p.IsController)
				{
					if (hit is not null)
						throw new System.ArgumentException(
							$"BlockPattern: more than one controller cell ('{Shape[hit.Value.Row][hit.Value.Col]}' " +
							$"at {hit.Value} and '{ch}' at ({c}, {r})). Shapes must have exactly one.");
					hit = (c, r);
				}
			}
		}
		if (hit is null)
			throw new System.ArgumentException(
				"BlockPattern: no controller cell found. Exactly one shape char must map to a " +
				"predicate built via `Predicates.Controller(...)` (IsController=true).");
		return hit.Value;
	}

	public bool CheckPatternAt(MultiblockState state, bool savePredicate = false)
	{
		state.Clean();

		int originX = state.ControllerPosX - ControllerCol * 2;
		int originY = state.ControllerPosY - ControllerRow * 2;

		for (int row = 0; row < Height; row++)
		{
			state.LayerCount.Clear();
			for (int col = 0; col < Width; col++)
			{
				char ch = Shape[row][col];
				if (!Predicates.TryGetValue(ch, out var predicate))
				{
					state.SetError(new PatternStringError($"gtceu.multiblock.pattern.error.unmapped_char:{ch}"));
					return false;
				}
				int tileX = originX + col * 2;
				int tileY = originY + row * 2;
				state.SetError(null);
				if (!state.Update(tileX, tileY, predicate))
					return false;

				if (predicate.AddCache())
				{
					state.AddPosCache(tileX, tileY);
					if (savePredicate)
					{
						var preds = state.MatchContext.GetOrCreate("predicates",
							() => new Dictionary<long, TraceabilityPredicate>());
						preds[MultiblockState.PackPos(tileX, tileY)] = predicate;
					}
				}

				bool canPartShared = true;
				if (state.GetMachine() is IMultiPart part)
				{
					if (!predicate.IsAny())
					{
						bool partOwned = part.IsFormed()
							&& !part.HasController(state.ControllerPosX, state.ControllerPosY);
						if (partOwned && !part.CanShared())
						{
							canPartShared = false;
							state.SetError(new PatternStringError("multiblocked.pattern.error.share"));
						}
						else
						{
							var parts = state.MatchContext.GetOrCreate("parts", () => new HashSet<IMultiPart>());
							parts.Add(part);
						}
					}
				}

				if (!predicate.Test(state) || !canPartShared)
				{
					if (state.Error == null)
						state.SetError(new Error.PatternError());
					return false;
				}

				var ioMap = state.MatchContext.GetOrCreate("ioMap", () => new Dictionary<long, Capability.Recipe.IO>());
				ioMap[MultiblockState.PackPos(tileX, tileY)] = state.Io;
			}
		}

		foreach (var predicate in Predicates.Values)
		{
			foreach (var sp in predicate.Limited)
			{
				if (sp.MinCount > 0 && !state.GlobalCount.ContainsKey(sp))
					state.GlobalCount[sp] = 0;
			}
		}
		foreach (var kv in state.GlobalCount)
		{
			var sp = kv.Key;
			if (sp.MinCount != -1 && kv.Value < sp.MinCount)
			{
				state.SetError(new SinglePredicateError(sp, 1));
				return false;
			}
		}

		state.SetError(null);
		return true;
	}
}
