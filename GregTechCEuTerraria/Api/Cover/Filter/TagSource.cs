#nullable enable
using System;
using System.Collections.Generic;
using GregTechCEuTerraria.Api.Fluids;
using Terraria;

namespace GregTechCEuTerraria.Api.Cover.Filter;

public static class TagSource
{
	private static readonly IReadOnlyCollection<string> Empty = Array.Empty<string>();

	public static Func<Item, IReadOnlyCollection<string>>? ItemTags { get; set; }
	public static Func<FluidStack, IReadOnlyCollection<string>>? FluidTags { get; set; }

	public static IReadOnlyCollection<string> TagsOf(Item item) =>
		ItemTags?.Invoke(item) ?? Empty;

	public static IReadOnlyCollection<string> TagsOf(FluidStack fluid) =>
		FluidTags?.Invoke(fluid) ?? Empty;

	public static Func<IReadOnlyCollection<string>>? AllItemTags { get; set; }
	public static Func<IReadOnlyCollection<string>>? AllFluidTags { get; set; }

	public static IReadOnlyCollection<string> AllItemTagNames() => AllItemTags?.Invoke() ?? Empty;

	public static IReadOnlyCollection<string> AllFluidTagNames() => AllFluidTags?.Invoke() ?? Empty;
}
