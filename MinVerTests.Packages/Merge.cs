using MinVerTests.Infra;
using System.Reflection;
using Xunit;

namespace MinVerTests.Packages;
public class Merge
{
    [Fact]
    public async Task MergeOrigin_LeftTreeLongerReturnsExpectedVersion()
    {
        const string mainBranchName = "main";
        const string developBranchName = "develop";
        const string remoteName = "origin";
        var expected = Package.WithVersion(0, 0, 3, [developBranchName], 2);

        // Arrange
        var path = MethodBase.GetCurrentMethod().GetTestDirectory();

        var localRepository = Path.Combine(path, $"foo-{developBranchName}");
        var otherUserRepository = Path.Combine(path, $"bar-{developBranchName}");
        var remoteRepository = Path.Combine(path, $"origin-{developBranchName}");

        // Create remote repository (bare repo) in a temporary directory
        _ = Directory.CreateDirectory(remoteRepository);
        await Git.Init(remoteRepository, bare: true, branchName: mainBranchName);

        // Create local repository and add remote
        _ = Directory.CreateDirectory(localRepository);
        await Git.Init(localRepository, branchName: mainBranchName);

        // Add remote pointing to the local folder
        await Git.AddRemoteAsync(localRepository, remoteRepository, remoteName);

        // Create initial commit and push to remote
        await Sdk.CreateProject(localRepository, ensureEmptyDirectory: false);
        await Git.AddAsync(localRepository, "*");
        await Git.Commit(localRepository);
        await Git.Tag(localRepository, "0.0.1");
        await Git.PushAsync(localRepository, remoteName, mainBranchName);

        // Create develop branch and push to remote
        await Git.CreateBranchAsync(localRepository, developBranchName);
        await Git.PushAsync(localRepository, remoteName, developBranchName);

        // Make changes in remote develop (simulating another user)
        // We'll create a temporary clone for this
        _ = Directory.CreateDirectory(otherUserRepository);
        await Git.CloneAsync(otherUserRepository, remoteRepository);
        await Git.Checkout(otherUserRepository, developBranchName);
        await Git.Commit(otherUserRepository); // 0.0.2
        await Git.PushAsync(otherUserRepository, remoteName, developBranchName);

        // Back to local repository, make a commit
        // This simulates working on our own develop branch while someone else commits changes to it on the remote
        await Git.Commit(localRepository); // 0.0.2

        // Now merge origin/develop into develop
        await Git.Fetch(localRepository, remoteName);
        await Git.MergeAsync(localRepository, $"{remoteName}/{developBranchName}"); // 0.0.3

        // Act - run with includeBranchName and different branch name in ignoreBranchNames
        var envVars = new[]
        {
            ("MinVerIncludeBranchName", "true"),
            ("MinVerIgnorePreReleaseIdentifiers".ToAltCase(), "true")
        };

        var (actual, sdkStandardOutput, _) = await Sdk.BuildProject(localRepository, envVars: envVars);
        var (cliStandardOutput, cliStandardError) = await MinVerCli.ReadAsync(localRepository, envVars: envVars);

        // Assert - branch name should be part of version (not ignored)
        Assert.Equal(expected, actual);
        Assert.Equal(expected.Version, cliStandardOutput.Trim());
    }

    [Fact]
    public async Task MergeOrigin_RightTreeLongerReturnsExpectedVersion()
    {
        const string mainBranchName = "main";
        const string developBranchName = "develop";
        const string remoteName = "origin";
        var expected = Package.WithVersion(0, 0, 5, [developBranchName], 4);

        // Arrange
        var path = MethodBase.GetCurrentMethod().GetTestDirectory();

        var localRepository = Path.Combine(path, $"foo-{developBranchName}");
        var otherUserRepository = Path.Combine(path, $"bar-{developBranchName}");
        var remoteRepository = Path.Combine(path, $"origin-{developBranchName}");

        // Create remote repository (bare repo) in a temporary directory
        _ = Directory.CreateDirectory(remoteRepository);
        await Git.Init(remoteRepository, bare: true, branchName: mainBranchName);

        // Create local repository and add remote
        _ = Directory.CreateDirectory(localRepository);
        await Git.Init(localRepository, branchName: mainBranchName);

        // Add remote pointing to the local folder
        await Git.AddRemoteAsync(localRepository, remoteRepository, remoteName);

        // Create initial commit and push to remote
        await Sdk.CreateProject(localRepository, ensureEmptyDirectory: false);
        await Git.AddAsync(localRepository, "*");
        await Git.Commit(localRepository);
        await Git.Tag(localRepository, "0.0.1");
        await Git.PushAsync(localRepository, remoteName, mainBranchName);

        // Create develop branch and push to remote
        await Git.CreateBranchAsync(localRepository, developBranchName);
        await Git.PushAsync(localRepository, remoteName, developBranchName);

        // Make changes in remote develop (simulating another user)
        // We'll create a temporary clone for this
        _ = Directory.CreateDirectory(otherUserRepository);
        await Git.CloneAsync(otherUserRepository, remoteRepository);
        await Git.Checkout(otherUserRepository, developBranchName);
        await Git.Commit(otherUserRepository); // 0.0.2
        await Git.Commit(otherUserRepository); // 0.0.3
        await Git.Commit(otherUserRepository); // 0.0.4
        await Git.PushAsync(otherUserRepository, remoteName, developBranchName);

        // Back to local repository, make a commit
        // This simulates working on our own develop branch while someone else commits changes to it on the remote
        await Git.Commit(localRepository); // 0.0.2

        // Now merge origin/develop into develop
        await Git.Fetch(localRepository, remoteName);
        await Git.MergeAsync(localRepository, $"{remoteName}/{developBranchName}"); // 0.0.5

        // Act - run with includeBranchName and different branch name in ignoreBranchNames
        var envVars = new[]
        {
            ("MinVerIncludeBranchName", "true"),
            ("MinVerIgnorePreReleaseIdentifiers".ToAltCase(), "true")
        };

        var (actual, sdkStandardOutput, _) = await Sdk.BuildProject(localRepository, envVars: envVars);
        var (cliStandardOutput, cliStandardError) = await MinVerCli.ReadAsync(localRepository, envVars: envVars);

        // Assert - branch name should be part of version (not ignored)
        Assert.Equal(expected, actual);
        Assert.Equal(expected.Version, cliStandardOutput.Trim());
    }

    [Fact]
    public async Task MergeFeature_RightTreeLongerReturnsExpectedVersion()
    {
        const string mainBranchName = "main";
        const string developBranchName = "develop";
        const string remoteName = "origin";
        const string featureBranchName = "feature";

        var expected = Package.WithVersion(0, 0, 7, [featureBranchName], 6);

        // Arrange
        var path = MethodBase.GetCurrentMethod().GetTestDirectory();

        var localRepository = Path.Combine(path, $"local-{developBranchName}");
        var remoteRepository = Path.Combine(path, $"origin-{developBranchName}");

        // Create remote repository (bare repo) in a temporary directory
        _ = Directory.CreateDirectory(remoteRepository);
        await Git.Init(remoteRepository, bare: true, branchName: mainBranchName);

        // Create local repository and add remote
        _ = Directory.CreateDirectory(localRepository);
        await Git.Init(localRepository, branchName: mainBranchName);

        // Add remote pointing to the local folder
        await Git.AddRemoteAsync(localRepository, remoteRepository, remoteName);

        // Create initial commit and push to remote
        await Sdk.CreateProject(localRepository, ensureEmptyDirectory: false);
        await Git.AddAsync(localRepository, "*");
        await Git.Commit(localRepository);
        await Git.Tag(localRepository, "0.0.1");
        await Git.PushAsync(localRepository, remoteName, mainBranchName);

        // Create develop branch and push to remote
        await Git.CreateBranchAsync(localRepository, developBranchName);
        await Git.PushAsync(localRepository, remoteName, developBranchName);

        // Make changes in local develop
        await Git.Commit(localRepository); // 0.0.2
        await Git.Commit(localRepository); // 0.0.3

        // Branch, then make changes in both
        await Git.CreateBranchAsync(localRepository, featureBranchName);

        await Git.Commit(localRepository); // 0.0.4 in feature branch

        // Switch back to develop
        await Git.Checkout(localRepository, developBranchName);

        await Git.Commit(localRepository); // 0.0.4 in develop branch
        await Git.Commit(localRepository); // 0.0.5 in develop branch
        await Git.Commit(localRepository); // 0.0.6 in develop branch

        // Switch to feature branch
        await Git.Checkout(localRepository, featureBranchName);

        // Then merge develop into feature
        await Git.MergeAsync(localRepository, developBranchName);

        // Act - run with includeBranchName and different branch name in ignoreBranchNames
        var envVars = new[]
        {
            ("MinVerIncludeBranchName", "true"),
            ("MinVerIgnorePreReleaseIdentifiers".ToAltCase(), "true")
        };

        var (actual, sdkStandardOutput, _) = await Sdk.BuildProject(localRepository, envVars: envVars);
        var (cliStandardOutput, cliStandardError) = await MinVerCli.ReadAsync(localRepository, envVars: envVars);

        // Assert - branch name should be part of version (not ignored)
        Assert.Equal(expected, actual);
        Assert.Equal(expected.Version, cliStandardOutput.Trim());
    }
}
