using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace EngineTask.Generator;

internal readonly record struct LocationInfo(
    string FilePath,
    TextSpan TextSpan,
    LinePositionSpan LineSpan)
{
    public Location ToLocation() => Location.Create(FilePath, TextSpan, LineSpan);

    public static LocationInfo? Create(Location? location)
    {
        if (location is null) return null;
        if (location.SourceTree is null) return null;
        return new LocationInfo(
            location.SourceTree.FilePath ?? string.Empty,
            location.SourceSpan,
            location.GetLineSpan().Span);
    }
}
