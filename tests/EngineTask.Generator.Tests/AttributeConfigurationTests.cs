using System.Threading.Tasks;
using Xunit;

namespace EngineTask.Generator.Tests;

public class AttributeConfigurationTests
{
    [Fact]
    public Task NamespaceOverride_PutsMirrorInExplicitNamespace() =>
        TestHelper.VerifyMirrorAsync("""
            using System.Threading.Tasks;
            using EngineTask;

            namespace Sample;

            [GenerateMirror(TaskFlavour.GDTask, Namespace = "Sample.Engine")]
            public partial class Calculator
            {
                public Task<int> AddAsync(int a, int b) => Task.FromResult(a + b);
            }
            """);

    [Fact]
    public Task ClassSuffix_AppendsToMirrorClassName() =>
        TestHelper.VerifyMirrorAsync("""
            using System.Threading.Tasks;
            using EngineTask;

            namespace Sample;

            [GenerateMirror(TaskFlavour.GDTask, ClassSuffix = "GD")]
            public partial class Calculator
            {
                public Task<int> AddAsync(int a, int b) => Task.FromResult(a + b);
            }
            """);

    [Fact]
    public Task NamespaceAndClassSuffix_TogetherCompose() =>
        TestHelper.VerifyMirrorAsync("""
            using System.Threading.Tasks;
            using EngineTask;

            namespace Sample;

            [GenerateMirror(TaskFlavour.UniTask, Namespace = "Sample.Engine", ClassSuffix = "Uni")]
            public partial class Calculator
            {
                public Task<int> AddAsync(int a, int b) => Task.FromResult(a + b);
            }
            """, flavourSuffix: "UniTask");
}
