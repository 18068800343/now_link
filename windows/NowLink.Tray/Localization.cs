using System.Globalization;

namespace NowLink.Tray
{
    internal static class Localization
    {
        private static readonly bool IsChinese = CultureInfo.CurrentUICulture.Name.StartsWith("zh");

        public static string Text(string english, string chinese)
        {
            return IsChinese ? chinese : english;
        }
    }
}
