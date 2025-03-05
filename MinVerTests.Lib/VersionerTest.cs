using MinVerTests.Infra;
using MinVerTests.Lib.Infra;
using System.Reflection;
using Xunit;

namespace MinVerTests.Lib;
public class VersionerTest
{
    private const string MainBranchName = "main";
    private const string DevelopBranchName = "develop";
    private const string RemoteName = "origin";

    [Fact]
    public async Task GetVersion_TreeHasMergeLongerLeftPath_ReturnsExpected()
    {
        var expectedVersion = "0.0.1";
        const int expectedHeight = 4;
        const bool expectedTag = true;

        // Arrange
        var path = MethodBase.GetCurrentMethod().GetTestDirectory();

        var localRepository = Path.Combine(path, $"foo-{DevelopBranchName}");
        var otherUserRepository = Path.Combine(path, $"bar-{DevelopBranchName}");
        var remoteRepository = Path.Combine(path, $"origin-{DevelopBranchName}");

        // Create remote repository (bare repo) in a temporary directory
        _ = Directory.CreateDirectory(remoteRepository);
        await Git.Init(remoteRepository, bare: true, branchName: MainBranchName);

        // Create local repository and add remote
        _ = Directory.CreateDirectory(localRepository);
        await Git.Init(localRepository, branchName: MainBranchName);

        // Add remote pointing to the local folder
        await Git.AddRemoteAsync(localRepository, remoteRepository, RemoteName);

        // Create initial commit and push to remote
        await Sdk.CreateProject(localRepository, ensureEmptyDirectory: false);
        await Git.AddAsync(localRepository, "*");
        await Git.Commit(localRepository);
        await Git.Tag(localRepository, expectedVersion);
        await Git.PushAsync(localRepository, RemoteName, MainBranchName);

        // Create develop branch and push to remote
        await Git.CreateBranchAsync(localRepository, DevelopBranchName);
        await Git.PushAsync(localRepository, RemoteName, DevelopBranchName);

        // Make changes in remote develop (simulating another user)
        // We'll create a temporary clone for this
        _ = Directory.CreateDirectory(otherUserRepository);
        await Git.CloneAsync(otherUserRepository, remoteRepository);
        await Git.Checkout(otherUserRepository, DevelopBranchName);
        await Git.Commit(otherUserRepository); // 0.0.2
        await Git.Commit(otherUserRepository); // 0.0.3
        await Git.Commit(otherUserRepository); // 0.0.4
        await Git.PushAsync(otherUserRepository, RemoteName, DevelopBranchName);

        // Back to local repository, make a commit
        // This simulates working on our own develop branch while someone else commits changes to it on the remote
        await Git.Commit(localRepository); // 0.0.2

        // Now merge origin/develop into develop
        await Git.Fetch(localRepository, RemoteName);
        await Git.MergeAsync(localRepository, $"{RemoteName}/{DevelopBranchName}"); // 0.0.5

        // Act
        var (version, height, tag) = MinVer.Lib.Versioner.GetVersion(localRepository, string.Empty, [], NullLogger.Instance);
        Assert.Equal(expectedVersion, version.ToString());
        Assert.Equal(expectedHeight, height);
        Assert.Equal(expectedTag, tag);
    }

    [Fact]
    public async Task GetVersion_TreeHasMergeLongerRightPath_ReturnsExpected()
    {
        var expectedVersion = "0.0.1";
        const int expectedHeight = 4;
        const bool expectedTag = true;

        // Arrange
        var path = MethodBase.GetCurrentMethod().GetTestDirectory();

        var localRepository = Path.Combine(path, $"foo-{DevelopBranchName}");
        var otherUserRepository = Path.Combine(path, $"bar-{DevelopBranchName}");
        var remoteRepository = Path.Combine(path, $"origin-{DevelopBranchName}");

        // Create remote repository (bare repo) in a temporary directory
        _ = Directory.CreateDirectory(remoteRepository);
        await Git.Init(remoteRepository, bare: true, branchName: MainBranchName);

        // Create local repository and add remote
        _ = Directory.CreateDirectory(localRepository);
        await Git.Init(localRepository, branchName: MainBranchName);

        // Add remote pointing to the local folder
        await Git.AddRemoteAsync(localRepository, remoteRepository, RemoteName);

        // Create initial commit and push to remote
        await Sdk.CreateProject(localRepository, ensureEmptyDirectory: false);
        await Git.AddAsync(localRepository, "*");
        await Git.Commit(localRepository);
        await Git.Tag(localRepository, expectedVersion);
        await Git.PushAsync(localRepository, RemoteName, MainBranchName);

        // Create develop branch and push to remote
        await Git.CreateBranchAsync(localRepository, DevelopBranchName);
        await Git.PushAsync(localRepository, RemoteName, DevelopBranchName);

        // Make changes in remote develop (simulating another user)
        // We'll create a temporary clone for this
        _ = Directory.CreateDirectory(otherUserRepository);
        await Git.CloneAsync(otherUserRepository, remoteRepository);
        await Git.Checkout(otherUserRepository, DevelopBranchName);
        await Git.Commit(otherUserRepository); // 0.0.2
        await Git.PushAsync(otherUserRepository, RemoteName, DevelopBranchName);

        // Back to local repository, make a commit
        // This simulates working on our own develop branch while someone else commits changes to it on the remote
        await Git.Commit(localRepository); // 0.0.2
        await Git.Commit(localRepository); // 0.0.3
        await Git.Commit(localRepository); // 0.0.4

        // Now merge origin/develop into develop
        await Git.Fetch(localRepository, RemoteName);
        await Git.MergeAsync(localRepository, $"{RemoteName}/{DevelopBranchName}"); // 0.0.5

        // Act
        var (version, height, tag) = MinVer.Lib.Versioner.GetVersion(localRepository, string.Empty, [], NullLogger.Instance);
        Assert.Equal(expectedVersion, version.ToString());
        Assert.Equal(expectedHeight, height);
        Assert.Equal(expectedTag, tag);
    }
}
