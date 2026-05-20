namespace EngineTask.Generator.CustomFlavours;

// Equatable record-struct so EquatableArray<CustomMapping> can flow
// through the incremental pipeline as a cache key.
public readonly record struct CustomMapping(string From, string To);
