using System.Text;

public static class VietnameseText
{
    private static readonly UTF8Encoding StrictUtf8 = new UTF8Encoding(false, true);

    public static string Fix(string text)
    {
        if (string.IsNullOrEmpty(text) || !LooksMojibake(text))
            return text;

        byte[] bytes = new byte[text.Length];
        for (int i = 0; i < text.Length; i++)
        {
            if (!TryGetWindows1252Byte(text[i], out bytes[i]))
                return text;
        }

        try
        {
            string fixedText = StrictUtf8.GetString(bytes);
            return ScoreMojibake(fixedText) < ScoreMojibake(text) ? fixedText : text;
        }
        catch (DecoderFallbackException)
        {
            return text;
        }
    }

    public static string[] Fix(string[] lines)
    {
        if (lines == null)
            return null;

        string[] fixedLines = new string[lines.Length];
        for (int i = 0; i < lines.Length; i++)
        {
            fixedLines[i] = Fix(lines[i]);
        }

        return fixedLines;
    }

    public static string FixNpcName(string npcId, string npcName)
    {
        string fixedName = Fix(npcName);

        if (!string.IsNullOrWhiteSpace(fixedName) && fixedName != npcName)
            return fixedName;

        switch (npcId)
        {
            case "village_elder":
                return string.IsNullOrWhiteSpace(fixedName) || fixedName == "Gia Lang" ? "Gi\u00e0 L\u00e0ng" : fixedName;
            case "giong_mother":
                return string.IsNullOrWhiteSpace(fixedName) ? "M\u1eb9 Gi\u00f3ng" : fixedName;
            case "bac_ba":
                return string.IsNullOrWhiteSpace(fixedName) ? "B\u00e1c Ba" : fixedName;
            case "blacksmith":
                return string.IsNullOrWhiteSpace(fixedName) ? "B\u00e1c Th\u1ee3 R\u00e8n" : fixedName;
            default:
                return fixedName;
        }
    }

    private static bool LooksMojibake(string text)
    {
        return ScoreMojibake(text) >= 2;
    }

    private static int ScoreMojibake(string text)
    {
        int score = 0;
        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            if (c == '\u00c3' || c == '\u00c4' || c == '\u00c6')
            {
                score += 2;
            }
            else if (c == '\u00c2' && i + 1 < text.Length && text[i + 1] >= 128 && text[i + 1] <= 191)
            {
                score += 2;
            }
            else if (c == '\u00e2')
            {
                score += 2;
            }
            else if (c == '\u00e1' && i + 1 < text.Length && (text[i + 1] == '\u00ba' || text[i + 1] == '\u00bb'))
            {
                score += 2;
            }
            else if (IsWindows1252Punctuation(c))
            {
                score++;
            }
            else if (c < 32 && c != '\n' && c != '\r' && c != '\t')
            {
                score++;
            }
        }

        return score;
    }

    private static bool TryGetWindows1252Byte(char c, out byte value)
    {
        if (c <= 0xff)
        {
            value = (byte)c;
            return true;
        }

        switch (c)
        {
            case '\u20ac': value = 0x80; return true;
            case '\u201a': value = 0x82; return true;
            case '\u0192': value = 0x83; return true;
            case '\u201e': value = 0x84; return true;
            case '\u2026': value = 0x85; return true;
            case '\u2020': value = 0x86; return true;
            case '\u2021': value = 0x87; return true;
            case '\u02c6': value = 0x88; return true;
            case '\u2030': value = 0x89; return true;
            case '\u0160': value = 0x8a; return true;
            case '\u2039': value = 0x8b; return true;
            case '\u0152': value = 0x8c; return true;
            case '\u017d': value = 0x8e; return true;
            case '\u2018': value = 0x91; return true;
            case '\u2019': value = 0x92; return true;
            case '\u201c': value = 0x93; return true;
            case '\u201d': value = 0x94; return true;
            case '\u2022': value = 0x95; return true;
            case '\u2013': value = 0x96; return true;
            case '\u2014': value = 0x97; return true;
            case '\u02dc': value = 0x98; return true;
            case '\u2122': value = 0x99; return true;
            case '\u0161': value = 0x9a; return true;
            case '\u203a': value = 0x9b; return true;
            case '\u0153': value = 0x9c; return true;
            case '\u017e': value = 0x9e; return true;
            case '\u0178': value = 0x9f; return true;
            default:
                value = 0;
                return false;
        }
    }

    private static bool IsWindows1252Punctuation(char c)
    {
        switch (c)
        {
            case '\u20ac':
            case '\u201a':
            case '\u0192':
            case '\u201e':
            case '\u2026':
            case '\u2020':
            case '\u2021':
            case '\u02c6':
            case '\u2030':
            case '\u0160':
            case '\u2039':
            case '\u0152':
            case '\u017d':
            case '\u2018':
            case '\u2019':
            case '\u201c':
            case '\u201d':
            case '\u2022':
            case '\u2013':
            case '\u2014':
            case '\u02dc':
            case '\u2122':
            case '\u0161':
            case '\u203a':
            case '\u0153':
            case '\u017e':
            case '\u0178':
                return true;
            default:
                return false;
        }
    }
}
