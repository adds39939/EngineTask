using System.Threading.Tasks;
using Xunit;

namespace EngineTask.Generator.Tests;

public class EdgeCaseTests
{
    [Fact]
    public Task GenericMethod_PreservesTypeParameter() =>
        TestHelper.VerifyEntryAsync(
            "public Task<T> WrapAsync<T>(T value) => Task.FromResult(value);");

    [Fact]
    public Task GenericMethodWithClassConstraint_PreservesConstraintClause() =>
        TestHelper.VerifyEntryAsync(
            "public Task<T> WrapAsync<T>(T value) where T : class => Task.FromResult(value);");

    [Fact]
    public Task GenericMethodWithStructAndNewConstraint_PreservesConstraintClause() =>
        TestHelper.VerifyEntryAsync(
            "public Task<T> CreateAsync<T>() where T : struct, new() => Task.FromResult(new T());");

    [Fact]
    public Task MultiStatementBody_PreservesStatements() =>
        TestHelper.VerifyEntryAsync("""
            public async Task<int> ComputeAsync()
                {
                    var x = 1;
                    var y = await Task.FromResult(2);
                    return x + y;
                }
            """);

    [Fact]
    public Task CancellationTokenParameter_PassesThroughUnchanged() =>
        TestHelper.VerifyEntryAsync("""
            public Task DoAsync(System.Threading.CancellationToken ct)
                {
                    return Task.CompletedTask;
                }
            """);
}
