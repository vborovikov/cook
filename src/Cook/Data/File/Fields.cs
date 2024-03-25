namespace Cook.Data.File
{
    using System;
    using System.Diagnostics;
    using System.Globalization;
    using Text;

    // https://tools.ietf.org/html/rfc4180

    enum FieldCategory
    {
        Regular,
        Enclosed,
        Array,
    }

    [DebuggerDisplay("{" + nameof(Span) + ",nq}")]
    readonly ref struct FieldSpan
    {
        public FieldSpan(ReadOnlySpan<char> span, FieldCategory category)
        {
            this.Span = span;
            this.Category = category;
        }

        public ReadOnlySpan<char> Span { get; }
        public FieldCategory Category { get; }

        public static implicit operator ReadOnlySpan<char>(FieldSpan field) => field.Span;
    }

    [DebuggerDisplay("{" + nameof(Span) + ",nq}")]
    readonly ref struct EnclosureSpan
    {
        public EnclosureSpan(ReadOnlySpan<char> span, int index)
        {
            this.Span = span;
            this.Index = index;
        }

        public ReadOnlySpan<char> Span { get; }
        public int Index { get; }
        public int EndIndex => this.Index + this.Span.Length;

        public static implicit operator ReadOnlySpan<char>(EnclosureSpan field) => field.Span;
    }

    static class Fields
    {
        internal const char Delimiter = ',';
        internal const char Encloser = '"';
        internal const char EncloserEscape = '\\';
        internal const char ArrayOpener = '[';
        internal const char ArrayCloser = ']';
        internal const char ArrayItemDelimiter = ',';
        internal static readonly char[] Separators = { ' ', '\r', '\n', '\t', '\xA0', /*'\f', '\v'*/ };
        internal static readonly char[] Delimiters = { Delimiter };
        internal static readonly char[] TrimChars = { Delimiter, Encloser, EncloserEscape, ArrayOpener, ArrayCloser };

        public ref struct FieldEnumerator
        {
            private readonly ReadOnlySpan<char> record;
            private int index;

            public FieldEnumerator(ReadOnlySpan<char> record)
            {
                this.record = record;
                this.index = 0;
                this.Current = default;
            }

            public FieldSpan Current { get; private set; }

            public FieldEnumerator GetEnumerator() => this;

            public void Reset()
            {
                this.index = 0;
                this.Current = default;
            }

            public bool MoveNext()
            {
                if (this.index >= this.record.Length)
                    return false;

                var category = FieldCategory.Regular;
                var start = this.record.ClampStart(this.index, Separators);
                var end = start;
                if (this.record[start] == Encloser)
                {
                    // escaped field
                    category = FieldCategory.Enclosed;
                    end = this.record.ClampQuotes(start, Encloser, Delimiters);
                }
                else if (this.record[start] == ArrayOpener)
                {
                    // array field
                    end = this.record.ClampBrackets(start, ArrayOpener, ArrayCloser, Encloser, Delimiters);
                    if (end > start)
                    {
                        for (var i = end - 1; i > start; --i)
                        {
                            if (char.IsWhiteSpace(this.record[i]))
                                continue;
                            if (this.record[i] == ArrayCloser)
                            {
                                category = FieldCategory.Array;
                                break;
                            }
                            break;
                        }
                    }
                }
                else
                {
                    // regular field
                    end = this.record.IndexOfChar(Delimiter, start);
                    if (end < 0)
                        end = this.record.Length;
                }

                this.Current = new FieldSpan(this.record[start..end].TrimEnd(Separators), category);
                this.index = end + 1;

                return true;
            }
        }

        public static FieldEnumerator Parse(ReadOnlySpan<char> record)
        {
            return new FieldEnumerator(record);
        }

        public ref struct EnclosureEnumerator
        {
            private ReadOnlySpan<char> span;
            private int index;

            public EnclosureEnumerator(ReadOnlySpan<char> text)
            {
                this.span = text;
                this.index = 0;
                this.Current = default;
            }

            public EnclosureSpan Current { get; private set; }

            public EnclosureEnumerator GetEnumerator() => this;

            public bool MoveNext()
            {
                if (this.span.IsEmpty)
                    return false;

                var start = this.span.IndexOfChar(Encloser);
                if (start < 0)
                    return false;

                var end = this.span.IndexOfChar(Encloser, start + 1);
                if (end < 0)
                    return false;

                ++end;
                this.Current = new EnclosureSpan(this.span[start..end], this.index + start);
                this.span = this.span[end..];
                this.index += end;

                return true;
            }
        }

        public static EnclosureEnumerator Unclose(ReadOnlySpan<char> fieldSpan)
        {
            return new EnclosureEnumerator(fieldSpan);
        }

        public static ReadOnlySpan<char> Unclose(this ReadOnlySpan<char> source, Span<char> span)
        {
            var length = 0;

            var destination = span;
            Span<char> buffer = stackalloc char[span.Length];
            foreach (var enclosure in Unclose(source))
            {
                var unclosed = enclosure.Span.Unescape(buffer);
                if (unclosed.Length > 0)
                {
                    unclosed.CopyTo(destination);
                    destination = destination[unclosed.Length..];
                    length += unclosed.Length;
                }
            }

            return span[..length];
        }

        private static ReadOnlySpan<char> Unescape(this ReadOnlySpan<char> source, Span<char> span)
        {
            if (source.IsEmpty)
                return source;

            if (source.IndexOfChar(EncloserEscape) < 0)
                return source[1..^1];

            // eat Encloser
            // if EncloserEscape found then treat next char as special
            // special:
            //      EncloserEscape -> copy
            //      Encloser -> copy
            //      't' -> ' '
            //      'n' -> ' '
            //      'u' -> unicode (\u####)

            var x = 0;
            var escape = false;
            var unicode = 0;
            for (var i = 0; i != source.Length; ++i)
            {
TestAgain:
                if (escape)
                {
                    //todo: handle html entities
                    if (unicode > 0)
                    {
                        if (!char.IsAsciiHexDigit(source[i]) || ((i - unicode) == 4))
                        {
                            if (int.TryParse(source[unicode..i], NumberStyles.HexNumber, null, out var codePoint))
                            {
                                span[x++] = Convert.ToChar(codePoint);
                            }

                            unicode = 0;
                            escape = false;
                            goto TestAgain;
                        }
                    }
                    else if (source[i] == 'u')
                    {
                        unicode = i + 1;
                    }
                    else
                    {
                        span[x++] = source[i] switch
                        {
                            't' or 'n' => ' ',
                            var ch => ch
                        };
                        escape = false;
                    }
                }
                else
                {
                    escape = source[i] == EncloserEscape;
                    if (!escape && source[i] != Encloser)
                    {
                        span[x++] = source[i];
                    }
                }
            }

            return span[..x];
        }
    }
}
