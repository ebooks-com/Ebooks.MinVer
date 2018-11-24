namespace MinVer.Lib
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using LibGit2Sharp;

    public static class Versioner
    {
        public static Version GetVersion(Repository repo, string tagPrefix, MajorMinor range, string buildMetadata, ILogger log)
        {
            if (log.IsDebugEnabled)
            {
                log.Debug($"MinVer {typeof(Versioner).Assembly.GetCustomAttributes<AssemblyInformationalVersionAttribute>().Single().InformationalVersion}");
            }

            var commit = repo.Commits.FirstOrDefault();

            if (commit == default)
            {
                log.Info("No commits found. Using default version.");
                return new Version(range?.Major ?? 0, range?.Minor ?? 0, buildMetadata);
            }

            var tagsAndVersions = repo.Tags
                .Select(tag => (tag, Version.ParseOrDefault(tag.FriendlyName, tagPrefix)))
                .OrderByDescending(tagAndVersion => tagAndVersion.Item2)
                .ToList();

            var commitsChecked = new HashSet<string>();
            var count = 0;
            var height = 0;
            var candidates = new List<Candidate>();
            var commitsToCheck = new Stack<(Commit, int)>();
            Commit previousCommit = default;

            while (true)
            {
                if (commitsChecked.Add(commit.Sha))
                {
                    ++count;

                    var (tag, commitVersion) = tagsAndVersions.FirstOrDefault(tagAndVersion => tagAndVersion.tag.Target.Sha == commit.Sha);

                    if (commitVersion != default)
                    {
                        var candidate = new Candidate { Commit = commit.Sha, Height = height, Tag = tag.FriendlyName, Version = commitVersion, };

                        if (log.IsTraceEnabled)
                        {
                            log.Trace($"Found version tag {candidate}");
                        }

                        candidates.Add(candidate);
                    }
                    else
                    {
                        if (tag != default)
                        {
                            var candidate = new Candidate { Commit = commit.Sha, Height = height, Tag = tag.FriendlyName, Version = default, };

                            if (log.IsTraceEnabled)
                            {
                                log.Trace($"Found non-version tag {candidate}");
                            }

                            candidates.Add(candidate);
                        }

                        var parentIndex = 0;
                        Commit firstParent = default;

                        foreach (var parent in commit.Parents.Reverse())
                        {
                            if (log.IsTraceEnabled)
                            {
                                switch (parentIndex)
                                {
                                    case 0:
                                        firstParent = parent;
                                        break;
                                    case 1:
                                        log.Trace($"History diverges from {commit.Sha} to:");
                                        log.Trace($"  {firstParent.Sha}");
                                        goto case default;
                                    default:
                                        log.Trace($"  {parent.Sha}");
                                        break;
                                }

                                ++parentIndex;
                            }

                            commitsToCheck.Push((parent, height + 1));
                        }

                        if (commitsToCheck.Count == 0 || commitsToCheck.Peek().Item2 <= height)
                        {
                            var candidate = new Candidate { Commit = commit.Sha, Height = height, Tag = default, Version = new Version(), };

                            if (log.IsTraceEnabled)
                            {
                                log.Trace($"Found root commit {candidate}");
                            }

                            candidates.Add(candidate);
                        }
                    }
                }
                else
                {
                    if (log.IsTraceEnabled)
                    {
                        log.Trace($"History converges from {previousCommit.Sha} to {commit.Sha}.");
                    }
                }

                if (commitsToCheck.Count == 0)
                {
                    break;
                }

                if (log.IsTraceEnabled)
                {
                    previousCommit = commit;
                }

                (commit, height) = commitsToCheck.Pop();
            }

            log.Debug($"{count:N0} commits checked.");

            var orderedCandidates = candidates.OrderBy(candidate => candidate.Version).ToList();

            var tagWidth = log.IsDebugEnabled ? orderedCandidates.Max(candidate => candidate.Tag?.Length ?? 2) : 0;
            var versionWidth = log.IsDebugEnabled ? orderedCandidates.Max(candidate => candidate.Version?.ToString().Length ?? 4) : 0;
            var heightWidth = log.IsDebugEnabled ? orderedCandidates.Max(candidate => candidate.Height).ToString().Length : 0;

            if (log.IsDebugEnabled)
            {
                foreach (var candidate in orderedCandidates.Take(orderedCandidates.Count - 1))
                {
                    log.Debug($"Ignoring {candidate.ToString(tagWidth, versionWidth, heightWidth)}.");
                }
            }

            var selectedCandidate = orderedCandidates.Last();
            log.Info($"Using{(log.IsDebugEnabled && orderedCandidates.Count > 1 ? "    " : " ")}{selectedCandidate.ToString(tagWidth, versionWidth, heightWidth)}.");

            var baseVersion = range != default && selectedCandidate.Version.IsBefore(range.Major, range.Minor)
                ? new Version(range.Major, range.Minor)
                : selectedCandidate.Version;

            if (baseVersion != selectedCandidate.Version)
            {
                log.Info($"Bumping version to {baseVersion} to satisfy {range} range.");
            }

            var calculatedVersion = baseVersion.WithHeight(selectedCandidate.Height).AddBuildMetadata(buildMetadata);
            log.Debug($"Calculated version {calculatedVersion}.");

            return calculatedVersion;
        }

        private class Candidate
        {
            public string Commit { get; set; }

            public int Height { get; set; }

            public string Tag { get; set; }

            public Version Version { get; set; }

            public override string ToString() => this.ToString(0, 0, 0);

            public string ToString(int tagWidth, int versionWidth, int heightWidth) =>
                $"{{ {nameof(this.Commit)}: {this.Commit.Substring(0, 7)}, {nameof(this.Tag)}: {$"{(this.Tag == default ? "null" : $"'{this.Tag}'")},".PadRight(tagWidth + 3)} {nameof(this.Version)}: {$"{this.Version?.ToString() ?? "null"},".PadRight(versionWidth + 1)} {nameof(this.Height)}: {this.Height.ToString().PadLeft(heightWidth)} }}";
        }
    }
}
