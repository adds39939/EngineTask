using System.Threading.Tasks;
using Xunit;

namespace EngineTask.Generator.Tests;

public class MirrorCollisionTests
{
    [Fact]
    public Task UserPartialWithSameSignature_EmitsENGTASK004AndSkips() =>
        TestHelper.VerifyWithDiagnosticsAsync("""
            using System.Threading.Tasks;
            using EngineTask;

            namespace Sample
            {
                [GenerateMirror(TaskFlavour.GDTask)]
                public partial class C
                {
                    public Task<int> AddAsync(int a, int b) => Task.FromResult(a + b);
                    public Task<int> SubAsync(int a, int b) => Task.FromResult(a - b);
                }
            }

            namespace Sample.GDTask
            {
                public partial class C
                {
                    public global::GodotTask.GDTask<int> AddAsync(int a, int b)
                        => global::GodotTask.GDTask.FromResult(a + b + 100);
                }
            }
            """);
}
