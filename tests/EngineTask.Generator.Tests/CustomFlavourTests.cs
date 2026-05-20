using System.Threading.Tasks;
using Xunit;

namespace EngineTask.Generator.Tests;

public class CustomFlavourTests
{
    private const string AwaitableCatalog = """
        {
          "flavours": [
            {
              "id": "Awaitable",
              "namespaceSuffix": "UnityAwaitable",
              "typeMappings": {
                "System.Threading.Tasks.Task":   "global::UnityEngine.Awaitable",
                "System.Threading.Tasks.Task`1": "global::UnityEngine.Awaitable"
              },
              "memberMappings": {
                "System.Threading.Tasks.Task.FromResult":    "global::UnityEngine.Awaitable.FromResult",
                "System.Threading.Tasks.Task.CompletedTask": "global::UnityEngine.Awaitable.CompletedTask"
              }
            }
          ]
        }
        """;

    [Fact]
    public Task CustomFlavour_FromAdditionalFile_EmitsMirror() =>
        TestHelper.VerifyMirrorWithCatalogAsync("""
            using System.Threading.Tasks;
            using EngineTask;

            namespace Sample;

            [GenerateMirror("Awaitable")]
            public partial class Calculator
            {
                public Task<int> AddAsync(int a, int b) => Task.FromResult(a + b);
            }
            """,
            AwaitableCatalog,
            flavourSuffix: "UnityAwaitable");

    [Fact]
    public Task UnknownCustomFlavour_EmitsENGTASK005() =>
        TestHelper.VerifyWithDiagnosticsAsync("""
            using System.Threading.Tasks;
            using EngineTask;

            namespace Sample;

            [GenerateMirror("NotInTheCatalog")]
            public partial class C
            {
                public Task<int> AddAsync(int a, int b) => Task.FromResult(a + b);
            }
            """);
}
