using System.Threading.Tasks;
using EngineTask;

namespace EngineTask.IntegrationTests.UniTaskFlavour.Lib;

[GenerateMirror(TaskFlavour.UniTask)]
public partial class Adder
{
    public Task<int> AddAsync(int a, int b) => Task.FromResult(a + b);
}
