using System;
using MinVer.Lib;

namespace MinVer
{
    internal class Logger : ILogger
    {
        private readonly Verbosity verbosity;

        public Logger(Verbosity verbosity) => this.verbosity = verbosity;

        public bool IsTraceEnabled => this.verbosity >= Verbosity.Trace;

        public bool IsDebugEnabled => this.verbosity >= Verbosity.Debug;

        public bool IsInfoEnabled => this.verbosity >= Verbosity.Info;

        public bool IsWarnEnabled => this.verbosity >= Verbosity.Warn;

        public bool Trace(string message) => this.IsTraceEnabled && Message(message);

        public bool Debug(string message) => this.IsDebugEnabled && Message(message);

        public bool Info(string message) => this.IsInfoEnabled && Message(message);

        public bool Warn(int code, string message) => this.IsWarnEnabled && Message($"warning : {message}");

        public static void ErrorInvalidEnvVar(string name, string value, string validValueString)
        {
            if (validValueString.Length == 0)
            {
                Error($"Invalid environment variable '{name}' '{value}'.");
            }
            else
            {
                Error($"Invalid environment variable '{name}' '{value}'. Valid values are {validValueString}");
            }
        }

        public static void ErrorWorkDirDoesNotExist(string workDir) =>
            Error($"Working directory '{workDir}' does not exist.");

        public static void ErrorInvalidAutoIncrement(string autoIncrement) =>
            Error($"Invalid auto increment '{autoIncrement}'. Valid values are {VersionPartExtensions.ValidValues}");

        public static void ErrorInvalidMinMajorMinor(string minMajorMinor) =>
            Error($"Invalid minimum MAJOR.MINOR '{minMajorMinor}'. Valid values are {MajorMinor.ValidValues}");

        public static void ErrorInvalidVerbosity(string verbosity) =>
            Error($"Invalid verbosity '{verbosity}'. Valid values are {VerbosityMap.ValidValues}.");

        private static void Error(string message) => Message($"error : {message}");

        private static bool Message(string message)
        {
            if (message.Contains('\r', StringComparison.OrdinalIgnoreCase) || message.Contains('\n', StringComparison.OrdinalIgnoreCase))
            {
                var lines = message.Replace("\r\n", "\n", StringComparison.OrdinalIgnoreCase).Split('\r', '\n');
                message = string.Join($"{Environment.NewLine}MinVer: ", lines);
            }

            Console.Error.WriteLine($"MinVer: {message}");

            return true;
        }
    }
}
