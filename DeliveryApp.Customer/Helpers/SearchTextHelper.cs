// ═══════════════════════════════════════════════════════════════
// Helpers / SearchTextHelper.cs
// تطبيع النص العربي + بحث "بالتخمين" محلي (للعروض ولبحث جوه المحل).
// نفس منطق السيرفر (Services/FuzzySearch.cs): تحمّل أخطاء الكتابة والحروف المتقاربة في النطق.
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
                'ء' => ' ',
                _ => char.ToLowerInvariant(ch)
            });
        }
        return sb.ToString();
    }

    /// <summary>تطابق حرفي (بعد التطبيع) — مناسب للاستخدامات البسيطة.</summary>
    public static bool Matches(string? text, string normalizedQuery)
    {
        if (string.IsNullOrEmpty(normalizedQuery)) return true;
        return Normalize(text).Contains(normalizedQuery, StringComparison.Ordinal);
    }

    // ═════════ بحث بالتخمين ═════════

    static string Loose(string normalized)
    {
        var sb = new StringBuilder(normalized.Length);
        foreach (var ch in normalized)
        {
            sb.Append(ch switch
            {
                'ث' or 'ص' => 'س',
                'ذ' or 'ظ' => 'ز',
                'ط' => 'ت',
                'ض' => 'د',
                'ق' => 'ك',
                _ => ch
            });
        }
        return sb.ToString();
    }

    static string[] Words(string normalized)
    {
        var parts = new List<string>();
        var sb = new StringBuilder();
        foreach (var ch in normalized)
        {
            if (char.IsLetterOrDigit(ch)) sb.Append(ch);
            else if (sb.Length > 0) { parts.Add(sb.ToString()); sb.Clear(); }
        }
        if (sb.Length > 0) parts.Add(sb.ToString());
        return parts.ToArray();
    }

    public sealed class FuzzyQuery
    {
        public string Normalized { get; }
        public string[] LooseTokens { get; }
        public bool IsEmpty => Normalized.Length == 0;
        public FuzzyQuery(string? text)
        {
            Normalized = Normalize(text);
            LooseTokens = Words(Loose(Normalized));
        }
    }

    /// <summary>رقم أقل = تطابق أفضل، و null = مفيش تطابق.</summary>
    public static int? FuzzyScore(FuzzyQuery q, string? text)
    {
        if (q.IsEmpty) return null;
        var norm = Normalize(text);
        if (norm.Length == 0) return null;

        if (norm.StartsWith(q.Normalized, StringComparison.Ordinal)) return 0;
        if (norm.Contains(q.Normalized, StringComparison.Ordinal)) return 1;

        var words = Words(Loose(norm));
        int total = 2;
        foreach (var t in q.LooseTokens)
        {
            int? best = null;
            foreach (var w in words)
            {
                var s = TokenScore(t, w);
                if (s.HasValue && (best == null || s < best)) best = s;
                if (best == 0) break;
            }
            if (best == null) return null;
            total += best.Value;
        }
        return total;
    }

    /// <summary>أفضل (أقل) رقم بين عدة نصوص — null لو ولا واحد اتطابق.</summary>
    public static int? FuzzyBest(FuzzyQuery q, params string?[] texts)
    {
        int? best = null;
        foreach (var t in texts)
        {
            var s = FuzzyScore(q, t);
            if (s.HasValue && (best == null || s < best)) best = s;
        }
        return best;
    }

    static int? TokenScore(string t, string w)
    {
        if (w.StartsWith(t, StringComparison.Ordinal)) return 0;
        if (t.Length >= 3 && w.Contains(t, StringComparison.Ordinal)) return 1;

        int allowed = t.Length <= 3 ? 0 : t.Length <= 6 ? 1 : 2;
        if (allowed == 0) return null;

        int d = Distance(t, w);
        if (w.Length > t.Length) d = Math.Min(d, Distance(t, w.Substring(0, t.Length)));

        return d <= allowed ? 2 + d : null;
    }

    static int Distance(string a, string b)
    {
        if (a.Length == 0) return b.Length;
        if (b.Length == 0) return a.Length;
        var d = new int[a.Length + 1, b.Length + 1];
        for (int i = 0; i <= a.Length; i++) d[i, 0] = i;
        for (int j = 0; j <= b.Length; j++) d[0, j] = j;
        for (int i = 1; i <= a.Length; i++)
        {
            for (int j = 1; j <= b.Length; j++)
            {
                int cost = a[i - 1] == b[j - 1] ? 0 : 1;
                d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost);
                if (i > 1 && j > 1 && a[i - 1] == b[j - 2] && a[i - 2] == b[j - 1])
                    d[i, j] = Math.Min(d[i, j], d[i - 2, j - 2] + 1);
            }
        }
        return d[a.Length, b.Length];
    }
}
