#nullable enable
using System;
using System.Collections.Generic;

namespace GregTechCEuTerraria.Api.Recipe.Lookup;

public sealed class RecipeDB
{
	private readonly Branch _rootBranch = new();

	public static System.Action<string>? Warn;

	public void Clear() => _rootBranch.Clear();

	public GTRecipe? Find(List<List<AbstractMapIngredient>> list, Predicate<GTRecipe> predicate)
	{
		var iter = new RecipeIterator(this, list, predicate);
		return iter.HasNext() ? iter.Next() : null;
	}

	private static Dictionary<AbstractMapIngredient, Either<GTRecipe, Branch>> NodesForIngredient(
		AbstractMapIngredient ingredient, Branch branch) =>
		ingredient.IsSpecialIngredient() ? branch.GetSpecialNodes() : branch.GetNodes();

	public bool Add(GTRecipe recipe, List<List<AbstractMapIngredient>> ingredients) =>
		AddRecursive(recipe, ingredients, _rootBranch, 0);

	private bool AddRecursive(GTRecipe recipe, List<List<AbstractMapIngredient>> ingredients,
		Branch branch, int index)
	{
		if (index >= ingredients.Count)
			return true;
		bool lastIngredient = index == ingredients.Count - 1;
		var current = ingredients[index];
		foreach (var ingredient in current)
		{
			var nodes = NodesForIngredient(ingredient, branch);

			if (lastIngredient && nodes.TryGetValue(ingredient, out var existing) && existing.IsRight)
			{
				Warn?.Invoke(
					$"RecipeDB: '{recipe.Id}' terminal ingredient masked by an existing branch - " +
					"routed to flat-scan fallback (frequency sort should have prevented this)");
				return false;
			}

			nodes.TryGetValue(ingredient, out var v);
			Either<GTRecipe, Branch> either = lastIngredient
				? v ?? Either<GTRecipe, Branch>.Left(recipe)
				: v ?? Either<GTRecipe, Branch>.Right(new Branch());
			nodes[ingredient] = either;

			if (either.IsLeft)
			{
				if (ReferenceEquals(either.LeftValue, recipe))
					continue;
				return false;
			}
			bool added = either.IsRight &&
			             AddRecursive(recipe, ingredients, either.RightValue, index + 1);
			if (!added)
			{
				if (lastIngredient)
				{
					nodes.Remove(ingredient);
				}
				else
				{
					if (nodes.TryGetValue(ingredient, out var child) && child.IsRight)
					{
						var childBranch = child.RightValue;
						if (childBranch.IsEmptyBranch())
							nodes.Remove(ingredient);
					}
				}
				return false;
			}
		}
		return true;
	}

	private sealed class SearchFrame
	{
		public int    Index;
		public int    IngredientIndex;
		public Branch Branch;

		public SearchFrame(int index, Branch branch)
		{
			Index           = index;
			IngredientIndex = 0;
			Branch          = branch;
		}
	}

	public sealed class RecipeIterator
	{
		private readonly RecipeDB                       _db;
		private readonly List<List<AbstractMapIngredient>> _ingredients;
		private readonly Predicate<GTRecipe>            _predicate;

		private readonly Stack<SearchFrame> _stack = new();

		private GTRecipe? _nextCached;
		private bool      _hasCached;

		public RecipeIterator(RecipeDB db, List<List<AbstractMapIngredient>> ingredients,
			Predicate<GTRecipe> predicate)
		{
			_db          = db;
			_ingredients = ingredients;
			_predicate   = predicate;

			for (int i = ingredients.Count - 1; i >= 0; i--)
				_stack.Push(new SearchFrame(i, db._rootBranch));
		}

		private GTRecipe? GetNext()
		{
			while (_stack.Count != 0)
			{
				SearchFrame frame = _stack.Peek();

				if (frame.IngredientIndex >= _ingredients[frame.Index].Count)
				{
					_stack.Pop();
					continue;
				}

				List<AbstractMapIngredient> ingredientList = _ingredients[frame.Index];
				AbstractMapIngredient ingredient = ingredientList[frame.IngredientIndex];
				frame.IngredientIndex++;
				var nodes  = NodesForIngredient(ingredient, frame.Branch);
				if (!nodes.TryGetValue(ingredient, out var result))
					continue;

				if (result.IsLeft)
				{
					var recipe = result.LeftValue;
					if (_predicate(recipe))
						return recipe;
				}

				if (result.IsRight)
				{
					var b = result.RightValue;
					for (int j = _ingredients.Count - 1; j >= 0; j--)
						_stack.Push(new SearchFrame(j, b));
				}
			}

			return null;
		}

		public bool HasNext()
		{
			if (!_hasCached)
			{
				_nextCached = GetNext();
				_hasCached  = true;
			}
			return _nextCached != null;
		}

		public GTRecipe Next()
		{
			if (!_hasCached) _nextCached = GetNext();
			_hasCached = false;
			if (_nextCached == null) throw new InvalidOperationException("No more recipes");
			return _nextCached;
		}

		public void Reset()
		{
			_stack.Clear();
			for (int i = _ingredients.Count - 1; i >= 0; i--)
				_stack.Push(new SearchFrame(i, _db._rootBranch));
			_nextCached = null;
			_hasCached  = false;
		}
	}
}
