using EngineTask.IntegrationTests.UniTaskFlavour.Lib.UniTask;
using Xunit;

namespace EngineTask.IntegrationTests.UniTaskFlavour;

public class AdderMirrorTests
{
    [Fact]
    public async System.Threading.Tasks.Task AddAsync_ReturnsSum_ViaUniTaskAwaiter()
    {
        var adder = new Adder();

        // The mirror's body is `Cysharp.Threading.Tasks.UniTask.FromResult(a + b)`.
        // Awaiting the resulting UniTask<int> goes through UniTask's
        // synchronous Awaiter. Allocation behaviour is asserted in
        // AllocationTests.cs alongside this file.
        var result = await adder.AddAsync(2, 3);

        Assert.Equal(5, result);
    }

    [Fact]
    public async System.Threading.Tasks.Task AddAsync_Async_ReturnsSum_ViaUniTaskStateMachine()
    {
        var adder = new Adder();

        // The mirror's body uses `async UniTask<int>`. The C# compiler
        // builds the state machine with AsyncUniTaskMethodBuilder<int>.
        // For a synchronously-completing path
        // (`await UniTask.CompletedTask; return a + b;`) the entire
        // chain stays on the stack — see AllocationTests.
        var result = await adder.AddAsync_Async(2, 3);

        Assert.Equal(5, result);
    }
}
