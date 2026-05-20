using System.Threading.Tasks;
using Xunit;

namespace EngineTask.Generator.Tests;

public class NestedTypeTests
{
    [Fact]
    public Task SingleLevelNested_EmitsWrappingOuterPartial() =>
        TestHelper.VerifyMirrorAsync("""
            using System.Threading.Tasks;
            using EngineTask;

            namespace Sample;

            public partial class Container
            {
                [GenerateMirror(TaskFlavour.GDTask)]
                public partial class Inner
                {
                    public Task<int> AddAsync(int a, int b) => Task.FromResult(a + b);
                }
            }
            """);

    [Fact]
    public Task TwoLevelNested_EmitsTwoWrappingOuterPartials() =>
        TestHelper.VerifyMirrorAsync("""
            using System.Threading.Tasks;
            using EngineTask;

            namespace Sample;

            public partial class Outer
            {
                public partial class Middle
                {
                    [GenerateMirror(TaskFlavour.GDTask)]
                    public partial class Inner
                    {
                        public Task<int> AddAsync(int a, int b) => Task.FromResult(a + b);
                    }
                }
            }
            """);

    // Two distinct nested classes with the same inner name — pre-Phase-7.4
    // both mirrored to `Sample.GDTask.Inner` and collided on file name.
    // With the type path in the hint name, they get distinct generated files.
    [Fact]
    public Task TwoDifferentOuterParents_SameInnerName_BothMirror() =>
        TestHelper.VerifyAllMirrorsAsync("""
            using System.Threading.Tasks;
            using EngineTask;

            namespace Sample;

            public partial class Alpha
            {
                [GenerateMirror(TaskFlavour.GDTask)]
                public partial class Inner
                {
                    public Task<int> A() => Task.FromResult(1);
                }
            }

            public partial class Beta
            {
                [GenerateMirror(TaskFlavour.GDTask)]
                public partial class Inner
                {
                    public Task<int> B() => Task.FromResult(2);
                }
            }
            """);
}
