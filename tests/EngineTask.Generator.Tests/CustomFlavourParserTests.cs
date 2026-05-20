using EngineTask.Generator.CustomFlavours;
using Xunit;

namespace EngineTask.Generator.Tests;

public class CustomFlavourParserTests
{
    [Fact]
    public void Empty_RootObject_ReturnsEmptyList()
    {
        var result = CustomFlavourParser.TryParse("{}", out var err);
        Assert.Null(err);
        Assert.NotNull(result);
        Assert.Empty(result!);
    }

    [Fact]
    public void EmptyFlavoursArray_ReturnsEmptyList()
    {
        var result = CustomFlavourParser.TryParse("""{ "flavours": [] }""", out var err);
        Assert.Null(err);
        Assert.NotNull(result);
        Assert.Empty(result!);
    }

    [Fact]
    public void SingleFlavour_RoundTripsAllFields()
    {
        const string json = """
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
                    "System.Threading.Tasks.Task.Delay": "global::UnityEngine.Awaitable.WaitForSecondsAsync"
                  }
                }
              ]
            }
            """;
        var result = CustomFlavourParser.TryParse(json, out var err);
        Assert.Null(err);
        Assert.NotNull(result);
        var flavour = Assert.Single(result!);
        Assert.Equal("Awaitable", flavour.Id);
        Assert.Equal("UnityAwaitable", flavour.NamespaceSuffix);
        Assert.Equal(2, flavour.TypeMappings.Length);
        Assert.Equal(1, flavour.MemberMappings.Length);
    }

    [Fact]
    public void MissingId_ProducesErrorMessage()
    {
        const string json = """
            { "flavours": [ { "namespaceSuffix": "X" } ] }
            """;
        var result = CustomFlavourParser.TryParse(json, out var err);
        Assert.Null(result);
        Assert.NotNull(err);
        Assert.Contains("id", err!);
    }

    [Fact]
    public void MalformedJson_ProducesErrorMessage()
    {
        var result = CustomFlavourParser.TryParse("{ this isn't json", out var err);
        Assert.Null(result);
        Assert.NotNull(err);
    }

    [Fact]
    public void MissingTypeMappings_DefaultsToEmpty()
    {
        const string json = """
            {
              "flavours": [
                { "id": "X", "namespaceSuffix": "Y" }
              ]
            }
            """;
        var result = CustomFlavourParser.TryParse(json, out var err);
        Assert.Null(err);
        var flavour = Assert.Single(result!);
        Assert.Equal(0, flavour.TypeMappings.Length);
        Assert.Equal(0, flavour.MemberMappings.Length);
    }

    [Fact]
    public void MultipleFlavours_AllParsed()
    {
        const string json = """
            {
              "flavours": [
                { "id": "A", "namespaceSuffix": "X" },
                { "id": "B", "namespaceSuffix": "Y" }
              ]
            }
            """;
        var result = CustomFlavourParser.TryParse(json, out _);
        Assert.NotNull(result);
        Assert.Equal(2, result!.Count);
        Assert.Equal("A", result[0].Id);
        Assert.Equal("B", result[1].Id);
    }

    [Fact]
    public void StringEscapes_AreHonoured()
    {
        const string json = """
            {
              "flavours": [
                { "id": "Q", "namespaceSuffix": "Z",
                  "typeMappings": { "key\twith\ttabs": "value\nwith\nnewlines" } }
              ]
            }
            """;
        var result = CustomFlavourParser.TryParse(json, out _);
        var f = Assert.Single(result!);
        Assert.Equal("key\twith\ttabs", f.TypeMappings[0].From);
        Assert.Equal("value\nwith\nnewlines", f.TypeMappings[0].To);
    }
}
