using System.Reflection;
using MinVerTests.Infra;
using Xunit;

namespace MinVerTests.Packages;

public static class SubfolderScenarios
{
    [Fact]
    public static async Task BuildFromSubfolder_WithGitInParent_FindsCorrectVersion()
    {
        // arrange
        var basePath = MethodBase.GetCurrentMethod().GetTestDirectory();
        await Sdk.CreateProject(basePath);

        await Git.Init(basePath);
        await Git.Commit(basePath);
        await Git.Tag(basePath, "1.2.3");
        await Git.Commit(basePath);

        var subfolderPath = Path.Combine(basePath, "src");
        await Sdk.CreateProject(subfolderPath);

        var expected = Package.WithVersion(1, 2, 4, ["alpha", "0", "1",]);

        // act - build from subfolder
        var (actual, sdkStandardOutput, _) = await Sdk.BuildProject(subfolderPath);
        var (cliStandardOutput, _) = await MinVerCli.ReadAsync(subfolderPath);

        // assert
        Assert.Equal(expected, actual);
        Assert.DoesNotContain("MINVER1001", sdkStandardOutput, StringComparison.Ordinal);
        Assert.Equal(expected.Version, cliStandardOutput.Trim());
    }

    [Fact]
    public static async Task BuildFromNestedSubfolder_WithGitInRoot_FindsCorrectVersion()
    {
        // arrange
        var basePath = MethodBase.GetCurrentMethod().GetTestDirectory();
        await Sdk.CreateProject(basePath);

        await Git.Init(basePath);
        await Git.Commit(basePath);
        await Git.Tag(basePath, "2.0.0");
        await Git.Commit(basePath);
        await Git.Commit(basePath);

        var nestedPath = Path.Combine(basePath, "projects", "myproject", "src");
        await Sdk.CreateProject(nestedPath);

        var expected = Package.WithVersion(2, 0, 2, ["alpha", "0", "2",]);

        // act - build from nested subfolder
        var (actual, sdkStandardOutput, _) = await Sdk.BuildProject(nestedPath);
        var (cliStandardOutput, _) = await MinVerCli.ReadAsync(nestedPath);

        // assert
        Assert.Equal(expected, actual);
        Assert.DoesNotContain("MINVER1001", sdkStandardOutput, StringComparison.Ordinal);
        Assert.Equal(expected.Version, cliStandardOutput.Trim());
    }

    [Fact]
    public static async Task BuildFromSubfolder_WithBranchName_IncludesBranchInVersion()
    {
        // arrange
        var basePath = MethodBase.GetCurrentMethod().GetTestDirectory();
        await Sdk.CreateProject(basePath);

        await Git.Init(basePath);
        await Git.Commit(basePath);
        await Git.Tag(basePath, "1.0.0");
        await Git.CreateBranchAsync(basePath, "feature/awesome-feature");
        await Git.Commit(basePath);

        var subfolderPath = Path.Combine(basePath, "components");
        await Sdk.CreateProject(subfolderPath);

        var expected = Package.WithVersion(1, 0, 1, ["feature-awesome-feature", "alpha", "0", "1",]);

        // act - build from subfolder with branch name inclusion
        var envVar = ("MinVerIncludeBranchName".ToAltCase(), "true");
        var (actual, _, _) = await Sdk.BuildProject(subfolderPath, envVars: envVar);
        var (cliStandardOutput, _) = await MinVerCli.ReadAsync(subfolderPath, args: "--include-branch-name");

        // assert
        Assert.Equal(expected, actual);
        Assert.Equal(expected.Version, cliStandardOutput.Trim());
    }
}
