using System.Threading.Tasks;
using EngineTask;

namespace EngineTask.Sample.Core;

[GenerateMirror(TaskFlavour.GDTask)]
public partial class Calculator
{
    public Task<int> AddAsync(int a, int b) => Task.FromResult(a + b);
}
