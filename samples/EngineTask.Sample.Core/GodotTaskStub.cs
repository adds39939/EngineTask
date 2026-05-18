// Phase 1 stand-in for the GDTask NuGet package. Provides only the type
// identities the generator emits in mirror return positions. Phase 2 replaces
// this file with a real PackageReference to GDTask.Nuget.
namespace GodotTask
{
    public class GDTask { }

    public class GDTask<T> { }
}
