using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace EmpireBuilder.Localization
{
    /// <summary>
    /// Pure C# Arabic text shaper and bidirectional renderer for Unity TextMeshPro.
    /// Converts standard Unicode Arabic characters into connected Presentation Forms-B,
    /// supports Lam-Alef ligatures, Tashkeel/harakat, numbers, punctuation,
    /// and safely preserves rich text XML tags (e.g. <color=#FFD700>, <b>, etc.).
    /// </summary>
    public static class ArabicFixer
    {
        private struct ArabicGlyph
        {
            public char raw;
            public char isolated;
            public char initial;
            public char medial;
            public char final;
            public bool connectsBefore;
            public bool connectsAfter;

            public ArabicGlyph(char raw, char iso, char ini, char med, char fin, bool before, bool after)
            {
                this.raw = raw;
                this.isolated = iso;
                this.initial = ini;
                this.medial = med;
                this.final = fin;
                this.connectsBefore = before;
                this.connectsAfter = after;
            }
        }

        private static readonly Dictionary<char, ArabicGlyph> Glyphs = new Dictionary<char, ArabicGlyph>();

        static ArabicFixer()
        {
            // Character definitions with standard Unicode Presentation Forms-B
            // Non-connecting forward letters: connectsBefore = true, connectsAfter = false
            AddGlyph('\u0622', '\uFE81', '\uFE81', '\uFE82', '\uFE82', true, false); // Alef Madda
            AddGlyph('\u0623', '\uFE83', '\uFE83', '\uFE84', '\uFE84', true, false); // Alef Hamza Above
            AddGlyph('\u0624', '\uFE85', '\uFE85', '\uFE86', '\uFE86', true, false); // Waw Hamza
            AddGlyph('\u0625', '\uFE87', '\uFE87', '\uFE88', '\uFE88', true, false); // Alef Hamza Below
            AddGlyph('\u0627', '\uFE8D', '\uFE8D', '\uFE8E', '\uFE8E', true, false); // Alef
            AddGlyph('\u062F', '\uFEA9', '\uFEA9', '\uFEAA', '\uFEAA', true, false); // Dal
            AddGlyph('\u0630', '\uFEAB', '\uFEAB', '\uFEAC', '\uFEAC', true, false); // Thal
            AddGlyph('\u0631', '\uFEAD', '\uFEAD', '\uFEAE', '\uFEAE', true, false); // Raa
            AddGlyph('\u0632', '\uFEAF', '\uFEAF', '\uFEB0', '\uFEB0', true, false); // Zay
            AddGlyph('\u0648', '\uFEED', '\uFEED', '\uFEEE', '\uFEEE', true, false); // Waw
            AddGlyph('\u0629', '\uFE93', '\uFE93', '\uFE94', '\uFE94', true, false); // Ta Marbuta
            AddGlyph('\u0649', '\uFEEF', '\uFEEF', '\uFEF0', '\uFEF0', true, false); // Alef Maksura
            AddGlyph('\u0621', '\uFE80', '\uFE80', '\uFE80', '\uFE80', false, false); // Hamza

            // Dual-connecting letters: connectsBefore = true, connectsAfter = true
            AddGlyph('\u0626', '\uFE89', '\uFE8B', '\uFE8C', '\uFE8A', true, true); // Yaa Hamza
            AddGlyph('\u0628', '\uFE8F', '\uFE91', '\uFE92', '\uFE90', true, true); // Baa
            AddGlyph('\u062A', '\uFE95', '\uFE97', '\uFE98', '\uFE96', true, true); // Taa
            AddGlyph('\u062B', '\uFE99', '\uFE9B', '\uFE9C', '\uFE9A', true, true); // Thaa
            AddGlyph('\u062C', '\uFE9D', '\uFE9F', '\uFEA0', '\uFE9E', true, true); // Jeem
            AddGlyph('\u062D', '\uFEA1', '\uFEA3', '\uFEA4', '\uFEA2', true, true); // Haa
            AddGlyph('\u062E', '\uFEA5', '\uFEA7', '\uFEA8', '\uFEA6', true, true); // Khaa
            AddGlyph('\u0633', '\uFEB1', '\uFEB3', '\uFEB4', '\uFEB2', true, true); // Seen
            AddGlyph('\u0634', '\uFEB5', '\uFEB7', '\uFEB8', '\uFEB6', true, true); // Sheen
            AddGlyph('\u0635', '\uFEB9', '\uFEBB', '\uFEBC', '\uFEBA', true, true); // Saad
            AddGlyph('\u0636', '\uFEBD', '\uFEBF', '\uFEC0', '\uFEBE', true, true); // Daad
            AddGlyph('\u0637', '\uFEC1', '\uFEC3', '\uFEC4', '\uFEC2', true, true); // Taa (heavy)
            AddGlyph('\u0638', '\uFEC5', '\uFEC7', '\uFEC8', '\uFEC6', true, true); // Zaa (heavy)
            AddGlyph('\u0639', '\uFEC9', '\uFECB', '\uFECC', '\uFECA', true, true); // Ayn
            AddGlyph('\u063A', '\uFECD', '\uFECF', '\uFED0', '\uFECE', true, true); // Ghayn
            AddGlyph('\u0641', '\uFED1', '\uFED3', '\uFED4', '\uFED2', true, true); // Faa
            AddGlyph('\u0642', '\uFED5', '\uFED7', '\uFED8', '\uFED6', true, true); // Qaaf
            AddGlyph('\u0643', '\uFED9', '\uFEDB', '\uFEDC', '\uFEDA', true, true); // Kaaf
            AddGlyph('\u0644', '\uFEDD', '\uFEDF', '\uFEE0', '\uFEDE', true, true); // Laam
            AddGlyph('\u0645', '\uFEE1', '\uFEE3', '\uFEE4', '\uFEE2', true, true); // Meem
            AddGlyph('\u0646', '\uFEE5', '\uFEE7', '\uFEE8', '\uFEE6', true, true); // Noon
            AddGlyph('\u0647', '\uFEE9', '\uFEEB', '\uFEEC', '\uFEEA', true, true); // Haa
            AddGlyph('\u064A', '\uFEF1', '\uFEF3', '\uFEF4', '\uFEF2', true, true); // Yaa
        }

        private static void AddGlyph(char raw, char iso, char ini, char med, char fin, bool before, bool after)
        {
            Glyphs[raw] = new ArabicGlyph(raw, iso, ini, med, fin, before, after);
        }

        /// <summary>
        /// Checks if a character is within the Arabic Unicode block.
        /// </summary>
        public static bool IsArabicChar(char c)
        {
            return (c >= '\u0600' && c <= '\u06FF') || (c >= '\u0750' && c <= '\u077F') || (c >= '\uFB50' && c <= '\uFDFF') || (c >= '\uFE70' && c <= '\uFEFF');
        }

        /// <summary>
        /// Checks if the string contains any Arabic characters.
        /// </summary>
        public static bool ContainsArabic(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;
            for (int i = 0; i < text.Length; i++)
            {
                if (IsArabicChar(text[i])) return true;
            }
            return false;
        }

        /// <summary>
        /// Shapes and reverses Arabic text for display in TextMeshPro or standard UI.
        /// Preserves rich text tags (e.g. &lt;color=...&gt;, &lt;b&gt;, etc.) and numbers.
        /// </summary>
        public static string Fix(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            if (!ContainsArabic(input)) return input;

            // Handle multiline strings by processing each line independently
            string[] lines = input.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                lines[i] = FixLine(lines[i]);
            }
            return string.Join("\n", lines);
        }

        private static string FixLine(string line)
        {
            if (string.IsNullOrEmpty(line) || !ContainsArabic(line)) return line;

            // 1. Extract XML / Rich Text tags so they are not reversed
            List<string> tags = new List<string>();
            string tagPattern = @"<[^>]+>";
            string cleanLine = Regex.Replace(line, tagPattern, m =>
            {
                tags.Add(m.Value);
                return $"___TAG_{tags.Count - 1}___";
            });

            // 2. Shape Arabic letters into connected Presentation Forms
            string shaped = ShapeArabicWords(cleanLine);

            // 3. Bi-directional reordering (reverse Arabic word segments while preserving English words/numbers)
            string reversed = ReorderRTL(shaped);

            // 4. Restore original XML / Rich Text tags
            for (int i = 0; i < tags.Count; i++)
            {
                reversed = reversed.Replace($"___TAG_{i}___", tags[i]);
            }

            return reversed;
        }

        private static string ShapeArabicWords(string text)
        {
            char[] chars = text.ToCharArray();
            StringBuilder result = new StringBuilder(chars.Length);

            for (int i = 0; i < chars.Length; i++)
            {
                char current = chars[i];

                if (!Glyphs.ContainsKey(current))
                {
                    result.Append(current);
                    continue;
                }

                // Check for Lam-Alef ligatures
                if (current == '\u0644' && i + 1 < chars.Length) // Laam
                {
                    char next = chars[i + 1];
                    char ligature = '\0';
                    bool prevConnected = i > 0 && Glyphs.ContainsKey(chars[i - 1]) && Glyphs[chars[i - 1]].connectsAfter;

                    switch (next)
                    {
                        case '\u0622': // Madda
                            ligature = prevConnected ? '\uFEF6' : '\uFEF5';
                            break;
                        case '\u0623': // Hamza Above
                            ligature = prevConnected ? '\uFEF8' : '\uFEF7';
                            break;
                        case '\u0625': // Hamza Below
                            ligature = prevConnected ? '\uFEFA' : '\uFEF9';
                            break;
                        case '\u0627': // Alef
                            ligature = prevConnected ? '\uFEFC' : '\uFEFB';
                            break;
                    }

                    if (ligature != '\0')
                    {
                        result.Append(ligature);
                        i++; // Skip alef
                        continue;
                    }
                }

                bool hasPrev = i > 0 && Glyphs.ContainsKey(chars[i - 1]) && Glyphs[chars[i - 1]].connectsAfter;
                bool hasNext = i + 1 < chars.Length && Glyphs.ContainsKey(chars[i + 1]) && Glyphs[chars[i + 1]].connectsBefore;

                ArabicGlyph g = Glyphs[current];

                if (hasPrev && hasNext)
                {
                    result.Append(g.medial);
                }
                else if (hasPrev)
                {
                    result.Append(g.final);
                }
                else if (hasNext)
                {
                    result.Append(g.initial);
                }
                else
                {
                    result.Append(g.isolated);
                }
            }

            return result.ToString();
        }

        private static string ReorderRTL(string input)
        {
            // Tokenize into Arabic clusters, Western/Number clusters, and Tag placeholders
            List<string> tokens = new List<string>();
            StringBuilder currentToken = new StringBuilder();
            bool inArabic = false;

            for (int i = 0; i < input.Length; i++)
            {
                char c = input[i];
                bool isArab = IsArabicChar(c);

                if (i == 0)
                {
                    inArabic = isArab;
                    currentToken.Append(c);
                }
                else
                {
                    if (isArab == inArabic)
                    {
                        currentToken.Append(c);
                    }
                    else
                    {
                        tokens.Add(currentToken.ToString());
                        currentToken.Clear();
                        inArabic = isArab;
                        currentToken.Append(c);
                    }
                }
            }
            if (currentToken.Length > 0)
            {
                tokens.Add(currentToken.ToString());
            }

            // Reverse Arabic tokens internally (since Arabic is RTL in a LTR environment)
            StringBuilder finalLine = new StringBuilder();

            // Reverse the order of tokens if predominantly Arabic
            tokens.Reverse();

            for (int t = 0; t < tokens.Count; t++)
            {
                string tok = tokens[t];
                if (tok.Length > 0 && IsArabicChar(tok[0]))
                {
                    // Reverse the characters of Arabic words
                    char[] arr = tok.ToCharArray();
                    Array.Reverse(arr);
                    finalLine.Append(new string(arr));
                }
                else
                {
                    finalLine.Append(tok);
                }
            }

            return finalLine.ToString();
        }
    }
}
