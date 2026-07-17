#nullable enable
using System.Collections.Generic;
using GregTechCEuTerraria.Api.Pattern.Error;
using GregTechCEuTerraria.TerrariaCompat.Machine.Multiblock;

namespace GregTechCEuTerraria.Api.Pattern;

public sealed class RepeatableBlockPattern : IBlockPattern
{
	public readonly RepeatableShape Shape;
	public readonly IReadOnlyDictionary<char, TraceabilityPredicate> Predicates;

	private readonly Dictionary<(int v, int h), BlockPattern> _byPair = new();

	public RepeatableBlockPattern(RepeatableShape shape,
		IReadOnlyDictionary<char, TraceabilityPredicate> predicates)
	{
		Shape = shape;
		Predicates = predicates;
		BlockPattern.WarnUnreachableRequiredChars(predicates,
			shape.Build(shape.MaxVerticalRepeats, System.Math.Max(0, shape.MaxHorizontalRepeats)));
	}

	public bool CheckPatternAt(MultiblockState state, bool savePredicate = false)
	{
		PatternError? bestError = null;
		bool          bestIsMissingPart = false;

		void Consider(PatternError? err)
		{
			if (err is null) return;
			bool isMissingPart = err is Error.SinglePredicateError spe && (spe.Type == 1 || spe.Type == 3);
			if (bestError is null || (isMissingPart && !bestIsMissingPart))
			{
				bestError = err;
				bestIsMissingPart = isMissingPart;
			}
		}

		int hStep = System.Math.Max(1, Shape.HorizontalStep);
		int vStep = System.Math.Max(1, Shape.VerticalStep);

		int minN, maxN, stepN;
		bool horizontalMode = Shape.Axis == TerrariaCompat.Machine.Multiblock.RepeatAxis.Horizontal;
		if (horizontalMode)
		{
			if (Shape.MaxHorizontalRepeats >= Shape.MinHorizontalRepeats && Shape.MaxHorizontalRepeats > 0)
			{
				minN  = Shape.MinHorizontalRepeats;
				maxN  = Shape.MaxHorizontalRepeats;
				stepN = hStep;
			}
			else
			{
				minN  = Shape.MinVerticalRepeats;
				maxN  = Shape.MaxVerticalRepeats;
				stepN = vStep;
			}
		}
		else
		{
			minN  = Shape.MinVerticalRepeats;
			maxN  = Shape.MaxVerticalRepeats;
			stepN = vStep;
		}

		for (int v = minN; v <= maxN; v += stepN)
		{
			if (horizontalMode)
			{
				if (!_byPair.TryGetValue((0, v), out var pattern))
				{
					pattern = new BlockPattern(Shape.Build(0, v), Predicates, validateReachable: false);
					_byPair[(0, v)] = pattern;
				}
				if (pattern.CheckPatternAt(state, savePredicate))
				{
					state.MatchContext.Set("verticalRepeats",   0);
					state.MatchContext.Set("horizontalRepeats", v);
					return true;
				}
				Consider(state.Error);
				continue;
			}

			for (int h = Shape.MinHorizontalRepeats; h <= Shape.MaxHorizontalRepeats; h += hStep)
			{
				if (!_byPair.TryGetValue((v, h), out var pattern))
				{
					pattern = new BlockPattern(Shape.Build(v, h), Predicates, validateReachable: false);
					_byPair[(v, h)] = pattern;
				}
				if (pattern.CheckPatternAt(state, savePredicate))
				{
					state.MatchContext.Set("verticalRepeats",   v);
					state.MatchContext.Set("horizontalRepeats", h);
					return true;
				}
				Consider(state.Error);
			}
			if (Shape.MaxHorizontalRepeats < Shape.MinHorizontalRepeats)
			{
				if (!_byPair.TryGetValue((v, 0), out var pattern))
				{
					pattern = new BlockPattern(Shape.Build(v, 0), Predicates, validateReachable: false);
					_byPair[(v, 0)] = pattern;
				}
				if (pattern.CheckPatternAt(state, savePredicate))
				{
					state.MatchContext.Set("verticalRepeats",   v);
					state.MatchContext.Set("horizontalRepeats", 0);
					return true;
				}
				Consider(state.Error);
			}
		}
		state.SetError(bestError);
		return false;
	}

	public BlockPattern GetPreviewPattern()
	{
		int v, h;
		if (Shape.Axis == TerrariaCompat.Machine.Multiblock.RepeatAxis.Horizontal)
		{
			v = 0;
			h = Shape.MaxHorizontalRepeats >= Shape.MinHorizontalRepeats && Shape.MaxHorizontalRepeats > 0
				? Shape.MaxHorizontalRepeats
				: Shape.MaxVerticalRepeats;
		}
		else
		{
			v = Shape.MaxVerticalRepeats;
			h = System.Math.Max(0, Shape.MaxHorizontalRepeats);
		}
		if (!_byPair.TryGetValue((v, h), out var pattern))
		{
			pattern = new BlockPattern(Shape.Build(v, h), Predicates);
			_byPair[(v, h)] = pattern;
		}
		return pattern;
	}
}
