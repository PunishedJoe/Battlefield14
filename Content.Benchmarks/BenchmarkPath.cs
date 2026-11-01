using System;
using System.Reflection;
using Robust.Shared.GameObjects;

namespace Content.Benchmarks;

internal static class BenchmarkPath
{
    public static void Configure()
    {
        // The benchmark runner starts from a different directory than the game binaries.
        // ProgramShared is internal to Robust.Shared, so set its path offset here.
        var programShared = typeof(IEntityManager).Assembly.GetType("Robust.Shared.ProgramShared")
            ?? throw new InvalidOperationException("Could not find Robust.Shared.ProgramShared");
        var pathOffset = programShared.GetField("PathOffset", BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException("Could not find ProgramShared.PathOffset");
        pathOffset.SetValue(null, "../../../../");
    }
}
