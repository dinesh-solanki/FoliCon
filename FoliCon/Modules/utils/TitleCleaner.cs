using NLog;
using Logger = NLog.Logger;

namespace FoliCon.Modules.utils;

internal static partial class TitleCleaner
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    public static string Clean(string title)
    {
        var normalizedTitle = title.Replace('-', ' ').Replace('_', ' ').Replace('.', ' ');

        // \s* --Remove any whitespace which would be left at the end after this substitution
        // \(? --Remove optional bracket starting (720p)
        // (\d{4}) --Remove year from movie
        // (420)|(720)|(1080) resolutions
        // (year|resolutions) find at least one main token to remove
        // p?i? \)? --Not needed. To emphasize removal of 1080i, closing bracket etc, but not needed due to the last part
        // .* --Remove all trailing information after having found year or resolution as junk usually follows
        var cleanTitle = QualityAndResolutionFormatRegex().Replace(normalizedTitle, "");
        cleanTitle = EnclosedInBracketsRegex().Replace(cleanTitle, "");
        cleanTitle = MultipleSpacesRegex().Replace(cleanTitle, " ");
        var clean = string.IsNullOrWhiteSpace(cleanTitle) ? normalizedTitle : cleanTitle;
        
        Logger.Debug("Cleaned title: {Clean}, Original title: {Title}", clean, title);
        return clean;
    }

    [GeneratedRegex("\\s*\\(?((\\d{4})|(420)|(720)|(1080))p?i?\\)?.*", RegexOptions.IgnoreCase | RegexOptions.Compiled, "en-US")]
    private static partial Regex QualityAndResolutionFormatRegex();
    [GeneratedRegex(@"\[.*\]", RegexOptions.IgnoreCase | RegexOptions.Compiled, "en-US")]
    private static partial Regex EnclosedInBracketsRegex();
    [GeneratedRegex(" {2,}", RegexOptions.IgnoreCase | RegexOptions.Compiled, "en-US")]
    private static partial Regex MultipleSpacesRegex();
}