using Microsoft.CodeAnalysis;

namespace EngineTask.Generator;

internal readonly record struct DiagnosticInfo(
    string DescriptorId,
    EquatableArray<string> Arguments,
    LocationInfo? Location)
{
    public static DiagnosticInfo Create(string descriptorId, Location? location, params string[] arguments) =>
        new(descriptorId, new EquatableArray<string>(arguments), LocationInfo.Create(location));
}
