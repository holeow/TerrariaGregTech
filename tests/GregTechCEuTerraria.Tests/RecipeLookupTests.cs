using System;
using System.Collections.Generic;
using GregTechCEuTerraria.Api.Recipe;
using GregTechCEuTerraria.Api.Recipe.Lookup;
using Xunit;

namespace GregTechCEuTerraria.Tests
{
	public class RecipeLookupTests
	{
		private const int Cobble = 1, Stone = 2, Acacia = 3, Birch = 4, Cherry = 5,
		                  RedstoneTorch = 6, A = 7, B = 8;

		private static readonly Predicate<GTRecipe> AlwaysTrue  = _ => true;
		private static readonly Predicate<GTRecipe> AlwaysFalse = _ => false;

		private static GTRecipe R(string id) => new(id);
		private static ItemMapIngredient ItemKey(int type) => new(type);

		private static List<AbstractMapIngredient> Slot(params AbstractMapIngredient[] keys) =>
			new(keys);

		private static List<List<AbstractMapIngredient>> Ings(params List<AbstractMapIngredient>[] slots) =>
			new(slots);

		[Fact]
		public void SimpleSuccess_FindsRecipeByItsIngredient()
		{
			var db = new RecipeDB();
			var smelt = R("smelt_stone");
			Assert.True(db.Add(smelt, Ings(Slot(ItemKey(Cobble)))));

			Assert.Same(smelt, db.Find(Ings(Slot(ItemKey(Cobble))), AlwaysTrue));
		}

		[Fact]
		public void SimpleFailure_UnrelatedIngredientFindsNothing()
		{
			var db = new RecipeDB();
			db.Add(R("smelt_stone"), Ings(Slot(ItemKey(Cobble))));

			Assert.Null(db.Find(Ings(Slot(ItemKey(RedstoneTorch))), AlwaysTrue));
		}

		[Fact]
		public void FalsePredicate_FindsNothing()
		{
			var db = new RecipeDB();
			db.Add(R("smelt_stone"), Ings(Slot(ItemKey(Cobble))));

			Assert.Null(db.Find(Ings(Slot(ItemKey(Cobble))), AlwaysFalse));
		}

		[Fact]
		public void ExtraUnrelatedIngredients_StillFinds()
		{
			var db = new RecipeDB();
			var smelt = R("smelt_stone");
			db.Add(smelt, Ings(Slot(ItemKey(Cobble))));

			Assert.Same(smelt,
				db.Find(Ings(Slot(ItemKey(Cobble)), Slot(ItemKey(RedstoneTorch))), AlwaysTrue));
		}

		[Fact]
		public void EmptyDb_FindsNothing()
		{
			Assert.Null(new RecipeDB().Find(Ings(Slot(ItemKey(Cobble))), AlwaysTrue));
		}

		[Fact]
		public void MultiInputRecipe_RequiresEveryInput()
		{
			var db = new RecipeDB();
			var r = R("a_plus_b");
			db.Add(r, Ings(Slot(ItemKey(A)), Slot(ItemKey(B))));

			Assert.Same(r, db.Find(Ings(Slot(ItemKey(A)), Slot(ItemKey(B))), AlwaysTrue));
			Assert.Same(r, db.Find(Ings(Slot(ItemKey(B)), Slot(ItemKey(A))), AlwaysTrue));

			Assert.Null(db.Find(Ings(Slot(ItemKey(A))), AlwaysTrue));
			Assert.Null(db.Find(Ings(Slot(ItemKey(B))), AlwaysTrue));
		}

		[Fact]
		public void TagExpansion_RecipeIndexedUnderEveryAlternativeKey()
		{
			var db = new RecipeDB();
			var r = R("smelt_any_wood");
			db.Add(r, Ings(Slot(ItemKey(Acacia), ItemKey(Birch), ItemKey(Cherry))));

			Assert.Same(r, db.Find(Ings(Slot(ItemKey(Acacia))), AlwaysTrue));
			Assert.Same(r, db.Find(Ings(Slot(ItemKey(Birch))),  AlwaysTrue));
			Assert.Same(r, db.Find(Ings(Slot(ItemKey(Cherry))), AlwaysTrue));
			Assert.Null(db.Find(Ings(Slot(ItemKey(Stone))), AlwaysTrue));
		}

		[Fact]
		public void Conflict_SecondRecipeOnSameExactPathIsRejected()
		{
			var db = new RecipeDB();
			var first = R("first");
			Assert.True(db.Add(first, Ings(Slot(ItemKey(Cobble)))));
			Assert.False(db.Add(R("second"), Ings(Slot(ItemKey(Cobble)))));
			Assert.Same(first, db.Find(Ings(Slot(ItemKey(Cobble))), AlwaysTrue));
		}

		[Fact]
		public void ShortRecipeMaskedByExistingBranch_AddFails_NotSilentlyLost()
		{
			var db     = new RecipeDB();
			var longR  = R("iron_plus_circuit");
			var shortR = R("iron_only");
			Assert.True(db.Add(longR, Ings(Slot(ItemKey(A)), Slot(new CircuitMapIngredient(1)))));
			Assert.False(db.Add(shortR, Ings(Slot(ItemKey(A)))));
			Assert.Same(longR, db.Find(
				Ings(Slot(ItemKey(A)), Slot(new CircuitMapIngredient(1))), AlwaysTrue));
			Assert.Null(db.Find(Ings(Slot(ItemKey(A))), AlwaysTrue));
		}

		[Fact]
		public void CustomPredicate_SelectsMatchingRecipe()
		{
			var db = new RecipeDB();
			db.Add(R("wanted"), Ings(Slot(ItemKey(Cobble))));

			Assert.Equal("wanted",
				db.Find(Ings(Slot(ItemKey(Cobble))), r => r.Id == "wanted")?.Id);
			Assert.Null(db.Find(Ings(Slot(ItemKey(Cobble))), r => r.Id == "other"));
		}

		[Fact]
		public void Iterator_YieldsEveryReachableRecipe()
		{
			var db = new RecipeDB();
			var r1 = R("r1");
			var r2 = R("r2");
			db.Add(r1, Ings(Slot(ItemKey(Cobble))));
			db.Add(r2, Ings(Slot(ItemKey(Stone))));

			var iter = new RecipeDB.RecipeIterator(
				db, Ings(Slot(ItemKey(Cobble)), Slot(ItemKey(Stone))), AlwaysTrue);
			var found = new List<GTRecipe>();
			while (iter.HasNext()) found.Add(iter.Next());

			Assert.Contains(r1, found);
			Assert.Contains(r2, found);

			iter.Reset();
			Assert.True(iter.HasNext());
		}

		[Fact]
		public void MapIngredient_SameClassEqualityGate()
		{
			var item    = new ItemMapIngredient(1);
			var circuit = new CircuitMapIngredient(31);
			Assert.Equal(item.GetHashCode(), circuit.GetHashCode());
			Assert.False(item.Equals(circuit));
			Assert.False(circuit.Equals(item));

			Assert.Equal(new ItemMapIngredient(5), new ItemMapIngredient(5));
			Assert.NotEqual(new ItemMapIngredient(5), new ItemMapIngredient(6));
		}

		[Fact]
		public void HashCollidingKeys_OfDifferentClassesStaySeparateInTrie()
		{
			var db = new RecipeDB();
			var itemRecipe    = R("item_recipe");
			var circuitRecipe = R("circuit_recipe");
			db.Add(itemRecipe,    Ings(Slot(new ItemMapIngredient(1))));
			db.Add(circuitRecipe, Ings(Slot(new CircuitMapIngredient(31))));

			Assert.Same(itemRecipe,
				db.Find(Ings(Slot(new ItemMapIngredient(1))), AlwaysTrue));
			Assert.Same(circuitRecipe,
				db.Find(Ings(Slot(new CircuitMapIngredient(31))), AlwaysTrue));
		}

		[Fact]
		public void MixedCapabilities_ItemFluidCircuitInOneRecipe()
		{
			var db = new RecipeDB();
			var r = R("mixed");
			db.Add(r, Ings(
				Slot(new ItemMapIngredient(Cobble)),
				Slot(new FluidMapIngredient("water")),
				Slot(new CircuitMapIngredient(2))));

			Assert.Same(r, db.Find(Ings(
				Slot(new ItemMapIngredient(Cobble)),
				Slot(new FluidMapIngredient("water")),
				Slot(new CircuitMapIngredient(2))), AlwaysTrue));

			Assert.Null(db.Find(Ings(
				Slot(new ItemMapIngredient(Cobble)),
				Slot(new FluidMapIngredient("water")),
				Slot(new CircuitMapIngredient(9))), AlwaysTrue));
		}
	}
}

namespace GregTechCEuTerraria.Api.Recipe
{
	public sealed class GTRecipe
	{
		public readonly string Id;
		public GTRecipe(string id) => Id = id;
		public override string ToString() => Id;
	}
}
