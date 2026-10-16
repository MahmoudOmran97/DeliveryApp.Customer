// ═══════════════════════════════════════════════════════════════
// Helpers / SearchTextHelper.cs
// تطبيع النص العربي للبحث المحلي (client-side): بيشيل التشكيل ويوحّد أشكال الألف
// والياء/الألف المقصورة والتاء المربوطة، عشان "ماء" و"ماه" و"إبريق" و"ابريق" يتطابقوا.
// ═══════════════════════════════════════════════════════════════
using System.Text;

namespace DeliveryApp.Customer.Helpers;

public static class SearchTextHelper
{
    public static string Normalize(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;

        var sb = new StringBuilder(text.Length);
        foreach (var ch in text.Trim())
        {
            // تشكيل + تطويل
            if (ch is >= '\u064B' and <= '\u065F' or '\u0670' or '\u0640') continue;

            sb.Append(ch switch
            {
                'أ' or 'إ' or 'آ' or 'ٱ' => 'ا',
                'ى' => 'ي',
                'ة' => 'ه',
                'ؤ' => 'و',
                'ئ' => 'ي',
                _ => char.ToLowerInvariant(ch)
            });
        }
        return sb.ToString();
    }

    /// <summary>هل النص (بعد التطبيع) بيحتوي على الاستعلام (بعد التطبيع)؟</summary>
    public static bool Matches(string? text, string normalizedQuery)
    {
        if (string.IsNullOrEmpty(normalizedQuery)) return true;
        return Normalize(text).Contains(normalizedQuery, StringComparison.Ordinal);
    }
}
