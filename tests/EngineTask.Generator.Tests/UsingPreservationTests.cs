using System.Threading.Tasks;
using Xunit;

namespace EngineTask.Generator.Tests;

public class UsingPreservationTests
{
    [Fact]
    public Task CancellationToken_Parameter_ResolvesViaCopiedUsing() =>
        TestHelper.VerifyMirrorAsync("""
            using System.Threading;
            using System.Threading.Tasks;
            using EngineTask;

            namespace Sample;

            [GenerateMirror(TaskFlavour.GDTask)]
            public partial class C
            {
                public Task DoAsync(CancellationToken ct) => Task.CompletedTask;
            }
            """);

    [Fact]
    public Task SystemThreadingTasks_Using_IsStripped() =>
        TestHelper.VerifyMirrorAsync("""
            using System;
            using System.Threading.Tasks;
            using EngineTask;

            namespace Sample;

            [GenerateMirror(TaskFlavour.GDTask)]
            public partial class C
            {
                public Task<Func<int>> Wrap(Func<int> f) => Task.FromResult(f);
            }
            """);
}
