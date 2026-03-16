using System.Text.RegularExpressions;

namespace TeamSearch.Infrastructure.Utilities;

public static class SqliteConnectionStringResolver
{
    // If the connection string contains a Data Source that is a relative path,
    // resolve it to an absolute path under the TeamSearch.Server project folder
    // (so all tools use the same DB file location).
    public static string Resolve(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString)) return connectionString;

        // Match patterns like: Data Source=teamsearch.db; or Data Source=./teamsearch.db
        var m = Regex.Match(connectionString, @"Data Source\s*=\s*([^;]+)", RegexOptions.IgnoreCase);
        if (!m.Success) return connectionString;

        var dataSource = m.Groups[1].Value.Trim().Trim('"');
        if (Path.IsPathRooted(dataSource)) return connectionString;

        // Resolve relative to TeamSearch.Server project folder
        var repoRoot = AppContext.BaseDirectory;
        // Walk up until we find the solution folder containing TeamSearch.Server
        var dir = new DirectoryInfo(repoRoot);
        while (dir != null && !dir.EnumerateDirectories("TeamSearch.Server").Any()) dir = dir.Parent;

        var serverDir = dir?.EnumerateDirectories("TeamSearch.Server").FirstOrDefault()?.FullName
                        ?? Path.Combine(Directory.GetCurrentDirectory(), "TeamSearch.Server");

        var resolved = Path.Combine(serverDir, dataSource);
        var resolvedConn = Regex.Replace(connectionString, @"Data Source\s*=\s*([^;]+)", $"Data Source={resolved}",
            RegexOptions.IgnoreCase);
        return resolvedConn;
    }
}