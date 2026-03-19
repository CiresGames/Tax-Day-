using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using System.Text.RegularExpressions;
using UnityEngine;

namespace AHAKuo.Signalia.GameSystems.Localization.Internal
{
    /// <summary>
    /// Provides Arabic text shaping utilities and RTL formatting for proper text display.
    /// Handles contextual letter forms for connected Arabic script and intelligently mirrors
    /// paired symbols (parentheses, brackets, braces) in RTL contexts.
    /// <para>
    /// <b>Features:</b>
    /// - Arabic letter shaping with contextual forms (isolated, initial, medial, final)
    /// - Lam-Alef ligature support
    /// - Smart mirroring of paired symbols ((), [], {}, &lt;&gt;, etc.) near Arabic text
    /// - Font glyph availability checking for fallback support
    /// </para>
    /// <para>
    /// <b>Example:</b> Input "Press {A} to continue" in Arabic context becomes "Press }A{ to continue"
    /// with properly mirrored braces, while pure English text remains unchanged.
    /// </para>
    /// </summary>
    internal static class ArabicTextFormatter
    {
        private const char LamCharacter = '\u0644'; // i dunno why its lonely here

        /// <summary>
        /// To catch alternates in case the font asset did not find a specific character.
        /// Works for things like Alef when its alternate unicode (isolated) is not found when 0627 version exists instead.
        /// Used when a font asset does not contain the code for the letter.
        /// Only for letters that visually have two characters that look the same but have different unicodes.
        /// </summary>
        private readonly struct ArabicGlyphFishnet
        {
            public char[] iso_Alternates { get; }
            public char[] f_alternates { get; }
            public char[] i_alternates { get; }
            public char[] med_alternates { get; }

            public ArabicGlyphFishnet(char[] isoAlternates, char[] fAlternates, char[] iAlternates, char[] medAlternates)
            {
                iso_Alternates = isoAlternates;
                f_alternates = fAlternates;
                i_alternates = iAlternates;
                med_alternates = medAlternates;
            }
        }

        private readonly struct ArabicGlyph
        {
            public ArabicGlyph(char isolated, char final, char initial, char medial, ArabicGlyphFishnet agf, bool connectsBefore, bool connectsAfter)
            {
                Isolated = isolated;
                Final = final;
                Initial = initial;
                this.agf = agf;
                Medial = medial;
                ConnectsBefore = connectsBefore;
                ConnectsAfter = connectsAfter;
            }

            public char Isolated { get; }
            public char Final { get; }
            public char Initial { get; }
            public char Medial { get; }
            public ArabicGlyphFishnet agf { get; }
            public bool ConnectsBefore { get; }
            public bool ConnectsAfter { get; }

            public char GetForm(bool connectBefore, bool connectAfter)
            {
                if (connectBefore && connectAfter && Medial != '\0')
                {
                    return Medial;
                }

                if (connectBefore && Final != '\0')
                {
                    return Final;
                }

                if (connectAfter && Initial != '\0')
                {
                    return Initial;
                }

                return Isolated;
            }
        }

        private readonly struct LamAlefGlyph
        {
            public LamAlefGlyph(char isolated, char final)
            {
                Isolated = isolated;
                Final = final;
            }

            public char Isolated { get; }
            public char Final { get; }
        }

        private static readonly Dictionary<char, ArabicGlyph> Glyphs = new Dictionary<char, ArabicGlyph>
        {
            ['\u0621'] = new ArabicGlyph('\uFE80', '\0', '\0', '\0', new ArabicGlyphFishnet(null, null, null, null), false, false), // Hamza
            ['\u0622'] = new ArabicGlyph('\uFE81', '\uFE82', '\0', '\0', new ArabicGlyphFishnet(new[] { '\u0622' }, new[] { '\u0622' }, null, null), true, false), // Alef with madda
            ['\u0623'] = new ArabicGlyph('\uFE83', '\uFE84', '\0', '\0', new ArabicGlyphFishnet(new[] { '\u0623' }, new[] { '\u0623' }, null, null), true, false), // Alef with hamza above
            ['\u0624'] = new ArabicGlyph('\uFE85', '\uFE86', '\0', '\0', new ArabicGlyphFishnet(new[] { '\u0624' }, new[] { '\u0624' }, null, null), true, false), // Waw with hamza
            ['\u0625'] = new ArabicGlyph('\uFE87', '\uFE88', '\0', '\0', new ArabicGlyphFishnet(new[] { '\u0625' }, new[] { '\u0625' }, null, null), true, false), // Alef with hamza below
            ['\u0626'] = new ArabicGlyph('\uFE89', '\uFE8A', '\uFE8B', '\uFE8C', new ArabicGlyphFishnet(new[] { '\u0626' }, new[] { '\u0626' }, new[] { '\u0626' }, new[] { '\u0626' }), true, true), // Yeh with hamza
            ['\u0627'] = new ArabicGlyph('\uFE8D', '\uFE8E', '\0', '\0', new ArabicGlyphFishnet(new[] { '\u0627' }, new[] { '\u0627' }, null, null), true, false), // Alef
            ['\u0628'] = new ArabicGlyph('\uFE8F', '\uFE90', '\uFE91', '\uFE92', new ArabicGlyphFishnet(new[] { '\u0628' }, new[] { '\u0628' }, new[] { '\u0628' }, new[] { '\u0628' }), true, true), // Beh
            ['\u0629'] = new ArabicGlyph('\uFE93', '\uFE94', '\0', '\0', new ArabicGlyphFishnet(new[] { '\u0629' }, new[] { '\u0629' }, null, null), true, false), // Teh marbuta
            ['\u062A'] = new ArabicGlyph('\uFE95', '\uFE96', '\uFE97', '\uFE98', new ArabicGlyphFishnet(new[] { '\u062A' }, new[] { '\u062A' }, new[] { '\u062A' }, new[] { '\u062A' }), true, true), // Teh
            ['\u062B'] = new ArabicGlyph('\uFE99', '\uFE9A', '\uFE9B', '\uFE9C', new ArabicGlyphFishnet(new[] { '\u062B' }, new[] { '\u062B' }, new[] { '\u062B' }, new[] { '\u062B' }), true, true), // Theh
            ['\u062C'] = new ArabicGlyph('\uFE9D', '\uFE9E', '\uFE9F', '\uFEA0', new ArabicGlyphFishnet(new[] { '\u062C' }, new[] { '\u062C' }, new[] { '\u062C' }, new[] { '\u062C' }), true, true), // Jeem
            ['\u062D'] = new ArabicGlyph('\uFEA1', '\uFEA2', '\uFEA3', '\uFEA4', new ArabicGlyphFishnet(new[] { '\u062D' }, new[] { '\u062D' }, new[] { '\u062D' }, new[] { '\u062D' }), true, true), // Hah
            ['\u062E'] = new ArabicGlyph('\uFEA5', '\uFEA6', '\uFEA7', '\uFEA8', new ArabicGlyphFishnet(new[] { '\u062E' }, new[] { '\u062E' }, new[] { '\u062E' }, new[] { '\u062E' }), true, true), // Khah
            ['\u062F'] = new ArabicGlyph('\uFEA9', '\uFEAA', '\0', '\0', new ArabicGlyphFishnet(new[] { '\u062F' }, new[] { '\u062F' }, null, null), true, false), // Dal
            ['\u0630'] = new ArabicGlyph('\uFEAB', '\uFEAC', '\0', '\0', new ArabicGlyphFishnet(new[] { '\u0630' }, new[] { '\u0630' }, null, null), true, false), // Thal
            ['\u0631'] = new ArabicGlyph('\uFEAD', '\uFEAE', '\0', '\0', new ArabicGlyphFishnet(new[] { '\u0631' }, new[] { '\u0631' }, null, null), true, false), // Reh
            ['\u0632'] = new ArabicGlyph('\uFEAF', '\uFEB0', '\0', '\0', new ArabicGlyphFishnet(new[] { '\u0632' }, new[] { '\u0632' }, null, null), true, false), // Zain
            ['\u0633'] = new ArabicGlyph('\uFEB1', '\uFEB2', '\uFEB3', '\uFEB4', new ArabicGlyphFishnet(new[] { '\u0633' }, new[] { '\u0633' }, new[] { '\u0633' }, new[] { '\u0633' }), true, true), // Seen
            ['\u0634'] = new ArabicGlyph('\uFEB5', '\uFEB6', '\uFEB7', '\uFEB8', new ArabicGlyphFishnet(new[] { '\u0634' }, new[] { '\u0634' }, new[] { '\u0634' }, new[] { '\u0634' }), true, true), // Sheen
            ['\u0635'] = new ArabicGlyph('\uFEB9', '\uFEBA', '\uFEBB', '\uFEBC', new ArabicGlyphFishnet(new[] { '\u0635' }, new[] { '\u0635' }, new[] { '\u0635' }, new[] { '\u0635' }), true, true), // Sad
            ['\u0636'] = new ArabicGlyph('\uFEBD', '\uFEBE', '\uFEBF', '\uFEC0', new ArabicGlyphFishnet(new[] { '\u0636' }, new[] { '\u0636' }, new[] { '\u0636' }, new[] { '\u0636' }), true, true), // Dad
            ['\u0637'] = new ArabicGlyph('\uFEC1', '\uFEC2', '\uFEC3', '\uFEC4', new ArabicGlyphFishnet(new[] { '\u0637' }, new[] { '\u0637' }, new[] { '\u0637' }, new[] { '\u0637' }), true, true), // Tah
            ['\u0638'] = new ArabicGlyph('\uFEC5', '\uFEC6', '\uFEC7', '\uFEC8', new ArabicGlyphFishnet(new[] { '\u0638' }, new[] { '\u0638' }, new[] { '\u0638' }, new[] { '\u0638' }), true, true), // Zah
            ['\u0639'] = new ArabicGlyph('\uFEC9', '\uFECA', '\uFECB', '\uFECC', new ArabicGlyphFishnet(new[] { '\u0639' }, new[] { '\u0639' }, new[] { '\u0639' }, new[] { '\u0639' }), true, true), // Ain
            ['\u063A'] = new ArabicGlyph('\uFECD', '\uFECE', '\uFECF', '\uFED0', new ArabicGlyphFishnet(new[] { '\u063A' }, new[] { '\u063A' }, new[] { '\u063A' }, new[] { '\u063A' }), true, true), // Ghain
            ['\u0641'] = new ArabicGlyph('\uFED1', '\uFED2', '\uFED3', '\uFED4', new ArabicGlyphFishnet(new[] { '\u0641' }, new[] { '\u0641' }, new[] { '\u0641' }, new[] { '\u0641' }), true, true), // Feh
            ['\u0642'] = new ArabicGlyph('\uFED5', '\uFED6', '\uFED7', '\uFED8', new ArabicGlyphFishnet(new[] { '\u0642' }, new[] { '\u0642' }, new[] { '\u0642' }, new[] { '\u0642' }), true, true), // Qaf
            ['\u0643'] = new ArabicGlyph('\uFED9', '\uFEDA', '\uFEDB', '\uFEDC', new ArabicGlyphFishnet(new[] { '\u0643' }, new[] { '\u0643' }, new[] { '\u0643' }, new[] { '\u0643' }), true, true), // Kaf
            ['\u0644'] = new ArabicGlyph('\uFEDD', '\uFEDE', '\uFEDF', '\uFEE0', new ArabicGlyphFishnet(new[] { '\u0644' }, new[] { '\u0644' }, new[] { '\u0644' }, new[] { '\u0644' }), true, true), // Lam
            ['\u0645'] = new ArabicGlyph('\uFEE1', '\uFEE2', '\uFEE3', '\uFEE4', new ArabicGlyphFishnet(new[] { '\u0645' }, new[] { '\u0645' }, new[] { '\u0645' }, new[] { '\u0645' }), true, true), // Meem
            ['\u0646'] = new ArabicGlyph('\uFEE5', '\uFEE6', '\uFEE7', '\uFEE8', new ArabicGlyphFishnet(new[] { '\u0646' }, new[] { '\u0646' }, new[] { '\u0646' }, new[] { '\u0646' }), true, true), // Noon
            ['\u0647'] = new ArabicGlyph('\uFEE9', '\uFEEA', '\uFEEB', '\uFEEC', new ArabicGlyphFishnet(new[] { '\u0647' }, new[] { '\u0647' }, new[] { '\u0647' }, new[] { '\u0647' }), true, true), // Heh
            ['\u0648'] = new ArabicGlyph('\uFEED', '\uFEEE', '\0', '\0', new ArabicGlyphFishnet(new[] { '\u0648' }, new[] { '\u0648' }, null, null), true, false), // Waw
            ['\u0649'] = new ArabicGlyph('\uFEEF', '\uFEF0', '\0', '\0', new ArabicGlyphFishnet(new[] { '\u0649' }, new[] { '\u0649' }, null, null), true, false), // Alef maksura
            ['\u064A'] = new ArabicGlyph('\uFEF1', '\uFEF2', '\uFEF3', '\uFEF4', new ArabicGlyphFishnet(new[] { '\u064A' }, new[] { '\u064A' }, new[] { '\u064A' }, new[] { '\u064A' }), true, true), // Yeh
            ['\u0640'] = new ArabicGlyph('\u0640', '\u0640', '\u0640', '\u0640', new ArabicGlyphFishnet(new[] { '\u0640' }, new[] { '\u0640' }, new[] { '\u0640' }, new[] { '\u0640' }), true, true), // Tatweel
        };

        private static readonly Dictionary<char, LamAlefGlyph> LamAlefGlyphs = new Dictionary<char, LamAlefGlyph>
        {
            ['\u0622'] = new LamAlefGlyph('\uFEF5', '\uFEF6'), // Lam + Alef with madda
            ['\u0623'] = new LamAlefGlyph('\uFEF7', '\uFEF8'), // Lam + Alef with hamza above
            ['\u0625'] = new LamAlefGlyph('\uFEF9', '\uFEFA'), // Lam + Alef with hamza below
            ['\u0627'] = new LamAlefGlyph('\uFEFB', '\uFEFC'), // Lam + Alef
        };

        private static readonly HashSet<char> ArabicDiacritics = new HashSet<char>
        {
            '\u064B', '\u064C', '\u064D', '\u064E', '\u064F', '\u0650', '\u0651', '\u0652', '\u0653', '\u0654', '\u0655', '\u0670'
        };

        /// <summary>
        /// Checks if a character is an Arabic-Indic or Extended Arabic-Indic digit.
        /// </summary>
        private static bool IsArabicNumber(char c)
        {
            // Arabic-Indic digits: ٠ (U+0660) to ٩ (U+0669)
            // Extended Arabic-Indic digits (Persian): ۰ (U+06F0) to ۹ (U+06F9)
            return (c >= '\u0660' && c <= '\u0669') || (c >= '\u06F0' && c <= '\u06F9');
        }

        /// <summary>
        /// Maps characters that need mirroring in RTL context to their mirrored forms.
        /// This includes parentheses, brackets, braces, and other paired symbols.
        /// </summary>
        private static readonly Dictionary<char, char> MirroredCharacters = new Dictionary<char, char>
        {
            // Parentheses
            ['('] = ')',
            [')'] = '(',
            
            // Square brackets
            ['['] = ']',
            [']'] = '[',
            
            // Mathematical symbols
            ['≤'] = '≥',
            ['≥'] = '≤',
            ['«'] = '»',
            ['»'] = '«',
            ['‹'] = '›',
            ['›'] = '‹',
            
            // Other paired symbols
            ['⟨'] = '⟩',
            ['⟩'] = '⟨',
            ['⟪'] = '⟫',
            ['⟫'] = '⟪',
            ['⟮'] = '⟯',
            ['⟯'] = '⟮',
            ['⦃'] = '⦄',
            ['⦄'] = '⦃',
            ['⦅'] = '⦆',
            ['⦆'] = '⦅',
            ['⦇'] = '⦈',
            ['⦈'] = '⦇',
            ['⦉'] = '⦊',
            ['⦊'] = '⦉',
            ['⦋'] = '⦌',
            ['⦌'] = '⦋',
            ['⦍'] = '⦎',
            ['⦎'] = '⦍',
            ['⦏'] = '⦐',
            ['⦐'] = '⦏',
        };

        /// <summary>
        /// Formats Arabic text, replacing characters with their contextual presentation forms
        /// and mirroring paired symbols (parentheses, brackets, braces) for proper RTL display.
        /// </summary>
        public static string Format(string input, TMP_FontAsset fontAsset = null)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }

            if (!ContainsArabicCharacters(input) || ContainsPresentationForms(input))
            {
                return input;
            }

            StringBuilder builder = new StringBuilder(input.Length);
            bool previousIsArabic = false;
            bool previousCanConnectAfter = false;

            for (int i = 0; i < input.Length; i++)
            {
                char current = input[i];

                if (ArabicDiacritics.Contains(current))
                {
                    builder.Append(current);
                    continue;
                }

                if (current == '\u0640') // Tatweel
                {
                    builder.Append(current);
                    previousCanConnectAfter = true;
                    continue;
                }

                // Handle Arabic numbers
                if (IsArabicNumber(current))
                {
                    builder.Append(current);
                    // Arabic numbers are part of RTL context but don't connect
                    previousIsArabic = true;
                    previousCanConnectAfter = false;
                    continue;
                }

                if (!Glyphs.TryGetValue(current, out ArabicGlyph glyph)
                    && current != '\u0640')
                {
                    builder.Append(current);
                    previousIsArabic = false;
                    previousCanConnectAfter = false;
                    continue;
                }

                bool connectPrev = previousIsArabic && previousCanConnectAfter && glyph.ConnectsBefore;

                // Check for Lam-Alef ligature (skip diacritics between Lam and Alef)
                if (current == LamCharacter)
                {
                    // Look ahead, skipping diacritics and Tatweel to find Alef
                    int alefIndex = -1;
                    for (int j = i + 1; j < input.Length; j++)
                    {
                        char next = input[j];
                        
                        // Skip diacritics and Tatweel
                        if (ArabicDiacritics.Contains(next) || next == '\u0640')
                        {
                            continue;
                        }
                        
                        // Check if this is an Alef variant that can form Lam-Alef ligature
                        if (LamAlefGlyphs.TryGetValue(next, out LamAlefGlyph lamAlef))
                        {
                            alefIndex = j;
                            char ligatureChar = connectPrev ? lamAlef.Final : lamAlef.Isolated;

                            // Try to find available ligature glyph, fallback to base Alef if needed
                            if (!HasGlyph(fontAsset, ligatureChar))
                            {
                                // Try the other ligature form
                                char alternateLigature = connectPrev ? lamAlef.Isolated : lamAlef.Final;
                                if (HasGlyph(fontAsset, alternateLigature))
                                {
                                    ligatureChar = alternateLigature;
                                }
                                else if (HasGlyph(fontAsset, next))
                                {
                                    // Fallback to base Alef character
                                    ligatureChar = next;
                                }
                            }

                            builder.Append(ligatureChar);
                            
                            // Skip to after the Alef (including any diacritics in between)
                            i = alefIndex;
                            previousIsArabic = true;
                            previousCanConnectAfter = false; // Lam-Alef ligature never connects to the following letter
                            break;
                        }
                        
                        // If we hit an Arabic number, stop looking (numbers don't form ligatures)
                        if (IsArabicNumber(next))
                        {
                            break;
                        }
                        
                        // If we hit another Arabic letter (not Alef variant that we already checked), stop looking
                        if (Glyphs.ContainsKey(next))
                        {
                            break;
                        }
                        
                        // If we hit a non-Arabic character, stop looking
                        break;
                    }
                    
                    // If we found and processed Lam-Alef, continue to next character
                    if (alefIndex != -1)
                    {
                        continue;
                    }
                }

                bool connectNext = false;

                for (int j = i + 1; j < input.Length; j++)
                {
                    char next = input[j];

                    if (ArabicDiacritics.Contains(next))
                    {
                        continue;
                    }

                    // Skip Arabic numbers when checking connections (they don't connect)
                    if (IsArabicNumber(next))
                    {
                        connectNext = false;
                        break;
                    }

                    if (!Glyphs.TryGetValue(next, out ArabicGlyph nextGlyph))
                    {
                        connectNext = false;
                        break;
                    }

                    if (current == LamCharacter && LamAlefGlyphs.ContainsKey(next))
                    {
                        connectNext = glyph.ConnectsAfter;
                    }
                    else
                    {
                        connectNext = glyph.ConnectsAfter && nextGlyph.ConnectsBefore;
                    }

                    break;
                }

                char shaped = glyph.GetForm(connectPrev, connectNext);

                // Simple swap: if font doesn't have the character, try alternates, then base
                // Get the appropriate alternates array for this form
                char[] alternatesToUse = null;
                if (connectPrev && connectNext && glyph.Medial != '\0')
                {
                    alternatesToUse = glyph.agf.med_alternates;
                }
                else if (connectPrev && glyph.Final != '\0')
                {
                    alternatesToUse = glyph.agf.f_alternates;
                }
                else if (connectNext && glyph.Initial != '\0')
                {
                    alternatesToUse = glyph.agf.i_alternates;
                }
                else
                {
                    alternatesToUse = glyph.agf.iso_Alternates;
                }

                // Swap in place: try primary, then alternates, then base
                if (!HasGlyph(fontAsset, shaped))
                {
                    shaped = TryGetAvailableGlyph(fontAsset, shaped, alternatesToUse, current);
                }

                builder.Append(shaped);
                previousIsArabic = true;
                // Track connection capability based on intended connection, not actual form used
                // This ensures tatweel and other extensions maintain proper connection chains
                previousCanConnectAfter = glyph.ConnectsAfter && connectNext;
            }
            
            // A reversal fix for mixed text where there is both arabic and non-arabic, and mirrored symbols.
            var postRes = FixContinuousLTR(builder.ToString());
            postRes = FixMirroredSymbols(postRes);
            postRes = FixColonSpacing(postRes);
            
            return postRes;
        }

        /// <summary>
        /// Ensures colons have a space after them and no space before, per Arabic typography conventions.
        /// </summary>
        private static string FixColonSpacing(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            var sb = new StringBuilder(input.Length);
            for (int i = 0; i < input.Length; i++)
            {
                // Skip space that immediately precedes a colon
                if (input[i] == ' ' && i + 1 < input.Length && input[i + 1] == ':')
                    continue;
                sb.Append(input[i]);
                if (input[i] == ':' && i + 1 < input.Length && input[i + 1] != ' ')
                    sb.Append(' ');
            }
            return sb.ToString();
        }

        private static string FixMirroredSymbols(string postRes)
        {
            if (string.IsNullOrEmpty(postRes))
                return postRes;

            var sb = new StringBuilder(postRes.Length);

            foreach (char c in postRes)
            {
                if (MirroredCharacters.TryGetValue(c, out char mirrored))
                    sb.Append(mirrored);
                else
                    sb.Append(c);
            }

            return sb.ToString();
        }

        /// <summary>
        /// Fixes placement of words in continuous text like: "Hello, world! I am a program." which might come in as "program a am I..." worded in reverse.
        /// Fixes done in place and in sections avoiding any other edits that might damaged previously processed text.
        /// </summary>
        /// <param name="postRes"></param>
        /// <returns></returns>
        private static string FixContinuousLTR(string postRes)
        {
            if (string.IsNullOrEmpty(postRes))
                return postRes;

            var result = new StringBuilder(postRes.Length);
            int i = 0;

            while (i < postRes.Length)
            {
                // Check if we're at a rich text tag
                if (postRes[i] == '<')
                {
                    // Find the end of the tag
                    int tagEnd = postRes.IndexOf('>', i);
                    if (tagEnd == -1)
                    {
                        // Malformed tag, just append and continue
                        result.Append(postRes[i]);
                        i++;
                        continue;
                    }

                    // Append the entire tag as-is (including the '>')
                    result.Append(postRes, i, tagEnd - i + 1);
                    i = tagEnd + 1;
                    continue;
                }

                // Check if we're at the start of an LTR segment
                if (IsLatinSegmentChar(postRes[i]))
                {
                    int start = i;
                    
                    // Collect the entire LTR segment
                    while (i < postRes.Length && postRes[i] != '<' && (IsLatinSegmentChar(postRes[i]) || postRes[i] == ' '))
                    {
                        i++;
                    }

                    var wordTest = string.Empty;

                    // Reverse the entire LTR segment
                    for (int j = i - 1; j >= start; j--)
                    {
                        wordTest += postRes[j];
                    }
                    
                    result.Append(wordTest);
                }
                else
                {
                    // Non-LTR character (Arabic, etc.), append as-is
                    result.Append(postRes[i]);
                    i++;
                }
            }

            return result.ToString();
        }

        private static bool IsLatinSegmentChar(char c)
        {
            // Letters and digits
            if (c >= 'A' && c <= 'Z') return true;
            if (c >= 'a' && c <= 'z') return true;
            if (c >= '0' && c <= '9') return true;

            // Internal punctuation that appears *inside* English sequences
            // Example: "can't", "e-mail", "hello!", "font-size"
            switch (c)
            {
                case '!':
                case '?':
                case '.':
                case ',':
                case ':':
                case ';':
                case '-':
                case '_':
                case '/':
                case '\\':
                case '\'':
                    return true; // SAFE punctuation
            }

            return false;
        }

        /// <summary>
        /// Reverses a string so that when FixContinuousLTR reverses it during Arabic formatting,
        /// the original value is displayed correctly. Use for dynamic format arguments (numbers, etc.)
        /// that would otherwise appear reversed in Arabic RTL text.
        /// </summary>
        /// <param name="value">The string to pre-reverse (e.g., "123" becomes "321")</param>
        /// <returns>Reversed string, or original if null/empty</returns>
        internal static string ReverseForRTLDisplay(string value)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            char[] chars = value.ToCharArray();
            Array.Reverse(chars);
            return new string(chars);
        }

        private static bool ContainsArabicCharacters(string input)
        {
            foreach (char c in input)
            {
                if (Glyphs.ContainsKey(c) || IsArabicNumber(c))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool ContainsPresentationForms(string input)
        {
            foreach (char c in input)
            {
                if (c >= '\uFE70' && c <= '\uFEFF')
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasGlyph(TMP_FontAsset fontAsset, char character)
        {
            if (character == '\0')
            {
                return false;
            }
            
            if (fontAsset == null)
            {
                return true;
            }

            return fontAsset.HasCharacter(character, true, true);
        }

        /// <summary>
        /// Tries to find an available glyph from the primary character or its alternates.
        /// Returns the first available character from: primary, alternates array, or base character.
        /// </summary>
        private static char TryGetAvailableGlyph(TMP_FontAsset fontAsset, char primary, char[] alternates, char baseChar)
        {
            // Try primary character first
            if (HasGlyph(fontAsset, primary))
            {
                return primary;
            }

            // Try alternates if available
            if (alternates != null)
            {
                foreach (char alternate in alternates)
                {
                    if (alternate != '\0' && HasGlyph(fontAsset, alternate))
                    {
                        return alternate;
                    }
                }
            }

            // Fall back to base character only if it's different from primary
            // (to avoid infinite loops and preserve connection state)
            if (baseChar != '\0' && baseChar != primary && HasGlyph(fontAsset, baseChar))
            {
                return baseChar;
            }

            // If nothing is available, return primary (will be replaced by system fallback)
            return primary;
        }
    }
}
