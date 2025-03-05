using MinVerTests.Infra;
using System.Reflection;
using Xunit;

namespace MinVerTests.Packages;

public class IgnoreBranchNames
{
    [Fact]
    public async Task SingleBranchNameIgnored()
    {
        var expected = Package.WithVersion(1, 2, 4, ["alpha", "0"], 1);

        // Arrange
        var path = MethodBase.GetCurrentMethod().GetTestDirectory();
        await Sdk.CreateProject(path);

        await Git.Init(path);
        await Git.Commit(path);
        await Git.Tag(path, "1.2.3");
        await Git.Commit(path);

        // Create a feature branch and switch to it
        var branchName = "feature-xyz";
        await Git.CreateBranchAsync(path, branchName);

        // Act - run with both includeBranchName and ignoreBranchNames
        var envVars = new[]
        {
            ("MinVerIncludeBranchName", "true"),
            ("MinVerIgnoreBranchNames", branchName)
        };

        var (actual, sdkStandardOutput, _) = await Sdk.BuildProject(path, envVars: envVars);
        var (cliStandardOutput, cliStandardError) = await MinVerCli.ReadAsync(path, envVars: envVars);

        // Assert - branch name should NOT be part of version (ignored)
        Assert.Equal(expected, actual);
        Assert.Equal(expected.Version, cliStandardOutput.Trim());
    }

    [Fact]
    public async Task MultipleBranchNamesIgnored()
    {
        var expected = Package.WithVersion(1, 2, 5, ["alpha", "0"], 2);

        // Arrange
        var path = MethodBase.GetCurrentMethod().GetTestDirectory();
        await Sdk.CreateProject(path);

        await Git.Init(path);
        await Git.Commit(path);
        await Git.Tag(path, "1.2.3");
        await Git.Commit(path);
        await Git.Commit(path);

        // Create a feature branch and switch to it
        var branchName = "feature-xyz";
        await Git.CreateBranchAsync(path, branchName);

        // Act - run with both includeBranchName and ignoreBranchNames (multiple branches)
        var envVars = new[]
{
            ("MinVerIncludeBranchName", "true"),
            ("MinVerIgnoreBranchNames", $"main;master;{branchName}")
        };

        // act
        var (actual, sdkStandardOutput, _) = await Sdk.BuildProject(path, envVars: envVars);
        var (cliStandardOutput, cliStandardError) = await MinVerCli.ReadAsync(path, envVars: envVars);

        // Assert - branch name should NOT be part of version (ignored)
        Assert.Equal(expected, actual);
        Assert.Equal(expected.Version, cliStandardOutput.Trim());
    }

    [Fact]
    public async Task BranchNameNotIgnored()
    {
        var expected = Package.WithVersion(1, 2, 4, ["feature-xyz"], 1);

        // Arrange
        var path = MethodBase.GetCurrentMethod().GetTestDirectory();
        await Sdk.CreateProject(path);

        await Git.Init(path);
        await Git.Commit(path);
        await Git.Tag(path, "1.2.3");
        await Git.Commit(path);

        // Create a feature branch and switch to it
        var branchName = "feature-xyz";
        await Git.CreateBranchAsync(path, branchName);

        // Act - run with includeBranchName and different branch name in ignoreBranchNames
        var envVars = new[]
{
            ("MinVerIncludeBranchName", "true"),
            ("MinVerIgnoreBranchNames", "main;master;develop"),
            ("MinVerIgnorePreReleaseIdentifiers".ToAltCase(), "true")
        };

        var (actual, sdkStandardOutput, _) = await Sdk.BuildProject(path, envVars: envVars);
        var (cliStandardOutput, cliStandardError) = await MinVerCli.ReadAsync(path, envVars: envVars);

        // Assert - branch name should be part of version (not ignored)
        Assert.Equal(expected, actual);
        Assert.Equal(expected.Version, cliStandardOutput.Trim());
    }

    [Fact]
    public async Task EmptyIgnoreBranchNamesDoesNotExcludeAnyBranch()
    {
        var expected = Package.WithVersion(1, 2, 4, ["feature-xyz", "alpha", "0"], 1);

        // Arrange
        var path = MethodBase.GetCurrentMethod().GetTestDirectory();
        await Sdk.CreateProject(path);

        await Git.Init(path);
        await Git.Commit(path);
        await Git.Tag(path, "1.2.3");
        await Git.Commit(path);

        // Create a feature branch and switch to it
        var branchName = "feature-xyz";
        await Git.CreateBranchAsync(path, branchName);

        // Act - run with includeBranchName and empty ignoreBranchNames
        var envVars = new[]
{
            ("MinVerIncludeBranchName", "true"),
            ("MinVerIgnoreBranchNames", "")
        };

        var (actual, sdkStandardOutput, _) = await Sdk.BuildProject(path, envVars: envVars);
        var (cliStandardOutput, cliStandardError) = await MinVerCli.ReadAsync(path, envVars: envVars);

        // Assert - branch name should be part of version (not ignored)
        Assert.Equal(expected, actual);
        Assert.Equal(expected.Version, cliStandardOutput.Trim());
    }

    [Fact]
    public async Task CommandLineOptionOverridesEnvVar()
    {
        var expected = Package.WithVersion(1, 2, 4, ["feature-xyz", "alpha", "0"], 1);

        // Arrange
        var path = MethodBase.GetCurrentMethod().GetTestDirectory();
        await Sdk.CreateProject(path);

        await Git.Init(path);
        await Git.Commit(path);
        await Git.Tag(path, "1.2.3");
        await Git.Commit(path);

        // Create a feature branch and switch to it
        var branchName = "feature-xyz";
        await Git.CreateBranchAsync(path, branchName);

        // Act - set env var to ignore different branch, but command line to ignore current
        var envVars = new[]
{
            ("MinVerIncludeBranchName", "true"),
            ("MinVerIgnoreBranchNames", "other-branch") // This should be overridden
        };

        // Ignore current branch via command line
        var (actual, sdkStandardOutput, _) = await Sdk.BuildProject(path, envVars: envVars);
        var (cliStandardOutput, cliStandardError) = await MinVerCli.ReadAsync(path, envVars: envVars);

        // Assert - branch name should be ignored (command line takes precedence)
        Assert.Equal(expected, actual);
        Assert.Equal(expected.Version, cliStandardOutput.Trim());
    }

    [Fact]
    public async Task IncludeBranchNameFalseIgnoresAllBranches()
    {
        var expected = Package.WithVersion(1, 2, 4, ["alpha", "0"], 1);

        // Arrange
        var path = MethodBase.GetCurrentMethod().GetTestDirectory();
        await Sdk.CreateProject(path);

        await Git.Init(path);
        await Git.Commit(path);
        await Git.Tag(path, "1.2.3");
        await Git.Commit(path);

        // Create a feature branch and switch to it
        var branchName = "feature-xyz";
        await Git.CreateBranchAsync(path, branchName);

        // Act - set includeBranchName to false, this should take precedence
        var envVars = new[]
{
            ("MinVerIncludeBranchName", "false"),
            ("MinVerIgnoreBranchNames", "other-branch") // This should be overridden
        };

        var (actual, sdkStandardOutput, _) = await Sdk.BuildProject(path, envVars: envVars);
        var (cliStandardOutput, cliStandardError) = await MinVerCli.ReadAsync(path, envVars: envVars);

        // Assert - branch name should not be included at all
        Assert.Equal(expected, actual);
        Assert.Equal(expected.Version, cliStandardOutput.Trim());
    }
}
