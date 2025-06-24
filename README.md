# Ebooks.MinVer

This is a fork of [MinVer](https://github.com/adamralph/minver).

This fork intentionally deviates from vanilla MinVer in several ways:

- New parameter `IncludeBranchName` (`boolean`). When enabled, branch names are added to the pre-release identifiers.
- New parameter `IgnoreBranchNames` (semi-colon separated list of `string`). When IncludeBranchName is enabled, this parameter can be used to ignore specific branch names in the generated version.
- Versions (Major, Minor, or Patch, depending on MinVerAutoIncrement) are incremented on every commit. This is useful for CI/CD pipelines where you want to have a unique version for every build.
- We support ignoring pre-release identifiers entirely, which is not supported in vanilla MinVer. Useful for when all packages built by a release a build server are considered RTM.
- Height calculations have been modified so that versions will use the maximum height of the two merge lines - prevents unintended version number regressions.
- The `MinVer` package is renamed to `Ebooks.MinVer` to avoid conflicts with the original package.

## Thanks 

eBooks.com would like to thank the following people for their contributions to the original MinVer project:

- [Adam Ralph](http://adamralph.com/about/)
- All other [contributors](https://github.com/adamralph/minver/graphs/contributors)
