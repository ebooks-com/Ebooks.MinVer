using System.Globalization;

namespace MinVerTests.Infra;

public static class Git
{
    public static async Task EnsureEmptyRepositoryAndCommit(string path)
    {
        await EnsureEmptyRepository(path).ConfigureAwait(false);
        await Commit(path).ConfigureAwait(false);
    }

    public static Task Commit(string path, string? message = null) =>
        CommandEx.ReadLoggedAsync("git", $"commit -m '{message ?? DateTimeOffset.UtcNow.Ticks.ToString(CultureInfo.InvariantCulture)}' --allow-empty", path);

    public static Task EnsureEmptyRepository(string path)
    {
        FileSystem.EnsureEmptyDirectory(path);
        return Init(path);
    }

    public static async Task Init(string path, string? branchName = null, bool bare = false)
    {
        if (string.IsNullOrEmpty(branchName))
        {
            branchName = "main";
        }

        _ = bare
            ? await CommandEx.ReadLoggedAsync("git", "init --bare", path).ConfigureAwait(false)
            : await CommandEx.ReadLoggedAsync("git", $"init --initial-branch={branchName}", path).ConfigureAwait(false);

        _ = await CommandEx.ReadLoggedAsync("git", "config user.email johndoe@tempuri.org", path).ConfigureAwait(false);
        _ = await CommandEx.ReadLoggedAsync("git", "config user.name John Doe", path).ConfigureAwait(false);
        _ = await CommandEx.ReadLoggedAsync("git", "config commit.gpgsign false", path).ConfigureAwait(false);
    }

    public static async Task<string> GetGraph(string path) =>
        (await CommandEx.ReadLoggedAsync("git", "log --graph --pretty=format:'%d'", path).ConfigureAwait(false)).StandardOutput;

    public static Task Tag(string path, string tag) =>
        CommandEx.ReadLoggedAsync("git", $"tag {tag}", path);

    public static Task Tag(string path, string tag, string sha) =>
        CommandEx.ReadLoggedAsync("git", $"tag {tag} {sha}", path);

    public static Task AnnotatedTag(string path, string tag, string message) =>
        CommandEx.ReadLoggedAsync("git", $"tag {tag} -a -m '{message}'", path);

    public static async Task<IEnumerable<string>> GetCommitShas(string path) =>
        (await CommandEx.ReadLoggedAsync("git", "log --pretty=format:\"%H\"", path).ConfigureAwait(false)).StandardOutput.Split('\r', '\n');

    public static Task Checkout(string path, string sha) =>
        CommandEx.ReadLoggedAsync("git", $"checkout {sha}", path);

    public static Task CreateBranchAsync(string path, string name) =>
        CommandEx.ReadLoggedAsync("git", $"checkout -b {name}", path);

    public static Task AddRemoteAsync(string path, string remotePath, string name) =>
        CommandEx.ReadLoggedAsync("git", $"remote add {name} {remotePath}", path);

    public static Task PushAsync(string path, string remoteName, string branchName) =>
        CommandEx.ReadLoggedAsync("git", $"push -u \"{remoteName}\" {branchName}", path);

    public static Task SwitchBranchAsync(string path, string name) =>
        CommandEx.ReadLoggedAsync("git", $"switch {name}", path);

    public static Task MergeAsync(string path, string name) =>
        CommandEx.ReadLoggedAsync("git", $"merge {name}", path);

    public static Task CloneAsync(string path, string remoteRepository) =>
        CommandEx.ReadLoggedAsync("git", $"clone {remoteRepository} .", path);

    public static Task Fetch(string path, string remoteName) =>
        CommandEx.ReadLoggedAsync("git", $"fetch {remoteName}", path);

    public static Task AddAsync(string path, string pattern) =>
        CommandEx.ReadLoggedAsync("git", $"add {pattern}", path);
}
