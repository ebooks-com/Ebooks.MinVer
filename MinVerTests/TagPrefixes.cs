namespace MinVerTests
{
    using LibGit2Sharp;
    using MinVer.Lib;
    using MinVerTests.Infra;
    using Xbehave;
    using Xunit;

    using static MinVerTests.Infra.Git;
    using static MinVerTests.Infra.FileSystem;

    using Version = MinVer.Lib.Version;

    public static class TagPrefixes
    {
        [Scenario]
        [Example("1.2.3", default, "1.2.3")]
        [Example("2.3.4", "", "2.3.4")]
        [Example("v3.4.5", "v", "3.4.5")]
        [Example("version5.6.7", "version", "5.6.7")]
        public static void TagPrefix(string tag, string prefix, string expectedVersion, string path, Repository repo, Version actualVersion)
        {
            $"Given a git repository with a commit in '{path = GetScenarioDirectory($"tag-prefixes-{tag}")}'"
                .x(c => repo = EnsureEmptyRepositoryAndCommit(path).Using(c));

            $"And the commit is tagged '{tag}'"
                .x(() => repo.ApplyTag(tag));

            $"When the version is determined using the tag prefix '{prefix}'"
                .x(() => actualVersion = Versioner.GetVersion(repo, prefix, default, default, new TestLogger()));

            $"Then the version is '{expectedVersion}'"
                .x(() => Assert.Equal(expectedVersion, actualVersion.ToString()));
        }
    }
}
