namespace EngineTask.Sample.Core;

internal static class MirrorProbe
{
    public static global::GodotTask.GDTask<int> Add(int a, int b)
        => new GDTask.Calculator().AddAsync(a, b);
}
