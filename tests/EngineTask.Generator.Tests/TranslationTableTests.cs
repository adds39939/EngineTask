using System.Threading.Tasks;
using Xunit;

namespace EngineTask.Generator.Tests;

public class TranslationTableTests
{
    // -------- Type mappings --------

    [Fact]
    public Task Task_AsReturnAndParameter_IsMapped() =>
        TestHelper.VerifyEntryAsync("public Task Consume(Task t) => t;");

    [Fact]
    public Task TaskOfT_AsReturnAndParameter_IsMapped() =>
        TestHelper.VerifyEntryAsync("public Task<int> Get(Task<int> t) => t;");

    [Fact]
    public Task ValueTask_AsReturn_IsMapped() =>
        TestHelper.VerifyEntryAsync("public ValueTask DoAsync() => default;");

    [Fact]
    public Task ValueTaskOfT_AsReturn_IsMapped() =>
        TestHelper.VerifyEntryAsync("public ValueTask<int> GetAsync() => default;");

    [Fact]
    public Task TaskCompletionSource_AsLocal_IsMapped() =>
        TestHelper.VerifyEntryAsync("""
            public Task<int> Make()
                {
                    var tcs = new TaskCompletionSource<int>();
                    return tcs.Task;
                }
            """);

    // -------- Member mappings --------

    [Fact]
    public Task TaskDelay_StaticCall_IsMapped() =>
        TestHelper.VerifyEntryAsync("public async Task DelayAsync() => await Task.Delay(100);");

    [Fact]
    public Task TaskWhenAll_StaticCall_IsMapped() =>
        TestHelper.VerifyEntryAsync("public Task AllOf(Task[] tasks) => Task.WhenAll(tasks);");

    [Fact]
    public Task TaskWhenAny_StaticCall_IsMapped() =>
        TestHelper.VerifyEntryAsync("public Task<Task> AnyOf(Task[] tasks) => Task.WhenAny(tasks);");

    [Fact]
    public Task TaskFromResult_StaticCall_IsMapped() =>
        TestHelper.VerifyEntryAsync("public Task<int> Wrap(int v) => Task.FromResult(v);");

    [Fact]
    public Task TaskCompletedTask_Property_IsMapped() =>
        TestHelper.VerifyEntryAsync("public Task Done() => Task.CompletedTask;");

    [Fact]
    public Task TaskFromException_StaticCall_IsMapped() =>
        TestHelper.VerifyEntryAsync("public Task Fail(System.Exception ex) => Task.FromException(ex);");

    [Fact]
    public Task TaskFromCanceled_StaticCall_IsMapped() =>
        TestHelper.VerifyEntryAsync(
            "public Task Cancel(System.Threading.CancellationToken ct) => Task.FromCanceled(ct);");
}
