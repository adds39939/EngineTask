using System.Threading.Tasks;
using Xunit;

namespace EngineTask.Generator.Tests;

public class StructuralDiagnosticTests
{
    [Fact]
    public Task NonPartialClass_EmitsENGTASK002() =>
        TestHelper.VerifyWithDiagnosticsAsync("""
            using System.Threading.Tasks;
            using EngineTask;

            namespace Sample;

            [GenerateMirror(TaskFlavour.GDTask)]
            public class NotPartial
            {
                public Task Done() => Task.CompletedTask;
            }
            """);

    [Fact]
    public Task AsyncVoidMethod_EmitsENGTASK003AndSkips() =>
        TestHelper.VerifyWithDiagnosticsAsync("""
            using System.Threading.Tasks;
            using EngineTask;

            namespace Sample;

            [GenerateMirror(TaskFlavour.GDTask)]
            public partial class C
            {
                public async void BadAsync() { await Task.Delay(1); }
                public Task Good() => Task.CompletedTask;
            }
            """);
}
