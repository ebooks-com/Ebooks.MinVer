using System.Diagnostics.CodeAnalysis;

namespace MinVer.Lib;

internal static class Git
{
    private static readonly char[] newLineChars = ['\r', '\n',];

    /// <summary>
    /// Finds the root directory of the Git repository that contains the specified starting path.
    /// </summary>
    /// <param name="startPath">The starting path to search from.</param>
    /// <param name="gitRepoPath">The root directory of the Git repository if found; otherwise, null.</param>
    /// <param name="log">The logger to use for logging.</param>
    /// <returns></returns>
    public static bool TryFindGitRepository(string startPath, [NotNullWhen(returnValue: true)] out string? gitRepoPath, ILogger log)
    {
        gitRepoPath = null;

        var currentPath = Path.GetFullPath(startPath);

        while (currentPath != null)
        {
            var gitPath = Path.Combine(currentPath, ".git");

            if (Directory.Exists(gitPath) || File.Exists(gitPath))
            {
                gitRepoPath = currentPath;
                return true;
            }

            var parentPath = Path.GetDirectoryName(currentPath);
            if (parentPath == currentPath)
            {
                break;
            }
            currentPath = parentPath;
        }

        return false;
    }

    /// <summary>
    /// Determines if the specified directory is part of a Git repository and that the repository is accessible.
    /// </summary>
    /// <param name="directory">The directory to check.</param>
    /// <param name="log">The logger to use for logging.</param>
    /// <returns></returns>
    public static bool IsGitTracked(string directory, ILogger log) => TryFindGitRepository(directory, out var gitRepoPath, log) && GitCommand.TryRun("status --short", gitRepoPath, log, out _);

    /// <summary>
    /// Gets the HEAD commit of the Git repository that contains the specified directory.
    /// </summary>
    /// <param name="directory">The directory to check.</param>
    /// <param name="head">The HEAD commit if found; otherwise, null.</param>
    /// <param name="log">The logger to use for logging.</param>
    /// <returns>True if the HEAD commit was successfully retrieved; otherwise, false.</returns>
    public static bool TryGetHead(string directory, [NotNullWhen(returnValue: true)] out Commit? head, ILogger log)
    {
        head = null;

        if (!TryFindGitRepository(directory, out var gitRepoPath, log))
        {
            return false;
        }

        if (!GitCommand.TryRun("log --pretty=format:\"%H %P\"", gitRepoPath, log, out var output))
        {
            return false;
        }

        var lines = output.Split(newLineChars, StringSplitOptions.RemoveEmptyEntries);

        if (lines.Length == 0)
        {
            return false;
        }

        var commits = new Dictionary<string, Commit>();

        foreach (var shas in lines
            .Select(line => line.Split(" ", StringSplitOptions.RemoveEmptyEntries)))
        {
            commits.GetOrAdd(shas[0], () => new Commit(shas[0]))
                .Parents.AddRange(shas.Skip(1).Select(parentSha => commits.GetOrAdd(parentSha, () => new Commit(parentSha))));
        }

        head = commits.Values.First();

        return true;
    }

    /// <summary>
    /// Gets all tags in the Git repository that contains the specified directory.
    /// </summary>
    /// <param name="directory"></param>
    /// <param name="log"></param>
    /// <returns></returns>
    public static IEnumerable<(string Name, string Sha)> GetTags(string directory, ILogger log) => !TryFindGitRepository(directory, out var gitRepoPath, log)
            ? []
            : (IEnumerable<(string Name, string Sha)>)(GitCommand.TryRun("show-ref --tags --dereference", gitRepoPath, log, out var output)
            ? output
                .Split(newLineChars, StringSplitOptions.RemoveEmptyEntries)
                .Select(line => line.Split(" ", 2))
                .Select(tokens => (tokens[1][10..].RemoveFromEnd("^{}"), tokens[0]))
            : []);

    /// <summary>
    /// Gets the current branch name of the Git repository that contains the specified directory.
    /// </summary>
    /// <param name="directory"></param>
    /// <param name="branchName"></param>
    /// <param name="log"></param>
    /// <returns></returns>
    public static bool TryGetCurrentBranch(string directory, [NotNullWhen(returnValue: true)] out string? branchName, ILogger log)
    {
        branchName = null;

        if (!TryFindGitRepository(directory, out var gitRepoPath, log))
        {
            _ = log.IsDebugEnabled && log.Debug("No git repository found for getting branch name.");
            return false;
        }

        if (!GitCommand.TryRun("rev-parse --abbrev-ref HEAD", gitRepoPath, log, out var output))
        {
            _ = log.IsDebugEnabled && log.Debug("Failed to get current branch name.");
            return false;
        }

        branchName = output.Trim();

        if (string.IsNullOrEmpty(branchName) || branchName == "HEAD")
        {
            _ = log.IsDebugEnabled && log.Debug("Current branch name is empty or HEAD - likely detached HEAD state.");
            return false;
        }

        _ = log.IsDebugEnabled && log.Debug($"Current branch name is '{branchName}'");
        return true;
    }

    /// <summary>
    /// Removes the specified value from the end of the string, ignoring case. If the string does not end with the specified value, the original string is returned.
    /// </summary>
    /// <param name="text"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    private static string RemoveFromEnd(this string text, string value) =>
        text.EndsWith(value, StringComparison.OrdinalIgnoreCase) ? text[..^value.Length] : text;
}
