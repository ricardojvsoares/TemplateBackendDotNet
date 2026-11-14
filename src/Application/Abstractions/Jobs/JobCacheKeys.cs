namespace Application.Abstractions.Jobs;

public static class JobCacheKeys
{
    public static string Occurrence(string name, string occurrence) =>
        $"jobs:{name}:{occurrence}";
}
