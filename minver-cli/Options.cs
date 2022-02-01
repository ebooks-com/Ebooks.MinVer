using System;
using System.Diagnostics.CodeAnalysis;
#if MINVER_CLI
using System.Linq;
#endif
using MinVer.Lib;

namespace MinVer
{
    internal class Options
    {
        public Options(
            VersionPart? autoIncrement,
            string? buildMeta,
            string? defaultPreReleasePhase,
            MajorMinor? minMajorMinor,
            string? tagPrefix,
            Verbosity? verbosity,
            Lib.Version? versionOverride)
        {
            this.AutoIncrement = autoIncrement;
            this.BuildMeta = buildMeta;
            this.DefaultPreReleasePhase = defaultPreReleasePhase;
            this.MinMajorMinor = minMajorMinor;
            this.TagPrefix = tagPrefix;
            this.Verbosity = verbosity;
            this.VersionOverride = versionOverride;
        }

#if MINVER_CLI
        public static bool TryParseEnvVars([NotNullWhen(returnValue: true)] out Options? options)
        {
            options = null;

            VersionPart? autoIncrement = null;
            MajorMinor? minMajorMinor = null;
            Verbosity? verbosity = null;
            Lib.Version? versionOverride = null;

            var autoIncrementEnvVar = GetEnvVar("MinVerAutoIncrement");
            if (!string.IsNullOrEmpty(autoIncrementEnvVar))
            {
                if (Enum.TryParse<VersionPart>(autoIncrementEnvVar, true, out var versionPart))
                {
                    autoIncrement = versionPart;
                }
                else
                {
                    Logger.ErrorInvalidEnvVar("MinVerAutoIncrement", autoIncrementEnvVar, VersionPartExtensions.ValidValues);
                    return false;
                }
            }

            var buildMeta = GetEnvVar("MinVerBuildMetadata");

            var defaultPreReleasePhase = GetEnvVar("MinVerDefaultPreReleasePhase");

            var minMajorMinorEnvVar = GetEnvVar("MinVerMinimumMajorMinor");
            if (!string.IsNullOrEmpty(minMajorMinorEnvVar) && !MajorMinor.TryParse(minMajorMinorEnvVar, out minMajorMinor))
            {
                Logger.ErrorInvalidEnvVar("MinVerMinimumMajorMinor", minMajorMinorEnvVar, MajorMinor.ValidValues);
                return false;
            }

            var tagPrefix = GetEnvVar("MinVerTagPrefix");

            var verbosityEnvVar = GetEnvVar("MinVerVerbosity");
            if (!string.IsNullOrEmpty(verbosityEnvVar) && !VerbosityMap.TryMap(verbosityEnvVar, out verbosity))
            {
                Logger.ErrorInvalidEnvVar("MinVerVerbosity", verbosityEnvVar, VerbosityMap.ValidValues);
                return false;
            }

            var versionOverrideEnvVar = GetEnvVar("MinVerVersionOverride");
            if (!string.IsNullOrEmpty(versionOverrideEnvVar) && !Lib.Version.TryParse(versionOverrideEnvVar, out versionOverride))
            {
                Logger.ErrorInvalidEnvVar("MinVerVersionOverride", versionOverrideEnvVar, "");
                return false;
            }

            options = new Options(autoIncrement, buildMeta, defaultPreReleasePhase, minMajorMinor, tagPrefix, verbosity, versionOverride);

            return true;
        }

        private static string? GetEnvVar(string name)
        {
            var vars = Environment.GetEnvironmentVariables();

            var key = vars.Keys
                .Cast<string>()
                .OrderBy(_ => _, StringComparer.Ordinal)
                .FirstOrDefault(key => string.Equals(key, name, StringComparison.OrdinalIgnoreCase));

            return key == null ? null : (string?)vars[key];
        }
#endif

        public static bool TryParse(
            string? autoIncrementOption,
            string? buildMetaOption,
            string? defaultPreReleasePhaseOption,
            string? minMajorMinorOption,
            string? tagPrefixOption,
            string? verbosityOption,
#if MINVER
            string? versionOverrideOption,
#endif
            [NotNullWhen(returnValue: true)] out Options? options)
        {
            options = null;

            VersionPart? autoIncrement = null;
            MajorMinor? minMajorMinor = null;
            Verbosity? verbosity = null;
            Lib.Version? versionOverride = null;

            if (!string.IsNullOrEmpty(autoIncrementOption))
            {
                if (Enum.TryParse<VersionPart>(autoIncrementOption, true, out var versionPart))
                {
                    autoIncrement = versionPart;
                }
                else
                {
                    Logger.ErrorInvalidAutoIncrement(autoIncrementOption);
                    return false;
                }
            }

            if (!string.IsNullOrEmpty(minMajorMinorOption) && !MajorMinor.TryParse(minMajorMinorOption, out minMajorMinor))
            {
                Logger.ErrorInvalidMinMajorMinor(minMajorMinorOption);
                return false;
            }

            if (!string.IsNullOrEmpty(verbosityOption) && !VerbosityMap.TryMap(verbosityOption, out verbosity))
            {
                Logger.ErrorInvalidVerbosity(verbosityOption);
                return false;
            }

#if MINVER
            if (!string.IsNullOrEmpty(versionOverrideOption) && !Lib.Version.TryParse(versionOverrideOption, out versionOverride))
            {
                Logger.ErrorInvalidVersionOverride(versionOverrideOption);
                return false;
            }
#endif

            options = new Options(autoIncrement, buildMetaOption, defaultPreReleasePhaseOption, minMajorMinor, tagPrefixOption, verbosity, versionOverride);

            return true;
        }

        public Options Mask(Options other) =>
            new Options(
#pragma warning disable format
                this.AutoIncrement          ?? other.AutoIncrement,
                this.BuildMeta              ?? other.BuildMeta,
                this.DefaultPreReleasePhase ?? other.DefaultPreReleasePhase,
                this.MinMajorMinor          ?? other.MinMajorMinor,
                this.TagPrefix              ?? other.TagPrefix,
                this.Verbosity              ?? other.Verbosity,
                this.VersionOverride        ?? other.VersionOverride);
#pragma warning restore format

        public VersionPart? AutoIncrement { get; private set; }

        public string? BuildMeta { get; private set; }

        public string? DefaultPreReleasePhase { get; private set; }

        public MajorMinor? MinMajorMinor { get; private set; }

        public string? TagPrefix { get; private set; }

        public Verbosity? Verbosity { get; private set; }

        public Lib.Version? VersionOverride { get; private set; }
    }
}
