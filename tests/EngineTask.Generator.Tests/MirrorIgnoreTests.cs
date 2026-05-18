using System.Threading.Tasks;
using Xunit;

namespace EngineTask.Generator.Tests;

public class MirrorIgnoreTests
{
    [Fact]
    public Task MirrorIgnore_OnMethod_SkipsThatMethodInMirror() =>
        TestHelper.VerifyEntryAsync("""
            [MirrorIgnore]
                public Task Hidden() => Task.CompletedTask;
                public Task Visible() => Task.CompletedTask;
            """);
}
