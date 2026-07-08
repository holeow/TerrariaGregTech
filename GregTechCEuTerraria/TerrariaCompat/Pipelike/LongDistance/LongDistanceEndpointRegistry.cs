#nullable enable
using System.Collections.Generic;
using GregTechCEuTerraria.Api.Capability.Recipe;

namespace GregTechCEuTerraria.TerrariaCompat.Pipelike.LongDistance;

public static class LongDistanceEndpointRegistry
{
	private static readonly Dictionary<(int x, int y), ILDEndpoint> _endpoints = new();

	public static void Register(ILDEndpoint e)
	{
		var key = e.EndpointPos;
		if (_endpoints.TryGetValue(key, out var existing) && ReferenceEquals(existing, e)) return;
		_endpoints[key] = e;
		InvalidateAll();
	}

	public static void Unregister(ILDEndpoint e)
	{
		var key = e.EndpointPos;
		if (_endpoints.TryGetValue(key, out var existing) && ReferenceEquals(existing, e))
		{
			_endpoints.Remove(key);
			InvalidateAll();
		}
	}

	public static void Clear() => _endpoints.Clear();

	public static void InvalidateAll()
	{
		foreach (var e in _endpoints.Values) e.InvalidateLink();
	}

	public static ILDEndpoint? ResolveLink(ILDEndpoint self)
	{
		if (self.IoType is not (IO.IN or IO.OUT)) return null;
		var net = self.AttachedNet;
		if (net is null) return null;

		var (input, output) = ActivePair(net, self.PipeType);
		if (input is null || output is null) return null;
		if (!SatisfiesMinLength(input, output)) return null;

		if (ReferenceEquals(self, input)) return output;
		if (ReferenceEquals(self, output)) return input;
		return null;
	}

	private static (ILDEndpoint? input, ILDEndpoint? output) ActivePair(
		LongDistancePipeNet net, LongDistancePipeType type)
	{
		ILDEndpoint? input = null, output = null;
		foreach (var e in _endpoints.Values)
		{
			if (e.IsRemoved || e.PipeType != type) continue;
			if (!ReferenceEquals(e.AttachedNet, net)) continue;
			if (e.IoType == IO.IN  && (input  is null || Key(e) < Key(input)))  input  = e;
			if (e.IoType == IO.OUT && (output is null || Key(e) < Key(output))) output = e;
		}
		return (input, output);
	}

	private static bool SatisfiesMinLength(ILDEndpoint a, ILDEndpoint b)
	{
		if (ReferenceEquals(a, b)) return false;
		int min = a.PipeType.MinLength();
		long dx = a.EndpointPos.x - b.EndpointPos.x;
		long dy = a.EndpointPos.y - b.EndpointPos.y;
		return dx * dx + dy * dy >= (long)min * min;
	}

	private static long Key(ILDEndpoint e) =>
		((long)e.EndpointPos.x << 32) | (uint)e.EndpointPos.y;
}
