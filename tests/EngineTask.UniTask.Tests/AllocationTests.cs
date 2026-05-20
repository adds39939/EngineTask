using System;
using EngineTask.IntegrationTests.UniTaskFlavour.Lib.UniTask;
using Xunit;

namespace EngineTask.IntegrationTests.UniTaskFlavour;

// Regression boundary for the UniTask flavour. Same shape as
// AllocationTests.cs in EngineTask.GDTask.Tests: warm up, force GC,
// measure per-thread allocated bytes across a loop, assert zero.
//
// This runs against the real Cysharp UniTask NuGet package.
// UniTask<T> is a readonly struct holding T inline on the
// synchronously-completed FromResult path. If a regression replaced
// FromResult with anything that allocates, this test will surface it.
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

    // Regression boundary for the `async UniTask<int>` path. The C#
    // compiler builds the state machine with UniTask's
    // AsyncUniTaskMethodBuilder<int>; for synchronously-completing
    // awaits (UniTask.CompletedTask is sync), the state machine
    // stays on the stack and no heap byte is allocated.
    //
    // The assertion is only valid in Release. The C# compiler emits
    // async state machines as *classes* in Debug builds (for
    // debuggability), which forces heap allocation regardless of the
    // builder. CI runs `dotnet test -c Release`, so this test fires
    // there; local `dotnet test` (Debug) skips it.
#if DEBUG
    [Fact(Skip = "Async state machines are reference types in Debug; allocation test is Release-only. See test comment.")]
#else
    [Fact]
#endif
    public void UniTaskMirror_AddAsync_Async_AllocatesZeroBytesPerCall()
    {
        var adder = new Adder();

        for (var i = 0; i < WarmupIterations; i++) _ = adder.AddAsync_Async(i, i + 1);

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < MeasureIterations; i++) _ = adder.AddAsync_Async(i, i + 1);
        var after = GC.GetAllocatedBytesForCurrentThread();

        var total = after - before;
        // Real UniTask lazily initialises some pooling machinery on the
        // first async path it sees in a process — that's the only
        // allocation observable here (~32 bytes one-shot). A Task<int>
        // regression would be ~72 bytes/call × 10k = 720 000 bytes, so
        // a 256-byte total budget catches any real regression by four
        // orders of magnitude.
        const long Budget = 256;
        Assert.True(total < Budget,
            $"UniTask async mirror allocated {total} bytes across {MeasureIterations} calls — exceeds {Budget}-byte budget.");
    }
}
