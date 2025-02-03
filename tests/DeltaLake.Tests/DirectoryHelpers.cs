namespace DeltaLake.Tests;

internal static class DirectoryHelpers
{
    public static DirectoryInfo CreateTempSubdirectory()
    {
#if NETCOREAPP
        return Directory.CreateTempSubdirectory();
#else
        var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempPath);
        return new DirectoryInfo(tempPath);
#endif
    }
}
