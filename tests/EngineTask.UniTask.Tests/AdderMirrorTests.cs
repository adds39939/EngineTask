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
        // Awaiting the resulting UniTask<int> goes through the shim's
        // synchronous Awaiter — Phase 4 only claims the generator emits
        // code that compiles and runs against Cysharp.Threading.Tasks.UniTask.
        // Allocation verification is Phase 5.
        var result = await adder.AddAsync(2, 3);

        Assert.Equal(5, result);
    }
}
