using EngineTask.Sample.Core.GDTask;
using Xunit;

namespace EngineTask.IntegrationTests.GDTask;

public class CalculatorMirrorTests
{
    [Fact]
    public async System.Threading.Tasks.Task AddAsync_ReturnsSum_ViaGDTaskStateMachine()
    {
        var calculator = new Calculator();

        // The mirror's body is `global::GodotTask.GDTask.FromResult(a + b)`.
        // Awaiting a synchronously-completed GDTask doesn't touch the Godot
        // player loop, so this works in a vanilla `dotnet test` host. The
        // value of this test is that the C# compiler had to pick
        // AsyncGDTaskMethodBuilder for the mirror — without that, this would
        // be a Task-returning method and our claim is broken.
        var result = await calculator.AddAsync(2, 3);

        Assert.Equal(5, result);
    }
}
