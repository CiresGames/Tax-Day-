#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Text;

namespace AHAKuo.Signalia.GameSystems.InlineScript.Internal.Editor
{
    public static class SyntaxHighlightingUtility
    {
        private const string KeywordColor = "6FC3FF";
        private const string TypeColor = "F2D98D";
        private const string StringColor = "C897F5";
        private const string CommentColor = "7BAF7C";
        private const string NumberColor = "F4B67A";
        private const char ZeroWidthSpace = '\u200B';

        private static readonly HashSet<string> Keywords = new HashSet<string>(StringComparer.Ordinal)
        {
            "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked", "class", "const",
            "continue", "decimal", "default", "delegate", "do", "double", "else", "enum", "event", "explicit",
            "extern", "false", "finally", "fixed", "float", "for", "foreach", "goto", "if", "implicit", "in",
            "int", "interface", "internal", "is", "lock", "long", "namespace", "new", "null", "object",
            "operator", "out", "override", "params", "private", "protected", "public", "readonly", "ref", "return",
            "sbyte", "sealed", "short", "sizeof", "stackalloc", "static", "string", "struct", "switch", "this",
            "throw", "true", "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort", "using", "virtual",
            "void", "volatile", "while"
        };

        public static string ToRichText(string source)
        {
            if (string.IsNullOrEmpty(source))
            {
                return string.Empty;
            }

            var builder = new StringBuilder(source.Length * 2);
            var length = source.Length;
            var index = 0;

            while (index < length)
            {
                var ch = source[index];

                if (IsLineComment(source, index))
                {
                    var end = index + 2;
                    while (end < length && source[end] != '\n')
                    {
                        end++;
                    }

                    builder.Append(WrapWithColor(source.Substring(index, end - index), CommentColor));
                    index = end;
                    continue;
                }

                if (IsBlockComment(source, index, out var blockEnd))
                {
                    builder.Append(WrapWithColor(source.Substring(index, blockEnd - index), CommentColor));
                    index = blockEnd;
                    continue;
                }

                if (IsVerbatimString(source, index, out var verbatimEnd))
                {
                    builder.Append(WrapWithColor(source.Substring(index, verbatimEnd - index), StringColor));
                    index = verbatimEnd;
                    continue;
                }

                if (IsRegularString(source, index, out var stringEnd))
                {
                    builder.Append(WrapWithColor(source.Substring(index, stringEnd - index), StringColor));
                    index = stringEnd;
                    continue;
                }

                if (IsCharLiteral(source, index, out var charEnd))
                {
                    builder.Append(WrapWithColor(source.Substring(index, charEnd - index), StringColor));
                    index = charEnd;
                    continue;
                }

                if (char.IsLetter(ch) || ch == '_')
                {
                    var start = index;
                    index++;
                    while (index < length && (char.IsLetterOrDigit(source[index]) || source[index] == '_'))
                    {
                        index++;
                    }

                    var token = source.Substring(start, index - start);
                    if (Keywords.Contains(token))
                    {
                        builder.Append(WrapWithColor(token, KeywordColor));
                    }
                    else if (char.IsUpper(token[0]))
                    {
                        builder.Append(WrapWithColor(token, TypeColor));
                    }
                    else
                    {
                        builder.Append(Escape(token));
                    }
                    continue;
                }

                if (char.IsDigit(ch))
                {
                    var start = index;
                    index++;
                    while (index < length && IsNumericCharacter(source[index]))
                    {
                        index++;
                    }

                    builder.Append(WrapWithColor(source.Substring(start, index - start), NumberColor));
                    continue;
                }

                builder.Append(Escape(ch.ToString()));
                index++;
            }

            return builder.ToString();
        }

        private static bool IsLineComment(string source, int index)
        {
            return index + 1 < source.Length && source[index] == '/' && source[index + 1] == '/';
        }

        private static bool IsBlockComment(string source, int index, out int endIndex)
        {
            if (index + 1 < source.Length && source[index] == '/' && source[index + 1] == '*')
            {
                var end = index + 2;
                while (end < source.Length - 1)
                {
                    if (source[end] == '*' && source[end + 1] == '/')
                    {
                        end += 2;
                        endIndex = end;
                        return true;
                    }
                    end++;
                }

                endIndex = source.Length;
                return true;
            }

            endIndex = index;
            return false;
        }

        private static bool IsVerbatimString(string source, int index, out int endIndex)
        {
            if (index + 1 < source.Length && source[index] == '@' && source[index + 1] == '"')
            {
                var end = index + 2;
                while (end < source.Length)
                {
                    if (source[end] == '"')
                    {
                        if (end + 1 < source.Length && source[end + 1] == '"')
                        {
                            end += 2;
                            continue;
                        }

                        end++;
                        break;
                    }
                    end++;
                }

                endIndex = end;
                return true;
            }

            endIndex = index;
            return false;
        }

        private static bool IsRegularString(string source, int index, out int endIndex)
        {
            if (source[index] == '"')
            {
                var end = index + 1;
                while (end < source.Length)
                {
                    if (source[end] == '\\')
                    {
                        end += 2;
                        continue;
                    }

                    if (source[end] == '"')
                    {
                        end++;
                        break;
                    }

                    end++;
                }

                endIndex = end;
                return true;
            }

            endIndex = index;
            return false;
        }

        private static bool IsCharLiteral(string source, int index, out int endIndex)
        {
            const char singleQuote = '\u0027';
            const char escapeCharacter = '\\';

            if (source[index] == singleQuote)
            {
                var end = index + 1;
                while (end < source.Length)
                {
                    if (source[end] == escapeCharacter)
                    {
                        end += 2;
                        continue;
                    }

                    if (source[end] == singleQuote)
                    {
                        end++;
                        break;
                    }

                    end++;
                }

                endIndex = end;
                return true;
            }

            endIndex = index;
            return false;
        }

        private static bool IsNumericCharacter(char ch)
        {
            return char.IsDigit(ch) || ch == '_' || ch == '.' || ch == 'f' || ch == 'F' || ch == 'd' || ch == 'D' ||
                   ch == 'm' || ch == 'M' || ch == 'u' || ch == 'U' || ch == 'l' || ch == 'L' || ch == 'x' || ch == 'X' ||
                   ch == 'b' || ch == 'B' || ch == 'e' || ch == 'E' || ch == '+' || ch == '-';
        }

        private static string WrapWithColor(string text, string color)
        {
            return $"<color=#{color}>{Escape(text)}</color>";
        }

        private static string Escape(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            var builder = new StringBuilder(value.Length + 4);

            foreach (var ch in value)
            {
                switch (ch)
                {
                    case '<':
                        builder.Append(ZeroWidthSpace);
                        builder.Append('<');
                        break;
                    case '&':
                        builder.Append('&');
                        break;
                    default:
                        builder.Append(ch);
                        break;
                }
            }

            return builder.ToString();
        }
    }
}
#endif
