namespace Cook.Data.Text
{
    using System;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using System.Text;

    public static class CharSpanExtensions
    {
        private const string WhiteSpace = " ";

        public static void Clear(this Span<char> span, char ch)
        {
            for (var i = 0; i != span.Length; ++i)
            {
                span[i] = ch;
            }
        }

        public static bool IsChar(this ReadOnlySpan<char> span, char ch)
        {
            var occurrences = 0;
            for (var i = 0; i != span.Length; ++i)
            {
                if (span[i] == ch)
                {
                    ++occurrences;
                    goto Next;
                }

                if (char.IsWhiteSpace(span[i]))
                {
                    goto Next;
                }

                return false;
            Next:
                ;
            }

            return occurrences == 1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IndexOfChar(this ReadOnlySpan<char> span, char value, int start = 0)
        {
#if true
            if (start > 0)
                span = span.Slice(start);

            var index = span.IndexOf(value);
            if (index >= 0)
                index += start;

            return index;
#else
            var length = span.Length;
            if (length == 0 || start >= length)
                return -1;

            ref char ch = ref MemoryMarshal.GetReference(span);
            ch = ref Unsafe.Add(ref ch, start);
            var index = start;

            while (index < length)
            {
                if (ch == value)
                    return index;

                ch = ref Unsafe.Add(ref ch, 1);
                ++index;
            }

            return -1;
#endif
        }

        /// <summary>
        /// Removes all leading white-space characters from the span.
        /// </summary>
        /// <param name="span">The source span from which the characters are removed.</param>
        public static ReadOnlySpan<char> TrimStart(this ReadOnlySpan<char> span, out int start)
        {
            start = 0;
            for (; start != span.Length; ++start)
            {
                if (!char.IsWhiteSpace(span[start]))
                {
                    break;
                }
            }

            return span.Slice(start);
        }

        /// <summary>
        /// Removes all leading occurrences of a set of characters specified
        /// in a readonly span from the span.
        /// </summary>
        /// <param name="span">The source span from which the characters are removed.</param>
        /// <param name="trimChars">The span which contains the set of characters to remove.</param>
        /// <remarks>If <paramref name="trimChars"/> is empty, white-space characters are removed instead.</remarks>
        public static ReadOnlySpan<char> TrimStart(this ReadOnlySpan<char> span, ReadOnlySpan<char> trimChars, out int start)
        {
            if (trimChars.IsEmpty)
            {
                return span.TrimStart(out start);
            }

            start = 0;
            for (; start != span.Length; ++start)
            {
                for (var i = 0; i != trimChars.Length; ++i)
                {
                    if (span[start] == trimChars[i])
                    {
                        goto Next;
                    }
                }

                break;
            Next:
                ;
            }

            return span.Slice(start);
        }

        /// <summary>
        /// Delimits all leading occurrences of quotation characters from the span.
        /// </summary>
        /// <param name="span">The source span from which the characters are removed.</param>
        /// <param name="quoteChars"></param>
        /// <param name="stopChars"></param>
        /// <returns></returns>
        public static int ClampQuotes(this ReadOnlySpan<char> span, int start, char quoteChar, ReadOnlySpan<char> stopChars)
        {
            var insideQuotes = false;
            for (; start < span.Length; ++start)
            {
                insideQuotes ^= span[start] == quoteChar;
                if (!insideQuotes)
                {
                    for (var i = 0; i != stopChars.Length; ++i)
                    {
                        if (span[start] == stopChars[i])
                        {
                            return start;
                        }
                    }
                }
            }

            return start;
        }

        public static int ClampQuotes(this ReadOnlySpan<char> span, ReadOnlySpan<char> quoteChars, ReadOnlySpan<char> stopChars)
        {
            var quoteChar = quoteChars.Contains(span[0]) ? span[0] : quoteChars[0];
            return span.ClampQuotes(0, quoteChar, stopChars);
        }

        /// <summary>
        /// Delimits all leading occurrences of brace characters from the span.
        /// </summary>
        /// <param name="span">The source span from which the characters are removed.</param>
        /// <param name="braceChars"></param>
        /// <param name="escapeChar"></param>
        /// <param name="stopChars"></param>
        /// <returns></returns>
        public static int ClampBrackets(this ReadOnlySpan<char> span, int start, 
            char bracketOpener, char bracketCloser, char escapeChar, ReadOnlySpan<char> stopChars)
        {
            var insideBrackets = span[start] == bracketOpener;
            var escaped = false;
            for (; start < span.Length; ++start)
            {
                if (insideBrackets)
                {
                    escaped ^= span[start] == escapeChar;
                    if (!escaped && span[start] == bracketCloser)
                    {
                        insideBrackets = false;
                    }
                }
                else
                {
                    for (var i = 0; i != stopChars.Length; ++i)
                    {
                        if (span[start] == stopChars[i])
                        {
                            return start;
                        }
                    }
                }
            }

            return start;
        }

        public static int ClampStart(this ReadOnlySpan<char> span, int start, ReadOnlySpan<char> trimChars)
        {
            for (; start < span.Length; start++)
            {
                for (int i = 0; i < trimChars.Length; i++)
                {
                    if (span[start] == trimChars[i])
                    {
                        goto Next;
                    }
                }

                break;
            Next:
                ;
            }

            return start;
        }

        public static int ClampStartUntil(this ReadOnlySpan<char> span, int start, ReadOnlySpan<char> stopChars)
        {
            for (; start < span.Length; ++start)
            {
                for (var i = 0; i < stopChars.Length; ++i)
                {
                    if (span[start] == stopChars[i])
                    {
                        return start;
                    }
                }
            }

            return start;
        }

        public static int ClampEnd(ReadOnlySpan<char> span, int start, ReadOnlySpan<char> trimChars)
        {
            var end = span.Length - 1;
            for (; end >= start; end--)
            {
                if (!trimChars.Contains(span[end]))
                {
                    break;
                }
            }

            return end - start + 1;
        }

        /// <summary>
        ///  Any consecutive white-space (including tabs, newlines) is replaced with whatever is in normalizeTo.
        /// </summary>
        /// <param name="input">Input string.</param>
        /// <param name="normalizeTo">Character which is replacing whitespace.</param>
        /// <remarks>Based on http://stackoverflow.com/a/25023688/897326 </remarks>
        public static string NormalizeWhiteSpace(this ReadOnlySpan<char> input, 
            ReadOnlySpan<char> whiteSpace, ReadOnlySpan<char> normalizeTo)
        {
            if (input.IsEmpty)
            {
                return string.Empty;
            }

            var output = new StringBuilder();
            var skipped = false;

            for (var i = 0; i != input.Length; ++i)
            {
                if (whiteSpace.Contains(input[i]))
                {
                    if (!skipped)
                    {
                        output.Append(normalizeTo);
                        skipped = true;
                    }
                }
                else
                {
                    skipped = false;
                    output.Append(input[i]);
                }
            }

            return output.ToString();
        }

        public static string NormalizeWhiteSpace(this string input, 
            ReadOnlySpan<char> whiteSpace, string normalizeTo = WhiteSpace) =>
            NormalizeWhiteSpace(input.AsSpan(), whiteSpace, normalizeTo);
    }
}
