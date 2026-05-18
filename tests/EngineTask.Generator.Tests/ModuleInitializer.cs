using System.Runtime.CompilerServices;
using VerifyTests;

namespace EngineTask.Generator.Tests;

internal static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Init() => VerifySourceGenerators.Initialize();
}
