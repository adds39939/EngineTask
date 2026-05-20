using System;
using EngineTask.IntegrationTests.UniTaskFlavour.Lib.UniTask;
using Xunit;

namespace EngineTask.IntegrationTests.UniTaskFlavour;

// Phase 5 — regression boundary for the UniTask flavour. Same shape as
// AllocationTests.cs in EngineTask.GDTask.Tests: warm up, force GC,
// measure per-thread allocated bytes across a loop, assert zero.
//
// This runs against the Cysharp.Threading.Tasks shim
// (EngineTask.UniTask.Shim project). UniTask<T> in the shim is a
// readonly struct holding T inline — exactly the shape of the real
// package on the synchronously-completed FromResult path. If a
// regression replaced FromResult with anything that allocates, this
// test will surface it.
public class AllocationTests
{
    private const int WarmupIterations = 5_000;
    private const int MeasureIterations = 10_000;

    [Fact]
    public void UniTaskMirror_AddAsync_AllocatesZeroBytesPerCall()
    {
        var adder = new Adder();

        for (var i = 0; i < WarmupIterations; i++) _ = adder.AddAsync(i, i + 1);

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < MeasureIterations; i++) _ = adder.AddAsync(i, i + 1);
        var after = GC.GetAllocatedBytesForCurrentThread();

        var total = after - before;
        Assert.True(total == 0,
            $"UniTask mirror allocated {total} bytes across {MeasureIterations} calls — expected 0. " +
            "UniTask<int> is a readonly struct, so the call site should be stack-only.");
    }
}
