#nullable enable
using System.Collections.Generic;
using GregTechCEuTerraria.Api.Recipe.Category;
using GregTechCEuTerraria.Api.Recipe.Chance.Boost;
using GregTechCEuTerraria.Api.Recipe.Lookup;

namespace GregTechCEuTerraria.Api.Recipe;

// port of com.gregtechceu.gtceu.api.recipe.GTRecipeType.
public sealed class GTRecipeType
{
	public string RegistryName { get; }

	public GTRecipeType(string registryName)
	{
		RegistryName = registryName;
		lock (_registry) _registry[registryName] = this;
	}

	// === Registry =============================================================
	// Mirrors upstream's `GTRegistries.RECIPE_TYPES` lookup but flat
	private static readonly System.Collections.Generic.Dictionary<string, GTRecipeType> _registry = new();

	private GTRecipeType() : this("__placeholder__") { }

	public static readonly GTRecipeType PLACEHOLDER = new();

	public static GTRecipeType GetOrCreate(string registryName)
	{
		lock (_registry)
		{
			if (_registry.TryGetValue(registryName, out var existing)) return existing;
		}
		return new GTRecipeType(registryName); // ctor inserts
	}

	// === Research data-stick entries ========================================
	// Port of GTRecipeType.researchEntries (addDataStickEntry /
	// getDataStickEntry / removeDataStickEntry). Maps a research_id to the
	// recipes. Populated at recipe-load when a recipe carries a ResearchCondition
	private readonly System.Collections.Generic.Dictionary<string, System.Collections.Generic.HashSet<GTRecipe>> _researchEntries = new();

	public void AddDataStickEntry(string researchId, GTRecipe recipe)
	{
		lock (_researchEntries)
		{
			if (!_researchEntries.TryGetValue(researchId, out var set))
			{
				set = new System.Collections.Generic.HashSet<GTRecipe>();
				_researchEntries[researchId] = set;
			}
			set.Add(recipe);
		}
	}

	public System.Collections.Generic.IReadOnlyCollection<GTRecipe>? GetDataStickEntry(string researchId)
	{
		lock (_researchEntries)
			return _researchEntries.TryGetValue(researchId, out var set) ? set : null;
	}

	public bool RemoveDataStickEntry(string researchId, GTRecipe recipe)
	{
		lock (_researchEntries)
		{
			if (!_researchEntries.TryGetValue(researchId, out var set)) return false;
			bool removed = set.Remove(recipe);
			if (removed && set.Count == 0) _researchEntries.Remove(researchId);
			return removed;
		}
	}

	public static GTRecipeType? Get(string registryName)
	{
		lock (_registry) return _registry.TryGetValue(registryName, out var t) ? t : null;
	}

	public static System.Collections.Generic.IReadOnlyCollection<GTRecipeType> All
	{
		get { lock (_registry) return new System.Collections.Generic.List<GTRecipeType>(_registry.Values); }
	}

	public GTRecipeCategory GetCategory() => GTRecipeCategory.DEFAULT;

	public ChanceBoostFunction ChanceFunction { get; set; } = ChanceBoostFunction.NONE;

	// === RecipeLookup trie ==================================================
	// Port of upstream's per-GTRecipeType `RecipeDB db`. Built lazily on first
	// SearchRecipe from the station's RecipeRegistry list
	private RecipeDB?       _db;
	private List<GTRecipe>? _untrieable;
	private int             _dbBuiltAtCount = -1;

	private void EnsureDb()
	{
		int count = TerrariaCompat.Recipes.RecipeRegistry.Count;
		if (_db != null && _dbBuiltAtCount == count) return;

		var db         = new RecipeDB();
		var untrieable = new List<GTRecipe>();
		foreach (var r in TerrariaCompat.Recipes.RecipeRegistry.ForStation(RegistryName))
		{
			if (HasUnresolvedItemInput(r)) continue;
			var lists = RecipeLookupCompiler.TryCompileRecipe(r);
			if (lists == null || lists.Count == 0 || !db.Add(r, lists))
				untrieable.Add(r);
		}
		_db             = db;
		_untrieable     = untrieable;
		_dbBuiltAtCount = count;
	}

	private static bool HasUnresolvedItemInput(GTRecipe r)
	{
		if (!r.Inputs.TryGetValue(Capability.Recipe.ItemRecipeCapability.CAP, out var contents)) return false;
		foreach (var c in contents)
			if (c.Payload is Ingredient.Ingredient ing && ing.IsEmpty) return true;
		return false;
	}

	public IEnumerable<GTRecipe> SearchRecipe(
		Machine.Feature.IRecipeLogicMachine holder,
		System.Predicate<GTRecipe> filter)
	{
		if (!holder.SupportsRecipeLookup)
		{
			foreach (var r in TerrariaCompat.Recipes.RecipeRegistry.ForStation(RegistryName))
				if (!HasUnresolvedItemInput(r) && filter(r)) yield return r;
			yield break;
		}

		EnsureDb();
		var query = RecipeLookupCompiler.CompileQuery(holder);
		var seen  = new HashSet<GTRecipe>();

		var iter = new RecipeDB.RecipeIterator(_db!, query, filter);
		while (iter.HasNext())
		{
			var r = iter.Next();
			if (seen.Add(r)) yield return r;
		}
		foreach (var r in _untrieable!)
			if (seen.Add(r) && filter(r)) yield return r;
	}

	// Used by RecipeLogic to re-attach the running recipe after a world load / MP join
	public GTRecipe? GetRecipeById(string id)
	{
		foreach (var r in TerrariaCompat.Recipes.RecipeRegistry.ForStation(RegistryName))
			if (r.Id == id) return r;
		return null;
	}

	// === Capability presence ================================================
	private HashSet<object>? _inputCaps;
	private HashSet<object>? _outputCaps;
	private int              _capsBuiltAtCount = -1;

	private void EnsureCaps()
	{
		int count = TerrariaCompat.Recipes.RecipeRegistry.Count;
		if (_inputCaps != null && _capsBuiltAtCount == count) return;
		var ins  = new HashSet<object>();
		var outs = new HashSet<object>();
		foreach (var r in TerrariaCompat.Recipes.RecipeRegistry.ForStation(RegistryName))
		{
			foreach (var k in r.Inputs.Keys)      ins.Add(k);
			foreach (var k in r.TickInputs.Keys)  ins.Add(k);
			foreach (var k in r.Outputs.Keys)     outs.Add(k);
			foreach (var k in r.TickOutputs.Keys) outs.Add(k);
		}
		_inputCaps        = ins;
		_outputCaps       = outs;
		_capsBuiltAtCount = count;
	}

	public bool HasInput(object capability)  { EnsureCaps(); return _inputCaps!.Contains(capability); }
	public bool HasOutput(object capability) { EnsureCaps(); return _outputCaps!.Contains(capability); }

	public override string ToString() => $"GTRecipeType{{{RegistryName}}}";
	public override bool Equals(object? obj) => obj is GTRecipeType t && RegistryName == t.RegistryName;
	public override int GetHashCode() => RegistryName.GetHashCode();
}
