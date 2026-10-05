namespace DealerDatabase.Data;

/// <summary>
/// Resolves well-known locations relative to the solution root, so the console app,
/// the web app and the EF Core tooling all use the same data folder and database file
/// regardless of the working directory they are started from.
/// </summary>
public static class SolutionPaths
{
    private const string SolutionFileName = "DealerDatabase.sln";

    private static readonly Lazy<string> Root = new(FindSolutionRoot);

    public static string SolutionRoot => Root.Value;

    public static string DataDirectory => Path.Combine(SolutionRoot, "data");

    public static string DatabaseFile => Path.Combine(SolutionRoot, "dealers.db");

    private static string FindSolutionRoot()
    {
        foreach (var start in new[] { AppContext.BaseDirectory, Directory.GetCurrentDirectory() })
        {
            var directory = new DirectoryInfo(start);
            while (directory is not null)
            {
                if (File.Exists(Path.Combine(directory.FullName, SolutionFileName)))
                {
                    return directory.FullName;
                }

                directory = directory.Parent;
            }
        }

        throw new DirectoryNotFoundException(
            $"Could not locate {SolutionFileName} above '{AppContext.BaseDirectory}'.");
    }
}
