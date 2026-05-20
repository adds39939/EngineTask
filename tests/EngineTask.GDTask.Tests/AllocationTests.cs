using System;
using EngineTask.Sample.Core.GDTask;
using Xunit;

namespace EngineTask.IntegrationTests.GDTask;

// Phase 5 — these tests are the regression boundary for the project's
// central claim: a consumer calling the engine-task mirror does not
// allocate a Task. They use GC.GetAllocatedBytesForCurrentThread() to
// measure heap allocation on the current thread across a warmed-up
// loop, and assert the mirror's per-call cost stays at zero.
//
// The second test exercises the *source* Task path on the same input
// to validate the measurement itself — if Task.FromResult ever stopped
// allocating (and the mirror test still passed), the regression
// boundary would be dishonest.
public class AllocationTests
{
    private const int WarmupIterations = 5_000;
    private const int MeasureIterations = 10_000;

    [Fact]
    public void GDTaskMirror_AddAsync_AllocatesZeroBytesPerCall()
    {
        var calc = new Calculator();

        for (var i = 0; i < WarmupIterations; i++) _ = calc.AddAsync(i, i + 1);

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < MeasureIterations; i++) _ = calc.AddAsync(i, i + 1);
        var after = GC.GetAllocatedBytesForCurrentThread();

        var total = after - before;
        Assert.True(total == 0,
            $"GDTask mirror allocated {total} bytes across {MeasureIterations} calls — expected 0. " +
            "GDTask<int> is a readonly struct, so the call site should be stack-only.");
    }

    [Fact]
    public void SourceTaskPath_AddAsync_AllocatesPerCall()
    {
        var source = new EngineTask.Sample.Core.Calculator();

        for (var i = 0; i < WarmupIterations; i++) _ = source.AddAsync(i, i + 1);

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < MeasureIterations; i++) _ = source.AddAsync(i, i + 1);
        var after = GC.GetAllocatedBytesForCurrentThread();

        var total = after - before;
        // The exact byte count depends on Task<int>'s layout and any
        // small-int cache, but it must be > 0 — otherwise the
        // GDTask test above is asserting against a baseline of zero
        // and would not detect a regression.
        Assert.True(total > 0,
            $"Source Task.FromResult allocated {total} bytes — expected > 0 to validate the GDTask mirror measurement above.");
    }
}
