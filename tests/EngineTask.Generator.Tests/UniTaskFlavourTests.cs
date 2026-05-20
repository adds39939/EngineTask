using System.Threading.Tasks;
using Xunit;

namespace EngineTask.Generator.Tests;

public class UniTaskFlavourTests
{
    [Fact]
    public Task Task_AsReturnAndParameter_IsMapped() =>
        TestHelper.VerifyEntryAsync(
            "public Task Pass(Task t) => t;",
            flavour: "UniTask");

    [Fact]
    public Task TaskOfT_AsReturnAndParameter_IsMapped() =>
        TestHelper.VerifyEntryAsync(
            "public Task<int> Pass(Task<int> t) => t;",
            flavour: "UniTask");

    [Fact]
    public Task ValueTask_AsReturn_IsMapped() =>
        TestHelper.VerifyEntryAsync(
            "public ValueTask Pass() => default;",
            flavour: "UniTask");

    [Fact]
    public Task ValueTaskOfT_AsReturn_IsMapped() =>
        TestHelper.VerifyEntryAsync(
            "public ValueTask<int> Pass() => default;",
            flavour: "UniTask");

    [Fact]
    public Task TaskDelay_StaticCall_IsMapped() =>
        TestHelper.VerifyEntryAsync(
            "public Task Wait() => Task.Delay(1);",
            flavour: "UniTask");

    [Fact]
    public Task TaskWhenAll_StaticCall_IsMapped() =>
        TestHelper.VerifyEntryAsync(
            "public Task All(Task a, Task b) => Task.WhenAll(a, b);",
            flavour: "UniTask");

    [Fact]
    public Task TaskWhenAny_StaticCall_IsMapped() =>
        TestHelper.VerifyEntryAsync(
            "public Task<Task> Any(Task a, Task b) => Task.WhenAny(a, b);",
            flavour: "UniTask");

    [Fact]
    public Task TaskFromResult_StaticCall_IsMapped() =>
        TestHelper.VerifyEntryAsync(
            "public Task<int> Done() => Task.FromResult(42);",
            flavour: "UniTask");

    [Fact]
    public Task TaskCompletedTask_Property_IsMapped() =>
        TestHelper.VerifyEntryAsync(
            "public Task Done() => Task.CompletedTask;",
            flavour: "UniTask");

    [Fact]
    public Task TaskFromException_StaticCall_IsMapped() =>
        TestHelper.VerifyEntryAsync(
            "public Task Boom() => Task.FromException(new System.Exception());",
            flavour: "UniTask");

    [Fact]
    public Task TaskFromCanceled_StaticCall_IsMapped() =>
        TestHelper.VerifyEntryAsync(
            "public Task Bail(System.Threading.CancellationToken ct) => Task.FromCanceled(ct);",
            flavour: "UniTask");

    [Fact]
    public Task TaskCompletionSource_AsLocal_IsMapped() =>
        TestHelper.VerifyEntryAsync("""
            public Task<int> Boxed()
            {
                var tcs = new TaskCompletionSource<int>();
                tcs.SetResult(42);
                return tcs.Task;
            }
            """,
            flavour: "UniTask");

    [Fact]
    public Task BothFlavoursOnOneClass_EmitsTwoMirrors() =>
        TestHelper.VerifyAllMirrorsAsync("""
            using System.Threading.Tasks;
            using EngineTask;

            namespace Sample;

            [GenerateMirror(TaskFlavour.GDTask)]
            [GenerateMirror(TaskFlavour.UniTask)]
            public partial class Calculator
            {
                public Task<int> AddAsync(int a, int b) => Task.FromResult(a + b);
            }
            """);
}
