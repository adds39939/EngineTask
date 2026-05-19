using System.Threading.Tasks;
using Xunit;

namespace EngineTask.Generator.Tests;

public class UnmappedApiDiagnosticTests
{
    [Fact]
    public Task TaskRun_EmitsENGTASK001AndSkipsMethod() =>
        TestHelper.VerifyWithDiagnosticsAsync("""
            using System.Threading.Tasks;
            using EngineTask;

            namespace Sample;

            [GenerateMirror(TaskFlavour.GDTask)]
            public partial class C
            {
                public Task Risky() => Task.Run(() => 42);
                public Task Safe() => Task.CompletedTask;
            }
            """);

    [Fact]
    public Task IAsyncEnumerableParameter_EmitsENGTASK001AndSkipsMethod() =>
        TestHelper.VerifyWithDiagnosticsAsync("""
            using System.Collections.Generic;
            using System.Threading.Tasks;
            using EngineTask;

            namespace Sample;

            [GenerateMirror(TaskFlavour.GDTask)]
            public partial class C
            {
                public Task Consume(IAsyncEnumerable<int> source) => Task.CompletedTask;
                public Task Safe() => Task.CompletedTask;
            }
            """);

    [Fact]
    public Task MirrorIgnore_SuppressesENGTASK001() =>
        TestHelper.VerifyWithDiagnosticsAsync("""
            using System.Threading.Tasks;
            using EngineTask;

            namespace Sample;

            [GenerateMirror(TaskFlavour.GDTask)]
            public partial class C
            {
                [MirrorIgnore]
                public Task Risky() => Task.Run(() => 42);
                public Task Safe() => Task.CompletedTask;
            }
            """);
}
