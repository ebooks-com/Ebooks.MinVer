namespace MinVer.Lib
{
    public enum VersionPart
    {
        Patch = 0,
        Minor = 1,
        Major = 2,
    }

    public static class VersionPartEx
    {
        public static string ValidValues => "major, minor, or patch (default)";
    }
}
