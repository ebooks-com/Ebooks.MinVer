using MinVer.Lib;
using MinVerTests.Infra;
using MinVerTests.Lib.Infra;
using System.Reflection;
using Xunit;
using static MinVerTests.Infra.FileSystem;
using static MinVerTests.Infra.Git;
using static SimpleExec.Command;

namespace MinVerTests.Lib;

public static class Versions
{
    [Fact]
    public static async Task RepoWithHistory()
    {
        // arrange
        var historicalCommands =
            @"
git commit --allow-empty -m '.'
git commit --allow-empty -m '.'
git commit --allow-empty -m '.'
git tag 0.1.0-alpha.1
git commit --allow-empty -m '.'
git commit --allow-empty -m '.'
git tag 0.1.0
git commit --allow-empty -m '.'
git commit --allow-empty -m '.'
git tag 0.2.0-beta.1
git commit --allow-empty -m '.'
git commit --allow-empty -m '.'
git tag 0.2.0
git commit --allow-empty -m '.'
git commit --allow-empty -m '.'
git tag 1.0.0-alpha.1
git commit --allow-empty -m '.'
git commit --allow-empty -m '.'
git tag 1.0.0-rc.1
git tag 1.0.0
git checkout -b foo
git commit --allow-empty -m '.'
git commit --allow-empty -m '.'
git commit --allow-empty -m '.'
git tag 1.1.0-alpha.1
git commit --allow-empty -m '.'
git commit --allow-empty -m '.'
git tag 1.2.0
git commit --allow-empty -m '.'
git checkout main
git commit --allow-empty -m '.'
git commit --allow-empty -m '.'
git commit --allow-empty -m '.'
git tag 1.3.0-alpha.1
git commit --allow-empty -m '.'
git merge foo --no-edit
git commit --allow-empty -m '.'
git tag 1.3.0-beta.2
git tag 1.3.0-beta.10
git commit --allow-empty -m '.'
git commit --allow-empty -m '.'
git tag 1.3.0-rc.1
git tag 1.3.0 -a -m '.'
";

        var path = MethodBase.GetCurrentMethod().GetTestDirectory();

        await EnsureEmptyRepositoryAndCommit(path);

        foreach (var command in historicalCommands.ToNonEmptyLines())
        {
            var nameAndArgs = command.Split(" ", 2);
            _ = await ReadAsync(nameAndArgs[0], nameAndArgs[1], path);
            await Task.Delay(200);
        }

        var log = new TestLogger();

        // act
        var versionCounts = new Dictionary<string, int>();
        foreach (var sha in await GetCommitShas(path))
        {
            await Checkout(path, sha);

            var version = Versioner.GetVersion(path, "", MajorMinor.Default, "", default, PreReleaseIdentifiers.Default, false, log);
            var versionString = version.ToString();
            var tagName = $"v/{versionString}";

            _ = versionCounts.TryGetValue(versionString, out var oldVersionCount);
            var versionCount = oldVersionCount + 1;
            versionCounts[versionString] = versionCount;

            tagName = versionCount > 1 ? $"v({versionCount})/{versionString}" : tagName;

            await Tag(path, tagName, sha);
        }

        await Checkout(path, "main");

        await File.WriteAllTextAsync(Path.Combine(path, "log.txt"), log.ToString());

        // assert
        await AssertFile.Contains("../../../versions.txt", await GetGraph(path));
    }

    [Fact]
    public static async Task EmptyRepo()
    {
        // arrange
        var path = MethodBase.GetCurrentMethod().GetTestDirectory();
        await EnsureEmptyRepository(path);

        // act
        var version = Versioner.GetVersion(path, "", MajorMinor.Default, "", default, PreReleaseIdentifiers.Default, false, NullLogger.Instance);

        // assert
        Assert.Equal("0.0.0-alpha.0", version.ToString());
    }

    [Fact]
    public static void NoRepo()
    {
        // arrange
        var path = MethodBase.GetCurrentMethod().GetTestDirectory();
        EnsureEmptyDirectory(path);

        // act
        var version = Versioner.GetVersion(path, "", MajorMinor.Default, "", default, PreReleaseIdentifiers.Default, false, NullLogger.Instance);

        // assert
        Assert.Equal("0.0.0-alpha.0", version.ToString());
    }

    [Fact]
    public static void TryFindGitRepository_CurrentDirectoryHasGit_ReturnsCurrentDirectory()
    {
        // arrange
        var path = MethodBase.GetCurrentMethod().GetTestDirectory();
        EnsureEmptyDirectory(path);
        var gitPath = Path.Combine(path, ".git");
        _ = Directory.CreateDirectory(gitPath);

        // act
        var result = MinVer.Lib.Git.TryFindGitRepository(path, out var gitRepoPath, NullLogger.Instance);

        // assert
        Assert.True(result);
        Assert.Equal(Path.GetFullPath(path), gitRepoPath);
    }

    [Fact]
    public static void TryFindGitRepository_ParentDirectoryHasGit_ReturnsParentDirectory()
    {
        // arrange
        var basePath = MethodBase.GetCurrentMethod().GetTestDirectory();
        EnsureEmptyDirectory(basePath);

        var gitPath = Path.Combine(basePath, ".git");
        _ = Directory.CreateDirectory(gitPath);

        var subfolderPath = Path.Combine(basePath, "subfolder");
        _ = Directory.CreateDirectory(subfolderPath);

        // act
        var result = MinVer.Lib.Git.TryFindGitRepository(subfolderPath, out var gitRepoPath, NullLogger.Instance);

        // assert
        Assert.True(result);
        Assert.Equal(Path.GetFullPath(basePath), gitRepoPath);
    }

    [Fact]
    public static void TryFindGitRepository_NestedSubfoldersWithGitInRoot_ReturnsRootDirectory()
    {
        // arrange
        var basePath = MethodBase.GetCurrentMethod().GetTestDirectory();
        EnsureEmptyDirectory(basePath);

        var gitPath = Path.Combine(basePath, ".git");
        _ = Directory.CreateDirectory(gitPath);

        var deepSubfolderPath = Path.Combine(basePath, "level1", "level2", "level3");
        _ = Directory.CreateDirectory(deepSubfolderPath);

        // act
        var result = MinVer.Lib.Git.TryFindGitRepository(deepSubfolderPath, out var gitRepoPath, NullLogger.Instance);

        // assert
        Assert.True(result);
        Assert.Equal(Path.GetFullPath(basePath), gitRepoPath);
    }

    [Fact]
    public static void TryFindGitRepository_NoGitRepository_ReturnsFalse()
    {
        // arrange
        var path = MethodBase.GetCurrentMethod().GetTestDirectory();
        EnsureEmptyDirectory(path);

        // act
        var result = MinVer.Lib.Git.TryFindGitRepository(path, out var gitRepoPath, NullLogger.Instance);

        // assert
        Assert.False(result);
        Assert.Null(gitRepoPath);
    }

    [Fact]
    public static async Task TryFindGitRepository_GitFileInsteadOfDirectory_ReturnsTrue()
    {
        // arrange  
        var path = MethodBase.GetCurrentMethod().GetTestDirectory();
        EnsureEmptyDirectory(path);

        var gitFilePath = Path.Combine(path, ".git");
        await File.WriteAllTextAsync(gitFilePath, "gitdir: ../../../.git");

        // act
        var result = MinVer.Lib.Git.TryFindGitRepository(path, out var gitRepoPath, NullLogger.Instance);

        // assert
        Assert.True(result);
        Assert.Equal(Path.GetFullPath(path), gitRepoPath);
    }
}
