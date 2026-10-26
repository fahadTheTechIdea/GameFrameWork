using System.Buffers.Binary;
using System.Collections.Immutable;
using System.Globalization;
using System.Security.Cryptography;
using ScenarioEngine.Contracts;

namespace ScenarioEngine.Core;

public sealed class PcgRandomStreams(ulong seed, ImmutableDictionary<string, RandomStreamState> initial) : IRandomStreams
{
    public const string Algorithm = "pcg32-xsh-rr-v1";
    private readonly Dictionary<string, RandomStreamState> _states = initial.ToDictionary(p => p.Key, p => p.Value, StringComparer.Ordinal);
    public ImmutableDictionary<string, RandomStreamState> Capture() => _states.ToImmutableDictionary(StringComparer.Ordinal);
    public uint NextUInt32(string streamName, EntityId? entity = null)
    {
        if (string.IsNullOrWhiteSpace(streamName) || streamName.Length > 128)
            throw new ArgumentException("Invalid random stream name.", nameof(streamName));
        var key = streamName + "::" + (entity?.Value.ToString(CultureInfo.InvariantCulture) ?? "global");
        if (!_states.TryGetValue(key, out var state))
        {
            var digest = SHA256.HashData(ContractJson.Canonical(ContractJson.Element(new { seed, streamName, entity })));
            state = Initialize(BinaryPrimitives.ReadUInt64LittleEndian(digest), BinaryPrimitives.ReadUInt64LittleEndian(digest.AsSpan(8)));
        }
        var value = Next(state, out var next);
        _states[key] = next;
        return value;
    }
    public static RandomStreamState Initialize(ulong initialState, ulong sequence)
    {
        var state = new RandomStreamState(0, unchecked((sequence << 1) | 1), 0);
        Next(state, out state);
        state = state with { State = unchecked(state.State + initialState) };
        Next(state, out state);
        return state with { DrawCount = 0 };
    }
    public static uint Next(RandomStreamState state, out RandomStreamState next)
    {
        var old = state.State;
        next = state with { State = unchecked(old * 6364136223846793005UL + state.Increment), DrawCount = checked(state.DrawCount + 1) };
        var shifted = (uint)(((old >> 18) ^ old) >> 27);
        var rotation = (int)(old >> 59);
        return (shifted >> rotation) | (shifted << ((-rotation) & 31));
    }
}
