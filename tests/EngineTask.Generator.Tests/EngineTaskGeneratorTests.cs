using System.Threading.Tasks;
using Xunit;

namespace EngineTask.Generator.Tests;

public class EngineTaskGeneratorTests
{
    [Fact]
    public Task EmitsMirrorForTaskAndTaskOfIntMethods()
    {
        const string source = """
            using System.Threading.Tasks;
            using EngineTask;

            namespace Sample;

            [GenerateMirror(TaskFlavour.GDTask)]
            public partial class Calculator
            {
                public Task DoNothingAsync() => Task.CompletedTask;
                public Task<int> AddAsync(int a, int b) => Task.FromResult(a + b);
            }
            """;

        return TestHelper.RunAsync(source);
    }
}
