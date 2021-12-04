using System.Globalization;

namespace BUTR.Harmony.Analyzer
{
    public static class RuleIdentifiers
    {
        public const string MemberDoesntExists = "BHA0001";
        public const string AssemblyNotFound = "BHA0002";
        public const string TypeNotFound = "BHA0003";
        public const string MissingGetter = "BHA0004";
        public const string MissingSetter = "BHA0005";
        public const string WrongType = "BHA0006";

        public static string GetHelpUri(string idenfifier)
        {
            return string.Format(CultureInfo.InvariantCulture, "https://github.com/BUTR/BUTR.Harmony.Analyzer/blob/master/docs/Rules/{0}.md", idenfifier);
        }
    }
}