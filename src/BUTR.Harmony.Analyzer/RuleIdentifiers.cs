using System.Globalization;

namespace BUTR.Harmony.Analyzer
{
    public static class RuleIdentifiers
    {
        public const string MemberDoesntExists = "BHA0001";
        public const string AssemblyNotFound = "BHA0002";
        public const string TypeNotFound = "BHA0003";

        public static string GetHelpUri(string idenfifier)
        {
            return string.Format(CultureInfo.InvariantCulture, "https://github.com/BUTR/BUTR.Harmony.Analyzer/blob/master/docs/Rules/{0}.md", idenfifier);
        }
    }
}