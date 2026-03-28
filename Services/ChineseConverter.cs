using System.Runtime.InteropServices;
using System.Text;

namespace TranscribeWin.Services;

public static class ChineseConverter
{
    private const uint LCMAP_TRADITIONAL_CHINESE = 0x04000000;
    private const uint LOCALE_ZH_CN = 0x0804;

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int LCMapString(uint Locale, uint dwMapFlags, string lpSrcStr, int cchSrc, StringBuilder? lpDestStr, int cchDest);

    public static string ToTraditional(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        int size = LCMapString(LOCALE_ZH_CN, LCMAP_TRADITIONAL_CHINESE, text, -1, null, 0);
        if (size <= 0) return text;

        var sb = new StringBuilder(size);
        LCMapString(LOCALE_ZH_CN, LCMAP_TRADITIONAL_CHINESE, text, -1, sb, size);
        
        // Remove null terminator if present
        int len = sb.Length;
        if (len > 0 && sb[len - 1] == '\0')
            sb.Length--;

        return sb.ToString();
    }
}
