namespace EngineTask.Generator.CustomFlavours;

// Equatable bundle of parsed flavours + parse errors, produced by the
// AdditionalTextsProvider stage and consumed by both the transform
// stage (to resolve flavour IDs) and the source-output stage (to
// surface parse errors as ENGTASK006).
public readonly record struct FlavourCatalog(
    EquatableArray<CustomFlavourData> Flavours,
    EquatableArray<FlavourParseError> Errors)
{
    public static FlavourCatalog Empty { get; } = new(
        EquatableArray<CustomFlavourData>.Empty,
        EquatableArray<FlavourParseError>.Empty);

    public CustomFlavourData? Find(string id)
    {
        foreach (var flavour in Flavours)
        {
            if (flavour.Id == id) return flavour;
        }
        return null;
    }
}

public readonly record struct FlavourParseError(string FilePath, string Message);
