namespace EngineTask.Generator.CustomFlavours;

// Parsed representation of a single entry under "flavours" in a
// user-supplied enginetask.json. Lives in the incremental pipeline as
// an equatable value, so changes to the JSON re-trigger generation
// while unchanged content stays cached.
public readonly record struct CustomFlavourData(
    string Id,
    string NamespaceSuffix,
    EquatableArray<CustomMapping> TypeMappings,
    EquatableArray<CustomMapping> MemberMappings);
